// Compatibility for features missing from some frameworks.

#if (NETFX_TARGET && !NETFX_GE_3_5)

namespace System.Threading {

  internal enum LockRecursionPolicy { NoRecursion }

  internal class ReaderWriterLockSlim {

    private readonly ReaderWriterLock _lock = new ReaderWriterLock();

    public ReaderWriterLockSlim(LockRecursionPolicy policy) { }

    public void EnterWriteLock() => this._lock.AcquireWriterLock(-1);

    public void ExitWriteLock() => this._lock.ReleaseWriterLock();

  }

}

#endif

#if (NETSTD_TARGET && !NETSTD_GE_2_0) || (NETCORE_TARGET && !NETCORE_GE_2_0) // TODO: Exact versions

namespace System {

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
  internal sealed class SerializableAttribute : Attribute { }

}

#endif

#if (NETSTD_TARGET && !NETSTD_GE_2_0) || (NETCORE_TARGET && !NETCORE_GE_2_0) // TODO: Exact versions

namespace System {

  [Serializable]
  internal sealed class EntryPointNotFoundException : Exception { }

}

#endif

#if (NETSTD_TARGET && !NETSTD_GE_2_0) || (NETCORE_TARGET && !NETCORE_GE_2_0) // TODO: Exact versions

namespace System.Runtime.InteropServices {

  /// <summary>
  ///   An external exception (with system error code).
  ///   Defined here because the standard System.Runtime.InteropServices.ExternalException class is not available in this framework version.
  /// </summary>
  /// <remarks>Contains only the API subset used by this library's code</remarks>
  [Serializable]
  public class ExternalException : Exception {

    /// <summary>Creates a new external exception, with a standard message and error code.</summary>
    public ExternalException() : base("An external error occurred.") {
      this.ErrorCode = -1;
    }

    /// <summary>Creates a new external exception with the specified message and error code.</summary>
    /// <param name="message">The message for the error.</param>
    /// <param name="code">The system error code.</param>
    public ExternalException(string message, int code) : base(message) {
      this.ErrorCode = code;
    }

    /// <summary>The system error code.</summary>
    public virtual int ErrorCode { get; }

    /// <summary>The error message.</summary>
    public override string Message => $"(0x{this.ErrorCode:X8}) {base.Message}";

  }

}

#endif
