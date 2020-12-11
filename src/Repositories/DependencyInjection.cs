using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Repositories
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            var dbConfiguration = configuration.GetSection("SqlLite");

            services.AddSingleton<ICredentialsRepository, CredentialsRepository>();
            services.AddSingleton<IPlaylistRepository, PlaylistRepository>();
            services.AddSingleton<ITrackRepository, TrackRepository>();
            
            return services;
        }
    }
}
