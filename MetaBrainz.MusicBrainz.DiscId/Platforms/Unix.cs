using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal abstract class Unix : Platform {

    protected Unix(DiscReadFeature features = DiscReadFeature.TableOfContents) : base(features) { }

    public new static IPlatform Create() {
      var os = string.Empty;
      try { // uname() technically fills a struct with multiple arrays of fixed-but-undefined size.
        // However, the arrays are guaranteed to be NUL-terminated, and since there's only 6 of them, 8K should be plenty.
        var buf = new byte[8 * 1024];
        if (NativeApi.UName(buf) == 0) {
          var endPos = Array.IndexOf<byte>(buf, 0);
          if (endPos >= 0)
            os = Encoding.ASCII.GetString(buf, 0, endPos); // FIXME: Or Encoding.Default?
        }
      }
      catch (DllNotFoundException) { }
      catch (EntryPointNotFoundException) { }
      switch (os) {
        case "FreeBSD": return new FreeBsd    ();
        case "NetBSD" : return new NetBsd     ();
        case "OpenBSD": return new OpenBsd    ();
        case "Linux"  : return new Linux      ();
        case "Darwin" : return new Darwin     ();
        case "SunOS"  : return new Solaris    ();
        default:        return new Unsupported();
      }
    }

    public override IEnumerable<string> AvailableDevices { get { yield break; } }

    protected override TableOfContents ReadTableOfContents(string device, DiscReadFeature features) {
      throw new NotImplementedException($"CD device access has not been implemented for this platform ({this.GetType().Name} {Environment.OSVersion}).");
    }

    private static class NativeApi {

      [DllImport("libc", EntryPoint = "uname", SetLastError = true)]
      public static extern int UName(byte[] data);

    }

  }

}
