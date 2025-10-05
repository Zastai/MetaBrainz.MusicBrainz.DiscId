using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms;

internal abstract class Unix(DiscReadFeature features) : Platform(features) {

  public new static IPlatform Create() {
    var os = string.Empty;
    try {
      // uname() technically fills a struct with multiple arrays of fixed-but-undefined size.
      // However, the arrays are guaranteed to be NUL-terminated, and since there's only 6 of them, 8K should be plenty.
      var buf = new byte[8 * 1024];
      if (NativeApi.UName(buf) == 0) {
        var endPos = Array.IndexOf<byte>(buf, 0);
        if (endPos >= 0) {
          os = Encoding.ASCII.GetString(buf, 0, endPos); // FIXME: Or Encoding.Default?
        }
      }
    }
    catch (DllNotFoundException) { }
    catch (EntryPointNotFoundException) { }
    return os switch {
      "FreeBSD" => new FreeBsd(),
      "Linux" => new Linux(),
      "Darwin" => new MacOS(),
      _ => new Unsupported(),
    };
  }

  public override IEnumerable<string> AvailableDevices { get { yield break; } }

  protected override TableOfContents ReadTableOfContents(string device, DiscReadFeature features) {
    var msg = $"CD device access has not been implemented for this platform ({this.GetType().Name} {Environment.OSVersion}).";
    throw new NotImplementedException(msg);
  }

  private static class NativeApi {

    [DllImport("libc", EntryPoint = "uname", SetLastError = true)]
    public static extern int UName(byte[] data);

  }

}
