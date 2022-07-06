using System.Collections.Generic;
using SonicLair.Services;
using SonicLair.Types.SonicLair;

namespace SonicLair.Types.Subsonic
{
    public class ArtistsResponse : SubsonicResponse
    {
        public InnerArtistsResponse Artists { get; set; }

        public ArtistsResponse() : base()
        {
        }
    }

    public class InnerArtistsResponse
    {
        public string IgnoredArticles { get; set; }
        public List<ArtistIndex> Index { get; set; }
    }

    public class ArtistIndex
    {
        public string Name { get; set; }
        public string Length { get; set; }
        public List<Artist> Artist { get; set; }
    }
}