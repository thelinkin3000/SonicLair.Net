using SonicLair.Types.SonicLair;

using System.Collections.Generic;

namespace SonicLair.Types.Subsonic
{
    public class SearchResponse : SubsonicResponse
    {
        public InnerSearchResponse SearchResult3 { get; set; }
    }

    public class InnerSearchResponse
    {
        public List<Album> Album { get; set; }
        public List<Song> Song { get; set; }
    }

}