using LibVLCSharp.Shared;

using SonicLair.Services;
using SonicLair.Types.SonicLair;

using SonicLairXbox.Infrastructure;
using SonicLairXbox.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

// La plantilla de elemento Página en blanco está documentada en https://go.microsoft.com/fwlink/?LinkId=234238

namespace SonicLairXbox
{
    /// <summary>
    /// Una página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>


    public sealed partial class NowPlaying : Page, INotificationObserver
    {
        public NowPlayingModel Model { get; set; }
        private readonly IMusicPlayerService _player;
        private readonly ISubsonicService _client;
        private readonly List<Button> _buttons;
        

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Doesn't make sense to put it elsewhere as it's a local asset")]
        public NowPlaying()
        {
            this.InitializeComponent();
            _buttons = new List<Button>()
            {
                btn_skipBack,
                btn_prev,
                btn_play,
                btn_next,
                btn_skipForw
            };
            _player = (IMusicPlayerService)((App)App.Current).Container.GetService(typeof(IMusicPlayerService));
            _client = (ISubsonicService)((App)App.Current).Container.GetService(typeof(ISubsonicService));
            var state = _player.GetCurrentState();
            Model = new NowPlayingModel()
            {
                CurrentTrack = state.CurrentTrack ?? Song.GetDefault(),
                IsPlaying = state.IsPlaying,
                Position = state.Position,
                Image = _client.GetCoverArtUri(state.CurrentTrack?.AlbumId ?? "")
            };
            btn_play.Content = new Image
            {
                Source = new SvgImageSource(new Uri(state.IsPlaying ? "ms-appx:///Assets/Icons/pause.svg" : "ms-appx:///Assets/Icons/play.svg", UriKind.RelativeOrAbsolute)),
                VerticalAlignment = VerticalAlignment.Center,
                Stretch = Windows.UI.Xaml.Media.Stretch.Fill,
                Margin = new Thickness(10, 0, 10, 0),
            };
            if (state.CurrentPlaylist.Entry.Count > 0)
            {
                Model.ClearPlaylist();
                Model.AddToPlaylist(state.CurrentPlaylist.Entry);
                lv_playlist.SelectedItem = state.CurrentTrack;
            }
            btn_next.Click += Btn_next_Click;
            btn_prev.Click += Btn_prev_Click;
            btn_play.Click += Btn_play_Click;
            btn_skipForw.Click += Btn_skipForw_Click;
            btn_skipBack.Click += Btn_skipBack_Click;
            _player.RegisterCurrentStateHandler(UpdateCurrentState);
            _player.RegisterTimeChangedHandler(UpdateCurrentTime);
            foreach (var button in _buttons)
            {
                button.KeyDown += Button_KeyDown;
            }
            btn_play.Focus(FocusState.Keyboard);
            ((App)App.Current).RegisterObserver(this);
            gr_imageContainer.Loaded += Gr_imageContainer_Loaded;
            lv_playlist.SelectionChanged += Lv_playlist_SelectionChanged;
            Window.Current.SizeChanged += Current_SizeChanged;
        }

        private void Lv_playlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1 || e.RemovedItems.Count != 1 || lv_playlist.Items == null || lv_playlist.Items.Count == 0)
            {
                // I'm not cut out for this, kid
                return;
            }

           
            var verticalOffset = lv_playlist.Items.IndexOf(e.AddedItems[0]) * lv_playlist.GetScrollViewer().ExtentHeight / lv_playlist.Items.Count;
            var actualVerticalOffset = verticalOffset + 45 - lv_playlist.GetScrollViewer().ActualHeight / 2;
            lv_playlist.GetScrollViewer().ChangeView(0, actualVerticalOffset, null);
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            gr_imageContainer.Height = Math.Clamp(gr_container.ActualHeight, 0, 10000);
            rg_image.Height = Math.Clamp(gr_imageContainer.ActualHeight - 120, 0, 10000);
            rg_image.Width = Math.Clamp(gr_imageContainer.ActualHeight - 120, 0, 10000);
        }

        private void Gr_imageContainer_Loaded(object sender, RoutedEventArgs e)
        {
            rg_image.Height = Math.Clamp(gr_imageContainer.ActualHeight - 120, 0, 10000);
            rg_image.Width = Math.Clamp(gr_imageContainer.ActualHeight - 120, 0, 10000);
        }

        private void Btn_skipBack_Click(object sender, RoutedEventArgs e)
        {
            var newPosition = 10f / Model.CurrentTrack.Duration;
            _player.Seek(-newPosition, true);
        }

        private void Btn_skipForw_Click(object sender, RoutedEventArgs e)
        {
            var newPosition = 10f / Model.CurrentTrack.Duration;
            _player.Seek(newPosition, true);
        }

        private void Btn_play_Click(object sender, RoutedEventArgs e)
        {
            if (Model.IsPlaying)
            {
                _player.Pause();
            }
            else
            {
                _player.Play();
            }
        }

        private void Button_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var index = _buttons.IndexOf((Button)sender);
            if (e.Key == Windows.System.VirtualKey.Left && index == 0)
            {
                ((App)App.Current).NotifyObservers("focusSidebar");
            }
            if (e.Key == Windows.System.VirtualKey.Right && index < _buttons.Count - 1)
            {
                _buttons[index + 1].Focus(Windows.UI.Xaml.FocusState.Keyboard);
            }
            else if (e.Key == Windows.System.VirtualKey.Left && index > 0)
            {
                _buttons[index - 1].Focus(Windows.UI.Xaml.FocusState.Keyboard);

            }
            e.Handled = true;
        }



        private void Btn_prev_Click(object sender, RoutedEventArgs e)
        {
            _player.Prev();
        }

        private void Btn_next_Click(object sender, RoutedEventArgs e)
        {
            _player.Next();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Doesn't make sense to put it elsewhere as it's a local asset")]
        public void UpdateCurrentState(object sender, CurrentStateChangedEventArgs e)
        {
            var currentState = e.CurrentState;
            var imageUri = _client.GetCoverArtUri(e.CurrentState.CurrentTrack.AlbumId);
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                Model.CurrentTrack = currentState.CurrentTrack;
                Model.IsPlaying = currentState.IsPlaying;
                Model.Position = currentState.Position;
                Model.Image = imageUri;
                if (!Model.CurrentPlaylist.All(s => currentState.CurrentPlaylist.Entry.Contains(s)))
                {
                    Model.ClearPlaylist();
                    Model.AddToPlaylist(currentState.CurrentPlaylist.Entry);
                }
                lv_playlist.SelectedItem = currentState.CurrentTrack;

                btn_play.Content = new Image
                {
                    Source = new SvgImageSource(new Uri(Model.IsPlaying ? "ms-appx:///Assets/Icons/pause.svg" : "ms-appx:///Assets/Icons/play.svg", UriKind.RelativeOrAbsolute)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Stretch = Windows.UI.Xaml.Media.Stretch.Fill,
                    Margin = new Thickness(10, 0, 10, 0),
                };

            }
            );
        }

        

        public void UpdateCurrentTime(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                var pos = (e.Time * 100) / (Model.CurrentTrack.Duration * 1000);
                sld_time.Value = pos;
                Model.Position = pos;
            }
            );
        }

        public void Update(string action, string value = "")
        {
            if (action == "focusContent")
            {
                btn_skipBack.Focus(FocusState.Keyboard);
            }
        }
    }

    public class CurrentStateChangedEventArgs
    {
        public CurrentState CurrentState { get; set; }
    }

    public class NowPlayingModel : INotifyPropertyChanged
    {
        private Song _currentTrack;
        private decimal _position;
        private string _image;
        private bool _isPlaying;
        public ObservableCollection<Song> CurrentPlaylist { get; set; }

        public NowPlayingModel()
        {
            IsPlaying = false;
            Position = 0;
            Image = "";
            CurrentPlaylist = new ObservableCollection<Song>();
        }

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ClearPlaylist()
        {
            CurrentPlaylist.Clear();
        }

        public void AddToPlaylist(List<Song> songs)
        {
            foreach (var song in songs)
            {
                CurrentPlaylist.Add(song);
            }
        }

        public Song CurrentTrack
        {
            get
            {
                return this._currentTrack;
            }
            set

            {
                if (value != this._currentTrack)
                {
                    this._currentTrack = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("CurrentTrackDuration");
                }
            }
        }

        public String CurrentTrackDuration
        {
            get
            {
                return this._currentTrack.GetDuration;
            }
        }
        public bool IsPlaying
        {
            get
            {
                return this._isPlaying;
            }
            set

            {
                if (value != this._isPlaying)
                {
                    this._isPlaying = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public decimal Position
        {
            get
            {
                return this._position;
            }
            set

            {
                if (value != this._position)
                {
                    this._position = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("PositionString");
                }
            }
        }

        public String PositionString
        {
            get
            {
                var c = (int)Math.Floor(this._position * (this.CurrentTrack?.Duration ?? 0) / 100);
                return c.GetAsMMSS();
            }
        }

        public string Image
        {
            get
            {
                return this._image;
            }
            set

            {
                if (value != this._image)
                {
                    this._image = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
