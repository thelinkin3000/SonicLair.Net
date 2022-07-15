

using SonicLair.Lib.Types.SonicLair;

using System.Collections.Generic;

namespace SonicLair.Lib.Types.Subsonic
{
    public class PlaylistsResponse : SubsonicResponse
    {
        public PlaylistsInnerResponse Playlists { get; set; }
    }

    public class PlaylistsInnerResponse
    {
        public List<Playlist> Playlist { get; set; }
    }
    
    public class PlaylistResponse : SubsonicResponse
    {
        public Playlist Playlist { get; set; }
    }

    

}