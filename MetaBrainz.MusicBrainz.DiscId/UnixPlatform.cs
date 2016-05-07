using System;

namespace MetaBrainz.MusicBrainz.DiscId {

  internal class UnixPlatform : Platform {

    protected UnixPlatform(CdDeviceFeature features) : base(features) { }

    public static UnixPlatform Create() {
      // TODO: Detect FreeBSD/NetBSD/Darwin/OSX/Linux/Solaris/... and return the appropriate subclass.
      return new UnixPlatform(CdDeviceFeature.None);
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
