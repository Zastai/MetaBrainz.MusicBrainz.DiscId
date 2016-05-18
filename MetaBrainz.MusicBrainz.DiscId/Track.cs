
namespace MetaBrainz.MusicBrainz.DiscId {

  internal struct Track {

    public Track(int address) {
      this.Address = address;
      this.Control = MMC.SubChannelControl.TwoChannelAudio;
      this.Isrc    = null;
    }

    public Track(int address, MMC.SubChannelControl control, string isrc) {
      this.Address = address;
      this.Control = control;
      this.Isrc    = isrc;
    }

    public readonly int                   Address;
    public readonly MMC.SubChannelControl Control;
    public readonly string                Isrc;

  }

}
