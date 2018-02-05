using System;
using System.Collections.Generic;

namespace MetaBrainz.MusicBrainz.DiscId {

  internal abstract class Platform : IPlatform {

    public static IPlatform Create() {
      switch (Environment.OSVersion.Platform) {
        case PlatformID.MacOSX:
          return new Platforms.Darwin();
        case PlatformID.Win32NT:
        case PlatformID.Win32S:
        case PlatformID.Win32Windows:
        case PlatformID.WinCE:
        case PlatformID.Xbox:
          return new Platforms.Windows();
        case PlatformID.Unix:
          return Platforms.Unix.Create();
        default:
          return new Platforms.Unsupported();
      }
    }

    private readonly DiscReadFeature _features;

    protected Platform(DiscReadFeature features) {
      this._features = features;
    }

    public abstract IEnumerable<string> AvailableDevices { get; }

    public DiscReadFeature AvailableFeatures => this._features;

    public virtual string DefaultDevice {
      get { // Equivalent to FirstOrDefault(), but without using LINQ.
        foreach (var device in this.AvailableDevices)
          return device;
        return null;
      }
    }

    public bool HasFeature(DiscReadFeature feature) => (feature & this._features) != 0;

    protected abstract TableOfContents ReadTableOfContents(string device, DiscReadFeature features);

    TableOfContents IPlatform.ReadTableOfContents(string device, DiscReadFeature features) {
      if (string.IsNullOrWhiteSpace(device)) // Map null/blanks to the default device
        device = this.DefaultDevice;
      if (device == null) // But we do need a device at this point
        throw new NotSupportedException("No cd-rom device found.");
      // Mask off unsupported features
      features &= this._features;
      return this.ReadTableOfContents(device, features);
    }

  }

}
