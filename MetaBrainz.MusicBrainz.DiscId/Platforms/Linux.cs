using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal sealed class Linux : Unix {

    public Linux() : base(DiscReadFeature.TableOfContents | DiscReadFeature.MediaCatalogNumber | DiscReadFeature.TrackIsrc) { }

    private const string GenericDevice = "/dev/cdrom";

    public override IEnumerable<string> AvailableDevices {
      get {
        string[] devices = null;
        try {
          using (var info = File.OpenText("/proc/sys/dev/cdrom/info")) {
            string line;
            while ((line = info.ReadLine()) != null) {
              if (!line.StartsWith("drive name:"))
                continue;
              devices = line.Substring(11).Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
              break;
            }
          }
        }
        catch { }
        if (devices != null) {
          Array.Reverse(devices);
          foreach (var device in devices)
            yield return string.Concat("/dev/", device);
        }
      }
    }

    public override string DefaultDevice {
      get { // Prefer the generic device name (typically a symlink to the "preferred" device)
        using (var fd = NativeApi.OpenDevice(Linux.GenericDevice))
          return fd.IsInvalid ? base.DefaultDevice : Linux.GenericDevice;
      }
    }

    protected override TableOfContents ReadTableOfContents(string device, DiscReadFeature features) {
      using (var fd = NativeApi.OpenDevice(device)) {
        if (fd.IsInvalid)
          throw new IOException($"Failed to open '{device}'.", new UnixException());
        byte first = 0;
        byte last  = 0;
        Track[] tracks = null;
        { // Read the TOC itself
          MMC.TOCDescriptor rawtoc;
          NativeApi.GetTableOfContents(fd, out rawtoc);
          first = rawtoc.FirstTrack;
          last = rawtoc.LastTrack;
          tracks = new Track[last + 1];
          var i = 0;
          for (var trackno = rawtoc.FirstTrack; trackno <= rawtoc.LastTrack; ++trackno, ++i) { // Add the regular tracks.
            if (rawtoc.Tracks[i].TrackNumber != trackno)
              throw new InvalidDataException($"Internal logic error; first track is {rawtoc.FirstTrack}, but entry at index {i} claims to be track {rawtoc.Tracks[i].TrackNumber} instead of {trackno}");
            var isrc = ((features & DiscReadFeature.TrackIsrc) != 0) ? NativeApi.GetTrackIsrc(fd, trackno) : null;
            tracks[trackno] = new Track(rawtoc.Tracks[i].Address, rawtoc.Tracks[i].ControlAndADR.Control, isrc);
          }
          // Next entry should be the leadout (track number 0xAA)
          if (rawtoc.Tracks[i].TrackNumber != 0xAA)
            throw new InvalidDataException($"Internal logic error; track data ends with a record that reports track number {rawtoc.Tracks[i].TrackNumber} instead of 0xAA (lead-out)");
          tracks[0] = new Track(rawtoc.Tracks[i].Address, rawtoc.Tracks[i].ControlAndADR.Control, null);
        }
        // TODO: If requested, try getting CD-TEXT data.
        var mcn = ((features & DiscReadFeature.MediaCatalogNumber) != 0) ? NativeApi.GetMediaCatalogNumber(fd) : null;
        return new TableOfContents(device, first, last, tracks, mcn);
      }
    }

    #region Native API

    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Local

    private static class NativeApi {

      #region Constants

      // in milliseconds; timeout better shouldn't happen for scsi commands -> device is reset
      private const uint DefaultSCSIRequestTimeOut = 30000;

      private enum IOCTL : int {
        // Generic SCSI command (uses standard SCSI MMC structures)
        SG_IO              = 0x2285,
      }

      [Flags]
      private enum SCSIFlags : uint {
        SG_FLAG_DIRECT_IO   = 1,       // default is indirect IO
        SG_FLAG_LUN_INHIBIT = 2,       // default is to put device's lun into the 2nd byte of SCSI command
        SG_FLAG_NO_DXFER    = 0x10000, // no transfer of kernel buffers to/from user space (debug indirect IO)
      }

      [Flags]
      private enum SCSIInfoStatus : uint {
        SG_INFO_OK    = 0x00, // no sense, host nor driver "noise"
        SG_INFO_CHECK = 0x01, // something abnormal happened
      }

      [Flags]
      private enum SCSIInfoIOMode : uint {
        SG_INFO_INDIRECT_IO = 0x00, // data xfer via kernel buffers (or no xfer)
        SG_INFO_DIRECT_IO   = 0x02, // direct IO requested and performed
        SG_INFO_MIXED_IO    = 0x04, // part direct, part indirect IO
      }

      private enum SCSITransferDirection : int {
        SG_DXFER_NONE        = -1, // e.g. a SCSI Test Unit Ready command
        SG_DXFER_TO_DEV      = -2, // e.g. a SCSI WRITE command
        SG_DXFER_FROM_DEV    = -3, // e.g. a SCSI READ command
        SG_DXFER_TO_FROM_DEV = -4, // like SG_DXFER_FROM_DEV, but during indirect IO the user buffer is copied into the kernel buffers before the transfer
      }

      #endregion

      #region Structures

      [StructLayout(LayoutKind.Sequential, Pack = 1)]
      private struct SCSIRequest {
        public int                   interface_id;
        public SCSITransferDirection dxfer_direction;
        public byte                  cmd_len;
        public byte                  mx_sb_len;
        public ushort                iovec_count;
        public uint                  dxfer_len;
        public IntPtr                dxferp;
        public IntPtr                cmdp;
        public IntPtr                sbp;
        public uint                  timeout;
        public SCSIFlags             flags;
        public int                   pack_id;
        public IntPtr                usr_ptr;
        public byte                  status;
        public byte                  masked_status;
        public byte                  msg_status;
        public byte                  sb_len_wr;
        public ushort                host_status;
        public ushort                driver_status;
        public int                   resid;
        public uint                  duration;
        public uint                  info;

        public SCSIInfoStatus Status => (SCSIInfoStatus) (this.info & 0x1);
        public SCSIInfoIOMode IOMode => (SCSIInfoIOMode) (this.info & 0x6);
      }

      #endregion

      #region Public Methods

      public static string GetMediaCatalogNumber(UnixFileDescriptor fd) {
        MMC.SubChannelMediaCatalogNumber mcn;
        var cmd = MMC.CDB.ReadSubChannel.MediaCatalogNumber();
        try { NativeApi.SendSCSIRequest(fd, ref cmd, out mcn); }
        catch (Exception e) { throw new IOException("Failed to retrieve media catalog number.", e); }
        mcn.FixUp();
        return mcn.Status.IsValid ? Encoding.ASCII.GetString(mcn.MCN) : string.Empty;
      }

      public static void GetTableOfContents(UnixFileDescriptor fd, out MMC.TOCDescriptor rawtoc) {
        var msf = false;
        var cmd = MMC.CDB.ReadTocPmaAtip.TOC(msf);
        try { NativeApi.SendSCSIRequest(fd, ref cmd, out rawtoc); }
        catch (Exception e) { throw new IOException("Failed to retrieve table of contents.", e); }
        rawtoc.FixUp(msf);
      }

      public static string GetTrackIsrc(UnixFileDescriptor fd, byte track) {
        MMC.SubChannelISRC isrc;
        var cmd = MMC.CDB.ReadSubChannel.ISRC(track);
        try { NativeApi.SendSCSIRequest(fd, ref cmd, out isrc); }
        catch (Exception e) { throw new IOException($"Failed to retrieve ISRC for track {track}.", e); }
        isrc.FixUp();
        return isrc.Status.IsValid ? Encoding.ASCII.GetString(isrc.ISRC) : string.Empty;
      }

      public static UnixFileDescriptor OpenDevice(string name) {
        const uint O_RDONLY   = 0x0000;
        const uint O_NONBLOCK = 0x0800;
        return UnixFileDescriptor.OpenPath(name, O_RDONLY | O_NONBLOCK, 0);
      }

      #endregion

      #region Private Methods

      private static void SendSCSIRequest<TCommand, TData>(UnixFileDescriptor fd, ref TCommand cmd, out TData data) where TCommand : struct where TData : struct {
        var cmdlen = Marshal.SizeOf(typeof(TCommand));
        if (cmdlen > 16)
          throw new InvalidOperationException("A SCSI command must not exceed 16 bytes in size.");
        var req = new SCSIRequest {
          interface_id    = 'S',
          dxfer_direction = SCSITransferDirection.SG_DXFER_FROM_DEV,
          timeout         = NativeApi.DefaultSCSIRequestTimeOut,
          cmd_len         = (byte) cmdlen,
          mx_sb_len       = 16,
          dxfer_len       = (uint) Marshal.SizeOf(typeof(TData)),
        };
        var bytes = Marshal.AllocHGlobal(new IntPtr(req.cmd_len + req.mx_sb_len + req.dxfer_len));
        try {
          req.cmdp   = bytes;
          req.sbp    = req.cmdp + req.cmd_len;
          req.dxferp = req.sbp  + req.mx_sb_len;
          Marshal.StructureToPtr(cmd, req.cmdp, false);
          try {
            if (NativeApi.SendSCSIRequest(fd.Value, IOCTL.SG_IO, ref req) != 0)
              throw new UnixException();
            // TODO: Check SCSI sense data
            data = (TData) Marshal.PtrToStructure(req.dxferp, typeof(TData));
          }
          finally {
            Marshal.DestroyStructure(req.cmdp, typeof(TCommand));
          }
        }
        finally {
          Marshal.FreeHGlobal(bytes);
        }
      }

      [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
      private static extern int SendSCSIRequest(int fd, IOCTL command, ref SCSIRequest request);

    }

    #endregion

  }

  #endregion

}
