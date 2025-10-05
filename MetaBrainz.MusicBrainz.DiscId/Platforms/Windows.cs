using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using JetBrains.Annotations;

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
      Debug.Print($"I/O: CD-TEXT descriptor has data length as {expected} but only {length} bytes were read.");
    }
    if (length < expected) {
      throw new IOException($"CD-TEXT Retrieval: the structure says its size is {expected} but only {length} bytes were read.");
    }
    return cdText.Data.Packs is not null ? cdText.Data : null;
  }

  private static string GetMediaCatalogNumber(SafeFileHandle hDevice) {
    Kernel32.ReadMediaCatalogNumber(hDevice, out var mcn, out var length);
    mcn.FixUp();
    var expected = mcn.Header.DataLength + Marshal.SizeOf(mcn.Header);
    if (length != expected) {
      Debug.Print($"I/O: MCN has data length as {expected} but {length} bytes were read.");
    }
    if (length < expected) {
      throw new IOException($"MCN Retrieval: the structure says its size is {expected} but only {length} bytes were read.");
    }
    return mcn.Status.IsValid ? Encoding.ASCII.GetString(mcn.MCN) : string.Empty;
  }

  private static void GetTableOfContents(SafeFileHandle hDevice, out MMC.TOCDescriptor toc) {
    Kernel32.ReadTOC(hDevice, out toc, out var length);
    var expected = toc.DataLength + Marshal.SizeOf(toc.DataLength);
    if (length != expected) {
      Debug.Print($"I/O: TOC descriptor has data length as {expected} but only {length} bytes were read.");
    }
    if (length < expected) {
      throw new IOException($"TOC Retrieval: the structure says its size is {expected} but only {length} bytes were read.");
    }
  }

  private static string GetTrackIsrc(SafeFileHandle hDevice, byte track) {
    Kernel32.ReadTrackISRC(hDevice, track, out var isrc, out var len);
    var expected = isrc.Header.DataLength + Marshal.SizeOf(isrc.Header);
    if (len != expected) {
      Debug.Print($"I/O: ISRC has data length as {expected} but {len} bytes were read.");
    }
    if (len < expected) {
      throw new IOException($"ISRC Retrieval: the structure says its size is {expected} but only {len} bytes were read.");
    }
    return isrc.Status.IsValid ? Encoding.ASCII.GetString(isrc.ISRC) : string.Empty;
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
        tracks = Track.Import(first, last, toc.Tracks, null);
      }
    }
    var mcn = features.HasFlag(DiscReadFeature.MediaCatalogNumber) ? Windows.GetMediaCatalogNumber(hDevice) : null;
    var cdTextInfo = features.HasFlag(DiscReadFeature.CdText) ? Windows.GetCdTextInfo(hDevice) : null;
    return new TableOfContents(device, first, last, tracks, mcn, cdTextInfo);
  }

}
