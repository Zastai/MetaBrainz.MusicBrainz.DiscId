using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;

namespace MetaBrainz.MusicBrainz.DiscId.Standards {

  /// <summary>Static class containing structures, enumerations and constants for the SCSI MultiMedia Commands.</summary>
  /// <remarks>
  /// Based on the following (draft) standard documents:
  /// <list type="bullet">
  ///   <item><term>[MMC]  </term><description>X3T10 1048D revision 10a</description></item>
  ///   <item><term>[MMC-2]</term><description>NCITS 333 T10/1228-D revision 11a</description></item>
  ///   <item><term>[MMC-3]</term><description>NCITS T10/1363-D revision 10g</description></item>
  ///   <item><term>[MMC-4]</term><description>INCITS T10/1545-D revision 5a (note: this is the last version to include CD-specific commands like <c>READ SUB-CHANNEL</c>)</description></item>
  ///   <item><term>[MMC-5]</term><description>INCITS T10/1675D revision 4</description></item>
  ///   <item><term>[MMC-6]</term><description>INCITS T10/1836D revision 2g</description></item>
  /// </list>
  /// </remarks>
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
  internal static class MMC {

    #region Enumerations

    /// <summary>The current status of audio playback.</summary>
    public enum AudioStatus : byte {

      NotSupported = 0x00,
      InProgress   = 0x11,
      Paused       = 0x12,
      PlayComplete = 0x13,
      PlayError    = 0x14,
      NoStatus     = 0x15,

    }

    /// <summary>Enumeration of possible encodings for CD-TEXT data (values other than 0 for ID1 0x80-0x85 only).</summary>
    public enum CDTextCharacterCode : byte {

      ISO_8859_1      = 0x00, // modified, see CD-EXTRA specification, appendix 1
      ISO_646         = 0x01, // 7-bit ASCII
      // 0x02-0x7f: Reserved
      MusicShiftJis   = 0x80, // Kanji
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

      // From the Red Book standard:
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

    /// <summary>Possible operation codes for CD/DVD/... SCSI commands.</summary>
    public enum OperationCode : byte {

      Blank                      = 0xA1,
      CloseTrackSession          = 0x5B,
      Erase10                    = 0x2C, // [MMC-4] Added, [MMC-6] Dropped
      FormatUnit                 = 0x04,
      GetConfiguration           = 0x46,
      GetEventStatusNotification = 0x4A,
      GetPerformance             = 0xAC,
      LoadUnloadMedium           = 0xA6,
      MechanismStatus            = 0xBD,
      PauseResume                = 0x4B, // [MMC-5] Dropped
      PlayAudio10                = 0x45, // [MMC-5] Dropped
      PlayAudio12                = 0xA5, // [MMC-5] Dropped
      PlayAudioMsf               = 0x47, // [MMC-5] Dropped
      PreventAllowMediumRemoval  = 0x1E, // [MMC-4] Added
      Read10                     = 0x28, // [MMC-4] Added
      Read12                     = 0xA8,
      ReadBufferCapacity         = 0x5C,
      ReadCapacity               = 0x25,
      ReadCd                     = 0xBE,
      ReadCdMsf                  = 0xB9,
      ReadDiscInformation        = 0x51,
      ReadDiscStructure          = 0xAD, // [MMC-5] Added
      ReadDvdStructure           = 0xAD, // [MMC-5] Dropped
      ReadFormatCapabilities     = 0x23,
      ReadSubChannel             = 0x42, // [MMC-5] Dropped
      ReadTocPmaAtip             = 0x43,
      ReadTrackInformation       = 0x52,
      RepairTrack                = 0x58,
      ReportKey                  = 0xA4,
      ReserveTrack               = 0x53,
      Scan                       = 0xBA, // [MMC-5] Dropped
      Seek10                     = 0x2B, // [MMC-4] Added
      SendCueSheet               = 0x5D,
      SendDiscStructure          = 0xBF, // [MMC-5] Added
      SendDvdStructure           = 0xBF, // [MMC-5] Dropped
      SendEvent                  = 0x5D, // [MMC-4] Dropped
      SendKey                    = 0xA3,
      SendOpcInformation         = 0x54,
      SetCdSpeed                 = 0xBB,
      SetReadAhead               = 0xA7,
      SetStreaming               = 0xB6,
      StartStopUnit              = 0x1B, // [MMC-4] Added
      StopPlayScan               = 0x4E, // [MMC-5] Dropped
      SynchronizeCache           = 0x35,
      Verify10                   = 0x2F, // [MMC-4] Added
      Write10                    = 0x2A,
      Write12                    = 0xAA,
      WriteAndVerify10           = 0x2E,

    }

    /// <summary>Possible values for the control field (4 bits) in some structures.</summary>
    [Flags]
    public enum SubChannelControl : byte {

      // The first two bits are "content type".
      ContentTypeMask      = 0x0c,
      TwoChannelAudio      = 0x00,
      Data                 = 0x04,
      ReservedAudio        = 0x08,

      // The third bit indicates whether or not a digital copy is permitted.
      DigitalCopyPermitted = 0x02,

      // The last bit specifies a modifier for the content type: pre-emphasis for audio, incremental recording for data.
      PreEmphasis          = 0x01,
      Incremental          = 0x01,

    }

    /// <summary>Possible values for the ADR field (typically 4 bits) in some structures.</summary>
    public enum SubChannelDataFormat : byte {

      NotSpecified       = 0x00,
      Position           = 0x01,
      MediaCatalogNumber = 0x02,
      ISRC               = 0x03,
      // All other values (4-f) are reserved

    }

    /// <summary>Values for the "sub-channel parameter list" in a READ SUB-CHANNEL command.</summary>
    public enum SubChannelRequestFormat : byte {

      Position           = 0x01,
      MediaCatalogNumber = 0x02,
      ISRC               = 0x03,
      VendorSpecific1    = 0xf0,
      VendorSpecific2    = 0xf1,
      VendorSpecific3    = 0xf2,
      VendorSpecific4    = 0xf3,
      VendorSpecific5    = 0xf4,
      VendorSpecific6    = 0xf5,
      VendorSpecific7    = 0xf6,
      VendorSpecific8    = 0xf7,
      VendorSpecific9    = 0xf8,
      VendorSpecific10   = 0xf9,
      VendorSpecific11   = 0xfa,
      VendorSpecific12   = 0xfb,
      VendorSpecific13   = 0xfc,
      VendorSpecific14   = 0xfd,
      VendorSpecific15   = 0xfe,
      VendorSpecific16   = 0xff,
      // All other values (00, 04-ef) are reserved

    }

    /// <summary>Values for the format in a READ TOC/PMA/ATIP command.</summary>
    public enum TOCRequestFormat : byte {

      TOC         = 0x00,
      SessionInfo = 0x01,
      FullTOC     = 0x02,
      PMA         = 0x03,
      ATIP        = 0x04,
      CDText      = 0x05,
      // All other values (6-f) reserved

    }

    #endregion

    #region Structures

    #region Commands

    /// <summary>Static class containing the command descriptor blocks; their names will match the corresponding <see cref="OperationCode"/> enumeration names.</summary>
    public static class CDB {

      /// <summary>Command structure for READ SUB-CHANNEL.</summary>
      [StructLayout(LayoutKind.Sequential, Pack = 1)]
      public struct ReadSubChannel {
        public readonly OperationCode           OperationCode;
        public readonly byte                    TimeFlag;
        public readonly byte                    SubQFlag;
        public readonly SubChannelRequestFormat Format;
        public readonly byte                    Reserved1;
        public readonly byte                    Reserved2;
        public readonly byte                    TrackNumber;
        public readonly ushort                  AllocationLength;
        public readonly byte                    Control;

        private ReadSubChannel(SubChannelRequestFormat format, bool msf = false, byte track = 0) {
          Type responsetype;
          switch (format) {
            case SubChannelRequestFormat.ISRC:               responsetype = typeof(SubChannelISRC);               break;
            case SubChannelRequestFormat.MediaCatalogNumber: responsetype = typeof(SubChannelMediaCatalogNumber); break;
            default:
              throw new NotSupportedException($"READ SUB-CHANNEL with format '{format}' is not (yet) supported.");
          }
          this.OperationCode    = OperationCode.ReadSubChannel;
          this.TimeFlag         = (byte) (msf ? 0x02 : 0x00);
          this.SubQFlag         = 0x40;
          this.Format           = format;
          this.Reserved1        = 0;
          this.Reserved2        = 0;
          this.TrackNumber      = track;
          this.AllocationLength = (ushort) IPAddress.HostToNetworkOrder((short) Marshal.SizeOf(responsetype));
          this.Control          = 0;
        }

        public static ReadSubChannel ISRC              (byte track) => new ReadSubChannel(SubChannelRequestFormat.ISRC, track: track);
        public static ReadSubChannel MediaCatalogNumber()           => new ReadSubChannel(SubChannelRequestFormat.MediaCatalogNumber);

      }

      /// <summary>Command structure for READ TOC/PMA/ATIP.</summary>
      [StructLayout(LayoutKind.Sequential, Pack = 1)]
      public struct ReadTocPmaAtip {
        public readonly OperationCode    OperationCode;
        public readonly byte             TimeFlag;
        public readonly TOCRequestFormat Format;
        public readonly byte             Reserved1;
        public readonly byte             Reserved2;
        public readonly byte             Reserved3;
        public readonly byte             TrackSessionNumber;
        public readonly ushort           AllocationLength;
        public readonly byte             Control;

        private ReadTocPmaAtip(TOCRequestFormat format, bool msf = false, byte trackOrSession = 0) {
          Type responsetype;
          switch (format) {
            case TOCRequestFormat.TOC:    responsetype = typeof(TOCDescriptor);    break;
            case TOCRequestFormat.CDText: responsetype = typeof(CDTextDescriptor); break;
            default:
              throw new NotSupportedException($"READ TOC/PMA/ATIP with format '{format}' is not (yet) supported.");
          }
          this.OperationCode      = OperationCode.ReadTocPmaAtip;
          this.TimeFlag           = (byte) (msf ? 0x02 : 0x00);
          this.Format             = format;
          this.Reserved1          = 0;
          this.Reserved2          = 0;
          this.Reserved3          = 0;
          this.TrackSessionNumber = trackOrSession;
          this.AllocationLength   = (ushort) IPAddress.HostToNetworkOrder((short) Marshal.SizeOf(responsetype));
          this.Control            = 0;
        }

        public static ReadTocPmaAtip TOC   (bool msf = false) => new ReadTocPmaAtip(TOCRequestFormat.TOC, msf: msf);
        public static ReadTocPmaAtip CDText()                 => new ReadTocPmaAtip(TOCRequestFormat.CDText);

      }

    }

    #endregion

    #region Response Data

    /// <summary>The structure returned for a READ TOC/PMA/ATIP with request code <see cref="TOCRequestFormat.CDText"/>.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CDTextDescriptor {

      public ushort       DataLength;
      public byte         Reserved1;
      public byte         Reserved2;

      // From the Red Book standard:
      // - A set of text info representing is called a BLOCK; a BLOCK can consist of up to 256 PACKs.
      // - Up to 8 BLOCKs can be combined into a text group.
      // - The size of a text group is recommended to be less than 512 PACKs, and shall be at maximum 2048 PACKs.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2048)]
      public CDTextPack[] Items;

      public void FixUp() {
        this.DataLength = (ushort) IPAddress.NetworkToHostOrder((short) this.DataLength);
        // Fix up the items too, reducing the array to the actual size.
        var itemcount = (ushort) ((this.DataLength - 2) / Marshal.SizeOf(typeof(CDTextPack)));
        var realItems = new CDTextPack[itemcount];
        for (var i = 0; i < itemcount; ++i) {
          realItems[i] = this.Items[i];
          realItems[i].FixUp();
        }
        this.Items = realItems;
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
      //   - 0x87     : Genre information, encoded as 2 bytes (MSB first) in the first two bytes of the text data (values: CD-EXTRA spec III.3.2.5.3.8).
      //                The supplementary description of the genre may be appended.
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
        int remainder = 0;
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
      public byte[] LanguageCode; // for blocks 0-7; language code is encoded as specified in annex 1 to part 5 of EBU Tech 3258-E.

    }

    /// <summary>Structure mapping the control and ADR values found in sub-channel data (each taking half a byte).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SubChannelControlAndADR {

      public byte Byte;

      public SubChannelDataFormat ADR     => (SubChannelDataFormat) ((this.Byte >> 4) & 0x0f);
      public SubChannelControl    Control => (SubChannelControl)    ((this.Byte >> 0) & 0x0f);

    }

    /// <summary>The header for the result of 'READ SUB-CHANNEL'.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SubChannelDataHeader {

      public byte        Reserved;
      public AudioStatus AudioStatus;
      public ushort      DataLength;

      public void FixUp() { this.DataLength = (ushort) IPAddress.NetworkToHostOrder((short) this.DataLength); }

    }

    /// <summary>Convenience struct to represent the byte containing the MCVAL/TCVAL bit in the READ SUB-CHANNEL result structures.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SubChannelDataStatus {

      public byte Byte;

      public bool IsValid => (this.Byte & 0x80) == 0x80;

    }

    /// <summary>The structure returned for a READ SUB-CHANNEL with request code <see cref="SubChannelRequestFormat.ISRC"/>.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SubChannelISRC {

      public SubChannelDataHeader    Header;
      public SubChannelDataFormat    Format;
      public SubChannelControlAndADR ControlAndADR;
      public byte                    TrackNumber;
      public byte                    Reserved1;
      public SubChannelDataStatus    Status;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
      public byte[]                  ISRC;
      public byte                    Zero;
      public byte                    AFrame;
      public byte                    Reserved2;

      public void FixUp() { this.Header.FixUp(); }

    }

    /// <summary>The structure returned for a READ SUB-CHANNEL with request code <see cref="SubChannelRequestFormat.MediaCatalogNumber"/>.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SubChannelMediaCatalogNumber {

      public SubChannelDataHeader Header;
      public SubChannelDataFormat Format;
      public byte                 Reserved1;
      public byte                 Reserved2;
      public byte                 Reserved3;
      public SubChannelDataStatus Status;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
      public byte[]               MCN;
      public byte                 Zero;
      public byte                 AFrame;

      public void FixUp() { this.Header.FixUp(); }

    }

    /// <summary>The structure returned for a READ TOC/PMA/ATIP with request code <see cref="TOCRequestFormat.TOC"/>.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TOCDescriptor {

      public ushort            DataLength;
      public byte              FirstTrack;
      public byte              LastTrack;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
      public TrackDescriptor[] Tracks;

      public void FixUp(bool msf) {
        this.DataLength = (ushort) IPAddress.NetworkToHostOrder((short) this.DataLength);
        for (var i = 0; i <= this.LastTrack; ++i)
          this.Tracks[i].FixUp(msf);
      }

    }

    /// <summary>The structure returned, as part of <see cref="TOCDescriptor"/>, for a READ TOC/PMA/ATIP with request code <see cref="TOCRequestFormat.TOC"/>.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TrackDescriptor {

      public byte                    Reserved1;
      public SubChannelControlAndADR ControlAndADR;
      public byte                    TrackNumber;
      public byte                    Reserved2;
      public int                     Address;

      public TimeSpan                TimeCode => new TimeSpan(0, 0, 0, 0, this.Address * 1000 / 75);

      public void FixUp(bool msf) {
        // Endianness
        this.Address = IPAddress.NetworkToHostOrder(this.Address);
        if (!msf) {
          if (this.Address < 0) // this seems to happen on "copy-protected" discs
            this.Address = 0;
          this.Address += 150;
          return;
        }
        // MSF -> Sectors
        var m = (byte) (this.Address >> 16 & 0xff);
        var s = (byte) (this.Address >>  8 & 0xff);
        var f = (byte) (this.Address >>  0 & 0xff);
        this.Address = (m * 60 + s) * 75 + f;
      }

    }

    #endregion

    #endregion

  }

}
