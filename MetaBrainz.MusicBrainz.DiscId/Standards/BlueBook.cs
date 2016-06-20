using System.Diagnostics.CodeAnalysis;

namespace MetaBrainz.MusicBrainz.DiscId.Standards {

  /// <summary>Static class containing structures, enumerations and constants for the "Blue Book" standard.</summary>
  /// <remarks>Not based on any actual standards document (because I could not find a copy).</remarks>
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  public static class BlueBook {

    /// <summary>Enumeration of the genres defined by the "Blue Book" standard (section III.3.2.5.3.8).</summary>
    public enum Genre : ushort {

      /// <summary>No Genre Specified.</summary>
      Unspecified           = 0x0000,

      /// <summary>Unknown Genre.</summary>
      Unknown               = 0x0001,

      /// <summary>Adult Contemporary.</summary>
      AdultContemporary     = 0x0002,

      /// <summary>Alternative Rock.</summary>
      AlternativeRock       = 0x0003,

      /// <summary>Childrens.</summary>
      Childrens             = 0x0004,

      /// <summary>Classical.</summary>
      Classical             = 0x0005,

      /// <summary>Contemporary Christian.</summary>
      ContemporaryChristian = 0x0006,

      /// <summary>Country.</summary>
      Country               = 0x0007,

      /// <summary>Dance.</summary>
      Dance                 = 0x0008,

      /// <summary>Easy Listening.</summary>
      EasyListening         = 0x0009,

      /// <summary>Erotic.</summary>
      Erotic                = 0x000a,

      /// <summary>Folk.</summary>
      Folk                  = 0x000b,

      /// <summary>Gospel.</summary>
      Gospel                = 0x000c,

      /// <summary>Hip-Hop.</summary>
      HipHop                = 0x000d,

      /// <summary>Jazz.</summary>
      Jazz                  = 0x000e,

      /// <summary>Latin.</summary>
      Latin                 = 0x000f,

      /// <summary>Musical.</summary>
      Musical               = 0x0010,

      /// <summary>New Age.</summary>
      NewAge                = 0x0011,

      /// <summary>Opera.</summary>
      Opera                 = 0x0012,

      /// <summary>Operetta.</summary>
      Operetta              = 0x0013,

      /// <summary>Pop.</summary>
      Pop                   = 0x0014,

      /// <summary>Rap.</summary>
      Rap                   = 0x0015,

      /// <summary>Reggae.</summary>
      Reggae                = 0x0016,

      /// <summary>Rock.</summary>
      Rock                  = 0x0017,

      /// <summary>Rhythm and Blues.</summary>
      RhythmAndBlues        = 0x0018,

      /// <summary>Sound Effects.</summary>
      SoundEffects          = 0x0019,

      /// <summary>Soundtrack.</summary>
      Soundtrack            = 0x001a,

      /// <summary>Spoken Word.</summary>
      SpokenWord            = 0x001b,

      /// <summary>World Music.</summary>
      WorldMusic            = 0x001c,

    }

  }

}
