using System;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal abstract class Unix : Platform {

    protected Unix(CdDeviceFeature features = CdDeviceFeature.ReadTableOfContents) : base(features) { }

    public new static Unix Create() {
      // TODO: Detect FreeBSD/NetBSD/Solaris and return the appropriate subclass.
      return new Linux();
    }

    public override string DefaultDevice => "/dev/cdrom";

    public override string GetDeviceByIndex(int n) {
      return null;
    }

    public override TableOfContents ReadTableOfContents(string device, CdDeviceFeature features) {
      throw new NotImplementedException($"CD device access has not been implemented for this platform ({Environment.OSVersion}).");
    }

  }

}
