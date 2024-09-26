#pragma warning disable CS8603

using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using Miosuke;

using SimpleMarketBoard.UniversalisModels;


namespace SimpleMarketBoard;

public class Universalis
{
    private readonly Plugin plugin;

    public Universalis(Plugin plugin)
    {
        httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(plugin.Config.RequestTimeout),
        };
        this.plugin = plugin;
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }


    // -------------------------------- http client --------------------------------
    private const string Host = "https://universalis.app";
    private HttpClient httpClient;

    public void ReloadHttpClient()
    {
        httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(plugin.Config.RequestTimeout),
        };
        httpClient.DefaultRequestHeaders.Add("User-Agent", "SimpleMarketBoard/1.0 (Dalamud; FFXIV)");
    }


    // -------------------------------- http methods --------------------------------
    public async Task<UniversalisResponse> GetDataAsync(PriceChecker.GameItem gameItem)
    {
        return await GetData(gameItem);
    }

    public async Task<UniversalisResponse> GetData(PriceChecker.GameItem gameItem)
    {
        try
        {
            // build url
            var _hq = plugin.Config.UniversalisHqOnly ? "&hq=1" : "";
            var API_URL = new UriBuilder($"{Host}/api/v2/{gameItem.TargetRegion}/{gameItem.Id}?listings={plugin.Config.UniversalisListings}&entries={plugin.Config.UniversalisEntries}{_hq}").Uri.ToString();

            // get response
            Service.Log.Info($"[Universalis] Fetch: {API_URL}");
            var response = await httpClient.GetAsync(API_URL);
            if (response.IsSuccessStatusCode == false)
            {
                Service.Log.Warning($"[Universalis] HTTP request not successful: {response.StatusCode}");
                return new UniversalisResponse { Status = UniversalisResponseStatus.ServerError };
            }

            // decode response
            var data = await response.Content.ReadFromJsonAsync<MarketDataCurrent>();
            if (data is null)
            {
                Service.Log.Warning($"[Universalis] Parse JSON failed");
                return new UniversalisResponse { Status = UniversalisResponseStatus.InvalidData };
            }

            // update if there's world data
            var worldUpdatedData = new Dictionary<string, long>();
            if (data.WorldUploadTimes.Count > 0)
            {
                var worldUploadTimes = data.WorldUploadTimes.OrderByDescending(w => w.Value).ToList();
                foreach (var i in worldUploadTimes ?? [])
                {
                    var worldRow = plugin.WorldSheet.GetRow(uint.Parse(i.Key));
                    if (worldRow is null) continue;
                    var worldName = worldRow.Name.ToString();
                    var hours = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - i.Value) / 1000 / 3600;
                    worldUpdatedData.Add(worldName, hours);
                }
            }
            else
            {
                worldUpdatedData.Add(gameItem.TargetRegion, (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - data.LastUploadTime) / 1000 / 3600);
            }

            var universalisResponse = new UniversalisResponse
            {
                Status = UniversalisResponseStatus.Success,
                ItemId = data.ItemId,
                IsCrossWorld = data.WorldUploadTimes.Count > 0,
                WorldOutOfDate = worldUpdatedData,
                UnitsForSale = data.UnitsForSale,
                AveragePrice = data.AveragePrice,
                AveragePriceNq = data.AveragePriceNq,
                AveragePriceHq = data.AveragePriceHq,
                Velocity = data.Velocity,
                VelocityNq = data.VelocityNq,
                VelocityHq = data.VelocityHq,
                Listings = data.Listings,
                Entries = data.Entries,
            };
            Service.Log.Debug($"[Universalis] UniversalisResponse: {JsonSerializer.Serialize(universalisResponse)}");

            return universalisResponse;
        }
        catch (TaskCanceledException ex)
        {
            Service.Log.Warning($"[Universalis] HTTP request cancelled by user configured timeout");
            Service.Log.Debug(ex.Message);
            return new UniversalisResponse { Status = UniversalisResponseStatus.UserCancellation };
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, $"[Universalis] Unknown error: {ex.Message}");
            return new UniversalisResponse { Status = UniversalisResponseStatus.UnknownError };
        }
    }
}
