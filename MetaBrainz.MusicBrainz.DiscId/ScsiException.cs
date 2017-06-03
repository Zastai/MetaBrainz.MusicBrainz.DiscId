using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId {

  /// <summary>Exception thrown for a SCSI-related error (based on the sense key as well as the ASC/ASCQ values).</summary>
  [Serializable]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  public class ScsiException : ExternalException {

    /// <summary>Creates a new <see cref="ScsiException"/> instance for the specified SCSI sense information.</summary>
    /// <param name="sk">The SCSI &quot;sense key&quot; value.</param>
    /// <param name="asc">The SCSI &quot;additional sense code&quot; value.</param>
    /// <param name="ascq">The SCSI &quot;additional sense code qualifier&quot; value.</param>
    public ScsiException(byte sk, byte asc, byte ascq) {
      this.SenseKey = sk;
      this.AdditionalSenseCode = asc;
      this.AdditionalSenseCodeQualifier = ascq;
    }

    /// <summary>Creates a new <see cref="ScsiException"/> instance for the specified SCSI fixed-format sense data.</summary>
    /// <param name="senseData">The SCSI sense data to derive the exception message from.</param>
    internal ScsiException(SPC.FixedSenseData senseData) : this(senseData.SenseKey, senseData.AdditionalSenseCode, senseData.AdditionalSenseCodeQualifier) { }

    /// <summary>Creates a new <see cref="ScsiException"/> instance for the specified SCSI sense information.</summary>
    /// <param name="senseData">The SCSI sense data to derive the exception message from.</param>
    internal ScsiException(SPC.DescriptorSenseData senseData) : this(senseData.SenseKey, senseData.AdditionalSenseCode, senseData.AdditionalSenseCodeQualifier) { }

    /// <summary>The SCSI error code. The lower three bytes contain the sense key, additional sense code and additional sense code qualifier, respectively.</summary>
    public override int ErrorCode => (this.SenseKey << 16) | (this.AdditionalSenseCode << 8) | this.AdditionalSenseCodeQualifier;

    /// <summary>The SCSI error message. This is of the form &quot;A / B&quot;, where A is the description of the sense key and B is the description of the additional sense code.</summary>
    public override string Message => string.Concat(SPC.SenseKeyDescription(this.SenseKey), " / ", SPC.AdditionalSenseDescription(this.AdditionalSenseCode, this.AdditionalSenseCodeQualifier));

    /// <summary>The SCSI &quot;sense key&quot; value.</summary>
    public byte SenseKey { get; }

    /// <summary>The SCSI &quot;additional sense code&quot; value.</summary>
    public byte AdditionalSenseCode { get; }

    /// <summary>The SCSI &quot;additional sense code qualifier&quot; value.</summary>
    public byte AdditionalSenseCodeQualifier { get; }

  }

}

