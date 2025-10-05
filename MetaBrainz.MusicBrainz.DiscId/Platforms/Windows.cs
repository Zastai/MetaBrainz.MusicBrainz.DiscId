using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using MetaBrainz.MusicBrainz.DiscId.Platforms.NativeApi;
using MetaBrainz.MusicBrainz.DiscId.Standards;

using Microsoft.Win32.SafeHandles;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms;

// FIXME: Ideally, this would be reworked to use SPTI, to align the Linux & Windows implementation.
//        However, initial attempts have been unsuccessful.

internal sealed class Windows() : Platform(Windows.Features) {

  private const DiscReadFeature Features =
    DiscReadFeature.TableOfContents |
    DiscReadFeature.MediaCatalogNumber |
    DiscReadFeature.TrackIsrc |
    DiscReadFeature.CdText;

  public override IEnumerable<string> AvailableDevices {
    get {
      foreach (var drive in DriveInfo.GetDrives()) {
        if (drive.DriveType == DriveType.CDRom) {
          yield return drive.Name;
        }
      }
    }
  }

  private static RedBook.CDTextGroup? GetCdTextInfo(SafeFileHandle hDevice) {
    Kernel32.ReadCdText(hDevice, out var cdText, out var length);
    var expected = cdText.DataLength + Marshal.SizeOf(cdText.DataLength);
    if (length != expected) {
      TableOfContents.TraceSource.TraceEvent(TraceEventType.Warning, 1300,
                                             "The CD TEXT data reports a total size of {0} bytes, but only {1} bytes were read.",
                                             expected, length);
    }
    if (length < expected) {
      throw new IOException($"CD-TEXT Retrieval: incomplete data read ({length} of {expected} bytes).");
    }
    if (cdText.Data.Packs is not null) {
      TableOfContents.TraceSource.TraceEvent(TraceEventType.Verbose, 1301, "CD-TEXT info read successfully (packs: {0}).",
                                             cdText.Data.Packs.Length);
      return cdText.Data;
    }
    TableOfContents.TraceSource.TraceEvent(TraceEventType.Verbose, 1302, "CD-TEXT info read successfully, but it is empty.");
    return null;
  }

  private static string GetMediaCatalogNumber(SafeFileHandle hDevice) {
    Kernel32.ReadMediaCatalogNumber(hDevice, out var mcn, out var length);
    var expected = mcn.Header.DataLength + Marshal.SizeOf(mcn.Header);
    if (length != expected) {
      TableOfContents.TraceSource.TraceEvent(TraceEventType.Warning, 1200,
                                             "The MCN reports a total data size of {0} bytes, but only {1} bytes were read.",
                                             expected, length);
    }
    if (length < expected) {
      throw new IOException($"MCN Retrieval: incomplete data read ({length} of {expected} bytes).");
    }
    var result = mcn.Status.IsValid ? Encoding.ASCII.GetString(mcn.MCN) : string.Empty;
    TableOfContents.TraceSource.TraceEvent(TraceEventType.Verbose, 1201, "MCN read successfully: [{0}].", result);
    return result;
  }

  private static void GetTableOfContents(SafeFileHandle hDevice, out MMC.TOCDescriptor toc) {
    Kernel32.ReadTOC(hDevice, out toc, out var length);
    var expected = toc.DataLength + Marshal.SizeOf(toc.DataLength);
    if (length != expected) {
      TableOfContents.TraceSource.TraceEvent(TraceEventType.Warning, 1000,
                                             "The TOC reports a total data size of {0} bytes, but only {1} bytes were read.",
                                             expected, length);
    }
    if (length < expected) {
      throw new IOException($"TOC Retrieval: incomplete data read ({length} of {expected} bytes).");
    }
    TableOfContents.TraceSource.TraceEvent(TraceEventType.Verbose, 1001, "TOC read successfully (tracks: {0} -> {1}).",
                                           toc.FirstTrack, toc.LastTrack);
  }

  private static string GetTrackIsrc(SafeFileHandle hDevice, byte track) {
    Kernel32.ReadTrackISRC(hDevice, track, out var isrc, out var length);
    var expected = isrc.Header.DataLength + Marshal.SizeOf(isrc.Header);
    if (length != expected) {
      TableOfContents.TraceSource.TraceEvent(TraceEventType.Warning, 1100,
                                             "The ISRC reports a total data size of {0} bytes, but only {1} bytes were read.",
                                             expected, length);
    }
    if (length < expected) {
      throw new IOException($"ISRC Retrieval: incomplete data read ({length} of {expected} bytes).");
    }
    var result = isrc.Status.IsValid ? Encoding.ASCII.GetString(isrc.ISRC) : string.Empty;
    TableOfContents.TraceSource.TraceEvent(TraceEventType.Verbose, 1101, "ISRC read successfully: [{0}].", result);
    return result;
  }

  protected override TableOfContents ReadTableOfContents(string device, DiscReadFeature features) {
    using var hDevice = Kernel32.OpenDevice(device);
    byte first;
    byte last;
    Track[] tracks;
    { // Read the TOC itself
      Windows.GetTableOfContents(hDevice, out var toc);
      first = toc.FirstTrack;
      last = toc.LastTrack;
      if (features.HasFlag(DiscReadFeature.TrackIsrc)) {
        // ReSharper disable once AccessToDisposedClosure
        tracks = Track.Import(first, last, toc.Tracks, t => Windows.GetTrackIsrc(hDevice, t));
      }
      else {
        TableOfContents.TraceSource.TraceEvent(TraceEventType.Verbose, 1199, "ISRC retrieval not requested or not available.");
        tracks = Track.Import(first, last, toc.Tracks, null);
      }
    }
    string? mcn;
    if (features.HasFlag(DiscReadFeature.MediaCatalogNumber)) {
      mcn = Windows.GetMediaCatalogNumber(hDevice);
    }
    else {
      TableOfContents.TraceSource.TraceEvent(TraceEventType.Verbose, 1299, "MCN retrieval not requested or not available.");
      mcn = null;
    }
    RedBook.CDTextGroup? cdTextInfo;
    if (features.HasFlag(DiscReadFeature.CdText)) {
      cdTextInfo = Windows.GetCdTextInfo(hDevice);
    }
    else {
      TableOfContents.TraceSource.TraceEvent(TraceEventType.Verbose, 1399, "CD-TEXT retrieval not requested or not available.");
      cdTextInfo = null;
    }
    return new TableOfContents(device, first, last, tracks, mcn, cdTextInfo);
  }

}
