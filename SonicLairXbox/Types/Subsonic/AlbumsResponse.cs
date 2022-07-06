using SonicLair.Services;
using SonicLair.Types.SonicLair;

using System.Collections.Generic;

namespace SonicLair.Types.Subsonic
{
    public class AlbumsResponse : SubsonicResponse
    {
        public InnerAlbumResponse AlbumList2 { get; set; }

        public AlbumsResponse() : base()
        {
        }
    }

    public class InnerAlbumResponse
    {
        public List<Album> Album { get; set; }
    }

}