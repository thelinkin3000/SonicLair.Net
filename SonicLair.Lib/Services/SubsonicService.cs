using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SonicLair.Lib.Infrastructure;
using SonicLair.Lib.Types.SonicLair;
using SonicLair.Lib.Types.Subsonic;

namespace SonicLair.Lib.Services
{
    public interface ISubsonicService
    {
        void Configure(Account account);
        Account GetActiveAccount();
        Task<Album> GetAlbum(string id);
        Task<List<Album>> GetAlbums(string type = "frequent", int size = 10);
        Task<List<Album>> GetAllAlbums();
        Task<Artist> GetArtist(string id);
        Task<List<Artist>> GetArtists();
        Dictionary<string, string> GetBasicParams();
        Task<byte[]> GetCoverArt(string id);
        string GetCoverArtUri(string id);
        Task<Playlist> GetPlaylist(string id);
        Task<List<Playlist>> GetPlaylists();
        Task<List<Song>> GetRandomSongs();
        Task<List<Song>> GetSimilarSongs(string id);
        Task<Song> GetSong(string id);
        Uri GetSongUri(string id);
        void Logout();
        Task<T> MakeSubsonicRequest<T>(string path, Dictionary<string, string> parameters) where T : SubsonicResponse;
        Task<SearchResult> Search(string query);
    }

    public class SubsonicService : ISubsonicService
    {
        private Account activeAccount;
        private const string AppIdentifier = "SonicLair";
        private const string Version = "1.16.1";
        private HttpClient client;

        public SubsonicService()
        {

        }

        public Account GetActiveAccount()
        {
            return activeAccount;
        }

        public void Configure(Account account)
        {
            activeAccount = account;

            client = new HttpClient() { BaseAddress = new Uri(activeAccount.Url) };
        }

        public void Logout()
        {
            activeAccount = null;
        }

        public Dictionary<string, string> GetBasicParams()
        {
            Dictionary<string, string> param = new Dictionary<string, string>
            {
                { "u", activeAccount.Username },
                { "c", AppIdentifier },
                { "v", Version },
                { "f", "json" }
            };
            if (activeAccount.UsePlaintext)
            {
                param.Add("p", activeAccount.Password);
            }
            else
            {
                var salt = TokenGenerator.RandomString(10);
                param.Add("s", salt);
                var token = TokenGenerator.GetTokenizedPassword(activeAccount.Password, salt);
                param.Add("t", token);
            }
            return param;
        }

        public string GetCoverArtUri(string id)
        {
            var param = GetBasicParams();
            param.Add("id", id);
            return new Uri($"{activeAccount.Url}/rest/getCoverArt{Tools.ToUrlEncodedParams(param)}").ToString();
        }

        public Uri GetSongUri(string id)
        {
            var param = GetBasicParams();
            param.Add("id", id);
            return new Uri($"{activeAccount.Url}/rest/stream{Tools.ToUrlEncodedParams(param)}");
        }

        public async Task<T> MakeSubsonicRequest<T>(string path, Dictionary<string, string> parameters) where T : SubsonicResponse
        {
            var param = GetBasicParams();
            foreach (var p in parameters)
            {
                param.Add(p.Key, p.Value);
            }
            var response = await client.GetAsync($"{path}{Tools.ToUrlEncodedParams(param)}");
            var s = await response.Content.ReadAsStringAsync();
            JObject jObject = JObject.Parse(s);
            if (!jObject.ContainsKey("subsonic-response"))
            {
                throw new SubsonicException("The server sent a malformed response");
            }
            var r = JsonConvert.DeserializeObject<T>(jObject["subsonic-response"].ToString());
            if (r.Status != "ok")
            {
                throw new SubsonicException(r.Error.Message);
            }
            return r;
        }

        public async Task<List<Artist>> GetArtists()
        {
            var reqUrl = $"/rest/getArtists";
            ArtistsResponse ret;

            ret = await MakeSubsonicRequest<ArtistsResponse>(reqUrl, new Dictionary<string, string>());
            List<Artist> r = new List<Artist>();
            foreach (var index in ret.Artists.Index)
            {
                r.AddRange(index.Artist);
            }
            return r;

        }

        public async Task<Artist> GetArtist(string id)
        {
            var reqUrl = $"/rest/getArtist";
            ArtistResponse ret;
            var parameters = new Dictionary<string, string>();
            parameters.Add("id", id);
            ret = await MakeSubsonicRequest<ArtistResponse>(reqUrl, parameters);
            return ret.Artist;
        }

        public async Task<List<Album>> GetAllAlbums()
        {
            var reqUrl = $"/rest/getAlbumList2";
            var ret = new List<Album>();
            var i = 0;
            while(ret.Count == 0 || (ret.Count % 500) == 0)
            {
                var param = new Dictionary<string, string>
                {
                    { "type", "alphabeticalByName" },
                    { "size", "500" },
                    { "offset", (i * 500).ToString() },
                };
                try
                {
                    var r = await MakeSubsonicRequest<AlbumsResponse>(reqUrl, param);
                    if(r.AlbumList2.Album.Count == 0)
                    {
                        break;
                    }
                    ret.AddRange(r.AlbumList2.Album);
                }
                catch(Exception)
                {
                    break;
                }
                i += 1;
            }
            return ret;
        }

        public async Task<List<Album>> GetAlbums(string type = "frequent", int size = 10)
        {
            var reqUrl = $"/rest/getAlbumList2";
            var param = new Dictionary<string, string>
            {
                { "type", type },
                { "size", size.ToString() }
            };
            var ret = await MakeSubsonicRequest<AlbumsResponse>(reqUrl, param);
            return ret.AlbumList2.Album;
        }

        public async Task<List<Playlist>> GetPlaylists()
        {
            var reqUrl = $"/rest/getPlaylists";
            var param = new Dictionary<string, string>();

            var ret = await MakeSubsonicRequest<PlaylistsResponse>(reqUrl, param);
            return ret.Playlists.Playlist;
        }

        public async Task<Playlist> GetPlaylist(string id)
        {
            var reqUrl = $"/rest/getPlaylist";
            var param = new Dictionary<string, string>
            {
                { "id", id }
            };
            var ret = await MakeSubsonicRequest<PlaylistResponse>(reqUrl, param);
            return ret.Playlist;
        }

        public async Task<Album> GetAlbum(string id)
        {
            var param = new Dictionary<string, string>
            {
                { "id", id }
            };
            var reqUrl = $"/rest/getAlbum";
            var ret = await MakeSubsonicRequest<AlbumResponse>(reqUrl, param);
            return ret.Album;
        }

        public async Task<Song> GetSong(string id)
        {
            var param = new Dictionary<string, string>
            {
                { "id", id }
            };
            var reqUrl = $"/rest/getSong";
            var ret = await MakeSubsonicRequest<SongResponse>(reqUrl, param);
            return ret.Song;
        }

        public async Task<List<Song>> GetSimilarSongs(string id)
        {
            var param = new Dictionary<string, string>
            {
                { "id", id }
            };
            var reqUrl = $"/rest/getSimilarSongs2";
            var ret = await MakeSubsonicRequest<SimilarSongsResponse>(reqUrl, param);
            return ret.SimilarSongs2.Song;
        }
        public async Task<List<Song>> GetRandomSongs()
        {
            var param = new Dictionary<string, string>
            {
                { "size", "10" }
            };
            var reqUrl = $"/rest/getRandomSongs";
            var ret = await MakeSubsonicRequest<RandomSongsResponse>(reqUrl, param);
            return ret.RandomSongs.Song;
        }

        public async Task<byte[]> GetCoverArt(string id)
        {
            var param = GetBasicParams();
            param.Add("id", id);
            var reqUrl = $"/rest/getCoverArt{Tools.ToUrlEncodedParams(param)}";
            var response = await client.GetAsync(reqUrl);
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<SearchResult> Search(string query)
        {
            var param = new Dictionary<string, string>
            {
                { "query", query }
            };
            var reqUrl = $"/rest/search3";
            var ret = await MakeSubsonicRequest<SearchResponse>(reqUrl, param);

            return new SearchResult(ret.SearchResult3.Song, ret.SearchResult3.Album, ret.SearchResult3.Artist);
        }
    }


    public enum ResponseStatus
    {
        Ok,
        Failure
    }
}