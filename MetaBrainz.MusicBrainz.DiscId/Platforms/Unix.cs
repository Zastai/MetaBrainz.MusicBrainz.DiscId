using System;
using System.Text;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal abstract class Unix : Platform {

    protected Unix(CdDeviceFeature features = CdDeviceFeature.ReadTableOfContents) : base(features) { }

    [DllImport("libc")]
    private static extern int uname(byte[] data);

    public new static IPlatform Create() {
      var os = string.Empty;
      { // uname() technically fills a struct with multiple arrays of fixed-but-undefined size.
        // However, the arrays are guaranteed to be NUL-terminated, and since there's only 6 of them, 8K should be plenty.
      var buf = new byte[8 * 1024];
        if (uname(buf) == 0) {
        var endpos = Array.IndexOf<byte>(buf, 0);
          if (endpos >= 0)
            os = Encoding.ASCII.GetString(buf, 0, endpos); // FIXME: Or Encoding.Default?
        }
      }
      switch (os) {
        case "FreeBSD": return new FreeBsd    ();
        case "NetBSD" : return new NetBsd     ();
        case "OpenBSD": return new NetBsd     ();
        case "Linux"  : return new Linux      ();
        case "Darwin" : // OSX is not currently supported (no access to it)
        case "SunOS"  : // Solaris is not currently supported (couldn't get Mono to work on it) 
        default:        return new Unsupported();
      }
    }

    public override string DefaultDevice => "/dev/cdrom";

    public override string GetDeviceByIndex(int n) {
      return null;
    }

    public override TableOfContents ReadTableOfContents(string device, CdDeviceFeature features) {
      throw new NotImplementedException($"CD device access has not been implemented for this platform ({Environment.OSVersion}).");
    }

    #region PInvoke Support

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    internal static extern int Close(IntPtr handle);

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    internal static extern SafeUnixHandle Open(string path, uint flag, int mode);

    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
    [SecurityPermission(SecurityAction.Demand,            UnmanagedCode = true)]
    internal sealed class SafeUnixHandle : SafeHandle {

      [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
      private SafeUnixHandle() : base(new IntPtr(-1), true) { }

      public override bool IsInvalid => (this.handle == new IntPtr(-1));

      [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
      protected override bool ReleaseHandle() {
        return Unix.Close(this.handle) != -1;
      }

    }

    #endregion

  }

}
