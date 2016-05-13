using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using JetBrains.Annotations;

using Microsoft.Win32.SafeHandles;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal sealed class Windows : Platform {

    public Windows() : base(CdDeviceFeature.ReadTableOfContents | CdDeviceFeature.ReadMediaCatalogNumber | CdDeviceFeature.ReadTrackIsrc) { }

    public override string DefaultDevice => "D:";

    public override string GetDeviceByIndex(int n) {
      if (n < 0)
        return null;
      foreach (var drive in DriveInfo.GetDrives()) {
        if (drive.DriveType == DriveType.CDRom) {
          if (n == 0)
            return drive.Name;
          --n;
        }
      }
      return null;
    }

    public override TableOfContents ReadTableOfContents(string device, CdDeviceFeature features) {
      if (device == null)
        device = this.GetDeviceByIndex(0) ?? this.DefaultDevice;
      using (var hDevice = Windows.CreateDeviceHandle(device)) {
        TableOfContents toc = null;
        { // Read the TOC itself
          var req = new TOCRequest(MMC3.TOCRequestFormat.TOC);
          var rawtoc = new MMC3.TOCDescriptor();
          var returned = 0;
          // LIB-44: Apparently for some multi-session discs, the first TOC read can be wrong. So issue two reads.
          var ok = Windows.DeviceIoControl(hDevice, Windows.IOCTL_CDROM_READ_TOC_EX, ref req, Marshal.SizeOf(req), out rawtoc, Marshal.SizeOf(rawtoc), out returned, IntPtr.Zero);
          if (ok)
            ok = Windows.DeviceIoControl(hDevice, Windows.IOCTL_CDROM_READ_TOC_EX, ref req, Marshal.SizeOf(req), out rawtoc, Marshal.SizeOf(rawtoc), out returned, IntPtr.Zero);
          if (!ok)
            throw new IOException("Failed to retrieve TOC.", new Win32Exception(Marshal.GetLastWin32Error()));
          rawtoc.FixUp(req.AddressAsMSF);
          var mcn = ((features & CdDeviceFeature.ReadMediaCatalogNumber) != 0) ? Windows.GetMediaCatalogNumber(hDevice) : null;
          toc = new TableOfContents(device, rawtoc.FirstTrack, rawtoc.LastTrack, mcn);
          var i = 0;
          for (var trackno = rawtoc.FirstTrack; trackno <= rawtoc.LastTrack; ++trackno, ++i) { // Add the regular tracks.
            if (rawtoc.Tracks[i].TrackNumber != trackno)
              throw new InvalidDataException($"Internal logic error; first track is {rawtoc.FirstTrack}, but entry at index {i} claims to be track {rawtoc.Tracks[i].TrackNumber} instead of {trackno}");
            var isrc = ((features & CdDeviceFeature.ReadTrackIsrc) != 0) ? Windows.GetTrackIsrc(hDevice, trackno) : null;
            toc.SetTrack(rawtoc.Tracks[i], isrc);
          }
          // Next entry should be the leadout (track number 0xAA)
          if (rawtoc.Tracks[i].TrackNumber != 0xAA)
            throw new InvalidDataException($"Internal logic error; track data ends with a record that reports track number {rawtoc.Tracks[i].TrackNumber} instead of 0xAA (lead-out)");
          toc.SetTrack(rawtoc.Tracks[i], null);
        }
        // TODO: If requested, try getting CD-TEXT data.
        return toc;
      }
    }

    private static SafeFileHandle CreateDeviceHandle([NotNull] string device) {
      var colon = device.IndexOf(':');
      if (colon >= 0)
        device = device.Substring(0, colon + 1);
      var path = string.Concat("\\\\.\\", device);
      var handle = Windows.CreateFile(path, FileAccess.Read, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
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
      if (!Windows.DeviceIoControl(hDevice, Windows.IOCTL_CDROM_READ_Q_CHANNEL, ref req, Marshal.SizeOf(req), out mcn, Marshal.SizeOf(mcn), out returned, IntPtr.Zero))
        throw new IOException("Failed to retrieve media catalog number.", new Win32Exception(Marshal.GetLastWin32Error()));
      mcn.FixUp();
      return mcn.Status.IsValid ? Encoding.ASCII.GetString(mcn.MCN) : string.Empty;
    }

    private static string GetTrackIsrc(SafeFileHandle hDevice, byte track) {
      var req = new SubChannelRequest { Format = MMC3.SubChannelRequestFormat.ISRC, Track = track };
      var isrc = new MMC3.SubChannelISRC();
      var returned = 0;
      if (!Windows.DeviceIoControl(hDevice, Windows.IOCTL_CDROM_READ_Q_CHANNEL, ref req, Marshal.SizeOf(req), out isrc, Marshal.SizeOf(isrc), out returned, IntPtr.Zero))
        throw new IOException($"Failed to retrieve ISRC for track {track}.", new Win32Exception(Marshal.GetLastWin32Error()));
      isrc.FixUp();
      return isrc.Status.IsValid ? Encoding.ASCII.GetString(isrc.ISRC) : string.Empty;
    }

    #region WinAPI

    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Local

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


    private const int IOCTL_CDROM_CHECK_VERIFY       = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x0200 << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_FIND_NEW_DEVICES   = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x0206 << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_GET_CONTROL        = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x000D << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_GET_DRIVE_GEOMETRY = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x0013 << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_GET_LAST_SESSION   = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x000E << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_GET_VOLUME         = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x0005 << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_PAUSE_AUDIO        = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x0003 << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_PLAY_AUDIO_MSF     = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x0006 << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_RAW_READ           = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x000F << 2) | Windows.METHOD_OUT_DIRECT);
    private const int IOCTL_CDROM_READ_Q_CHANNEL     = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x000B << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_READ_TOC           = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x0000 << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_READ_TOC_EX        = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x0015 << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_RESUME_AUDIO       = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x0004 << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_SEEK_AUDIO_MSF     = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x0001 << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_SET_VOLUME         = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x000A << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_SIMBAD             = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x1003 << 2) | Windows.METHOD_BUFFERED);
    private const int IOCTL_CDROM_STOP_AUDIO         = ((Windows.FILE_DEVICE_CD_ROM << 16) | (Windows.FILE_READ_ACCESS << 14) | (0x0002 << 2) | Windows.METHOD_BUFFERED);

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

      public TOCRequest(MMC3.TOCRequestFormat format = MMC3.TOCRequestFormat.TOC, byte track = 0, bool msf = false) {
        this.FormatInfo   = (byte) format;
        this.SessionTrack = track;
        this.Reserved1    = 0;
        this.Reserved2    = 0;
        if (msf)
          this.FormatInfo |= 0x80;
      }

      public MMC3.TOCRequestFormat Format => (MMC3.TOCRequestFormat) (this.FormatInfo & 0x0f);

      public bool AddressAsMSF => (this.FormatInfo & 0x80) == 0x80;

    }

    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle hDevice, int ioControlCode, ref TOCRequest request, int nInBufferSize, out MMC3.CDTextDescriptor cdtext, int textSize, out int pBytesReturned, IntPtr overlapped);

    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle hDevice, int ioControlCode, ref TOCRequest request, int nInBufferSize, out MMC3.TOCDescriptor toc, int tocSize, out int pBytesReturned, IntPtr overlapped);

    #endregion

    #endregion

    #endregion

  }

}
