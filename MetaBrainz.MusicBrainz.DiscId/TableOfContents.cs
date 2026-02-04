using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using JetBrains.Annotations;

using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId;

/// <summary>Class representing a CD's table of contents.</summary>
[PublicAPI]
public sealed class TableOfContents {

  #region Constants

  // FIXME: libdiscid (rather arbitrarily) uses 90 minutes. But overburning can go above 100 minutes. For now, use 99:59.74.
  /// <summary>The largest possible AudioCD (1 sector shy of 100 minutes).</summary>
  /// <remarks>This is only used for validation of user-supplied offsets (<see cref="SimulateDisc"/>).</remarks>
  public const int MaxSectors = (100 * 60 * 75) - 1;

  /// <summary>The distance between the last audio track and the first data track.</summary>
  public const int XAInterval = (60 + 90 + 2) * 75;

  #endregion

  #region Static Properties

  /// <summary>Enumerates the names of all cd-rom devices in the system.</summary>
  /// <returns>The names of all cd-rom devices in the system.</returns>
  public static IEnumerable<string> AvailableDevices => TableOfContents.Platform.AvailableDevices;

  /// <summary>The list of features supported on this platform for use with <see cref="ReadDisc"/>.</summary>
  public static DiscReadFeature AvailableFeatures => TableOfContents.Platform.AvailableFeatures;

  /// <summary>The default cd-rom device (used when passing <see langword="null"/> to <see cref="ReadDisc"/>).</summary>
  public static string? DefaultDevice => TableOfContents.Platform.DefaultDevice;

  /// <summary>
  /// The default port number to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property); -1 means no
  /// explicit port is used.
  /// </summary>
  public static int DefaultPort { get; set; } = -1;

  /// <summary>The default URL scheme to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property).</summary>
  public static string DefaultUrlScheme { get; set; } = "https";

  /// <summary>
  /// The default website to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property).<br/>
  /// This must not include any URL scheme; that can be configured via <see cref="DefaultUrlScheme"/>.
  /// </summary>
  public static string DefaultWebSite { get; set; } = "musicbrainz.org";

  /// <summary>The trace source (named 'MetaBrainz.MusicBrainz.DiscId') used by this class.</summary>
  public static readonly TraceSource TraceSource = Tracing.Source;

  #endregion

  #region Static Methods

  /// <summary>Determines whether the specified feature(s) are supported for use with <see cref="ReadDisc"/>.</summary>
  /// <param name="feature">The feature(s) to test.</param>
  /// <returns><see langword="true"/> if all specified features are supported; <see langword="false"/> otherwise.</returns>
  public static bool HasReadFeature(DiscReadFeature feature) => TableOfContents.Platform.HasFeature(feature);

  /// <summary>
  /// Reads the table of contents for the current disc in the specified device, getting the requested information.
  /// </summary>
  /// <param name="device">
  /// The name of the device to read from; <see langword="null"/> to read from <see cref="DefaultDevice"/>.
  /// </param>
  /// <param name="features">The features to use (if supported). Note that the table of contents will always be read.</param>
  /// <returns>The table of contents for the current disc in <paramref name="device"/>.</returns>
  public static TableOfContents ReadDisc(string? device, DiscReadFeature features = DiscReadFeature.All)
    => TableOfContents.Platform.ReadTableOfContents(device, features);

  /// <summary>Simulates the reading of a disc, setting up a table of contents based on the specified information.</summary>
  /// <param name="first">The first audio track for the disc.</param>
  /// <param name="last">The last audio track for the disc.</param>
  /// <param name="offsets">
  /// Array of track offsets; the offset at index 0 should be the offset of the end of the last (audio) track.
  /// </param>
  /// <returns>The table of contents for a disc with the specified track layout.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="offsets"/> is null.</exception>
  public static TableOfContents SimulateDisc(byte first, byte last, int[] offsets) => new(first, last, offsets);

  #endregion

  #region Instance Properties

  /// <summary>The name of the device from which this table of contents was read (null if it was simulated).</summary>
  public string? DeviceName { get; }

  /// <summary>Returns the MusicBrainz Disc ID associated with this table of contents.</summary>
  public string DiscId => field ??= this.CalculateDiscId();

  /// <summary>The first (audio) track on the disc.</summary>
  public byte FirstTrack { get; }

  /// <summary>Returns the FreeDB Disc ID associated with this table of contents.</summary>
  public string FreeDbId => field ??= this.CalculateFreeDbId();

  /// <summary>The last (audio) track on the disc.</summary>
  public byte LastTrack { get; }

  /// <summary>
  /// The length, in sectors, of the disc (i.e. the starting sector of either the first data track or the lead-out).
  /// </summary>
  public int Length => this._tracks[0].Address;

  /// <summary>
  /// The media catalog number (typically the UPC/EAN) for the disc; null if not retrieved, empty if not available.
  /// </summary>
  public string? MediaCatalogNumber { get; }

  /// <summary>
  /// The port number to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property); -1 to not specify any
  /// explicit port.
  /// </summary>
  public int Port { get; set; }

  /// <summary>
  /// The album-level CD-TEXT data. If no such data is available, this is <see langword="null"/>; otherwise, it will have as many
  /// entries as <see cref="TextLanguages"/>.
  /// </summary>
  public IReadOnlyList<AlbumText>? TextInfo => this._albumText;

  /// <summary>
  /// The languages for which CD-TEXT data is available (null if CD-TEXT information is not available).
  /// </summary>
  public IReadOnlyList<EBU.LanguageCode>? TextLanguages => this._textLanguages;

  /// <summary>
  /// The tracks in this table of contents. Only subscripts between <see cref="FirstTrack"/> and <see cref="LastTrack"/>
  /// (inclusive) are valid.
  /// </summary>
  public AudioTrackCollection Tracks => new(this);

  /// <summary>The URL to open to submit information about this table of contents to MusicBrainz.</summary>
  public Uri SubmissionUrl => field ??= this.ConstructSubmissionUrl();

  /// <summary>The URL scheme to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property).</summary>
  public string UrlScheme { get; set; }

  /// <summary>The website to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property).</summary>
  public string WebSite { get; set; }

  #endregion

  #region Instance Methods

  /// <summary>Returns a string representing the TOC.</summary>
  /// <returns>
  /// A string consisting of the following values, separated by a space: the first track number, the last track number, the total
  /// length of the disc in sectors, and the offsets of all tracks.
  /// </returns>
  public override string ToString() {
    if (this._stringForm is not null) {
      return this._stringForm;
    }
    var sb = new StringBuilder();
    this.AppendTocString(sb, ' ');
    return this._stringForm = sb.ToString();
  }

  #endregion

  #region Track-Related Helper Types

  /// <summary>Class providing information about a single audio track on a cd-rom.</summary>
  [PublicAPI]
  public sealed class AudioTrack {

    internal AudioTrack(TableOfContents toc, byte number) {
      var address = toc._tracks[number].Address;
      var size = ((number == toc.LastTrack) ? toc._tracks[0] : toc._tracks[number + 1]).Address - address;
      this.Duration = new TimeSpan(0, 0, 0, 0, size * 1000 / 75);
      this.Isrc = toc._tracks[number].Isrc;
      this.Length = size;
      this.Number = number;
      this.Offset = address;
      this.StartTime = new TimeSpan(0, 0, 0, 0, address * 1000 / 75);
      if (toc._trackText is null) {
        return;
      }
      var idx = number - toc.FirstTrack;
      if (idx >= 0 && idx <= toc._trackText.Length) {
        this.TextInfo = toc._trackText[idx];
      }
    }

    /// <summary>The length of this track expressed as a timespan.</summary>
    public TimeSpan Duration { get; }

    /// <summary>The ISRC for the track. null if not retrieved, empty if not available.</summary>
    public string? Isrc { get; }

    /// <summary>The length, in sectors, of this track.</summary>
    public int Length { get; }

    /// <summary>The track number.</summary>
    public byte Number { get; }

    /// <summary>The start position, in sectors, of this track.</summary>
    public int Offset { get; }

    /// <summary>The start position of this track expressed as a timespan.</summary>
    public TimeSpan StartTime { get; }

    /// <summary>
    /// The track-level CD-TEXT data. If no such data is available, this is <see langword="null"/>; otherwise, it will have as many
    /// entries as <see cref="TextLanguages"/>.
    /// </summary>
    public IReadOnlyList<TrackText>? TextInfo { get; }

  }

  /// <summary>A collection of information about tracks on an audio cd.</summary>
  [PublicAPI]
  public sealed class AudioTrackCollection : IReadOnlyList<AudioTrack> {

    internal AudioTrackCollection(TableOfContents toc) {
      this._toc = toc;
    }

    private readonly TableOfContents _toc;

    /// <summary>Gets the number of tracks in the <see cref="AudioTrackCollection"/>.</summary>
    public int Count => 1 + this._toc.LastTrack - this._toc.FirstTrack;

    /// <summary>The first valid track number for the collection.</summary>
    public byte FirstTrack => this._toc.FirstTrack;

    /// <summary>The last valid track number for the collection.</summary>
    public byte LastTrack => this._toc.LastTrack;

    /// <summary>Gets the track with the specified number.</summary>
    /// <param name="number">
    /// The track number to get; must be between <see cref="FirstTrack"/> and <see cref="LastTrack"/>, inclusive.
    /// </param>
    /// <returns>The track with the specified number.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// When <paramref name="number"/> is not between <see cref="FirstTrack"/> and <see cref="LastTrack"/>, inclusive.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// When an attempt is made to set an element, because a <see cref="AudioTrackCollection"/> is read-only.
    /// </exception>
    public AudioTrack this[int number] {
      get {
        if (number >= this.FirstTrack && number <= this.LastTrack) {
          return new AudioTrack(this._toc, (byte) number);
        }
        var msg = $"Invalid track number (valid track numbers range from {this.FirstTrack} to {this.LastTrack}).";
        throw new ArgumentOutOfRangeException(nameof(number), number, msg);
      }
      set => throw new NotSupportedException();
    }

    #region Enumerator

    private sealed class Enumerator(AudioTrackCollection collection) : IEnumerator<AudioTrack> {

      private byte _index;

      /// <summary>
      /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
      /// </summary>
      public void Dispose() { }

      /// <summary>Advances the enumerator to the next element of the collection.</summary>
      /// <returns>
      /// <see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the
      /// enumerator has passed the end of the collection.
      /// </returns>
      public bool MoveNext() {
        if (this._index == 0) {
          this._index = collection.FirstTrack;
          return true;
        }
        if (this._index > collection.LastTrack) {
          return false;
        }
        ++this._index;
        return this._index <= collection.LastTrack;
      }

      /// <summary>Sets the enumerator to its initial position, which is before the first element in the collection.</summary>
      public void Reset() => this._index = 0;

      /// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
      /// <returns>The element in the collection at the current position of the enumerator.</returns>
      public AudioTrack Current => collection[this._index];

      /// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
      /// <returns>The element in the collection at the current position of the enumerator.</returns>
      object IEnumerator.Current => this.Current;

    }

    /// <summary>Returns an enumerator that iterates through the collection.</summary>
    /// <returns>
    /// An <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<AudioTrack> GetEnumerator() => new Enumerator(this);

    /// <summary>Returns an enumerator that iterates through the collection.</summary>
    /// <returns>
    /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    #endregion

  }

  #endregion

  #region Internal Constructors

  private TableOfContents(string? device, byte first, byte last, Track[] tracks) {
    if (first is 0 or > 99) {
      throw new ArgumentOutOfRangeException(nameof(first), first, "The first track number must be between 1 and 99.");
    }
    if (last is 0 or > 99) {
      throw new ArgumentOutOfRangeException(nameof(last), last, "The last track number must be between 1 and 99.");
    }
    if (last < first) {
      throw new ArgumentOutOfRangeException(nameof(last), last, "The last track number cannot be smaller than the first.");
    }
    if (tracks.Length < last + 1) {
      throw new ArgumentException("Not enough track data given.", nameof(tracks));
    }
    this.DeviceName = device;
    this.FirstTrack = first;
    this.LastTrack = last;
    this.Port = TableOfContents.DefaultPort;
    this.UrlScheme = TableOfContents.DefaultUrlScheme;
    this.WebSite = TableOfContents.DefaultWebSite;
    this._tracks = tracks;
  }

  private TableOfContents(byte first, byte last, int[] offsets) : this(null, first, last, new Track[last + 1]) {
    // libdiscid wants last + 1 entries, even if first > 1. So we do the same.
    if (offsets.Length < last + 1) {
      throw new ArgumentException($"Not enough offsets provided (need at least {last + 1}).", nameof(offsets));
    }
    if (offsets[0] > TableOfContents.MaxSectors) {
      throw new ArgumentException($"Disc is too large ({offsets[0]} > {TableOfContents.MaxSectors}).", nameof(offsets));
    }
    this._tracks[0] = new Track(offsets[0]);
    for (byte i = 1; i <= last; ++i) {
      if (offsets[i] > offsets[0]) {
        throw new ArgumentException($"Track offset #{i} points past the end of the disc.", nameof(offsets));
      }
      if (i > 1 && offsets[i] < offsets[i - 1]) {
        throw new ArgumentException($"Track offset #{i} points before the preceding track.", nameof(offsets));
      }
      this._tracks[i] = new Track(offsets[i]);
    }
  }

  internal TableOfContents(string device, byte first, byte last, Track[] tracks, string? mcn, RedBook.CDTextGroup? cdText)
    : this(device, first, last, tracks) {
    this.MediaCatalogNumber = mcn;
    this.LastTrack = 0;
    for (var i = first; i <= last; ++i) {
      if ((this._tracks[i].Control & MMC.SubChannelControl.ContentTypeMask) != MMC.SubChannelControl.Data) {
        this.LastTrack = i;
      }
    }
    if (this.LastTrack == 0) {
      throw new NotSupportedException("No audio tracks found: CD-ROM, DVD or BD?");
    }
    // If the last audio track is not the last track on the CD, use the offset of the next data track as the "lead-out" offset
    if (this.LastTrack < last) {
      tracks[0] = new Track(tracks[this.LastTrack + 1].Address - TableOfContents.XAInterval);
    }
    // As long as the lead-out isn't actually bigger than the position of the last track, the last track is invalid.
    // This happens on "copy-protected"/invalid discs. The track is then neither a valid audio track, nor data track.
    for (; this.LastTrack > 0 && tracks[0].Address < tracks[this.LastTrack].Address; --this.LastTrack) {
      tracks[0] = new Track(tracks[this.LastTrack].Address - TableOfContents.XAInterval);
    }
    if (this.LastTrack < this.FirstTrack) {
      throw new NotSupportedException("Invalid TOC (no tracks remain): \"copy-protected\" disc?");
    }
    this.ApplyCdTextInfo(cdText, out this._textLanguages, out this._albumText, out this._trackText);
  }

  #endregion

  #region Internal Fields

  private static readonly IPlatform Platform = MusicBrainz.DiscId.Platform.Create();

  private string? _stringForm;

  private readonly Track[] _tracks;

  private readonly EBU.LanguageCode[]? _textLanguages;

  private readonly AlbumText[]? _albumText;

  private readonly TrackText[][]? _trackText;

  #endregion

  #region Internal Methods

  private void AppendTocString(StringBuilder sb, char delimiter) {
    sb.Append(this.FirstTrack).Append(delimiter).Append(this.LastTrack).Append(delimiter).Append(this._tracks[0].Address);
    for (var i = this.FirstTrack; i <= this.LastTrack; ++i) {
      sb.Append(delimiter).Append(this._tracks[i].Address);
    }
  }

  private void ApplyCdTextInfo(RedBook.CDTextGroup? cdTextGroup, out EBU.LanguageCode[]? languages, out AlbumText[]? albumText,
                               out TrackText[][]? trackText) {
    languages = null;
    albumText = null;
    trackText = null;
    if (!cdTextGroup.HasValue) {
      return;
    }
    var packs = cdTextGroup.Value.Packs;
    if (packs is null || packs.Length == 0) {
      Tracing.Verbose(100, "No CD-TEXT packs found.");
      return;
    }
    // Assumption: Valid CD-TEXT blocks must have a SizeInfo entry.
    if (packs.Length < 3 || packs[^1].Type != RedBook.CDTextContentType.SizeInfo) {
      Tracing.Verbose(101, "No CD-TEXT information (packs do not end with SizeInfo data).");
      return;
    }
    RedBook.CDTextSizeInfo si;
    {
      var bytes = new byte[36];
      for (var i = 0; i < 3; ++i) {
        Array.Copy(packs[packs.Length - 3 + i].Data, 0, bytes, 12 * i, 12);
      }
      si = Util.MarshalBytesToStructure<RedBook.CDTextSizeInfo>(bytes);
    }
    var blockCount = 8;
    while (blockCount > 0 && si.LastSequenceNumber[blockCount - 1] == 0) {
      --blockCount;
    }
    if (blockCount == 0) {
      Tracing.Verbose(102, "No CD-TEXT information (size info says there are 0 blocks).");
      return;
    }
    // Now set up the info arrays
    languages = new EBU.LanguageCode[blockCount];
    for (var b = 0; b < blockCount; ++b) {
      languages[b] = si.LanguageCode[b];
    }
    albumText = new AlbumText[blockCount];
    {
      var trackCount = this.LastTrack - this.FirstTrack + 1;
      trackText = new TrackText[trackCount][];
      for (var t = 0; t < trackCount; ++t) {
        trackText[t] = new TrackText[blockCount];
      }
    }
    // Process the blocks
    var p = 0;
    for (var b = 0; b < blockCount; ++b) {
      var endPack = si.LastSequenceNumber[b];
      var blockBytes = new Dictionary<RedBook.CDTextContentType, List<byte>>();
      var albumInfo = new HashSet<RedBook.CDTextContentType>();
      Tracing.Verbose(103, "Processing CD-TEXT block #{0} (language: {1})...", b + 1, si.LanguageCode[b]);
      var dbcs = packs[p].IsUnicode;
      for (; p <= endPack; ++p) {
        var pack = packs[p];
        if (!pack.IsValid) {
          Tracing.Verbose(104, "Ignoring CD-TEXT pack #{0} (type: {1}) because it failed the CRC check.", p + 1, pack.Type);
          continue;
        }
        if (pack.IsExtension) {
          Tracing.Verbose(105, "Ignoring CD-TEXT pack #{0} (type: {1}) because it's flagged as an extension.", p + 1, pack.Type);
          continue;
        }
        if (pack.IsUnicode != dbcs) {
          Tracing.Verbose(106, "CD-TEXT Pack #{0} (type: {1}) has a mismatched DBCS flag.", p + 1, pack.Type);
        }
        if (!blockBytes.TryGetValue(pack.Type, out var bytes)) {
          blockBytes.Add(pack.Type, bytes = [ ]);
        }
        bytes.AddRange(pack.Data);
        if (pack.ID2 == 0) {
          albumInfo.Add(pack.Type);
        }
      }
      if (blockBytes.TryGetValue(RedBook.CDTextContentType.SizeInfo, out var sizeInfoBytes) && sizeInfoBytes.Count == 36) {
        si = Util.MarshalBytesToStructure<RedBook.CDTextSizeInfo>(sizeInfoBytes.ToArray());
      }
      else {
        Tracing.Verbose(107, "Ignoring CD-TEXT block because it does not include size info.");
        continue;
      }
      blockBytes.Remove(RedBook.CDTextContentType.SizeInfo);
      if (!PackSizeIsValid(RedBook.CDTextContentType.Title, si.PacksWithType80)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.Performer, si.PacksWithType81)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.Lyricist, si.PacksWithType82)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.Composer, si.PacksWithType83)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.Arranger, si.PacksWithType84)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.Messages, si.PacksWithType85)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.DiscID, si.PacksWithType86)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.Genre, si.PacksWithType87)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.TOCInfo1, si.PacksWithType88)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.TOCInfo2, si.PacksWithType89)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.Reserved1, si.PacksWithType8A)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.Reserved2, si.PacksWithType8B)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.Reserved3, si.PacksWithType8C)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.Internal, si.PacksWithType8D)) {
        continue;
      }
      if (!PackSizeIsValid(RedBook.CDTextContentType.Code, si.PacksWithType8E)) {
        continue;
      }
      // While there is a PacksWithType8F too, that seems to always be 0 while the size info always spans 3 packs (36 bytes).
      Encoding encoding;
      if (dbcs) {
        Tracing.Verbose(108, "This CD-TEXT block contains DBCS data; assuming this means UTF-16.");
        encoding = Encoding.BigEndianUnicode;
      }
      else {
        switch (si.CharacterCode) {
          case RedBook.CDTextCharacterCode.ISO_646:
            encoding = Encoding.ASCII;
            break;
          case RedBook.CDTextCharacterCode.ISO_8859_1:
            // FIXME: It's supposed to be a modified form of latin-1, but without the Blue Book, it's unclear what those
            //        modifications entail.
            Tracing.Verbose(109, "Using plain ISO-8859-1 as CD-TEXT block encoding; some characters may not be correct.");
            encoding = Encoding.GetEncoding("iso-8859-1");
            break;
          case RedBook.CDTextCharacterCode.Korean:
            Tracing.Verbose(110, "Assuming EUC-KR as Korean CD-TEXT block encoding.");
            encoding = Encoding.GetEncoding("euc-kr");
            break;
          case RedBook.CDTextCharacterCode.MandarinChinese:
            Tracing.Verbose(111, "Assuming GB2312 as Mandarin Chinese CD-TEXT block encoding.");
            encoding = Encoding.GetEncoding("gb2312");
            break;
          case RedBook.CDTextCharacterCode.MusicShiftJis:
            // FIXME: Without standard RIAJ RS506 it's unclear how this differs from plain Shift-JIS, but some comments online
            //        suggest it has a LOT of extra emoji.
            Tracing.Verbose(112, "Using plain Shift-Jis as CD-TEXT block encoding; some characters may not be correct.");
            encoding = Encoding.GetEncoding("iso-2022-jp");
            break;
          default:
            if ((byte) si.CharacterCode >= 0x83) {
              Tracing.Verbose(113, "Ignoring this CD-TEXT block because it specifies a reserved character set ({0}).",
                              si.CharacterCode);
            }
            else {
              Tracing.Verbose(114, "Ignoring this CD-TEXT block because it specifies an unknown character set ({0}).",
                              si.CharacterCode);
            }
            continue;
        }
      }
      var latin1 = Encoding.GetEncoding("iso-8859-1");
      BlueBook.Genre? genre = null;
      string? genreDescription = null;
      if (blockBytes.TryGetValue(RedBook.CDTextContentType.Genre, out var genreBytes)) {
        var rawGenre = genreBytes.ToArray();
        var code = BitConverter.ToInt16(rawGenre, 0);
        if (BitConverter.IsLittleEndian) {
          code = IPAddress.NetworkToHostOrder(code);
        }
        if (code == 0 && genreBytes.Count == 2) {
          Tracing.Verbose(115, "CD-TEXT: Ignoring genre information because it is set to zero.");
        }
        else {
          genre = (BlueBook.Genre) code;
          genreDescription = latin1.GetString(rawGenre, 2, rawGenre.Length - 2).TrimEnd('\0');
          if (genreDescription.Length == 0) {
            Tracing.Verbose(116, "CD-TEXT: Genre set as {0:D} ({0}), with no description.", genre);
            genreDescription = null;
          }
          else {
            Tracing.Verbose(117, "CD-TEXT: Genre set as {0:D} ({0}) [{1}].", genre, genreDescription);
          }
        }
        blockBytes.Remove(RedBook.CDTextContentType.Genre);
      }
      var discId = DecodeSingleString(RedBook.CDTextContentType.DiscID);
      if (discId is not null) {
        Tracing.Verbose(118, "CD-TEXT: Disc identification set as [{0}].", discId);
      }
      var closedInfo = DecodeSingleString(RedBook.CDTextContentType.Internal);
      if (closedInfo is not null) {
        Tracing.Verbose(119, "CD-TEXT: Closed information set as [{0}].", closedInfo);
      }
      var tracks = si.LastTrack - si.FirstTrack + 1;
      var (titles, albumTitle) = DecodeText(RedBook.CDTextContentType.Title);
      var (performers, albumPerformer) = DecodeText(RedBook.CDTextContentType.Performer);
      var (lyricists, albumLyricist) = DecodeText(RedBook.CDTextContentType.Lyricist);
      var (composers, albumComposer) = DecodeText(RedBook.CDTextContentType.Composer);
      var (arrangers, albumArranger) = DecodeText(RedBook.CDTextContentType.Arranger);
      var (messages, albumMessage) = DecodeText(RedBook.CDTextContentType.Messages);
      var (codes, albumCode) = DecodeText(RedBook.CDTextContentType.Code);
      if (Tracing.Source.Switch.ShouldTrace(TraceEventType.Verbose)) {
        // The two TOC types are binary data, and not relevant to us.
        blockBytes.Remove(RedBook.CDTextContentType.TOCInfo1);
        blockBytes.Remove(RedBook.CDTextContentType.TOCInfo2);
        ReportUnhandledText();
        blockBytes.Clear();
      }
      {
        var title = (albumTitle && titles?.Length > 0) ? titles[0] : null;
        var performer = (albumPerformer && performers?.Length > 0) ? performers[0] : null;
        var lyricist = (albumLyricist && lyricists?.Length > 0) ? lyricists[0] : null;
        var composer = (albumComposer && composers?.Length > 0) ? composers[0] : null;
        var arranger = (albumArranger && arrangers?.Length > 0) ? arrangers[0] : null;
        var message = (albumMessage && messages?.Length > 0) ? messages[0] : null;
        var code = (albumCode && codes?.Length > 0) ? codes[0] : null;
        if (genre.HasValue || discId is not null || closedInfo is not null || title is not null || performer is not null ||
            lyricist is not null || composer is not null || arranger is not null || message is not null || code is not null) {
          albumText[b] = new AlbumText {
            ClosedInformation = closedInfo,
            Genre = genre,
            GenreDescription = genreDescription,
            Identification = discId,
            Title = title,
            Performer = performer,
            Lyricist = lyricist,
            Composer = composer,
            Arranger = arranger,
            Message = message,
            ProductCode = code,
          };
        }
      }
      var delta = si.FirstTrack - this.FirstTrack;
      for (var t = 0; t < tracks; ++t) {
        if (t + delta < 0) {
          continue;
        }
        if (t + delta >= trackText.Length) {
          break;
        }
        var idx = t + (albumTitle ? 1 : 0);
        var title = (titles?.Length > idx) ? titles[idx] : null;
        idx = t + (albumPerformer ? 1 : 0);
        var performer = (performers?.Length > idx) ? performers[idx] : null;
        idx = t + (albumLyricist ? 1 : 0);
        var lyricist = (lyricists?.Length > idx) ? lyricists[idx] : null;
        idx = t + (albumComposer ? 1 : 0);
        var composer = (composers?.Length > idx) ? composers[idx] : null;
        idx = t + (albumArranger ? 1 : 0);
        var arranger = (arrangers?.Length > idx) ? arrangers[idx] : null;
        idx = t + (albumMessage ? 1 : 0);
        var message = (messages?.Length > idx) ? messages[idx] : null;
        idx = t + (albumCode ? 1 : 0);
        var code = (codes?.Length > idx) ? codes[idx] : null;
        if (title is not null || performer is not null || lyricist is not null || composer is not null || arranger is not null ||
            message is not null || code is not null) {
          trackText[t][b] = new TrackText {
            Title = title,
            Performer = performer,
            Lyricist = lyricist,
            Composer = composer,
            Arranger = arranger,
            Message = message,
            Isrc = code,
          };
        }
      }
      continue;
      string? DecodeSingleString(RedBook.CDTextContentType type) {
        return blockBytes.Remove(type, out var bytes) ? latin1.GetString(bytes.ToArray()).TrimEnd('\0') : null;
      }
      (string[]?, bool) DecodeText(RedBook.CDTextContentType type) {
        var includesAlbumLevelValue = albumInfo.Contains(type);
        var count = tracks + (includesAlbumLevelValue ? 1 : 0);
        if (!blockBytes.TryGetValue(type, out var bytes)) {
          return (null, false);
        }
        var values = TableOfContents.TextValue(type, bytes, encoding, count);
        blockBytes.Remove(type);
        return (values, includesAlbumLevelValue);
      }
      bool PackSizeIsValid(RedBook.CDTextContentType type, int count) {
        var expectedBytes = 12 * count;
        var actualBytes = blockBytes.TryGetValue(type, out var bytes) ? bytes.Count : 0;
        if (actualBytes != expectedBytes) {
          Tracing.Verbose(120, "Ignoring this CD-TEXT block: type {0:X} ({0}) pack size mismatch ({1} != {2}).", type, actualBytes,
                          expectedBytes);
        }
        return actualBytes == expectedBytes;
      }
      void ReportUnhandledText() {
        foreach (var item in blockBytes) {
          var text = latin1.GetString(item.Value.ToArray()).TrimEnd('\0');
          Tracing.Verbose(121, "CD-TEXT: Ignoring text data of type {0:X} ({0}): [{1}].", item.Key, text);
        }
      }
    }
  }

  private string CalculateDiscId() {
    var sb = new StringBuilder(804);
    sb.Append(this.FirstTrack.ToString("X2"));
    sb.Append(this.LastTrack.ToString("X2"));
    for (var i = 0; i < 100; ++i) {
      sb.Append(i <= this.LastTrack ? this._tracks[i].Address.ToString("X8") : "00000000");
    }
    var textBytes = Encoding.ASCII.GetBytes(sb.ToString());
    return Convert.ToBase64String(SHA1.HashData(textBytes)).Replace('/', '_').Replace('+', '.').Replace('=', '-');
  }

  private string CalculateFreeDbId() {
    var n = 0;
    for (var i = 0; i < this.LastTrack; ++i) {
      var m = this._tracks[i + 1].Address / 75;
      while (m > 0) {
        n += m % 10;
        m /= 10;
      }
    }
    var t = (this._tracks[0].Address / 75) - (this._tracks[1].Address / 75);
    return (((n % 0xff) << 24) | (t << 8) | this.LastTrack).ToString("x8");
  }

  private Uri ConstructSubmissionUrl() {
    var uri = new UriBuilder(this.UrlScheme, this.WebSite, this.Port, "cdtoc/attach", null);
    var query = new StringBuilder();
    query.Append("id=").Append(Uri.EscapeDataString(this.DiscId));
    // Avoids having to explicitly run Uri.EscapeDataString on all the integers
    var culture = Thread.CurrentThread.CurrentCulture;
    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
    try {
      query.Append("&tracks=").Append(1 + this.LastTrack - this.FirstTrack).Append("&toc=");
      this.AppendTocString(query, '+');
    }
    finally {
      Thread.CurrentThread.CurrentCulture = culture;
    }
    uri.Query = query.ToString();
    return uri.Uri;
  }

  private static readonly char[] TextPackSeparator = [ '\0' ];

  private static string[]? TextValue(RedBook.CDTextContentType type, List<byte> data, Encoding encoding, int items) {
    if (data.Count == 0) {
      return null;
    }
    var parts = encoding.GetString(data.ToArray()).Split(TableOfContents.TextPackSeparator, items);
    if (parts.Length == items) {
      parts[items - 1] = parts[items - 1].TrimEnd('\0');
    }
    if (parts.Length < items) {
      Tracing.Verbose(200, "CD-TEXT: Not enough values provided in the {0} packs (expected {1}, got {2}).", type, items,
                      parts.Length);
    }
    for (var i = 0; i < parts.Length; ++i) {
      // TAB means "same as preceding value"
      if (parts[i] == "\t") {
        if (i == 0) {
          Tracing.Verbose(201, "CD-TEXT: Found a TAB in the first value of the {0} packs.", type);
          parts[i] = "";
        }
        else {
          parts[i] = parts[i - 1];
          Tracing.Verbose(202, "CD-TEXT: Value #{0} for content type {1:X} ({1}) is the same as the previous value.", i + 1, type);
        }
      }
      else {
        Tracing.Verbose(203, "CD-TEXT: Value #{0} for content type {1:X} ({1}) is [{2}].", i + 1, type, parts[i]);
      }
    }
    return parts;
  }

  #endregion

}
