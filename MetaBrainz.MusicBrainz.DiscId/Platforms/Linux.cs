namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal sealed class Linux : Unix {

    public override string DefaultDevice => "/dev/cdrom";

    // TODO: Port disc_linux.c

  }

}
