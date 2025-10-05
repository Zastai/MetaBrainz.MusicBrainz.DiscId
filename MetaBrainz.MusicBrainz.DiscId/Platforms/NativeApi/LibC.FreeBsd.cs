using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

using JetBrains.Annotations;

using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms.NativeApi;

internal static partial class LibC {

  public static class FreeBsd {

    #region Constants

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum CDAddressFormat : byte {

      CD_LBA_FORMAT = 1,

      CD_MSF_FORMAT = 2,

    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private enum CDDataFormat : byte {

      CD_SUBQ_DATA = 0,

      CD_CURRENT_POSITION = 1,

      CD_MEDIA_CATALOG = 2,

      CD_TRACK_INFO = 3,

    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum IOCTL : ulong {

      CD_IO_READ_TOC_HEADER = 0x40046304,

      CD_IO_READ_TOC_ENTRIES = 0xC0106305,

      CD_IO_C_READ_SUB_CHANNEL = 0xC0106303,

    }

    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum OpenMode : uint {

      O_RDONLY = 0x0000,

      O_NONBLOCK = 0x0004,

    }

    #endregion

    #region Structures

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    private struct ReadSubChannelRequest {

      public CDAddressFormat address_format;

      public CDDataFormat data_format;

      public byte track;

      public int data_len;

      public IntPtr data;

    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private struct TOCHeaderRequest {

      public ushort len;

      public byte starting_track;

      public byte ending_track;

    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    private struct TOCEntriesRequest {

      public CDAddressFormat address_format;

      public byte starting_track;

      public ushort data_len;

      public IntPtr data;

    }

    #endregion

    #region P/Invoke Methods

    // TBD: Does LibraryImport work for this?
    #pragma warning disable SYSLIB1054

    [DllImport(NativeApi.LibC.LibraryName, EntryPoint = "ioctl", SetLastError = true)]
    private static extern int SendIORequest(int fd, IOCTL command, ref TOCHeaderRequest request);

    [DllImport(NativeApi.LibC.LibraryName, EntryPoint = "ioctl", SetLastError = true)]
    private static extern int SendIORequest(int fd, IOCTL command, ref TOCEntriesRequest request);

    [DllImport(NativeApi.LibC.LibraryName, EntryPoint = "ioctl", SetLastError = true)]
    private static extern int SendIORequest(int fd, IOCTL command, ref ReadSubChannelRequest request);

    #pragma warning restore SYSLIB1054

    #endregion

    public static UnixFileDescriptor OpenDevice(string name)
      => UnixFileDescriptor.OpenPath(name, (uint) (OpenMode.O_RDONLY | OpenMode.O_NONBLOCK), 0);

    public static void ReadMediaCatalogNumber(UnixFileDescriptor fd, out MMC.SubChannelMediaCatalogNumber mcn) {
      if (FreeBsd.ReadSubChannel(fd, CDDataFormat.CD_MEDIA_CATALOG, 0, out mcn) != 0) {
        throw new IOException("Failed to retrieve media catalog number.", new UnixException());
      }
      mcn.FixUp();
    }

    private static int ReadSubChannel<T>(UnixFileDescriptor fd, CDDataFormat format, byte track, out T data) where T : struct {
      var req = new ReadSubChannelRequest {
        address_format = CDAddressFormat.CD_LBA_FORMAT,
        data_format = format,
        data_len = Marshal.SizeOf<T>(),
        track = track,
      };
      req.data = Marshal.AllocHGlobal(new IntPtr(req.data_len));
      try {
        var rc = LibC.FreeBsd.SendIORequest(fd.Value, IOCTL.CD_IO_C_READ_SUB_CHANNEL, ref req);
        data = (rc == 0) ? Marshal.PtrToStructure<T>(req.data) : default;
        return rc;
      }
      finally {
        Marshal.FreeHGlobal(req.data);
      }
    }

    public static void ReadTOC(UnixFileDescriptor fd, out byte first, out byte last, out MMC.TrackDescriptor[] tracks) {
      { // Read the TOC header
        var req = new TOCHeaderRequest();
        if (LibC.FreeBsd.SendIORequest(fd.Value, IOCTL.CD_IO_READ_TOC_HEADER, ref req) != 0) {
          throw new IOException("Failed to retrieve table of contents.", new UnixException());
        }
        first = req.starting_track;
        last = req.ending_track;
      }
      {
        var trackCount = last - first + 2; // first->last plus lead-out
        var itemSize = Marshal.SizeOf<MMC.TrackDescriptor>();
        var req = new TOCEntriesRequest {
          address_format = CDAddressFormat.CD_LBA_FORMAT,
          data_len = (ushort) (trackCount * itemSize),
          starting_track = first,
        };
        req.data = Marshal.AllocHGlobal(new IntPtr(req.data_len));
        try {
          if (LibC.FreeBsd.SendIORequest(fd.Value, IOCTL.CD_IO_READ_TOC_ENTRIES, ref req) != 0) {
            throw new IOException("Failed to retrieve TOC entries.", new UnixException());
          }
          tracks = new MMC.TrackDescriptor[trackCount];
          var walker = req.data;
          for (var i = 0; i < trackCount; ++i) {
            tracks[i] = Marshal.PtrToStructure<MMC.TrackDescriptor>(walker);
            tracks[i].FixUp(req.address_format == CDAddressFormat.CD_MSF_FORMAT);
            walker += itemSize;
          }
        }
        finally {
          Marshal.FreeHGlobal(req.data);
        }
      }
    }

    public static void ReadTrackISRC(UnixFileDescriptor fd, byte track, out MMC.SubChannelISRC isrc) {
      if (FreeBsd.ReadSubChannel(fd, CDDataFormat.CD_TRACK_INFO, track, out isrc) != 0) {
        throw new IOException($"Failed to retrieve ISRC for track {track}.", new UnixException());
      }
      isrc.FixUp();
    }

  }

}
