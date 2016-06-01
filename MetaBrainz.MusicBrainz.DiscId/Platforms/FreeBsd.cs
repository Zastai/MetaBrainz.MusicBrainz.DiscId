namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal sealed class FreeBsd : Bsd {

    protected override bool AddressesAreNative => false;
 
    protected override string GetDevicePath(string device) => string.Concat("/dev/", device);


  }

}
