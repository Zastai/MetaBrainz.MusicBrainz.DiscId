using System.Diagnostics.CodeAnalysis;

namespace MetaBrainz.MusicBrainz.DiscId.Standards {

  /// <summary>Static class containing structures, enumerations and constants from EBU standards.</summary>
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  internal static class EBU {

    #region Enumerations

    /// <summary>Enumeration of EBU Language Codes</summary>
    /// <remarks>Originally defined in EBU Tech 3258-E. These are taken from 3264-E instead, because I could not find the original standard document.</remarks>
    public enum LanguageCode : byte {

      // European languages with Latin-style alphabets

      Unknown       = 0x00,
      Albanian      = 0x01,
      Breton        = 0x02,
      Catalan       = 0x03,
      Croatian      = 0x04,
      Welsh         = 0x05,
      Czech         = 0x06,
      Danish        = 0x07,
      German        = 0x08,
      English       = 0x09,
      Spanish       = 0x0A,
      Esperanto     = 0x0B,
      Estonian      = 0x0C,
      Basque        = 0x0D,
      Faroese       = 0x0E,
      French        = 0x0F,
      Frisian       = 0x10,
      Irish         = 0x11,
      Gaelic        = 0x12,
      Galician      = 0x13,
      Icelandic     = 0x14,
      Italian       = 0x15,
      Lappish       = 0x16,
      Latin         = 0x17,
      Latvian       = 0x18,
      Luxembourgian = 0x19,
      Lithuanian    = 0x1A,
      Hungarian     = 0x1B,
      Maltese       = 0x1C,
      Dutch         = 0x1D,
      Norwegian     = 0x1E,
      Occitan       = 0x1F,
      Polish        = 0x20,
      Portugese     = 0x21,
      Romanian      = 0x22,
      Romansh       = 0x23,
      Serbian       = 0x24,
      Slovak        = 0x25,
      Slovenian     = 0x26,
      Finnish       = 0x27,
      Swedish       = 0x28,
      Turkish       = 0x29,
      Flemish       = 0x2A,
      Wallon        = 0x2B,

      // 0x2C-0x2F Not assigned
      // 0x30-0x3F Reserved for national assignment
      // 0x40-0x44 Not assigned

      // Other languages

      Zulu         = 0x45,
      Vietnamese   = 0x46,
      Uzbek        = 0x47,
      Urdu         = 0x48,
      Ukrainian    = 0x49,
      Thai         = 0x4A,
      Telugu       = 0x4B,
      Tatar        = 0x4C,
      Tamil        = 0x4D,
      Tadzhik      = 0x4E,
      Swahili      = 0x4F,
      SrananTongo  = 0x50,
      Somali       = 0x51,
      Sinhalese    = 0x52,
      Shona        = 0x53,
      SerboCroat   = 0x54,
      Ruthenian    = 0x55,
      Russian      = 0x56,
      Quechua      = 0x57,
      Pushtu       = 0x58,
      Punjabi      = 0x59,
      Persian      = 0x5A,
      Papamiento   = 0x5B,
      Oriya        = 0x5C,
      Nepali       = 0x5D,
      Ndebele      = 0x5E,
      Marathi      = 0x5F,
      Moldavian    = 0x60,
      Malaysian    = 0x61,
      Malagasay    = 0x62,
      Macedonian   = 0x63,
      Laotian      = 0x64,
      Korean       = 0x65,
      Khmer        = 0x66,
      Kazakh       = 0x67,
      Kannada      = 0x68,
      Japanese     = 0x69,
      Indonesian   = 0x6A,
      Hindi        = 0x6B,
      Hebrew       = 0x6C,
      Hausa        = 0x6D,
      Gurani       = 0x6E,
      Gujurati     = 0x6F,
      Greek        = 0x70,
      Georgian     = 0x71,
      Fulani       = 0x72,
      Dari         = 0x73,
      Churash      = 0x74,
      Chinese      = 0x75,
      Burmese      = 0x76,
      Bulgarian    = 0x77,
      Bengali      = 0x78,
      Bielorussian = 0x79,
      Bambora      = 0x7A,
      Azerbaijani  = 0x7B,
      Assamese     = 0x7C,
      Armenian     = 0x7D,
      Arabic       = 0x7E,
      Amharic      = 0x7F,

    }

    #endregion

    #region Structures

    #endregion

  }

}
