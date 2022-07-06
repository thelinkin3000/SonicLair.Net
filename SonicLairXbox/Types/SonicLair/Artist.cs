using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

using SonicLair.Types.Subsonic;

namespace SonicLair.Types.SonicLair
{
    public class Artist
    {


        public string Id { get; set; }
        public string Name { get; set; }
        public int AlbumCount { get; set; }
        public string ArtistImageUrl { get; set; }
        public List<Album> Album { get; set; }

    }
}