using Newtonsoft.Json;

using SonicLair.Services;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// La plantilla de elemento Página en blanco está documentada en https://go.microsoft.com/fwlink/?LinkId=234238

namespace SonicLairXbox
{
    /// <summary>
    /// Una página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public class LayoutModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Visibility _connected;
        private int _height;
        private int _width;

        public LayoutModel()
        {
            _connected = Visibility.Collapsed;
        }

        public int Height
        {
            get { return _height; }
            set
            {
                if (_height != value)
                {
                    _height = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Height"));
                }
            }
        }
        public int Width
        {
            get { return _width; }
            set
            {
                if (_width != value)
                {
                    _width = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Width"));
                }
            }
        }

        public Visibility Connected
        {
            get
            {
                return _connected;
            }
            set
            {
                if (_connected != value)
                {
                    _connected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Connected"));

                }
            }
        }

    }
    public sealed partial class Layout : Page, INotificationObserver
    {
        private readonly SubsonicService _client;
        private readonly List<Button> _sidebarButtons;
        public LayoutModel Model { get; set; }
        public Layout()
        {
            this.InitializeComponent();
            Model = new LayoutModel();
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SetDesiredBoundsMode(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseCoreWindow);

            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (!localSettings.Containers.ContainsKey("soniclair") || !localSettings.Containers["soniclair"].Values.ContainsKey("activeAccount"))
            {
                // How did we end up here?!
                this.Frame.Navigate(typeof(MainPage));
            }
            var activeAccount = JsonConvert.DeserializeObject<Account>(localSettings.Containers["soniclair"].Values["activeAccount"].ToString());
            _client = new SubsonicService();
            _client.Configure(activeAccount);
            this.ContentFrame.Navigate(typeof(Home));
            ((App)App.Current).RegisterObserver(this);
            _sidebarButtons = new List<Button>()
            {
                btnHome,
                btnSearch,
                btnPlaylists,
                btnAccount,
                btnJukebox,
                btnPlaying
            };
            foreach (var button in _sidebarButtons)
            {
                button.KeyDown += SidebarButtonKeyDown;
            }
            Window.Current.SizeChanged += Current_SizeChanged;
            gr_menu.Loaded += Gr_menu_Loaded;
            gr_top.Loaded += Gr_top_Loaded;
        }

        private void Gr_top_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                Thread.Sleep(500);
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
            Model.Height = (int)Math.Floor(Window.Current.Bounds.Height - gr_top.ActualHeight - 30);
        });
            });
        }


        private void Gr_menu_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                Thread.Sleep(500);
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
            Model.Width = (int)Math.Floor(Window.Current.Bounds.Width - gr_menu.ActualWidth - 20);

        });
            });

        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            Model.Width = (int)Math.Floor(e.Size.Width - gr_menu.ActualWidth - 20);
            Model.Height = (int)Math.Floor(e.Size.Height - gr_top.ActualHeight - 30);
            Debug.WriteLine(e.Size);
        }

        private void SidebarButtonKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Right)
            {
                ((App)App.Current).NotifyObservers("focusContent");
            }
            var index = _sidebarButtons.IndexOf((Button)sender);
            if (e.Key == Windows.System.VirtualKey.Down && index < _sidebarButtons.Count - 1)
            {
                _sidebarButtons[index + 1].Focus(Windows.UI.Xaml.FocusState.Keyboard);
            }
            else if (e.Key == Windows.System.VirtualKey.Up && index > 0)
            {
                _sidebarButtons[index - 1].Focus(Windows.UI.Xaml.FocusState.Keyboard);
            }
            e.Handled = true;
        }

        private void BtnHome_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.ContentFrame.Navigate(typeof(Home));

        }

        private void BtnSearch_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.ContentFrame.Navigate(typeof(Search));

        }

        private void BtnPlaylists_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.ContentFrame.Navigate(typeof(Playlists));

        }

        private void BtnAccount_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.ContentFrame.Navigate(typeof(AccountPage));

        }

        private void BtnJukebox_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.ContentFrame.Navigate(typeof(Jukebox));
        }

        private void BtnPlaying_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.ContentFrame.Navigate(typeof(NowPlaying));
        }



        public void Update(string action, string value = "")
        {
            switch (action)
            {
                case "focusSidebar":
                    btnHome.Focus(Windows.UI.Xaml.FocusState.Keyboard);
                    break;
                case "WSOPEN":
                    Model.Connected = Visibility.Visible;
                    break;
                case "WSCLOSED":
                    Model.Connected = Visibility.Collapsed;
                    break;
                case "LOGOUT":
                    Frame.Navigate(typeof(MainPage));
                    break;
            }
        }
    }
}
