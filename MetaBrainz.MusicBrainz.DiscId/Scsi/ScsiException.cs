using System;
using System.Runtime.InteropServices;

namespace MetaBrainz.MusicBrainz.DiscId.Scsi {

  /// <summary>Exception thrown for a SCSI-related error (based on the sense key as well as the ASC/ASCQ values).</summary>
  [Serializable]
  public class ScsiException : ExternalException {

    /// <summary>Creates a new <see cref="ScsiException"/> instance for the specified SCSI sense information.</summary>
    /// <param name="senseKey">The SCSI &quot;sense key&quot; value.</param>
    /// <param name="asc">The SCSI &quot;additional sense code&quot; value.</param>
    /// <param name="ascq">The SCSI &quot;additional sense code qualifier&quot; value.</param>
    public ScsiException(byte senseKey, byte asc, byte ascq) : base(string.Concat(SPC.SenseKeyDescription(senseKey), " / ", SPC.AdditionalSenseDescription(asc, ascq))) { }

    /// <summary>Creates a new <see cref="ScsiException"/> instance for the specified SCSI fixed-format sense data.</summary>
    /// <param name="senseData">The SCSI sense data to derive the exception message from.</param>
    internal ScsiException(SPC.FixedSenseData senseData) : this(senseData.SenseKey, senseData.AdditionalSenseCode, senseData.AdditionalSenseCodeQualifier) { }

    /// <summary>Creates a new <see cref="ScsiException"/> instance for the specified SCSI sense information.</summary>
    /// <param name="senseData">The SCSI sense data to derive the exception message from.</param>
    internal ScsiException(SPC.DescriptorSenseData senseData) : this(senseData.SenseKey, senseData.AdditionalSenseCode, senseData.AdditionalSenseCodeQualifier) { }

  }

}

