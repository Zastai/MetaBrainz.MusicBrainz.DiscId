using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal abstract class Bsd : Unix {

    protected Bsd() : base(DiscReadFeature.TableOfContents | DiscReadFeature.MediaCatalogNumber | DiscReadFeature.TrackIsrc) { }

    protected abstract bool AddressesAreNative { get; }

    protected abstract string GetDevicePath(string device);

    public override IEnumerable<string> AvailableDevices {
      get {
        for (var i = 0; i < 100; ++i) { // 100 is an arbitrary, but sufficiently high, limit
          var path = this.GetDevicePath(string.Concat("cd", i.ToString(CultureInfo.InvariantCulture)));
          if (File.Exists(path))
            yield return path;
          else
            break;
        }
      }
    }

    protected override TableOfContents ReadTableOfContents(string device, DiscReadFeature features) {
      using (var fd = NativeApi.OpenDevice(device)) {
        if (fd.IsInvalid)
          throw new IOException($"Failed to open '{device}'.", new UnixException());
        byte first;
        byte last;
        Track[] tracks;
        { // Read the TOC itself
          MMC.TrackDescriptor[] rawtracks;
          NativeApi.ReadTOC(fd, out first, out last, out rawtracks, this.AddressesAreNative);
          tracks = new Track[rawtracks.Length];
          var i = 0;
          if (first > 0) {
            for (var trackno = first; trackno <= last; ++trackno, ++i) { // Add the regular tracks.
              if (rawtracks[i].TrackNumber != trackno)
                throw new InvalidDataException($"Internal logic error; first track is {first}, but entry at index {i} claims to be track {rawtracks[i].TrackNumber} instead of {trackno}");
              var isrc = ((features & DiscReadFeature.TrackIsrc) != 0) ? NativeApi.GetTrackIsrc(fd, trackno) : null;
              tracks[trackno] = new Track(rawtracks[i].Address, rawtracks[i].ControlAndADR.Control, isrc);
            }
          }
          // Next entry should be the leadout (track number 0xAA)
          if (rawtracks[i].TrackNumber != 0xAA)
            throw new InvalidDataException($"Internal logic error; track data ends with a record that reports track number {rawtracks[i].TrackNumber} instead of 0xAA (lead-out)");
          tracks[0] = new Track(rawtracks[i].Address, rawtracks[i].ControlAndADR.Control, null);
        }
        var mcn = ((features & DiscReadFeature.MediaCatalogNumber) != 0) ? NativeApi.GetMediaCatalogNumber(fd) : null;
        // TODO: Find out how to get CD-TEXT data.
        return new TableOfContents(device, first, last, tracks, mcn, null);
      }
    }

    #region Native API

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
    private static class NativeApi {

      #region Constants

      private enum CDAddressFormat : byte {
        CD_LBA_FORMAT = 1,
        CD_MSF_FORMAT = 2,
      }

      private enum CDDataFormat : byte {
        CD_SUBQ_DATA        = 0,
        CD_CURRENT_POSITION = 1,
        CD_MEDIA_CATALOG    = 2,
        CD_TRACK_INFO       = 3,
      }

      private enum IOCTL : ulong {
        CDIOREADTOCHEADER   = 0x40046304,
        CDIOREADTOCENTRIES  = 0xc0106305,
        CDIOCREADSUBCHANNEL = 0xc0106303,
      }

      #endregion

      #region Structures

      [StructLayout(LayoutKind.Sequential, Pack = 0)]
      private struct ReadSubChannelRequest {
        public  CDAddressFormat address_format;
        public  CDDataFormat    data_format;
        public  byte            track;
        public  int             data_len;
        public  IntPtr          data;
      }

      [StructLayout(LayoutKind.Sequential, Pack = 0)]
      private struct TOCHeaderRequest {
        public ushort len;
        public byte   starting_track;
        public byte   ending_track;
      }

      [StructLayout(LayoutKind.Sequential, Pack = 0)]
      private struct TOCEntriesRequest {
        public  CDAddressFormat address_format;
        public  byte            starting_track;
        public  ushort          data_len;
        public  IntPtr          data;
      }

      #endregion

      #region Public Methods

      public static string GetMediaCatalogNumber(UnixFileDescriptor fd) {
        MMC.SubChannelMediaCatalogNumber mcn;
        if (NativeApi.ReadSubChannel(fd, CDDataFormat.CD_MEDIA_CATALOG, 0, out mcn) != 0)
          throw new IOException("Failed to retrieve media catalog number.", new UnixException());
        mcn.FixUp();
        return mcn.Status.IsValid ? Encoding.ASCII.GetString(mcn.MCN) : string.Empty;
      }

      public static string GetTrackIsrc(UnixFileDescriptor fd, byte track) {
        MMC.SubChannelISRC isrc;
        if (NativeApi.ReadSubChannel(fd, CDDataFormat.CD_TRACK_INFO, track, out isrc) != 0)
          throw new IOException($"Failed to retrieve ISRC for track {track}.", new UnixException());
        isrc.FixUp();
        return isrc.Status.IsValid ? Encoding.ASCII.GetString(isrc.ISRC) : string.Empty;
      }

      public static UnixFileDescriptor OpenDevice(string name) {
        const uint O_RDONLY   = 0x0000;
        const uint O_NONBLOCK = 0x0004;
        return UnixFileDescriptor.OpenPath(name, O_RDONLY | O_NONBLOCK, 0);
      }

      public static void ReadTOC(UnixFileDescriptor fd, out byte first, out byte last, out MMC.TrackDescriptor[] tracks, bool nativeAddress) {
        { // Read the TOC header
          var req = new TOCHeaderRequest();
          if (NativeApi.SendIORequest(fd.Value, IOCTL.CDIOREADTOCHEADER, ref req) != 0)
            throw new IOException("Failed to retrieve table of contents.", new UnixException());
          first = req.starting_track;
          last  = req.ending_track;
        }
        {
          var datatype = typeof(MMC.TrackDescriptor);
          var trackcount = last - first + 2; // first->last plus lead-out
          var itemsize = Marshal.SizeOf(datatype);
          var req = new TOCEntriesRequest {
            address_format = CDAddressFormat.CD_LBA_FORMAT,
            starting_track = first,
            data_len       = (ushort) (trackcount * itemsize),
          };
          req.data = Marshal.AllocHGlobal(new IntPtr(req.data_len));
          try {
            if (NativeApi.SendIORequest(fd.Value, IOCTL.CDIOREADTOCENTRIES, ref req) != 0)
              throw new IOException("Failed to retrieve TOC entries.", new UnixException());
            tracks = new MMC.TrackDescriptor[trackcount];
            var walker = req.data;
            for (var i = 0; i < trackcount; ++i) {
              tracks[i] = (MMC.TrackDescriptor) Marshal.PtrToStructure(walker, datatype);
              // The FixUp call assumes the address is in network byte order.
              if (nativeAddress)
                tracks[i].Address = IPAddress.HostToNetworkOrder(tracks[i].Address);
              tracks[i].FixUp(req.address_format == CDAddressFormat.CD_MSF_FORMAT);
#if NETFX_LT_4_0
              walker = new IntPtr(walker.ToInt64() + itemsize);
#else
              walker += itemsize;
#endif
            }
          }
          finally {
            Marshal.FreeHGlobal(req.data);
          }
        }
      }

      #endregion

      #region Private Methods

      [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
      private static extern int SendIORequest(int fd, IOCTL command, ref TOCHeaderRequest request);

      [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
      private static extern int SendIORequest(int fd, IOCTL command, ref TOCEntriesRequest request);

      [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
      private static extern int SendIORequest(int fd, IOCTL command, ref ReadSubChannelRequest request);

      private static int ReadSubChannel<T>(UnixFileDescriptor fd, CDDataFormat format, byte track, out T data) where T : struct {
        var datatype = typeof(T);
        var req = new ReadSubChannelRequest {
          address_format = CDAddressFormat.CD_LBA_FORMAT,
          data_format    = format,
          track          = track,
          data_len       = Marshal.SizeOf(datatype),
        };
        req.data = Marshal.AllocHGlobal(new IntPtr(req.data_len));
        try {
          var rc = NativeApi.SendIORequest(fd.Value, IOCTL.CDIOCREADSUBCHANNEL, ref req);
          if (rc == 0)
            data = (T) Marshal.PtrToStructure(req.data, datatype);
          else
            data = default(T);
          return rc;
        }
        finally {
          Marshal.FreeHGlobal(req.data);
        }
      }

      #endregion

    }

    #endregion

  }

}
