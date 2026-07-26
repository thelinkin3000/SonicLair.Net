using SonicLair.Lib.Services;

namespace SonicLair.Cli.Services
{
    public static class MusicPlayerServiceFactory
    {
        public static bool UseMpv { get; set; }

        public static IMusicPlayerService Create(ISubsonicService subsonicService)
        {
            if (UseMpv)
            {
                return new MpvMusicPlayerService(subsonicService);
            }
            return new MusicPlayerService(subsonicService);
        }
    }
}
