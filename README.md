# MetaBrainz.MusicBrainz.DiscId [![Build Status][CI-S]][CI-L] [![NuGet Version][NuGet-S]][NuGet-L]

This is a .NET implementation of [libdiscid][libdiscid].

The main point of divergence at this point is that fewer platforms are
supported (see below), and that this library supports retrieval of
CD-TEXT information.

It uses PInvoke to access devices so is platform-dependent; currently,
Windows, Linux and FreeBSD are supported.

Support for macOS is unlikely, because I have no access to a system.

[libdiscid]: https://github.com/metabrainz/libdiscid

[CI-S]: https://github.com/Zastai/MetaBrainz.MusicBrainz.DiscId/actions/workflows/build.yml/badge.svg
[CI-L]: https://github.com/Zastai/MetaBrainz.MusicBrainz.DiscId/actions/workflows/build.yml

[NuGet-S]: https://img.shields.io/nuget/v/MetaBrainz.MusicBrainz.DiscId
[NuGet-L]: https://www.nuget.org/packages/MetaBrainz.MusicBrainz.DiscId
