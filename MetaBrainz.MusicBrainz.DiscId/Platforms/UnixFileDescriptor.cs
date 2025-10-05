using System;
using System.IO;
using System.Runtime.InteropServices;

using JetBrains.Annotations;

using MetaBrainz.MusicBrainz.DiscId.Platforms.NativeApi;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms;

[PublicAPI]
[StructLayout(LayoutKind.Sequential)]
internal sealed class UnixFileDescriptor(int descriptor) : IDisposable {

  public static UnixFileDescriptor OpenPath(string path, uint flags, int mode) => new(LibC.Open(path, flags, mode));

  public bool IsInvalid => this.Value == -1;

  public int Value { get; private set; } = descriptor;

  public void Close() {
    if (this.Value == -1) {
      return;
    }
    var rc = LibC.Close(this.Value);
    this.Value = -1;
    if (rc != 0) {
      throw new IOException("Failed to close file descriptor.", new UnixException());
    }
  }

  public override string ToString() => $"fd {this.Value}";

  void IDisposable.Dispose() => this.Close();

}
