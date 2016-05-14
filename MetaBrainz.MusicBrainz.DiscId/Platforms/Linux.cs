using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal sealed class Linux : Unix {

    public Linux() : base(CdDeviceFeature.ReadTableOfContents | CdDeviceFeature.ReadMediaCatalogNumber) { }

    public override string DefaultDevice => "/dev/cdrom";

    public override string GetDeviceByIndex(int n) {
      if (n < 0)
        return null;
      using (var info = System.IO.File.OpenText("/proc/sys/dev/cdrom/info")) {
        string line;
        while ((line = info.ReadLine()) != null) {
          if (line.StartsWith("drive name:")) {
            var devices = line.Substring(11).Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (n >= devices.Length)
              return null;
            Array.Reverse(devices);
            return string.Concat("/dev/", devices[n]);
          }
        }
      }
      return null;
    }

    public override TableOfContents ReadTableOfContents(string device, CdDeviceFeature features) {
      if (device == null) {
        // Prefer the generic name
        using (var fd = this.OpenDevice(device = this.DefaultDevice)) {
          if (fd.IsInvalid) // But if that does not exist, try the first specific one
            device = this.GetDeviceByIndex(0);
        }
        if (device == null) // But we do need a device at this point
          throw new NotSupportedException("No cd-rom device found.");
      }
      using (var fd = this.OpenDevice(device)) {
        if (fd.IsInvalid)
          throw new IOException($"Failed to open '{device}'.", new UnixException());
        byte first = 0;
        byte last  = 0;
        {
          var tochdr = new TOCHeader();
          if (Linux.ReadTOCHeader(fd, IOCTL.CDROMREADTOCHDR, ref tochdr) == -1)
            throw new IOException("Failed to read TOC header.", new UnixException());
          first = tochdr.FirstTrack;
          last  = tochdr.LastTrack;
        }
        var tracks = new TableOfContents.RawTrack[last + 1];
        for (var i = first; i <= last; ++i) {
          var tocentry = new TOCEntry { TrackNumber = i, Format = TOCAddressFormat.LBA };
          if (Linux.ReadTOCEntry(fd, IOCTL.CDROMREADTOCENTRY, ref tocentry) == -1)
            throw new IOException($"Failed to read TOC entry for track {i}.", new UnixException());
          tocentry.FixUp();
          tracks[i] = new TableOfContents.RawTrack(tocentry.Address, tocentry.ControlAndADR.Control, null);
        }
        { // Lead-Out is track 0xAA
          var tocentry = new TOCEntry { TrackNumber = 0xAA, Format = TOCAddressFormat.LBA };
          if (Linux.ReadTOCEntry(fd, IOCTL.CDROMREADTOCENTRY, ref tocentry) == -1)
            throw new IOException("Failed to read TOC entry for lead-out.", new UnixException());
          tocentry.FixUp();
          tracks[0] = new TableOfContents.RawTrack(tocentry.Address, tocentry.ControlAndADR.Control, null);
        }
        string mcn = null;
        if ((features & CdDeviceFeature.ReadMediaCatalogNumber) != 0) {
          var rawmcn = new MCN();
          if (Linux.ReadMCN(fd, IOCTL.CDROM_GET_MCN, ref rawmcn) == -1)
            throw new IOException("Failed to read media catalog number.", new UnixException());
          mcn = Encoding.ASCII.GetString(rawmcn.Data).TrimEnd('\0');
        }
        return new TableOfContents(device, first, last, tracks, mcn);
      }
    }

    #region System Commands

    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Local

    [Flags]
    internal enum FileOpenFlags : uint {
      // Access Modes
      ReadOnly              = 0x0000, // O_RDONLY
      WriteOnly             = 0x0001, // O_WRONLY
      ReadWrite             = 0x0002, // O_RDWR
      AccessMode            = 0x0003, // O_ACCMODE
      // Open-Time Flags
      Create                = 0x0040, // O_CREAT
      Exclusive             = 0x0080, // O_EXCL
      CreateNew             = Create | Exclusive,
      NonBlocking           = 0x0800, // O_NONBLOCK
      NoControllingTerminal = 0x0100, // O_NOCTTY
      Truncate              = 0x0200, // O_TRUNC
      // Operation Modes
      Append                = 0x0400, // O_APPEND
    }

    private Unix.SafeUnixHandle OpenDevice(string name) => Unix.Open(name, (uint) (FileOpenFlags.ReadOnly | FileOpenFlags.NonBlocking), 0);

    #region ioctl

    #region Constants

    enum IOCTL : int {
      CDROMPAUSE           = 0x5301, // Pause Audio Operation
      CDROMRESUME          = 0x5302, // Resume paused Audio Operation
      CDROMPLAYMSF         = 0x5303, // Play Audio MSF (struct cdrom_msf)
      CDROMPLAYTRKIND      = 0x5304, // Play Audio Track/index (struct cdrom_ti)
      CDROMREADTOCHDR      = 0x5305, // Read TOC header (struct cdrom_tochdr)
      CDROMREADTOCENTRY    = 0x5306, // Read TOC entry (struct cdrom_tocentry)
      CDROMSTOP            = 0x5307, // Stop the cdrom drive
      CDROMSTART           = 0x5308, // Start the cdrom drive
      CDROMEJECT           = 0x5309, // Ejects the cdrom media
      CDROMVOLCTRL         = 0x530a, // Control output volume (struct cdrom_volctrl)
      CDROMSUBCHNL         = 0x530b, // Read subchannel data (struct cdrom_subchnl)
      CDROMREADMODE2       = 0x530c, // Read CDROM mode 2 data (2336 Bytes) (struct cdrom_read)
      CDROMREADMODE1       = 0x530d, // Read CDROM mode 1 data (2048 Bytes) (struct cdrom_read)
      CDROMREADAUDIO       = 0x530e, // (struct cdrom_read_audio)
      CDROMEJECT_SW        = 0x530f, // enable(1)/disable(0) auto-ejecting
      CDROMMULTISESSION    = 0x5310, // Obtain the start-of-last-session address of multi session disks (struct cdrom_multisession)
      CDROM_GET_MCN        = 0x5311, // Obtain the "Universal Product Code" if available (struct cdrom_mcn)
      CDROM_GET_UPC        = CDROM_GET_MCN, // This one is deprecated, but here anyway for compatibility
      CDROMRESET           = 0x5312, // hard-reset the drive
      CDROMVOLREAD         = 0x5313, // Get the drive's volume setting (struct cdrom_volctrl)
      CDROMREADRAW         = 0x5314, // read data in raw mode (2352 Bytes) (struct cdrom_read)
      CDROMREADCOOKED      = 0x5315, // read data in cooked mode
      CDROMSEEK            = 0x5316, // seek msf address
      CDROMPLAYBLK         = 0x5317, // (struct cdrom_blk)
      CDROMREADALL         = 0x5318, // read all 2646 bytes
      CDROMGETSPINDOWN     = 0x531d, //
      CDROMSETSPINDOWN     = 0x531e, //
      CDROMCLOSETRAY       = 0x5319, // pendant of CDROMEJECT
      CDROM_SET_OPTIONS    = 0x5320, // Set behavior options
      CDROM_CLEAR_OPTIONS  = 0x5321, // Clear behavior options
      CDROM_SELECT_SPEED   = 0x5322, // Set the CD-ROM speed
      CDROM_SELECT_DISC    = 0x5323, // Select disc (for juke-boxes)
      CDROM_MEDIA_CHANGED  = 0x5325, // Check is media changed
      CDROM_DRIVE_STATUS   = 0x5326, // Get tray position, etc.
      CDROM_DISC_STATUS    = 0x5327, // Get disc type, etc.
      CDROM_CHANGER_NSLOTS = 0x5328, // Get number of slots
      CDROM_LOCKDOOR       = 0x5329, // lock or unlock door
      CDROM_DEBUG          = 0x5330, // Turn debug messages on/off
      CDROM_GET_CAPABILITY = 0x5331, // get capabilities
      CDROMAUDIOBUFSIZ     = 0x5382, // set the audio buffer size - conflict with SCSI_IOCTL_GET_IDLUN
      DVD_READ_STRUCT      = 0x5390, // Read structure
      DVD_WRITE_STRUCT     = 0x5391, // Write structure
      DVD_AUTH             = 0x5392, // Authentication
      CDROM_SEND_PACKET    = 0x5393, // send a packet to the drive
      CDROM_NEXT_WRITABLE  = 0x5394, // get next writable block
      CDROM_LAST_WRITTEN   = 0x5395, // get last block written on disc
    }

    enum TOCAddressFormat : byte {
      None = 0x00,
      LBA  = 0x01,
      MSF  = 0x02,
    }

    #endregion

    #region Structures

    // The Linux interface decided not to use the structures defined by MMC3. Yay.

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SubChannelControlAndADR {

      public byte Byte;

      // And again, Linux inverts things for no obvious reason.
      public MMC3.SubChannelDataFormat ADR     => (MMC3.SubChannelDataFormat) ((this.Byte >> 0) & 0x0f);
      public MMC3.SubChannelControl    Control => (MMC3.SubChannelControl)    ((this.Byte >> 4) & 0x0f);

    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    private struct TOCEntry {

      public byte                    TrackNumber;
      public SubChannelControlAndADR ControlAndADR;
      public TOCAddressFormat        Format;
      public int                     Address;
      public byte                    DataMode;

      public TimeSpan                TimeCode => new TimeSpan(0, 0, 0, 0, this.Address * 1000 / 75);

      public void FixUp() {
        if (this.Format == TOCAddressFormat.MSF) { // MSF -> Sectors
          // In MMC3, this is stored (seen as raw bytes) as 0x00, 0xmm, 0xss, 0xff; Linux uses 0xmm 0xss 0xff 0x00. Yay.
          this.Address = System.Net.IPAddress.NetworkToHostOrder(this.Address);
          var m = (byte) (this.Address >> 24 & 0xff);
          var s = (byte) (this.Address >> 16 & 0xff);
          var f = (byte) (this.Address >>  8 & 0xff);
          this.Address = (m * 60 + s) * 75 + f;
        }
        else // LBA -> Sectors
          this.Address += 150;
      }

    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    private struct TOCHeader {

      public byte FirstTrack;
      public byte LastTrack;

    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    private struct MCN {

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
      public byte[] Data;
      public byte   Zero;

    }

    #endregion

    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    private static extern int ReadMCN(SafeUnixHandle fd, IOCTL command, ref MCN mcn);

    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    private static extern int ReadTOCEntry(SafeUnixHandle fd, IOCTL command, ref TOCEntry entry);

    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    private static extern int ReadTOCHeader(SafeUnixHandle fd, IOCTL command, ref TOCHeader header);

    #endregion

    #endregion

  }

}
