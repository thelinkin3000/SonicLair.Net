

using SonicLair.Lib.Types.SonicLair;

using System.Collections.Generic;

namespace SonicLair.Lib.Types.Subsonic
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