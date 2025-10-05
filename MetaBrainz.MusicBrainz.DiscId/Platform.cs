using System;
using System.Collections.Generic;
using System.Linq;

using MetaBrainz.MusicBrainz.DiscId.Platforms;

namespace MetaBrainz.MusicBrainz.DiscId;

internal abstract class Platform(DiscReadFeature features) : IPlatform {

  public static IPlatform Create() => Environment.OSVersion.Platform switch {
    PlatformID.MacOSX => new MacOS(),
    PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.WinCE or PlatformID.Xbox => new Windows(),
    PlatformID.Unix => Unix.Create(),
    _ => new Unsupported(),
  };

  public abstract IEnumerable<string> AvailableDevices { get; }

  public DiscReadFeature AvailableFeatures { get; } = features;

  public virtual string? DefaultDevice => this.AvailableDevices.FirstOrDefault();

  public bool HasFeature(DiscReadFeature feature) => (feature & this.AvailableFeatures) == feature;

  protected abstract TableOfContents ReadTableOfContents(string device, DiscReadFeature features);

  TableOfContents IPlatform.ReadTableOfContents(string? device, DiscReadFeature features) {
    if (string.IsNullOrWhiteSpace(device)) {
      // Map null/blanks to the default device
      device = this.DefaultDevice;
    }
    if (device is null) {
      // But we do need a device at this point
      throw new NotSupportedException("No cd-rom device found.");
    }
    // Mask off unsupported features
    features &= this.AvailableFeatures;
    return this.ReadTableOfContents(device, features);
  }

}
