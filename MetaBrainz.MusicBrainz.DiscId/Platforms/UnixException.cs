using System;
using System.Runtime.InteropServices;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms;

/// <summary>Exception thrown for a Unix-related error (based on the &quot;errno&quot; value).</summary>
/// <param name="errno">The error code for which the exception is being created.</param>
[Serializable]
public class UnixException(int errno) : ExternalException(UnixException.GetErrorText(errno), errno) {

  /// <summary>
  /// Initializes a new instance of the <see cref="UnixException"/> class, for the most recent error reported by a Unix system
  /// routine.
  /// </summary>
  public UnixException() : this(Marshal.GetLastPInvokeError()) { }

  private static string? GetErrorText(int errno) => Marshal.GetPInvokeErrorMessage(errno);

}
