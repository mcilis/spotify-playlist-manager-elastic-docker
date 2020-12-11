using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Connector.Radio;
using Connector.Spotify;
using Domain;
using Microsoft.Extensions.Hosting;
using Repositories;
using Serilog;

namespace Worker
{
    public class Worker : BackgroundService
    {
        private readonly IRadioFactory _radioFactory;
        private readonly ISpotifyManagement _spotify;
        private readonly IPlaylistRepository _playlistRepository;
        private readonly ITrackRepository _trackRepository;

        public Worker(IRadioFactory radioFactory, ISpotifyManagement spotify,
        IPlaylistRepository playlistRepository, ITrackRepository trackRepository)
        {
            _radioFactory = radioFactory;
            _spotify = spotify;
            _playlistRepository = playlistRepository;
            _trackRepository = trackRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {   
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var radio in _radioFactory.ResolveAll())
                    {
                        var playlist = await GetPlaylistAsync(radio);
                        var song = await radio.GetCurrentSongAsync();

                        await AddSongToSpotify(playlist, song);
                    }
                }
                catch (System.Exception exception)
                {
                    Log.Fatal(exception, "SpotifyPlaylistManager Worker Error!");
                }

                if (DateTime.UtcNow.Hour > 22)
                {
                    await Task.Delay(28800000, stoppingToken); // wait 8 hours for the new song
                }

                await Task.Delay(240000, stoppingToken); // wait 4 minutes for the new song
            }
        }



        private async Task RegisterAllPlaylistsAsync()
        {
            var offset = 0;
            List<Playlist> playlists;
            do
            {
                playlists = await _spotify.GetPlaylistsAsync(offset: offset);

                foreach (var spotifyPlaylist in playlists)
                {
                    spotifyPlaylist.CreateDate = DateTime.UtcNow;

                    var playlistId = await _playlistRepository.RegisterAsync(spotifyPlaylist);

                    spotifyPlaylist.PlaylistId = playlistId;
                    offset++;                    
                }
                Log.Information("{offset}", offset);
            }
            while (playlists?.Count > 0);
        }

        private async Task<Playlist> GetPlaylistAsync(IRadio radio)
        {
            var name = $"{radio.Name} {DateTime.UtcNow:MMMM yyyy}";

            if (!(DateTime.UtcNow.Hour == 10 && DateTime.UtcNow.Minute > 2 && DateTime.UtcNow.Minute < 8))
            {
                // Once a day update the playlists!

                var dbPlaylist = await _playlistRepository.RetrieveAsync(name: name);

                if (dbPlaylist != null)
                    return dbPlaylist;
            }

            var remotePlaylist = await _spotify.FindPlaylistAsync(name) ??
                                    await _spotify.CreatePlaylistAsync(name);

            if (remotePlaylist != null)
            {
                var id = await _playlistRepository.RegisterAsync(remotePlaylist);

                if (id < 1)
                {
                    Log.Error("Failed to register the playlist to repository! {@RemotePlaylist}", remotePlaylist);
                    return null;
                }
                remotePlaylist.PlaylistId = id;
            }

            return remotePlaylist;
        }

        private async Task AddSongToSpotify(Playlist playlist, string song, bool checkExistence = true)
        {
            if (playlist == null || string.IsNullOrWhiteSpace(song) || song == "+" || song =="-")
                return;

            var track = await _trackRepository.RetrieveAsync(searchText: song);

            if (track == null)
            {
                track = await _spotify.SearchForATrackAsync(song);

                if (track == null)
                {
                    Log.Warning("SpotifyTrackNotFound! {SearchedText} for Radio {radio}", song, playlist.Name.Split(' ')[0]);
                    return;
                }

                track.Artist = track.Artists.FirstOrDefault()?.Name;
                track.SearchText = song;
                track.CreateDate = DateTime.UtcNow;

                var dbTrack = await _trackRepository.RetrieveAsync(code: track.Code);

                if (dbTrack == null)
                {
                    var trackId = await _trackRepository.RegisterAsync(track);

                    if (trackId < 1)
                    {
                        Log.Error("Failed to register the track to repository! {@track}", track);
                        return;
                    }
                    track.TrackId = trackId;
                }
                else
                {
                    track.TrackId = dbTrack.TrackId;
                    Log.Warning("Track is present with different search text! {@SpotifyTrack} {@DbTrack}", track, dbTrack);                    
                }
            }

            if (checkExistence && await _playlistRepository.ContainsTrackAsync(playlist, track))
            {
                return;
            }                       

            if (await _spotify.AddTrackToPlaylistAsync(playlist, track))
            {
                Log.Information("Spotify track is added. Playlist:{@Playlist} Track:{@Track}", playlist, track);
            }

            if (!await _playlistRepository.AddTrackAsync(playlist, track))
            {
                Log.Error("Failed to add the track to playlist! {@Playlist} {@Track}", playlist, track);
            }
        }
    }
}
