# MetaBrainz.MusicBrainz.DiscId [![Build Status][CI-S]][CI-L] [![NuGet Version][NuGet-S]][NuGet-L]

This is a .NET implementation of [libdiscid].

The main point of divergence at this point is that fewer platforms are
supported (see below), and that this library supports retrieval of
CD-TEXT information.

It uses PInvoke to access devices so is platform-dependent; currently,
Windows, Linux and FreeBSD are supported.

Support for macOS is unlikely, because I have no access to a system.


## Debugging

The `TableOfContents` class provides a `TraceSource` that can be used to
configure debug output; its name is `MetaBrainz.MusicBrainz.DiscId`.

### Configuration

#### In Code

In code, you can enable tracing like follows:

```cs
// Use the default switch, turning it on.
TableOfContents.TraceSource.Switch.Level = SourceLevels.All;

// Alternatively, use your own switch so multiple things can be
// enabled/disabled at the same time.
var mySwitch = new TraceSwitch("MyAppDebugSwitch", "All");
TableOfContents.TraceSource.Switch = mySwitch;

// By default, there is a single listener that writes trace events to
// the debug output (typically only seen in an IDE's debugger). You can
// add (and remove) listeners as desired.
var listener = new ConsoleTraceListener {
  Name = "MyAppConsole",
  TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ProcessId,
};
TableOfContents.TraceSource.Listeners.Clear();
TableOfContents.TraceSource.Listeners.Add(listener);
```

#### In Configuration

Your application can also be set up to read tracing configuration from
the application configuration file. To do so, the following needs to be
added to its startup code:

```cs
System.Diagnostics.TraceConfiguration.Register();
```

(Provided by the `System.Configuration.ConfigurationManager` package.)

The application config file can then have a `system.diagnostics` section
where sources, switches and listeners can be configured.

```xml
<configuration>
  <system.diagnostics>
    <sharedListeners>
      <add name="console" type="System.Diagnostics.ConsoleTraceListener" traceOutputOptions="DateTime,ProcessId" />
    </sharedListeners>
    <sources>
      <source name="MetaBrainz.MusicBrainz.DiscId" switchName="MetaBrainz.MusicBrainz.DiscId">
        <listeners>
          <add name="console" />
          <add name="discid-log" type="System.Diagnostics.TextWriterTraceListener" initializeData="discid.log" />
        </listeners>
      </source>
    </sources>
    <switches>
      <add name="MetaBrainz.MusicBrainz.DiscId" value="All" />
    </switches>
  </system.diagnostics>
</configuration>
```

## Release Notes

These are available [on GitHub][release-notes].

[libdiscid]: https://github.com/metabrainz/libdiscid
[release-notes]: https://github.com/Zastai/MetaBrainz.MusicBrainz.DiscId/releases

[CI-S]: https://github.com/Zastai/MetaBrainz.MusicBrainz.DiscId/actions/workflows/build.yml/badge.svg
[CI-L]: https://github.com/Zastai/MetaBrainz.MusicBrainz.DiscId/actions/workflows/build.yml

[NuGet-S]: https://img.shields.io/nuget/v/MetaBrainz.MusicBrainz.DiscId
[NuGet-L]: https://www.nuget.org/packages/MetaBrainz.MusicBrainz.DiscId
