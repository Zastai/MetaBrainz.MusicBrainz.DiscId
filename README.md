# MetaBrainz.MusicBrainz.DiscId [![Build Status](https://img.shields.io/appveyor/build/zastai/metabrainz-musicbrainz-discid)](https://ci.appveyor.com/project/Zastai/metabrainz-musicbrainz-discid) [![NuGet Version](https://img.shields.io/nuget/v/MetaBrainz.MusicBrainz.DiscId)](https://www.nuget.org/packages/MetaBrainz.MusicBrainz.DiscId)

This is a .NET implementation of libdiscid.
The main point of divergence at this point is that fewer platforms are supported (see below), and that this library supports retrieval of CD-TEXT information.

It uses PInvoke to access devices so is platform-dependent; currently, Windows, Linux and BSD (FreeBSD, NetBSD and OpenBSD) are supported.
However, things should work regardless of the host implementation (.NET Framework, Mono or .NET Core).

Support for Solaris is unlikely, because there does not seem to be any easy way to get Mono to work on it.
Support for OSX is similarly unlikely, because I have no access to a system.
