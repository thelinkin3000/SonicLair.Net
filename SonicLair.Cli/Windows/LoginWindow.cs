using Newtonsoft.Json;

using SonicLair.Cli;
using SonicLair.Lib.Services;
using SonicLair.Lib.Types.SonicLair;

using System.Text;

using Terminal.Gui;

namespace SonicLairCli
{
    public class LoginWindow : IWindowFrame
    {
        private readonly Toplevel _top;
        private readonly ISubsonicService _service;
        private TextField loginText;
        private TextField passText;
        private TextField urlText;
        private MainWindow _main;

        public LoginWindow(Toplevel top, MainWindow main)
        {
            _top = top;
            _service = new SubsonicService();
            _main = main;
        }

        private async void LoginClick()
        {
            if (loginText.Text == null || loginText.Text.IsEmpty ||
                passText.Text == null || passText.Text.IsEmpty ||
                urlText.Text == null || urlText.Text.IsEmpty)
            {
                MessageBox.ErrorQuery("Error", "At least one field is empty. Please, fill out all fields.", "Ok");
                return;
            }
            else
            {
                Login(loginText.Text.ToString()!.Trim(),
                passText.Text.ToString()!.Trim(),
                urlText.Text.ToString()!.Trim());
            }
        }

        private async void Login(string user, string password, string url)
        {
            Account account = new Account()
            {
                Username = user,
                Password = password,
                Url = url
            };
            _service.Configure(account);
            try
            {
                var artists = await _service.GetArtists();
                var directory = Path.Join(Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".soniclair");
                Directory.CreateDirectory(directory);
                var path = Path.Join(directory, "config.json");
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    using (var configFile = File.OpenWrite(path))
                    {
                        var json = JsonConvert.SerializeObject(account);
                        var bytes = Encoding.UTF8.GetBytes(json);
                        configFile.Write(bytes);
                        configFile.Flush();
                        configFile.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery("Error!", $"Error writing the config file! You'll have to login again next time. {ex.Message}", "Ok");
                }
                Statics.SetActiveAccount(account);
                _main.Load();
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", ex.Message, "Ok");
                return;
            }
        }

        public void Load()
        {
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
                            MessageBox.ErrorQuery("Error!", "Error reading the config file. Log in again and the app will recreate it.", "Ok");
                            return;
                        }
                        configFile.Close();
                        Login(account.Username, account.Password, account.Url);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery("Error!", "Error reading the config file. Log in again and the app will recreate it.", "Ok");
                }
            }
            // Creates the top-level window to show
            var win = new Window("SonicLair")
            {
                X = 0,
                Y = 0, // Leave one row for the toplevel menu

                // By using Dim.Fill(), it will automatically resize without manual intervention
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };

            var logo = new Label(@"                                      -
                                     +*+
                                   .+****:
                                  :*******-
                                 -*********=               .:-
                                =***********+.      .:-=+*****.
                               =*************+.-=+************.
                             .+**************  -**********+++*.
                       -=:  .****************   :**++=-:.   =*.
                      =****=*****************  -++. :++     =*.
                     =***********************  =***+***+    =*.
                   .+************************  =*******+-.: =*.
                  .**************************  =*****-    +***.
                 :***********************+++*  =****-      =**.
                -*********************=.       =****:       -*.
               =*********************=         =*****:       =
              +**********************-         =******+-::-=**+  .-
         =*-.*************************.       :****************+=**=
       .+******************************+-:::-+**********************+.
      :***************************************************************.
     :*****************************************************************:
    =*******************************************************************-
   +*********************************************************************=
 .+***********************************************************************+.")
            {
                X = Pos.Center() - 38,
                Y = Pos.Center() - 12,
            };

            var login = new Label("Login: ") { X = 0, Y = 0 };
            var password = new Label("Password: ")
            {
                X = Pos.Left(login),
                Y = Pos.Top(login) + 1
            };
            var url = new Label("Url: ") { X = Pos.Left(password), Y = Pos.Top(password) + 1 };
            loginText = SonicLairControls.GetTextField("");
            loginText.X = Pos.Right(password);
            loginText.Y = Pos.Top(login);
            loginText.Width = 40;

            passText = SonicLairControls.GetTextField("");
            passText.Secret = true;
            passText.X = Pos.Left(loginText);
            passText.Y = Pos.Top(password);
            passText.Width = Dim.Width(loginText);

            urlText = SonicLairControls.GetTextField("");
            urlText.X = Pos.Left(passText);
            urlText.Y = Pos.Top(url);
            urlText.Width = Dim.Width(passText);

            var loginBtn = new Button("Login")
            {
                X = Pos.Left(urlText),
                Y = Pos.Top(urlText) + 1,
            };

            loginBtn.Clicked += LoginClick;
            var quit = new Button("Quit!")
            {
                X = Pos.Right(loginBtn) + 1,
                Y = Pos.Top(loginBtn),
            };
            quit.Clicked += () =>
            {
                Application.RequestStop();
            };

            win.Add(
                logo,
                login,
                password,
                loginText,
                passText,
                urlText,
                url,
                loginBtn,
                quit
            );
            _top.RemoveAll();
            _top.Add(win);
        }
    }
}