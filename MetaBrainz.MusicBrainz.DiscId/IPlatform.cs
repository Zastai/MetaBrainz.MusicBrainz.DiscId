using System.Collections.Generic;

namespace MetaBrainz.MusicBrainz.DiscId {

  internal interface IPlatform {

    IEnumerable<string> AvailableDevices { get; }

    string DefaultDevice { get; }

    IEnumerable<string> Features { get; }

    bool HasFeature(DiscReadFeature feature);

    TableOfContents ReadTableOfContents(string device, DiscReadFeature features);

  }

}
