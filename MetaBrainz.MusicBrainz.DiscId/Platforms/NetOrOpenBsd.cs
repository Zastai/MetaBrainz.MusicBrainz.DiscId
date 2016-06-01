using System.Runtime.InteropServices;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal abstract class NetOrOpenBsd : Bsd {

    protected override bool AddressesAreNative => true;

    protected override string GetDevicePath(string device) => string.Concat("/dev/r", device, NativeApi.RawPartition);

    #region Native API

    private static class NativeApi {

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
