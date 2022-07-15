using LibVLCSharp.Shared;

using SonicLair.Lib.Types;
using SonicLair.Lib.Types.SonicLair;

using System;
using System.Threading.Tasks;

namespace SonicLair.Lib.Services
{
    public interface IMusicPlayerService
    {
        Song _currentTrack { get; }

        CurrentState GetCurrentState();
        void Next();
        Task PlayAlbum(string id, int track = 0);
        void Prev();

        void RegisterCurrentStateHandler(EventHandler<CurrentStateChangedEventArgs> handler);
        void UnregisterCurrentStateHandler(EventHandler<CurrentStateChangedEventArgs> handler);
        void RegisterTimeChangedHandler(EventHandler<MediaPlayerTimeChangedEventArgs> handler);
        void UnregisterTimeChangedHandler(EventHandler<MediaPlayerTimeChangedEventArgs> handler);
        void Play();
        void Pause();
        Task PlayRadio(string id);
        void Seek(float time, bool relative = false);
        Task PlayPlaylist(string id, int track);
        void Shuffle();
        void SetNotifier(INotifier notifier);
        void PlayPause();
        void AddToCurrentPlaylist(Song song);
    }
}