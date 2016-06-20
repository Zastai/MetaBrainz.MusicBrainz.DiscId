using System.Diagnostics.CodeAnalysis;

namespace MetaBrainz.MusicBrainz.DiscId {

  /// <summary>Class holding the CD-TEXT fields applicable to a track on an album.</summary>
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
  public sealed class TrackText {

    /// <summary>The track's title.</summary>
    public string Title { get; }

    /// <summary>The track's performer.</summary>
    public string Performer { get; }

    /// <summary>The track's lyricist.</summary>
    public string Lyricist { get; }

    /// <summary>The track's composer.</summary>
    public string Composer { get; }

    /// <summary>The track's arranger.</summary>
    public string Arranger { get; }

    /// <summary>A message associated with the track.</summary>
    public string Message { get; }

    /// <summary>The track's ISRC.</summary>
    public string Isrc { get; }

    internal TrackText(string title, string performer, string lyricist, string composer, string arranger, string message, string code) {
      this.Title     = title;
      this.Performer = performer;
      this.Lyricist  = lyricist;
      this.Composer  = composer;
      this.Arranger  = arranger;
      this.Message   = message;
      this.Isrc      = code;
    }

  }

}
