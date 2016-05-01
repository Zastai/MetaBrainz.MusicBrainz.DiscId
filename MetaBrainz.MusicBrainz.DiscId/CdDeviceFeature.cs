using System;

namespace MetaBrainz.MusicBrainz {

  /// <summary>Enumeration of features the library may support on any given platform.</summary>
  [Flags]
  public enum CdDeviceFeature : uint {

    /// <summary>The core functionality of reading the TOC, enabling DiscId (and freedb id) computation.</summary>
    ReadTableOfContents    = 1 << 0,

    /// <summary>The ability to read the media catalog number (typically the UPC/EAN number) for a disc.</summary>
    ReadMediaCatalogNumber = 1 << 1,

    /// <summary>The ability to read a track's international standard recording code (ISRC).</summary>
    /// <remarks>This does not guarantee that the returned values are <em>accurate</em>; many drives (especially slimline ones) do not read ISRC values correctly.</remarks>
    ReadTrackIsrc          = 1 << 2,

    /// <summary>The ability to read CD-TEXT information.</summary>
    /// <remarks>Even when supported, relatively few discs will include this information (and even when they do, often not for all fields).</remarks>
    ReadCdText             = 1 << 3,

    /// <summary>All available features.</summary>
    All = 0xffffffff,

  }

}
