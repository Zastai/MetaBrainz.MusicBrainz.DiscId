using System;
using System.IO;
using System.Runtime.InteropServices;

using JetBrains.Annotations;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms;

[PublicAPI]
[StructLayout(LayoutKind.Sequential)]
internal sealed class UnixFileDescriptor(int descriptor) : IDisposable {

  public static UnixFileDescriptor OpenPath(string path, uint flags, int mode) => new(NativeApi.Open(path, flags, mode));

  public bool IsInvalid => this.Value == -1;

  public int Value { get; private set; } = descriptor;

  public void Close() {
    if (this.Value == -1) {
      return;
    }
    var rc = NativeApi.Close(this.Value);
    this.Value = -1;
    if (rc != 0) {
      throw new IOException("Failed to close file descriptor.", new UnixException());
    }
  }

  public override string ToString() => $"fd {this.Value}";

  void IDisposable.Dispose() => this.Close();

  private static class NativeApi {

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    public static extern int Close(int handle);

#pragma warning disable CA2101 // Inspection about string marshaling; unavoidable on non-Windows (no Unicode API)

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    public static extern int Open(string path, uint flags, int mode);

#pragma warning restore CA2101

  }

}
