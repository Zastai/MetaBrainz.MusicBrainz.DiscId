﻿using System;
using System.Collections.Generic;
using System.IO;

using JetBrains.Annotations;

namespace MetaBrainz.MusicBrainz.DiscId {

  /// <summary>Class representing a cd-rom device.</summary>
  public sealed class CdDevice {

    #region Static Properties / Methods

    private static IPlatform _platform;

    static CdDevice() {
      switch (Environment.OSVersion.Platform) {
        case PlatformID.Win32NT:
        case PlatformID.Win32S:
        case PlatformID.Win32Windows:
        case PlatformID.WinCE:
        case PlatformID.Xbox:
          CdDevice._platform = new WindowsPlatform();
          break;
        case PlatformID.MacOSX:
          // FIXME: Is this really ever reported? Mono seems to always say Unix.
          break;
        case PlatformID.Unix:
          CdDevice._platform = UnixPlatform.Create();
          break;
      }
      if (CdDevice._platform == null)
        CdDevice._platform = new GenericPlatform();
      // Mono's C# compiler does not like initializers on auto-properties, so set them up here instead.
      CdDevice.DefaultPort      = -1;
      CdDevice.DefaultUrlScheme = "https";
      CdDevice.DefaultWebSite   = "musicbrainz.org";
    }

    /// <summary>The default cd-rom device used.</summary>
    public static string DefaultName => CdDevice._platform.GetDeviceByIndex(0) ?? CdDevice._platform.DefaultDevice;

    /// <summary>The default port number to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property); -1 means no explicit port is used.</summary>
    public static int DefaultPort { get; set; }

    /// <summary>The default URL scheme to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property).</summary>
    public static string DefaultUrlScheme { get; set; }

    /// <summary>
    ///   The default web site to use when constructing URLs (i.e. for the <see cref="SubmissionUrl"/> property).
    ///   Must not include any URL scheme; that can be configured via <see cref="DefaultUrlScheme"/>.
    /// </summary>
    public static string DefaultWebSite { get; set; }

    /// <summary>The list of supported features.</summary>
    public static IEnumerable<string> Features => CdDevice._platform.Features;

    /// <summary>Returns the name of the <paramref name="n"/>th cd-rom device in the system.</summary>
    /// <param name="n">The (0-based) sequence number of the cd-rom device.</param>
    /// <returns>The requested drive name, or null if there are not enough cd-drives in the system.</returns>
    public static string GetName(byte n) => CdDevice._platform.GetDeviceByIndex(n);

    /// <summary>Determines whether or not the specified feature is supported.</summary>
    /// <param name="feature">The (single) feature to test.</param>
    /// <returns>true if the feature is supported; false otherwise.</returns>
    public static bool HasFeature(CdDeviceFeature feature) => CdDevice._platform.HasFeature(feature);

    #endregion

    #region Constructors / Destructors

    /// <summary>Constructs a new <see cref="CdDevice" /> object using the default device name (<see cref="DefaultName" />).</summary>
    /// <exception cref="ArgumentException">When the specified device cannot be opened.</exception>
    public CdDevice() : this(CdDevice.DefaultName) { }

    /// <summary>Constructs a new <see cref="CdDevice" /> object using the specified device.</summary>
    /// <param name="device">The cd-rom device's sequence number (0 being the first cd-rom device in the system).</param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="device" /> equals or exceeds the number of cd-rom devices in the system.</exception>
    public CdDevice(int device) {
      this.Name = CdDevice._platform.GetDeviceByIndex(device);
      if (this.Name == null)
        throw new ArgumentOutOfRangeException(nameof(device), device, "No such cd-rom device was found.");
    }

    /// <summary>Constructs a new <see cref="CdDevice" /> object using the specified device.</summary>
    /// <param name="device">The name of the device to open.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="device" /> is null.</exception>
    /// <remarks>The device name is not validated by this constructor; if it is not valid, <see cref="ReadDisc"/> will fail.</remarks>
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
      this.TableOfContents = CdDevice._platform.ReadTableOfContents(this.Name, features);
    }

    /// <summary>Simulates the reading of a disc, setting up a table of contents based on the specified information.</summary>
    /// <param name="first">The first audio track for the disc.</param>
    /// <param name="last">The last audio track for the disc.</param>
    /// <param name="offsets">Array of track offsets; the offset at index 0 should be the offset of the end of the last (audio) track.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="offsets"/> is null.</exception>
    public void SimulateDisc(byte first, byte last, [CanBeNull] int[] offsets) {
      if (offsets == null)
        throw new ArgumentNullException(nameof(offsets));
      this.TableOfContents = new TableOfContents(first, last, offsets);
    }

    #endregion

  }

}