using MetaBrainz.MusicBrainz.DiscId.Standards;

namespace MetaBrainz.MusicBrainz.DiscId;

internal struct Track(int address, MMC.SubChannelControl control = MMC.SubChannelControl.TwoChannelAudio, string? isrc = null) {

  public readonly int Address = address;

  public readonly MMC.SubChannelControl Control = control;

  public readonly string? Isrc = isrc;

}
