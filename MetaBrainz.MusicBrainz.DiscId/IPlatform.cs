using System.Collections.Generic;

namespace MetaBrainz.MusicBrainz.DiscId {

  internal interface IPlatform {

    IEnumerable<string> AvailableDevices { get; }

    string DefaultDevice { get; }

    bool HasFeature(DiscReadFeature feature);

    TableOfContents ReadTableOfContents(string device, DiscReadFeature features);

  }

}
