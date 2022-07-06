using SonicLair.Types.SonicLair;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonicLairXbox.Types.SonicLair
{
    public class SearchResult
    {
        public SearchResult(List<Song> songs, List<Album> albums)
        {
            Songs = songs;
            Albums = albums;
        }
        public List<Song> Songs { get; set; }
        public List<Album> Albums { get; set; }
    }
}
