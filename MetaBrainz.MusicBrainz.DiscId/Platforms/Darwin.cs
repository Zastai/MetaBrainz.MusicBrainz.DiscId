namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal sealed class Darwin : Unix {

    public override string DefaultDevice => "1";

    // TODO: Port disc_darwin.c (looks a lot harder than the others)

  }

}
