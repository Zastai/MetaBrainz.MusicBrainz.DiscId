using System.Diagnostics.CodeAnalysis;

namespace MetaBrainz.MusicBrainz.DiscId.Standards {

  /// <summary>Static class containing structures, enumerations and constants for the SCSI Architecture Model.</summary>
  /// <remarks>
  /// Based on the following (draft) standard documents:
  /// <list type="bullet">
  ///   <item><term>[SAM]</term>  <description>X3T10 994D revision 18</description></item>
  ///   <item><term>[SAM-2]</term><description>T10 Project 1157-D revision 24</description></item>
  ///   <item><term>[SAM-3]</term><description>T10 Project 1561-D revision 13</description></item>
  ///   <item><term>[SAM-4]</term><description>Project T10/1683-D revision 14</description></item>
  ///   <item><term>[SAM-5]</term><description>Project T10/BSR INCITS 515 revision 21</description></item>
  ///   <item><term>[SAM-6]</term><description>Project T10/BSR INCITS 546 revision 2</description></item>
  /// </list>
  /// </remarks>
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  internal static class SAM {

    #region Enumerations

    /// <summary>Enumeration of the status codes that can be returned by SCSI commands.</summary>
    public enum StatusCode : byte {
      GOOD                       = 0x00,
      CHECK_CONDITION            = 0x02,
      CONDITION_MET              = 0x04,
      BUSY                       = 0x08,
      INTERMEDIATE               = 0x10, // Obsolete since SAM-4
      INTERMEDIATE_CONDITION_MET = 0x14, // Obsolete since SAM-4
      RESERVATION_CONFLICT       = 0x18,
      COMMAND_TERMINATED         = 0x22, // Obsolete since SAM-2
      TASK_SET_FULL              = 0x28,
      ACA_ACTIVE                 = 0x30,
      TASK_ABORTED               = 0x40,
    }

    #endregion

    #region Structures

    #endregion

  }

}
