using System.Collections.Generic;

namespace MetaBrainz.MusicBrainz.DiscId {

  internal abstract class Platform : IPlatform {

    private CdDeviceFeature _features;

    protected Platform(CdDeviceFeature features) {
      this._features = features;
    }

    public IEnumerable<string> Features {
      get {
        if (this.HasFeature(CdDeviceFeature.ReadTableOfContents   )) yield return "read";
        if (this.HasFeature(CdDeviceFeature.ReadMediaCatalogNumber)) yield return "mcn";
        if (this.HasFeature(CdDeviceFeature.ReadTrackIsrc         )) yield return "isrc";
        if (this.HasFeature(CdDeviceFeature.ReadCdText            )) yield return "text";
      }
    }

    public abstract string DefaultDevice { get; }

    public abstract string GetDeviceByIndex(int n);

    public bool HasFeature(CdDeviceFeature feature) => (feature & this._features) != 0;

    public abstract TableOfContents ReadTableOfContents(string device, CdDeviceFeature features);

    TableOfContents IPlatform.ReadTableOfContents(string device, CdDeviceFeature features) {
      features &= this._features; // Mask off unsupported features
      return this.ReadTableOfContents(device, features);
    }

  }

}
