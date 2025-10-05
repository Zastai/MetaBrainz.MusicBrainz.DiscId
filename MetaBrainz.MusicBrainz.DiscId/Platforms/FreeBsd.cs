using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using MetaBrainz.MusicBrainz.DiscId.Standards;
using MetaBrainz.MusicBrainz.DiscId.Platforms.NativeApi;

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

  private static string GetMediaCatalogNumber(UnixFileDescriptor fd) {
    LibC.FreeBsd.ReadMediaCatalogNumber(fd, out var mcn);
    return mcn.Status.IsValid ? Encoding.ASCII.GetString(mcn.MCN) : string.Empty;
  }

  private static string GetTrackIsrc(UnixFileDescriptor fd, byte track) {
    LibC.FreeBsd.ReadTrackISRC(fd, track, out var isrc);
    return isrc.Status.IsValid ? Encoding.ASCII.GetString(isrc.ISRC) : string.Empty;
  }

  /// <summary>The maximum number of devices considered by <see cref="AvailableDevices"/>.</summary>
  private const int MaxDevices = 100;

  protected override TableOfContents ReadTableOfContents(string device, DiscReadFeature features) {
    using var fd = LibC.FreeBsd.OpenDevice(device);
    if (fd.IsInvalid) {
      throw new IOException($"Failed to open '{device}'.", new UnixException());
    }
    byte first;
    byte last;
    Track[] tracks;
    { // Read the TOC itself
      LibC.FreeBsd.ReadTOC(fd, out first, out last, out var rawTracks);
      if (features.HasFlag(DiscReadFeature.TrackIsrc)) {
        // ReSharper disable once AccessToDisposedClosure
        tracks = Track.Import(first, last, rawTracks, t => FreeBsd.GetTrackIsrc(fd, t));
      }
      else {
        tracks = Track.Import(first, last, rawTracks, null);
      }
    }
    var mediaCatalogNumber = features.HasFlag(DiscReadFeature.MediaCatalogNumber) ? FreeBsd.GetMediaCatalogNumber(fd) : null;
    // TODO: Find out how to get CD-TEXT data.
    return new TableOfContents(device, first, last, tracks, mediaCatalogNumber, null);
  }

}
