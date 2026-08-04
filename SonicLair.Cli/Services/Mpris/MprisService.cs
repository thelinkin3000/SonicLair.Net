using SonicLair.Lib.Services;

using System;
using System.Threading.Tasks;

using Tmds.DBus;

namespace SonicLair.Cli.Services.Mpris
{
    // Exposes an MPRIS2 player (org.mpris.MediaPlayer2.soniclair) on the session
    // D-Bus so desktop panels/widgets (GNOME Shell, KDE Plasma, Waybar, playerctl...)
    // can see what's playing and send play/pause/next/prev back to us.
    public sealed class MprisService : IDisposable
    {
        private const string ServiceName = "org.mpris.MediaPlayer2.soniclair";

        private readonly IMusicPlayerService _player;
        private Connection? _connection;

        public MprisService(IMusicPlayerService player)
        {
            _player = player;
        }

        public async Task StartAsync()
        {
            var connection = new Connection(Address.Session);
            await connection.ConnectAsync();
            await connection.RegisterObjectAsync(new MprisMediaPlayer(_player));
            await connection.RegisterServiceAsync(ServiceName, ServiceRegistrationOptions.ReplaceExisting);
            _connection = connection;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
