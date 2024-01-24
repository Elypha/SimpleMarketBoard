#pragma warning disable CS8603

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Dalamud.Logging;
using System.Text.Json;
using SimpleMarketBoard.UniversalisModels;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Linq;

namespace SimpleMarketBoard
{
    public class Universalis
    {
        private const string Host = "https://universalis.app";
        private readonly HttpClient httpClient;
        private readonly Plugin plugin;

        public Universalis(Plugin plugin)
        {
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(plugin.Config.RequestTimeoutMS),
            };
            this.plugin = plugin;
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        public async Task<UniversalisResponse> CheckPrice()
        {
            var currentDataUrl = new UriBuilder(
                    $"{Host}/api/v2/{plugin.Config.selectedWorld}/{plugin.PriceChecker.gameItem.Id}?listings=75&entries=75"
                ).Uri.ToString();
            // var historyDataUrl = new UriBuilder($"{Host}/api/v2/history/{plugin.Config.selectedWorld}/{itemCache.ItemId}?entriesToReturn=75").Uri.ToString();
            var currentData = await GetJsonAsync<MarketDataCurrent>(currentDataUrl);
            // var historyData = await GetJsonAsync<MarketDataHistory>(historyDataUrl).ConfigureAwait(false);


            // update if there's world data
            var worldOutOfDateDict = new Dictionary<string, long>();
            if (currentData.WorldUploadTimes.Count > 0)
            {
                var worldUploadTimes = currentData.WorldUploadTimes.OrderByDescending(w => w.Value).ToList();
                foreach (var i in worldUploadTimes ?? new List<KeyValuePair<string, long>>() { })
                {
                    var worldRow = plugin.WorldSheet.GetRow(uint.Parse(i.Key));
                    if (worldRow == null) continue;
                    var worldName = worldRow.Name.ToString();
                    // name = name.Substring(0, Math.Min(4, name.Length));
                    var hours = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - i.Value) / 1000 / 3600;
                    // var hoursStr = hours.ToString("0.0");
                    worldOutOfDateDict.Add(worldName, hours);
                }
            }
            else
            {
                worldOutOfDateDict.Add(plugin.Config.selectedWorld, (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - currentData.LastUploadTime) / 1000 / 3600);
            }


            var response = new UniversalisResponse
            {
                ItemId = currentData.ItemId,
                RegionName = plugin.Config.selectedWorld,
                IsCrossWorld = currentData.WorldUploadTimes.Count > 0,
                WorldOutOfDate = worldOutOfDateDict,
                UnitsForSale = currentData.UnitsForSale,
                AveragePrice = currentData.AveragePrice,
                AveragePriceNq = currentData.AveragePriceNq,
                AveragePriceHq = currentData.AveragePriceHq,
                Velocity = currentData.Velocity,
                VelocityNq = currentData.VelocityNq,
                VelocityHq = currentData.VelocityHq,
                Listings = currentData.Listings,
                Entries = currentData.Entries,
            };


            Service.PluginLog.Debug($"UniversalisResponse: {JsonSerializer.Serialize(response)}");

            return response;
        }

        private async Task<T> GetJsonAsync<T>(string url)
        {
            try
            {
                Service.PluginLog.Info($"GetJsonAsync: {url}");

                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var responseStream = await response.Content.ReadAsStreamAsync();
                if (responseStream == null) throw new Exception($"responseStream is null: {response.StatusCode}");

                return await JsonSerializer.DeserializeAsync<T>(responseStream);
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error(ex, $"GetJsonAsync: {ex.Message}");
                return default;
            }
        }
    }
}
