
using SonicLair.Lib.Types.SonicLair;

namespace SonicLair.Lib.Types.Subsonic
{
    public class SongResponse : SubsonicResponse
    {
        public Song Song { get; set; }

        public SongResponse() : base()
        {
        }
    }
}