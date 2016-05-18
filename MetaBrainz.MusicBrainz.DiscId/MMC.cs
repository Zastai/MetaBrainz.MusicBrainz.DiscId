using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

namespace MetaBrainz.MusicBrainz.DiscId {

  /// <summary>Static class containing structures, enumerations and constants for SCSI MultiMedia Commands.</summary>
  /// <remarks>
  /// Based on the following (draft) standard documents:
  /// <list type="bullet">
  ///   <item><term>[MMC-3]</term><description>NCITS T10/1363-D revision 10g</description></item>
  ///   <item><term>[MMC-4]</term><description>INCITS T10/1545-D revision 5a (note: this is the last version to include CD-specific commands like <c>READ SUB-CHANNEL</c>)</description></item>
  ///   <item><term>[MMC-5]</term><description>INCITS T10/1675D revision 4</description></item>
  ///   <item><term>[MMC-6]</term><description>INCITS T10/1836D revision 2g</description></item>
  /// </list>
  /// </remarks>
  internal static class MMC {

    #region Enumerations

    public enum AudioStatus : byte {
      NotSupported = 0x00,
      InProgress   = 0x11,
      Paused       = 0x12,
      PlayComplete = 0x13,
      PlayError    = 0x14,
      NoStatus     = 0x15,
    }

    public enum CDTextContentType : byte {
      Nothing   = 0x00,
      AlbumName = 0x80,
      Performer = 0x81,
      Lyricist  = 0x82,
      Composer  = 0x83,
      Arranger  = 0x84,
      Messages  = 0x85,
      DiscID    = 0x86,
      Genre     = 0x87,
      TOCInfo   = 0x88,
      TOCInfo2  = 0x89,
      UPC       = 0x8e,
      EAN       = 0x8e,
      SizeInfo  = 0x8f,
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
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 150)]
      public CDTextItem[] Items;

      public void FixUp() {
        this.DataLength = (ushort) IPAddress.NetworkToHostOrder((short) this.DataLength);
        var items = (this.DataLength - 2) / Marshal.SizeOf(this.Items[0]);
        for (var i = 0; i < items; ++i)
          this.Items[i].FixUp();
      }

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CDTextItem {

      public CDTextContentType Type;
      public byte              RawInfo1;
      public byte              SequenceNumber;
      public byte              RawInfo2;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
      public byte[]            Data;
      public ushort            CRC;

      public byte TrackNumber => (byte) (this.RawInfo1 & 0x7f);

      public bool IsExtension => (this.RawInfo1 & 0x80) == 0x80;

      public byte CharacterPosition => (byte) (this.RawInfo2 & 0x0f);

      public byte BlockNumber => (byte) ((this.RawInfo2 & 0x70) >> 4);

      public bool IsUnicode => (this.RawInfo2 & 0x80) == 0x80;

      public string Text => (this.IsUnicode ? Encoding.BigEndianUnicode : Encoding.ASCII).GetString(this.Data);

      public bool? IsValid => null; // TODO: Add CRC check.

      public void FixUp() { this.CRC = (ushort) IPAddress.NetworkToHostOrder((short) this.CRC); }

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
