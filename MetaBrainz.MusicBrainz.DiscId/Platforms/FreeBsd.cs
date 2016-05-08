namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal sealed class FreeBsd : Unix {

    private static readonly string[] DeviceCandidates = { "/dev/cd0", "/dev/acd0" };

    public override string DefaultDevice => FreeBsd.DeviceCandidates[0];

    // TODO: Port disc_freebsd.c

  }

}
