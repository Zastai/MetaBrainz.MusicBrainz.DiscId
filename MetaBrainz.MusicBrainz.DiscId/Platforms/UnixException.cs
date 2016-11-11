using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  /// <summary>Exception thrown for a Unix-related error (based on the &quot;errno&quot; value).</summary>
  [Serializable]
  public class UnixException : ExternalException {

    /// <summary>Initializes a new instance of the <see cref="UnixException"/> class, for the most recent error reported by a Unix system routine.</summary>
    public UnixException() : this(Marshal.GetLastWin32Error()) { }

    /// <summary>Initializes a new instance of the <see cref="UnixException"/> class, for the specified error code.</summary>
    public UnixException(int errno) : base(UnixException.GetErrorText(errno), errno) { }

    private static string GetErrorText(int errno) {
      // strerror() is not threadsafe, and strerror_r() is not portable, so use strerror() plus a lock to help with the threadsafety issue.
      UnixException.Lock();
      try {
        return Marshal.PtrToStringAnsi(UnixException.StrError(errno));
      }
      catch (DllNotFoundException)        { }
      catch (EntryPointNotFoundException) { }
      finally {
        UnixException.Unlock();
      }
      return $"[errno {errno}]";
    }

    [DllImport("libc", EntryPoint = "strerror", SetLastError = true)]
    private static extern IntPtr StrError(int error);

#if NETFX_LT_3_5

    private static readonly ReaderWriterLock ThreadLock = new ReaderWriterLock();

    private static void Lock() {
      UnixException.ThreadLock.AcquireWriterLock(-1);
    }

    private static void Unlock() {
      UnixException.ThreadLock.ReleaseWriterLock();
    }

#else

    private static readonly ReaderWriterLockSlim ThreadLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

    private static void Lock() {
      UnixException.ThreadLock.EnterWriteLock();
    }

    private static void Unlock() {
      UnixException.ThreadLock.ExitWriteLock();
    }

#endif

  }

}
