# API Reference: MetaBrainz.MusicBrainz.DiscId

## Assembly Attributes

```cs
[assembly: System.Runtime.InteropServices.ComVisibleAttribute(false)]
[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v8.0", FrameworkDisplayName = ".NET 8.0")]
```

## Namespace: MetaBrainz.MusicBrainz.DiscId

### Type: AlbumText

```cs
public sealed class AlbumText {

  string? Arranger {
    public get;
  }

  string? Composer {
    public get;
  }

  MetaBrainz.MusicBrainz.DiscId.Standards.BlueBook.Genre? Genre {
    public get;
  }

  string? GenreDescription {
    public get;
  }

  string? Identification {
    public get;
  }

  string? Lyricist {
    public get;
  }

  string? Message {
    public get;
  }

  string? Performer {
    public get;
  }

  string? ProductCode {
    public get;
  }

  string? Title {
    public get;
  }

  public AlbumText();

}
```

### Type: DiscReadFeature

```cs
[System.FlagsAttribute]
public enum DiscReadFeature {

  All = -1,
  CdText = 8,
  MediaCatalogNumber = 2,
  None = 0,
  TableOfContents = 1,
  TrackIsrc = 4,

}
```

### Type: ScsiException

```cs
[System.SerializableAttribute]
public class ScsiException : System.Runtime.InteropServices.ExternalException {

  byte AdditionalSenseCode {
    public get;
  }

  byte AdditionalSenseCodeQualifier {
    public get;
  }

  int ErrorCode {
    public override get;
  }

  string Message {
    public override get;
  }

  byte SenseKey {
    public get;
  }

  public ScsiException(byte sk, byte asc, byte ascq);

}
```

### Type: TableOfContents

```cs
public sealed class TableOfContents {

  public const int MaxSectors = 449999;

  public const int XAInterval = 11400;

  System.Collections.Generic.IEnumerable<string> AvailableDevices {
    public static get;
  }

  DiscReadFeature AvailableFeatures {
    public static get;
  }

  string? DefaultDevice {
    public static get;
  }

  int DefaultPort {
    public static get;
    public static set;
  }

  string DefaultUrlScheme {
    public static get;
    public static set;
  }

  string DefaultWebSite {
    public static get;
    public static set;
  }

  string? DeviceName {
    public get;
  }

  string DiscId {
    public get;
  }

  byte FirstTrack {
    public get;
  }

  string FreeDbId {
    public get;
  }

  byte LastTrack {
    public get;
  }

  int Length {
    public get;
  }

  string? MediaCatalogNumber {
    public get;
  }

  int Port {
    public get;
    public set;
  }

  System.Uri SubmissionUrl {
    public get;
  }

  System.Collections.Generic.IReadOnlyList<AlbumText>? TextInfo {
    public get;
  }

  System.Collections.Generic.IReadOnlyList<MetaBrainz.MusicBrainz.DiscId.Standards.EBU.LanguageCode>? TextLanguages {
    public get;
  }

  AudioTrackCollection Tracks {
    public get;
  }

  string UrlScheme {
    public get;
    public set;
  }

  string WebSite {
    public get;
    public set;
  }

  public static bool HasReadFeature(DiscReadFeature feature);

  public static TableOfContents ReadDisc(string? device, DiscReadFeature features = DiscReadFeature.All | DiscReadFeature.CdText | DiscReadFeature.MediaCatalogNumber | DiscReadFeature.None | DiscReadFeature.TableOfContents | DiscReadFeature.TrackIsrc);

  public static TableOfContents SimulateDisc(byte first, byte last, int[] offsets);

  public override string ToString();

  public sealed class AudioTrack {

    System.TimeSpan Duration {
      public get;
    }

    string? Isrc {
      public get;
    }

    int Length {
      public get;
    }

    byte Number {
      public get;
    }

    int Offset {
      public get;
    }

    System.TimeSpan StartTime {
      public get;
    }

    System.Collections.Generic.IReadOnlyList<TrackText>? TextInfo {
      public get;
    }

  }

  [System.Reflection.DefaultMemberAttribute("Item")]
  public sealed class AudioTrackCollection : System.Collections.Generic.IEnumerable<AudioTrack>, System.Collections.Generic.IReadOnlyCollection<AudioTrack>, System.Collections.Generic.IReadOnlyList<AudioTrack>, System.Collections.IEnumerable {

    AudioTrack this[int number] {
      public sealed override get;
      public set;
    }

    int Count {
      public sealed override get;
    }

    byte FirstTrack {
      public get;
    }

    byte LastTrack {
      public get;
    }

    public sealed override System.Collections.Generic.IEnumerator<AudioTrack> GetEnumerator();

  }

}
```

### Type: TrackText

```cs
public sealed class TrackText {

  string? Arranger {
    public get;
  }

  string? Composer {
    public get;
  }

  string? Isrc {
    public get;
  }

  string? Lyricist {
    public get;
  }

  string? Message {
    public get;
  }

  string? Performer {
    public get;
  }

  string? Title {
    public get;
  }

  public TrackText();

}
```

## Namespace: MetaBrainz.MusicBrainz.DiscId.Platforms

### Type: UnixException

```cs
[System.SerializableAttribute]
public class UnixException : System.Runtime.InteropServices.ExternalException {

  public UnixException();

  public UnixException(int errno);

}
```

## Namespace: MetaBrainz.MusicBrainz.DiscId.Standards

### Type: BlueBook

```cs
public static class BlueBook {

  public enum Genre : short {

    AdultContemporary = (short) 2,
    AlternativeRock = (short) 3,
    Childrens = (short) 4,
    Classical = (short) 5,
    ContemporaryChristian = (short) 6,
    Country = (short) 7,
    Dance = (short) 8,
    EasyListening = (short) 9,
    Erotic = (short) 10,
    Folk = (short) 11,
    Gospel = (short) 12,
    HipHop = (short) 13,
    Jazz = (short) 14,
    Latin = (short) 15,
    Musical = (short) 16,
    NewAge = (short) 17,
    Opera = (short) 18,
    Operetta = (short) 19,
    Pop = (short) 20,
    Rap = (short) 21,
    Reggae = (short) 22,
    RhythmAndBlues = (short) 24,
    Rock = (short) 23,
    SoundEffects = (short) 25,
    Soundtrack = (short) 26,
    SpokenWord = (short) 27,
    Unknown = (short) 1,
    Unspecified = (short) 0,
    WorldMusic = (short) 28,

  }

}
```

### Type: EBU

```cs
public static class EBU {

  public enum LanguageCode : byte {

    Albanian = (byte) 1,
    Amharic = (byte) 127,
    Arabic = (byte) 126,
    Armenian = (byte) 125,
    Assamese = (byte) 124,
    Azerbaijani = (byte) 123,
    Bambora = (byte) 122,
    Basque = (byte) 13,
    Bengali = (byte) 120,
    Bielorussian = (byte) 121,
    Breton = (byte) 2,
    Bulgarian = (byte) 119,
    Burmese = (byte) 118,
    Catalan = (byte) 3,
    Chinese = (byte) 117,
    Churash = (byte) 116,
    Croatian = (byte) 4,
    Czech = (byte) 6,
    Danish = (byte) 7,
    Dari = (byte) 115,
    Dutch = (byte) 29,
    English = (byte) 9,
    Esperanto = (byte) 11,
    Estonian = (byte) 12,
    Faroese = (byte) 14,
    Finnish = (byte) 39,
    Flemish = (byte) 42,
    French = (byte) 15,
    Frisian = (byte) 16,
    Fulani = (byte) 114,
    Gaelic = (byte) 18,
    Galician = (byte) 19,
    Georgian = (byte) 113,
    German = (byte) 8,
    Greek = (byte) 112,
    Gujurati = (byte) 111,
    Gurani = (byte) 110,
    Hausa = (byte) 109,
    Hebrew = (byte) 108,
    Hindi = (byte) 107,
    Hungarian = (byte) 27,
    Icelandic = (byte) 20,
    Indonesian = (byte) 106,
    Irish = (byte) 17,
    Italian = (byte) 21,
    Japanese = (byte) 105,
    Kannada = (byte) 104,
    Kazakh = (byte) 103,
    Khmer = (byte) 102,
    Korean = (byte) 101,
    Laotian = (byte) 100,
    Lappish = (byte) 22,
    Latin = (byte) 23,
    Latvian = (byte) 24,
    Lithuanian = (byte) 26,
    Luxembourgian = (byte) 25,
    Macedonian = (byte) 99,
    Malagasay = (byte) 98,
    Malaysian = (byte) 97,
    Maltese = (byte) 28,
    Marathi = (byte) 95,
    Moldavian = (byte) 96,
    Ndebele = (byte) 94,
    Nepali = (byte) 93,
    Norwegian = (byte) 30,
    Occitan = (byte) 31,
    Oriya = (byte) 92,
    Papamiento = (byte) 91,
    Persian = (byte) 90,
    Polish = (byte) 32,
    Portuguese = (byte) 33,
    Punjabi = (byte) 89,
    Pushtu = (byte) 88,
    Quechua = (byte) 87,
    Romanian = (byte) 34,
    Romansh = (byte) 35,
    Russian = (byte) 86,
    Ruthenian = (byte) 85,
    Serbian = (byte) 36,
    SerboCroat = (byte) 84,
    Shona = (byte) 83,
    Sinhalese = (byte) 82,
    Slovak = (byte) 37,
    Slovenian = (byte) 38,
    Somali = (byte) 81,
    Spanish = (byte) 10,
    SrananTongo = (byte) 80,
    Swahili = (byte) 79,
    Swedish = (byte) 40,
    Tadzhik = (byte) 78,
    Tamil = (byte) 77,
    Tatar = (byte) 76,
    Telugu = (byte) 75,
    Thai = (byte) 74,
    Turkish = (byte) 41,
    Ukrainian = (byte) 73,
    Unknown = (byte) 0,
    Urdu = (byte) 72,
    Uzbek = (byte) 71,
    Vietnamese = (byte) 70,
    Wallon = (byte) 43,
    Welsh = (byte) 5,
    Zulu = (byte) 69,

  }

}
```
