using Newtonsoft.Json;

using SonicLair.Lib.Infrastructure;
using SonicLair.Lib.Services;
using SonicLair.Lib.Types;
using SonicLair.Lib.Types.SonicLair;

using System.Globalization;
using System.Runtime.InteropServices;

namespace SonicLair.Cli.Services
{
    public class MpvMusicPlayerService : IMusicPlayerService, IDisposable
    {
        private const ulong TimePosUserdata = 1;
        private const ulong PauseUserdata = 2;
        private const ulong VolumeUserdata = 3;

        private readonly IntPtr _mpv;
        private readonly Thread _eventThread;
        private volatile bool _running = true;
        private bool _isPlaying;

        private readonly ISubsonicService _client;
        private Playlist _playlist;
        private List<Song> _originalPlaylist;
        private readonly List<EventHandler<CurrentStateChangedEventArgs>> _currentStateListeners;
        private readonly List<EventHandler<PlayerTimeChangedEventArgs>> _playerTimeListeners;
        private readonly List<EventHandler<PlayerVolumeChangedEventArgs>> _playerVolumeListeners;
        private bool _isShuffling;
        public Song _currentTrack { get; private set; }
        public INotifier Notifier { get; set; }

        public MpvMusicPlayerService(ISubsonicService subsonicService)
        {
            _client = subsonicService;
            _isShuffling = false;
            _playlist = new Playlist()
            {
                CoverArt = "",
                Comment = "",
                Created = "",
                Duration = 0,
                Entry = new List<Song>(),
                Id = "",
                Image = "",
                Name = "",
                Owner = "",
                Public = false,
                SongCount = 0
            };
            _originalPlaylist = new List<Song>();
            _currentStateListeners = new List<EventHandler<CurrentStateChangedEventArgs>>();
            _playerTimeListeners = new List<EventHandler<PlayerTimeChangedEventArgs>>();
            _playerVolumeListeners = new List<EventHandler<PlayerVolumeChangedEventArgs>>();

            MpvNative.EnsureCNumericLocale();
            _mpv = MpvNative.mpv_create();
            if (_mpv == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create the mpv instance.");
            }

            // Audio-only, no UI/config surprises: this process already owns the terminal (Terminal.Gui).
            MpvNative.SetOptionString(_mpv, "config", "no");
            MpvNative.SetOptionString(_mpv, "vid", "no");
            MpvNative.SetOptionString(_mpv, "vo", "null");
            MpvNative.SetOptionString(_mpv, "terminal", "no");
            MpvNative.SetOptionString(_mpv, "input-terminal", "no");
            MpvNative.SetOptionString(_mpv, "idle", "yes");
            MpvNative.SetOptionString(_mpv, "volume", "100");

            var initResult = MpvNative.mpv_initialize(_mpv);
            if (initResult < 0)
            {
                throw new InvalidOperationException($"mpv_initialize failed: {MpvNative.GetErrorString(initResult)}");
            }

            MpvNative.ObserveProperty(_mpv, TimePosUserdata, "time-pos");
            MpvNative.ObserveProperty(_mpv, PauseUserdata, "pause");
            MpvNative.ObserveProperty(_mpv, VolumeUserdata, "volume");

            _eventThread = new Thread(EventLoop) { IsBackground = true };
            _eventThread.Start();
        }

        private void EventLoop()
        {
            while (_running)
            {
                var evtPtr = MpvNative.mpv_wait_event(_mpv, 1.0);
                var evt = Marshal.PtrToStructure<MpvNative.MpvEvent>(evtPtr);
                switch ((MpvNative.MpvEventId)evt.EventId)
                {
                    case MpvNative.MpvEventId.PropertyChange:
                        HandlePropertyChange(evt);
                        break;
                    case MpvNative.MpvEventId.EndFile:
                        ThreadPool.QueueUserWorkItem(_ => Next());
                        break;
                    case MpvNative.MpvEventId.Shutdown:
                        _running = false;
                        break;
                }
            }
        }

        private void HandlePropertyChange(MpvNative.MpvEvent evt)
        {
            if (evt.Data == IntPtr.Zero)
            {
                return;
            }
            var prop = Marshal.PtrToStructure<MpvNative.MpvEventProperty>(evt.Data);
            var value = MpvNative.ReadObservedString(prop);
            if (value == null)
            {
                return;
            }

            switch (evt.ReplyUserdata)
            {
                case TimePosUserdata:
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                    {
                        RaiseTimeChanged((long)(seconds * 1000));
                    }
                    break;
                case PauseUserdata:
                    _isPlaying = value == "no";
                    RaiseCurrentStateChanged();
                    if (Notifier != null)
                    {
                        Notifier.NotifyObservers(_isPlaying ? "MSplay" : "MSpaused");
                    }
                    break;
                case VolumeUserdata:
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var volume))
                    {
                        var mappedArgs = new PlayerVolumeChangedEventArgs { Volume = (float)(volume / 100d) };
                        try
                        {
                            foreach (var handler in _playerVolumeListeners)
                            {
                                handler.Invoke(this, mappedArgs);
                            }
                        }
                        catch (Exception)
                        {
                            // Concurrency is hard
                        }
                    }
                    break;
            }
        }

        private void RaiseTimeChanged(long timeMs)
        {
            try
            {
                var mappedArgs = new PlayerTimeChangedEventArgs { Time = timeMs };
                foreach (var handler in _playerTimeListeners)
                {
                    handler.Invoke(this, mappedArgs);
                }
            }
            catch (Exception)
            {
                // Concurrency is hard
            }
            if (_currentTrack != null && Notifier != null)
            {
                string t = ((timeMs / _currentTrack.Duration) / 1000d).ToString(new CultureInfo("en-US"));
                Notifier.NotifyObservers("MSprogress", $"{{\"time\": {t}}}");
            }
        }

        private void RaiseCurrentStateChanged()
        {
            try
            {
                foreach (var handler in _currentStateListeners)
                {
                    handler.Invoke(this, new CurrentStateChangedEventArgs()
                    {
                        CurrentState = GetCurrentState()
                    });
                }
            }
            catch (Exception)
            {
                // Concurrency is hard
            }
        }

        public void SetNotifier(INotifier notifier)
        {
            Notifier = notifier;
        }

        public void SetVolume(int v, bool relative = false)
        {
            var newVol = v;
            if (relative)
            {
                var current = (int)MpvNative.GetPropertyDouble(_mpv, "volume", 100);
                newVol = (current + v).Clamp(0, 100);
            }
            MpvNative.SetPropertyString(_mpv, "volume", newVol.ToString(CultureInfo.InvariantCulture));
        }

        public CurrentState GetCurrentState()
        {
            var percentPos = MpvNative.GetPropertyDouble(_mpv, "percent-pos", 0);
            return new CurrentState()
            {
                CurrentTrack = _currentTrack,
                Position = (decimal)(percentPos / 100d),
                IsPlaying = _isPlaying,
                CurrentPlaylist = _playlist,
                IsShuffled = _isShuffling
            };
        }

        public void Shuffle()
        {
            if (_isShuffling)
            {
                _playlist.Entry = _originalPlaylist;
            }
            else
            {
                if (_playlist.Entry.Count == 0)
                {
                    return;
                }
                _playlist.Entry.Shuffle();
                var first = _playlist.Entry[0];
                var index = _playlist.Entry.IndexOf(_currentTrack);
                _playlist.Entry[0] = _currentTrack;
                _playlist.Entry[index] = first;
            }
            _isShuffling = !_isShuffling;
            RaiseCurrentStateChanged();
        }

        public void PlayPause()
        {
            if (_isPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        public void Play()
        {
            if (_playlist.Entry.Count > 0 && _currentTrack == null)
            {
                _currentTrack = _playlist.Entry[0];
                LoadMedia();
            }
            MpvNative.SetPropertyString(_mpv, "pause", "no");
        }

        public void SkipTo(int index)
        {
            if (index >= 0 && index < _playlist.Entry.Count)
            {
                _currentTrack = _playlist.Entry[index];
            }
            LoadMedia();
            Play();
            RaiseCurrentStateChanged();
        }

        public void Pause()
        {
            MpvNative.SetPropertyString(_mpv, "pause", "yes");
        }

        public void Seek(float time, bool relative = false)
        {
            if (!relative && time < 0)
            {
                // Roads? Where we're going we don't need roads.
                return;
            }
            float position = 0;
            if (relative)
            {
                position = (float)(MpvNative.GetPropertyDouble(_mpv, "percent-pos", 0) / 100d);
            }
            var targetPercent = (position + time).Clamp(0, 1) * 100;
            MpvNative.Command(_mpv, "seek", targetPercent.ToString(CultureInfo.InvariantCulture), "absolute-percent");
        }

        private void LoadMedia()
        {
            var uri = _client.GetSongUri(_currentTrack.Id);
            MpvNative.Command(_mpv, "loadfile", uri.AbsoluteUri);
            if (Notifier != null)
            {
                Notifier.NotifyObservers("MScurrentTrack", $"{{\"currentTrack\": {JsonConvert.SerializeObject(_currentTrack, StaticHelpers.GetJsonSerializerSettings())}}}");
            }
            _client.Scrobble(_currentTrack.Id);
        }

        public void Next()
        {
            if (_playlist.Entry.IndexOf(_currentTrack) == _playlist.Entry.Count - 1)
            {
                return;
            }
            _currentTrack = _playlist.Entry[_playlist.Entry.IndexOf(_currentTrack) + 1];
            LoadMedia();
            Play();
            RaiseCurrentStateChanged();
        }

        public void Prev()
        {
            if (_playlist.Entry.IndexOf(_currentTrack) == 0)
            {
                return;
            }
            _currentTrack = _playlist.Entry[_playlist.Entry.IndexOf(_currentTrack) - 1];
            LoadMedia();
            Play();
            RaiseCurrentStateChanged();
        }

        private int GetPlaylistDuration(List<Song> songs)
        {
            return songs.Sum(s => s.Duration);
        }

        public void AddToCurrentPlaylist(Song song)
        {
            _playlist.Entry.Add(song);
            _originalPlaylist.Add(song);
            RaiseCurrentStateChanged();
        }

        public async Task PlayPlaylist(string id, int track)
        {
            try
            {
                var playlist = await _client.GetPlaylist(id);
                _playlist = playlist;
                _originalPlaylist = playlist.Entry.ToList();
            }
            catch (SubsonicException ex)
            {
                if (Notifier != null)
                {
                    Notifier.NotifyObservers("EX", ex.Message);
                }
                return;
            }
            LoadImagesAndPlay(track);
        }

        private void LoadImagesAndPlay(int track = 0)
        {
            foreach (var s in _playlist.Entry)
            {
                s.Image = _client.GetCoverArtUri(s.AlbumId);
            }
            _currentTrack = _playlist.Entry[track];
            LoadMedia();
            Play();
            RaiseCurrentStateChanged();
        }

        public async Task PlayRadio(string id)
        {
            try
            {
                var songs = await _client.GetSimilarSongs(id);
                var song = await _client.GetSong(id);
                songs = songs.Prepend(song).ToList();
                _playlist = new Playlist(
                    "",
                    $"Radio based on {songs[0].Title}",
                    $"by {songs[0].Artist} from {songs[0].Album}",
                    _client.GetActiveAccount().Username,
                    false,
                    songs.Count + 1,
                    GetPlaylistDuration(songs),
                    "",
                    "",
                    songs
                    );
                _originalPlaylist = _playlist.Entry.ToList();
            }
            catch (SubsonicException ex)
            {
                if (Notifier != null)
                {
                    Notifier.NotifyObservers("EX", ex.Message);
                }
                return;
            }
            LoadImagesAndPlay();
        }

        public async Task PlayAlbum(string id, int track = 0)
        {
            try
            {
                var album = await _client.GetAlbum(id);
                _playlist = new Playlist(
                    "",
                    album.Name,
                    $"by {album.Artist}",
                    _client.GetActiveAccount().Username,
                    false,
                    album.Song.Count + 1,
                    GetPlaylistDuration(album.Song),
                    "",
                    "",
                    album.Song
                    );
                _originalPlaylist = _playlist.Entry.ToList();
            }
            catch (SubsonicException ex)
            {
                if (Notifier != null)
                {
                    Notifier.NotifyObservers("EX", ex.Message);
                }
                return;
            }

            LoadImagesAndPlay(track);
        }

        public void RegisterPlayerVolumeHandler(EventHandler<PlayerVolumeChangedEventArgs> handler)
        {
            _playerVolumeListeners.Add(handler);
        }

        public void UnregisterPlayerVolumeHandler(EventHandler<PlayerVolumeChangedEventArgs> handler)
        {
            if (_playerVolumeListeners.Contains(handler))
            {
                _playerVolumeListeners.Remove(handler);
            }
        }

        public void RegisterCurrentStateHandler(EventHandler<CurrentStateChangedEventArgs> handler)
        {
            _currentStateListeners.Add(handler);
        }

        public void UnregisterCurrentStateHandler(EventHandler<CurrentStateChangedEventArgs> handler)
        {
            if (_currentStateListeners.Contains(handler))
            {
                _currentStateListeners.Remove(handler);
            }
        }

        public void RegisterTimeChangedHandler(EventHandler<PlayerTimeChangedEventArgs> handler)
        {
            _playerTimeListeners.Add(handler);
        }

        public void UnregisterTimeChangedHandler(EventHandler<PlayerTimeChangedEventArgs> handler)
        {
            if (_playerTimeListeners.Contains(handler))
            {
                _playerTimeListeners.Remove(handler);
            }
        }

        public void Dispose()
        {
            _running = false;
            MpvNative.mpv_terminate_destroy(_mpv);
            if (_eventThread.IsAlive)
            {
                _eventThread.Join(TimeSpan.FromSeconds(2));
            }
        }
    }
}
