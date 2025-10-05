using System;
using System.Collections.Generic;
using System.Text;

using MetaBrainz.MusicBrainz.DiscId.Platforms.NativeApi;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms;

internal abstract class Unix(DiscReadFeature features) : Platform(features) {

  public new static IPlatform Create() {
    var os = string.Empty;
    try {
      // uname() technically fills a struct with multiple arrays of fixed-but-undefined size.
      // However, the arrays are guaranteed to be NUL-terminated, and since there's only 6 of them, 8K should be plenty.
      var buf = new byte[8 * 1024];
      if (LibC.UName(buf) == 0) {
        var endPos = Array.IndexOf<byte>(buf, 0);
        if (endPos >= 0) {
          // FIXME: Or determine a better encoding somehow?
          os = Encoding.ASCII.GetString(buf, 0, endPos);
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

}
