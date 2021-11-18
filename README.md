# MetaBrainz.MusicBrainz.DiscId [![Build Status][CI-S]][CI-L] [![NuGet Version][NuGet-S]][NuGet-L]

This is a .NET implementation of libdiscid.
The main point of divergence at this point is that fewer platforms are
supported (see below), and that this library supports retrieval of
CD-TEXT information.

It uses PInvoke to access devices so is platform-dependent; currently,
Windows, Linux and BSD (FreeBSD, NetBSD and OpenBSD) are supported.
However, things should work regardless of the host implementation
(.NET Framework, .NET Core, .NET or Mono).

Support for Solaris is unlikely, because there does not seem to be any
easy way to get Mono to work on it.
Support for OSX is similarly unlikely, because I have no access to a
system.

[CI-S]: https://img.shields.io/appveyor/build/zastai/metabrainz-musicbrainz-discid
[CI-L]: https://ci.appveyor.com/project/Zastai/metabrainz-musicbrainz-discid

[NuGet-S]: https://img.shields.io/nuget/v/MetaBrainz.MusicBrainz.DiscId
[NuGet-L]: https://www.nuget.org/packages/MetaBrainz.MusicBrainz.DiscId
