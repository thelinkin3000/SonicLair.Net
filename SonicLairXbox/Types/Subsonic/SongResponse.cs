using SonicLair.Services;
using SonicLair.Types.SonicLair;

namespace SonicLair.Types.Subsonic
{
    public class SongResponse : SubsonicResponse
    {
        public Song Song { get; set; }

        public SongResponse() : base()
        {
        }
    }
}