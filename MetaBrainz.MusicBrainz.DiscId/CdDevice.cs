using System;
using System.Collections.Generic;
using System.IO;

using JetBrains.Annotations;

namespace MetaBrainz.MusicBrainz {

  /// <summary>Class representing a cd-rom device.</summary>
  public sealed class CdDevice {

    #region Static Properties / Methods

    /// <summary>The default cd-rom device used.</summary>
    public static string DefaultName => CdDevice.GetCdDrive(0) ?? "D:";

    /// <summary>The default URL scheme to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property).</summary>
    public static string DefaultUrlScheme { get; set; } = "https";

    /// <summary>
    ///   The default web site to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property).
    ///   Must not include any URL scheme; that can be configured via <see cref="DefaultUrlScheme"/>.
    /// </summary>
    public static string DefaultWebSite { get; set; } = "musicbrainz.org";

    /// <summary>The list of supported features.</summary>
    public static IEnumerable<string> Features {
      get {
        if (CdDevice.HasFeature(CdDeviceFeature.ReadTableOfContents   )) yield return "read";
        if (CdDevice.HasFeature(CdDeviceFeature.ReadMediaCatalogNumber)) yield return "mcn";
        if (CdDevice.HasFeature(CdDeviceFeature.ReadTrackIsrc         )) yield return "isrc";
        if (CdDevice.HasFeature(CdDeviceFeature.ReadCdText            )) yield return "text";
      }
    }

    /// <summary>Returns the name of the <paramref name="n"/>th cd-rom drive in the system.</summary>
    /// <param name="n">The (0-based) sequence number of the cd-rom drive.</param>
    /// <returns>The request drive name, or null if there are not enough cd-drives in the system.</returns>
    public static string GetCdDrive(byte n) {
      foreach (var drive in DriveInfo.GetDrives()) {
        if (drive.DriveType == DriveType.CDRom) {
          if (n == 0)
            return drive.Name;
          --n;
        }
      }
      return null;
    }

    /// <summary>Determines whether or not the specified feature is supported.</summary>
    /// <param name="feature">The (single) feature to test.</param>
    /// <returns>true if the feature is supported; false otherwise.</returns>
    public static bool HasFeature(CdDeviceFeature feature) {
      switch (feature) {
        case CdDeviceFeature.ReadTableOfContents:
        case CdDeviceFeature.ReadMediaCatalogNumber:
        case CdDeviceFeature.ReadTrackIsrc:
        case CdDeviceFeature.ReadCdText:
          return true;
        default:
          return false;
      }
    }

    #endregion

    #region Constructors / Destructors

    /// <summary>Constructs a new <see cref="CdDevice" /> object using the default device name (<see cref="DefaultName" />).</summary>
    /// <exception cref="ArgumentException">When the specified device cannot be opened.</exception>
    public CdDevice() : this(CdDevice.DefaultName) { }

    /// <summary>Constructs a new <see cref="CdDevice" /> object using the specified device.</summary>
    /// <param name="device">The cd-rom device's sequence number (0 being the first cd-rom device in the system).</param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="device" /> equals or exceeds the number of cd-rom devices in the system.</exception>
    public CdDevice(byte device) {
      var deviceName = CdDevice.GetCdDrive(device);
      if (deviceName == null)
        throw new ArgumentOutOfRangeException(nameof(device), device, "No such cd-rom device was found.");
      this.Name = deviceName;
    }

    /// <summary>Constructs a new <see cref="CdDevice" /> object using the specified device.</summary>
    /// <param name="device">The name of the device to open (typically of the form &quot;X:&quot;).</param>
    /// <exception cref="ArgumentNullException">When <paramref name="device" /> is null.</exception>
    public CdDevice([CanBeNull] string device) {
      if (device == null)
        throw new ArgumentNullException(nameof(device));
      this.Name = device;
    }

    #endregion

    #region Instance Properties / Methods

    /// <summary>The name of this cd-rom device.</summary>
    public string Name { get; }

    /// <summary>The MusicBrainz Disc ID for the last disc read by <see cref="ReadDisc"/>.</summary>
    public string DiscId => this.TableOfContents?.DiscId;

    /// <summary>The FreeDB ID for the last disc read by <see cref="ReadDisc"/>.</summary>
    public string FreeDbId => this.TableOfContents?.FreeDbId;

    /// <summary>The media catalog number (typically the UPC/EAN) for the last disc read by <see cref="ReadDisc"/>; null if not retrieved, empty if not available.</summary>
    public string MediaCatalogNumber => this.TableOfContents?.MediaCatalogNumber;

    /// <summary>The URL to open to submit information about the last disc read by <see cref="ReadDisc"/> to MusicBrainz; null if not retrieved, empty if not available.</summary>
    public Uri SubmissionUrl => this.TableOfContents?.SubmissionUrl;

    /// <summary>The table of contents for the last disc read by <see cref="ReadDisc"/>; null if not retrieved, empty if not available.</summary>
    public TableOfContents TableOfContents { get; private set; }

    /// <summary>Reads the current disc in the device, getting the requested information.</summary>
    /// <param name="features">The features to use (if supported). Note that the table of contents will always be read.</param>
    public void ReadDisc(CdDeviceFeature features = CdDeviceFeature.All) {
      // Forget current info
      this.TableOfContents = null;
      var includeMcn  = (features & CdDeviceFeature.ReadMediaCatalogNumber) != 0 && CdDevice.HasFeature(CdDeviceFeature.ReadMediaCatalogNumber);
      var includeIsrc = (features & CdDeviceFeature.ReadTrackIsrc         ) != 0 && CdDevice.HasFeature(CdDeviceFeature.ReadTrackIsrc         );
      var includeText = (features & CdDeviceFeature.ReadCdText            ) != 0 && CdDevice.HasFeature(CdDeviceFeature.ReadCdText            );
      this.TableOfContents = WinApi.GetTableOfContents(this.Name, includeMcn, includeIsrc, includeText);
    }

    #endregion

  }

}
