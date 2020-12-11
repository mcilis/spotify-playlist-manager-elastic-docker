using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Connector.Radio
{
    internal class RedFm : IRadio
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfigurationSection _radioConfiguration;

        public string Name => "RedFm";   

        public RedFm(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _radioConfiguration = configuration.GetSection("Connector.Radio:RedFm");
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

                    var items = document.RootElement.GetProperty("feed").GetProperty("items").EnumerateArray();

                    foreach (var item in items)
                    {
                        var type = item.GetProperty("type").GetString();

                        if (type != "song")
                        {
                            continue;
                        }

                        var artistName = item.GetProperty("title").GetString();
                        var trackName = item.GetProperty("desc").GetString();
                        var song = $"{trackName.Trim().Replace(" ", "+")}+{artistName.Trim().Replace(" ", "+")}";

                        return song;
                    }
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