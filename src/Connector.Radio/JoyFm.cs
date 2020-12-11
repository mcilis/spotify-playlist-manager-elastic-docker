using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Connector.Radio
{
    internal class JoyFm : IRadio
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfigurationSection _radioConfiguration;

        public string Name => "JoyFm";   

        public JoyFm(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _radioConfiguration = configuration.GetSection("Connector.Radio:JoyFm");
        }    

        public async Task<string> GetCurrentSongAsync()
        {
            using var client = _httpClientFactory.CreateClient(Name);
            
            var request = new HttpRequestMessage(HttpMethod.Get, _radioConfiguration["RequestUri"]);

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    using var document = JsonDocument.Parse(responseContent, new JsonDocumentOptions { AllowTrailingCommas = true });

                    var currentSongProperty = document.RootElement.GetProperty("data").GetProperty("current_song");

                    var artistName = currentSongProperty.GetProperty("artist").GetString();
                    var trackName = currentSongProperty.GetProperty("title").GetString();
                    var song = $"{trackName.Trim().Replace(" ", "+")}+{artistName.Trim().Replace(" ", "+")}".Trim('+');

                    return song;
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Response:{ResponseContent}", responseContent);
                    return "";
                }
            }

            Log.Error("Response:{StatusCode} | {ResponseContent}", response.StatusCode, responseContent);
            return "";
        }
    }
}