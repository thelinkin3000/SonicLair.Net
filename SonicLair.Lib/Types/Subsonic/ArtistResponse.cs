using SonicLair.Lib.Types.SonicLair;

namespace SonicLair.Lib.Types.Subsonic
{
    public class ArtistResponse : SubsonicResponse
    {
        public Artist Artist { get; set; }

        public ArtistResponse() : base()
        {
        }
    }

    public class ArtistInfoResponse : SubsonicResponse
    {
        public string Bio { get; set; }
        public string Cover { get; set; }

        public ArtistInfoResponse() : base()
        {
        }
    }
}