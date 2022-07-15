using System;
using System.Collections.Generic;

namespace SonicLair.Lib.Types.SonicLair
{
    public class Artist
    {
        public override string ToString()
        {
            return Name;
        }


        public string Id { get; set; }
        public string Name { get; set; }
        public int AlbumCount { get; set; }
        public string ArtistImageUrl { get; set; }
        public List<Album> Album { get; set; }

    }
}