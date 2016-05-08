namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal sealed class Solaris : Unix {

    private static readonly string[] DeviceCandidates = { "/vol/dev/aliases/cdrom0", "/volumes/dev/aliases/cdrom0" };

    public override string DefaultDevice => Solaris.DeviceCandidates[0];

    // TODO: Port disc_solaris.c

  }

}
