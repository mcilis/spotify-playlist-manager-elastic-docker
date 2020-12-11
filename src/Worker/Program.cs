using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Connector.Radio;
using Connector.Spotify;
using Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using System.Reflection;

namespace Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var elasticSearchAddress = Environment.GetEnvironmentVariable("ELASTICSEARCH_URI") ?? "http://spotify_elasticsearch_1:9200";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning) 
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithExceptionDetails()
                .WriteTo.Console()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticSearchAddress))
                {
                    AutoRegisterTemplate = true,
                    IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name.ToLower()}-{DateTime.UtcNow:yyyy-MM}",
                    FailureCallback = e => Console.WriteLine($"ElasticsearchSink - Unable to submit event {e.MessageTemplate}"),
                    EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog | EmitEventFailureHandling.RaiseCallback,
                })
                .CreateLogger();
            
            try
            {
                 // Testing environment values
                Log.Debug("Environment values access test. Result={@test}", Environment.GetEnvironmentVariable("TEST") ?? "Cannot access the test environment variable!!!");

                Log.Information("SpotifyPlaylistManagerWorker.Program Starting Up");
                CreateHostBuilder(args).Build().Run();               
            }
            catch (Exception e)
            {
                Log.Fatal(e, "SpotifyPlaylistManagerWorker.Program failed to start correctly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();

                    services.AddRepositories(hostContext.Configuration);
                    services.AddRadioManagement(hostContext.Configuration);
                    services.AddSpotifyManagement(hostContext.Configuration);
                });
    }
}
