﻿using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

namespace MetaBrainz.MusicBrainz.DiscId {

  /// <summary>Static class containing structures, enumerations and constants as defined by NCITS document T10/1363-D (MMC-3).</summary>
  internal static class MMC3 {

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

    /// <summary>Possible values for the control field (4 bits) in some structures.</summary>
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

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
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

    /// <summary>The structure returned for a READ TOC/PMA/ATIP with request code <see cref="TOCRequestFormat.CDText"/>.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
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

    /// <summary>Structure mapping the control and ADR values found in sub-channel data (each taking half a byte).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SubChannelControlAndADR {

      byte Byte;

      public SubChannelDataFormat ADR     => (SubChannelDataFormat) ((this.Byte >> 4) & 0x0f);
      public SubChannelControl    Control => (SubChannelControl)    ((this.Byte >> 0) & 0x0f);

    }

    /// <summary>Convenience struct to represent the byte containing the MCVAL/TCVAL bit in the READ SUB-CHANNEL result structures.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SubChannelDataStatus {

      public byte Byte;

      public bool IsValid => (this.Byte & 0x80) == 0x80;

    }

    /// <summary>The header for the result of 'READ SUB-CHANNEL'.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SubChannelDataHeader {

      public byte        Reserved;
      public AudioStatus AudioStatus;
      public ushort      DataLength;

      public void FixUp() { this.DataLength = (ushort) IPAddress.NetworkToHostOrder((short) this.DataLength); }

    }

    /// <summary>The structure returned for a READ SUB-CHANNEL with request code <see cref="SubChannelRequestFormat.MediaCatalogNumber"/>.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
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

    /// <summary>The structure returned for a READ SUB-CHANNEL with request code <see cref="SubChannelRequestFormat.ISRC"/>.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
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

    /// <summary>The structure returned for a READ TOC/PMA/ATIP with request code <see cref="TOCRequestFormat.TOC"/>.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct TOCDescriptor {

      public ushort            DataLength;
      public byte              FirstTrack;
      public byte              LastTrack;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
      public TrackDescriptor[] Tracks;

      public void FixUp() {
        this.DataLength = (ushort) IPAddress.NetworkToHostOrder((short) this.DataLength);
        for (var i = 0; i <= this.LastTrack; ++i)
          this.Tracks[i].FixUp();
      }

    }

    /// <summary>The structure returned, as part of <see cref="TOCDescriptor"/>, for a READ TOC/PMA/ATIP with request code <see cref="TOCRequestFormat.TOC"/>.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct TrackDescriptor {

      public byte                    Reserved1;
      public SubChannelControlAndADR ControlAndADR;
      public byte                    TrackNumber;
      public byte                    Reserved2;
      public int                     Address;

      public TimeSpan                TimeCode => new TimeSpan(0, 0, 0, 0, this.Address * 1000 / 75);

      public void FixUp() { MMC3.FixUpAddress(ref this.Address); }

    }

    #endregion

    #region Utility Methods

    private static void FixUpAddress(ref int address) {
      // Endianness
      address = IPAddress.NetworkToHostOrder(address);
#if GET_TOC_AS_MSF // MSF -> Sectors
      var m = (byte) (address >> 16 & 0xff);
      var s = (byte) (address >>  8 & 0xff);
      var f = (byte) (address >>  0 & 0xff);
      address = (m * 60 + s) * 75 + f;
#else // LBA -> Sectors
      address += 150;
#endif
    }

    #endregion

  }

}