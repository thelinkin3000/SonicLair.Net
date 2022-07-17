using Newtonsoft.Json;

using SonicLair.Lib.Services;
using SonicLair.Lib.Types.SonicLair;

namespace SonicLair.Cli
{
    public class Headless
    {
        public CancellationToken Token { get; set; }
        private readonly CancellationTokenSource _tokenSource =  new CancellationTokenSource();
        public Headless()
        {

            Console.WriteLine("Headless mode!");
            // We get the token out so the main thread can listen to it
            Token = _tokenSource.Token;

        }

        public async void Configure(string role)
        {
            var _subsonicService = new SubsonicService();
            var _musicPlayerService = new MusicPlayerService(_subsonicService);
            try
            {
                var _messageServer = new WebSocketService(_subsonicService, _musicPlayerService, true, role);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't start the websockets server. " +
                    "Won't be able to control this instance from outside. " +
                    "Do you have permission to bind port 30001?" + ex.Message) ;
                _tokenSource.Cancel();
                return;
            }
            var path = Path.Join(Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".soniclair", "config.json");
            if (File.Exists(path))
            {
                try
                {
                    using (var configFile = File.OpenText(path))
                    {
                        var jsonConfig = configFile.ReadToEnd();
                        var account = JsonConvert.DeserializeObject<Account>(jsonConfig);
                        if (account == null)
                        {
                            Console.WriteLine("Error reading the config file. " +
                                "Please read the docs on github or launch the app in gui mode (no -h) to log in for the first time to your server.");
                            _tokenSource.Cancel();
                            return;
                        }
                        configFile.Close();
                        _subsonicService.Configure(account);
                        var a = await _subsonicService.GetArtists();
                        if(a != null && a.Any())
                        {
                            Console.WriteLine("Configured and listening!!");
                        }
                        else
                        {
                            Console.WriteLine("Error connecting to server. Please check your credentials and connection.");
                            _tokenSource.Cancel();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading the config file. " +
                                "Please read the docs on github or launch the app in gui mode (no -h) to log in for the first time to your server.");
                    _tokenSource.Cancel();
                }
            }
            else
            {
                Console.WriteLine("Error reading the config file. " +
                                "Please read the docs on github or launch the app in gui mode (no -h) to log in for the first time to your server.");
                _tokenSource.Cancel();
            }
        }
    }
}
