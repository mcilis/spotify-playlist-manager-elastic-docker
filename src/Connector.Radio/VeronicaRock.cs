using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Connector.Radio
{
    internal class VeronicaRock : IRadio
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfigurationSection _radioConfiguration;

        public string Name => "VeronicaRock";   

        public VeronicaRock(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _radioConfiguration = configuration.GetSection("Connector.Radio:VeronicaRock");
        }    

        public async Task<string> GetCurrentSongAsync()
        {
            using var client = _httpClientFactory.CreateClient(Name);

            var request = new HttpRequestMessage(HttpMethod.Get, _radioConfiguration["RequestUri"]);
            request.Headers.Add("x-api-key", _radioConfiguration["ApiKey"]);

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    using var document = JsonDocument.Parse(responseContent, new JsonDocumentOptions { AllowTrailingCommas = true });

                    var stationsProperty = document.RootElement.GetProperty("data").GetProperty("getStations")[0].GetProperty("items");

                    var stationProperty = stationsProperty.EnumerateArray().FirstOrDefault(x => x.GetProperty("slug").GetString() == "radio-veronica-rockradio");

                    var currentSongProperty = stationProperty.GetProperty("playouts")[0].GetProperty("track");

                    var artistName = currentSongProperty.GetProperty("artistName").GetString();
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