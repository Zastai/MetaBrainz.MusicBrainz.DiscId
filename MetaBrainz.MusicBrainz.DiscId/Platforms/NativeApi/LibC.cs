using System;
using System.Runtime.InteropServices;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms.NativeApi;

internal static partial class LibC {

  private const string LibraryName = "libc";

  #region P/Invoke Methods

  // TBD: Does LibraryImport work for any of these?
  #pragma warning disable SYSLIB1054

  [DllImport(LibC.LibraryName, EntryPoint = "calloc", SetLastError = true)]
  private static extern IntPtr AllocZero(nuint items, nuint itemSize);

  [DllImport(LibC.LibraryName, EntryPoint = "close", SetLastError = true)]
  public static extern int Close(int handle);

  [DllImport(LibC.LibraryName, EntryPoint = "free", SetLastError = true)]
  private static extern void Free(IntPtr ptr);

#pragma warning disable CA2101 // Inspection about string marshaling; unavoidable on non-Windows (no Unicode API)

  [DllImport(LibC.LibraryName, EntryPoint = "open", SetLastError = true)]
  public static extern int Open(string path, uint flags, int mode);

#pragma warning restore CA2101

  [DllImport(LibC.LibraryName, EntryPoint = "uname", SetLastError = true)]
  public static extern int UName(byte[] data);

  #pragma warning restore SYSLIB1054

  #endregion

}
