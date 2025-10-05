using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MetaBrainz.MusicBrainz.DiscId.Platforms.NativeApi;
using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms;

internal sealed class Linux() : Unix(Linux.Features) {

  private const DiscReadFeature Features =
    DiscReadFeature.TableOfContents |
    DiscReadFeature.MediaCatalogNumber |
    DiscReadFeature.TrackIsrc |
    DiscReadFeature.CdText;

  private const string GenericDevice = "/dev/cdrom";

  private static readonly char[] InfoDelimiter = ['\t'];

  public override IEnumerable<string> AvailableDevices {
    get {
      string[]? devices = null;
      try {
        using var info = File.OpenText("/proc/sys/dev/cdrom/info");
        while (info.ReadLine() is { } line) {
          if (!line.StartsWith("drive name:", StringComparison.Ordinal)) {
            continue;
          }
          devices = line[11..].Split(Linux.InfoDelimiter, StringSplitOptions.RemoveEmptyEntries);
          break;
        }
      }
      catch {
        // ignore
      }
      if (devices is not null) {
        Array.Reverse(devices);
        foreach (var device in devices) {
          yield return string.Concat("/dev/", device);
        }
      }
    }
  }

  public override string? DefaultDevice {
    get {
      // Prefer the generic device name (typically a symlink to the "preferred" device)
      using var fd = LibC.Linux.OpenDevice(Linux.GenericDevice);
      return fd.IsInvalid ? base.DefaultDevice : Linux.GenericDevice;
    }
  }

  private static RedBook.CDTextGroup? GetCdTextInfo(UnixFileDescriptor fd) {
    LibC.Linux.ReadCdText(fd, out var cdText);
    cdText.FixUp();
    return cdText.Data.Packs is not null ? cdText.Data : null;
  }

  private static string GetMediaCatalogNumber(UnixFileDescriptor fd) {
    LibC.Linux.ReadMediaCatalogNumber(fd, out var mcn);
    mcn.FixUp();
    return mcn.Status.IsValid ? Encoding.ASCII.GetString(mcn.MCN) : "";
  }

  private static void GetTableOfContents(UnixFileDescriptor fd, out MMC.TOCDescriptor toc) {
    LibC.Linux.ReadTOC(fd, out toc);
    toc.FixUp(false);
  }

  private static string GetTrackIsrc(UnixFileDescriptor fd, byte track) {
    LibC.Linux.ReadTrackISRC(fd, track, out var isrc);
    isrc.FixUp();
    return isrc.Status.IsValid ? Encoding.ASCII.GetString(isrc.ISRC) : string.Empty;
  }

  protected override TableOfContents ReadTableOfContents(string device, DiscReadFeature features) {
    using var fd = LibC.Linux.OpenDevice(device);
    if (fd.IsInvalid) {
      throw new IOException($"Failed to open '{device}'.", new UnixException());
    }
    byte first;
    byte last;
    Track[] tracks;
    { // Read the TOC itself
      Linux.GetTableOfContents(fd, out var toc);
      first = toc.FirstTrack;
      last = toc.LastTrack;
      if (features.HasFlag(DiscReadFeature.TrackIsrc)) {
        // ReSharper disable once AccessToDisposedClosure
        tracks = Track.Import(first, last, toc.Tracks, t => Linux.GetTrackIsrc(fd, t));
      }
      else {
        tracks = Track.Import(first, last, toc.Tracks, null);
      }
    }
    var mediaCatalogNumber = features.HasFlag(DiscReadFeature.MediaCatalogNumber) ? Linux.GetMediaCatalogNumber(fd) : null;
    var cdTextInfo = features.HasFlag(DiscReadFeature.CdText) ? Linux.GetCdTextInfo(fd) : null;
    return new TableOfContents(device, first, last, tracks, mediaCatalogNumber, cdTextInfo);
  }

}
