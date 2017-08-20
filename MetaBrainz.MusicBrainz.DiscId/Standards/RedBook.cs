using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;

namespace MetaBrainz.MusicBrainz.DiscId.Standards {

  /// <summary>Static class containing structures, enumerations and constants for the "Red Book" (CD-DA) standard.</summary>
  /// <remarks>Based on the IEC 60908:1999 standard document.</remarks>
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
  internal static class RedBook {

    #region Enumerations

    /// <summary>Enumeration of possible encodings for CD-TEXT data (values other than 0 for ID1 0x80-0x85 only).</summary>
    public enum CDTextCharacterCode : byte {

      ISO_8859_1      = 0x00, // modified, see CD-EXTRA specification, appendix 1
      ISO_646         = 0x01, // 7-bit ASCII
      // 0x02-0x7f: Reserved
      MusicShiftJis   = 0x80, // Kanji (RIAJ RS506 - could not find the document; apparently Shift-Jis plus many emoji)
      Korean          = 0x81, // Encoding to be defined
      MandarinChinese = 0x82, // Encoding to be defined
      // 0x83-0xff: Reserved

    }

    /// <summary>The type of information stored in a CD-TEXT &quot;pack&quot; (<see cref="CDTextPack"/>).</summary>
    public enum CDTextContentType : byte {

      Nothing   = 0x00,
      Title     = 0x80, // Album Title with ID2=0, Track Title otherwise
      Performer = 0x81,
      Lyricist  = 0x82,
      Composer  = 0x83,
      Arranger  = 0x84,
      Messages  = 0x85,
      DiscID    = 0x86,
      Genre     = 0x87,
      TOCInfo   = 0x88,
      TOCInfo2  = 0x89,
      // 0x8a, 0x8b, 0x8c: Reserved
      Internal  = 0x8d, // Closed Information (for internal use by content provider)
      Code      = 0x8e, // UPC/EAN with ID2=0, ISRC otherwise
      SizeInfo  = 0x8f,

    }

    /// <summary>The flag values for the CD-TEXT size info data.</summary>
    [Flags]
    public enum CDTextSizeInfoFlags : byte {

      // The flags byte encodes the following information:
      //   1....... Mode 2 Flag (indicates the presence of mode-2 CD-TEXT data packets)
      //   .1...... Program Area Copy Protection (indicates the presence of information in the program area about the copyright assertion of specific items)
      //            => Set to 0 if the mode-2 flag is 0. If not set, copyright should be assumed to be asserted for all CD-TEXT data.
      //   ..111... Reserved (0)
      //   .....1.. Copyright is asserted for messages (content type 0x85) in this block.
      //   ......1. Copyright is asserted for artist names (content type 0x81-0x84) in this block.
      //   .......1 Copyright is asserted for titles (content type 0x80) in this block.

      Mode2DataPresent          = 0x80,
      ProgramAreaCopyProtection = 0x40,
      Reserved1                 = 0x20,
      Reserved2                 = 0x10,
      Reserved3                 = 0x08,
      CopyrightedMessages       = 0x04,
      CopyrightedArtists        = 0x02,
      CopyrightedTitles         = 0x01,
      None                      = 0x00,

    }

    #endregion

    #region Structures

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CDTextGroup {

      // From the Red Book standard:
      // - A set of text info representing is called a BLOCK; a BLOCK can consist of up to 256 PACKs.
      // - Up to 8 BLOCKs can be combined into a text group.
      // - The size of a text group is recommended to be less than 512 PACKs, and shall be at maximum 2048 PACKs.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2048)]
      public CDTextPack[] Packs;

      public void FixUp(int size) {
        // Fix up the items too, reducing the array to the actual size.
        var packcount = size / Util.SizeOfStructure<CDTextPack>();
        if (packcount < 0 || packcount > 2048)
          throw new InvalidOperationException($"Invalid pack count ({packcount}) for CD-TEXT text group; should be between 0 and 2048.");
        if (packcount == 0) {
          this.Packs = null;
          return;
        }
        var packs = new CDTextPack[packcount];
        for (var i = 0; i < packcount; ++i) {
          packs[i] = this.Packs[i];
          packs[i].FixUp();
        }
        this.Packs = packs;
      }

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CDTextPack {

      // From the Red Book standard:
      // The header consists of 4 indicator (ID) bytes:
      // - ID1 (Pack Type)
      //   => see CDTextContentType for values
      //   => content appears in ascending order of this byte (within each block)
      // - ID2
      //   => most significant bit indicates the pack is for a to-be-defined extended application
      //   => rest forms the track number for the first character in the pack; 0 indicates disc-level information
      //      => or, for content types not tied to tracks, this value is taken as "pack element number"
      // - ID3
      //   => sequence number of this pack in its containing block
      // - ID4
      //   => most significant bit indicates DBCS content
      //      => applies to content types 0x80-0x85 only, but if used in a block, all its packs should have this bit set
      //   => next three bits are the block number
      //   => final four bits are the character position
      //      => position (in characters, not bytes) within the string (0-15, with 15 to be used for all values > 15)
      //      => not used (and should be 0) in content types 0x88, 0x89 and 0x8f

      public byte ID1;
      public byte ID2;
      public byte ID3;
      public byte ID4;

      // From the Red Book standard:
      // - For content types 0x88, 0x89 and 0x8f, this is pure binary data. For all others, this (also) contains text.
      // - In the case of content types 0x80-0x85 and 0x8e, this should provide one string per track.
      //   Multiple strings are separated by a NUL byte (or 2 NUL bytes for DBCS); strings can be empty.
      // - A string is recommended to be 160 characters or less.
      // - Strings exceeding the size of a pack (and at just 12 or 6 characters, that is likely), continue into the next one (with the same ID1).
      // - Unused bytes at the end of a sequence with the same ID1 are set to NULL.
      // - If the same value is used for multiple tracks, the tab character (0x09, or 0x0909 for DBCS) can be used as string to indicate "same as previous".
      //   This is only applicable to content types 0x80-0x85, and should not be used for the first entry, nor for a track where the previous value was empty.
      // - Specific Information per Content Type:
      //   - 0x80-0x85: Text as described above; if ID2 is 0, this indicates the presence of album-level information.
      //   - 0x86     : "Disc ID" information such as catalog number, name of the record company, point of sale code, etc.
      //                Different pieces of information are to be separated by a slash. ID2 should be 0.
      //   - 0x87     : Genre information, encoded as 2 bytes (MSB first) in the first two bytes of the text data. The supplementary description of the genre may be appended.
      //   - 0x88     : TOC information; copy of what is stored in the Q channel of the lead-in.
      //                For ID2 = 0, this is
      //                  byte FirstTrack;
      //                  byte LastTrack;
      //                  byte Reserved;
      //                  MSF  LeadOut;
      //                  (rest reserved)
      //                For ID2 <> 0, this is a sequence of pointers to tracks (4 per pack: M,S,F; unused pointers set to 0x000000).
      //                For these, ID2 indicates the track number of the first such pointer in the pack.
      //    - 0x89    : Second TOC. This indicates intervals in the program area. These may start and end in different tracks.
      //                One pack per interval, including a priority code. The priority code provides a sequence for the intervals (starting from 1).
      //                ID2 will still contain the applicable track number (presumably the one containing the start position).
      //                  byte PriorityNumber;
      //                  byte IntervalCount;
      //                  byte Reserved[4];
      //                  MSF  Start;
      //                  MSF  End;
      //    - 0x8D    : Closed information. Strings for whole disc plus the tracks. Not to be shown nor read by players available to the public.
      //    - 0x8E    : UPC/EAN to be stored (typically as 13 characters) for track 0; ISRC to be stored (typically as 12 characters) for each track.
      //    - 0x8F    : Size information (mainly for validation purposes); spread across 3 packs. See CDTextSizeInfo for the layout.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
      public byte[]            Data;

      // From the Red Book standard:
      // A cyclic redundancy check field consists of a 2-byte field, MSB first.
      // The CRC polynomial is X^16 + X^12 + X^5 + 1. All bits shall be inverted.
      public ushort            CRC;

      public CDTextContentType Type              => (CDTextContentType)   this.ID1;
      public bool              IsExtension       =>                      (this.ID2 & 0x80) == 0x80;
      public byte              TrackNumber       => (byte)               (this.ID2 & 0x7f);
      public byte              SequenceNumber    =>                       this.ID3;
      public bool              IsUnicode         =>                      (this.ID4 & 0x80) == 0x80;
      public byte              BlockNumber       => (byte)              ((this.ID4 & 0x70) >> 4);
      public byte              CharacterPosition => (byte)               (this.ID4 & 0x0f);

      public bool IsValid => this.CRC == this.CalculateChecksum();

      private ushort CalculateChecksum() {
        const int polynomial = 0x1021; // X^16 + X^12 + X^5 + 1 = (1)0001000000100001 = 0x1021
        const int n = 16;
        var remainder = 0;
        var data = new byte[16];
        data[0] = this.ID1;
        data[1] = this.ID2;
        data[2] = this.ID3;
        data[3] = this.ID4;
        Array.Copy(this.Data, 0, data, 4, 12);
        foreach (var b in data) {
          remainder = remainder ^ (((int) b) << (n - 8));
          for (var j = 1; j <= 8; ++j) {
            if ((remainder & 0x8000) != 0)
              remainder = (remainder << 1) ^ polynomial;
            else
              remainder <<= 1;
          }
          remainder &= 0xffff;
        }
        return (ushort) ~remainder;
      }

      public void FixUp() { this.CRC = (ushort) IPAddress.NetworkToHostOrder((short) this.CRC); }

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CDTextSizeInfo {

      // From the Red Book standard:
      // - The character code defines the encoding for packs in the block this size info belongs to.
      // - Applies to content types 0x80-0x85 only; any other characters are always in the modified ISO-8859-1 encoding.
      // - Blocks with ISO-8859-1 or ASCII as code should precede those with higher code values.

      public CDTextCharacterCode CharacterCode;

      public byte FirstTrack;
      public byte LastTrack;

      public CDTextSizeInfoFlags Flags;

      public byte PacksWithType80;
      public byte PacksWithType81;
      public byte PacksWithType82;
      public byte PacksWithType83;
      public byte PacksWithType84;
      public byte PacksWithType85;
      public byte PacksWithType86;
      public byte PacksWithType87;
      public byte PacksWithType88;
      public byte PacksWithType89;
      public byte PacksWithType8A;
      public byte PacksWithType8B;
      public byte PacksWithType8C;
      public byte PacksWithType8D;
      public byte PacksWithType8E;
      public byte PacksWithType8F;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
      public byte[] LastSequenceNumber; // for blocks 0-7; 0 if there is no such block

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
      public EBU.LanguageCode[] LanguageCode; // for blocks 0-7; language code is encoded as specified in annex 1 to part 5 of EBU Tech 3258-E.

    }

    #endregion

  }

}
