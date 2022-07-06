﻿using SonicLair.Types.SonicLair;

using System.Collections.Generic;

namespace SonicLair.Types.Subsonic
{
    public class SimilarSongsResponse : SubsonicResponse
    {
        public InnerSongListResponse SimilarSongs2 { get; set; }
    }

    public class InnerSongListResponse
    {
        public List<Song> Song { get; set; }
    }

    public class RandomSongsResponse : SubsonicResponse
    {
        public InnerSongListResponse RandomSongs { get; set; }
    }

}