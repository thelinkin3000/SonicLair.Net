using SonicLair.Lib.Services;
using SonicLair.Lib.Types;
using SonicLair.Lib.Types.SonicLair;

using SonicLairXbox.Infrastructure;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

namespace SonicLairXbox
{
    /// <summary>
    /// Una página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class Home : Page, INotificationObserver
    {

        private readonly ISubsonicService _client;
        private readonly IMusicPlayerService _player;
        private List<int> _sizes;
        public HomeViewModel Model { get; set; }
        public ListViewBase LastSelected { get; set; }
        public List<ListViewBase> Listviews { get; set; }
        private List<Section> _sections;
        public Home()
        {
            this.InitializeComponent();
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SetDesiredBoundsMode(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseCoreWindow);

            _client = (ISubsonicService)((App)App.Current).Container.GetService(typeof(ISubsonicService));
            _player = (IMusicPlayerService)((App)App.Current).Container.GetService(typeof(IMusicPlayerService));
            Model = new HomeViewModel();
            _ = Load();
            ((App)App.Current).RegisterObserver(this);
        }

        private async Task Load()
        {
            var list = new List<Section>();

            List<Album> mostPlayed;
            try
            {
                mostPlayed = await _client.GetAlbums();
            }
            catch (SubsonicException ex)
            {
                StaticHelpers.ShowError(ex.Message);
                return;
            }
            if (mostPlayed != null && mostPlayed.Count > 0)
            {
                foreach (var album in mostPlayed)
                {
                    album.Image = _client.GetCoverArtUri(album.Id);
                }
                list.Add(new Section("Most Played Albums", new ObservableCollection<ISelectableCardItem>(mostPlayed)));
            }

            List<Song> randomSongs;
            try
            {
                randomSongs = await _client.GetRandomSongs();
            }
            catch (SubsonicException ex)
            {
                StaticHelpers.ShowError(ex.Message);
                return;
            }
            if (randomSongs != null && randomSongs.Count > 0)
            {
                foreach (var song in randomSongs)
                {
                    song.Image = _client.GetCoverArtUri(song.AlbumId);
                }
                list.Add(new Section("Random Songs", new ObservableCollection<ISelectableCardItem>(randomSongs)));
            }

            List<Album> recentlyPlayed;
            try
            {
                recentlyPlayed = await _client.GetAlbums("recent");
            }
            catch (SubsonicException ex)
            {
                StaticHelpers.ShowError(ex.Message);
                return;
            }
            if (recentlyPlayed != null && recentlyPlayed.Count > 0)
            {
                foreach (var album in recentlyPlayed)
                {
                    album.Image = _client.GetCoverArtUri(album.Id);
                }
                list.Add(new Section("Recently Played", new ObservableCollection<ISelectableCardItem>(recentlyPlayed)));
            }

            List<Album> recentlyAdded;
            try
            {

                recentlyAdded = await _client.GetAlbums("newest");
            }
            catch (SubsonicException ex)
            {
                StaticHelpers.ShowError(ex.Message);
                return;
            }
            if (recentlyAdded != null && recentlyAdded.Count > 0)
            {
                foreach (var album in recentlyAdded)
                {
                    album.Image = _client.GetCoverArtUri(album.Id);
                }
                list.Add(new Section("Recently Added", new ObservableCollection<ISelectableCardItem>(recentlyAdded)));
            }

            sections.Source = list;
            _sections = list;
            _sizes = list.Select(s => s.Items.Count).ToList();
            for (int i = 0; i < _sizes.Count - 1; i++)
            {
                _sizes[i + 1] = _sizes[i] + _sizes[i + 1];
            }
            gr_sections.ItemClick += HandleItemClick;
            gr_sections.Loaded += Gr_sections_Loaded;
            gr_sections.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
            gr_sections.KeyDown += Gr_sections_KeyDown;
            gr_sections.SelectionChanged += Gr_sections_SelectionChanged;

        }



        private void Gr_sections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1 || e.RemovedItems.Count != 1)
            {
                // I'm not cut out for this, kid
                return;
            }

            var rowcoladded = StaticHelpers.GetRowCol(gr_sections.Items.IndexOf(e.AddedItems[0]), _sizes);
            var rowcolremoved = StaticHelpers.GetRowCol(gr_sections.Items.IndexOf(e.RemovedItems[0]), _sizes);
            if (rowcoladded.Item1 != rowcolremoved.Item1 && rowcolremoved.Item2 == 0 && rowcoladded.Item2 != 0)
            {

                // Go to sidebar
                gr_sections.SelectedIndex = -1;
                gr_sections.GetScrollViewer().ChangeView(0, null, null);
                ((App)App.Current).NotifyObservers("focusSidebar");
                return;

            }
            var verticalOffset = rowcoladded.Item1 * gr_sections.GetScrollViewer().ExtentHeight / _sizes.Count;
            var horizontalStartingPoint = rowcoladded.Item2 * gr_sections.GetScrollViewer().ExtentWidth / _sections[rowcoladded.Item1].Items.Count;
            Debug.WriteLine(horizontalStartingPoint);
            var horizontalOffset = Math.Clamp(horizontalStartingPoint - (gr_sections.GetScrollViewer().ViewportWidth / 2) + 150, 0, gr_sections.GetScrollViewer().ExtentWidth - gr_sections.GetScrollViewer().ViewportWidth);
            Debug.WriteLine(horizontalOffset);
            gr_sections.GetScrollViewer().ChangeView(horizontalOffset, verticalOffset, null);


        }

        private void Gr_sections_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Left)
            {
                gr_sections.SelectedIndex = -1;
                ((App)App.Current).NotifyObservers("focusSidebar");
            }
            Debug.WriteLine(e.Key.ToString());
        }

        private void Gr_sections_Loaded(object sender, RoutedEventArgs e)
        {
            gr_sections.Focus(FocusState.Keyboard);
            gr_sections.SelectedIndex = 0;
        }

        private async void HandleItemClick(object sender, ItemClickEventArgs e)
        {

            var item = e.ClickedItem;
            if (item is Album album)
            {
                await _player.PlayAlbum(album.Id);
                Frame.Navigate(typeof(NowPlaying));

            }
            else if (item is Song song)
            {
                await _player.PlayRadio(song.Id);
                Frame.Navigate(typeof(NowPlaying));
            }
            Debug.WriteLine(item);
        }

        public void Update(string action, string value = "")
        {
            if (action == "focusContent")
            {
                gr_sections.SelectedIndex = 0;
                gr_sections.Focus(FocusState.Keyboard);
            }
        }

        public class HomeViewModel
        {
            public HomeViewModel()
            {
                Sections = new CollectionViewSource();
            }

            public CollectionViewSource Sections { get; set; }
        }

        public class Section
        {
            public Section(string name, ObservableCollection<ISelectableCardItem> items)
            {
                Name = name;
                Items = items;
            }

            public string Name { get; set; }
            public ObservableCollection<ISelectableCardItem> Items { get; set; }
        }

    }
}
