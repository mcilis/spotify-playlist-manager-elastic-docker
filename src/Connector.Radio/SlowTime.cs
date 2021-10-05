using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Connector.Radio
{
    internal class SlowTime : IRadio
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfigurationSection _radioConfiguration;

        public string Name => "SlowTime";   

        public SlowTime(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _radioConfiguration = configuration.GetSection("Connector.Radio:SlowTime");
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

                    var artistName = document.RootElement.GetProperty("Artist").GetString();
                    var trackName = document.RootElement.GetProperty("Title").GetString();
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

// {"Time":"16:37:46","Artist":"COLDPLAY","Title":"INK","Duration":"03:34.5","Cover":""}