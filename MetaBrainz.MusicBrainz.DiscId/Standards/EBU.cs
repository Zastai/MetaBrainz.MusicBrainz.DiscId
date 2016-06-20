using System.Diagnostics.CodeAnalysis;

namespace MetaBrainz.MusicBrainz.DiscId.Standards {

  /// <summary>Static class containing structures, enumerations and constants from EBU standards.</summary>
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  public static class EBU {

    /// <summary>Enumeration of EBU Language Codes.</summary>
    /// <remarks>Originally defined in EBU Tech 3258-E. These are taken from 3264-E instead, because I could not find the original standard document.</remarks>
    public enum LanguageCode : byte {

      /// <summary>Language not known or not specified.</summary>
      Unknown       = 0x00,

      #region European Languages With Latin-Style Alphabets

      /// <summary>Albanian.</summary>
      Albanian      = 0x01,

      /// <summary>Breton.</summary>
      Breton        = 0x02,

      /// <summary>Catalan.</summary>
      Catalan       = 0x03,

      /// <summary>Croatian.</summary>
      Croatian      = 0x04,

      /// <summary>Welsh.</summary>
      Welsh         = 0x05,

      /// <summary>Czech.</summary>
      Czech         = 0x06,

      /// <summary>Danish.</summary>
      Danish        = 0x07,

      /// <summary>German.</summary>
      German        = 0x08,

      /// <summary>English.</summary>
      English       = 0x09,

      /// <summary>Spanish.</summary>
      Spanish       = 0x0A,

      /// <summary>Esperanto.</summary>
      Esperanto     = 0x0B,

      /// <summary>Estonian.</summary>
      Estonian      = 0x0C,

      /// <summary>Basque.</summary>
      Basque        = 0x0D,

      /// <summary>Faroese.</summary>
      Faroese       = 0x0E,

      /// <summary>French.</summary>
      French        = 0x0F,

      /// <summary>Frisian.</summary>
      Frisian       = 0x10,

      /// <summary>Irish.</summary>
      Irish         = 0x11,

      /// <summary>Gaelic.</summary>
      Gaelic        = 0x12,

      /// <summary>Galician.</summary>
      Galician      = 0x13,

      /// <summary>Icelandic.</summary>
      Icelandic     = 0x14,

      /// <summary>Italian.</summary>
      Italian       = 0x15,

      /// <summary>Lappish.</summary>
      Lappish       = 0x16,

      /// <summary>Latin.</summary>
      Latin         = 0x17,

      /// <summary>Latvian.</summary>
      Latvian       = 0x18,

      /// <summary>Luxembourgian.</summary>
      Luxembourgian = 0x19,

      /// <summary>Lithuanian.</summary>
      Lithuanian    = 0x1A,

      /// <summary>Hungarian.</summary>
      Hungarian     = 0x1B,

      /// <summary>Maltese.</summary>
      Maltese       = 0x1C,

      /// <summary>Dutch.</summary>
      Dutch         = 0x1D,

      /// <summary>Norwegian.</summary>
      Norwegian     = 0x1E,

      /// <summary>Occitan.</summary>
      Occitan       = 0x1F,

      /// <summary>Polish.</summary>
      Polish        = 0x20,

      /// <summary>Portugese.</summary>
      Portugese     = 0x21,

      /// <summary>Romanian.</summary>
      Romanian      = 0x22,

      /// <summary>Romansh.</summary>
      Romansh       = 0x23,

      /// <summary>Serbian.</summary>
      Serbian       = 0x24,

      /// <summary>Slovak.</summary>
      Slovak        = 0x25,

      /// <summary>Slovenian.</summary>
      Slovenian     = 0x26,

      /// <summary>Finnish.</summary>
      Finnish       = 0x27,

      /// <summary>Swedish.</summary>
      Swedish       = 0x28,

      /// <summary>Turkish.</summary>
      Turkish       = 0x29,

      /// <summary>Flemish.</summary>
      Flemish       = 0x2A,

      /// <summary>Wallon.</summary>
      Wallon        = 0x2B,

      #endregion

      // 0x2C-0x2F Not assigned
      // 0x30-0x3F Reserved for national assignment
      // 0x40-0x44 Not assigned

      #region Other languages

      /// <summary>Zulu.</summary>
      Zulu         = 0x45,

      /// <summary>Vietnamese.</summary>
      Vietnamese   = 0x46,

      /// <summary>Uzbek.</summary>
      Uzbek        = 0x47,

      /// <summary>Urdu.</summary>
      Urdu         = 0x48,

      /// <summary>Ukrainian.</summary>
      Ukrainian    = 0x49,

      /// <summary>Thai.</summary>
      Thai         = 0x4A,

      /// <summary>Telugu.</summary>
      Telugu       = 0x4B,

      /// <summary>Tatar.</summary>
      Tatar        = 0x4C,

      /// <summary>Tamil.</summary>
      Tamil        = 0x4D,

      /// <summary>Tadzhik.</summary>
      Tadzhik      = 0x4E,

      /// <summary>Swahili.</summary>
      Swahili      = 0x4F,

      /// <summary>Sranan Tongo.</summary>
      SrananTongo  = 0x50,

      /// <summary>Somali.</summary>
      Somali       = 0x51,

      /// <summary>Sinhalese.</summary>
      Sinhalese    = 0x52,

      /// <summary>Shona.</summary>
      Shona        = 0x53,

      /// <summary>Serbo-Croat.</summary>
      SerboCroat   = 0x54,

      /// <summary>Ruthenian.</summary>
      Ruthenian    = 0x55,

      /// <summary>Russian.</summary>
      Russian      = 0x56,

      /// <summary>Quechua.</summary>
      Quechua      = 0x57,

      /// <summary>Pushtu.</summary>
      Pushtu       = 0x58,

      /// <summary>Punjabi.</summary>
      Punjabi      = 0x59,

      /// <summary>Persian.</summary>
      Persian      = 0x5A,

      /// <summary>Papamiento.</summary>
      Papamiento   = 0x5B,

      /// <summary>Oriya.</summary>
      Oriya        = 0x5C,

      /// <summary>Nepali.</summary>
      Nepali       = 0x5D,

      /// <summary>Ndebele.</summary>
      Ndebele      = 0x5E,

      /// <summary>Marathi.</summary>
      Marathi      = 0x5F,

      /// <summary>Moldavian.</summary>
      Moldavian    = 0x60,

      /// <summary>Malaysian.</summary>
      Malaysian    = 0x61,

      /// <summary>Malagasay.</summary>
      Malagasay    = 0x62,

      /// <summary>Macedonian.</summary>
      Macedonian   = 0x63,

      /// <summary>Laotian.</summary>
      Laotian      = 0x64,

      /// <summary>Korean.</summary>
      Korean       = 0x65,

      /// <summary>Khmer.</summary>
      Khmer        = 0x66,

      /// <summary>Kazakh.</summary>
      Kazakh       = 0x67,

      /// <summary>Kannada.</summary>
      Kannada      = 0x68,

      /// <summary>Japanese.</summary>
      Japanese     = 0x69,

      /// <summary>Indonesian.</summary>
      Indonesian   = 0x6A,

      /// <summary>Hindi.</summary>
      Hindi        = 0x6B,

      /// <summary>Hebrew.</summary>
      Hebrew       = 0x6C,

      /// <summary>Hausa.</summary>
      Hausa        = 0x6D,

      /// <summary>Gurani.</summary>
      Gurani       = 0x6E,

      /// <summary>Gujurati.</summary>
      Gujurati     = 0x6F,

      /// <summary>Greek.</summary>
      Greek        = 0x70,

      /// <summary>Georgian.</summary>
      Georgian     = 0x71,

      /// <summary>Fulani.</summary>
      Fulani       = 0x72,

      /// <summary>Dari.</summary>
      Dari         = 0x73,

      /// <summary>Churash.</summary>
      Churash      = 0x74,

      /// <summary>Chinese.</summary>
      Chinese      = 0x75,

      /// <summary>Burmese.</summary>
      Burmese      = 0x76,

      /// <summary>Bulgarian.</summary>
      Bulgarian    = 0x77,

      /// <summary>Bengali.</summary>
      Bengali      = 0x78,

      /// <summary>Bielorussian.</summary>
      Bielorussian = 0x79,

      /// <summary>Bambora.</summary>
      Bambora      = 0x7A,

      /// <summary>Azerbaijani.</summary>
      Azerbaijani  = 0x7B,

      /// <summary>Assamese.</summary>
      Assamese     = 0x7C,

      /// <summary>Armenian.</summary>
      Armenian     = 0x7D,

      /// <summary>Arabic.</summary>
      Arabic       = 0x7E,

      /// <summary>Amharic.</summary>
      Amharic      = 0x7F,

      #endregion

    }

  }

}
