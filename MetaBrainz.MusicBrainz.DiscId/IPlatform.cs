using System.Collections.Generic;

namespace MetaBrainz.MusicBrainz.DiscId {

  internal interface IPlatform {

    IEnumerable<string> AvailableDevices { get; }

    string DefaultDevice { get; }

    IEnumerable<string> Features { get; }

    bool HasFeature(CdDeviceFeature feature);

    TableOfContents ReadTableOfContents(string device, CdDeviceFeature features);

  }

}
