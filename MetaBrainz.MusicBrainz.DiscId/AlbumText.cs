using System.Diagnostics.CodeAnalysis;

using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId {

  /// <summary>Class holding the CD-TEXT fields applicable to an album.</summary>
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
  public sealed class AlbumText {

    /// <summary>The album's genre.</summary>
    public BlueBook.Genre? Genre { get; }

    /// <summary>A textual description of the album's genre.</summary>
    public string GenreDescription { get; }

    /// <summary>An identification string for the album (e.g. a catalog number); if this contains multiple elements, they will be separated by a forward slash.</summary>
    public string Identification { get; }

    /// <summary>The album's title.</summary>
    public string Title { get; }

    /// <summary>The album's performer.</summary>
    public string Performer { get; }

    /// <summary>The album's lyricist.</summary>
    public string Lyricist { get; }

    /// <summary>The album's composer.</summary>
    public string Composer { get; }

    /// <summary>The album's arranger.</summary>
    public string Arranger { get; }

    /// <summary>A message associated with the album.</summary>
    public string Message { get; }

    /// <summary>The album's UPC or EAN.</summary>
    public string ProductCode { get; }

    internal AlbumText(BlueBook.Genre? genre, string genreDescription, string ident, string title, string performer, string lyricist, string composer, string arranger, string message, string code) {
      this.Genre            = genre;
      this.GenreDescription = genreDescription;
      this.Identification   = ident;
      this.Title            = title;
      this.Performer        = performer;
      this.Lyricist         = lyricist;
      this.Composer         = composer;
      this.Arranger         = arranger;
      this.Message          = message;
      this.ProductCode      = code;
    }

  }

}
