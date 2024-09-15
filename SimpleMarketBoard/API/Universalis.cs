#pragma warning disable CS8603

using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;

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
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");
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
            var API_URL = new UriBuilder(
                $"{Host}/api/v2/{gameItem.TargetRegion}/{gameItem.Id}?listings={plugin.Config.UniversalisListings}&entries={plugin.Config.UniversalisEntries}{_hq}"
            ).Uri.ToString();

            // get response
            Service.PluginLog.Info($"[Universalis] Fetch: {API_URL}");
            HttpResponseMessage API_Response = await httpClient.GetAsync(API_URL);
            API_Response.EnsureSuccessStatusCode();

            // decode response
            var API_ResponseDict = await API_Response.Content.ReadFromJsonAsync<MarketDataCurrent>();

            // validate response
            if (API_ResponseDict!.ItemId == 0 || API_ResponseDict!.ItemId != gameItem.Id)
            {
                throw new JsonException();
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
                worldOutOfDateDict.Add(gameItem.TargetRegion, (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - API_ResponseDict.LastUploadTime) / 1000 / 3600);
            }


            var response = new UniversalisResponse
            {
                Status = UniversalisResponseStatus.Success,
                ItemId = API_ResponseDict.ItemId,
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
            return new UniversalisResponse { Status = UniversalisResponseStatus.ServerError };
        }
        catch (JsonException ex)
        {
            Service.PluginLog.Warning($"[Universalis] Invalid item ID");
            Service.PluginLog.Debug(ex.Message);
            return new UniversalisResponse { Status = UniversalisResponseStatus.InvalidItemId };
        }
        catch (TaskCanceledException ex)
        {
            Service.PluginLog.Warning($"[Universalis] HTTP request cancelled by user configured timeout");
            Service.PluginLog.Debug(ex.Message);
            return new UniversalisResponse { Status = UniversalisResponseStatus.UserCancellation };
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, $"[Universalis] Unknown error: {ex.Message}");
            return new UniversalisResponse { Status = UniversalisResponseStatus.UnknownError };
        }
    }
}
