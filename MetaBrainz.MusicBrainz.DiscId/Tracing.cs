using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MetaBrainz.MusicBrainz.DiscId;

internal static class Tracing {

  public static readonly TraceSource Source = new("MetaBrainz.MusicBrainz.DiscId", SourceLevels.Off);

  public static void Critical(int id, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string message, params object?[]? args)
    => Tracing.Source.TraceEvent(TraceEventType.Critical, id, message, args);

  public static void Error(int id, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string message, params object?[]? args)
    => Tracing.Source.TraceEvent(TraceEventType.Error, id, message, args);

  public static void Info(int id, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string message, params object?[]? args)
    => Tracing.Source.TraceEvent(TraceEventType.Information, id, message, args);

  public static void Verbose(int id, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string message, params object?[]? args)
    => Tracing.Source.TraceEvent(TraceEventType.Verbose, id, message, args);

  public static void Warning(int id, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string message, params object?[]? args)
    => Tracing.Source.TraceEvent(TraceEventType.Warning, id, message, args);

}
