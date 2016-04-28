using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using JetBrains.Annotations;

using Microsoft.Win32.SafeHandles;

#pragma warning disable 649

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

namespace MetaBrainz.MusicBrainz {

  internal static class WinApi {

    #region CreateFile

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string filename, [MarshalAs(UnmanagedType.U4)] FileAccess access, [MarshalAs(UnmanagedType.U4)] FileShare share, IntPtr securityAttributes, [MarshalAs(UnmanagedType.U4)] FileMode mode, uint flags, IntPtr templateFile);

    #endregion

    #region DeviceIoControl

    #region Constants

    private const int FILE_DEVICE_CD_ROM = 0x00002;

    private const int FILE_READ_ACCESS = 0x0001;

    private const int METHOD_BUFFERED   = 0;
    private const int METHOD_IN_DIRECT  = 1;
    private const int METHOD_OUT_DIRECT = 2;
    private const int METHOD_NEITHER    = 3;

    private const int IOCTL_CDROM_CHECK_VERIFY       = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x0200 << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_FIND_NEW_DEVICES   = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x0206 << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_GET_CONTROL        = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x000D << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_GET_DRIVE_GEOMETRY = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x0013 << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_GET_LAST_SESSION   = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x000E << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_GET_VOLUME         = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x0005 << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_PAUSE_AUDIO        = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x0003 << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_PLAY_AUDIO_MSF     = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x0006 << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_RAW_READ           = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x000F << 2) | WinApi.METHOD_OUT_DIRECT);
    private const int IOCTL_CDROM_READ_Q_CHANNEL     = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x000B << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_READ_TOC           = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x0000 << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_READ_TOC_EX        = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x0015 << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_RESUME_AUDIO       = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x0004 << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_SEEK_AUDIO_MSF     = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x0001 << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_SET_VOLUME         = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x000A << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_SIMBAD             = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x1003 << 2) | WinApi.METHOD_BUFFERED);
    private const int IOCTL_CDROM_STOP_AUDIO         = ((WinApi.FILE_DEVICE_CD_ROM << 16) | (WinApi.FILE_READ_ACCESS << 14) | (0x0002 << 2) | WinApi.METHOD_BUFFERED);

    #endregion

    #region IOCTL_CDROM_READ_Q_CHANNEL

    [StructLayout(LayoutKind.Sequential)]
    private struct SubChannelRequest { // aka CDROM_SUB_Q_DATA_FORMAT
      public MMC3.SubChannelRequestFormat Format;
      public byte                         Track;
    }

    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle hDevice, int ioControlCode, ref SubChannelRequest request, int requestSize, out MMC3.SubChannelMediaCatalogNumber data, int dataSize, out int pBytesReturned, IntPtr overlapped);

    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle hDevice, int ioControlCode, ref SubChannelRequest request, int requestSize, out MMC3.SubChannelISRC data, int dataSize, out int pBytesReturned, IntPtr overlapped);

    #endregion

    #region IOCTL_CDROM_READ_TOC_EX

    private struct TOCRequest {
      public readonly byte FormatInfo;
      public readonly byte SessionTrack;
      public readonly byte Reserved1;
      public readonly byte Reserved2;

      public TOCRequest(MMC3.TOCRequestFormat format = MMC3.TOCRequestFormat.TOC, byte track = 0) {
        this.FormatInfo   = (byte) format;
#if GET_TOC_AS_MSF
        this.FormatInfo  |= 0x80;
#endif
        this.SessionTrack = track;
        this.Reserved1    = 0;
        this.Reserved2    = 0;
      }

      public MMC3.TOCRequestFormat Format => (MMC3.TOCRequestFormat) (this.FormatInfo & 0x0f);

      public bool AddressAsMSF => (this.FormatInfo & 0x80) == 0x80;

    }

    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle hDevice, int ioControlCode, ref TOCRequest request, int nInBufferSize, out MMC3.TOCDescriptor toc, int tocSize, out int pBytesReturned, IntPtr overlapped);

    #endregion

    #endregion

    private static SafeFileHandle CreateDeviceHandle([NotNull] string device) {
      var colon = device.IndexOf(':');
      if (colon >= 0)
        device = device.Substring(0, colon + 1);
      var path = string.Concat("\\\\.\\", device);
      var handle = WinApi.CreateFile(path, FileAccess.Read, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
      if (handle.IsInvalid) {
        var error = Marshal.GetLastWin32Error();
        throw new ArgumentException($"Cannot open the CD audio device '{device}' (error {error:X8}).", new Win32Exception(error));
      }
      return handle;
    }

    private static string GetMediaCatalogNumber(SafeFileHandle hDevice) {
      var req = new SubChannelRequest { Format = MMC3.SubChannelRequestFormat.MediaCatalogNumber, Track = 0 };
      var mcn = new MMC3.SubChannelMediaCatalogNumber();
      var returned = 0;
      if (!WinApi.DeviceIoControl(hDevice, WinApi.IOCTL_CDROM_READ_Q_CHANNEL, ref req, Marshal.SizeOf(req), out mcn, Marshal.SizeOf(mcn), out returned, IntPtr.Zero))
        throw new IOException("Failed to retrieve media catalog number.", new Win32Exception(Marshal.GetLastWin32Error()));
      mcn.FixUp();
      return mcn.Status.IsValid ? Encoding.ASCII.GetString(mcn.MCN) : string.Empty;
    }

    private static string GetTrackIsrc(SafeFileHandle hDevice, byte track) {
      var req = new SubChannelRequest { Format = MMC3.SubChannelRequestFormat.ISRC, Track = track };
      var isrc = new MMC3.SubChannelISRC();
      var returned = 0;
      if (!WinApi.DeviceIoControl(hDevice, WinApi.IOCTL_CDROM_READ_Q_CHANNEL, ref req, Marshal.SizeOf(req), out isrc, Marshal.SizeOf(isrc), out returned, IntPtr.Zero))
        throw new IOException($"Failed to retrieve ISRC for track {track}.", new Win32Exception(Marshal.GetLastWin32Error()));
      isrc.FixUp();
      return isrc.Status.IsValid ? Encoding.ASCII.GetString(isrc.ISRC) : string.Empty;
    }

    public static TableOfContents GetTableOfContents([NotNull] string device, bool includeIsrc, bool includeMcn, bool includeText) {
      using (var hDevice = WinApi.CreateDeviceHandle(device)) {
        var req = new TOCRequest(MMC3.TOCRequestFormat.TOC);
        var rawtoc = new MMC3.TOCDescriptor();
        var returned = 0;
        // LIB-44: Apparently for some multi-session discs, the first TOC read can be wrong. So issue two reads.
        var ok = WinApi.DeviceIoControl(hDevice, WinApi.IOCTL_CDROM_READ_TOC_EX, ref req, Marshal.SizeOf(req), out rawtoc, Marshal.SizeOf(rawtoc), out returned, IntPtr.Zero);
        if (ok)
          ok = WinApi.DeviceIoControl(hDevice, WinApi.IOCTL_CDROM_READ_TOC_EX, ref req, Marshal.SizeOf(req), out rawtoc, Marshal.SizeOf(rawtoc), out returned, IntPtr.Zero);
        if (!ok)
          throw new IOException("Failed to retrieve TOC.", new Win32Exception(Marshal.GetLastWin32Error()));
        rawtoc.FixUp();
        var mcn = includeMcn ? WinApi.GetMediaCatalogNumber(hDevice) : null;
        var toc = new TableOfContents(rawtoc.FirstTrack, rawtoc.LastTrack, mcn);
        var i = 0;
        for (var trackno = rawtoc.FirstTrack; trackno <= rawtoc.LastTrack; ++trackno, ++i) { // Add the regular tracks.
          if (rawtoc.Track[i].TrackNumber != trackno)
            throw new InvalidDataException($"Internal logic error; first track is {rawtoc.FirstTrack}, but entry at index {i} claims to be track {rawtoc.Track[i].TrackNumber} instead of {trackno}");
          var isrc = includeIsrc ? WinApi.GetTrackIsrc(hDevice, trackno) : null;
          toc.SetTrack(rawtoc.Track[i], isrc);
        }
        // Next entry should be the leadout (track number 0xAA)
        if (rawtoc.Track[i].TrackNumber != 0xAA)
          throw new InvalidDataException($"Internal logic error; track data ends with a record that reports track number {rawtoc.Track[i].TrackNumber} instead of 0xAA (lead-out)");
        toc.SetTrack(rawtoc.Track[i], null);
        return toc;
      }
    }

  }

}
