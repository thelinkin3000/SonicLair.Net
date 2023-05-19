using LibVLCSharp.Shared;

using NStack;

using QRCoder;

using SonicLair.Cli;
using SonicLair.Lib.Infrastructure;
using SonicLair.Lib.Services;
using SonicLair.Lib.Types.SonicLair;

using Terminal.Gui;

namespace SonicLairCli
{
    public class MainWindow : IWindowFrame
    {
        private readonly Toplevel _top;
        private ISubsonicService? _subsonicService;
        private IMusicPlayerService? _musicPlayerService;
        private WebSocketService? _messageServer;
        private FrameView? mainView;
        private TextView? _nowPlaying;
        private ProgressBar? _playingTime;
        private ProgressBar? _volumeSlider;
        private TextView? _timeElapsed;
        private TextView? _songDuration;
        private CurrentState? _state;
        private SonicLairListView<Song>? _nowPlayingList;
        private readonly History _history;

        public MainWindow(Toplevel top)
        {
            _top = top;
            _history = new History();
            _history.Push(() =>
            {
                _ = ArtistsView();
            });

        }

        private FrameView GetCurrentPlaylist(View anchorLeft)
        {
            var ret = new FrameView()
            {
                X = Pos.Right(anchorLeft),
                Y = 0,
                Height = anchorLeft.Height,
                Width = 35,
                Title = "Current Playlist",
            };
            if (_nowPlayingList == null)
            {
                _nowPlayingList = new SonicLairListView<Song>()
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(),
                };
                _nowPlayingList.OpenSelectedItem += (ListViewItemEventArgs e) =>
                {
                    var currentState = _musicPlayerService!.GetCurrentState();
                    var index = currentState.CurrentPlaylist.Entry.IndexOf((Song)e.Value);
                    if (index != -1)
                    {
                        _musicPlayerService.SkipTo(index);
                    }
                };
                _nowPlayingList.SetOnLeave((lv) =>
                {
                    if (lv?.Source != null && lv.Source.Count > 0)
                    {
                        var currentState = _musicPlayerService!.GetCurrentState();
                        if (currentState?.CurrentPlaylist != null && currentState.CurrentPlaylist.Entry.Any())
                        {
                            var index = currentState.CurrentPlaylist.Entry.IndexOf(currentState.CurrentTrack);
                            lv.SelectedItem = index;
                        }
                    }
                });
            }

            ret.Add(_nowPlayingList);
            return ret;
        }

        public FrameView GetMainView()
        {
            var ret = new FrameView()
            {
                X = 0,
                Y = 0,
                Height = Dim.Fill() - 7,
                Width = Dim.Fill() - 35
            };
            return ret;
        }

        public TextView GetBaseBar(View controlView)
        {
            var ret = new TextView()
            {
                X = 0,
                Y = Pos.Bottom(controlView),
                Height = 2,
                Width = Dim.Fill(),
                CanFocus = false,
                Text = "C-a Artists | C-l Album | C-p Playlists | C-r Search | C-j Jukebox | C-Right Fw(10s) | C-Left Bw(10s)" +
                "\nC-c Quit | C-h Play/Pause | C-b Prev | C-n Next | C-s Shuffle | C-m Add | C-o Back",
            };
            return ret;
        }

        public FrameView GetAudioControlView(View anchorTop)
        {
            var ret = new FrameView()
            {
                X = 0,
                Y = Pos.Bottom(anchorTop),
                Height = 5,
                Width = Dim.Fill(),
                Title = "Now Playing"
            };
            _nowPlaying = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(60),
                Height = 1,
                CanFocus = false,
            };
            var volumeLabel = new TextView()
            {
                X = Pos.Right(_nowPlaying),
                Y = 0,
                Width = 10,
                Height = 1,
                CanFocus = false,
                Text = "Vol[C-k/i]"
            };
            _volumeSlider = new ProgressBar()
            {
                X = Pos.Right(volumeLabel),
                Y = 0,
                Width = Dim.Fill() - 10,
                Height = 1,
                ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage,
                ProgressBarStyle = ProgressBarStyle.Blocks,
                CanFocus = false,
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.BrightBlue, Color.Black),
                    Focus = Application.Driver.MakeAttribute(Color.White, Color.Black)
                },
                Fraction = 1
            };
            _timeElapsed = new TextView()
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = 1,
                CanFocus = false,
            };
            _playingTime = new ProgressBar()
            {
                X = 0,
                Y = 2,
                Width = Dim.Fill(),
                Height = 1,
                ProgressBarFormat = ProgressBarFormat.Simple,
                ProgressBarStyle = ProgressBarStyle.Blocks,
                CanFocus = false,
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.BrightBlue, Color.Black),
                    Focus = Application.Driver.MakeAttribute(Color.White, Color.Black)
                }
            };
            _songDuration = new TextView()
            {
                X = Pos.Right(_playingTime) - 5,
                Y = 1,
                Width = 5, // MM:SS
                Height = 1,
                CanFocus = false,
            };
            ret.Add(_nowPlaying);
            ret.Add(volumeLabel);
            ret.Add(_volumeSlider);
            ret.Add(_playingTime);
            ret.Add(_timeElapsed);
            ret.Add(_songDuration);
            return ret;
        }

        private void SearchView()
        {
            mainView!.RemoveAll();
            mainView.Title = "Search";
            TextView searchLabel = new TextView()
            {
                X = 0,
                Y = 0,
                Width = 9,
                Height = 1,
                Text = "[Search:]",
                CanFocus = false,
            };
            TextField searchField = SonicLairControls.GetTextField("");
            searchField.X = Pos.Right(searchLabel) + 1;
            searchField.Y = 0;
            searchField.Height = 1;
            searchField.Width = Dim.Fill();

            FrameView artistsContainer = new FrameView()
            {
                X = 0,
                Y = 1,
                Width = Dim.Percent(50),
                Height = Dim.Percent(50),
                Title = "Artists"
            };
            SonicLairListView<Artist> artistsList = new SonicLairListView<Artist>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            artistsList.OpenSelectedItem += (ListViewItemEventArgs e) =>
            {
                var artist = (Artist)e.Value;
                _ = ArtistView(artist.Id);
                _history.Push(() => { _ = ArtistView(artist.Id); });
            };
            artistsContainer.Add(artistsList);

            FrameView albumsContainer = new FrameView()
            {
                X = Pos.Right(artistsContainer),
                Y = 1,
                Width = Dim.Percent(50),
                Height = Dim.Percent(50),
                Title = "Albums",
            };
            SonicLairListView<Album> albumsList = new SonicLairListView<Album>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            albumsList.OpenSelectedItem += (ListViewItemEventArgs e) =>
            {
                var album = (Album)e.Value;
                _musicPlayerService!.PlayAlbum(album.Id, 0);
            };
            albumsContainer.Add(albumsList);

            FrameView songsContainer = new FrameView()
            {
                X = 0,
                Y = Pos.Bottom(artistsContainer),
                Width = Dim.Fill(),
                Height = Dim.Percent(50),
                Title = "Songs"
            };
            SonicLairListView<Song> songsList = new SonicLairListView<Song>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            songsList.OpenSelectedItem += (ListViewItemEventArgs e) =>
            {
                var song = (Song)e.Value;
                _musicPlayerService!.PlayRadio(song.Id);
            };
            songsContainer.Add(songsList);
            searchField.TextChanged += async (ustring value) =>
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return;
                }
                var cancellationTokenSource = new CancellationTokenSource();
                SonicLairControls.AnimateTextView(searchLabel, new[]{
                    "[Search/]",
                    "[Search-]",
                    "[Search\\]",
                    "[Search|]",
                }, 800, cancellationTokenSource.Token);
                var ret = await _subsonicService!.Search(value.ToString(), 100);
                cancellationTokenSource.Cancel();
                searchLabel.Text = "[Search:]";
                Application.MainLoop.Invoke((Action)(() =>
                {
                    if (ret.Artists != null && ret.Artists.Any<Artist>())
                    {
                        artistsList.Source = new SonicLairDataSource<Artist>(ret.Artists, (s) =>
                        {
                            return s.Name;
                        });
                    }
                    if (ret.Albums != null && ret.Albums.Any<Album>())
                    {
                        var max = ret.Albums.Max(s => s.Name.Length);
                        albumsList.Source = new SonicLairDataSource<Album>(ret.Albums, (s) =>
                        {
                            return $"{s.Name.PadRight(max, ' ')} by {s.Artist}";
                        });
                    }
                    if (ret.Songs != null && ret.Songs.Any<Song>())
                    {
                        var max = ret.Songs.Max(s => s.Title.Length);
                        songsList.Source = new SonicLairDataSource<Song>(ret.Songs, (s) =>
                        {
                            return $"{s.Title.PadRight(max, ' ')} by {s.Artist}";
                        });
                    }
                    Application.Refresh();
                }));
            };
            mainView.Add(searchLabel,
            searchField,
            artistsContainer,
            albumsContainer,
            songsContainer);
            searchField.FocusFirst();
        }

        private async Task AlbumsView()
        {
            var albums = await _subsonicService!.GetAllAlbums();
            if (albums == null)
            {
                return;
            }
            mainView!.RemoveAll();
            mainView.Title = "Albums";
            SonicLairListView<Album> listView = new SonicLairListView<Album>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            listView.SetSource(albums);
            listView.OpenSelectedItem += AlbumView_Selected;
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private void JukeboxView()
        {
            var ip = StaticHelpers.GetLocalIp();
            mainView!.RemoveAll();
            mainView.Title = "Jukebox";
            string qr = "";
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode($"{ip}j", QRCodeGenerator.ECCLevel.Q))
            using (AsciiQRCode qrCode = new AsciiQRCode(qrCodeData))
            {
                qr = qrCode.GetGraphic(1, drawQuietZones: false);
            }
            var slices = qr.Split('\n');

            TextView qrView = new TextView()
            {
                X = Pos.Center(),
                Y = Pos.Center(),
                Width = slices[0].Length,
                Height = slices.Length,
                Text = qr,
                CanFocus = false,
            };
            var label = "Scan this QR with your phone and connect to this instance to control it!";
            TextView labelView = new TextView()
            {
                Y = Pos.Top(qrView) - 2,
                X = Pos.Center(),
                Width = label.Length,
                Height = 1,
                Text = label,
                CanFocus = false,
            };

            TextView ipView = new TextView()
            {
                Y = Pos.Bottom(qrView) + 2,
                X = Pos.Center(),
                Width = ip.Length,
                Height = 1,
                Text = ip,
                CanFocus = false,
            };
            mainView.Add(qrView, labelView, ipView);
        }

        private async Task ArtistsView()
        {
            var artists = await _subsonicService!.GetArtists();
            if (artists == null)
            {
                return;
            }
            mainView!.RemoveAll();
            mainView.Title = "Artists";
            SonicLairListView<Artist> listView = new SonicLairListView<Artist>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            var max = artists.Max(s => s.Name.Length);
            var maxAlbums = artists.Max(s => s.AlbumCount.ToString().Length);
            listView.Source = new SonicLairDataSource<Artist>(artists, (a) =>
            {
                var tag = a.AlbumCount > 1 ? "Albums" : "Album";
                return $"{a.ToString().PadRight(max, ' ')} {a.AlbumCount.ToString().PadLeft(maxAlbums, ' ')} {tag}";
            });
            listView.OpenSelectedItem += ArtistsView_Selected;
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private async Task ArtistView(string id)
        {
            var artist = await _subsonicService!.GetArtist(id);
            mainView!.RemoveAll();
            mainView.Title = artist.Name;
            SonicLairListView<Album> listView = new SonicLairListView<Album>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            listView.Source = new SonicLairDataSource<Album>(artist.Album, (a) =>
            {
                return $"({a.Year:0000}) {a.Name}";
            });
            listView.OpenSelectedItem += AlbumView_Selected;
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private async Task AlbumView(string id)
        {
            var album = await _subsonicService!.GetAlbum(id);
            mainView!.RemoveAll();
            mainView.Title = $"{album.Name} by {album.Artist}";
            SonicLairListView<Song> listView = new SonicLairListView<Song>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            var source = new SonicLairDataSource<Song>(album.Song, (s) =>
            {
                return $"{s.Track:00} - {s.Title} [{s.Duration.GetAsMMSS()}]";
            });
            listView.Source = source;
            listView.OpenSelectedItem += (ListViewItemEventArgs e) =>
            {
                var song = (Song)e.Value;
                _musicPlayerService!.PlayAlbum(album.Id, album.Song.IndexOf(song));
            };
            listView.RegisterHotKey(Key.M | Key.CtrlMask, () =>
            {
                _musicPlayerService!.AddToCurrentPlaylist(source.Items[listView.SelectedItem]);
            });
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private async Task PlaylistView(string id)
        {
            var playlist = await _subsonicService!.GetPlaylist(id);
            mainView!.RemoveAll();
            mainView.Title = $"{playlist.Name} by {playlist.Owner} -- Lasts {playlist.Duration.GetAsMMSS()}";
            SonicLairListView<Song> listView = new SonicLairListView<Song>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            var maxTitle = playlist.Entry.Max(s => s.Title.Length);
            var maxArtist = playlist.Entry.Max(s => s.Artist.Length);
            var source = new SonicLairDataSource<Song>(playlist.Entry, (s) =>
            {
                return $"{s.Title.PadRight(maxTitle, ' ')} by {s.Artist.PadRight(maxArtist, ' ')} [{s.Duration.GetAsMMSS()}]";
            });
            listView.Source = source;
            listView.OpenSelectedItem += (ListViewItemEventArgs e) =>
            {
                var song = (Song)e.Value;
                _musicPlayerService!.PlayPlaylist(playlist.Id, playlist.Entry.IndexOf(song));
            };
            listView.RegisterHotKey(Key.M | Key.CtrlMask, () =>
             {
                 _musicPlayerService!.AddToCurrentPlaylist(source.Items[listView.SelectedItem]);
             });
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private async Task PlaylistsView()
        {
            var playlists = await _subsonicService!.GetPlaylists();
            mainView!.RemoveAll();
            mainView.Title = $"Playlists";
            SonicLairListView<Playlist> listView = new SonicLairListView<Playlist>()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            var maxName = playlists.Max(s => s.Name.Length);
            var maxOwner = playlists.Max(s => s.Owner.Length);
            listView.Source = new SonicLairDataSource<Playlist>(playlists, (p) =>
            {
                return $"{p.Name.PadRight(maxName + 1, ' ')} by {p.Owner.PadRight(maxOwner + 1, ' ')} [lasts {p.Duration.GetAsMMSS()}]";
            });
            listView.OpenSelectedItem += (ListViewItemEventArgs e) =>
            {
                _history.Push(() => { _ = PlaylistView(((Playlist)e.Value).Id); });

                _ = PlaylistView(((Playlist)e.Value).Id);
            };
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private void ArtistsView_Selected(ListViewItemEventArgs obj)
        {
            _history.Push(() => { _ = ArtistView(((Artist)obj.Value).Id); });

            _ = ArtistView(((Artist)obj.Value).Id);
        }

        private void AlbumView_Selected(ListViewItemEventArgs obj)
        {
            _history.Push(() => { _ = AlbumView(((Album)obj.Value).Id); });

            _ = AlbumView(((Album)obj.Value).Id);
        }

        private void CurrentStateHandler(object? sender, CurrentStateChangedEventArgs e)
        {
            _state = e.CurrentState;
            Application.MainLoop.Invoke(() =>
            {
                if (e.CurrentState?.CurrentTrack != null)
                {
                    _nowPlaying!.Text = $"{e.CurrentState.CurrentTrack.Title} by {e.CurrentState.CurrentTrack.Artist}";
                    _songDuration!.Text = e.CurrentState.CurrentTrack.Duration.GetAsMMSS();
                }
                if (_nowPlayingList != null && _state?.CurrentPlaylist?.Entry != null && _state.CurrentPlaylist.Entry.Any())
                {
                    _nowPlayingList.Source = new SonicLairDataSource<Song>(_state.CurrentPlaylist.Entry, (s) =>
                    {
                        // Max 35
                        var currentId = _state.CurrentTrack?.Id ?? "-";
                        var max = s.Id == currentId ? 24 : 25;
                        string title;
                        if (s.Title.Length > max)
                        {
                            title = s.Title.Substring(0, max);
                        }
                        else
                        {
                            title = s.Title.PadRight(max, ' ');
                        }
                        return $"{(s.Id == currentId ? "*" : "")}{title}[{s.Duration.GetAsMMSS()}]";
                    });
                    _nowPlayingList.SelectedItem = _state.CurrentPlaylist.Entry.IndexOf(_state.CurrentTrack);
                    _nowPlayingList.ScrollTo(_state.CurrentPlaylist.Entry.IndexOf(_state.CurrentTrack));
                }

                Application.Refresh();
            });
        }

        private void PlayingTimeHandler(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (_state != null && _state.CurrentTrack != null)
            {
                Application.MainLoop.Invoke(() =>
                {
                    _timeElapsed!.Text = ((int)e.Time / 1000).GetAsMMSS();
                    _playingTime!.Fraction = (e.Time / 1000f) / (_state.CurrentTrack.Duration);
                    Application.Refresh();
                });
            }
        }

        private void PlayerVolumeHandler(object? sender, MediaPlayerVolumeChangedEventArgs e)
        {
            if (_volumeSlider != null)
            {
                _volumeSlider.Fraction = e.Volume;
            }
        }

        public void Load()
        {
            var account = Statics.GetActiveAccount();
            if (_subsonicService == null)
            {
                _subsonicService = new SubsonicService();
                _subsonicService.Configure(account);
            }
            if (_musicPlayerService == null)
            {
                _musicPlayerService = new MusicPlayerService(_subsonicService);
                _musicPlayerService.RegisterCurrentStateHandler(CurrentStateHandler);
                _musicPlayerService.RegisterTimeChangedHandler(PlayingTimeHandler);
                _musicPlayerService.RegisterPlayerVolumeHandler(PlayerVolumeHandler);
            }
            if (_messageServer == null)
            {
                try
                {
                    _messageServer = new WebSocketService(_subsonicService, _musicPlayerService, false);
                }
                catch (Exception)
                {
                    MessageBox.Query("Error", "Couldn't start the websockets server. " +
                        "Won't be able to control this instance from outside. " +
                        "Do you have permission to bind port 30001?", "Ok");
                }
            }
            _top.RemoveAll();
            var win = new SonicLairWindow($"SonicLair | {account.Username} on {account.Url}")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            RegisterHotKeys(win);

            mainView = GetMainView();
            win.Add(mainView);
            var nowPlaying = GetAudioControlView(mainView);
            win.Add(nowPlaying);
            var baseBar = GetBaseBar(nowPlaying);
            win.Add(baseBar);
            var currentPlayingList = GetCurrentPlaylist(mainView);
            win.Add(currentPlayingList);
            _ = ArtistsView();
            _top.Add(win);
        }

        public void RegisterHotKeys(SonicLairWindow window)
        {
            window.RegisterHotKey(Key.C | Key.CtrlMask, () =>
            {
                Application.RequestStop();
            });
            window.RegisterHotKey(Key.N | Key.CtrlMask, () =>
            {
                _musicPlayerService!.Next();
            });
            window.RegisterHotKey(Key.B | Key.CtrlMask, () =>
            {
                _musicPlayerService!.Prev();
            });
            window.RegisterHotKey(Key.H | Key.CtrlMask, () =>
            {
                _musicPlayerService!.PlayPause();
            });
            window.RegisterHotKey(Key.A | Key.CtrlMask, () =>
            {
                _history.Push(() => { _ = ArtistsView(); });
                _ = ArtistsView();
            });
            window.RegisterHotKey(Key.L | Key.CtrlMask, () =>
            {
                _history.Push(() => { _ = AlbumsView(); });
                _ = AlbumsView();
            });
            window.RegisterHotKey(Key.P | Key.CtrlMask, () =>
            {
                _history.Push(() => { _ = PlaylistsView(); });
                _ = PlaylistsView();
            });
            window.RegisterHotKey(Key.R | Key.CtrlMask, () =>
            {
                _history.Push(() => { SearchView(); });
                SearchView();
            });
            window.RegisterHotKey(Key.J | Key.CtrlMask, () =>
            {
                _history.Push(() => { JukeboxView(); });
                JukeboxView();
            });
            window.RegisterHotKey(Key.S | Key.CtrlMask, () =>
            {
                _musicPlayerService!.Shuffle();
            });
            window.RegisterHotKey(Key.I | Key.CtrlMask, () =>
            {
                _musicPlayerService!.SetVolume(5, true);
            });
            window.RegisterHotKey(Key.K | Key.CtrlMask, () =>
            {
                _musicPlayerService!.SetVolume(-5, true);
            });
            window.RegisterHotKey(Key.O | Key.CtrlMask, () =>
            {
                _history.GoBack();
            });
            window.RegisterHotKey(Key.CursorRight | Key.CtrlMask, () =>
            {
                if (_musicPlayerService.GetCurrentState().IsPlaying)
                {
                    var newPosition = 10f / _musicPlayerService!.GetCurrentState().CurrentTrack.Duration;
                    _musicPlayerService.Seek(newPosition, true);
                }
            });
            window.RegisterHotKey(Key.CursorLeft | Key.CtrlMask, () =>
            {
                if (_musicPlayerService.GetCurrentState().IsPlaying)
                {
                    var newPosition = 10f / _musicPlayerService!.GetCurrentState().CurrentTrack.Duration;
                    _musicPlayerService.Seek(-newPosition, true);
                    
                }
            });
        }
    }
}