using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal sealed class Windows : Platform {

    public Windows() : base(DiscReadFeature.TableOfContents | DiscReadFeature.MediaCatalogNumber | DiscReadFeature.TrackIsrc | DiscReadFeature.CdText) { }

    public override IEnumerable<string> AvailableDevices {
      get {
        foreach (var drive in DriveInfo.GetDrives()) {
          if (drive.DriveType == DriveType.CDRom)
            yield return drive.Name;
        }
      }
    }

    protected override TableOfContents ReadTableOfContents(string device, DiscReadFeature features) {
      using (var hDevice = NativeApi.OpenDevice(device)) {
        // Read the TOC itself
        NativeApi.GetTableOfContents(hDevice, out MMC.TOCDescriptor rawtoc);
        var first = rawtoc.FirstTrack;
        var last = rawtoc.LastTrack;
        var tracks = new Track[last + 1];
        var i = 0;
        for (var trackno = rawtoc.FirstTrack; trackno <= rawtoc.LastTrack; ++trackno, ++i) { // Add the regular tracks.
          if (rawtoc.Tracks[i].TrackNumber != trackno)
            throw new InvalidDataException($"Internal logic error; first track is {rawtoc.FirstTrack}, but entry at index {i} claims to be track {rawtoc.Tracks[i].TrackNumber} instead of {trackno}");
          var isrc = ((features & DiscReadFeature.TrackIsrc) != 0) ? NativeApi.GetTrackIsrc(hDevice, trackno) : null;
          tracks[trackno] = new Track(rawtoc.Tracks[i].Address, rawtoc.Tracks[i].ControlAndADR.Control, isrc);
        }
        // Next entry should be the leadout (track number 0xAA)
        if (rawtoc.Tracks[i].TrackNumber != 0xAA)
          throw new InvalidDataException($"Internal logic error; track data ends with a record that reports track number {rawtoc.Tracks[i].TrackNumber} instead of 0xAA (lead-out)");
        tracks[0] = new Track(rawtoc.Tracks[i].Address, rawtoc.Tracks[i].ControlAndADR.Control, null);
        var mcn = ((features & DiscReadFeature.MediaCatalogNumber) != 0) ? NativeApi.GetMediaCatalogNumber(hDevice) : null;
        RedBook.CDTextGroup? cdtg = null;
        if ((features & DiscReadFeature.CdText) != 0) {
          NativeApi.GetCdTextInfo(hDevice, out MMC.CDTextDescriptor cdtext);
          if (cdtext.Data.Packs != null)
            cdtg = cdtext.Data;
        }
        return new TableOfContents(device, first, last, tracks, mcn, cdtg);
      }
    }

    #region Native API

    // FIXME: Ideally, I'd rework this to use SPTI, to align the Linux & Windows implementation; but initial attempts have been unsuccessful.

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
    private static class NativeApi {

      #region Constants

      private enum IOCTL : int {
        CDROM_READ_Q_CHANNEL = 0x2402C,
        CDROM_READ_TOC_EX    = 0x24054,
      }

      #endregion

      #region Structures

      [StructLayout(LayoutKind.Sequential, Pack = 1)]
      private struct SubChannelRequest { // aka CDROM_SUB_Q_DATA_FORMAT

        public MMC.SubChannelRequestFormat Format;
        public byte                        Track;

      }

      [StructLayout(LayoutKind.Sequential, Pack = 1)]
      private struct TOCRequest {

        private readonly byte FormatInfo;
        private readonly byte SessionTrack;
        private readonly byte Reserved1;
        private readonly byte Reserved2;

        public TOCRequest(MMC.TOCRequestFormat format = MMC.TOCRequestFormat.TOC, byte track = 0, bool msf = false) {
          this.FormatInfo   = (byte) format;
          this.SessionTrack = track;
          this.Reserved1    = 0;
          this.Reserved2    = 0;
          if (msf)
            this.FormatInfo |= 0x80;
        }

        public MMC.TOCRequestFormat Format => (MMC.TOCRequestFormat) (this.FormatInfo & 0x0f);

        public bool AddressAsMSF => (this.FormatInfo & 0x80) == 0x80;

      }

      #endregion

      #region Public Methods

      public static void GetCdTextInfo(SafeFileHandle hDevice, out MMC.CDTextDescriptor cdtext) {
        var req = new NativeApi.TOCRequest(MMC.TOCRequestFormat.CDText);
        var reqlen = Util.SizeOfStructure<TOCRequest>();
        var cdtextlen = Util.SizeOfStructure<MMC.CDTextDescriptor>();
        var ok = NativeApi.DeviceIoControl(hDevice, IOCTL.CDROM_READ_TOC_EX, ref req, reqlen, out cdtext, cdtextlen, out int _, IntPtr.Zero);
        if (!ok)
          throw new IOException("Failed to retrieve CD-TEXT information.", new Win32Exception(Marshal.GetLastWin32Error()));
        cdtext.FixUp();
      }

      public static string GetMediaCatalogNumber(SafeFileHandle hDevice) {
        var req = new SubChannelRequest { Format = MMC.SubChannelRequestFormat.MediaCatalogNumber, Track = 0 };
        var reqlen = Util.SizeOfStructure<SubChannelRequest>();
        var mcnlen = Util.SizeOfStructure<MMC.SubChannelMediaCatalogNumber>();
        if (!NativeApi.DeviceIoControl(hDevice, IOCTL.CDROM_READ_Q_CHANNEL, ref req, reqlen, out MMC.SubChannelMediaCatalogNumber mcn, mcnlen, out int _, IntPtr.Zero))
          throw new IOException("Failed to retrieve media catalog number.", new Win32Exception(Marshal.GetLastWin32Error()));
        mcn.FixUp();
        return mcn.Status.IsValid ? Encoding.ASCII.GetString(mcn.MCN) : string.Empty;
      }

      public static void GetTableOfContents(SafeFileHandle hDevice, out MMC.TOCDescriptor rawtoc) {
        var req = new NativeApi.TOCRequest(MMC.TOCRequestFormat.TOC);
        var reqlen = Util.SizeOfStructure<TOCRequest>();
        var rawtoclen = Util.SizeOfStructure<MMC.TOCDescriptor>();
        // LIB-44: Apparently for some multi-session discs, the first TOC read can be wrong. So issue two reads.
        var ok = NativeApi.DeviceIoControl(hDevice, IOCTL.CDROM_READ_TOC_EX, ref req, reqlen, out rawtoc, rawtoclen, out int returned, IntPtr.Zero);
        if (ok)
          ok = NativeApi.DeviceIoControl(hDevice, IOCTL.CDROM_READ_TOC_EX, ref req, reqlen, out rawtoc, rawtoclen, out returned, IntPtr.Zero);
        if (!ok)
          throw new IOException("Failed to retrieve TOC.", new Win32Exception(Marshal.GetLastWin32Error()));
        rawtoc.FixUp(req.AddressAsMSF);
      }

      public static string GetTrackIsrc(SafeFileHandle hDevice, byte track) {
        var req = new SubChannelRequest { Format = MMC.SubChannelRequestFormat.ISRC, Track = track };
        var reqlen = Util.SizeOfStructure<SubChannelRequest>();
        var isrclen = Util.SizeOfStructure<MMC.SubChannelISRC>();
        if (!NativeApi.DeviceIoControl(hDevice, IOCTL.CDROM_READ_Q_CHANNEL, ref req, reqlen, out MMC.SubChannelISRC isrc, isrclen, out int _, IntPtr.Zero))
          throw new IOException($"Failed to retrieve ISRC for track {track}.", new Win32Exception(Marshal.GetLastWin32Error()));
        isrc.FixUp();
        return isrc.Status.IsValid ? Encoding.ASCII.GetString(isrc.ISRC) : string.Empty;
      }

      public static SafeFileHandle OpenDevice(string device) {
        var colon = device.IndexOf(':');
        if (colon >= 0)
          device = device.Substring(0, colon + 1);
        var path = string.Concat("\\\\.\\", device);
        var handle = NativeApi.CreateFile(path, FileAccess.Read, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
        if (!handle.IsInvalid)
          return handle;
        var error = Marshal.GetLastWin32Error();
        throw new ArgumentException($"Cannot open device '{device}' (error {error:X8}).", new Win32Exception(error));
      }

      #endregion

      #region Private Methods

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      private static extern SafeFileHandle CreateFile(string filename, [MarshalAs(UnmanagedType.U4)] FileAccess access, [MarshalAs(UnmanagedType.U4)] FileShare share, IntPtr securityAttributes, [MarshalAs(UnmanagedType.U4)] FileMode mode, uint flags, IntPtr templateFile);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern bool DeviceIoControl(SafeFileHandle hDevice, IOCTL command, ref SubChannelRequest request, int requestSize, out MMC.SubChannelMediaCatalogNumber data, int dataSize, out int pBytesReturned, IntPtr overlapped);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern bool DeviceIoControl(SafeFileHandle hDevice, IOCTL command, ref SubChannelRequest request, int requestSize, out MMC.SubChannelISRC data, int dataSize, out int pBytesReturned, IntPtr overlapped);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern bool DeviceIoControl(SafeFileHandle hDevice, IOCTL command, ref TOCRequest request, int nInBufferSize, out MMC.CDTextDescriptor data, int dataSize, out int pBytesReturned, IntPtr overlapped);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern bool DeviceIoControl(SafeFileHandle hDevice, IOCTL command, ref TOCRequest request, int nInBufferSize, out MMC.TOCDescriptor data, int dataSize, out int pBytesReturned, IntPtr overlapped);

      #endregion

    }

    #endregion

  }

}
