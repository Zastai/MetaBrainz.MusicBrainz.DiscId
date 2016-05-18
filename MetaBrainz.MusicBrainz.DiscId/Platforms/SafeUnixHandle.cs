using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
  [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
  internal sealed class SafeUnixHandle : SafeHandle {

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    private SafeUnixHandle() : base(new IntPtr(-1), true) { }

    public override bool IsInvalid => this.handle == new IntPtr(-1);

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    protected override bool ReleaseHandle() {
      return NativeApi.Close(this.handle) != -1;
    }

    public static SafeUnixHandle OpenPath(string path, uint flags, int mode) => NativeApi.Open(path, flags, mode);

    private static class NativeApi {

      [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
      [DllImport("libc", EntryPoint = "close", SetLastError = true)]
      public static extern int Close(IntPtr handle);

      [DllImport("libc", EntryPoint = "open", SetLastError = true)]
      public static extern SafeUnixHandle Open(string path, uint flags, int mode);

    }

  }

}