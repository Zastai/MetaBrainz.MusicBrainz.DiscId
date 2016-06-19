using System.Diagnostics.CodeAnalysis;

namespace MetaBrainz.MusicBrainz.DiscId.Standards {

  /// <summary>Static class containing structures, enumerations and constants for the "Blue Book" standard.</summary>
  /// <remarks>Not based on any actual standards document (because I could not find a copy).</remarks>
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  internal static class BlueBook {

    #region Enumerations

    /// <summary>Enumeration of possible genres.</summary>
    /// <remarks>Supposedly these are described in section III.3.2.5.3.8 of the spec.</remarks>
    public enum Genre : ushort {

      Unspecified           = 0x0000,
      Unknown               = 0x0001,
      AdultContemporary     = 0x0002,
      AlternativeRock       = 0x0003,
      Childrens             = 0x0004,
      Classical             = 0x0005,
      ContemporaryChristian = 0x0006,
      Country               = 0x0007,
      Dance                 = 0x0008,
      EasyListening         = 0x0009,
      Erotic                = 0x000a,
      Folk                  = 0x000b,
      Gospel                = 0x000c,
      HipHop                = 0x000d,
      Jazz                  = 0x000e,
      Latin                 = 0x000f,
      Musical               = 0x0010,
      NewAge                = 0x0011,
      Opera                 = 0x0012,
      Operetta              = 0x0013,
      Pop                   = 0x0014,
      Rap                   = 0x0015,
      Reggae                = 0x0016,
      Rock                  = 0x0017,
      RhythmAndBlues        = 0x0018,
      SoundEffects          = 0x0019,
      Soundtrack            = 0x001a,
      SpokenWord            = 0x001b,
      WorldMusic            = 0x001c,

    }

    #endregion

    #region Structures

    #endregion

  }

}
