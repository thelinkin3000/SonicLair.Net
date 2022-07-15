using System.Collections.Generic;

namespace SonicLair.Lib.Types.SonicLair
{
    public class Playlist : ISelectableCardItem
    {
        public Playlist()
        {

        }

        public Playlist(string id, string name, string comment, string owner, bool @public, int songCount, int duration, string created, string coverArt, List<Song> entry)
        {
            Id = id;
            Name = name;
            Comment = comment;
            Owner = owner;
            Public = @public;
            SongCount = songCount;
            Duration = duration;
            Created = created;
            CoverArt = coverArt;
            Entry = entry;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public string Owner { get; set; }
        public bool Public { get; set; }
        public int SongCount { get; set; }
        public int Duration { get; set; }
        public string Created { get; set; }
        public string CoverArt { get; set; }
        public List<Song> Entry { get; set; }

        public string FirstLine => Name;

        public string SecondLine => Comment;

        public string Image { get ; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}
