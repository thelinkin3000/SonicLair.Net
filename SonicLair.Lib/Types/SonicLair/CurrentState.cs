namespace SonicLair.Lib.Types.SonicLair
{
    public class CurrentState
    {
        public Song CurrentTrack { get; set; }
        public decimal Position { get; set; }
        public bool IsPlaying { get; set; }
        public Playlist CurrentPlaylist { get; set; }
        public bool IsShuffled { get; set; }
    }
}
