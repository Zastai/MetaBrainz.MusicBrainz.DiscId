namespace MetaBrainz.MusicBrainz.DiscId.Platforms {

  internal sealed class NetBsd : Unix {

    private static readonly string[] DeviceCandidates = { "/dev/rcd0c", "/dev/rcd0d" };

    public override string DefaultDevice => NetBsd.DeviceCandidates[0];

    // TODO: Port disc_netbsd.c

  }

}
