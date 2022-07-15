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
        private ISubsonicService _subsonicService;
        private IMusicPlayerService _musicPlayerService;
        private WebSocketService _messageServer;
        private FrameView mainView;
        private TextView _nowPlaying;
        private ProgressBar _playingTime;
        private TextView _timeElapsed;
        private TextView _songDuration;
        private CurrentState _state;
        private SonicLairListView<Song> _nowPlayingList;

        public MainWindow(Toplevel top)
        {
            _top = top;
        }

        private FrameView GetSidebar(View container)
        {
            var ret = new FrameView()
            {
                X = 0,
                Y = 0,
                Height = container.Height - 6,
                Width = 20
            };
            var artistsBtn = new Button()
            {
                X = 0,
                Y = 0,
                Width = ret.Width,
                Text = "_Artists"
            };
            artistsBtn.Clicked += () =>
            {
                ArtistsView();
            };
            ret.Add(artistsBtn);
            var albumBtn = new Button()
            {
                X = 0,
                Y = Pos.Top(artistsBtn) + 1,
                Width = ret.Width,
                Text = "A_lbum"
            };
            albumBtn.Clicked += () =>
            {
                AlbumsView();
            };
            ret.Add(albumBtn);
            var playlistsBtn = new Button()
            {
                X = 0,
                Y = Pos.Top(albumBtn) + 1,
                Width = ret.Width,
                Text = "_Playlists"
            };
            playlistsBtn.Clicked += () =>
            {
                PlaylistsView();
            };
            ret.Add(playlistsBtn);
            var searchBtn = new Button()
            {
                X = 0,
                Y = Pos.Top(playlistsBtn) + 1,
                Width = ret.Width,
                Text = "Sea_rch"
            };
            searchBtn.Clicked += () =>
            {
                SearchView();
            };
            ret.Add(searchBtn);
            var jukeboxBtn = new Button()
            {
                X = 0,
                Y = Pos.Top(searchBtn) + 1,
                Width = ret.Width,
                Text = "_Jukebox"
            };
            jukeboxBtn.Clicked += () =>
            {
                JukeboxView();
            };
            ret.Add(jukeboxBtn);
            var quitBtn = new Button()
            {
                X = 0,
                Y = Pos.Bottom(ret) - 3,
                Width = ret.Width,
                Text = "Quit"
            };
            quitBtn.Clicked += () =>
            {
                Application.RequestStop();
            };
            ret.Add(quitBtn);
            return ret;
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
            }
            _nowPlayingList.OpenSelectedItem += (ListViewItemEventArgs e) => {
                var currentState = _musicPlayerService.GetCurrentState();
                var index = currentState.CurrentPlaylist.Entry.IndexOf((Song)e.Value);
                if (index != -1)
                {
                    _musicPlayerService.SkipTo(index);
                }
            };
            ret.Add(_nowPlayingList);
            return ret;
        }

        public FrameView GetMainView(View sidebar)
        {
            var ret = new FrameView()
            {
                X = Pos.Right(sidebar) + 1,
                Y = 0,
                Height = Dim.Fill() - 6,
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
                Height = 1,
                Width = Dim.Fill(),
                CanFocus = false,
                Text = "C-c Quit | C-h Play/Pause | C-b Prev | C-n Next | C-s Shuffle | C-m Add",
            };
            return ret;
        }

        public FrameView GetAudioControlView(View sidebar)
        {
            var ret = new FrameView()
            {
                X = 0,
                Y = Pos.Bottom(sidebar),
                Height = 5,
                Width = Dim.Fill(),
                Title = "Now Playing"
            };
            _nowPlaying = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 1,
                CanFocus = false,
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
            ret.Add(_playingTime);
            ret.Add(_timeElapsed);
            ret.Add(_songDuration);
            return ret;
        }

        private async void SearchView()
        {
            mainView.RemoveAll();
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
                Width = Dim.Percent(33),
                Height = Dim.Fill(),
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
                ArtistView(artist.Id);
            };
            artistsContainer.Add(artistsList);

            FrameView albumsContainer = new FrameView()
            {
                X = Pos.Right(artistsContainer),
                Y = 1,
                Width = Dim.Percent(33),
                Height = Dim.Fill(),
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
                _musicPlayerService.PlayAlbum(album.Id, 0);
            };
            albumsContainer.Add(albumsList);

            FrameView songsContainer = new FrameView()
            {
                X = Pos.Right(albumsContainer),
                Y = 1,
                Width = Dim.Percent(33),
                Height = Dim.Fill(),
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
                _musicPlayerService.PlayRadio(song.Id);
            };
            songsContainer.Add(songsList);
            searchField.TextChanged += async (ustring value) =>
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return;
                }
                var ret = await _subsonicService.Search(value.ToString(), 100);
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
                            return $"{s.Name.PadRight(max,' ')} by {s.Artist}";
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

        private async void AlbumsView()
        {
            var albums = await _subsonicService.GetAllAlbums();
            if (albums == null)
            {
                return;
            }
            mainView.RemoveAll();
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

        private async void JukeboxView()
        {
            var ip = StaticHelpers.GetLocalIp();
            mainView.RemoveAll();
            mainView.Title = "Jukebox";
            string qr = "";
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode($"{ip}j", QRCodeGenerator.ECCLevel.Q))
            using (AsciiQRCode qrCode = new AsciiQRCode(qrCodeData))
            {
                qr = qrCode.GetGraphic(1);
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
                Y = Pos.Top(qrView),
                X = Pos.Center(),
                Width = label.Length,
                Height = 1,
                Text = label,
                CanFocus = false,
            };

            TextView ipView = new TextView()
            {
                Y = Pos.Bottom(qrView),
                X = Pos.Center(),
                Width = ip.Length,
                Height = 1,
                Text = ip,
                CanFocus = false,
            };
            mainView.Add(qrView, labelView, ipView);
        }

        private async void ArtistsView()
        {
            var artists = await _subsonicService.GetArtists();
            if (artists == null)
            {
                return;
            }
            mainView.RemoveAll();
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
                return $"{a.ToString().PadRight(max, ' ')} {a.AlbumCount.ToString().PadLeft(maxAlbums,' ')} Albums";
            });
            listView.OpenSelectedItem += ArtistsView_Selected;
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private async void ArtistView(string id)
        {
            var artist = await _subsonicService.GetArtist(id);
            mainView.RemoveAll();
            mainView.Title = artist.Name;
            SonicLairListView<Artist> listView = new SonicLairListView<Artist>()
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

        private async void AlbumView(string id)
        {
            var album = await _subsonicService.GetAlbum(id);
            mainView.RemoveAll();
            mainView.Title = $"{album.Name} by {album.Artist}";
            SonicLairListView<Album> listView = new SonicLairListView<Album>()
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
                _musicPlayerService.PlayAlbum(album.Id, album.Song.IndexOf(song));
            };
            listView.RegisterHotKey(Key.M | Key.CtrlMask, () =>
            {
                _musicPlayerService.AddToCurrentPlaylist(source.Items[listView.SelectedItem]);
            });
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private async void PlaylistView(string id)
        {
            var playlist = await _subsonicService.GetPlaylist(id);
            mainView.RemoveAll();
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
                _musicPlayerService.PlayPlaylist(playlist.Id, playlist.Entry.IndexOf(song));
            };
            listView.RegisterHotKey(Key.M | Key.CtrlMask, () =>
             {
                 _musicPlayerService.AddToCurrentPlaylist(source.Items[listView.SelectedItem]);
             });
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private async void PlaylistsView()
        {
            var playlists = await _subsonicService.GetPlaylists();
            mainView.RemoveAll();
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
                PlaylistView(((Playlist)e.Value).Id);
            };
            mainView.Add(listView);
            listView.FocusFirst();
        }

        private void ArtistsView_Selected(ListViewItemEventArgs obj)
        {
            ArtistView(((Artist)obj.Value).Id);
        }

        private void AlbumView_Selected(ListViewItemEventArgs obj)
        {
            AlbumView(((Album)obj.Value).Id);
        }

        private void CurrentStateHandler(object sender, CurrentStateChangedEventArgs e)
        {
            _state = e.CurrentState;
            Application.MainLoop.Invoke(() =>
            {
                if (e.CurrentState?.CurrentTrack != null)
                {
                    _nowPlaying.Text = $"{e.CurrentState.CurrentTrack.Title} by {e.CurrentState.CurrentTrack.Artist}";
                    _songDuration.Text = e.CurrentState.CurrentTrack.Duration.GetAsMMSS();
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
                    _nowPlayingList.ScrollUp(_state.CurrentPlaylist.Entry.Count);
                    if (_state.CurrentTrack != null)
                    {
                        _nowPlayingList.SelectedItem = _state.CurrentPlaylist.Entry.IndexOf(_state.CurrentTrack);
                        _nowPlayingList.ScrollDown(_state.CurrentPlaylist.Entry.IndexOf(_state.CurrentTrack) - 3);
                    }
                }

                Application.Refresh();
            });
        }

        private void PlayingTimeHandler(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (_state != null && _state.CurrentTrack != null)
            {
                Application.MainLoop.Invoke(() =>
                {
                    _timeElapsed.Text = ((int)e.Time / 1000).GetAsMMSS();
                    _playingTime.Fraction = (e.Time / 1000f) / (_state.CurrentTrack.Duration);
                    Application.Refresh();
                });
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
            }
            if (_messageServer == null)
            {
                _messageServer = new WebSocketService(_subsonicService, _musicPlayerService);
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

            var sideBar = GetSidebar(win);
            win.Add(sideBar);
            mainView = GetMainView(sideBar);
            win.Add(mainView);
            var nowPlaying = GetAudioControlView(sideBar);
            win.Add(nowPlaying);
            var baseBar = GetBaseBar(nowPlaying);
            win.Add(baseBar);
            var currentPlayingList = GetCurrentPlaylist(mainView);
            win.Add(currentPlayingList);
            ArtistsView();
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
                _musicPlayerService.Next();
            });
            window.RegisterHotKey(Key.B | Key.CtrlMask, () =>
            {
                _musicPlayerService.Prev();
            });
            window.RegisterHotKey(Key.H | Key.CtrlMask, () =>
            {
                _musicPlayerService.PlayPause();
            });
            window.RegisterHotKey(Key.A | Key.CtrlMask, () =>
            {
                ArtistsView();
            });
            window.RegisterHotKey(Key.L | Key.CtrlMask, () =>
            {
                AlbumsView();
            });
            window.RegisterHotKey(Key.P | Key.CtrlMask, () =>
            {
                PlaylistsView();
            });
            window.RegisterHotKey(Key.R | Key.CtrlMask, () =>
            {
                SearchView();
            });
            window.RegisterHotKey(Key.S | Key.CtrlMask, () =>
            {
                _musicPlayerService.Shuffle();
            });
            window.RegisterHotKey(Key.J | Key.CtrlMask, () =>
            {
                JukeboxView();
            });
        }
    }
}