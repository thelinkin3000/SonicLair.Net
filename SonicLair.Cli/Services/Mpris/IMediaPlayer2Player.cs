using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Tmds.DBus;

namespace SonicLair.Cli.Services.Mpris
{
    [DBusInterface("org.mpris.MediaPlayer2.Player")]
    public interface IMediaPlayer2Player : IDBusObject
    {
        Task NextAsync();
        Task PreviousAsync();
        Task PauseAsync();
        Task PlayPauseAsync();
        Task StopAsync();
        Task PlayAsync();
        Task SeekAsync(long offset);
        Task SetPositionAsync(ObjectPath trackId, long position);
        Task OpenUriAsync(string uri);

        Task<IDisposable> WatchSeekedAsync(Action<long> handler);

        Task<object> GetAsync(string prop);
        Task<MediaPlayer2PlayerProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class MediaPlayer2PlayerProperties
    {
        public string PlaybackStatus;
        public string LoopStatus;
        public double Rate;
        public bool Shuffle;
        public IDictionary<string, object> Metadata;
        public double Volume;
        public long Position;
        public double MinimumRate;
        public double MaximumRate;
        public bool CanGoNext;
        public bool CanGoPrevious;
        public bool CanPlay;
        public bool CanPause;
        public bool CanSeek;
        public bool CanControl;
    }
}
