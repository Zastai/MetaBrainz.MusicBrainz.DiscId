namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal sealed class FreeBsd : Bsd {
 
    protected override string GetDevicePath(string device) {
      return string.Concat("/dev/", device);
    }

  }

}
