using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MetaBrainz.MusicBrainz.DiscId {

  /// <summary>Class representing a CD's table of contents.</summary>
  public sealed class TableOfContents {

    // FIXME: libdiscid (rather arbitrarily) uses 90 minutes. But overburning can go above 100 minutes. For now, use 99:59.74.
    /// <summary>The largest possible AudioCD (1 sector shy of 100 minutes).</summary>
    /// <remarks>This is only used for validation of user-supplied offsets (<see cref="CdDevice.SimulateDisc"/>).</remarks>
    public const int MaxSectors = 100 * 60 * 75 - 1;

    /// <summary>The distance between the last audio track and the first data track.</summary>
    public const int XAInterval = ((60 + 90 + 2) * 75);

    /// <summary>The name of the device from which this table of contents was read.</summary>
    public string DeviceName { get; }

    /// <summary>The first track on the disc (normally 1).</summary>
    public byte FirstTrack { get; }

    /// <summary>The last track on the disc.</summary>
    public byte LastTrack { get; }

    /// <summary>The media catalog number (typically the UPC/EAN) for the disc; null if not retrieved, empty if not available.</summary>
    public string MediaCatalogNumber { get; }

    /// <summary>Returns the MusicBrainz Disc ID associated with this table of contents.</summary>
    public string DiscId => this._discid ?? (this._discid = this.CalculateDiscId());

    /// <summary>Returns the FreeDB Disc ID associated with this table of contents.</summary>
    public string FreeDbId => this._freedbid ?? (this._freedbid = this.CalculateFreeDbId());

    /// <summary>The length, in sectors, of the disc (i.e. the starting sector of either the first data track or the lead-out).</summary>
    public int Sectors => this._tracks[0].Address;

    /// <summary>The URL to open to submit information about this table of contents to MusicBrainz.</summary>
    public Uri SubmissionUrl => this._url ?? (this._url = this.ConstructSubmissionUrl());

    /// <summary>Returns a string representing the TOC.</summary>
    /// <returns>
    ///   A string consisting of the following values, separated by a space: the first track number, the last track number,
    ///   the total length of the disc in sectors, and the offsets of all tracks.
    /// </returns>
    public override string ToString() {
      if (this._stringform != null)
        return this._stringform;
      var sb = new StringBuilder();
      this.AppendTocString(sb, ' ');
      return this._stringform = sb.ToString();
    }

    #region Tracks

    /// <summary>Class providing information about a single audio track on a cd-rom.</summary>
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
      }

      /// <summary>The length of this track expressed as a timespan.</summary>
      public TimeSpan Duration  { get; }

      /// <summary>The ISRC for the track. null if not retrieved, empty if not available.</summary>
      public string   Isrc      { get; }

      /// <summary>The length, in sectors, of this track.</summary>
      public int      Length    { get; }

      /// <summary>The track number.</summary>
      public byte     Number    { get; }

      /// <summary>The start position, in sectors, of this track.</summary>
      public int      Offset    { get; }

      /// <summary>The start position of this track expressed as a timespan.</summary>
      public TimeSpan StartTime { get; }

    }

    /// <summary>A collection of information about tracks on an audio cd.</summary>
    public sealed class AudioTrackCollection : IList<AudioTrack> {

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
        set { throw new NotSupportedException(); }
      }

      #region Enumerator

      private sealed class Enumerator : IEnumerator<AudioTrack> {

        public Enumerator(AudioTrackCollection collection) {
          this._collection = collection;
        }

        private readonly AudioTrackCollection _collection;
        private byte                     _index;

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

      #region Not Implemented

      /// <summary>Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.</summary>
      /// <returns>true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.</returns>
      /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
      public bool Contains(AudioTrack item) {
        throw new NotImplementedException();
      }

      /// <summary>Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.</summary>
      /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
      /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
      /// <exception cref="T:System.ArgumentNullException">
      /// <paramref name="array" /> is null.</exception>
      /// <exception cref="T:System.ArgumentOutOfRangeException">
      /// <paramref name="arrayIndex" /> is less than 0.</exception>
      /// <exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1" /> is greater than the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.</exception>
      public void CopyTo(AudioTrack[] array, int arrayIndex) {
        throw new NotImplementedException();
      }

      /// <summary>Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.</summary>
      /// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
      /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
      public int IndexOf(AudioTrack item) {
        throw new NotImplementedException();
      }

      #endregion

      #region Read-Only Collection

      /// <summary>Throws a <see cref="NotSupportedException" />, because a <see cref="AudioTrackCollection"/> is read-only.</summary>
      /// <param name="item">Ignored. No tracks can be added.</param>
      /// <exception cref="NotSupportedException">Always.</exception>
      public void Add(AudioTrack item) { throw new NotSupportedException(); }

      /// <summary>Throws a <see cref="NotSupportedException" />, because a <see cref="AudioTrackCollection"/> is read-only.</summary>
      /// <exception cref="NotSupportedException">Always.</exception>
      public void Clear() { throw new NotSupportedException(); }

      /// <summary>Throws a <see cref="NotSupportedException" />, because a <see cref="AudioTrackCollection"/> is read-only.</summary>
      /// <param name="index">Ignored. No tracks can be inserted.</param>
      /// <param name="item">Ignored. No tracks can be inserted.</param>
      /// <exception cref="NotSupportedException">Always.</exception>
      public void Insert(int index, AudioTrack item) { throw new NotSupportedException(); }

      /// <summary>Returns true, because a <see cref="AudioTrackCollection"/> is read-only.</summary>
      /// <returns>true.</returns>
      public bool IsReadOnly => true;

      /// <summary>Throws a <see cref="NotSupportedException" />, because a <see cref="AudioTrackCollection"/> is read-only.</summary>
      /// <param name="item">Ignored. No tracks can be removed.</param>
      /// <returns>Nothing; always throws a <see cref="NotSupportedException"/>.</returns>
      /// <exception cref="NotSupportedException">Always.</exception>
      public bool Remove(AudioTrack item) { throw new NotSupportedException(); }

      /// <summary>Throws a <see cref="NotSupportedException" />, because a <see cref="AudioTrackCollection"/> is read-only.</summary>
      /// <param name="index">Ignored. No tracks can be removed.</param>
      /// <exception cref="NotSupportedException">Always.</exception>
      public void RemoveAt(int index) { throw new NotSupportedException(); }

      #endregion

    }

    /// <summary>The tracks in this table of contents. Only indices between <see cref="FirstTrack"/> and <see cref="LastTrack"/> are valid.</summary>
    public AudioTrackCollection Tracks => new AudioTrackCollection(this);

    #endregion

    #region Constructors

    private readonly Track[] _tracks;

    private string _discid;

    private string _freedbid;

    private string _stringform;

    private Uri _url;

    private TableOfContents(string device, byte first, byte last) {
      if (first == 0 || first > 99) throw new ArgumentOutOfRangeException(nameof(first), first, "The first track number must be between 1 and 99.");
      if (last  == 0 || last  > 99) throw new ArgumentOutOfRangeException(nameof(last),  last,  "The last track number must be between 1 and 99.");
      if (last < first)             throw new ArgumentOutOfRangeException(nameof(last),  last,  "The last track number cannot be smaller than the first.");
      this.DeviceName = device;
      this.FirstTrack = first;
      this.LastTrack  = last;
    }

    internal TableOfContents(byte first, byte last, int[] offsets) : this(null, first, last) {
      // libdiscid wants last + 1 entries, even if first > 1. So we do the same.
      if (offsets.Length < last + 1)
        throw new ArgumentException(nameof(offsets), $"Not enough offsets provided (need at least {last + 1}).");
      if (offsets[0] > TableOfContents.MaxSectors)
        throw new ArgumentException(nameof(offsets), $"Disc is too large ({offsets[0]} > {TableOfContents.MaxSectors}).");
      this._tracks    = new Track[last + 1];
      this._tracks[0] = new Track(offsets[0]);
      for (byte i = 1; i <= last; ++i) {
        if (offsets[i] > offsets[0])
          throw new ArgumentException(nameof(offsets), $"Track offset #{i} points past the end of the disc.");
        if (i > 1 && offsets[i] < offsets[i - 1])
          throw new ArgumentException(nameof(offsets), $"Track offset #{i} points before the preceding track.");
        this._tracks[i] = new Track(offsets[i]);
      }
    }

    internal TableOfContents(string device, byte first, byte last, Track[] tracks, string mcn) : this(device, first, last) {
      if (tracks == null)
        throw new ArgumentNullException(nameof(tracks));
      if (tracks.Length < last + 1)
        throw new ArgumentException("Not enough track data given.", nameof(tracks));
      this._tracks = tracks;
      this.MediaCatalogNumber = mcn;
      this.LastTrack = 0;
      for (var i = first; i <= last; ++i) {
        if ((this._tracks[i].Control & MMC.SubChannelControl.ContentTypeMask) != MMC.SubChannelControl.Data)
          this.LastTrack = i;
      }
      if (this.LastTrack == 0)
        throw new NotSupportedException("No audio tracks found: CDROM, DVD or BD?");
      // If the last audio track is not the last track on the CD, use the offset of the next data track as the "lead-out" offset
      if (this.LastTrack < last)
        tracks[0] = new Track(tracks[this.LastTrack + 1].Address - TableOfContents.XAInterval);
      // As long as the lead-out isn't actually bigger than the position of the last track, the last track is invalid.
      // This happens on "copy-protected"/invalid discs. The track is then neither a valid audio track, nor data track.
      for (; this.LastTrack > 0 && tracks[0].Address < tracks[this.LastTrack].Address; --this.LastTrack)
        tracks[0] = new Track(tracks[this.LastTrack].Address - TableOfContents.XAInterval);
      if (this.LastTrack < this.FirstTrack)
        throw new NotSupportedException("Invalid TOC (no tracks remain): \"copy-protected\" disc?");
    }

    #endregion

    #region Internal Methods

    private void AppendTocString(StringBuilder sb, char delimiter) {
      sb.Append(this.FirstTrack).Append(delimiter).Append(this.LastTrack).Append(delimiter).Append(this._tracks[0].Address);
      for (var i = this.FirstTrack; i <= this.LastTrack; ++i)
        sb.Append(delimiter).Append(this._tracks[i].Address);
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
      using (var sha1 = SHA1.Create())
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
      var uri = new UriBuilder(CdDevice.DefaultUrlScheme, CdDevice.DefaultWebSite, CdDevice.DefaultPort, "cdtoc/attach", null);
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

    #endregion

  }

}
