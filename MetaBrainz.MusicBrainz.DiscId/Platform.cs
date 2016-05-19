using System;
using System.Collections.Generic;
using System.Linq;

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

    public IEnumerable<string> Features {
      get {
        if (this.HasFeature(DiscReadFeature.TableOfContents   )) yield return "read";
        if (this.HasFeature(DiscReadFeature.MediaCatalogNumber)) yield return "mcn";
        if (this.HasFeature(DiscReadFeature.TrackIsrc         )) yield return "isrc";
        if (this.HasFeature(DiscReadFeature.CdText            )) yield return "text";
      }
    }

    public abstract IEnumerable<string> AvailableDevices { get; }

    public virtual string DefaultDevice => this.AvailableDevices.FirstOrDefault();

    public bool HasFeature(DiscReadFeature feature) => (feature & this._features) != 0;

    protected abstract TableOfContents ReadTableOfContents(string device, DiscReadFeature features);

    TableOfContents IPlatform.ReadTableOfContents(string device, DiscReadFeature features) {
      if (device == null) // Map null to the default device
        device = this.DefaultDevice;
      if (device == null) // But we do need a device at this point
        throw new NotSupportedException("No cd-rom device found.");
      // Mask off unsupported features
      features &= this._features;
      return this.ReadTableOfContents(device, features);
    }

  }

}
