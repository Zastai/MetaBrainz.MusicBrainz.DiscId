using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using JetBrains.Annotations;

using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms;

internal sealed class FreeBsd() : Unix(FreeBsd.Features) {

  private const DiscReadFeature Features =
    DiscReadFeature.TableOfContents |
    DiscReadFeature.MediaCatalogNumber |
    DiscReadFeature.TrackIsrc;

  public override IEnumerable<string> AvailableDevices {
    get {
      for (var i = 0; i < FreeBsd.MaxDevices; ++i) {
        var path = string.Concat("/dev/cd", i.ToString(CultureInfo.InvariantCulture));
        if (File.Exists(path)) {
          yield return path;
        }
        else {
          break;
        }
      }
    }
  }

  /// <summary>The maximum number of devices considered by <see cref="AvailableDevices"/>.</summary>
  private const int MaxDevices = 100;

  protected override TableOfContents ReadTableOfContents(string device, DiscReadFeature features) {
    using var fd = NativeApi.OpenDevice(device);
    if (fd.IsInvalid) {
      throw new IOException($"Failed to open '{device}'.", new UnixException());
    }
    // Read the TOC itself
    NativeApi.ReadTOC(fd, out var first, out var last, out var rawTracks);
    var tracks = new Track[rawTracks.Length];
    var i = 0;
    if (first > 0) {
      for (var trackNo = first; trackNo <= last; ++trackNo, ++i) { // Add the regular tracks.
        if (rawTracks[i].TrackNumber != trackNo) {
          throw new InvalidDataException($"Internal logic error; the first track is #{first}, but the entry at index {i} claims " +
                                         $"to be track #{rawTracks[i].TrackNumber} instead of #{trackNo}.");
        }
        var isrc = ((features & DiscReadFeature.TrackIsrc) != 0) ? NativeApi.GetTrackIsrc(fd, trackNo) : null;
        tracks[trackNo] = new Track(rawTracks[i].Address, rawTracks[i].ControlAndADR.Control, isrc);
      }
    }
    // Next entry should be the lead-out (track number 0xAA)
    if (rawTracks[i].TrackNumber != 0xAA) {
      throw new InvalidDataException("Internal logic error; the track data ends with a record that reports track number " +
                                     $"{rawTracks[i].TrackNumber} instead of 0xAA (lead-out).");
    }
    tracks[0] = new Track(rawTracks[i].Address, rawTracks[i].ControlAndADR.Control, null);
    var mcn = ((features & DiscReadFeature.MediaCatalogNumber) != 0) ? NativeApi.GetMediaCatalogNumber(fd) : null;
    // TODO: Find out how to get CD-TEXT data.
    return new TableOfContents(device, first, last, tracks, mcn, null);
  }

  #region Native API

  private static class NativeApi {

    #region Constants

    private enum CDAddressFormat : byte {

      CD_LBA_FORMAT = 1,

      CD_MSF_FORMAT = 2,

    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private enum CDDataFormat : byte {

      CD_SUBQ_DATA = 0,

      CD_CURRENT_POSITION = 1,

      CD_MEDIA_CATALOG = 2,

      CD_TRACK_INFO = 3,

    }

    private enum IOCTL : ulong {

      CD_IO_READ_TOC_HEADER = 0x40046304,

      CD_IO_READ_TOC_ENTRIES = 0xc0106305,

      CD_IO_C_READ_SUB_CHANNEL = 0xc0106303,

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

    #region Public Methods

    public static string GetMediaCatalogNumber(UnixFileDescriptor fd) {
      if (NativeApi.ReadSubChannel(fd, CDDataFormat.CD_MEDIA_CATALOG, 0, out MMC.SubChannelMediaCatalogNumber mcn) != 0) {
        throw new IOException("Failed to retrieve media catalog number.", new UnixException());
      }
      mcn.FixUp();
      return mcn.Status.IsValid ? Encoding.ASCII.GetString(mcn.MCN) : string.Empty;
    }

    public static string GetTrackIsrc(UnixFileDescriptor fd, byte track) {
      if (NativeApi.ReadSubChannel(fd, CDDataFormat.CD_TRACK_INFO, track, out MMC.SubChannelISRC isrc) != 0) {
        throw new IOException($"Failed to retrieve ISRC for track {track}.", new UnixException());
      }
      isrc.FixUp();
      return isrc.Status.IsValid ? Encoding.ASCII.GetString(isrc.ISRC) : string.Empty;
    }

    public static UnixFileDescriptor OpenDevice(string name) {
      const uint O_RDONLY = 0x0000;
      const uint O_NONBLOCK = 0x0004;
      return UnixFileDescriptor.OpenPath(name, O_RDONLY | O_NONBLOCK, 0);
    }

    public static void ReadTOC(UnixFileDescriptor fd, out byte first, out byte last, out MMC.TrackDescriptor[] tracks) {
      { // Read the TOC header
        var req = new TOCHeaderRequest();
        if (NativeApi.SendIORequest(fd.Value, IOCTL.CD_IO_READ_TOC_HEADER, ref req) != 0) {
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
          if (NativeApi.SendIORequest(fd.Value, IOCTL.CD_IO_READ_TOC_ENTRIES, ref req) != 0) {
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

    #endregion

    #region Private Methods

    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    private static extern int SendIORequest(int fd, IOCTL command, ref TOCHeaderRequest request);

    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    private static extern int SendIORequest(int fd, IOCTL command, ref TOCEntriesRequest request);

    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    private static extern int SendIORequest(int fd, IOCTL command, ref ReadSubChannelRequest request);

    private static int ReadSubChannel<T>(UnixFileDescriptor fd, CDDataFormat format, byte track, out T data)
      where T : struct {
      var req = new ReadSubChannelRequest {
        address_format = CDAddressFormat.CD_LBA_FORMAT,
        data_format = format,
        data_len = Marshal.SizeOf<T>(),
        track = track,
      };
      req.data = Marshal.AllocHGlobal(new IntPtr(req.data_len));
      try {
        var rc = NativeApi.SendIORequest(fd.Value, IOCTL.CD_IO_C_READ_SUB_CHANNEL, ref req);
        data = (rc == 0) ? Marshal.PtrToStructure<T>(req.data) : default;
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
