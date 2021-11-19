using JetBrains.Annotations;

namespace MetaBrainz.MusicBrainz.DiscId;

/// <summary>Class holding the CD-TEXT fields applicable to a track on an album.</summary>
[PublicAPI]
public sealed class TrackText {

  /// <summary>The track's title.</summary>
  public string? Title { get; internal set; }

  /// <summary>The track's performer.</summary>
  public string? Performer { get; internal set; }

  /// <summary>The track's lyricist.</summary>
  public string? Lyricist { get; internal set; }

  /// <summary>The track's composer.</summary>
  public string? Composer { get; internal set; }

  /// <summary>The track's arranger.</summary>
  public string? Arranger { get; internal set; }

  /// <summary>A message associated with the track.</summary>
  public string? Message { get; internal set; }

  /// <summary>The track's ISRC.</summary>
  public string? Isrc { get; internal set; }

}
