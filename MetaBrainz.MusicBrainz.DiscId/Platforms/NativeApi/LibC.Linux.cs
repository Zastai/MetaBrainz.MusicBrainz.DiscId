using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

using JetBrains.Annotations;

using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms.NativeApi;

internal static partial class LibC {

  public static class Linux {

    #region Constants

    // in milliseconds; timeout better shouldn't happen for scsi commands -> device is reset
    private const uint DefaultSCSIRequestTimeOut = 30000;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum IOCTL : ulong {

      // Generic SCSI command (uses standard SCSI MMC structures)
      SG_IO = 0x2285,

    }

    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum OpenMode : uint {

      O_RDONLY = 0x0000,

      O_NONBLOCK = 0x0800,

    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
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
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private enum SCSIFlags : uint {

      DIRECT_IO = 1, // default is indirect IO

      LUN_INHIBIT = 2, // default is to put device's lun into the 2nd byte of SCSI command

      NO_DXFER = 0x10000, // no transfer of kernel buffers to/from user space (debug indirect IO)

    }

    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private enum SCSIInfoIOMode : uint {

      INDIRECT_IO = 0x00, // data xfer via kernel buffers (or no xfer)

      DIRECT_IO = 0x02, // direct IO requested and performed

      MIXED_IO = 0x04, // part direct, part indirect IO

    }

    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private enum SCSIInfoStatus : uint {

      OK = 0x00, // no sense, host nor driver "noise"

      CHECK = 0x01, // something abnormal happened

    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
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

    #region P/Invoke Methods

    // TBD: Does LibraryImport work for this?
    #pragma warning disable SYSLIB1054

    [DllImport(LibC.LibraryName, EntryPoint = "ioctl", SetLastError = true)]
    private static extern int SendSCSIRequest(int fd, IOCTL command, ref SCSIRequest request);

    #pragma warning restore SYSLIB1054

    #endregion

    public static UnixFileDescriptor OpenDevice(string name)
      => UnixFileDescriptor.OpenPath(name, (uint) (OpenMode.O_RDONLY | OpenMode.O_NONBLOCK), 0);

    public static void ReadCdText(UnixFileDescriptor fd, out MMC.CDTextDescriptor cdText) {
      try {
        var cmd = MMC.CDB.ReadTocPmaAtip.CDText();
        Linux.SendSCSIRequest(fd, ref cmd, out cdText);
      }
      catch (Exception e) {
        throw new IOException("Failed to retrieve CD-TEXT information.", e);
      }
      cdText.FixUp();
    }

    public static void ReadMediaCatalogNumber(UnixFileDescriptor fd, out MMC.SubChannelMediaCatalogNumber mcn) {
      try {
        var cmd = MMC.CDB.ReadSubChannel.MediaCatalogNumber();
        Linux.SendSCSIRequest(fd, ref cmd, out mcn);
      }
      catch (Exception e) {
        throw new IOException("Failed to retrieve media catalog number.", e);
      }
      mcn.FixUp();
    }

    public static void ReadTOC(UnixFileDescriptor fd, out MMC.TOCDescriptor toc) {
      try {
        var cmd = MMC.CDB.ReadTocPmaAtip.TOC();
        Linux.SendSCSIRequest(fd, ref cmd, out toc);
      }
      catch (Exception e) {
        throw new IOException("Failed to retrieve table of contents.", e);
      }
      toc.FixUp(false);
    }

    public static void ReadTrackISRC(UnixFileDescriptor fd, byte track, out MMC.SubChannelISRC isrc) {
      try {
        var cmd = MMC.CDB.ReadSubChannel.ISRC(track);
        Linux.SendSCSIRequest(fd, ref cmd, out isrc);
      }
      catch (Exception e) {
        throw new IOException($"Failed to retrieve ISRC for track {track}.", e);
      }
      isrc.FixUp();
    }

    private static void SendSCSIRequest<TCommand, TData>(UnixFileDescriptor fd, ref TCommand cmd,
                                                         out TData data) where TCommand : struct where TData : struct {
      var commandLength = Marshal.SizeOf<TCommand>();
      if (commandLength > 16) {
        throw new InvalidOperationException("A SCSI command must not exceed 16 bytes in size.");
      }
      var req = new SCSIRequest {
        interface_id = 'S',
        dxfer_direction = SCSITransferDirection.FROM_DEV,
        timeout = LibC.Linux.DefaultSCSIRequestTimeOut,
        cmd_len = (byte) commandLength,
        mx_sb_len = 64,
        dxfer_len = (uint) Marshal.SizeOf<TData>(),
      };
      var memorySize = (uint) (req.cmd_len + req.mx_sb_len + req.dxfer_len);
      var bytes = LibC.AllocZero(new UIntPtr(1), new UIntPtr(memorySize));
      try {
        req.cmdp = bytes;
        req.sbp = req.cmdp + req.cmd_len;
        req.dxferp = req.sbp + req.mx_sb_len;
        Marshal.StructureToPtr(cmd, req.cmdp, false);
        try {
          if (LibC.Linux.SendSCSIRequest(fd.Value, IOCTL.SG_IO, ref req) != 0) {
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
        LibC.Free(bytes);
      }
    }

  }

}
