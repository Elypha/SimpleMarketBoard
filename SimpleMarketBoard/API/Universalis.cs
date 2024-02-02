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
        private HttpClient httpClient;
        private readonly Plugin plugin;

        public Universalis(Plugin plugin)
        {
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(plugin.Config.RequestTimeout),
            };
            this.plugin = plugin;
        }

        public void ReloadHttpClient()
        {
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(plugin.Config.RequestTimeout),
            };
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        public async Task<UniversalisResponse> CheckPriceAsync()
        {
            return await CheckPrice();
        }

        public async Task<UniversalisResponse> CheckPrice()
        {
            try
            {
                // build url
                var API_URL = new UriBuilder(
                    $"{Host}/api/v2/{plugin.Config.selectedWorld}/{plugin.PriceChecker.gameItem.Id}?listings=75&entries=75"
                ).Uri.ToString();

                // get response
                Service.PluginLog.Info($"[Universalis] Fetch: {API_URL}");
                HttpResponseMessage API_Response = await httpClient.GetAsync(API_URL);
                API_Response.EnsureSuccessStatusCode();

                // decode response
                var API_ResponseDict = await API_Response.Content.ReadFromJsonAsync<MarketDataCurrent>();

                // validate response
                if (API_ResponseDict!.ItemId == 0)
                {
                    throw new JsonException("Response not decoded properly");
                }

                // update if there's world data
                var worldOutOfDateDict = new Dictionary<string, long>();
                if (API_ResponseDict.WorldUploadTimes.Count > 0)
                {
                    var worldUploadTimes = API_ResponseDict.WorldUploadTimes.OrderByDescending(w => w.Value).ToList();
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
                    worldOutOfDateDict.Add(plugin.Config.selectedWorld, (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - API_ResponseDict.LastUploadTime) / 1000 / 3600);
                }


                var response = new UniversalisResponse
                {
                    ItemId = API_ResponseDict.ItemId,
                    RegionName = plugin.Config.selectedWorld,
                    IsCrossWorld = API_ResponseDict.WorldUploadTimes.Count > 0,
                    WorldOutOfDate = worldOutOfDateDict,
                    UnitsForSale = API_ResponseDict.UnitsForSale,
                    AveragePrice = API_ResponseDict.AveragePrice,
                    AveragePriceNq = API_ResponseDict.AveragePriceNq,
                    AveragePriceHq = API_ResponseDict.AveragePriceHq,
                    Velocity = API_ResponseDict.Velocity,
                    VelocityNq = API_ResponseDict.VelocityNq,
                    VelocityHq = API_ResponseDict.VelocityHq,
                    Listings = API_ResponseDict.Listings,
                    Entries = API_ResponseDict.Entries,
                };


                Service.PluginLog.Debug($"[Universalis] UniversalisResponse: {JsonSerializer.Serialize(response)}");

                return response;

            }
            catch (HttpRequestException ex)
            {
                Service.PluginLog.Warning(ex, $"[Universalis] HTTP request not successful");
                Service.PluginLog.Debug(ex.Message);
                return new UniversalisResponse { ItemId = 0 };
            }
            catch (JsonException ex)
            {
                Service.PluginLog.Warning($"[Universalis] Response not decoded properly");
                Service.PluginLog.Debug(ex.Message);
                return new UniversalisResponse { ItemId = 0 };
            }
            catch (TaskCanceledException ex)
            {
                Service.PluginLog.Warning($"[Universalis] HTTP request cancelled by user configured timeout");
                Service.PluginLog.Debug(ex.Message);
                return new UniversalisResponse { ItemId = 0 };
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error(ex, $"[Universalis] Unknown error: {ex.Message}");
                return new UniversalisResponse { ItemId = 0 };
            }
        }
    }
}
