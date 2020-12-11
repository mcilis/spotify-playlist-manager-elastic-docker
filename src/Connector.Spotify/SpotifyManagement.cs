using System.Collections.Generic;
using Domain;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using Repositories;
using System.Reflection;
using System.Text;
using System.Linq;

namespace Connector.Spotify
{
    internal class SpotifyManagement : ISpotifyManagement
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ICredentialsRepository _credentialsRepository;
        private readonly IConfigurationSection _configs;
        private const string SpotifyApi = "SpotifyApi";
        private const string TokenApi = "TokenApi";


        public SpotifyManagement(IHttpClientFactory httpClientFactory, IConfiguration configuration, ICredentialsRepository credentialsRepository)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _credentialsRepository = credentialsRepository;
            _configs = configuration.GetSection("Connector.Spotify");
        }
        public async Task<bool> AddTrackToPlaylistAsync(Playlist playlist, Track track, string ownerId = "mcilis")
        {
            // curl -X POST "https://api.spotify.com/v1/users/mcilis/playlists/3cEYpjA9oz9GiPac4AsH4n/tracks?position=0&uris=spotify%3Atrack%3A4iV5W9uYEdYUVa79Axb7Rh,spotify%" -H "Accept: application/json" -H "Authorization: Bearer ..."

            if (playlist == null || track == null)
                return false;

            using var client = _httpClientFactory.CreateClient(SpotifyApi);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", await GetAccessTokenAsync());

            var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/users/{ownerId}/playlists/{playlist.Code}/tracks?position=0&uris={track.Uri}");

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync(); 

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            Log.Error("{Method} Response: {Status} | {Response}", MethodBase.GetCurrentMethod().Name, response.StatusCode, responseContent);
            return false;
        }

        public async Task<Playlist> CreatePlaylistAsync(string playlistName, bool isPublic = true, string ownerId = "mcilis")
        {
            // curl -X POST "https://api.spotify.com/v1/users//playlists" -H "Accept: application/json" -H "Authorization: Bearer ..." -H "Content-Type: application/json" --data "{\"name\":\"NewPlaylist\",\"public\":false}"

            if (string.IsNullOrWhiteSpace(playlistName))
                return null;

            using var client = _httpClientFactory.CreateClient(SpotifyApi);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", await GetAccessTokenAsync());

            var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/users/{ownerId}/playlists")
            {
                Content = new StringContent(JsonSerializer.Serialize(new {name = playlistName, @public = isPublic}), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var playlist = JsonSerializer.Deserialize<Playlist>(responseContent);
                    playlist.CreateDate = DateTime.UtcNow;
                    return playlist;
                }
                catch (Exception e)
                {
                    Log.Error(e, "{Method} Response:{Response}", MethodBase.GetCurrentMethod().Name, responseContent);
                    return null;
                }
            }

            Log.Error("{Method} Response: {Status} | {Response}", MethodBase.GetCurrentMethod().Name, response.StatusCode, responseContent);
            return null;
        }

        public async Task<Playlist> FindPlaylistAsync(string playlistName, string ownerId = "mcilis")
        {
            if (string.IsNullOrWhiteSpace(playlistName))
                return null;

            var playlists = await GetPlaylistsAsync(ownerId);

            return playlists?.FirstOrDefault(x => x.Name == playlistName);
        }

        public async Task<List<Playlist>> GetPlaylistsAsync(string ownerId = "mcilis", int offset = 0, int limit = 50)
        {
            // curl -X GET "https://api.spotify.com/v1/users/mcilis/playlists" -H "Accept: application/json" -H "Authorization: Bearer ..."

            using var client = _httpClientFactory.CreateClient(SpotifyApi);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", await GetAccessTokenAsync());

            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/users/{ownerId}/playlists?offset={offset}&limit={limit}");

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    using var document = JsonDocument.Parse(responseContent, new JsonDocumentOptions { AllowTrailingCommas = true });

                    var results = document.RootElement.GetProperty("items").EnumerateArray();

                    var playlistResults = new List<Playlist>();

                    foreach (var element in results)
                    {
                        var playlistResult = JsonSerializer.Deserialize<Playlist>(element.GetRawText());
                        playlistResult.Uri = element.GetProperty("tracks").GetProperty("href").GetString();
                        playlistResult.TracksCount = element.GetProperty("tracks").GetProperty("total").GetInt32();
                        playlistResults.Add(playlistResult);
                    }

                    return playlistResults;
                }
                catch (Exception e)
                {
                    Log.Error(e, "{Method} Response:{Response}", MethodBase.GetCurrentMethod().Name, responseContent);
                    return null;
                }
            }

            Log.Error("{Method} Response: {Status} | {Response}", MethodBase.GetCurrentMethod().Name, response.StatusCode, responseContent);
            return null;
        }

        public async Task<Track> SearchForATrackAsync(string query)
        {
            // curl -X GET "https://api.spotify.com/v1/search?q=Belfast+Child++Simple+Minds&type=track&market=TR&limit=1" -H "Accept: application/json"

            if (string.IsNullOrWhiteSpace(query))
                return null;

            using var client = _httpClientFactory.CreateClient(SpotifyApi);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", await GetAccessTokenAsync());

            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/search?q={query}&type=track&market=TR");

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    using var document = JsonDocument.Parse(responseContent, new JsonDocumentOptions { AllowTrailingCommas = true });

                    var results = document.RootElement.GetProperty("tracks").GetProperty("items").EnumerateArray();

                    var trackResults = new List<Track>();

                    foreach (var element in results)
                    {
                        var trackResult = JsonSerializer.Deserialize<Track>(element.GetRawText());
                        trackResults.Add(trackResult);
                    }
                    return trackResults.OrderByDescending(x => x.Popularity).FirstOrDefault();
                }
                catch (Exception e)
                {
                    Log.Error(e, "{Method} {Query} Response:{Response}", MethodBase.GetCurrentMethod().Name, query, responseContent);
                    return null;
                }
            }

            Log.Error("{Method} {Query} Response: {Status} | {Response}", MethodBase.GetCurrentMethod().Name, query, response.StatusCode, responseContent);

            return null;
        }



        private async Task<string> GetAccessTokenAsync()
        {
            var credentials = await _credentialsRepository.RetrieveAsync();

            if (credentials != null && !string.IsNullOrEmpty(credentials.AccessToken) && credentials.CreateDate.AddSeconds(credentials.ExpiresIn - 10) > DateTime.UtcNow)
            {
                return $"{credentials.TokenType} {credentials.AccessToken}";
            }

            switch (_configs["AuthorizationMode"])
            {
                case "InitialConfiguration":
                    {
                        // IMPORTANT! Use: GetSpotifyAccessTokenCode.html

                        credentials = await CreateCredentialsAsync();
                        Log.Fatal("{Method} Write the following refresh token value to the AppSettings:Spotify:RefreshToken: {RefreshToken}", 
                            MethodBase.GetCurrentMethod().Name, credentials.RefreshToken);  // Write this to the RefreshToken field in config file!!!
                        break;
                    }
                case "RefreshToken":
                    {
                        credentials = await RefreshCredentialsAsync();
                        break;
                    }
                default:
                    {
                        throw new Exception($"{MethodBase.GetCurrentMethod().Name} Invalid AuthorizationMode: {_configs["AuthorizationMode"]} Check settings!");
                    }
            }

            if (!await _credentialsRepository.RegisterAsync(credentials))
            {
                Log.Error("Failed to register credentials! {@Credentials}", credentials);
            }

            return $"{credentials.TokenType} {credentials.AccessToken}";
        }

        private async Task<Credentials> RefreshCredentialsAsync()
        {
            using var client = _httpClientFactory.CreateClient(TokenApi);

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/token")
            {
                Content = new StringContent($"grant_type=refresh_token&refresh_token={_configs["RefreshToken"]}", Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"{MethodBase.GetCurrentMethod().Name} request failed. Response:{response.StatusCode} | {responseContent}");
            }
            try
            {
                var credentials = JsonSerializer.Deserialize<Credentials>(responseContent);
                credentials.CreateDate = DateTime.UtcNow;
                return credentials;
            }
            catch (Exception e)
            {
                throw new Exception($"{MethodBase.GetCurrentMethod().Name} failed deserializing response: {responseContent}", e);
            }
        }

        private async Task<Credentials> CreateCredentialsAsync()
        {
            using var client = _httpClientFactory.CreateClient(TokenApi);

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/token")
            {
                Content = new StringContent($"grant_type=authorization_code&code={_configs["Code"]}&redirect_uri=http://localhost/callback/", Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"{MethodBase.GetCurrentMethod().Name} request failed. Response:{response.StatusCode} | {responseContent}");
            }
            try
            {
                var credentials = JsonSerializer.Deserialize<Credentials>(responseContent);
                credentials.CreateDate = DateTime.UtcNow;
                return credentials;
            }
            catch (Exception e)
            {
                throw new Exception($"{MethodBase.GetCurrentMethod().Name} failed deserializing response: {responseContent}", e);
            }
        }

        //private async Task<Credentials> CreateInitialCredentialsAsync()
        //{
        //    using var client = _httpClientFactory.CreateClient(TokenApi);

        //    var request = new HttpRequestMessage(HttpMethod.Post, "/api/token")
        //    {
        //        Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded")
        //    };

        //    var response = await client.SendAsync(request);
        //    var responseContent = await response.Content.ReadAsStringAsync();

        //    if (!response.IsSuccessStatusCode)
        //    {
        //        throw new Exception($"{MethodBase.GetCurrentMethod().Name} request failed. Response:{response.StatusCode} | {responseContent}");
        //    }
        //    try
        //    {
        //        return JsonSerializer.Deserialize<Credentials>(responseContent);
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception($"{MethodBase.GetCurrentMethod().Name} failed deserializing response: {responseContent}", e);
        //    }
        //}
    }
}