using SonicLair.Services;

namespace SonicLair.Types.Subsonic
{
    public class SubsonicResponse
    {
        public string Status { get; set; }
        public SubsonicError Error { get; set; }
        public string Version { get; set; }
        public string Type { get; set; }
    }

    public class SubsonicError
    {
        public string Message { get; set; }
        public string Code { get; set; }
    }
}

