using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId {

  internal class CdTextInfo {

    public abstract class CommonInfo {

      public readonly string Title;
      public readonly string Performer;
      public readonly string Lyricist;
      public readonly string Composer;
      public readonly string Arranger;
      public readonly string Message;
      public readonly string Code;

      internal CommonInfo(string title, string performer, string lyricist, string composer, string arranger, string message, string code) {
        this.Title     = title;
        this.Performer = performer;
        this.Lyricist  = lyricist;
        this.Composer  = composer;
        this.Arranger  = arranger;
        this.Message   = message;
        this.Code      = code;
      }

    }

    public sealed class AlbumInfo : CommonInfo {

      /// <remarks>Could also just expose this enum, but then it would need to have XML doc comments added.</remarks>
      private readonly BlueBook.Genre? _genre;
      public  readonly string          GenreDescription;
      public  readonly string          Identification;

      public ushort GenreCode => (ushort) this._genre.GetValueOrDefault();
      public string GenreName => this._genre?.ToString();


      internal AlbumInfo(BlueBook.Genre? genre, string genreDescription, string ident, string title, string performer, string lyricist, string composer, string arranger, string message, string code)
      : base(title, performer, lyricist, composer, arranger, message, code) {
        this._genre           = genre;
        this.GenreDescription = genreDescription;
        this.Identification   = ident;
      }

    }

    public sealed class TrackInfo : CommonInfo {

      internal TrackInfo(string title, string performer, string lyricist, string composer, string arranger, string message, string code)
      : base(title, performer, lyricist, composer, arranger, message, code) { }

    }

    public sealed class Block {

      /// <remarks>Could also just expose this enum, but then it would need to have XML doc comments added.</remarks>
      private readonly EBU.LanguageCode _language;
      public  readonly AlbumInfo        AlbumInfo;
      public  readonly TrackInfo[]      TrackInfo;

      public byte   LanguageCode => (byte) this._language;
      public string LanguageName => this._language.ToString();

      public Block(EBU.LanguageCode lc, byte tracks, BlueBook.Genre? genre, string genreDescription, string ident, string[] titles, bool albumTitle, string[] performers, bool albumPerformer, string[] lyricists, bool albumLyricist, string[] composers, bool albumComposer, string[] arrangers, bool albumArranger, string[] messages, bool albumMessage, string[] codes, bool albumCode) {
        this._language = lc;
        {
          var title     = (albumTitle     && titles    ?.Length > 0) ? titles    [0] : null;
          var performer = (albumPerformer && performers?.Length > 0) ? performers[0] : null;
          var lyricist  = (albumLyricist  && lyricists ?.Length > 0) ? lyricists [0] : null;
          var composer  = (albumComposer  && composers ?.Length > 0) ? composers [0] : null;
          var arranger  = (albumArranger  && arrangers ?.Length > 0) ? arrangers [0] : null;
          var message   = (albumMessage   && messages  ?.Length > 0) ? messages  [0] : null;
          var code      = (albumCode      && codes     ?.Length > 0) ? codes     [0] : null;
          if (genre.HasValue || title != null || performer != null || lyricist != null || composer != null || arranger != null || message != null || code != null)
            this.AlbumInfo = new AlbumInfo(genre, genreDescription, ident, title, performer, lyricist, composer, arranger, message, code);
        }
        this.TrackInfo = new TrackInfo[tracks];
        var idx = 0;
        for (var t = 0; t < tracks; ++t) {
          idx = t + (albumTitle     ? 1 : 0); var title     = (titles    ?.Length > idx) ? titles    [idx] : null;
          idx = t + (albumPerformer ? 1 : 0); var performer = (performers?.Length > idx) ? performers[idx] : null;
          idx = t + (albumLyricist  ? 1 : 0); var lyricist  = (lyricists ?.Length > idx) ? lyricists [idx] : null;
          idx = t + (albumComposer  ? 1 : 0); var composer  = (composers ?.Length > idx) ? composers [idx] : null;
          idx = t + (albumArranger  ? 1 : 0); var arranger  = (arrangers ?.Length > idx) ? arrangers [idx] : null;
          idx = t + (albumMessage   ? 1 : 0); var message   = (messages  ?.Length > idx) ? messages  [idx] : null;
          idx = t + (albumCode      ? 1 : 0); var code      = (codes     ?.Length > idx) ? codes     [idx] : null;
          if (title != null || performer != null || lyricist != null || composer != null || arranger != null || message != null || code != null)
            this.TrackInfo[t] = new TrackInfo(title, performer, lyricist, composer, arranger, message, code);
        }
      }

    }

    public readonly Block[] Blocks = null;

    public CdTextInfo(RedBook.CDTextGroup cdtext) {
      var packs = cdtext.Packs;
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
      this.Blocks = new Block[blockCount];
      var p = 0;
      for (var b = 0; b < blockCount; ++b) {
        var endpack        = si.LastSequenceNumber[b];
        var titles         = new List<byte>();
        var performers     = new List<byte>();
        var lyricists      = new List<byte>();
        var composers      = new List<byte>();
        var arrangers      = new List<byte>();
        var messages       = new List<byte>();
        var ident          = new List<byte>();
        var genreBytes     = new List<byte>();
        var codes          = new List<byte>();
        var sizeinfo       = new List<byte>();
        var albumTitle     = false;
        var albumPerformer = false;
        var albumLyricist  = false;
        var albumComposer  = false;
        var albumArranger  = false;
        var albumMessage   = false;
        var albumCode      = false;
        Trace.WriteLine($"Processing CD-TEXT block #{b + 1} (language: {si.LanguageCode[b]})...", "CD-TEXT");
        var dbcs = packs[p].IsUnicode;
        for (; p <= endpack; ++p) {
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
            case RedBook.CDTextContentType.Arranger:  arrangers     .AddRange(pack.Data); if (pack.ID2 == 0) albumArranger  = true; break;
            case RedBook.CDTextContentType.Code:      codes         .AddRange(pack.Data); if (pack.ID2 == 0) albumCode      = true; break;
            case RedBook.CDTextContentType.Composer:  composers     .AddRange(pack.Data); if (pack.ID2 == 0) albumComposer  = true; break;
            case RedBook.CDTextContentType.DiscID:    ident         .AddRange(pack.Data); break;
            case RedBook.CDTextContentType.Genre:     genreBytes    .AddRange(pack.Data); break;
            case RedBook.CDTextContentType.Lyricist:  lyricists     .AddRange(pack.Data); if (pack.ID2 == 0) albumLyricist  = true; break;
            case RedBook.CDTextContentType.Messages:  messages      .AddRange(pack.Data); if (pack.ID2 == 0) albumMessage   = true; break;
            case RedBook.CDTextContentType.Performer: performers    .AddRange(pack.Data); if (pack.ID2 == 0) albumPerformer = true; break;
            case RedBook.CDTextContentType.SizeInfo:  sizeinfo      .AddRange(pack.Data); break;
            case RedBook.CDTextContentType.Title:     titles        .AddRange(pack.Data); if (pack.ID2 == 0) albumTitle     = true; break;
            default:
              Trace.WriteLine($"Ignoring pack #{p + 1} because it is of ignored or unsupported type '{pack.Type}'.", "CD-TEXT");
              break;
          }
        }
        if (sizeinfo.Count == 36)
          si = Util.MarshalBytesToStructure<RedBook.CDTextSizeInfo>(sizeinfo.ToArray());
        else {
          Trace.WriteLine("Ignoring this block because it does not include size info.", "CD-TEXT");
          continue;
        }
        // FIXME: Any skipped packs above will cause these checks to fail.
        if (si.PacksWithType80 * 12 != titles    .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 80).", "CD-TEXT"); continue; }
        if (si.PacksWithType81 * 12 != performers.Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 81).", "CD-TEXT"); continue; }
        if (si.PacksWithType82 * 12 != lyricists .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 82).", "CD-TEXT"); continue; }
        if (si.PacksWithType83 * 12 != composers .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 83).", "CD-TEXT"); continue; }
        if (si.PacksWithType84 * 12 != arrangers .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 84).", "CD-TEXT"); continue; }
        if (si.PacksWithType85 * 12 != messages  .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 85).", "CD-TEXT"); continue; }
        if (si.PacksWithType86 * 12 != ident     .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 86).", "CD-TEXT"); continue; }
        if (si.PacksWithType87 * 12 != genreBytes.Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 87).", "CD-TEXT"); continue; }
        if (si.PacksWithType8E * 12 != codes     .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 8E).", "CD-TEXT"); continue; }
        if (si.PacksWithType8F * 12 != sizeinfo  .Count) { Trace.WriteLine("Ignoring this block because it fails validation (pack count, type 8F).", "CD-TEXT"); continue; }
        Encoding encoding = null;
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
        string genreDescription = null;
        if (genreBytes != null && genreBytes.Count > 0) {
          var rawgenre = genreBytes.ToArray();
          var code = BitConverter.ToInt16(rawgenre, 0);
          if (BitConverter.IsLittleEndian)
            code = IPAddress.NetworkToHostOrder(code);
          genre = (BlueBook.Genre) code;
          genreDescription = latin1.GetString(rawgenre, 2, rawgenre.Length - 2).TrimEnd('\0');
          if (genreDescription.Length == 0)
            genreDescription = null;
        }
        var tracks = (byte) (si.LastTrack - si.FirstTrack + 1);
        this.Blocks[b] = new Block(si.LanguageCode[b], tracks, genre, genreDescription, CdTextInfo.GetValue(ident, latin1),
                                   CdTextInfo.GetValue(RedBook.CDTextContentType.Title,     titles,     encoding, tracks + (albumTitle     ? 1 : 0)), albumTitle,
                                   CdTextInfo.GetValue(RedBook.CDTextContentType.Performer, performers, encoding, tracks + (albumPerformer ? 1 : 0)), albumPerformer,
                                   CdTextInfo.GetValue(RedBook.CDTextContentType.Lyricist,  lyricists,  encoding, tracks + (albumLyricist  ? 1 : 0)), albumLyricist,
                                   CdTextInfo.GetValue(RedBook.CDTextContentType.Composer,  composers,  encoding, tracks + (albumComposer  ? 1 : 0)), albumComposer,
                                   CdTextInfo.GetValue(RedBook.CDTextContentType.Arranger,  arrangers,  encoding, tracks + (albumArranger  ? 1 : 0)), albumArranger,
                                   CdTextInfo.GetValue(RedBook.CDTextContentType.Messages,  messages,   encoding, tracks + (albumMessage   ? 1 : 0)), albumMessage,
                                   CdTextInfo.GetValue(RedBook.CDTextContentType.Code,      codes,      latin1,   tracks + (albumCode      ? 1 : 0)), albumCode
                                   );
      }
    }

    private static string[] GetValue(RedBook.CDTextContentType type, List<byte> data, Encoding encoding, int items) {
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

    private static string GetValue(List<byte> data, Encoding encoding) {
      if (data == null || data.Count == 0)
        return null;
      return encoding.GetString(data.ToArray()).TrimEnd('\0');
    }

  }

}
