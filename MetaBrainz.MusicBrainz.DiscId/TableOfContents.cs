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

namespace MetaBrainz.MusicBrainz.DiscId {

  /// <summary>Class representing a CD's table of contents.</summary>
  [PublicAPI]
  public sealed class TableOfContents {

    #region Constants

    // FIXME: libdiscid (rather arbitrarily) uses 90 minutes. But overburning can go above 100 minutes. For now, use 99:59.74.
    /// <summary>The largest possible AudioCD (1 sector shy of 100 minutes).</summary>
    /// <remarks>This is only used for validation of user-supplied offsets (<see cref="SimulateDisc"/>).</remarks>
    public const int MaxSectors = 100 * 60 * 75 - 1;

    /// <summary>The distance between the last audio track and the first data track.</summary>
    public const int XAInterval = ((60 + 90 + 2) * 75);

    #endregion

    #region Static Properties

    /// <summary>Enumerates the names of all cd-rom devices in the system.</summary>
    /// <returns>The names of all cd-rom devices in the system.</returns>
    public static IEnumerable<string> AvailableDevices => Platform.AvailableDevices;

    /// <summary>The list of features supported on this platform for use with <see cref="ReadDisc"/>.</summary>
    public static DiscReadFeature AvailableFeatures => Platform.AvailableFeatures;

    /// <summary>The default cd-rom device (used when passing <see langword="null"/> to <see cref="ReadDisc"/>).</summary>
    public static string? DefaultDevice => Platform.DefaultDevice;

    /// <summary>
    /// The default port number to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property); -1 means no
    /// explicit port is used.
    /// </summary>
    public static int DefaultPort { get; set; } = -1;

    /// <summary>The default URL scheme to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property).</summary>
    public static string DefaultUrlScheme { get; set; } = "https";

    /// <summary>
    ///   The default web site to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property).
    ///   Must not include any URL scheme; that can be configured via <see cref="DefaultUrlScheme"/>.
    /// </summary>
    public static string DefaultWebSite { get; set; } = "musicbrainz.org";

    /// <summary>Determines whether or not the specified feature(s) are supported for use with <see cref="ReadDisc"/>.</summary>
    /// <param name="feature">The feature(s) to test.</param>
    /// <returns><see langword="true"/> if all specified features are supported; <see langword="false"/> otherwise.</returns>
    public static bool HasReadFeature(DiscReadFeature feature) => Platform.HasFeature(feature);

    #endregion

    #region Static Methods

    /// <summary>
    /// Reads the table of contents for the current disc in the specified device, getting the requested information.
    /// </summary>
    /// <param name="device">
    /// The name of the device to read from; <see langword="null"/> to read from <see cref="DefaultDevice"/>.
    /// </param>
    /// <param name="features">The features to use (if supported). Note that the table of contents will always be read.</param>
    /// <returns>The table of contents for the current disc in <paramref name="device"/>.</returns>
    public static TableOfContents ReadDisc(string? device, DiscReadFeature features = DiscReadFeature.All)
      => Platform.ReadTableOfContents(device, features);

    /// <summary>Simulates the reading of a disc, setting up a table of contents based on the specified information.</summary>
    /// <param name="first">The first audio track for the disc.</param>
    /// <param name="last">The last audio track for the disc.</param>
    /// <param name="offsets">
    /// Array of track offsets; the offset at index 0 should be the offset of the end of the last (audio) track.
    /// </param>
    /// <returns>The table of contents for a disc with the specified track layout.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="offsets"/> is null.</exception>
    public static TableOfContents SimulateDisc(byte first, byte last, int[] offsets) => new TableOfContents(first, last, offsets);

    #endregion

    #region Instance Properties

    /// <summary>The name of the device from which this table of contents was read (null if it was simulated).</summary>
    public string? DeviceName { get; }

    /// <summary>Returns the MusicBrainz Disc ID associated with this table of contents.</summary>
    public string DiscId => this._discid ??= this.CalculateDiscId();

    /// <summary>The first (audio) track on the disc.</summary>
    public byte FirstTrack { get; }

    /// <summary>Returns the FreeDB Disc ID associated with this table of contents.</summary>
    public string FreeDbId => this._freeDbId ??= this.CalculateFreeDbId();

    /// <summary>The last (audio) track on the disc.</summary>
    public byte LastTrack { get; }

    /// <summary>The length, in sectors, of the disc (i.e. the starting sector of either the first data track or the lead-out).</summary>
    public int Length => this._tracks[0].Address;

    /// <summary>The media catalog number (typically the UPC/EAN) for the disc; null if not retrieved, empty if not available.</summary>
    public string? MediaCatalogNumber { get; }

    /// <summary>The port number to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property); -1 to not specify any explicit port.</summary>
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
    public AudioTrackCollection Tracks => new AudioTrackCollection(this);

    /// <summary>The URL to open to submit information about this table of contents to MusicBrainz.</summary>
    public Uri SubmissionUrl => this._url ??= this.ConstructSubmissionUrl();

    /// <summary>The URL scheme to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property).</summary>
    public string UrlScheme { get; set; }

    /// <summary>The web site to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property).</summary>
    public string WebSite { get; set; }

    #endregion

    #region Instance Methods

    /// <summary>Returns a string representing the TOC.</summary>
    /// <returns>
    ///   A string consisting of the following values, separated by a space: the first track number, the last track number,
    ///   the total length of the disc in sectors, and the offsets of all tracks.
    /// </returns>
    public override string ToString() {
      if (this._stringForm != null)
        return this._stringForm;
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
        this.Duration  = new TimeSpan(0, 0, 0, 0, size * 1000 / 75);
        this.Isrc      = toc._tracks[number].Isrc;
        this.Length    = size;
        this.Number    = number;
        this.Offset    = address;
        this.StartTime = new TimeSpan(0, 0, 0, 0, address * 1000 / 75);
        if (toc._trackText == null)
          return;
        var idx = number - toc.FirstTrack;
        if (idx >= 0 && idx <= toc._trackText.Length)
          this.TextInfo = toc._trackText[idx];
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
      ///   The track-level CD-TEXT data.
      ///   If no such data is available, this is null; otherwise, it will have as many entries as <see cref="TextLanguages"/>.
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
      /// <returns>The number of number of tracks in the <see cref="AudioTrackCollection"/>.</returns>
      public int Count => 1 + this._toc.LastTrack - this._toc.FirstTrack;

      /// <summary>The first valid track number for the collection.</summary>
      public byte FirstTrack => this._toc.FirstTrack;

      /// <summary>The last valid track number for the collection.</summary>
      public byte LastTrack => this._toc.LastTrack;

      /// <summary>Gets the track with the specified number.</summary>
      /// <param name="number">The track number to get; must be between <see cref="FirstTrack"/> and <see cref="LastTrack"/>, inclusive.</param>
      /// <returns>The track with the specified number.</returns>
      /// <exception cref="ArgumentOutOfRangeException">When <paramref name="number"/> is not between <see cref="FirstTrack"/> and <see cref="LastTrack"/>, inclusive.</exception>
      /// <exception cref="NotSupportedException">When an attempt is made to set an element, because a <see cref="AudioTrackCollection"/> is read-only.</exception>
      public AudioTrack this[int number] {
        get {
          if (number < this.FirstTrack || number > this.LastTrack)
            throw new ArgumentOutOfRangeException(nameof(number), number, $"Invalid track number (valid track numbers range from {this.FirstTrack} to {this.LastTrack}).");
          return new AudioTrack(this._toc, (byte) number);
        }
        set => throw new NotSupportedException();
      }

      #region Enumerator

      private sealed class Enumerator : IEnumerator<AudioTrack> {

        public Enumerator(AudioTrackCollection collection) {
          this._collection = collection;
        }

        private readonly AudioTrackCollection _collection;
        private byte _index;

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose() { }

        /// <summary>Advances the enumerator to the next element of the collection.</summary>
        /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext() {
          if (this._index == 0) {
            this._index = this._collection.FirstTrack;
            return true;
          }
          if (this._index > this._collection.LastTrack)
            return false;
          ++this._index;
          return (this._index <= this._collection.LastTrack);
        }

        /// <summary>Sets the enumerator to its initial position, which is before the first element in the collection.</summary>
        public void Reset() {
          this._index = 0;
        }

        /// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        public AudioTrack Current => this._collection[this._index];

        /// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        object IEnumerator.Current => this.Current;

      }

      /// <summary>Returns an enumerator that iterates through the collection.</summary>
      /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
      public IEnumerator<AudioTrack> GetEnumerator() { return new Enumerator(this); }

      /// <summary>Returns an enumerator that iterates through the collection.</summary>
      /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
      IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }

      #endregion

    }

    #endregion

    #region Internal Constructors

    private TableOfContents(string? device, byte first, byte last, Track[] tracks) {
      if (first == 0 || first > 99)
        throw new ArgumentOutOfRangeException(nameof(first), first, "The first track number must be between 1 and 99.");
      if (last == 0 || last > 99)
        throw new ArgumentOutOfRangeException(nameof(last), last, "The last track number must be between 1 and 99.");
      if (last < first)
        throw new ArgumentOutOfRangeException(nameof(last), last, "The last track number cannot be smaller than the first.");
      if (tracks.Length < last + 1)
        throw new ArgumentException("Not enough track data given.", nameof(tracks));
      this.DeviceName = device;
      this.FirstTrack = first;
      this.LastTrack  = last;
      this.Port       = DefaultPort;
      this.UrlScheme  = DefaultUrlScheme;
      this.WebSite    = DefaultWebSite;
      this._tracks = tracks;
    }

    private TableOfContents(byte first, byte last, int[] offsets) : this(null, first, last, new Track[last + 1]) {
      // libdiscid wants last + 1 entries, even if first > 1. So we do the same.
      if (offsets.Length < last + 1)
        throw new ArgumentException(nameof(offsets), $"Not enough offsets provided (need at least {last + 1}).");
      if (offsets[0] > MaxSectors)
        throw new ArgumentException(nameof(offsets), $"Disc is too large ({offsets[0]} > {MaxSectors}).");
      this._tracks[0] = new Track(offsets[0]);
      for (byte i = 1; i <= last; ++i) {
        if (offsets[i] > offsets[0])
          throw new ArgumentException(nameof(offsets), $"Track offset #{i} points past the end of the disc.");
        if (i > 1 && offsets[i] < offsets[i - 1])
          throw new ArgumentException(nameof(offsets), $"Track offset #{i} points before the preceding track.");
        this._tracks[i] = new Track(offsets[i]);
      }
    }

    internal TableOfContents(string device, byte first, byte last, Track[] tracks, string? mcn, RedBook.CDTextGroup? cdText) : this(device, first, last, tracks) {
      this.MediaCatalogNumber = mcn;
      this.LastTrack = 0;
      for (var i = first; i <= last; ++i) {
        if ((this._tracks[i].Control & MMC.SubChannelControl.ContentTypeMask) != MMC.SubChannelControl.Data)
          this.LastTrack = i;
      }
      if (this.LastTrack == 0)
        throw new NotSupportedException("No audio tracks found: CD-ROM, DVD or BD?");
      // If the last audio track is not the last track on the CD, use the offset of the next data track as the "lead-out" offset
      if (this.LastTrack < last)
        tracks[0] = new Track(tracks[this.LastTrack + 1].Address - XAInterval);
      // As long as the lead-out isn't actually bigger than the position of the last track, the last track is invalid.
      // This happens on "copy-protected"/invalid discs. The track is then neither a valid audio track, nor data track.
      for (; this.LastTrack > 0 && tracks[0].Address < tracks[this.LastTrack].Address; --this.LastTrack)
        tracks[0] = new Track(tracks[this.LastTrack].Address - XAInterval);
      if (this.LastTrack < this.FirstTrack)
        throw new NotSupportedException("Invalid TOC (no tracks remain): \"copy-protected\" disc?");
      this.ApplyCdTextInfo(cdText, out this._textLanguages, out this._albumText, out this._trackText);
    }

    #endregion

    #region Internal Fields

    private static readonly IPlatform Platform = MusicBrainz.DiscId.Platform.Create();

    private string? _discid;

    private string? _freeDbId;

    private string? _stringForm;

    private readonly Track[] _tracks;

    private readonly EBU.LanguageCode[]? _textLanguages;

    private readonly AlbumText[]? _albumText;

    private readonly TrackText[][]? _trackText;

    private Uri? _url;

    #endregion

    #region Internal Methods

    private void AppendTocString(StringBuilder sb, char delimiter) {
      sb.Append(this.FirstTrack).Append(delimiter).Append(this.LastTrack).Append(delimiter).Append(this._tracks[0].Address);
      for (var i = this.FirstTrack; i <= this.LastTrack; ++i)
        sb.Append(delimiter).Append(this._tracks[i].Address);
    }

    private void ApplyCdTextInfo(RedBook.CDTextGroup? cdTextGroup, out EBU.LanguageCode[]? languages, out AlbumText[]? albumText, out TrackText[][]? trackText) {
      languages = null;
      albumText = null;
      trackText = null;
      if (!cdTextGroup.HasValue)
        return;
      var packs = cdTextGroup.Value.Packs;
      if (packs == null || packs.Length == 0) {
        Trace.WriteLine("No CD-TEXT information (no packs found).", "CD-TEXT");
        return;
      }
      // Assumption: Valid CD-TEXT blocks must have a SizeInfo entry.
      if (packs.Length < 3 || packs[packs.Length - 1].Type != RedBook.CDTextContentType.SizeInfo) {
        Trace.WriteLine("No CD-TEXT information (packs do not end with SizeInfo data).", "CD-TEXT");
        return;
      }
      RedBook.CDTextSizeInfo si;
      {
        var bytes = new byte[36];
        for (var i = 0; i < 3; ++i)
          Array.Copy(packs[packs.Length - 3 + i].Data, 0, bytes, 12 * i, 12);
        si = Util.MarshalBytesToStructure<RedBook.CDTextSizeInfo>(bytes);
      }
      var blockCount = 8;
      while (blockCount > 0 && si.LastSequenceNumber[blockCount - 1] == 0)
        --blockCount;
      if (blockCount == 0) {
        Trace.WriteLine("No CD-TEXT information (size info says there are 0 blocks).", "CD-TEXT");
        return;
      }
      // Now set up the info arrays
      languages = new EBU.LanguageCode[blockCount];
      for (var b = 0; b < blockCount; ++b)
        languages[b] = si.LanguageCode[b];
      albumText = new AlbumText[blockCount];
      {
        var trackCount = (this.LastTrack - this.FirstTrack + 1);
        trackText = new TrackText[trackCount][];
        for (var t = 0; t < trackCount; ++t)
          trackText[t] = new TrackText[blockCount];
      }
      // Process the blocks
      var p = 0;
      for (var b = 0; b < blockCount; ++b) {
        var endPack        = si.LastSequenceNumber[b];
        var titleBytes     = new List<byte>();
        var performerBytes = new List<byte>();
        var lyricistBytes  = new List<byte>();
        var composerBytes  = new List<byte>();
        var arrangerBytes  = new List<byte>();
        var messageBytes   = new List<byte>();
        var identBytes     = new List<byte>();
        var genreBytes     = new List<byte>();
        var codeBytes      = new List<byte>();
        var sizeInfoBytes  = new List<byte>();
        var albumTitle     = false;
        var albumPerformer = false;
        var albumLyricist  = false;
        var albumComposer  = false;
        var albumArranger  = false;
        var albumMessage   = false;
        var albumCode      = false;
        Trace.WriteLine($"Processing CD-TEXT block #{b + 1} (language: {si.LanguageCode[b]})...", "CD-TEXT");
        var dbcs = packs[p].IsUnicode;
        for (; p <= endPack; ++p) {
          var pack = packs[p];
          if (!pack.IsValid) {
            Trace.WriteLine($"Ignoring pack #{p + 1} (type: {pack.Type}) because it failed the CRC check.", "CD-TEXT");
            continue;
          }
          if (pack.IsExtension) {
            Trace.WriteLine($"Ignoring pack #{p + 1} (type: {pack.Type}) because it's flagged as an extension.", "CD-TEXT");
            continue;
          }
          if (pack.IsUnicode != dbcs)
            Trace.WriteLine($"Pack #{p + 1} (type: {pack.Type}) has a mismatched DBCS flag.", "CD-TEXT");
          switch (pack.Type) {
            case RedBook.CDTextContentType.Arranger:  arrangerBytes .AddRange(pack.Data); if (pack.ID2 == 0) albumArranger  = true; break;
            case RedBook.CDTextContentType.Code:      codeBytes     .AddRange(pack.Data); if (pack.ID2 == 0) albumCode      = true; break;
            case RedBook.CDTextContentType.Composer:  composerBytes .AddRange(pack.Data); if (pack.ID2 == 0) albumComposer  = true; break;
            case RedBook.CDTextContentType.DiscID:    identBytes    .AddRange(pack.Data); break;
            case RedBook.CDTextContentType.Genre:     genreBytes    .AddRange(pack.Data); break;
            case RedBook.CDTextContentType.Lyricist:  lyricistBytes .AddRange(pack.Data); if (pack.ID2 == 0) albumLyricist  = true; break;
            case RedBook.CDTextContentType.Messages:  messageBytes  .AddRange(pack.Data); if (pack.ID2 == 0) albumMessage   = true; break;
            case RedBook.CDTextContentType.Performer: performerBytes.AddRange(pack.Data); if (pack.ID2 == 0) albumPerformer = true; break;
            case RedBook.CDTextContentType.SizeInfo:  sizeInfoBytes .AddRange(pack.Data); break;
            case RedBook.CDTextContentType.Title:     titleBytes    .AddRange(pack.Data); if (pack.ID2 == 0) albumTitle     = true; break;
            default:
              Trace.WriteLine($"Ignoring pack #{p + 1} because it is of ignored or unsupported type '{pack.Type}'.", "CD-TEXT");
              break;
          }
        }
        if (sizeInfoBytes.Count == 36)
          si = Util.MarshalBytesToStructure<RedBook.CDTextSizeInfo>(sizeInfoBytes.ToArray());
        else {
          Trace.WriteLine("Ignoring this block because it does not include size info.", "CD-TEXT");
          continue;
        }
        // FIXME: Any skipped packs above will cause these checks to fail.
        if (si.PacksWithType80 * 12 != titleBytes    .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 80).", "CD-TEXT"); continue; }
        if (si.PacksWithType81 * 12 != performerBytes.Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 81).", "CD-TEXT"); continue; }
        if (si.PacksWithType82 * 12 != lyricistBytes .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 82).", "CD-TEXT"); continue; }
        if (si.PacksWithType83 * 12 != composerBytes .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 83).", "CD-TEXT"); continue; }
        if (si.PacksWithType84 * 12 != arrangerBytes .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 84).", "CD-TEXT"); continue; }
        if (si.PacksWithType85 * 12 != messageBytes  .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 85).", "CD-TEXT"); continue; }
        if (si.PacksWithType86 * 12 != identBytes    .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 86).", "CD-TEXT"); continue; }
        if (si.PacksWithType87 * 12 != genreBytes    .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 87).", "CD-TEXT"); continue; }
        if (si.PacksWithType8E * 12 != codeBytes     .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 8E).", "CD-TEXT"); continue; }
        if (si.PacksWithType8F * 12 != sizeInfoBytes .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 8F).", "CD-TEXT"); continue; }
        Encoding encoding;
        if (dbcs) {
          Trace.WriteLine("This block contains DBCS data; assuming this means UTF-16.", "CD-TEXT");
          encoding = Encoding.BigEndianUnicode;
        }
        else {
          switch (si.CharacterCode) {
            case RedBook.CDTextCharacterCode.ISO_646:
              encoding = Encoding.ASCII;
              break;
            case RedBook.CDTextCharacterCode.ISO_8859_1:
              // FIXME: It's supposed to be a modified form of latin-1, but without the Blue Book, it's unclear what those modifications entail.
              Trace.WriteLine("Using plain ISO-8859-1 as encoding; some characters may not be correct.", "CD-TEXT");
              encoding = Encoding.GetEncoding("iso-8859-1");
              break;
            case RedBook.CDTextCharacterCode.Korean:
              Trace.WriteLine("Assuming EUC-KR as Korean encoding.", "CD-TEXT");
              encoding = Encoding.GetEncoding("euc-kr");
              break;
            case RedBook.CDTextCharacterCode.MandarinChinese:
              Trace.WriteLine("Assuming GB2312 as Mandarin Chinese encoding.", "CD-TEXT");
              encoding = Encoding.GetEncoding("gb2312");
              break;
            case RedBook.CDTextCharacterCode.MusicShiftJis:
              // FIXME: Without standard RIAJ RS506 it's unclear how this differs from plain Shift-JIS, but some comments online suggest it has a LOT of extra emoji.
              Trace.WriteLine("Using plain Shift-Jis as encoding; some characters may not be correct.", "CD-TEXT");
              encoding = Encoding.GetEncoding("iso-2022-jp");
              break;
            default:
              Trace.WriteLine($"Ignoring this block because it specifies an unknown character set ({si.CharacterCode}).", "CD-TEXT");
              continue;
          }
        }
        var latin1 = Encoding.GetEncoding("iso-8859-1");
        BlueBook.Genre? genre = null;
        string? genreDescription = null;
        if (genreBytes.Count > 0) {
          var rawGenre = genreBytes.ToArray();
          var code = BitConverter.ToInt16(rawGenre, 0);
          if (BitConverter.IsLittleEndian)
            code = IPAddress.NetworkToHostOrder(code);
          genre = (BlueBook.Genre) code;
          genreDescription = latin1.GetString(rawGenre, 2, rawGenre.Length - 2).TrimEnd('\0');
          if (genreDescription.Length == 0)
            genreDescription = null;
        }
        string? ident = null;
        if (identBytes.Count > 0)
          ident = latin1.GetString(identBytes.ToArray()).TrimEnd('\0');
        var tracks = (si.LastTrack - si.FirstTrack + 1);
        var titles     = TextValue(RedBook.CDTextContentType.Title,     titleBytes,     encoding, tracks + (albumTitle     ? 1 : 0));
        var performers = TextValue(RedBook.CDTextContentType.Performer, performerBytes, encoding, tracks + (albumPerformer ? 1 : 0));
        var lyricists  = TextValue(RedBook.CDTextContentType.Lyricist,  lyricistBytes,  encoding, tracks + (albumLyricist  ? 1 : 0));
        var composers  = TextValue(RedBook.CDTextContentType.Composer,  composerBytes,  encoding, tracks + (albumComposer  ? 1 : 0));
        var arrangers  = TextValue(RedBook.CDTextContentType.Arranger,  arrangerBytes,  encoding, tracks + (albumArranger  ? 1 : 0));
        var messages   = TextValue(RedBook.CDTextContentType.Messages,  messageBytes,   encoding, tracks + (albumMessage   ? 1 : 0));
        var codes      = TextValue(RedBook.CDTextContentType.Code,      codeBytes,      latin1,   tracks + (albumCode      ? 1 : 0));
        {
          var title     = (albumTitle     && titles    ?.Length > 0) ? titles    [0] : null;
          var performer = (albumPerformer && performers?.Length > 0) ? performers[0] : null;
          var lyricist  = (albumLyricist  && lyricists ?.Length > 0) ? lyricists [0] : null;
          var composer  = (albumComposer  && composers ?.Length > 0) ? composers [0] : null;
          var arranger  = (albumArranger  && arrangers ?.Length > 0) ? arrangers [0] : null;
          var message   = (albumMessage   && messages  ?.Length > 0) ? messages  [0] : null;
          var code      = (albumCode      && codes     ?.Length > 0) ? codes     [0] : null;
          if (genre.HasValue || ident != null || title != null || performer != null || lyricist != null || composer != null || arranger != null || message != null || code != null) {
            albumText[b] = new AlbumText {
              Genre            = genre,
              GenreDescription = genreDescription,
              Identification   = ident,
              Title            = title,
              Performer        = performer,
              Lyricist         = lyricist,
              Composer         = composer,
              Arranger         = arranger,
              Message          = message,
              ProductCode      = code,
            };
          }
        }
        var delta = si.FirstTrack - this.FirstTrack;
        for (var t = 0; t < tracks; ++t) {
          var idx = 0;
          if (t + delta < 0)
            continue;
          if (t + delta >= trackText.Length)
            break;
          idx = t + (albumTitle     ? 1 : 0); var title     = (titles    ?.Length > idx) ? titles    [idx] : null;
          idx = t + (albumPerformer ? 1 : 0); var performer = (performers?.Length > idx) ? performers[idx] : null;
          idx = t + (albumLyricist  ? 1 : 0); var lyricist  = (lyricists ?.Length > idx) ? lyricists [idx] : null;
          idx = t + (albumComposer  ? 1 : 0); var composer  = (composers ?.Length > idx) ? composers [idx] : null;
          idx = t + (albumArranger  ? 1 : 0); var arranger  = (arrangers ?.Length > idx) ? arrangers [idx] : null;
          idx = t + (albumMessage   ? 1 : 0); var message   = (messages  ?.Length > idx) ? messages  [idx] : null;
          idx = t + (albumCode      ? 1 : 0); var code      = (codes     ?.Length > idx) ? codes     [idx] : null;
          if (title != null || performer != null || lyricist != null || composer != null || arranger != null || message != null || code != null) {
            trackText[t][b] = new TrackText {
              Title     = title,
              Performer = performer,
              Lyricist  = lyricist,
              Composer  = composer,
              Arranger  = arranger,
              Message   = message,
              Isrc      = code,
            };
          }
        }
      }
    }

    private string CalculateDiscId() {
      var sb = new StringBuilder(804);
      sb.Append(this.FirstTrack.ToString("X2"));
      sb.Append(this.LastTrack .ToString("X2"));
      for (var i = 0; i < 100; ++i) {
        if (i <= this.LastTrack)
          sb.Append(this._tracks[i].Address.ToString("X8"));
        else
          sb.Append("00000000");
      }
      using var sha1 = SHA1.Create();
      return Convert.ToBase64String(sha1.ComputeHash(Encoding.ASCII.GetBytes(sb.ToString()))).Replace('/', '_').Replace('+', '.').Replace('=', '-');
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
      var t = this._tracks[0].Address / 75 - this._tracks[1].Address / 75;
      return ((n % 0xff) << 24 | t << 8 | this.LastTrack).ToString("x8");
    }

    private Uri ConstructSubmissionUrl() {
      var uri = new UriBuilder(this.UrlScheme, this.WebSite, this.Port, "cdtoc/attach", null);
      var query = new StringBuilder();
      query.Append("id=").Append(Uri.EscapeDataString(this.DiscId));
      var culture = Thread.CurrentThread.CurrentCulture;
      Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; // Avoids having to explicitly run Uri.EscapeDataString on all the integers
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

    private static string[]? TextValue(RedBook.CDTextContentType type, List<byte> data, Encoding encoding, int items) {
      if (data == null || data.Count == 0)
        return null;
      var parts = encoding.GetString(data.ToArray()).Split(new [] { '\0' }, items);
      if (parts.Length == items)
        parts[items - 1] = parts[items - 1].TrimEnd('\0');
      if (parts.Length < items)
        Trace.WriteLine($"Not enough values provided in the {type} packs (expected {items}, got {parts.Length}).", "CD-TEXT");
      for (var i = 0; i < parts.Length; ++i) {
        if (parts[i] == "\t") { // TAB means "same as preceding value"
          if (i == 0)
            Trace.WriteLine($"Found a TAB in the first value of the {type} packs.", "CD-TEXT");
          else
            parts[i] = parts[i - 1];
        }
      }
      return parts;
    }

    #endregion

  }

}
