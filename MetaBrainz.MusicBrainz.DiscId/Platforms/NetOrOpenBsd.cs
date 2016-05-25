using System.Runtime.InteropServices;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal abstract class NetOrOpenBsd : Bsd {

    protected override string GetDevicePath(string device) {
      return string.Concat("/dev/r", device, NativeApi.RawPartition);
    }

    #region Native API

    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Local

    private new static class NativeApi {

      public static char RawPartition;

      static NativeApi() {
        NativeApi.RawPartition = (char) ('a' + NativeApi.GetRawPartition());
      }

      [DllImport("libutil", EntryPoint = "getrawpartition", SetLastError = true)]
      private static extern int GetRawPartition();

    }

    #endregion

  }

}
