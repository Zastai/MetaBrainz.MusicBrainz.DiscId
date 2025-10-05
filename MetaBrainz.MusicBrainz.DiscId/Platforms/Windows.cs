using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using JetBrains.Annotations;

using MetaBrainz.MusicBrainz.DiscId.Standards;

using Microsoft.Win32.SafeHandles;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms;

internal sealed class Windows() : Platform(Windows.Features) {

  private const DiscReadFeature Features =
    DiscReadFeature.TableOfContents |
    DiscReadFeature.MediaCatalogNumber |
    DiscReadFeature.TrackIsrc |
    DiscReadFeature.CdText;

  public override IEnumerable<string> AvailableDevices {
    get {
      foreach (var drive in DriveInfo.GetDrives()) {
        if (drive.DriveType == DriveType.CDRom) {
          yield return drive.Name;
        }
      }
    }
  }

  protected override TableOfContents ReadTableOfContents(string device, DiscReadFeature features) {
    using var hDevice = NativeApi.OpenDevice(device);
    // Read the TOC itself
    NativeApi.GetTableOfContents(hDevice, out var rawToc);
    var first = rawToc.FirstTrack;
    var last = rawToc.LastTrack;
    var tracks = new Track[last + 1];
    var i = 0;
    for (var trackNo = rawToc.FirstTrack; trackNo <= rawToc.LastTrack; ++trackNo, ++i) { // Add the regular tracks.
      if (rawToc.Tracks[i].TrackNumber != trackNo) {
        throw new InvalidDataException($"Internal logic error; first track is {rawToc.FirstTrack}, but entry at index {i} " +
                                       $"claims to be track {rawToc.Tracks[i].TrackNumber} instead of {trackNo}.");
      }
      var isrc = ((features & DiscReadFeature.TrackIsrc) != 0) ? NativeApi.GetTrackIsrc(hDevice, trackNo) : null;
      tracks[trackNo] = new Track(rawToc.Tracks[i].Address, rawToc.Tracks[i].ControlAndADR.Control, isrc);
    }
    // Next entry should be the lead-out (track number 0xAA)
    if (rawToc.Tracks[i].TrackNumber != 0xAA) {
      throw new InvalidDataException($"Internal logic error; track data ends with a record that reports track number " +
                                     $"{rawToc.Tracks[i].TrackNumber} instead of 0xAA (lead-out)");
    }
    tracks[0] = new Track(rawToc.Tracks[i].Address, rawToc.Tracks[i].ControlAndADR.Control, null);
    var mcn = ((features & DiscReadFeature.MediaCatalogNumber) != 0) ? NativeApi.GetMediaCatalogNumber(hDevice) : null;
    RedBook.CDTextGroup? cdTextGroup = null;
    if ((features & DiscReadFeature.CdText) != 0) {
      NativeApi.GetCdTextInfo(hDevice, out var cdText);
      if (cdText.Data.Packs is not null) {
        cdTextGroup = cdText.Data;
      }
    }
    return new TableOfContents(device, first, last, tracks, mcn, cdTextGroup);
  }

  #region Native API

  // FIXME: Ideally, I'd rework this to use SPTI, to align the Linux & Windows implementation.
  //        However, initial attempts have been unsuccessful.

  private static class NativeApi {

    #region Constants

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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private struct TOCRequest {

      private readonly byte FormatInfo;

      private readonly byte SessionTrack;

      private readonly byte Reserved1;

      private readonly byte Reserved2;

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

    #region Public Methods

    public static void GetCdTextInfo(SafeFileHandle hDevice, out MMC.CDTextDescriptor cdText) {
      var req = new TOCRequest(MMC.TOCRequestFormat.CDText);
      if (!NativeApi.DeviceIoControl(hDevice, ref req, out cdText, out var len)) {
        throw new IOException("Failed to retrieve CD-TEXT information.", new Win32Exception(Marshal.GetLastWin32Error()));
      }
      cdText.FixUp();
      var expected = cdText.DataLength + Marshal.SizeOf(cdText.DataLength);
      if (len != expected) {
        Debug.Print($"I/O: CD-TEXT descriptor has data length as {expected} but only {len} bytes were read.");
      }
      if (len < expected) {
        throw new IOException($"CD-TEXT Retrieval: the structure says its size is {expected} but only {len} bytes were read.");
      }
    }

    public static string GetMediaCatalogNumber(SafeFileHandle hDevice) {
      var req = new SubChannelRequest {
        Format = MMC.SubChannelRequestFormat.MediaCatalogNumber,
        Track = 0,
      };
      if (!NativeApi.DeviceIoControl(hDevice, ref req, out MMC.SubChannelMediaCatalogNumber mcn, out var len)) {
        throw new IOException("Failed to retrieve media catalog number.", new Win32Exception(Marshal.GetLastWin32Error()));
      }
      mcn.FixUp();
      var expected = mcn.Header.DataLength + Marshal.SizeOf(mcn.Header);
      if (len != expected) {
        Debug.Print($"I/O: MCN has data length as {expected} but {len} bytes were read.");
      }
      if (len < expected) {
        throw new IOException($"MCN Retrieval: the structure says its size is {expected} but only {len} bytes were read.");
      }
      return mcn.Status.IsValid ? Encoding.ASCII.GetString(mcn.MCN) : string.Empty;
    }

    public static void GetTableOfContents(SafeFileHandle hDevice, out MMC.TOCDescriptor rawTableOfContents) {
      var req = new TOCRequest(MMC.TOCRequestFormat.TOC);
      // LIB-44: Apparently for some multi-session discs, the first TOC read can be wrong. So issue two reads.
      var ok = NativeApi.DeviceIoControl(hDevice, ref req, out rawTableOfContents, out var len);
      if (ok) {
        ok = NativeApi.DeviceIoControl(hDevice, ref req, out rawTableOfContents, out len);
      }
      if (!ok) {
        throw new IOException("Failed to retrieve TOC.", new Win32Exception(Marshal.GetLastWin32Error()));
      }
      rawTableOfContents.FixUp(req.AddressAsMSF);
      var expected = rawTableOfContents.DataLength + Marshal.SizeOf(rawTableOfContents.DataLength);
      if (len != expected) {
        Debug.Print($"I/O: TOC descriptor has data length as {expected} but only {len} bytes were read.");
      }
      if (len < expected) {
        throw new IOException($"TOC Retrieval: the structure says its size is {expected} but only {len} bytes were read.");
      }
    }

    public static string GetTrackIsrc(SafeFileHandle hDevice, byte track) {
      var req = new SubChannelRequest {
        Format = MMC.SubChannelRequestFormat.ISRC,
        Track = track,
      };
      if (!NativeApi.DeviceIoControl(hDevice, ref req, out MMC.SubChannelISRC isrc, out var len)) {
        throw new IOException($"Failed to retrieve ISRC for track {track}.", new Win32Exception(Marshal.GetLastWin32Error()));
      }
      isrc.FixUp();
      var expected = isrc.Header.DataLength + Marshal.SizeOf(isrc.Header);
      if (len != expected) {
        Debug.Print($"I/O: ISRC has data length as {expected} but {len} bytes were read.");
      }
      if (len < expected) {
        throw new IOException($"ISRC Retrieval: the structure says its size is {expected} but only {len} bytes were read.");
      }
      return isrc.Status.IsValid ? Encoding.ASCII.GetString(isrc.ISRC) : string.Empty;
    }

    public static SafeFileHandle OpenDevice(string device) {
      var colon = device.IndexOf(':');
      if (colon >= 0) {
        device = device.Substring(0, colon + 1);
      }
      var path = string.Concat(@"\\.\", device);
      var handle = NativeApi.CreateFile(path, FileAccess.Read, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
      if (!handle.IsInvalid) {
        return handle;
      }
      var error = Marshal.GetLastWin32Error();
      throw new ArgumentException($"Cannot open device '{device}' (error {error:X8}).", new Win32Exception(error));
    }

    #endregion

    #region Private Methods - P/Invoke

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string filename, [MarshalAs(UnmanagedType.U4)] FileAccess access,
                                                    [MarshalAs(UnmanagedType.U4)] FileShare share, IntPtr securityAttributes,
                                                    [MarshalAs(UnmanagedType.U4)] FileMode mode, uint flags, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle hDevice, IOCTL command, ref SubChannelRequest request,
                                               int requestSize, out MMC.SubChannelMediaCatalogNumber data, int dataSize,
                                               out int pBytesReturned, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle hDevice, IOCTL command, ref SubChannelRequest request,
                                               int requestSize, out MMC.SubChannelISRC data, int dataSize, out int pBytesReturned,
                                               IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle hDevice, IOCTL command, ref TOCRequest request, int nInBufferSize,
                                               out MMC.CDTextDescriptor data, int dataSize, out int pBytesReturned,
                                               IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle hDevice, IOCTL command, ref TOCRequest request, int nInBufferSize,
                                               out MMC.TOCDescriptor data, int dataSize, out int pBytesReturned,
                                               IntPtr overlapped);

    #endregion

    #region Private Methods - Convenience

    private static bool DeviceIoControl(SafeFileHandle hDevice, ref SubChannelRequest request, out MMC.SubChannelISRC data,
                                        out int length) {
      const IOCTL command = IOCTL.CDROM_READ_Q_CHANNEL;
      var requestSize = Marshal.SizeOf<SubChannelRequest>();
      var dataSize = Marshal.SizeOf<MMC.SubChannelISRC>();
      return NativeApi.DeviceIoControl(hDevice, command, ref request, requestSize, out data, dataSize, out length, IntPtr.Zero);
    }

    private static bool DeviceIoControl(SafeFileHandle hDevice, ref SubChannelRequest request,
                                        out MMC.SubChannelMediaCatalogNumber data, out int length) {
      const IOCTL command = IOCTL.CDROM_READ_Q_CHANNEL;
      var requestSize = Marshal.SizeOf<SubChannelRequest>();
      var dataSize = Marshal.SizeOf<MMC.SubChannelMediaCatalogNumber>();
      return NativeApi.DeviceIoControl(hDevice, command, ref request, requestSize, out data, dataSize, out length, IntPtr.Zero);
    }

    private static bool DeviceIoControl(SafeFileHandle hDevice, ref TOCRequest request, out MMC.CDTextDescriptor data,
                                        out int length) {
      const IOCTL command = IOCTL.CDROM_READ_TOC_EX;
      var requestSize = Marshal.SizeOf<TOCRequest>();
      var dataSize = Marshal.SizeOf<MMC.CDTextDescriptor>();
      return NativeApi.DeviceIoControl(hDevice, command, ref request, requestSize, out data, dataSize, out length, IntPtr.Zero);
    }

    private static bool DeviceIoControl(SafeFileHandle hDevice, ref TOCRequest request, out MMC.TOCDescriptor data,
                                        out int length) {
      const IOCTL command = IOCTL.CDROM_READ_TOC_EX;
      var requestSize = Marshal.SizeOf<TOCRequest>();
      var dataSize = Marshal.SizeOf<MMC.TOCDescriptor>();
      return NativeApi.DeviceIoControl(hDevice, command, ref request, requestSize, out data, dataSize, out length, IntPtr.Zero);
    }

    #endregion

  }

  #endregion

}
