using SonicLair.Lib.Services;
using SonicLair.Lib.Types.SonicLair;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Tmds.DBus;

namespace SonicLair.Cli.Services.Mpris
{
    // Backs the MPRIS2 object at /org/mpris/MediaPlayer2, bridging the
    // org.mpris.MediaPlayer2(.Player) D-Bus interfaces onto IMusicPlayerService
    // so desktop panels (GNOME Shell, KDE, Waybar, playerctl...) can see and
    // control what SonicLair is playing.
    internal sealed class MprisMediaPlayer : IMediaPlayer2, IMediaPlayer2Player
    {
        public static readonly ObjectPath Path = new("/org/mpris/MediaPlayer2");
        private static readonly ObjectPath NoTrack = new("/org/mpris/MediaPlayer2/TrackList/NoTrack");

        public ObjectPath ObjectPath => Path;

        private readonly IMusicPlayerService _player;
        private readonly object _gate = new();
        private readonly List<Action<PropertyChanges>> _rootWatchers = new();
        private readonly List<Action<PropertyChanges>> _playerWatchers = new();
        private readonly List<Action<long>> _seekedWatchers = new();

        // mpv starts at volume 100 (see MpvMusicPlayerService); IMusicPlayerService has no
        // volume getter, only change notifications, so we track the last known value ourselves.
        private float _lastKnownVolume = 1.0f;

        public MprisMediaPlayer(IMusicPlayerService player)
        {
            _player = player;
            _player.RegisterCurrentStateHandler(OnCurrentStateChanged);
            _player.RegisterPlayerVolumeHandler(OnVolumeChanged);
        }

        // ---- org.mpris.MediaPlayer2 ----

        public Task RaiseAsync() => Task.CompletedTask;

        public Task QuitAsync() => Task.CompletedTask;

        Task<object> IMediaPlayer2.GetAsync(string prop) => Task.FromResult(GetRootProperty(prop));

        Task<MediaPlayer2Properties> IMediaPlayer2.GetAllAsync() => Task.FromResult(BuildRootProperties());

        Task IMediaPlayer2.SetAsync(string prop, object val) => Task.CompletedTask; // nothing writable at root

        Task<IDisposable> IMediaPlayer2.WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            lock (_gate) { _rootWatchers.Add(handler); }
            return Task.FromResult<IDisposable>(new Unsubscriber(() =>
            {
                lock (_gate) { _rootWatchers.Remove(handler); }
            }));
        }

        private static MediaPlayer2Properties BuildRootProperties() => new()
        {
            CanQuit = false,
            CanRaise = false,
            HasTrackList = false,
            Identity = "SonicLair",
            DesktopEntry = "soniclair",
            SupportedUriSchemes = Array.Empty<string>(),
            SupportedMimeTypes = Array.Empty<string>(),
        };

        private static object GetRootProperty(string prop)
        {
            var props = BuildRootProperties();
            return prop switch
            {
                nameof(MediaPlayer2Properties.CanQuit) => props.CanQuit,
                nameof(MediaPlayer2Properties.CanRaise) => props.CanRaise,
                nameof(MediaPlayer2Properties.HasTrackList) => props.HasTrackList,
                nameof(MediaPlayer2Properties.Identity) => props.Identity,
                nameof(MediaPlayer2Properties.DesktopEntry) => props.DesktopEntry,
                nameof(MediaPlayer2Properties.SupportedUriSchemes) => props.SupportedUriSchemes,
                nameof(MediaPlayer2Properties.SupportedMimeTypes) => props.SupportedMimeTypes,
                _ => throw new DBusException("org.freedesktop.DBus.Error.UnknownProperty", $"Unknown property {prop}"),
            };
        }

        // ---- org.mpris.MediaPlayer2.Player ----

        public Task NextAsync()
        {
            _player.Next();
            return Task.CompletedTask;
        }

        public Task PreviousAsync()
        {
            _player.Prev();
            return Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            _player.Pause();
            return Task.CompletedTask;
        }

        public Task PlayPauseAsync()
        {
            _player.PlayPause();
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _player.Pause();
            return Task.CompletedTask;
        }

        public Task PlayAsync()
        {
            _player.Play();
            return Task.CompletedTask;
        }

        public Task SeekAsync(long offset)
        {
            var track = _player._currentTrack;
            if (track == null || track.Duration <= 0)
            {
                return Task.CompletedTask;
            }

            var positionBeforeSeek = _player.GetCurrentState().Position;
            var fractionDelta = offset / 1_000_000f / track.Duration;
            _player.Seek(fractionDelta, relative: true);

            RaiseSeeked(ToMicroseconds(positionBeforeSeek, track.Duration) + offset);
            return Task.CompletedTask;
        }

        public Task SetPositionAsync(ObjectPath trackId, long position)
        {
            var track = _player._currentTrack;
            if (track == null || track.Duration <= 0 || trackId != BuildTrackId(track))
            {
                return Task.CompletedTask;
            }

            var fraction = position / 1_000_000f / track.Duration;
            _player.Seek(fraction, relative: false);
            RaiseSeeked(position);
            return Task.CompletedTask;
        }

        public Task OpenUriAsync(string uri) => Task.CompletedTask; // not supported: SonicLair only plays subsonic tracks

        public Task<IDisposable> WatchSeekedAsync(Action<long> handler)
        {
            lock (_gate) { _seekedWatchers.Add(handler); }
            return Task.FromResult<IDisposable>(new Unsubscriber(() =>
            {
                lock (_gate) { _seekedWatchers.Remove(handler); }
            }));
        }

        Task<object> IMediaPlayer2Player.GetAsync(string prop) => Task.FromResult(GetPlayerProperty(prop));

        Task<MediaPlayer2PlayerProperties> IMediaPlayer2Player.GetAllAsync() => Task.FromResult(BuildPlayerProperties());

        Task IMediaPlayer2Player.SetAsync(string prop, object val)
        {
            switch (prop)
            {
                case nameof(MediaPlayer2PlayerProperties.Volume):
                    _player.SetVolume((int)Math.Round(Convert.ToDouble(val) * 100), relative: false);
                    break;
                case nameof(MediaPlayer2PlayerProperties.Shuffle):
                    if ((bool)val != _player.GetCurrentState().IsShuffled)
                    {
                        _player.Shuffle();
                    }
                    break;
                // Rate/LoopStatus aren't supported by the underlying player: accept and ignore.
            }
            return Task.CompletedTask;
        }

        Task<IDisposable> IMediaPlayer2Player.WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            lock (_gate) { _playerWatchers.Add(handler); }
            return Task.FromResult<IDisposable>(new Unsubscriber(() =>
            {
                lock (_gate) { _playerWatchers.Remove(handler); }
            }));
        }

        private MediaPlayer2PlayerProperties BuildPlayerProperties()
        {
            var state = _player.GetCurrentState();
            var track = state.CurrentTrack;
            var entries = state.CurrentPlaylist?.Entry ?? new List<Song>();
            var index = track == null ? -1 : entries.IndexOf(track);

            return new MediaPlayer2PlayerProperties
            {
                PlaybackStatus = PlaybackStatusOf(state),
                LoopStatus = "None",
                Rate = 1.0,
                Shuffle = state.IsShuffled,
                Metadata = BuildMetadata(track),
                Volume = _lastKnownVolume,
                Position = ToMicroseconds(state.Position, track?.Duration ?? 0),
                MinimumRate = 1.0,
                MaximumRate = 1.0,
                CanGoNext = index >= 0 && index < entries.Count - 1,
                CanGoPrevious = index > 0,
                CanPlay = entries.Count > 0,
                CanPause = true,
                CanSeek = track != null,
                CanControl = true,
            };
        }

        private object GetPlayerProperty(string prop)
        {
            var props = BuildPlayerProperties();
            return prop switch
            {
                nameof(MediaPlayer2PlayerProperties.PlaybackStatus) => props.PlaybackStatus,
                nameof(MediaPlayer2PlayerProperties.LoopStatus) => props.LoopStatus,
                nameof(MediaPlayer2PlayerProperties.Rate) => props.Rate,
                nameof(MediaPlayer2PlayerProperties.Shuffle) => props.Shuffle,
                nameof(MediaPlayer2PlayerProperties.Metadata) => props.Metadata,
                nameof(MediaPlayer2PlayerProperties.Volume) => props.Volume,
                nameof(MediaPlayer2PlayerProperties.Position) => props.Position,
                nameof(MediaPlayer2PlayerProperties.MinimumRate) => props.MinimumRate,
                nameof(MediaPlayer2PlayerProperties.MaximumRate) => props.MaximumRate,
                nameof(MediaPlayer2PlayerProperties.CanGoNext) => props.CanGoNext,
                nameof(MediaPlayer2PlayerProperties.CanGoPrevious) => props.CanGoPrevious,
                nameof(MediaPlayer2PlayerProperties.CanPlay) => props.CanPlay,
                nameof(MediaPlayer2PlayerProperties.CanPause) => props.CanPause,
                nameof(MediaPlayer2PlayerProperties.CanSeek) => props.CanSeek,
                nameof(MediaPlayer2PlayerProperties.CanControl) => props.CanControl,
                _ => throw new DBusException("org.freedesktop.DBus.Error.UnknownProperty", $"Unknown property {prop}"),
            };
        }

        private static string PlaybackStatusOf(CurrentState state) =>
            state.CurrentTrack == null ? "Stopped" : state.IsPlaying ? "Playing" : "Paused";

        private static long ToMicroseconds(decimal fraction, int durationSeconds) =>
            durationSeconds <= 0 ? 0 : (long)(fraction * durationSeconds * 1_000_000m);

        private static IDictionary<string, object> BuildMetadata(Song? track)
        {
            if (track == null)
            {
                return new Dictionary<string, object> { ["mpris:trackid"] = NoTrack };
            }

            var metadata = new Dictionary<string, object>
            {
                ["mpris:trackid"] = BuildTrackId(track),
                ["mpris:length"] = (long)track.Duration * 1_000_000L,
                ["xesam:title"] = track.Title ?? "",
                ["xesam:album"] = track.Album ?? "",
                ["xesam:artist"] = new[] { track.Artist ?? "" },
                ["xesam:trackNumber"] = track.Track,
            };
            if (!string.IsNullOrEmpty(track.Image))
            {
                metadata["mpris:artUrl"] = track.Image;
            }
            return metadata;
        }

        private static ObjectPath BuildTrackId(Song track)
        {
            if (string.IsNullOrEmpty(track.Id))
            {
                return NoTrack;
            }
            var sanitized = new string(track.Id.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            return new ObjectPath($"/net/soniclair/Track/{sanitized}");
        }

        private void OnCurrentStateChanged(object? sender, CurrentStateChangedEventArgs e)
        {
            var props = BuildPlayerProperties();
            RaisePlayerPropertiesChanged(new[]
            {
                PropertyChanges.ForProperty(nameof(MediaPlayer2PlayerProperties.PlaybackStatus), props.PlaybackStatus),
                PropertyChanges.ForProperty(nameof(MediaPlayer2PlayerProperties.Metadata), props.Metadata),
                PropertyChanges.ForProperty(nameof(MediaPlayer2PlayerProperties.Shuffle), props.Shuffle),
                PropertyChanges.ForProperty(nameof(MediaPlayer2PlayerProperties.CanGoNext), props.CanGoNext),
                PropertyChanges.ForProperty(nameof(MediaPlayer2PlayerProperties.CanGoPrevious), props.CanGoPrevious),
                PropertyChanges.ForProperty(nameof(MediaPlayer2PlayerProperties.CanPlay), props.CanPlay),
                PropertyChanges.ForProperty(nameof(MediaPlayer2PlayerProperties.CanSeek), props.CanSeek),
            });
        }

        private void OnVolumeChanged(object? sender, PlayerVolumeChangedEventArgs e)
        {
            _lastKnownVolume = e.Volume;
            RaisePlayerPropertiesChanged(new[]
            {
                PropertyChanges.ForProperty(nameof(MediaPlayer2PlayerProperties.Volume), (double)e.Volume),
            });
        }

        private void RaisePlayerPropertiesChanged(IEnumerable<PropertyChanges> changes)
        {
            var merged = new Dictionary<string, object>();
            foreach (var change in changes)
            {
                foreach (var kv in change.Changed)
                {
                    merged[kv.Key] = kv.Value;
                }
            }
            var combined = new PropertyChanges(merged.ToArray(), Array.Empty<string>());

            Action<PropertyChanges>[] handlers;
            lock (_gate) { handlers = _playerWatchers.ToArray(); }
            foreach (var handler in handlers)
            {
                try { handler(combined); } catch { /* a misbehaving watcher shouldn't break playback */ }
            }
        }

        private void RaiseSeeked(long positionMicroseconds)
        {
            Action<long>[] handlers;
            lock (_gate) { handlers = _seekedWatchers.ToArray(); }
            foreach (var handler in handlers)
            {
                try { handler(positionMicroseconds); } catch { /* a misbehaving watcher shouldn't break playback */ }
            }
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly Action _dispose;
            public Unsubscriber(Action dispose) => _dispose = dispose;
            public void Dispose() => _dispose();
        }
    }
}
