

using SonicLair.Lib.Types.SonicLair;

using System.Collections.Generic;

namespace SonicLair.Lib.Types.Subsonic
{
    public class SearchResponse : SubsonicResponse
    {
        public InnerSearchResponse SearchResult3 { get; set; }
    }

    public class InnerSearchResponse
    {
        public List<Album> Album { get; set; }
        public List<Song> Song { get; set; }
        public List<Artist> Artist { get; set; }
    }

}