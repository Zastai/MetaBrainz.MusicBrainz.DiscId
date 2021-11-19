using System;
using System.Collections.Generic;

using MetaBrainz.MusicBrainz.DiscId.Platforms;

namespace MetaBrainz.MusicBrainz.DiscId;

internal abstract class Platform : IPlatform {

  public static IPlatform Create() {
    return Environment.OSVersion.Platform switch {
      PlatformID.MacOSX => new Darwin(),
      PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.WinCE or PlatformID.Xbox => new Windows(),
      PlatformID.Unix => Unix.Create(),
      _ => new Unsupported(),
    };
  }

  protected Platform(DiscReadFeature features) {
    this.AvailableFeatures = features;
  }

  public abstract IEnumerable<string> AvailableDevices { get; }

  public DiscReadFeature AvailableFeatures { get; }

  public virtual string? DefaultDevice {
    get { // Equivalent to FirstOrDefault(), but without using LINQ.
      foreach (var device in this.AvailableDevices) {
        return device;
      }
      return null;
    }
  }

  public bool HasFeature(DiscReadFeature feature) => (feature & this.AvailableFeatures) == feature;

  protected abstract TableOfContents ReadTableOfContents(string device, DiscReadFeature features);

  TableOfContents IPlatform.ReadTableOfContents(string? device, DiscReadFeature features) {
    if (string.IsNullOrWhiteSpace(device)) { // Map null/blanks to the default device
      device = this.DefaultDevice;
    }
    if (device == null) { // But we do need a device at this point
      throw new NotSupportedException("No cd-rom device found.");
    }
    // Mask off unsupported features
    features &= this.AvailableFeatures;
    return this.ReadTableOfContents(device, features);
  }

}
