﻿using System;

namespace MetaBrainz.MusicBrainz.DiscId {

  /// <summary>Enumeration of features the library may support on any given platform.</summary>
  [Flags]
  public enum DiscReadFeature {

    /// <summary>No functionality.</summary>
    None               = 0,

    /// <summary>The core functionality of reading the TOC, enabling DiscId (and freedb id) computation.</summary>
    TableOfContents    = 1,

    /// <summary>The ability to read the media catalog number (typically the UPC/EAN number) for a disc.</summary>
    MediaCatalogNumber = 2,

    /// <summary>The ability to read a track's international standard recording code (ISRC).</summary>
    /// <remarks>This does not guarantee that the returned values are <em>accurate</em>; many drives (especially slimline ones) do not read ISRC values correctly.</remarks>
    TrackIsrc          = 4,

    /// <summary>The ability to read CD-TEXT information.</summary>
    /// <remarks>Even when supported, relatively few discs will include this information (and even when they do, often not for all fields).</remarks>
    CdText             = 8,

    /// <summary>All available features.</summary>
    All                = -1

  }

}
