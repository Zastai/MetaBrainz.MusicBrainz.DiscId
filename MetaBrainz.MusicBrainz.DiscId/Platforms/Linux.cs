using System;
using System.IO;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal sealed class Linux : Unix {

    public Linux() : base(CdDeviceFeature.ReadTableOfContents | CdDeviceFeature.ReadMediaCatalogNumber | CdDeviceFeature.ReadTrackIsrc) { }

    public override string DefaultDevice => "/dev/cdrom";

    public override string GetDeviceByIndex(int n) {
      if (n < 0)
        return null;
      using (var info = System.IO.File.OpenText("/proc/sys/dev/cdrom/info")) {
        string line;
        while ((line = info.ReadLine()) != null) {
          if (line.StartsWith("drive name:")) {
            var devices = line.Substring(11).Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (n >= devices.Length)
              return null;
            Array.Reverse(devices);
            return string.Concat("/dev/", devices[n]);
          }
        }
      }
      return null;
    }

    // TODO: Port the rest of disc_linux.c

  }

}
