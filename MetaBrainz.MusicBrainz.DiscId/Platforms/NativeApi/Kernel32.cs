using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

using JetBrains.Annotations;

using MetaBrainz.MusicBrainz.DiscId.Standards;

using Microsoft.Win32.SafeHandles;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms.NativeApi;

internal static class Kernel32 {

  private const string LibraryName = "kernel32.dll";

  #region Constants

  [SuppressMessage("ReSharper", "InconsistentNaming")]
  private enum IOCTL {

    CDROM_READ_Q_CHANNEL = 0x2402C,

    CDROM_READ_TOC_EX = 0x24054,

  }

  #endregion

  #region Structures

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  private struct SubChannelRequest { // aka CDROM_SUB_Q_DATA_FORMAT

    public MMC.SubChannelRequestFormat Format;

    public byte Track;

  }

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  private struct TOCRequest {

    public readonly byte FormatInfo;

    public readonly byte SessionTrack;

    public readonly byte Reserved1;

    public readonly byte Reserved2;

    public TOCRequest(MMC.TOCRequestFormat format = MMC.TOCRequestFormat.TOC, byte track = 0, bool msf = false) {
      this.FormatInfo = (byte) format;
      this.SessionTrack = track;
      this.Reserved1 = 0;
      this.Reserved2 = 0;
      if (msf) {
        this.FormatInfo |= 0x80;
      }
    }

    public MMC.TOCRequestFormat Format => (MMC.TOCRequestFormat) (this.FormatInfo & 0x0f);

    public bool AddressAsMSF => (this.FormatInfo & 0x80) == 0x80;

  }

  #endregion

  #region P/Invoke Methods

  // LibraryImport does not handle DeviceIoControl's out parameter marshaling, and may not handle the enums for CreateFile.
  #pragma warning disable SYSLIB1054

  [DllImport(Kernel32.LibraryName, CharSet = CharSet.Unicode, SetLastError = true)]
  private static extern SafeFileHandle CreateFile(string filename, [MarshalAs(UnmanagedType.U4)] FileAccess access,
                                                  [MarshalAs(UnmanagedType.U4)] FileShare share, IntPtr securityAttributes,
                                                  [MarshalAs(UnmanagedType.U4)] FileMode mode, uint flags, IntPtr templateFile);

  [DllImport(Kernel32.LibraryName, SetLastError = true)]
  private static extern bool DeviceIoControl(SafeFileHandle hDevice, IOCTL command, ref SubChannelRequest request,
                                             int requestSize, out MMC.SubChannelMediaCatalogNumber data, int dataSize,
                                             out int pBytesReturned, IntPtr overlapped);

  [DllImport(Kernel32.LibraryName, SetLastError = true)]
  private static extern bool DeviceIoControl(SafeFileHandle hDevice, IOCTL command, ref SubChannelRequest request,
                                             int requestSize, out MMC.SubChannelISRC data, int dataSize, out int pBytesReturned,
                                             IntPtr overlapped);

  [DllImport(Kernel32.LibraryName, SetLastError = true)]
  private static extern bool DeviceIoControl(SafeFileHandle hDevice, IOCTL command, ref TOCRequest request, int nInBufferSize,
                                             out MMC.CDTextDescriptor data, int dataSize, out int pBytesReturned,
                                             IntPtr overlapped);

  [DllImport(Kernel32.LibraryName, SetLastError = true)]
  private static extern bool DeviceIoControl(SafeFileHandle hDevice, IOCTL command, ref TOCRequest request, int nInBufferSize,
                                             out MMC.TOCDescriptor data, int dataSize, out int pBytesReturned,
                                             IntPtr overlapped);

  #pragma warning restore SYSLIB1054

  #endregion

  public static SafeFileHandle OpenDevice(string device) {
    var colon = device.IndexOf(':');
    if (colon >= 0) {
      device = device[..++colon];
    }
    var handle = Kernel32.CreateFile($@"\\.\{device}", FileAccess.Read, FileShare.ReadWrite, 0, FileMode.Open, 0, 0);
    if (!handle.IsInvalid) {
      return handle;
    }
    var error = Marshal.GetLastWin32Error();
    throw new ArgumentException($"Cannot open device '{device}' (error {error:X8}).", new Win32Exception(error));
  }

  public static void ReadCdText(SafeFileHandle hDevice, out MMC.CDTextDescriptor cdText, out int length) {
    const IOCTL command = IOCTL.CDROM_READ_TOC_EX;
    var requestSize = Marshal.SizeOf<TOCRequest>();
    var dataSize = Marshal.SizeOf<MMC.CDTextDescriptor>();
    var request = new TOCRequest(MMC.TOCRequestFormat.CDText);
    if (!Kernel32.DeviceIoControl(hDevice, command, ref request, requestSize, out cdText, dataSize, out length, 0)) {
      throw new IOException("Failed to retrieve CD-TEXT information.", new Win32Exception(Marshal.GetLastWin32Error()));
    }
    cdText.FixUp();
  }

  public static void ReadMediaCatalogNumber(SafeFileHandle hDevice, out MMC.SubChannelMediaCatalogNumber mcn, out int length) {
    const IOCTL command = IOCTL.CDROM_READ_Q_CHANNEL;
    var requestSize = Marshal.SizeOf<SubChannelRequest>();
    var dataSize = Marshal.SizeOf<MMC.SubChannelMediaCatalogNumber>();
    var request = new SubChannelRequest {
      Format = MMC.SubChannelRequestFormat.MediaCatalogNumber,
      Track = 0,
    };
    if (!Kernel32.DeviceIoControl(hDevice, command, ref request, requestSize, out mcn, dataSize, out length, 0)) {
      throw new IOException("Failed to retrieve media catalog number.", new Win32Exception(Marshal.GetLastWin32Error()));
    }
    mcn.FixUp();
  }

  public static void ReadTOC(SafeFileHandle hDevice, out MMC.TOCDescriptor toc, out int length) {
    const IOCTL command = IOCTL.CDROM_READ_TOC_EX;
    var requestSize = Marshal.SizeOf<TOCRequest>();
    var dataSize = Marshal.SizeOf<MMC.TOCDescriptor>();
    var request = new TOCRequest(MMC.TOCRequestFormat.TOC);
    // LIB-44: Apparently for some multi-session discs, the first TOC read can be wrong. So issue two reads.
    var ok = Kernel32.DeviceIoControl(hDevice, command, ref request, requestSize, out toc, dataSize, out length, 0);
    if (ok) {
      ok = Kernel32.DeviceIoControl(hDevice, command, ref request, requestSize, out toc, dataSize, out length, 0);
    }
    if (!ok) {
      throw new IOException("Failed to retrieve TOC.", new Win32Exception(Marshal.GetLastWin32Error()));
    }
    toc.FixUp(request.AddressAsMSF);
  }

  public static void ReadTrackISRC(SafeFileHandle hDevice, byte track, out MMC.SubChannelISRC isrc, out int length) {
    const IOCTL command = IOCTL.CDROM_READ_Q_CHANNEL;
    var requestSize = Marshal.SizeOf<SubChannelRequest>();
    var dataSize = Marshal.SizeOf<MMC.SubChannelISRC>();
    var request = new SubChannelRequest {
      Format = MMC.SubChannelRequestFormat.ISRC,
      Track = track,
    };
    if (!Kernel32.DeviceIoControl(hDevice, command, ref request, requestSize, out isrc, dataSize, out length, 0)) {
      throw new IOException($"Failed to retrieve ISRC for track {track}.", new Win32Exception(Marshal.GetLastWin32Error()));
    }
    isrc.FixUp();
  }

}
