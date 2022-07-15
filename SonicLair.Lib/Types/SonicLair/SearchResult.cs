using System.Collections.Generic;

namespace SonicLair.Lib.Types.SonicLair
{
    public class SearchResult
    {
        public SearchResult(List<Song> songs, List<Album> albums, List<Artist> artists)
        {
            Songs = songs;
            Albums = albums;
            Artists = artists;
        }
        public List<Song> Songs { get; set; }
        public List<Album> Albums { get; set; }
        public List<Artist> Artists { get; set; }
    }
}
