using SonicLair.Lib.Infrastructure;

using System.Text.Json.Serialization;

namespace SonicLair.Lib.Types.SonicLair
{
    public class Song : ISelectableCardItem
    {
        public Song(string id, string parent, int track, string title, string artist, string album, string albumId, int duration, string coverArt)
        {
            Id = id;
            Parent = parent;
            Track = track;
            Title = title;
            Artist = artist;
            Album = album;
            AlbumId = albumId;
            Duration = duration;
            CoverArt = coverArt;
        }

        public static Song GetDefault()
        {
            return new Song("", "", 0, "", "", "", "", 0, "");
        }

        public string Id { get; set; }
        public string Parent { get; set; }
        public int Track { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string AlbumId { get; set; }
        public int Duration { get; set; }
        public string CoverArt { get; set; }

        [JsonIgnore]
        public string GetSecondLine => $"by {Artist} from {Album}";

        [JsonIgnore]
        public string GetDuration => Duration.GetAsMMSS();

        [JsonIgnore]
        public string FirstLine => Title;

        [JsonIgnore]
        public string SecondLine => GetSecondLine;

        [JsonIgnore]
        public string Image { get; set; }
        public override string ToString()
        {
            return Title;
        }
    }
}