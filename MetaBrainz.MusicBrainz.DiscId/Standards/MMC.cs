using System;
using System.Net;
using System.Runtime.InteropServices;

using JetBrains.Annotations;

namespace MetaBrainz.MusicBrainz.DiscId.Standards;

/// <summary>Static class containing structures, enumerations and constants for the SCSI MultiMedia Commands.</summary>
/// <remarks>
/// Based on the following (draft) standard documents:
/// <list type="bullet">
///   <item><term>[MMC]  </term><description>X3T10 1048D revision 10a</description></item>
///   <item><term>[MMC-2]</term><description>NCITS 333 T10/1228-D revision 11a</description></item>
///   <item><term>[MMC-3]</term><description>NCITS T10/1363-D revision 10g</description></item>
///   <item><term>[MMC-4]</term><description>
///     INCITS T10/1545-D revision 5a (note: this is the last version to include CD-specific commands like <c>READ SUB-CHANNEL</c>)
///   </description></item>
///   <item><term>[MMC-5]</term><description>INCITS T10/1675D revision 4</description></item>
///   <item><term>[MMC-6]</term><description>INCITS T10/1836D revision 2g</description></item>
/// </list>
/// </remarks>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal static class MMC {

  #region Enumerations

  /// <summary>The current status of audio playback.</summary>
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public enum AudioStatus : byte {

    NotSupported = 0x00,

    InProgress = 0x11,

    Paused = 0x12,

    PlayComplete = 0x13,

    PlayError = 0x14,

    NoStatus = 0x15,

  }

  /// <summary>Possible operation codes for CD/DVD/... SCSI commands.</summary>
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public enum OperationCode : byte {

    Blank = 0xA1,

    CloseTrackSession = 0x5B,

    // [MMC-4] Added, [MMC-6] Dropped
    Erase10 = 0x2C,

    FormatUnit = 0x04,

    GetConfiguration = 0x46,

    GetEventStatusNotification = 0x4A,

    GetPerformance = 0xAC,

    LoadUnloadMedium = 0xA6,

    MechanismStatus = 0xBD,

    // [MMC-5] Dropped
    PauseResume = 0x4B,

    // [MMC-5] Dropped
    PlayAudio10 = 0x45,

    // [MMC-5] Dropped
    PlayAudio12 = 0xA5,

    // [MMC-5] Dropped
    PlayAudioMsf = 0x47,

    // [MMC-4] Added
    PreventAllowMediumRemoval = 0x1E,

    // [MMC-4] Added
    Read10 = 0x28,

    Read12 = 0xA8,

    ReadBufferCapacity = 0x5C,

    ReadCapacity = 0x25,

    ReadCd = 0xBE,

    ReadCdMsf = 0xB9,

    ReadDiscInformation = 0x51,

    // [MMC-5] Added
    ReadDiscStructure = 0xAD,

    // [MMC-5] Dropped
    ReadDvdStructure = 0xAD,

    ReadFormatCapabilities = 0x23,

    // [MMC-5] Dropped
    ReadSubChannel = 0x42,

    ReadTocPmaAtip = 0x43,

    ReadTrackInformation = 0x52,

    RepairTrack = 0x58,

    ReportKey = 0xA4,

    ReserveTrack = 0x53,

    // [MMC-5] Dropped
    Scan = 0xBA,

    // [MMC-4] Added
    Seek10 = 0x2B,

    SendCueSheet = 0x5D,

    // [MMC-5] Added
    SendDiscStructure = 0xBF,

    // [MMC-5] Dropped
    SendDvdStructure = OperationCode.SendDiscStructure,

    // [MMC-4] Dropped
    SendEvent = OperationCode.SendCueSheet,

    SendKey = 0xA3,

    SendOpcInformation = 0x54,

    SetCdSpeed = 0xBB,

    SetReadAhead = 0xA7,

    SetStreaming = 0xB6,

    // [MMC-4] Added
    StartStopUnit = 0x1B,

    // [MMC-5] Dropped
    StopPlayScan = 0x4E,

    SynchronizeCache = 0x35,

    // [MMC-4] Added
    Verify10 = 0x2F,

    Write10 = 0x2A,

    Write12 = 0xAA,

    WriteAndVerify10 = 0x2E,

  }

  /// <summary>Possible values for the control field (4 bits) in some structures.</summary>
  [Flags]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public enum SubChannelControl : byte {

    // The first two bits are "content type".
    ContentTypeMask = 0x0c,

    TwoChannelAudio = 0x00,

    Data = 0x04,

    ReservedAudio = 0x08,

    // The third bit indicates whether a digital copy is permitted.
    DigitalCopyPermitted = 0x02,

    // The last bit specifies a modifier for the content type: pre-emphasis for audio, incremental recording for data.
    PreEmphasisOrIncrementalRecording = 0x01,

  }

  /// <summary>Possible values for the ADR field (typically 4 bits) in some structures.</summary>
  /// <remarks>All other values (<c>0x04</c>-<c>0xff</c>) are reserved.</remarks>
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public enum SubChannelDataFormat : byte {

    NotSpecified = 0x00,

    Position = 0x01,

    MediaCatalogNumber = 0x02,

    ISRC = 0x03,

  }

  /// <summary>Values for the "sub-channel parameter list" in a READ SUB-CHANNEL command.</summary>
  /// <remarks>All other values (<c>0x00</c>, <c>0x04</c>-<c>0xef</c>) are reserved.</remarks>
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public enum SubChannelRequestFormat : byte {

    Position = 0x01,

    MediaCatalogNumber = 0x02,

    ISRC = 0x03,

    VendorSpecific1 = 0xf0,

    VendorSpecific2 = 0xf1,

    VendorSpecific3 = 0xf2,

    VendorSpecific4 = 0xf3,

    VendorSpecific5 = 0xf4,

    VendorSpecific6 = 0xf5,

    VendorSpecific7 = 0xf6,

    VendorSpecific8 = 0xf7,

    VendorSpecific9 = 0xf8,

    VendorSpecific10 = 0xf9,

    VendorSpecific11 = 0xfa,

    VendorSpecific12 = 0xfb,

    VendorSpecific13 = 0xfc,

    VendorSpecific14 = 0xfd,

    VendorSpecific15 = 0xfe,

    VendorSpecific16 = 0xff,

  }

  /// <summary>Values for the format in a READ TOC/PMA/ATIP command.</summary>
  /// <remarks>All other values (<c>0x06</c>-<c>0xff</c>) are reserved.</remarks>
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public enum TOCRequestFormat : byte {

    TOC = 0x00,

    SessionInfo = 0x01,

    FullTOC = 0x02,

    PMA = 0x03,

    ATIP = 0x04,

    CDText = 0x05,

  }

  #endregion

  #region Structures

  #region Commands

  /// <summary>
  /// Static class containing the command descriptor blocks; their names will match the corresponding <see cref="OperationCode"/>
  /// enumeration names.
  /// </summary>
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public static class CDB {

    /// <summary>Command structure for READ SUB-CHANNEL.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public struct ReadSubChannel {

      public readonly OperationCode OperationCode;

      public readonly byte TimeFlag;

      public readonly byte SubQFlag;

      public readonly SubChannelRequestFormat Format;

      public readonly byte Reserved1;

      public readonly byte Reserved2;

      public readonly byte TrackNumber;

      public readonly ushort AllocationLength;

      public readonly byte Control;

      private ReadSubChannel(SubChannelRequestFormat format, bool msf = false, byte track = 0) {
        var size = format switch {
          SubChannelRequestFormat.ISRC => Marshal.SizeOf<SubChannelISRC>(),
          SubChannelRequestFormat.MediaCatalogNumber => Marshal.SizeOf<SubChannelMediaCatalogNumber>(),
          _ => throw new NotSupportedException($"READ SUB-CHANNEL with format '{format}' is not (yet) supported."),
        };
        this.OperationCode = OperationCode.ReadSubChannel;
        this.TimeFlag = (byte) (msf ? 0x02 : 0x00);
        this.SubQFlag = 0x40;
        this.Format = format;
        this.Reserved1 = 0;
        this.Reserved2 = 0;
        this.TrackNumber = track;
        this.AllocationLength = (ushort) IPAddress.HostToNetworkOrder((short) size);
        this.Control = 0;
      }

      /// <summary>Creates a new <c>READ SUB-CHANNEL</c> command, to read the ISRC for a track.</summary>
      /// <param name="track">The track for which the ISRC should be read.</param>
      /// <returns>A new <c>READ SUB-CHANNEL</c> command, to read the ISRC for track <paramref name="track"/>.</returns>
      /// <remarks>The returned command will return a <see cref="SubChannelISRC"/> structure.</remarks>
      public static ReadSubChannel ISRC(byte track) => new(SubChannelRequestFormat.ISRC, track: track);

      /// <summary>Creates a new <c>READ SUB-CHANNEL</c> command, to read the disc's media catalog number.</summary>
      /// <returns>A new <c>READ SUB-CHANNEL</c> command, to read the disc's media catalog number.</returns>
      /// <remarks>The returned command will return a <see cref="SubChannelMediaCatalogNumber"/> structure.</remarks>
      public static ReadSubChannel MediaCatalogNumber() => new(SubChannelRequestFormat.MediaCatalogNumber);

    }

    /// <summary>Command structure for READ TOC/PMA/ATIP.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public struct ReadTocPmaAtip {

      public readonly OperationCode OperationCode;

      public readonly byte TimeFlag;

      public readonly TOCRequestFormat Format;

      public readonly byte Reserved1;

      public readonly byte Reserved2;

      public readonly byte Reserved3;

      public readonly byte TrackSessionNumber;

      public readonly ushort AllocationLength;

      public readonly byte Control;

      private ReadTocPmaAtip(TOCRequestFormat format, bool msf = false, byte trackOrSession = 0) {
        var size = format switch {
          TOCRequestFormat.TOC => Marshal.SizeOf<TOCDescriptor>(),
          TOCRequestFormat.CDText => Marshal.SizeOf<CDTextDescriptor>(),
          _ => throw new NotSupportedException($"READ TOC/PMA/ATIP with format '{format}' is not (yet) supported."),
        };
        this.OperationCode = OperationCode.ReadTocPmaAtip;
        this.TimeFlag = (byte) (msf ? 0x02 : 0x00);
        this.Format = format;
        this.Reserved1 = 0;
        this.Reserved2 = 0;
        this.Reserved3 = 0;
        this.TrackSessionNumber = trackOrSession;
        this.AllocationLength = (ushort) IPAddress.HostToNetworkOrder((short) size);
        this.Control = 0;
      }

      /// <summary>Creates a new <c>READ TOC/PMA/ATIP</c> command, to read the disc's table of contents.</summary>
      /// <param name="msf">Indicates whether time codes should be returned in MSF format.</param>
      /// <returns>A new <c>READ TOC/PMA/ATIP</c> command, to read the disc's table of contents.</returns>
      /// <remarks>The returned command will return a <see cref="TOCDescriptor"/> structure.</remarks>
      public static ReadTocPmaAtip TOC(bool msf = false) => new(TOCRequestFormat.TOC, msf: msf);

      /// <summary>Creates a new <c>READ TOC/PMA/ATIP</c> command, to read the disc's CD-TEXT information.</summary>
      /// <returns>A new <c>READ TOC/PMA/ATIP</c> command, to read the disc's CD-TEXT information.</returns>
      /// <remarks>The returned command will return a <see cref="CDTextDescriptor"/> structure.</remarks>
      public static ReadTocPmaAtip CDText() => new(TOCRequestFormat.CDText);

    }

  }

  #endregion

  #region Response Data

  /// <summary>
  /// A CD-TEXT descriptor structure, as returned for a <c>READ TOC/PMA/ATIP</c> command with request code
  /// <see cref="TOCRequestFormat.CDText"/>.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct CDTextDescriptor {

    public ushort DataLength;

    public byte Reserved1;

    public byte Reserved2;

    public RedBook.CDTextGroup Data;

    public void FixUp() {
      this.DataLength = (ushort) IPAddress.NetworkToHostOrder((short) this.DataLength);
      this.Data.FixUp(this.DataLength - 2);
    }

  }

  /// <summary>Structure mapping the control and ADR values found in sub-channel data (each taking half a byte).</summary>
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct SubChannelControlAndADR {

    public byte Byte;

    public SubChannelDataFormat ADR => (SubChannelDataFormat) ((this.Byte >> 4) & 0x0f);

    public SubChannelControl Control => (SubChannelControl) (this.Byte & 0x0f);

  }

  /// <summary>The header for the result of a <c>READ SUB-CHANNEL</c> command.</summary>
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct SubChannelDataHeader {

    public byte Reserved;

    public AudioStatus AudioStatus;

    public ushort DataLength;

    public void FixUp() => this.DataLength = (ushort) IPAddress.NetworkToHostOrder((short) this.DataLength);

  }

  /// <summary>
  /// Convenience struct to represent the byte containing the MCVAL/TCVAL bit in the <c>READ SUB-CHANNEL</c> result structures.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct SubChannelDataStatus {

    public byte Byte;

    public bool IsValid => (this.Byte & 0x80) == 0x80;

  }

  /// <summary>
  /// The ISRC structure, as returned for a <c>READ SUB-CHANNEL</c> command with request code
  /// <see cref="SubChannelRequestFormat.ISRC"/>.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct SubChannelISRC {

    public SubChannelDataHeader Header;

    public SubChannelDataFormat Format;

    public SubChannelControlAndADR ControlAndADR;

    public byte TrackNumber;

    public byte Reserved1;

    public SubChannelDataStatus Status;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    public byte[] ISRC;

    public byte Zero;

    public byte AFrame;

    public byte Reserved2;

    public void FixUp() => this.Header.FixUp();

  }

  /// <summary>
  /// The media catalog number structure, as returned for a <c>READ SUB-CHANNEL</c> command with request code
  /// <see cref="SubChannelRequestFormat.MediaCatalogNumber"/>.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct SubChannelMediaCatalogNumber {

    public SubChannelDataHeader Header;

    public SubChannelDataFormat Format;

    public byte Reserved1;

    public byte Reserved2;

    public byte Reserved3;

    public SubChannelDataStatus Status;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
    public byte[] MCN;

    public byte Zero;

    public byte AFrame;

    public void FixUp() => this.Header.FixUp();

  }

  /// <summary>
  /// A TOC descriptor structure, as returned for a <c>READ TOC/PMA/ATIP</c> command with request code
  /// <see cref="TOCRequestFormat.TOC"/>.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct TOCDescriptor {

    public ushort DataLength;

    public byte FirstTrack;

    public byte LastTrack;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
    public TrackDescriptor[] Tracks;

    public void FixUp(bool msf) {
      this.DataLength = (ushort) IPAddress.NetworkToHostOrder((short) this.DataLength);
      for (var i = 0; i <= this.LastTrack; ++i) {
        this.Tracks[i].FixUp(msf);
      }
    }

  }

  /// <summary>
  /// The track descriptor structure, as returned (as part of <see cref="TOCDescriptor"/>) for a <c>READ TOC/PMA/ATIP</c> command
  /// with request code <see cref="TOCRequestFormat.TOC"/>.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct TrackDescriptor {

    public byte Reserved1;

    public SubChannelControlAndADR ControlAndADR;

    public byte TrackNumber;

    public byte Reserved2;

    public int Address;

    public TimeSpan TimeCode => new(0, 0, 0, 0, this.Address * 1000 / 75);

    public void FixUp(bool msf) {
      // Endianness
      this.Address = IPAddress.NetworkToHostOrder(this.Address);
      if (!msf) {
        if (this.Address < 0) { // this seems to happen on "copy-protected" discs
          this.Address = 0;
        }
        this.Address += 150;
        return;
      }
      // MSF -> Sectors
      var m = (byte) ((this.Address >> 16) & 0xff);
      var s = (byte) ((this.Address >> 8) & 0xff);
      var f = (byte) (this.Address & 0xff);
      this.Address = (((m * 60) + s) * 75) + f;
    }

  }

  #endregion

  #endregion

}
