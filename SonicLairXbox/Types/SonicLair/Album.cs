using SonicLairXbox;

using System;
using System.Collections.Generic;

using Windows.UI.Xaml.Controls;

namespace SonicLair.Types.SonicLair
{
    public class Album : ISelectableCardItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CoverArt { get; set; }
        public int SongCount { get; set; }
        public string Created { get; set; }
        public string Artist { get; set; }
        public string ArtistId { get; set; }
        public int Duration { get; set; }
        public int Year { get; set; }
        public List<Song> Song { get; set; }
        public string FirstLine { get => Name; }
        public string SecondLine { get => Year.ToString(); }
        public string Image { get; set; }
    }
}
