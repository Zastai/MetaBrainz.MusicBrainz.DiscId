using System;
using System.Runtime.InteropServices;

using JetBrains.Annotations;

namespace MetaBrainz.MusicBrainz.DiscId.Standards;

/// <summary>Static class containing structures, enumerations and constants for the SCSI Primary Commands.</summary>
/// <remarks>
/// Based on the following (draft) standard documents:
/// <list type="bullet">
///   <item><term>[SPC]</term>   <description>T10 995D revision 11a</description></item>
///   <item><term>[SPC-2]</term> <description>Project T10/1236-D revision 20</description></item>
///   <item><term>[SPC-3]</term> <description>Project T10/1416-D revision 23</description></item>
///   <item><term>[SPC-4]</term> <description>Project T10/BSR INCITS 513 revision 37</description></item>
///   <item><term>[SPC-5]</term> <description>Project T10/BSR INCITS 502 revision 10a</description></item>
/// </list>
/// </remarks>
internal static class SPC {

  #region Enumerations

  // TODO: Maybe create enums for sense key and additional sense info too?

  #endregion

  #region Structures

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct DescriptorSenseData {

    public byte Byte1;

    public byte Byte2;

    public byte AdditionalSenseCode;

    public byte AdditionalSenseCodeQualifier;

    public byte Byte5;

    public byte Byte6;

    public byte Byte7;

    public byte AdditionalSenseLength;

    public SenseDescriptor Descriptor;

    public byte ResponseCode => (byte) (this.Byte1 & 0x7f);

    public byte SenseKey => (byte) (this.Byte2 & 0x0f);

    public bool SenseDataOverflow => (this.Byte5 & 0x10) == 0x10;

  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct FixedSenseData {

    public byte Byte1;

    public byte SegmentNumber;

    public byte Byte3;

    public uint Information;

    public byte AdditionalSenseLength;

    public uint CommandSpecificInformation;

    public byte AdditionalSenseCode;

    public byte AdditionalSenseCodeQualifier;

    public byte FieldReplaceableUnitCode;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public byte[] SenseKeySpecific;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
    public byte[] AdditionalSenseData;

    public bool InformationIsValid => (this.Byte1 & 0x80) == 0x80;

    public byte ResponseCode => (byte) (this.Byte1 & 0x7f);

    public bool FileMark => (this.Byte3 & 0x80) == 0x80;

    public bool EndOfMedium => (this.Byte3 & 0x40) == 0x40;

    public bool InvalidLengthIndicator => (this.Byte3 & 0x20) == 0x20;

    public bool SenseDataOverflow => (this.Byte3 & 0x10) == 0x10;

    public byte SenseKey => (byte) (this.Byte3 & 0x0f);

  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct SenseDescriptor {

    public byte DescriptorType;

    public byte AdditionalLength;

    public byte Byte3;

    public byte Byte4;

    public uint Information;

    public bool InformationIsValid => (this.Byte3 & 0x80) == 0x80;

  }

  #endregion

  #region Utility Methods

  /// <summary>Creates a string describing the specified SCSI sense key value.</summary>
  /// <param name="senseKey">The SCSI &quot;sense key&quot; value to describe.</param>
  /// <returns>A description of the specified SCSI sense key value.</returns>
  public static string SenseKeyDescription(byte senseKey) => senseKey switch {
    0x0 => "NO SENSE",
    0x1 => "RECOVERED ERROR",
    0x2 => "NOT READY",
    0x3 => "MEDIUM ERROR",
    0x4 => "HARDWARE ERROR",
    0x5 => "ILLEGAL REQUEST",
    0x6 => "UNIT ATTENTION",
    0x7 => "DATA PROTECT",
    0x8 => "BLANK CHECK",
    0x9 => "VENDOR SPECIFIC",
    0xa => "COPY ABORTED",
    0xb => "ABORTED COMMAND",
    0xc => "RESERVED",
    0xd => "VOLUME OVERFLOW",
    0xe => "NO SENSE",
    0xf => "COMPLETED",
    // Not technically possible (value is only 4 bits)
    _ => throw new ArgumentOutOfRangeException(nameof(senseKey), senseKey, "A SCSI sense key must be between 0 and 15.")
  };

  /// <summary>Creates a string describing the specified SCSI additional sense information.</summary>
  /// <param name="asc">The SCSI &quot;additional sense code&quot; value.</param>
  /// <param name="ascq">The SCSI &quot;additional sense code qualifier&quot; value.</param>
  /// <returns>A description of the specified SCSI additional sense key information.</returns>
  public static string AdditionalSenseDescription(byte asc, byte ascq) {
    switch ((ushort) ((asc << 8) | ascq)) {
      case 0x0000:
        return "NO ADDITIONAL SENSE INFORMATION";
      case 0x0001:
        return "FILEMARK DETECTED";
      case 0x0002:
        return "END-OF-PARTITION/MEDIUM DETECTED";
      case 0x0003:
        return "SETMARK DETECTED";
      case 0x0004:
        return "BEGINNING-OF-PARTITION/MEDIUM DETECTED";
      case 0x0005:
        return "END-OF-DATA DETECTED";
      case 0x0006:
        return "I/O PROCESS TERMINATED";
      case 0x0007:
        return "PROGRAMMABLE EARLY WARNING DETECTED";
      case 0x0011:
        return "AUDIO PLAY OPERATION IN PROGRESS";
      case 0x0012:
        return "AUDIO PLAY OPERATION PAUSED";
      case 0x0013:
        return "AUDIO PLAY OPERATION SUCCESSFULLY COMPLETED";
      case 0x0014:
        return "AUDIO PLAY OPERATION STOPPED DUE TO ERROR";
      case 0x0015:
        return "NO CURRENT AUDIO STATUS TO RETURN";
      case 0x0016:
        return "OPERATION IN PROGRESS";
      case 0x0017:
        return "CLEANING REQUESTED";
      case 0x0018:
        return "ERASE OPERATION IN PROGRESS";
      case 0x0019:
        return "LOCATE OPERATION IN PROGRESS";
      case 0x001A:
        return "REWIND OPERATION IN PROGRESS";
      case 0x001B:
        return "SET CAPACITY OPERATION IN PROGRESS";
      case 0x001C:
        return "VERIFY OPERATION IN PROGRESS";
      case 0x001D:
        return "ATA PASS THROUGH INFORMATION AVAILABLE";
      case 0x001E:
        return "CONFLICTING SA CREATION REQUEST";
      case 0x001F:
        return "LOGICAL UNIT TRANSITIONING TO ANOTHER POWER CONDITION";
      case 0x0020:
        return "EXTENDED COPY INFORMATION AVAILABLE";
      case 0x0021:
        return "ATOMIC COMMAND ABORTED DUE TO ACA";
      case 0x0100:
        return "NO INDEX/SECTOR SIGNAL";
      case 0x0200:
        return "NO SEEK COMPLETE";
      case 0x0300:
        return "PERIPHERAL DEVICE WRITE FAULT";
      case 0x0301:
        return "NO WRITE CURRENT";
      case 0x0302:
        return "EXCESSIVE WRITE ERRORS";
      case 0x0400:
        return "LOGICAL UNIT NOT READY, CAUSE NOT REPORTABLE";
      case 0x0401:
        return "LOGICAL UNIT IS IN PROCESS OF BECOMING READY";
      case 0x0402:
        return "LOGICAL UNIT NOT READY, INITIALIZING COMMAND REQUIRED";
      case 0x0403:
        return "LOGICAL UNIT NOT READY, MANUAL INTERVENTION REQUIRED";
      case 0x0404:
        return "LOGICAL UNIT NOT READY, FORMAT IN PROGRESS";
      case 0x0405:
        return "LOGICAL UNIT NOT READY, REBUILD IN PROGRESS";
      case 0x0406:
        return "LOGICAL UNIT NOT READY, RECALCULATION IN PROGRESS";
      case 0x0407:
        return "LOGICAL UNIT NOT READY, OPERATION IN PROGRESS";
      case 0x0408:
        return "LOGICAL UNIT NOT READY, LONG WRITE IN PROGRESS";
      case 0x0409:
        return "LOGICAL UNIT NOT READY, SELF-TEST IN PROGRESS";
      case 0x040A:
        return "LOGICAL UNIT NOT ACCESSIBLE, ASYMMETRIC ACCESS STATE TRANSITION";
      case 0x040B:
        return "LOGICAL UNIT NOT ACCESSIBLE, TARGET PORT IN STANDBY STATE";
      case 0x040C:
        return "LOGICAL UNIT NOT ACCESSIBLE, TARGET PORT IN UNAVAILABLE STATE";
      case 0x040D:
        return "LOGICAL UNIT NOT READY, STRUCTURE CHECK REQUIRED";
      case 0x040E:
        return "LOGICAL UNIT NOT READY, SECURITY SESSION IN PROGRESS";
      case 0x0410:
        return "LOGICAL UNIT NOT READY, AUXILIARY MEMORY NOT ACCESSIBLE";
      case 0x0411:
        return "LOGICAL UNIT NOT READY, NOTIFY (ENABLE SPINUP) REQUIRED";
      case 0x0412:
        return "LOGICAL UNIT NOT READY, OFFLINE";
      case 0x0413:
        return "LOGICAL UNIT NOT READY, SA CREATION IN PROGRESS";
      case 0x0414:
        return "LOGICAL UNIT NOT READY, SPACE ALLOCATION IN PROGRESS";
      case 0x0415:
        return "LOGICAL UNIT NOT READY, ROBOTICS DISABLED";
      case 0x0416:
        return "LOGICAL UNIT NOT READY, CONFIGURATION REQUIRED";
      case 0x0417:
        return "LOGICAL UNIT NOT READY, CALIBRATION REQUIRED";
      case 0x0418:
        return "LOGICAL UNIT NOT READY, A DOOR IS OPEN";
      case 0x0419:
        return "LOGICAL UNIT NOT READY, OPERATING IN SEQUENTIAL MODE";
      case 0x041A:
        return "LOGICAL UNIT NOT READY, START STOP UNIT COMMAND IN PROGRESS";
      case 0x041B:
        return "LOGICAL UNIT NOT READY, SANITIZE IN PROGRESS";
      case 0x041C:
        return "LOGICAL UNIT NOT READY, ADDITIONAL POWER USE NOT YET GRANTED";
      case 0x041D:
        return "LOGICAL UNIT NOT READY, CONFIGURATION IN PROGRESS";
      case 0x041E:
        return "LOGICAL UNIT NOT READY, MICROCODE ACTIVATION REQUIRED";
      case 0x041F:
        return "LOGICAL UNIT NOT READY, MICROCODE DOWNLOAD REQUIRED";
      case 0x0420:
        return "LOGICAL UNIT NOT READY, LOGICAL UNIT RESET REQUIRED";
      case 0x0421:
        return "LOGICAL UNIT NOT READY, HARD RESET REQUIRED";
      case 0x0422:
        return "LOGICAL UNIT NOT READY, POWER CYCLE REQUIRED";
      case 0x0423:
        return "LOGICAL UNIT NOT READY, AFFILIATION REQUIRED";
      case 0x0500:
        return "LOGICAL UNIT DOES NOT RESPOND TO SELECTION";
      case 0x0600:
        return "NO REFERENCE POSITION FOUND";
      case 0x0700:
        return "MULTIPLE PERIPHERAL DEVICES SELECTED";
      case 0x0800:
        return "LOGICAL UNIT COMMUNICATION FAILURE";
      case 0x0801:
        return "LOGICAL UNIT COMMUNICATION TIME-OUT";
      case 0x0802:
        return "LOGICAL UNIT COMMUNICATION PARITY ERROR";
      case 0x0803:
        return "LOGICAL UNIT COMMUNICATION CRC ERROR (ULTRA-DMA/32)";
      case 0x0804:
        return "UNREACHABLE COPY TARGET";
      case 0x0900:
        return "TRACK FOLLOWING ERROR";
      case 0x0901:
        return "TRACKING SERVO FAILURE";
      case 0x0902:
        return "FOCUS SERVO FAILURE";
      case 0x0903:
        return "SPINDLE SERVO FAILURE";
      case 0x0904:
        return "HEAD SELECT FAULT";
      case 0x0905:
        return "VIBRATION INDUCED TRACKING ERROR";
      case 0x0A00:
        return "ERROR LOG OVERFLOW";
      case 0x0B00:
        return "WARNING";
      case 0x0B01:
        return "WARNING - SPECIFIED TEMPERATURE EXCEEDED";
      case 0x0B02:
        return "WARNING - ENCLOSURE DEGRADED";
      case 0x0B03:
        return "WARNING - BACKGROUND SELF-TEST FAILED";
      case 0x0B04:
        return "WARNING - BACKGROUND PRE-SCAN DETECTED MEDIUM ERROR";
      case 0x0B05:
        return "WARNING - BACKGROUND MEDIUM SCAN DETECTED MEDIUM ERROR";
      case 0x0B06:
        return "WARNING - NON-VOLATILE CACHE NOW VOLATILE";
      case 0x0B07:
        return "WARNING - DEGRADED POWER TO NON-VOLATILE CACHE";
      case 0x0B08:
        return "WARNING - POWER LOSS EXPECTED";
      case 0x0B09:
        return "WARNING - DEVICE STATISTICS NOTIFICATION ACTIVE";
      case 0x0B0A:
        return "WARNING - HIGH CRITICAL TEMPERATURE LIMIT EXCEEDED";
      case 0x0B0B:
        return "WARNING - LOW CRITICAL TEMPERATURE LIMIT EXCEEDED";
      case 0x0B0C:
        return "WARNING - HIGH OPERATING TEMPERATURE LIMIT EXCEEDED";
      case 0x0B0D:
        return "WARNING - LOW OPERATING TEMPERATURE LIMIT EXCEEDED";
      case 0x0B0E:
        return "WARNING - HIGH CRITICAL HUMIDITY LIMIT EXCEEDED";
      case 0x0B0F:
        return "WARNING - LOW CRITICAL HUMIDITY LIMIT EXCEEDED";
      case 0x0B10:
        return "WARNING - HIGH OPERATING HUMIDITY LIMIT EXCEEDED";
      case 0x0B11:
        return "WARNING - LOW OPERATING HUMIDITY LIMIT EXCEEDED";
      case 0x0C00:
        return "WRITE ERROR";
      case 0x0C01:
        return "WRITE ERROR - RECOVERED WITH AUTO REALLOCATION";
      case 0x0C02:
        return "WRITE ERROR - AUTO REALLOCATION FAILED";
      case 0x0C03:
        return "WRITE ERROR - RECOMMEND REASSIGNMENT";
      case 0x0C04:
        return "COMPRESSION CHECK MISCOMPARE ERROR";
      case 0x0C05:
        return "DATA EXPANSION OCCURRED DURING COMPRESSION";
      case 0x0C06:
        return "BLOCK NOT COMPRESSIBLE";
      case 0x0C07:
        return "WRITE ERROR - RECOVERY NEEDED";
      case 0x0C08:
        return "WRITE ERROR - RECOVERY FAILED";
      case 0x0C09:
        return "WRITE ERROR - LOSS OF STREAMING";
      case 0x0C0A:
        return "WRITE ERROR - PADDING BLOCKS ADDED";
      case 0x0C0B:
        return "AUXILIARY MEMORY WRITE ERROR";
      case 0x0C0C:
        return "WRITE ERROR - UNEXPECTED UNSOLICITED DATA";
      case 0x0C0D:
        return "WRITE ERROR - NOT ENOUGH UNSOLICITED DATA";
      case 0x0C0E:
        return "MULTIPLE WRITE ERRORS";
      case 0x0C0F:
        return "DEFECTS IN ERROR WINDOW";
      case 0x0C10:
        return "INCOMPLETE MULTIPLE ATOMIC WRITE OPERATIONS";
      case 0x0C11:
        return "WRITE ERROR - RECOVERY SCAN NEEDED";
      case 0x0C12:
        return "WRITE ERROR - INSUFFICIENT ZONE RESOURCES";
      case 0x0D00:
        return "ERROR DETECTED BY THIRD PARTY TEMPORARY INITIATOR";
      case 0x0D01:
        return "THIRD PARTY DEVICE FAILURE";
      case 0x0D02:
        return "COPY TARGET DEVICE NOT REACHABLE";
      case 0x0D03:
        return "INCORRECT COPY TARGET DEVICE TYPE";
      case 0x0D04:
        return "COPY TARGET DEVICE DATA UNDERRUN";
      case 0x0D05:
        return "COPY TARGET DEVICE DATA OVERRUN";
      case 0x0E00:
        return "INVALID INFORMATION UNIT";
      case 0x0E01:
        return "INFORMATION UNIT TOO SHORT";
      case 0x0E02:
        return "INFORMATION UNIT TOO LONG";
      case 0x0E03:
        return "INVALID FIELD IN COMMAND INFORMATION UNIT";
      case 0x0F00:
        return "10h 00h ID CRC OR ECC ERROR";
      case 0x1001:
        return "LOGICAL BLOCK GUARD CHECK FAILED";
      case 0x1002:
        return "LOGICAL BLOCK APPLICATION TAG CHECK FAILED";
      case 0x1003:
        return "LOGICAL BLOCK REFERENCE TAG CHECK FAILED";
      case 0x1004:
        return "LOGICAL BLOCK PROTECTION ERROR ON RECOVER BUFFERED DATA";
      case 0x1005:
        return "LOGICAL BLOCK PROTECTION METHOD ERROR";
      case 0x1100:
        return "UNRECOVERED READ ERROR";
      case 0x1101:
        return "READ RETRIES EXHAUSTED";
      case 0x1102:
        return "ERROR TOO LONG TO CORRECT";
      case 0x1103:
        return "MULTIPLE READ ERRORS";
      case 0x1104:
        return "UNRECOVERED READ ERROR - AUTO REALLOCATE FAILED";
      case 0x1105:
        return "L-EC UNCORRECTABLE ERROR";
      case 0x1106:
        return "CIRC UNRECOVERED ERROR";
      case 0x1107:
        return "DATA RE-SYNCHRONIZATION ERROR";
      case 0x1108:
        return "INCOMPLETE BLOCK READ";
      case 0x1109:
        return "NO GAP FOUND";
      case 0x110A:
        return "MISCORRECTED ERROR";
      case 0x110B:
        return "UNRECOVERED READ ERROR - RECOMMEND REASSIGNMENT";
      case 0x110C:
        return "UNRECOVERED READ ERROR - RECOMMEND REWRITE THE DATA";
      case 0x110D:
        return "DE-COMPRESSION CRC ERROR";
      case 0x110E:
        return "CANNOT DECOMPRESS USING DECLARED ALGORITHM";
      case 0x110F:
        return "ERROR READING UPC/EAN NUMBER";
      case 0x1110:
        return "ERROR READING ISRC NUMBER";
      case 0x1111:
        return "READ ERROR - LOSS OF STREAMING";
      case 0x1112:
        return "AUXILIARY MEMORY READ ERROR";
      case 0x1113:
        return "READ ERROR - FAILED RETRANSMISSION REQUEST";
      case 0x1114:
        return "READ ERROR - LBA MARKED BAD BY APPLICATION CLIENT";
      case 0x1115:
        return "WRITE AFTER SANITIZE REQUIRED";
      case 0x1200:
        return "ADDRESS MARK NOT FOUND FOR ID FIELD";
      case 0x1300:
        return "ADDRESS MARK NOT FOUND FOR DATA FIELD";
      case 0x1400:
        return "RECORDED ENTITY NOT FOUND";
      case 0x1401:
        return "RECORD NOT FOUND";
      case 0x1402:
        return "FILEMARK OR SETMARK NOT FOUND";
      case 0x1403:
        return "END-OF-DATA NOT FOUND";
      case 0x1404:
        return "BLOCK SEQUENCE ERROR";
      case 0x1405:
        return "RECORD NOT FOUND - RECOMMEND REASSIGNMENT";
      case 0x1406:
        return "RECORD NOT FOUND - DATA AUTO-REALLOCATED";
      case 0x1407:
        return "LOCATE OPERATION FAILURE";
      case 0x1500:
        return "RANDOM POSITIONING ERROR";
      case 0x1501:
        return "MECHANICAL POSITIONING ERROR";
      case 0x1502:
        return "POSITIONING ERROR DETECTED BY READ OF MEDIUM";
      case 0x1600:
        return "DATA SYNCHRONIZATION MARK ERROR";
      case 0x1601:
        return "DATA SYNC ERROR - DATA REWRITTEN";
      case 0x1602:
        return "DATA SYNC ERROR - RECOMMEND REWRITE";
      case 0x1603:
        return "DATA SYNC ERROR - DATA AUTO-REALLOCATED";
      case 0x1604:
        return "DATA SYNC ERROR - RECOMMEND REASSIGNMENT";
      case 0x1700:
        return "RECOVERED DATA WITH NO ERROR CORRECTION APPLIED";
      case 0x1701:
        return "RECOVERED DATA WITH RETRIES";
      case 0x1702:
        return "RECOVERED DATA WITH POSITIVE HEAD OFFSET";
      case 0x1703:
        return "RECOVERED DATA WITH NEGATIVE HEAD OFFSET";
      case 0x1704:
        return "RECOVERED DATA WITH RETRIES AND/OR CIRC APPLIED";
      case 0x1705:
        return "RECOVERED DATA USING PREVIOUS SECTOR ID";
      case 0x1706:
        return "RECOVERED DATA WITHOUT ECC - DATA AUTO-REALLOCATED";
      case 0x1707:
        return "RECOVERED DATA WITHOUT ECC - RECOMMEND REASSIGNMENT";
      case 0x1708:
        return "RECOVERED DATA WITHOUT ECC - RECOMMEND REWRITE";
      case 0x1709:
        return "RECOVERED DATA WITHOUT ECC - DATA REWRITTEN";
      case 0x1800:
        return "RECOVERED DATA WITH ERROR CORRECTION APPLIED";
      case 0x1801:
        return "RECOVERED DATA WITH ERROR CORR. & RETRIES APPLIED";
      case 0x1802:
        return "RECOVERED DATA - DATA AUTO-REALLOCATED";
      case 0x1803:
        return "RECOVERED DATA WITH CIRC";
      case 0x1804:
        return "RECOVERED DATA WITH L-EC";
      case 0x1805:
        return "RECOVERED DATA - RECOMMEND REASSIGNMENT";
      case 0x1806:
        return "RECOVERED DATA - RECOMMEND REWRITE";
      case 0x1807:
        return "RECOVERED DATA WITH ECC - DATA REWRITTEN";
      case 0x1808:
        return "RECOVERED DATA WITH LINKING";
      case 0x1900:
        return "DEFECT LIST ERROR";
      case 0x1901:
        return "DEFECT LIST NOT AVAILABLE";
      case 0x1902:
        return "DEFECT LIST ERROR IN PRIMARY LIST";
      case 0x1903:
        return "DEFECT LIST ERROR IN GROWN LIST";
      case 0x1A00:
        return "PARAMETER LIST LENGTH ERROR";
      case 0x1B00:
        return "SYNCHRONOUS DATA TRANSFER ERROR";
      case 0x1C00:
        return "DEFECT LIST NOT FOUND";
      case 0x1C01:
        return "PRIMARY DEFECT LIST NOT FOUND";
      case 0x1C02:
        return "GROWN DEFECT LIST NOT FOUND";
      case 0x1D00:
        return "MISCOMPARE DURING VERIFY OPERATION";
      case 0x1D01:
        return "MISCOMPARE VERIFY OF UNMAPPED LBA";
      case 0x1E00:
        return "RECOVERED ID WITH ECC CORRECTION";
      case 0x1F00:
        return "PARTIAL DEFECT LIST TRANSFER";
      case 0x2000:
        return "INVALID COMMAND OPERATION CODE";
      case 0x2001:
        return "ACCESS DENIED - INITIATOR PENDING-ENROLLED";
      case 0x2002:
        return "ACCESS DENIED - NO ACCESS RIGHTS";
      case 0x2003:
        return "ACCESS DENIED - INVALID MGMT ID KEY";
      case 0x2004:
        return "ILLEGAL COMMAND WHILE IN WRITE CAPABLE STATE";
      case 0x2005:
        return "Obsolete";
      case 0x2006:
        return "ILLEGAL COMMAND WHILE IN EXPLICIT ADDRESS MODE";
      case 0x2007:
        return "ILLEGAL COMMAND WHILE IN IMPLICIT ADDRESS MODE";
      case 0x2008:
        return "ACCESS DENIED - ENROLLMENT CONFLICT";
      case 0x2009:
        return "ACCESS DENIED - INVALID LU IDENTIFIER";
      case 0x200A:
        return "ACCESS DENIED - INVALID PROXY TOKEN";
      case 0x200B:
        return "ACCESS DENIED - ACL LUN CONFLICT";
      case 0x200C:
        return "ILLEGAL COMMAND WHEN NOT IN APPEND-ONLY MODE";
      case 0x200D:
        return "NOT AN ADMINISTRATIVE LOGICAL UNIT";
      case 0x200E:
        return "NOT A SUBSIDIARY LOGICAL UNIT";
      case 0x200F:
        return "NOT A CONGLOMERATE LOGICAL UNIT";
      case 0x2100:
        return "LOGICAL BLOCK ADDRESS OUT OF RANGE";
      case 0x2101:
        return "INVALID ELEMENT ADDRESS";
      case 0x2102:
        return "INVALID ADDRESS FOR WRITE";
      case 0x2103:
        return "INVALID WRITE CROSSING LAYER JUMP";
      case 0x2104:
        return "UNALIGNED WRITE COMMAND";
      case 0x2105:
        return "WRITE BOUNDARY VIOLATION";
      case 0x2106:
        return "ATTEMPT TO READ INVALID DATA";
      case 0x2107:
        return "READ BOUNDARY VIOLATION";
      case 0x2108:
        return "MISALIGNED WRITE COMMAND";
      case 0x2200:
        return "ILLEGAL FUNCTION (USE 20 00, 24 00, OR 26 00)";
      case 0x2300:
        return "INVALID TOKEN OPERATION, CAUSE NOT REPORTABLE";
      case 0x2301:
        return "INVALID TOKEN OPERATION, UNSUPPORTED TOKEN TYPE";
      case 0x2302:
        return "INVALID TOKEN OPERATION, REMOTE TOKEN USAGE NOT SUPPORTED";
      case 0x2303:
        return "INVALID TOKEN OPERATION, REMOTE ROD TOKEN CREATION NOT SUPPORTED";
      case 0x2304:
        return "INVALID TOKEN OPERATION, TOKEN UNKNOWN";
      case 0x2305:
        return "INVALID TOKEN OPERATION, TOKEN CORRUPT";
      case 0x2306:
        return "INVALID TOKEN OPERATION, TOKEN REVOKED";
      case 0x2307:
        return "INVALID TOKEN OPERATION, TOKEN EXPIRED";
      case 0x2308:
        return "INVALID TOKEN OPERATION, TOKEN CANCELLED";
      case 0x2309:
        return "INVALID TOKEN OPERATION, TOKEN DELETED";
      case 0x230A:
        return "INVALID TOKEN OPERATION, INVALID TOKEN LENGTH";
      case 0x2400:
        return "INVALID FIELD IN CDB";
      case 0x2401:
        return "CDB DECRYPTION ERROR";
      case 0x2402:
        return "Obsolete";
      case 0x2403:
        return "Obsolete";
      case 0x2404:
        return "SECURITY AUDIT VALUE FROZEN";
      case 0x2405:
        return "SECURITY WORKING KEY FROZEN";
      case 0x2406:
        return "NONCE NOT UNIQUE";
      case 0x2407:
        return "NONCE TIMESTAMP OUT OF RANGE";
      case 0x2408:
        return "INVALID XCDB";
      case 0x2500:
        return "LOGICAL UNIT NOT SUPPORTED";
      case 0x2600:
        return "INVALID FIELD IN PARAMETER LIST";
      case 0x2601:
        return "PARAMETER NOT SUPPORTED";
      case 0x2602:
        return "PARAMETER VALUE INVALID";
      case 0x2603:
        return "THRESHOLD PARAMETERS NOT SUPPORTED";
      case 0x2604:
        return "INVALID RELEASE OF PERSISTENT RESERVATION";
      case 0x2605:
        return "DATA DECRYPTION ERROR";
      case 0x2606:
        return "TOO MANY TARGET DESCRIPTORS";
      case 0x2607:
        return "UNSUPPORTED TARGET DESCRIPTOR TYPE CODE";
      case 0x2608:
        return "TOO MANY SEGMENT DESCRIPTORS";
      case 0x2609:
        return "UNSUPPORTED SEGMENT DESCRIPTOR TYPE CODE";
      case 0x260A:
        return "UNEXPECTED INEXACT SEGMENT";
      case 0x260B:
        return "INLINE DATA LENGTH EXCEEDED";
      case 0x260C:
        return "INVALID OPERATION FOR COPY SOURCE OR DESTINATION";
      case 0x260D:
        return "COPY SEGMENT GRANULARITY VIOLATION";
      case 0x260E:
        return "INVALID PARAMETER WHILE PORT IS ENABLED";
      case 0x260F:
        return "INVALID DATA-OUT BUFFER INTEGRITY CHECK VALUE";
      case 0x2610:
        return "DATA DECRYPTION KEY FAIL LIMIT REACHED";
      case 0x2611:
        return "INCOMPLETE KEY-ASSOCIATED DATA SET";
      case 0x2612:
        return "VENDOR SPECIFIC KEY REFERENCE NOT FOUND";
      case 0x2613:
        return "APPLICATION TAG MODE PAGE IS INVALID";
      case 0x2700:
        return "WRITE PROTECTED";
      case 0x2701:
        return "HARDWARE WRITE PROTECTED";
      case 0x2702:
        return "LOGICAL UNIT SOFTWARE WRITE PROTECTED";
      case 0x2703:
        return "ASSOCIATED WRITE PROTECT";
      case 0x2704:
        return "PERSISTENT WRITE PROTECT";
      case 0x2705:
        return "PERMANENT WRITE PROTECT";
      case 0x2706:
        return "CONDITIONAL WRITE PROTECT";
      case 0x2707:
        return "SPACE ALLOCATION FAILED WRITE PROTECT";
      case 0x2708:
        return "ZONE IS READ ONLY";
      case 0x2800:
        return "NOT READY TO READY CHANGE, MEDIUM MAY HAVE CHANGED";
      case 0x2801:
        return "IMPORT OR EXPORT ELEMENT ACCESSED";
      case 0x2802:
        return "FORMAT-LAYER MAY HAVE CHANGED";
      case 0x2803:
        return "IMPORT/EXPORT ELEMENT ACCESSED, MEDIUM CHANGED";
      case 0x2900:
        return "POWER ON, RESET, OR BUS DEVICE RESET OCCURRED";
      case 0x2901:
        return "POWER ON OCCURRED";
      case 0x2902:
        return "SCSI BUS RESET OCCURRED";
      case 0x2903:
        return "BUS DEVICE RESET FUNCTION OCCURRED";
      case 0x2904:
        return "DEVICE INTERNAL RESET";
      case 0x2905:
        return "TRANSCEIVER MODE CHANGED TO SINGLE-ENDED";
      case 0x2906:
        return "TRANSCEIVER MODE CHANGED TO LVD";
      case 0x2907:
        return "I_T NEXUS LOSS OCCURRED";
      case 0x2A00:
        return "PARAMETERS CHANGED";
      case 0x2A01:
        return "MODE PARAMETERS CHANGED";
      case 0x2A02:
        return "LOG PARAMETERS CHANGED";
      case 0x2A03:
        return "RESERVATIONS PREEMPTED";
      case 0x2A04:
        return "RESERVATIONS RELEASED";
      case 0x2A05:
        return "REGISTRATIONS PREEMPTED";
      case 0x2A06:
        return "ASYMMETRIC ACCESS STATE CHANGED";
      case 0x2A07:
        return "IMPLICIT ASYMMETRIC ACCESS STATE TRANSITION FAILED";
      case 0x2A08:
        return "PRIORITY CHANGED";
      case 0x2A09:
        return "CAPACITY DATA HAS CHANGED";
      case 0x2A0A:
        return "ERROR HISTORY I_T NEXUS CLEARED";
      case 0x2A0B:
        return "ERROR HISTORY SNAPSHOT RELEASED";
      case 0x2A0C:
        return "ERROR RECOVERY ATTRIBUTES HAVE CHANGED";
      case 0x2A0D:
        return "DATA ENCRYPTION CAPABILITIES CHANGED";
      case 0x2A10:
        return "TIMESTAMP CHANGED";
      case 0x2A11:
        return "DATA ENCRYPTION PARAMETERS CHANGED BY ANOTHER I_T NEXUS";
      case 0x2A12:
        return "DATA ENCRYPTION PARAMETERS CHANGED BY VENDOR SPECIFIC EVENT";
      case 0x2A13:
        return "DATA ENCRYPTION KEY INSTANCE COUNTER HAS CHANGED";
      case 0x2A14:
        return "SA CREATION CAPABILITIES DATA HAS CHANGED";
      case 0x2A15:
        return "MEDIUM REMOVAL PREVENTION PREEMPTED";
      case 0x2A16:
        return "ZONE RESET WRITE POINTER RECOMMENDED";
      case 0x2B00:
        return "COPY CANNOT EXECUTE SINCE HOST CANNOT DISCONNECT";
      case 0x2C00:
        return "COMMAND SEQUENCE ERROR";
      case 0x2C01:
        return "TOO MANY WINDOWS SPECIFIED";
      case 0x2C02:
        return "INVALID COMBINATION OF WINDOWS SPECIFIED";
      case 0x2C03:
        return "CURRENT PROGRAM AREA IS NOT EMPTY";
      case 0x2C04:
        return "CURRENT PROGRAM AREA IS EMPTY";
      case 0x2C05:
        return "ILLEGAL POWER CONDITION REQUEST";
      case 0x2C06:
        return "PERSISTENT PREVENT CONFLICT";
      case 0x2C07:
        return "PREVIOUS BUSY STATUS";
      case 0x2C08:
        return "PREVIOUS TASK SET FULL STATUS";
      case 0x2C09:
        return "PREVIOUS RESERVATION CONFLICT STATUS";
      case 0x2C0A:
        return "PARTITION OR COLLECTION CONTAINS USER OBJECTS";
      case 0x2C0B:
        return "NOT RESERVED";
      case 0x2C0C:
        return "ORWRITE GENERATION DOES NOT MATCH";
      case 0x2C0D:
        return "RESET WRITE POINTER NOT ALLOWED";
      case 0x2C0E:
        return "ZONE IS OFFLINE";
      case 0x2C0F:
        return "STREAM NOT OPEN";
      case 0x2C10:
        return "UNWRITTEN DATA IN ZONE";
      case 0x2C11:
        return "DESCRIPTOR FORMAT SENSE DATA REQUIRED";
      case 0x2D00:
        return "OVERWRITE ERROR ON UPDATE IN PLACE";
      case 0x2E00:
        return "INSUFFICIENT TIME FOR OPERATION";
      case 0x2E01:
        return "COMMAND TIMEOUT BEFORE PROCESSING";
      case 0x2E02:
        return "COMMAND TIMEOUT DURING PROCESSING";
      case 0x2E03:
        return "COMMAND TIMEOUT DURING PROCESSING DUE TO ERROR RECOVERY";
      case 0x2F00:
        return "COMMANDS CLEARED BY ANOTHER INITIATOR";
      case 0x2F01:
        return "COMMANDS CLEARED BY POWER LOSS NOTIFICATION";
      case 0x2F02:
        return "COMMANDS CLEARED BY DEVICE SERVER";
      case 0x2F03:
        return "SOME COMMANDS CLEARED BY QUEUING LAYER EVENT";
      case 0x3000:
        return "INCOMPATIBLE MEDIUM INSTALLED";
      case 0x3001:
        return "CANNOT READ MEDIUM - UNKNOWN FORMAT";
      case 0x3002:
        return "CANNOT READ MEDIUM - INCOMPATIBLE FORMAT";
      case 0x3003:
        return "CLEANING CARTRIDGE INSTALLED";
      case 0x3004:
        return "CANNOT WRITE MEDIUM - UNKNOWN FORMAT";
      case 0x3005:
        return "CANNOT WRITE MEDIUM - INCOMPATIBLE FORMAT";
      case 0x3006:
        return "CANNOT FORMAT MEDIUM - INCOMPATIBLE MEDIUM";
      case 0x3007:
        return "CLEANING FAILURE";
      case 0x3008:
        return "CANNOT WRITE - APPLICATION CODE MISMATCH";
      case 0x3009:
        return "CURRENT SESSION NOT FIXATED FOR APPEND";
      case 0x300A:
        return "CLEANING REQUEST REJECTED";
      case 0x300C:
        return "WORM MEDIUM - OVERWRITE ATTEMPTED";
      case 0x300D:
        return "WORM MEDIUM - INTEGRITY CHECK";
      case 0x3010:
        return "MEDIUM NOT FORMATTED";
      case 0x3011:
        return "INCOMPATIBLE VOLUME TYPE";
      case 0x3012:
        return "INCOMPATIBLE VOLUME QUALIFIER";
      case 0x3013:
        return "CLEANING VOLUME EXPIRED";
      case 0x3100:
        return "MEDIUM FORMAT CORRUPTED";
      case 0x3101:
        return "FORMAT COMMAND FAILED";
      case 0x3102:
        return "ZONED FORMATTING FAILED DUE TO SPARE LINKING";
      case 0x3103:
        return "SANITIZE COMMAND FAILED";
      case 0x3200:
        return "NO DEFECT SPARE LOCATION AVAILABLE";
      case 0x3201:
        return "DEFECT LIST UPDATE FAILURE";
      case 0x3300:
        return "TAPE LENGTH ERROR";
      case 0x3400:
        return "ENCLOSURE FAILURE";
      case 0x3500:
        return "ENCLOSURE SERVICES FAILURE";
      case 0x3501:
        return "UNSUPPORTED ENCLOSURE FUNCTION";
      case 0x3502:
        return "ENCLOSURE SERVICES UNAVAILABLE";
      case 0x3503:
        return "ENCLOSURE SERVICES TRANSFER FAILURE";
      case 0x3504:
        return "ENCLOSURE SERVICES TRANSFER REFUSED";
      case 0x3505:
        return "ENCLOSURE SERVICES CHECKSUM ERROR";
      case 0x3600:
        return "RIBBON, INK, OR TONER FAILURE";
      case 0x3700:
        return "ROUNDED PARAMETER";
      case 0x3800:
        return "EVENT STATUS NOTIFICATION";
      case 0x3802:
        return "ESN - POWER MANAGEMENT CLASS EVENT";
      case 0x3804:
        return "ESN - MEDIA CLASS EVENT";
      case 0x3806:
        return "ESN - DEVICE BUSY CLASS EVENT";
      case 0x3807:
        return "THIN PROVISIONING SOFT THRESHOLD REACHED";
      case 0x3900:
        return "SAVING PARAMETERS NOT SUPPORTED";
      case 0x3A00:
        return "MEDIUM NOT PRESENT";
      case 0x3A01:
        return "MEDIUM NOT PRESENT - TRAY CLOSED";
      case 0x3A02:
        return "MEDIUM NOT PRESENT - TRAY OPEN";
      case 0x3A03:
        return "MEDIUM NOT PRESENT - LOADABLE";
      case 0x3A04:
        return "MEDIUM NOT PRESENT - MEDIUM AUXILIARY MEMORY ACCESSIBLE";
      case 0x3B00:
        return "SEQUENTIAL POSITIONING ERROR";
      case 0x3B01:
        return "TAPE POSITION ERROR AT BEGINNING-OF-MEDIUM";
      case 0x3B02:
        return "TAPE POSITION ERROR AT END-OF-MEDIUM";
      case 0x3B03:
        return "TAPE OR ELECTRONIC VERTICAL FORMS UNIT NOT READY";
      case 0x3B04:
        return "SLEW FAILURE";
      case 0x3B05:
        return "PAPER JAM";
      case 0x3B06:
        return "FAILED TO SENSE TOP-OF-FORM";
      case 0x3B07:
        return "FAILED TO SENSE BOTTOM-OF-FORM";
      case 0x3B08:
        return "REPOSITION ERROR";
      case 0x3B09:
        return "READ PAST END OF MEDIUM";
      case 0x3B0A:
        return "READ PAST BEGINNING OF MEDIUM";
      case 0x3B0B:
        return "POSITION PAST END OF MEDIUM";
      case 0x3B0C:
        return "POSITION PAST BEGINNING OF MEDIUM";
      case 0x3B0D:
        return "MEDIUM DESTINATION ELEMENT FULL";
      case 0x3B0E:
        return "MEDIUM SOURCE ELEMENT EMPTY";
      case 0x3B0F:
        return "END OF MEDIUM REACHED";
      case 0x3B11:
        return "MEDIUM MAGAZINE NOT ACCESSIBLE";
      case 0x3B12:
        return "MEDIUM MAGAZINE REMOVED";
      case 0x3B13:
        return "MEDIUM MAGAZINE INSERTED";
      case 0x3B14:
        return "MEDIUM MAGAZINE LOCKED";
      case 0x3B15:
        return "MEDIUM MAGAZINE UNLOCKED";
      case 0x3B16:
        return "MECHANICAL POSITIONING OR CHANGER ERROR";
      case 0x3B17:
        return "READ PAST END OF USER OBJECT";
      case 0x3B18:
        return "ELEMENT DISABLED";
      case 0x3B19:
        return "ELEMENT ENABLED";
      case 0x3B1A:
        return "DATA TRANSFER DEVICE REMOVED";
      case 0x3B1B:
        return "DATA TRANSFER DEVICE INSERTED";
      case 0x3B1C:
        return "TOO MANY LOGICAL OBJECTS ON PARTITION TO SUPPORT OPERATION";
      case 0x3C00:
        return "3Dh 00h INVALID BITS IN IDENTIFY MESSAGE";
      case 0x3E00:
        return "LOGICAL UNIT HAS NOT SELF-CONFIGURED YET";
      case 0x3E01:
        return "LOGICAL UNIT FAILURE";
      case 0x3E02:
        return "TIMEOUT ON LOGICAL UNIT";
      case 0x3E03:
        return "LOGICAL UNIT FAILED SELF-TEST";
      case 0x3E04:
        return "LOGICAL UNIT UNABLE TO UPDATE SELF-TEST LOG";
      case 0x3F00:
        return "TARGET OPERATING CONDITIONS HAVE CHANGED";
      case 0x3F01:
        return "MICROCODE HAS BEEN CHANGED";
      case 0x3F02:
        return "CHANGED OPERATING DEFINITION";
      case 0x3F03:
        return "INQUIRY DATA HAS CHANGED";
      case 0x3F04:
        return "COMPONENT DEVICE ATTACHED";
      case 0x3F05:
        return "DEVICE IDENTIFIER CHANGED";
      case 0x3F06:
        return "REDUNDANCY GROUP CREATED OR MODIFIED";
      case 0x3F07:
        return "REDUNDANCY GROUP DELETED";
      case 0x3F08:
        return "SPARE CREATED OR MODIFIED";
      case 0x3F09:
        return "SPARE DELETED";
      case 0x3F0A:
        return "VOLUME SET CREATED OR MODIFIED";
      case 0x3F0B:
        return "VOLUME SET DELETED";
      case 0x3F0C:
        return "VOLUME SET DEASSIGNED";
      case 0x3F0D:
        return "VOLUME SET REASSIGNED";
      case 0x3F0E:
        return "REPORTED LUNS DATA HAS CHANGED";
      case 0x3F0F:
        return "ECHO BUFFER OVERWRITTEN";
      case 0x3F10:
        return "MEDIUM LOADABLE";
      case 0x3F11:
        return "MEDIUM AUXILIARY MEMORY ACCESSIBLE";
      case 0x3F12:
        return "iSCSI IP ADDRESS ADDED";
      case 0x3F13:
        return "iSCSI IP ADDRESS REMOVED";
      case 0x3F14:
        return "iSCSI IP ADDRESS CHANGED";
      case 0x3F15:
        return "INSPECT REFERRALS SENSE DESCRIPTORS";
      case 0x3F16:
        return "MICROCODE HAS BEEN CHANGED WITHOUT RESET";
      case 0x3F17:
        return "ZONE TRANSITION TO FULL";
      case 0x3F18:
        return "BIND COMPLETED";
      case 0x3F19:
        return "BIND REDIRECTED";
      case 0x3F1A:
        return "SUBSIDIARY BINDING CHANGED";
      case 0x4000:
        return "RAM FAILURE (SHOULD USE 40 NN)";
      case 0x4100:
        return "DATA PATH FAILURE (SHOULD USE 40 NN)";
      case 0x4200:
        return "POWER-ON OR SELF-TEST FAILURE (SHOULD USE 40 NN)";
      case 0x4300:
        return "MESSAGE ERROR";
      case 0x4400:
        return "INTERNAL TARGET FAILURE";
      case 0x4401:
        return "PERSISTENT RESERVATION INFORMATION LOST";
      case 0x4471:
        return "ATA DEVICE FAILED SET FEATURES";
      case 0x4500:
        return "SELECT OR RESELECT FAILURE";
      case 0x4600:
        return "UNSUCCESSFUL SOFT RESET";
      case 0x4700:
        return "SCSI PARITY ERROR";
      case 0x4701:
        return "DATA PHASE CRC ERROR DETECTED";
      case 0x4702:
        return "SCSI PARITY ERROR DETECTED DURING ST DATA PHASE";
      case 0x4703:
        return "INFORMATION UNIT iuCRC ERROR DETECTED";
      case 0x4704:
        return "ASYNCHRONOUS INFORMATION PROTECTION ERROR DETECTED";
      case 0x4705:
        return "PROTOCOL SERVICE CRC ERROR";
      case 0x4706:
        return "PHY TEST FUNCTION IN PROGRESS";
      case 0x477F:
        return "SOME COMMANDS CLEARED BY ISCSI PROTOCOL EVENT";
      case 0x4800:
        return "INITIATOR DETECTED ERROR MESSAGE RECEIVED";
      case 0x4900:
        return "INVALID MESSAGE ERROR";
      case 0x4A00:
        return "COMMAND PHASE ERROR";
      case 0x4B00:
        return "DATA PHASE ERROR";
      case 0x4B01:
        return "INVALID TARGET PORT TRANSFER TAG RECEIVED";
      case 0x4B02:
        return "TOO MUCH WRITE DATA";
      case 0x4B03:
        return "ACK/NAK TIMEOUT";
      case 0x4B04:
        return "NAK RECEIVED";
      case 0x4B05:
        return "DATA OFFSET ERROR";
      case 0x4B06:
        return "INITIATOR RESPONSE TIMEOUT";
      case 0x4B07:
        return "CONNECTION LOST";
      case 0x4B08:
        return "DATA-IN BUFFER OVERFLOW - DATA BUFFER SIZE";
      case 0x4B09:
        return "DATA-IN BUFFER OVERFLOW - DATA BUFFER DESCRIPTOR AREA";
      case 0x4B0A:
        return "DATA-IN BUFFER ERROR";
      case 0x4B0B:
        return "DATA-OUT BUFFER OVERFLOW - DATA BUFFER SIZE";
      case 0x4B0C:
        return "DATA-OUT BUFFER OVERFLOW - DATA BUFFER DESCRIPTOR AREA";
      case 0x4B0D:
        return "DATA-OUT BUFFER ERROR";
      case 0x4B0E:
        return "PCIE FABRIC ERROR";
      case 0x4B0F:
        return "PCIE COMPLETION TIMEOUT";
      case 0x4B10:
        return "PCIE COMPLETER ABORT";
      case 0x4B11:
        return "PCIE POISONED TLP RECEIVED";
      case 0x4B12:
        return "PCIE ECRC CHECK FAILED";
      case 0x4B13:
        return "PCIE UNSUPPORTED REQUEST";
      case 0x4B14:
        return "PCIE ACS VIOLATION";
      case 0x4B15:
        return "PCIE TLP PREFIX BLOCKED";
      case 0x4C00:
        return "LOGICAL UNIT FAILED SELF-CONFIGURATION";
      case 0x4E00:
        return "OVERLAPPED COMMANDS ATTEMPTED";
      case 0x4F00:
        return "50h 00h WRITE APPEND ERROR";
      case 0x5001:
        return "WRITE APPEND POSITION ERROR";
      case 0x5002:
        return "POSITION ERROR RELATED TO TIMING";
      case 0x5100:
        return "ERASE FAILURE";
      case 0x5101:
        return "ERASE FAILURE - INCOMPLETE ERASE OPERATION DETECTED";
      case 0x5200:
        return "CARTRIDGE FAULT";
      case 0x5300:
        return "MEDIA LOAD OR EJECT FAILED";
      case 0x5301:
        return "UNLOAD TAPE FAILURE";
      case 0x5302:
        return "MEDIUM REMOVAL PREVENTED";
      case 0x5303:
        return "MEDIUM REMOVAL PREVENTED BY DATA TRANSFER ELEMENT";
      case 0x5304:
        return "MEDIUM THREAD OR UNTHREAD FAILURE";
      case 0x5305:
        return "VOLUME IDENTIFIER INVALID";
      case 0x5306:
        return "VOLUME IDENTIFIER MISSING";
      case 0x5307:
        return "DUPLICATE VOLUME IDENTIFIER";
      case 0x5308:
        return "ELEMENT STATUS UNKNOWN";
      case 0x5309:
        return "DATA TRANSFER DEVICE ERROR - LOAD FAILED";
      case 0x530A:
        return "DATA TRANSFER DEVICE ERROR - UNLOAD FAILED";
      case 0x530B:
        return "DATA TRANSFER DEVICE ERROR - UNLOAD MISSING";
      case 0x530C:
        return "DATA TRANSFER DEVICE ERROR - EJECT FAILED";
      case 0x530D:
        return "DATA TRANSFER DEVICE ERROR - LIBRARY COMMUNICATION FAILED";
      case 0x5400:
        return "SCSI TO HOST SYSTEM INTERFACE FAILURE";
      case 0x5500:
        return "SYSTEM RESOURCE FAILURE";
      case 0x5501:
        return "SYSTEM BUFFER FULL";
      case 0x5502:
        return "INSUFFICIENT RESERVATION RESOURCES";
      case 0x5503:
        return "INSUFFICIENT RESOURCES";
      case 0x5504:
        return "INSUFFICIENT REGISTRATION RESOURCES";
      case 0x5505:
        return "INSUFFICIENT ACCESS CONTROL RESOURCES";
      case 0x5506:
        return "AUXILIARY MEMORY OUT OF SPACE";
      case 0x5507:
        return "QUOTA ERROR";
      case 0x5508:
        return "MAXIMUM NUMBER OF SUPPLEMENTAL DECRYPTION KEYS EXCEEDED";
      case 0x5509:
        return "MEDIUM AUXILIARY MEMORY NOT ACCESSIBLE";
      case 0x550A:
        return "DATA CURRENTLY UNAVAILABLE";
      case 0x550B:
        return "INSUFFICIENT POWER FOR OPERATION";
      case 0x550C:
        return "INSUFFICIENT RESOURCES TO CREATE ROD";
      case 0x550D:
        return "INSUFFICIENT RESOURCES TO CREATE ROD TOKEN";
      case 0x550E:
        return "INSUFFICIENT ZONE RESOURCES";
      case 0x550F:
        return "INSUFFICIENT ZONE RESOURCES TO COMPLETE WRITE";
      case 0x5510:
        return "MAXIMUM NUMBER OF STREAMS OPEN";
      case 0x5511:
        return "INSUFFICIENT RESOURCES TO BIND";
      case 0x5600:
        return "57h 00h UNABLE TO RECOVER TABLE-OF-CONTENTS";
      case 0x5800:
        return "GENERATION DOES NOT EXIST";
      case 0x5900:
        return "UPDATED BLOCK READ";
      case 0x5A00:
        return "OPERATOR REQUEST OR STATE CHANGE INPUT";
      case 0x5A01:
        return "OPERATOR MEDIUM REMOVAL REQUEST";
      case 0x5A02:
        return "OPERATOR SELECTED WRITE PROTECT";
      case 0x5A03:
        return "OPERATOR SELECTED WRITE PERMIT";
      case 0x5B00:
        return "LOG EXCEPTION";
      case 0x5B01:
        return "THRESHOLD CONDITION MET";
      case 0x5B02:
        return "LOG COUNTER AT MAXIMUM";
      case 0x5B03:
        return "LOG LIST CODES EXHAUSTED";
      case 0x5C00:
        return "RPL STATUS CHANGE";
      case 0x5C01:
        return "SPINDLES SYNCHRONIZED";
      case 0x5C02:
        return "SPINDLES NOT SYNCHRONIZED";
      case 0x5D00:
        return "FAILURE PREDICTION THRESHOLD EXCEEDED";
      case 0x5D01:
        return "MEDIA FAILURE PREDICTION THRESHOLD EXCEEDED";
      case 0x5D02:
        return "LOGICAL UNIT FAILURE PREDICTION THRESHOLD EXCEEDED";
      case 0x5D03:
        return "SPARE AREA EXHAUSTION PREDICTION THRESHOLD EXCEEDED";
      case 0x5D10:
        return "HARDWARE IMPENDING FAILURE GENERAL HARD DRIVE FAILURE";
      case 0x5D11:
        return "HARDWARE IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH";
      case 0x5D12:
        return "HARDWARE IMPENDING FAILURE DATA ERROR RATE TOO HIGH";
      case 0x5D13:
        return "HARDWARE IMPENDING FAILURE SEEK ERROR RATE TOO HIGH";
      case 0x5D14:
        return "HARDWARE IMPENDING FAILURE TOO MANY BLOCK REASSIGNS";
      case 0x5D15:
        return "HARDWARE IMPENDING FAILURE ACCESS TIMES TOO HIGH";
      case 0x5D16:
        return "HARDWARE IMPENDING FAILURE START UNIT TIMES TOO HIGH";
      case 0x5D17:
        return "HARDWARE IMPENDING FAILURE CHANNEL PARAMETRICS";
      case 0x5D18:
        return "HARDWARE IMPENDING FAILURE CONTROLLER DETECTED";
      case 0x5D19:
        return "HARDWARE IMPENDING FAILURE THROUGHPUT PERFORMANCE";
      case 0x5D1A:
        return "HARDWARE IMPENDING FAILURE SEEK TIME PERFORMANCE";
      case 0x5D1B:
        return "HARDWARE IMPENDING FAILURE SPIN-UP RETRY COUNT";
      case 0x5D1C:
        return "HARDWARE IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT";
      case 0x5D20:
        return "CONTROLLER IMPENDING FAILURE GENERAL HARD DRIVE FAILURE";
      case 0x5D21:
        return "CONTROLLER IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH";
      case 0x5D22:
        return "CONTROLLER IMPENDING FAILURE DATA ERROR RATE TOO HIGH";
      case 0x5D23:
        return "CONTROLLER IMPENDING FAILURE SEEK ERROR RATE TOO HIGH";
      case 0x5D24:
        return "CONTROLLER IMPENDING FAILURE TOO MANY BLOCK REASSIGNS";
      case 0x5D25:
        return "CONTROLLER IMPENDING FAILURE ACCESS TIMES TOO HIGH";
      case 0x5D26:
        return "CONTROLLER IMPENDING FAILURE START UNIT TIMES TOO HIGH";
      case 0x5D27:
        return "CONTROLLER IMPENDING FAILURE CHANNEL PARAMETRICS";
      case 0x5D28:
        return "CONTROLLER IMPENDING FAILURE CONTROLLER DETECTED";
      case 0x5D29:
        return "CONTROLLER IMPENDING FAILURE THROUGHPUT PERFORMANCE";
      case 0x5D2A:
        return "CONTROLLER IMPENDING FAILURE SEEK TIME PERFORMANCE";
      case 0x5D2B:
        return "CONTROLLER IMPENDING FAILURE SPIN-UP RETRY COUNT";
      case 0x5D2C:
        return "CONTROLLER IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT";
      case 0x5D30:
        return "DATA CHANNEL IMPENDING FAILURE GENERAL HARD DRIVE FAILURE";
      case 0x5D31:
        return "DATA CHANNEL IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH";
      case 0x5D32:
        return "DATA CHANNEL IMPENDING FAILURE DATA ERROR RATE TOO HIGH";
      case 0x5D33:
        return "DATA CHANNEL IMPENDING FAILURE SEEK ERROR RATE TOO HIGH";
      case 0x5D34:
        return "DATA CHANNEL IMPENDING FAILURE TOO MANY BLOCK REASSIGNS";
      case 0x5D35:
        return "DATA CHANNEL IMPENDING FAILURE ACCESS TIMES TOO HIGH";
      case 0x5D36:
        return "DATA CHANNEL IMPENDING FAILURE START UNIT TIMES TOO HIGH";
      case 0x5D37:
        return "DATA CHANNEL IMPENDING FAILURE CHANNEL PARAMETRICS";
      case 0x5D38:
        return "DATA CHANNEL IMPENDING FAILURE CONTROLLER DETECTED";
      case 0x5D39:
        return "DATA CHANNEL IMPENDING FAILURE THROUGHPUT PERFORMANCE";
      case 0x5D3A:
        return "DATA CHANNEL IMPENDING FAILURE SEEK TIME PERFORMANCE";
      case 0x5D3B:
        return "DATA CHANNEL IMPENDING FAILURE SPIN-UP RETRY COUNT";
      case 0x5D3C:
        return "DATA CHANNEL IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT";
      case 0x5D40:
        return "SERVO IMPENDING FAILURE GENERAL HARD DRIVE FAILURE";
      case 0x5D41:
        return "SERVO IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH";
      case 0x5D42:
        return "SERVO IMPENDING FAILURE DATA ERROR RATE TOO HIGH";
      case 0x5D43:
        return "SERVO IMPENDING FAILURE SEEK ERROR RATE TOO HIGH";
      case 0x5D44:
        return "SERVO IMPENDING FAILURE TOO MANY BLOCK REASSIGNS";
      case 0x5D45:
        return "SERVO IMPENDING FAILURE ACCESS TIMES TOO HIGH";
      case 0x5D46:
        return "SERVO IMPENDING FAILURE START UNIT TIMES TOO HIGH";
      case 0x5D47:
        return "SERVO IMPENDING FAILURE CHANNEL PARAMETRICS";
      case 0x5D48:
        return "SERVO IMPENDING FAILURE CONTROLLER DETECTED";
      case 0x5D49:
        return "SERVO IMPENDING FAILURE THROUGHPUT PERFORMANCE";
      case 0x5D4A:
        return "SERVO IMPENDING FAILURE SEEK TIME PERFORMANCE";
      case 0x5D4B:
        return "SERVO IMPENDING FAILURE SPIN-UP RETRY COUNT";
      case 0x5D4C:
        return "SERVO IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT";
      case 0x5D50:
        return "SPINDLE IMPENDING FAILURE GENERAL HARD DRIVE FAILURE";
      case 0x5D51:
        return "SPINDLE IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH";
      case 0x5D52:
        return "SPINDLE IMPENDING FAILURE DATA ERROR RATE TOO HIGH";
      case 0x5D53:
        return "SPINDLE IMPENDING FAILURE SEEK ERROR RATE TOO HIGH";
      case 0x5D54:
        return "SPINDLE IMPENDING FAILURE TOO MANY BLOCK REASSIGNS";
      case 0x5D55:
        return "SPINDLE IMPENDING FAILURE ACCESS TIMES TOO HIGH";
      case 0x5D56:
        return "SPINDLE IMPENDING FAILURE START UNIT TIMES TOO HIGH";
      case 0x5D57:
        return "SPINDLE IMPENDING FAILURE CHANNEL PARAMETRICS";
      case 0x5D58:
        return "SPINDLE IMPENDING FAILURE CONTROLLER DETECTED";
      case 0x5D59:
        return "SPINDLE IMPENDING FAILURE THROUGHPUT PERFORMANCE";
      case 0x5D5A:
        return "SPINDLE IMPENDING FAILURE SEEK TIME PERFORMANCE";
      case 0x5D5B:
        return "SPINDLE IMPENDING FAILURE SPIN-UP RETRY COUNT";
      case 0x5D5C:
        return "SPINDLE IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT";
      case 0x5D60:
        return "FIRMWARE IMPENDING FAILURE GENERAL HARD DRIVE FAILURE";
      case 0x5D61:
        return "FIRMWARE IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH";
      case 0x5D62:
        return "FIRMWARE IMPENDING FAILURE DATA ERROR RATE TOO HIGH";
      case 0x5D63:
        return "FIRMWARE IMPENDING FAILURE SEEK ERROR RATE TOO HIGH";
      case 0x5D64:
        return "FIRMWARE IMPENDING FAILURE TOO MANY BLOCK REASSIGNS";
      case 0x5D65:
        return "FIRMWARE IMPENDING FAILURE ACCESS TIMES TOO HIGH";
      case 0x5D66:
        return "FIRMWARE IMPENDING FAILURE START UNIT TIMES TOO HIGH";
      case 0x5D67:
        return "FIRMWARE IMPENDING FAILURE CHANNEL PARAMETRICS";
      case 0x5D68:
        return "FIRMWARE IMPENDING FAILURE CONTROLLER DETECTED";
      case 0x5D69:
        return "FIRMWARE IMPENDING FAILURE THROUGHPUT PERFORMANCE";
      case 0x5D6A:
        return "FIRMWARE IMPENDING FAILURE SEEK TIME PERFORMANCE";
      case 0x5D6B:
        return "FIRMWARE IMPENDING FAILURE SPIN-UP RETRY COUNT";
      case 0x5D6C:
        return "FIRMWARE IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT";
      case 0x5DFF:
        return "FAILURE PREDICTION THRESHOLD EXCEEDED (FALSE)";
      case 0x5E00:
        return "LOW POWER CONDITION ON";
      case 0x5E01:
        return "IDLE CONDITION ACTIVATED BY TIMER";
      case 0x5E02:
        return "STANDBY CONDITION ACTIVATED BY TIMER";
      case 0x5E03:
        return "IDLE CONDITION ACTIVATED BY COMMAND";
      case 0x5E04:
        return "STANDBY CONDITION ACTIVATED BY COMMAND";
      case 0x5E05:
        return "IDLE_B CONDITION ACTIVATED BY TIMER";
      case 0x5E06:
        return "IDLE_B CONDITION ACTIVATED BY COMMAND";
      case 0x5E07:
        return "IDLE_C CONDITION ACTIVATED BY TIMER";
      case 0x5E08:
        return "IDLE_C CONDITION ACTIVATED BY COMMAND";
      case 0x5E09:
        return "STANDBY_Y CONDITION ACTIVATED BY TIMER";
      case 0x5E0A:
        return "STANDBY_Y CONDITION ACTIVATED BY COMMAND";
      case 0x5E41:
        return "POWER STATE CHANGE TO ACTIVE";
      case 0x5E42:
        return "POWER STATE CHANGE TO IDLE";
      case 0x5E43:
        return "POWER STATE CHANGE TO STANDBY";
      case 0x5E45:
        return "POWER STATE CHANGE TO SLEEP";
      case 0x5E47:
        return "POWER STATE CHANGE TO DEVICE CONTROL";
      case 0x5F00:
        return "60h 00h LAMP FAILURE";
      case 0x6100:
        return "VIDEO ACQUISITION ERROR";
      case 0x6101:
        return "UNABLE TO ACQUIRE VIDEO";
      case 0x6102:
        return "OUT OF FOCUS";
      case 0x6200:
        return "SCAN HEAD POSITIONING ERROR";
      case 0x6300:
        return "END OF USER AREA ENCOUNTERED ON THIS TRACK";
      case 0x6301:
        return "PACKET DOES NOT FIT IN AVAILABLE SPACE";
      case 0x6400:
        return "ILLEGAL MODE FOR THIS TRACK";
      case 0x6401:
        return "INVALID PACKET SIZE";
      case 0x6500:
        return "VOLTAGE FAULT";
      case 0x6600:
        return "AUTOMATIC DOCUMENT FEEDER COVER UP";
      case 0x6601:
        return "AUTOMATIC DOCUMENT FEEDER LIFT UP";
      case 0x6602:
        return "DOCUMENT JAM IN AUTOMATIC DOCUMENT FEEDER";
      case 0x6603:
        return "DOCUMENT MISS FEED AUTOMATIC IN DOCUMENT FEEDER";
      case 0x6700:
        return "CONFIGURATION FAILURE";
      case 0x6701:
        return "CONFIGURATION OF INCAPABLE LOGICAL UNITS FAILED";
      case 0x6702:
        return "ADD LOGICAL UNIT FAILED";
      case 0x6703:
        return "MODIFICATION OF LOGICAL UNIT FAILED";
      case 0x6704:
        return "EXCHANGE OF LOGICAL UNIT FAILED";
      case 0x6705:
        return "REMOVE OF LOGICAL UNIT FAILED";
      case 0x6706:
        return "ATTACHMENT OF LOGICAL UNIT FAILED";
      case 0x6707:
        return "CREATION OF LOGICAL UNIT FAILED";
      case 0x6708:
        return "ASSIGN FAILURE OCCURRED";
      case 0x6709:
        return "MULTIPLY ASSIGNED LOGICAL UNIT";
      case 0x670A:
        return "SET TARGET PORT GROUPS COMMAND FAILED";
      case 0x670B:
        return "ATA DEVICE FEATURE NOT ENABLED";
      case 0x670C:
        return "COMMAND REJECTED";
      case 0x670D:
        return "EXPLICIT BIND NOT ALLOWED";
      case 0x6800:
        return "LOGICAL UNIT NOT CONFIGURED";
      case 0x6801:
        return "SUBSIDIARY LOGICAL UNIT NOT CONFIGURED";
      case 0x6900:
        return "DATA LOSS ON LOGICAL UNIT";
      case 0x6901:
        return "MULTIPLE LOGICAL UNIT FAILURES";
      case 0x6902:
        return "PARITY/DATA MISMATCH";
      case 0x6A00:
        return "INFORMATIONAL, REFER TO LOG";
      case 0x6B00:
        return "STATE CHANGE HAS OCCURRED";
      case 0x6B01:
        return "REDUNDANCY LEVEL GOT BETTER";
      case 0x6B02:
        return "REDUNDANCY LEVEL GOT WORSE";
      case 0x6C00:
        return "REBUILD FAILURE OCCURRED";
      case 0x6D00:
        return "RECALCULATE FAILURE OCCURRED";
      case 0x6E00:
        return "COMMAND TO LOGICAL UNIT FAILED";
      case 0x6F00:
        return "COPY PROTECTION KEY EXCHANGE FAILURE - AUTHENTICATION FAILURE";
      case 0x6F01:
        return "COPY PROTECTION KEY EXCHANGE FAILURE - KEY NOT PRESENT";
      case 0x6F02:
        return "COPY PROTECTION KEY EXCHANGE FAILURE - KEY NOT ESTABLISHED";
      case 0x6F03:
        return "READ OF SCRAMBLED SECTOR WITHOUT AUTHENTICATION";
      case 0x6F04:
        return "MEDIA REGION CODE IS MISMATCHED TO LOGICAL UNIT REGION";
      case 0x6F05:
        return "DRIVE REGION MUST BE PERMANENT/REGION RESET COUNT ERROR";
      case 0x6F06:
        return "INSUFFICIENT BLOCK COUNT FOR BINDING NONCE RECORDING";
      case 0x6F07:
        return "CONFLICT IN BINDING NONCE RECORDING";
      case 0x6F08:
        return "INSUFFICIENT PERMISSION";
      case 0x6F09:
        return "INVALID DRIVE-HOST PAIRING SERVER";
      case 0x6F0A:
        return "DRIVE-HOST PAIRING SUSPENDED";
      case 0x7100:
        return "DECOMPRESSION EXCEPTION LONG ALGORITHM ID";
      case 0x7200:
        return "SESSION FIXATION ERROR";
      case 0x7201:
        return "SESSION FIXATION ERROR WRITING LEAD-IN";
      case 0x7202:
        return "SESSION FIXATION ERROR WRITING LEAD-OUT";
      case 0x7203:
        return "SESSION FIXATION ERROR - INCOMPLETE TRACK IN SESSION";
      case 0x7204:
        return "EMPTY OR PARTIALLY WRITTEN RESERVED TRACK";
      case 0x7205:
        return "NO MORE TRACK RESERVATIONS ALLOWED";
      case 0x7206:
        return "RMZ EXTENSION IS NOT ALLOWED";
      case 0x7207:
        return "NO MORE TEST ZONE EXTENSIONS ARE ALLOWED";
      case 0x7300:
        return "CD CONTROL ERROR";
      case 0x7301:
        return "POWER CALIBRATION AREA ALMOST FULL";
      case 0x7302:
        return "POWER CALIBRATION AREA IS FULL";
      case 0x7303:
        return "POWER CALIBRATION AREA ERROR";
      case 0x7304:
        return "PROGRAM MEMORY AREA UPDATE FAILURE";
      case 0x7305:
        return "PROGRAM MEMORY AREA IS FULL";
      case 0x7306:
        return "RMA/PMA IS ALMOST FULL";
      case 0x7310:
        return "CURRENT POWER CALIBRATION AREA ALMOST FULL";
      case 0x7311:
        return "CURRENT POWER CALIBRATION AREA IS FULL";
      case 0x7317:
        return "RDZ IS FULL";
      case 0x7400:
        return "SECURITY ERROR";
      case 0x7401:
        return "UNABLE TO DECRYPT DATA";
      case 0x7402:
        return "UNENCRYPTED DATA ENCOUNTERED WHILE DECRYPTING";
      case 0x7403:
        return "INCORRECT DATA ENCRYPTION KEY";
      case 0x7404:
        return "CRYPTOGRAPHIC INTEGRITY VALIDATION FAILED";
      case 0x7405:
        return "ERROR DECRYPTING DATA";
      case 0x7406:
        return "UNKNOWN SIGNATURE VERIFICATION KEY";
      case 0x7407:
        return "ENCRYPTION PARAMETERS NOT USEABLE";
      case 0x7408:
        return "DIGITAL SIGNATURE VALIDATION FAILURE";
      case 0x7409:
        return "ENCRYPTION MODE MISMATCH ON READ";
      case 0x740A:
        return "ENCRYPTED BLOCK NOT RAW READ ENABLED";
      case 0x740B:
        return "INCORRECT ENCRYPTION PARAMETERS";
      case 0x740C:
        return "UNABLE TO DECRYPT PARAMETER LIST";
      case 0x740D:
        return "ENCRYPTION ALGORITHM DISABLED";
      case 0x7410:
        return "SA CREATION PARAMETER VALUE INVALID";
      case 0x7411:
        return "SA CREATION PARAMETER VALUE REJECTED";
      case 0x7412:
        return "INVALID SA USAGE";
      case 0x7421:
        return "DATA ENCRYPTION CONFIGURATION PREVENTED";
      case 0x7430:
        return "SA CREATION PARAMETER NOT SUPPORTED";
      case 0x7440:
        return "AUTHENTICATION FAILED";
      case 0x7461:
        return "EXTERNAL DATA ENCRYPTION KEY MANAGER ACCESS ERROR";
      case 0x7462:
        return "EXTERNAL DATA ENCRYPTION KEY MANAGER ERROR";
      case 0x7463:
        return "EXTERNAL DATA ENCRYPTION KEY NOT FOUND";
      case 0x7464:
        return "EXTERNAL DATA ENCRYPTION REQUEST NOT AUTHORIZED";
      case 0x746E:
        return "EXTERNAL DATA ENCRYPTION CONTROL TIMEOUT";
      case 0x746F:
        return "EXTERNAL DATA ENCRYPTION CONTROL ERROR";
      case 0x7471:
        return "LOGICAL UNIT ACCESS NOT AUTHORIZED";
      case 0x7479:
        return "SECURITY CONFLICT IN TRANSLATED DEVICE";
      // Other Values
      default:
        if (asc == 0x40) {
          return $"DIAGNOSTIC FAILURE ON COMPONENT {ascq:X2}h";
        }
        if (asc == 0x4D) {
          return $"TAGGED OVERLAPPED COMMANDS (TAG: {ascq:X2}h)";
        }
        if (asc == 0x70) {
          return $"DECOMPRESSION EXCEPTION (SHORT ALGORITHM ID: {ascq:X2}h)";
        }
        if (asc >= 0x80) {
          return $"VENDOR-SPECIFIC CODE (ASC: {asc:X2}h, ASCQ: {ascq:X2}h)";
        }
        if (ascq >= 0x80) {
          return $"VENDOR-SPECIFIC QUALIFIER (ASC: {asc:X2}h, ASCQ: {ascq:X2}h)";
        }
        return $"RESERVED (ASC: {asc:X2}h, ASCQ: {ascq:X2}h)";
    }
  }

  #endregion

}
