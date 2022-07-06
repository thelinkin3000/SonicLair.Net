using SonicLair.Services;
using SonicLair.Types.SonicLair;

using SonicLairXbox.Infrastructure;
using SonicLairXbox.Services;
using SonicLairXbox.Types.SonicLair;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
    /// 
    public class PlaylistsModel : INotifyPropertyChanged
    {
        public PlaylistsModel()
        {
            Playlists = new ObservableCollection<Playlist>();
            _playlist = new Playlist("", "", "", "", false, 0, 0, "", "", new List<Song>());
            _image = "ms-appx:///Assets/coverart.png";
        }
        public ObservableCollection<Playlist> Playlists { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private Playlist _playlist;
        public Playlist Playlist
        {
            get
            {
                return _playlist;
            }
            set
            {
                if (value != _playlist)
                {
                    _playlist = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Playlist"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaylistVisible"));
                }
            }
        }
        public Visibility PlaylistVisible
        {
            get => (_playlist != null && _playlist.Entry.Any()) ? Visibility.Visible : Visibility.Collapsed;
        }

        private string _image;

        public string Image
        {
            get
            {
                return _image;
            }
            set
            {
                if (value != _image)
                {
                    _image = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Image"));
                }
            }
        }

    }
    public sealed partial class Playlists : Page, INotificationObserver
    {
        public PlaylistsModel Model { get; set; }
        public ISubsonicService Client { get; set; }
        public IMusicPlayerService Player { get; set; }
        public Playlists()
        {
            this.InitializeComponent();
            Model = new PlaylistsModel();
            Client = (ISubsonicService)((App)App.Current).Container.GetService(typeof(ISubsonicService));
            Player = (IMusicPlayerService)((App)App.Current).Container.GetService(typeof(IMusicPlayerService));
            _ = Load();
            gr_playlists.ItemClick += Gr_playlists_ItemClick;
            gr_playlists.Loaded += Gr_playlists_Loaded;
            gr_playlists.KeyDown += Gr_playlists_KeyDown;
            Player.RegisterCurrentStateHandler(UpdateCurrentState);
            im_disc.Loaded += Im_disc_Loaded;
            ((App)App.Current).RegisterObserver(this);

        }

        private void Im_disc_Loaded(object sender, RoutedEventArgs e)
        {
            spinrect.Begin();
        }

        public void UpdateCurrentState(object sender, CurrentStateChangedEventArgs e)
        {
            var imageUri = Client.GetCoverArtUri(e.CurrentState.CurrentPlaylist.CoverArt);
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                Model.Playlist = e.CurrentState.CurrentPlaylist;
                Model.Image = imageUri;
            }
            );
        }

        private void Gr_playlists_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (gr_playlists.SelectedIndex == 0
                && (e.Key == Windows.System.VirtualKey.Left
                    || e.Key == Windows.System.VirtualKey.Up))
            {
                gr_playlists.SelectedIndex = -1;
                ((App)App.Current).NotifyObservers("focusSidebar");
            }
        }

        private void Gr_playlists_Loaded(object sender, RoutedEventArgs e)
        {
            gr_playlists.Focus(FocusState.Keyboard);
            gr_playlists.SelectedIndex = 0;
        }

        private async void Gr_playlists_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedItem = (Playlist)(e.ClickedItem);
            await Player.PlayPlaylist(selectedItem.Id, 0);
            Frame.Navigate(typeof(NowPlaying));
        }

        private async Task Load()
        {
            List<Playlist> playlists;
            try
            {
                playlists = await Client.GetPlaylists();
            }
            catch (SubsonicException ex)
            {
                StaticHelpers.ShowError(ex.Message);
                return;
            }
            foreach (var playlist in playlists)
            {
                playlist.Image = Client.GetCoverArtUri(playlist.CoverArt);
                Model.Playlists.Add(playlist);
            }
            var currentState = Player.GetCurrentState();
            Model.Playlist = currentState.CurrentPlaylist;
            Model.Image = Client.GetCoverArtUri(currentState.CurrentPlaylist.CoverArt);
        }

        public void Update(string action, string value = "")
        {
            if (action == "focusContent")
            {
                gr_playlists.SelectedIndex = 0;
                gr_playlists.Focus(FocusState.Keyboard);
            }
        }
    }
}
