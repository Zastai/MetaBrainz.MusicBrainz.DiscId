using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  [StructLayout(LayoutKind.Sequential)]
  internal sealed class UnixFileDescriptor : IDisposable {

    public static UnixFileDescriptor OpenPath(string path, uint flags, int mode) => new UnixFileDescriptor(NativeApi.Open(path, flags, mode));

    public bool IsInvalid => this._descriptor == -1;

    public int Value => this._descriptor;

    public void Close() {
      if (this._descriptor == -1)
        return;
      var rc = NativeApi.Close(this._descriptor);
      this._descriptor = -1;
      if (rc != 0)
        throw new IOException("Failed to close file descriptor.", new UnixException());
    }

    public override string ToString() => $"fd {this._descriptor}";

    void IDisposable.Dispose() {
      this.Close();
    }

    private UnixFileDescriptor(int descriptor) {
      this._descriptor = descriptor;
    }

    private int _descriptor;

    private static class NativeApi {

      [DllImport("libc", EntryPoint = "close", SetLastError = true)]
      public static extern int Close(int handle);

      [DllImport("libc", EntryPoint = "open", SetLastError = true)]
      public static extern int Open(string path, uint flags, int mode);

    }

  }

}
