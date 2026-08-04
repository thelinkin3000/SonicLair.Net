using System;
using System.Threading.Tasks;

using Tmds.DBus;

namespace SonicLair.Cli.Services.Mpris
{
    [DBusInterface("org.mpris.MediaPlayer2")]
    public interface IMediaPlayer2 : IDBusObject
    {
        Task RaiseAsync();
        Task QuitAsync();

        Task<object> GetAsync(string prop);
        Task<MediaPlayer2Properties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    public class MediaPlayer2Properties
    {
        public bool CanQuit;
        public bool CanRaise;
        public bool HasTrackList;
        public string Identity;
        public string DesktopEntry;
        public string[] SupportedUriSchemes;
        public string[] SupportedMimeTypes;
    }
}
