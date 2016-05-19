using System;
using System.Collections.Generic;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal class Unsupported : Platform {

    public Unsupported() : base(CdDeviceFeature.None) { }

    public override IEnumerable<string> AvailableDevices { get { yield break; } }

    protected override TableOfContents ReadTableOfContents(string device, CdDeviceFeature features) {
      throw new PlatformNotSupportedException($"CD device access is not supported on this platform ({Environment.OSVersion}).");
    }

  }

}
