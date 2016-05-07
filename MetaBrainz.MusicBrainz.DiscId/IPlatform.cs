using System.Collections.Generic;
using System.Globalization;

namespace MetaBrainz.MusicBrainz.DiscId {

  internal interface IPlatform {

    string DefaultDevice { get; }

    string GetDeviceByIndex(int n);

    IEnumerable<string> Features { get; }

    bool HasFeature(CdDeviceFeature feature);

    TableOfContents ReadTableOfContents(string device, CdDeviceFeature features);

  }

}
