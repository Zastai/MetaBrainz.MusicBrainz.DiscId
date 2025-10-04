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

  public static TableOfContents ReadDisc(string? device, DiscReadFeature features = DiscReadFeature.All);

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

    AdultContemporary = 2,
    AlternativeRock = 3,
    Childrens = 4,
    Classical = 5,
    ContemporaryChristian = 6,
    Country = 7,
    Dance = 8,
    EasyListening = 9,
    Erotic = 10,
    Folk = 11,
    Gospel = 12,
    HipHop = 13,
    Jazz = 14,
    Latin = 15,
    Musical = 16,
    NewAge = 17,
    Opera = 18,
    Operetta = 19,
    Pop = 20,
    Rap = 21,
    Reggae = 22,
    RhythmAndBlues = 24,
    Rock = 23,
    SoundEffects = 25,
    Soundtrack = 26,
    SpokenWord = 27,
    Unknown = 1,
    Unspecified = 0,
    WorldMusic = 28,

  }

}
```

### Type: EBU

```cs
public static class EBU {

  public enum LanguageCode : byte {

    Albanian = 1,
    Amharic = 127,
    Arabic = 126,
    Armenian = 125,
    Assamese = 124,
    Azerbaijani = 123,
    Bambora = 122,
    Basque = 13,
    Bengali = 120,
    Bielorussian = 121,
    Breton = 2,
    Bulgarian = 119,
    Burmese = 118,
    Catalan = 3,
    Chinese = 117,
    Churash = 116,
    Croatian = 4,
    Czech = 6,
    Danish = 7,
    Dari = 115,
    Dutch = 29,
    English = 9,
    Esperanto = 11,
    Estonian = 12,
    Faroese = 14,
    Finnish = 39,
    Flemish = 42,
    French = 15,
    Frisian = 16,
    Fulani = 114,
    Gaelic = 18,
    Galician = 19,
    Georgian = 113,
    German = 8,
    Greek = 112,
    Gujurati = 111,
    Gurani = 110,
    Hausa = 109,
    Hebrew = 108,
    Hindi = 107,
    Hungarian = 27,
    Icelandic = 20,
    Indonesian = 106,
    Irish = 17,
    Italian = 21,
    Japanese = 105,
    Kannada = 104,
    Kazakh = 103,
    Khmer = 102,
    Korean = 101,
    Laotian = 100,
    Lappish = 22,
    Latin = 23,
    Latvian = 24,
    Lithuanian = 26,
    Luxembourgian = 25,
    Macedonian = 99,
    Malagasay = 98,
    Malaysian = 97,
    Maltese = 28,
    Marathi = 95,
    Moldavian = 96,
    Ndebele = 94,
    Nepali = 93,
    Norwegian = 30,
    Occitan = 31,
    Oriya = 92,
    Papamiento = 91,
    Persian = 90,
    Polish = 32,
    Portuguese = 33,
    Punjabi = 89,
    Pushtu = 88,
    Quechua = 87,
    Romanian = 34,
    Romansh = 35,
    Russian = 86,
    Ruthenian = 85,
    Serbian = 36,
    SerboCroat = 84,
    Shona = 83,
    Sinhalese = 82,
    Slovak = 37,
    Slovenian = 38,
    Somali = 81,
    Spanish = 10,
    SrananTongo = 80,
    Swahili = 79,
    Swedish = 40,
    Tadzhik = 78,
    Tamil = 77,
    Tatar = 76,
    Telugu = 75,
    Thai = 74,
    Turkish = 41,
    Ukrainian = 73,
    Unknown = 0,
    Urdu = 72,
    Uzbek = 71,
    Vietnamese = 70,
    Wallon = 43,
    Welsh = 5,
    Zulu = 69,

  }

}
```
