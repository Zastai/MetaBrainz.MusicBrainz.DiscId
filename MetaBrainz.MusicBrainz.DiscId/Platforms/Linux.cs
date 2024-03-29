using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using JetBrains.Annotations;

using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms;

internal sealed class Linux : Unix {

  public Linux() : base(Linux.Features) { }

  private const DiscReadFeature Features =
    DiscReadFeature.TableOfContents |
    DiscReadFeature.MediaCatalogNumber |
    DiscReadFeature.TrackIsrc |
    DiscReadFeature.CdText;

  private const string GenericDevice = "/dev/cdrom";

  public override IEnumerable<string> AvailableDevices {
    get {
      string[]? devices = null;
      try {
        using var info = File.OpenText("/proc/sys/dev/cdrom/info");
        string? line;
        while ((line = info.ReadLine()) != null) {
          if (!line.StartsWith("drive name:", StringComparison.Ordinal)) {
            continue;
          }
          devices = line.Substring(11).Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
          break;
        }
      }
      catch {
        // ignore
      }
      if (devices != null) {
        Array.Reverse(devices);
        foreach (var device in devices) {
          yield return string.Concat("/dev/", device);
        }
      }
    }
  }

  public override string? DefaultDevice {
    get { // Prefer the generic device name (typically a symlink to the "preferred" device)
      using var fd = NativeApi.OpenDevice(Linux.GenericDevice);
      return fd.IsInvalid ? base.DefaultDevice : Linux.GenericDevice;
    }
  }

  protected override TableOfContents ReadTableOfContents(string device, DiscReadFeature features) {
    using var fd = NativeApi.OpenDevice(device);
    if (fd.IsInvalid) {
      throw new IOException($"Failed to open '{device}'.", new UnixException());
    }
    byte first;
    byte last;
    Track[] tracks;
    { // Read the TOC itself
      NativeApi.GetTableOfContents(fd, out var rawToc);
      first = rawToc.FirstTrack;
      last = rawToc.LastTrack;
      tracks = new Track[last + 1];
      var i = 0;
      for (var trackNo = rawToc.FirstTrack; trackNo <= rawToc.LastTrack; ++trackNo, ++i) { // Add the regular tracks.
        if (rawToc.Tracks[i].TrackNumber != trackNo) {
          throw new InvalidDataException($"Internal logic error; first track is {rawToc.FirstTrack}, but entry at index {i} " +
                                         $"claims to be track {rawToc.Tracks[i].TrackNumber} instead of {trackNo}.");
        }
        var isrc = ((features & DiscReadFeature.TrackIsrc) != 0) ? NativeApi.GetTrackIsrc(fd, trackNo) : null;
        tracks[trackNo] = new Track(rawToc.Tracks[i].Address, rawToc.Tracks[i].ControlAndADR.Control, isrc);
      }
      // Next entry should be the lead-out (track number 0xAA)
      if (rawToc.Tracks[i].TrackNumber != 0xAA) {
        throw new InvalidDataException($"Internal logic error; track data ends with a record that reports track number " +
                                       $"{rawToc.Tracks[i].TrackNumber} instead of 0xAA (lead-out).");
      }
      tracks[0] = new Track(rawToc.Tracks[i].Address, rawToc.Tracks[i].ControlAndADR.Control, null);
    }
    var mcn = ((features & DiscReadFeature.MediaCatalogNumber) != 0) ? NativeApi.GetMediaCatalogNumber(fd) : null;
    RedBook.CDTextGroup? cdTextGroup = null;
    if ((features & DiscReadFeature.CdText) != 0) {
      NativeApi.GetCdTextInfo(fd, out var cdText);
      if (cdText.Data.Packs != null) {
        cdTextGroup = cdText.Data;
      }
    }
    return new TableOfContents(device, first, last, tracks, mcn, cdTextGroup);
  }

  #region Native API

  private static class NativeApi {

    #region Constants

    // in milliseconds; timeout better shouldn't happen for scsi commands -> device is reset
    private const uint DefaultSCSIRequestTimeOut = 30000;

    private enum IOCTL : ulong {

      // Generic SCSI command (uses standard SCSI MMC structures)
      SG_IO = 0x2285,

    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private enum SCSIDriverStatus : ushort {

      DRIVER_OK = 0,

      DRIVER_BUSY = 1,

      DRIVER_SOFT = 2,

      DRIVER_MEDIA = 3,

      DRIVER_ERROR = 4,

      DRIVER_INVALID = 5,

      DRIVER_TIMEOUT = 6,

      DRIVER_HARD = 7,

      DRIVER_SENSE = 8,

    }

    [Flags]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private enum SCSIFlags : uint {

      DIRECT_IO = 1, // default is indirect IO

      LUN_INHIBIT = 2, // default is to put device's lun into the 2nd byte of SCSI command

      NO_DXFER = 0x10000, // no transfer of kernel buffers to/from user space (debug indirect IO)

    }

    [Flags]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private enum SCSIInfoIOMode : uint {

      INDIRECT_IO = 0x00, // data xfer via kernel buffers (or no xfer)

      DIRECT_IO = 0x02, // direct IO requested and performed

      MIXED_IO = 0x04, // part direct, part indirect IO

    }

    [Flags]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private enum SCSIInfoStatus : uint {

      OK = 0x00, // no sense, host nor driver "noise"

      CHECK = 0x01, // something abnormal happened

    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private enum SCSITransferDirection {

      NONE = -1, // e.g. a SCSI Test Unit Ready command

      TO_DEV = -2, // e.g. a SCSI WRITE command

      FROM_DEV = -3, // e.g. a SCSI READ command

      TO_FROM_DEV = -4, // like SG_DXFER_FROM_DEV, but during indirect IO the user buffer is copied into the kernel buffers before the transfer

    }

    #endregion

    #region Structures

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private struct SCSIRequest {

      public int interface_id;

      public SCSITransferDirection dxfer_direction;

      public byte cmd_len;

      public byte mx_sb_len;

      public ushort iovec_count;

      public uint dxfer_len;

      public IntPtr dxferp;

      public IntPtr cmdp;

      public IntPtr sbp;

      public uint timeout;

      public SCSIFlags flags;

      public int pack_id;

      public IntPtr usr_ptr;

      public SAM.StatusCode status;

      public byte masked_status;

      public byte msg_status;

      public byte sb_len_wr;

      public ushort host_status;

      public SCSIDriverStatus driver_status;

      public int resid;

      public uint duration;

      public uint info;

      public SCSIInfoStatus Status => (SCSIInfoStatus) (this.info & 0x1);

      public SCSIInfoIOMode IOMode => (SCSIInfoIOMode) (this.info & 0x6);

    }

    #endregion

    #region Public Methods

    public static void GetCdTextInfo(UnixFileDescriptor fd, out MMC.CDTextDescriptor cdText) {
      var cmd = MMC.CDB.ReadTocPmaAtip.CDText();
      try {
        NativeApi.SendSCSIRequest(fd, ref cmd, out cdText);
      }
      catch (Exception e) {
        throw new IOException("Failed to retrieve CD-TEXT information.", e);
      }
      cdText.FixUp();
    }

    public static string GetMediaCatalogNumber(UnixFileDescriptor fd) {
      MMC.SubChannelMediaCatalogNumber mcn;
      var cmd = MMC.CDB.ReadSubChannel.MediaCatalogNumber();
      try {
        NativeApi.SendSCSIRequest(fd, ref cmd, out mcn);
      }
      catch (Exception e) {
        throw new IOException("Failed to retrieve media catalog number.", e);
      }
      mcn.FixUp();
      return mcn.Status.IsValid ? Encoding.ASCII.GetString(mcn.MCN) : string.Empty;
    }

    public static void GetTableOfContents(UnixFileDescriptor fd, out MMC.TOCDescriptor rawTableOfContents) {
      var cmd = MMC.CDB.ReadTocPmaAtip.TOC();
      try {
        NativeApi.SendSCSIRequest(fd, ref cmd, out rawTableOfContents);
      }
      catch (Exception e) {
        throw new IOException("Failed to retrieve table of contents.", e);
      }
      rawTableOfContents.FixUp(false);
    }

    public static string GetTrackIsrc(UnixFileDescriptor fd, byte track) {
      MMC.SubChannelISRC isrc;
      var cmd = MMC.CDB.ReadSubChannel.ISRC(track);
      try {
        NativeApi.SendSCSIRequest(fd, ref cmd, out isrc);
      }
      catch (Exception e) {
        throw new IOException($"Failed to retrieve ISRC for track {track}.", e);
      }
      isrc.FixUp();
      return isrc.Status.IsValid ? Encoding.ASCII.GetString(isrc.ISRC) : string.Empty;
    }

    public static UnixFileDescriptor OpenDevice(string name) {
      const uint O_RDONLY = 0x0000;
      const uint O_NONBLOCK = 0x0800;
      return UnixFileDescriptor.OpenPath(name, O_RDONLY | O_NONBLOCK, 0);
    }

    #endregion

    #region Private Methods

    private static void SendSCSIRequest<TCommand, TData>(UnixFileDescriptor fd, ref TCommand cmd, out TData data)
      where TCommand : struct
      where TData : struct {
      var commandLength = Marshal.SizeOf<TCommand>();
      if (commandLength > 16) {
        throw new InvalidOperationException("A SCSI command must not exceed 16 bytes in size.");
      }
      var req = new SCSIRequest {
        interface_id = 'S',
        dxfer_direction = SCSITransferDirection.FROM_DEV,
        timeout = NativeApi.DefaultSCSIRequestTimeOut,
        cmd_len = (byte) commandLength,
        mx_sb_len = 64,
        dxfer_len = (uint) Marshal.SizeOf<TData>(),
      };
      var memorySize = (uint) (req.cmd_len + req.mx_sb_len + req.dxfer_len);
      var bytes = NativeApi.AllocZero(new UIntPtr(1), new UIntPtr(memorySize));
      try {
        req.cmdp = bytes;
        req.sbp = req.cmdp + req.cmd_len;
        req.dxferp = req.sbp + req.mx_sb_len;
        Marshal.StructureToPtr(cmd, req.cmdp, false);
        try {
          if (NativeApi.SendSCSIRequest(fd.Value, IOCTL.SG_IO, ref req) != 0) {
            throw new UnixException();
          }
          if (req.status == SAM.StatusCode.CHECK_CONDITION || req.driver_status == SCSIDriverStatus.DRIVER_SENSE) {
            var response = Marshal.ReadByte(req.sbp) & 0x7f;
            throw response switch {
              0x70 or 0x71 => new ScsiException(Marshal.PtrToStructure<SPC.FixedSenseData>(req.sbp)),
              0x72 or 0x73 => new ScsiException(Marshal.PtrToStructure<SPC.DescriptorSenseData>(req.sbp)),
              _ => new IOException($"SCSI CHECK CONDITION: BAD RESPONSE CODE ({response:X2})"),
            };
          }
          data = Marshal.PtrToStructure<TData>(req.dxferp);
        }
        finally {
          Marshal.DestroyStructure<TCommand>(req.cmdp);
        }
      }
      finally {
        NativeApi.Free(bytes);
      }
    }

    [DllImport("libc", EntryPoint = "calloc", SetLastError = true)]
    private static extern IntPtr AllocZero(UIntPtr items, UIntPtr itemSize);

    [DllImport("libc", EntryPoint = "free", SetLastError = true)]
    private static extern void Free(IntPtr ptr);

    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    private static extern int SendSCSIRequest(int fd, IOCTL command, ref SCSIRequest request);

    #endregion

  }

  #endregion

}
