using System;
using System.IO;

using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId;

internal struct Track(int address, MMC.SubChannelControl control = MMC.SubChannelControl.TwoChannelAudio, string? isrc = null) {

  public readonly int Address = address;

  public readonly MMC.SubChannelControl Control = control;

  public readonly string? Isrc = isrc;

  public static Track[] Import(byte first, byte last, MMC.TrackDescriptor[] rawTracks, Func<byte, string?>? getTrackIsrc) {
    var tracks = new Track[last + 1];
    var i = 0;
    // Add the regular tracks.
    for (var trackNo = first; trackNo <= last; ++trackNo, ++i) {
      if (rawTracks[i].TrackNumber != trackNo) {
        var msg = $"Internal logic error; first track is {first}, but entry at index {i} claims to be track " +
                  $"{rawTracks[i].TrackNumber} instead of {trackNo}.";
        throw new InvalidDataException(msg);
      }
      tracks[trackNo] = new Track(rawTracks[i].Address, rawTracks[i].ControlAndADR.Control, getTrackIsrc?.Invoke(trackNo) ?? "");
    }
    // Next entry should be the lead-out (track number 0xAA)
    if (rawTracks[i].TrackNumber != 0xAA) {
      var msg = $"Internal logic error; track data ends with a record that reports track number {rawTracks[i].TrackNumber} " +
                $"instead of 170 (0xAA, lead-out).";
      throw new InvalidDataException(msg);
    }
    tracks[0] = new Track(rawTracks[i].Address, rawTracks[i].ControlAndADR.Control);
    return tracks;
  }

}
