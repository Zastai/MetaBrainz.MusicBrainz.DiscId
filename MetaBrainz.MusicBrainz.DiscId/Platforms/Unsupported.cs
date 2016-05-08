using System;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal sealed class Unsupported : Platform {

    public Unsupported() : base(CdDeviceFeature.None) { }

    public override string DefaultDevice => "/dev/null";

    public override string GetDeviceByIndex(int n) {
      return null;
    }

    public override TableOfContents ReadTableOfContents(string device, CdDeviceFeature features) {
      throw new PlatformNotSupportedException($"CD device access is not supported on this platform ({Environment.OSVersion}).");
    }

  }

}
