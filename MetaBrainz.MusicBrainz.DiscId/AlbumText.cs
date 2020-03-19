using System.Diagnostics.CodeAnalysis;

using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId {

  /// <summary>Class holding the CD-TEXT fields applicable to an album.</summary>
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
  public sealed class AlbumText {

    /// <summary>The album's genre.</summary>
    public BlueBook.Genre? Genre { get; internal set; }

    /// <summary>A textual description of the album's genre.</summary>
    public string? GenreDescription { get; internal set; }

    /// <summary>An identification string for the album (e.g. a catalog number); if this contains multiple elements, they will be separated by a forward slash.</summary>
    public string? Identification { get; internal set; }

    /// <summary>The album's title.</summary>
    public string? Title { get; internal set; }

    /// <summary>The album's performer.</summary>
    public string? Performer { get; internal set; }

    /// <summary>The album's lyricist.</summary>
    public string? Lyricist { get; internal set; }

    /// <summary>The album's composer.</summary>
    public string? Composer { get; internal set; }

    /// <summary>The album's arranger.</summary>
    public string? Arranger { get; internal set; }

    /// <summary>A message associated with the album.</summary>
    public string? Message { get; internal set; }

    /// <summary>The album's UPC or EAN.</summary>
    public string? ProductCode { get; internal set; }

  }

}
