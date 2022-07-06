using SonicLair.Services;
using SonicLair.Types.SonicLair;

using SonicLairXbox.Infrastructure;
using SonicLairXbox.Services;
using SonicLairXbox.Types.SonicLair;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

using static SonicLairXbox.Home;

// La plantilla de elemento Página en blanco está documentada en https://go.microsoft.com/fwlink/?LinkId=234238

namespace SonicLairXbox
{
    /// <summary>
    /// Una página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class Search : Page, INotificationObserver
    {
        public readonly SearchModel _model;
        private readonly List<CancellationTokenSource> _cancellationTokenSourceList;
        private readonly ISubsonicService _client;
        private readonly IMusicPlayerService _player;
        private string searchText;
        public ListViewBase LastSelected { get; set; }
        public List<ListViewBase> Listviews { get; set; }
        private List<Section> _sections;
        private List<int> _sizes;
        private int _maxSize;

        public Search()
        {
            this.InitializeComponent();
            _model = new SearchModel(new ObservableCollection<Album>(), new ObservableCollection<Song>());
            tb_search.TextChanged += Tb_search_TextChanged;
            _cancellationTokenSourceList = new List<CancellationTokenSource>();
            _client = (ISubsonicService)((App)App.Current).Container.GetService(typeof(ISubsonicService));
            _player = (IMusicPlayerService)((App)App.Current).Container.GetService(typeof(IMusicPlayerService));
            gr_sections.ItemClick += HandleItemClick;
            gr_sections.Loaded += Gr_sections_Loaded;
            gr_sections.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
            gr_sections.KeyDown += Gr_sections_KeyDown;
            gr_sections.SelectionChanged += Gr_sections_SelectionChanged;
            tb_search.KeyDown += Tb_search_KeyDown;
            tb_search.Loaded += Tb_search_Loaded;
            ((App)App.Current).RegisterObserver(this);
        }

        private void Tb_search_Loaded(object sender, RoutedEventArgs e)
        {

            tb_search.Focus(FocusState.Keyboard);
        }

        private void Gr_sections_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Left)
            {
                gr_sections.SelectedIndex = -1;
                ((App)App.Current).NotifyObservers("focusSidebar");
            }
            else if (e.Key == Windows.System.VirtualKey.Up)
            {
                tb_search.Focus(FocusState.Keyboard);
            }

            Debug.WriteLine(e.Key.ToString());
        }

        private void Tb_search_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Down && _sections != null && _sections.Count > 0)
            {

                gr_sections.Focus(FocusState.Keyboard);
                gr_sections.SelectedIndex = 0;

            }
            if (e.Key == Windows.System.VirtualKey.Left && tb_search.SelectionStart == 0)
            {
                gr_sections.SelectedIndex = -1;
                ((App)App.Current).NotifyObservers("focusSidebar");
            }
        }
        private void Gr_sections_Loaded(object sender, RoutedEventArgs e)
        {
            gr_sections.Focus(FocusState.Keyboard);
            gr_sections.SelectedIndex = 0;
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
            var horizontalStartingPoint = rowcoladded.Item2 * gr_sections.GetScrollViewer().ExtentWidth / _maxSize;
            Debug.WriteLine(horizontalStartingPoint);
            var horizontalOffset = Math.Clamp(horizontalStartingPoint - (gr_sections.GetScrollViewer().ViewportWidth / 2) + 150, 0, gr_sections.GetScrollViewer().ExtentWidth - gr_sections.GetScrollViewer().ViewportWidth);
            Debug.WriteLine(horizontalOffset);
            gr_sections.GetScrollViewer().ChangeView(horizontalOffset, verticalOffset, null);


        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            tb_search.Focus(FocusState.Keyboard);
        }

        private async void HandleItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem;
            if (item is Album album)
            {
                await _player.PlayAlbum(album.Id);
                Frame.Navigate(typeof(NowPlaying));
            }
            if (item is Song song)
            {
                await _player.PlayRadio(song.Id);
                Frame.Navigate(typeof(NowPlaying));
            }
        }

        private void Tb_search_TextChanged(object sender, TextChangedEventArgs e)
        {
            _cancellationTokenSourceList.ForEach(s => s.Cancel());
            _cancellationTokenSourceList.Clear();
            Debug.WriteLine("Cancelling...");
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            searchText = tb_search.Text;
            if (string.IsNullOrEmpty(searchText))
                return;
            _ = Task.Run(async () =>
            {
                Debug.WriteLine("Sleeping...");
                Thread.Sleep(500);
                if (!token.IsCancellationRequested)
                {
                    SearchResult result;
                    try
                    {
                        result = await _client.Search(searchText);
                    }
                    catch (SubsonicException ex)
                    {
                        StaticHelpers.ShowError(ex.Message);
                        return;
                    }
                    if (!token.IsCancellationRequested)
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            () =>
                            {
                                var list = new List<Section>();
                                if (result.Albums != null && result.Albums.Count > 0)
                                {
                                    foreach (var album in result.Albums)
                                    {
                                        album.Image = _client.GetCoverArtUri(album.Id);
                                    }
                                    list.Add(new Section("Albums", new ObservableCollection<ISelectableCardItem>(result.Albums)));
                                }
                                _model.Songs.Clear();
                                if (result.Songs != null && result.Songs.Count > 0)
                                {
                                    foreach (var song in result.Songs)
                                    {
                                        song.Image = _client.GetCoverArtUri(song.AlbumId);
                                    }
                                    list.Add(new Section("Songs", new ObservableCollection<ISelectableCardItem>(result.Songs)));
                                }
                                sections.Source = list;
                                _sections = list;
                                if (list.Any())
                                {
                                    _maxSize = list.Max(s => s.Items.Count);
                                    _sizes = list.Select(s => s.Items.Count).ToList();
                                    for (int i = 0; i < _sizes.Count - 1; i++)
                                    {
                                        _sizes[i + 1] = _sizes[i] + _sizes[i + 1];
                                    }

                                }
                            }
                            );

                    }
                }
                else
                {
                    Debug.WriteLine("Cancelled");
                }
            }, token);
            _cancellationTokenSourceList.Add(tokenSource);
        }

        public void Update(string action, string value = "")
        {
            if (action.Equals("focusContent"))
            {
                if (_sections != null && _sections.Count > 0)
                {
                    gr_sections.Focus(FocusState.Keyboard);
                    gr_sections.SelectedIndex = 0;
                }
                else
                {
                    tb_search.Focus(FocusState.Keyboard);
                }
            }
        }
    }

    public class SearchModel : INotifyPropertyChanged
    {
        public SearchModel(ObservableCollection<Album> albums, ObservableCollection<Song> songs)
        {
            Albums = albums;
            Songs = songs;
            albums.CollectionChanged += (sender, e) => { NotifyPropertyChanged("AnyAlbums"); };
            songs.CollectionChanged += (sender, e) => { NotifyPropertyChanged("AnySongs"); };
        }
        public ObservableCollection<Album> Albums { get; set; }
        public ObservableCollection<Song> Songs { get; set; }
        public Visibility AnyAlbums
        {
            get
            {
                return Albums.Any() ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public Visibility AnySongs
        {
            get
            {
                return Songs.Any() ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
