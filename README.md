# MetaBrainz.MusicBrainz.DiscId [![Build Status](https://img.shields.io/appveyor/build/zastai/metabrainz-musicbrainz-discid)](https://ci.appveyor.com/project/Zastai/metabrainz-musicbrainz-discid) [![NuGet Version](https://img.shields.io/nuget/v/MetaBrainz.MusicBrainz.DiscId)](https://www.nuget.org/packages/MetaBrainz.MusicBrainz.DiscId)

This is a .NET implementation of libdiscid.
The main point of divergence at this point is that fewer platforms are supported (see below), and that this library supports retrieval of CD-TEXT information.

It uses PInvoke to access devices so is platform-dependent; currently, Windows, Linux and BSD (FreeBSD, NetBSD and OpenBSD) are supported.
However, things should work regardless of the host implementation (.NET Framework, Mono or .NET Core).

Support for Solaris is unlikely, because there does not seem to be any easy way to get Mono to work on it.
Support for OSX is similarly unlikely, because I have no access to a system.

## Release Notes

### v2.0 (2020-03-22)

- Target .NET Standard 2.0 and 2.1, .NET Core 2.1 and 3.1 (the current LTS releases) and .NET Framework 4.6.1, 4.7.2 and 4.8.
- Minor doc fixes.
- Use nullable reference types.

### v1.0.1 (2018-11-15)

Corrected the build so that the IntelliSense XML documentation is property built and packaged.

### v1.0 (2018-01-21)

First official release.

- Dropped support for .NET framework versions before 4.0 (and 4.0 may be dropped in a later version); this allows for builds using .NET Core (which cannot target 2.0/3.5).
- Added support for .NET Standard 2.0; the only unsupported API is RawImage.Decode() (because System.Drawing.Image is not available).
