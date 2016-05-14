using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace MetaBrainz.MusicBrainz.DiscId {

  /// <summary>Exception thrown for a Unix-related error (based on the &quot;errno&quot; value).</summary>
  [Serializable]
  public class UnixException : ExternalException {

    /// <summary>Initializes a new instance of the <see cref="UnixException"/> class, for the most recent error reported by a Unix system routine.</summary>
    public UnixException() : this(Marshal.GetLastWin32Error()) { }

    /// <summary>Initializes a new instance of the <see cref="UnixException"/> class, for the specified error code.</summary>
    public UnixException(int errno) : base(UnixException.GetErrorText(errno), errno) {
    }

    private static string GetErrorText(int errno) {
      var buffer = new StringBuilder(4096);
      try {
        var rc = UnixException.GetErrorText(errno, buffer, buffer.Capacity);
        if (rc == 0)
          return buffer.ToString();
      }
      catch (DllNotFoundException) { }
      catch (EntryPointNotFoundException) { }
      return null;
    }

    // strerror() is not threadsafe, and strerror_r() is not portable, so use Mono's helper for it.
    // Will need changes to work on the CoreCLR.
    [DllImport("MonoPosixHelper", EntryPoint = "Mono_Posix_Syscall_strerror_r", SetLastError = true)]
    private static extern int GetErrorText(int error, [Out] StringBuilder buffer, long length);

  }

}
