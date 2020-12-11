using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Repositories;

namespace Connector.Spotify
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSpotifyManagement(this IServiceCollection services, IConfiguration configuration)
        {
            var spotifyConfiguration = configuration.GetSection("Connector.Spotify");

            services.AddHttpClient("SpotifyApi", x =>
            {
                x.BaseAddress = new Uri(spotifyConfiguration["ApiServer"]);
                x.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            var key = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{spotifyConfiguration["ClientId"]}:{spotifyConfiguration["ClientSecret"]}"));
            services.AddHttpClient("TokenApi", x =>
            {
                x.BaseAddress = new Uri(spotifyConfiguration["TokenServer"]);
                x.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {key}");
            });

           
            services.AddSingleton<ISpotifyManagement, SpotifyManagement>();

            return services;
        }
    }
}
