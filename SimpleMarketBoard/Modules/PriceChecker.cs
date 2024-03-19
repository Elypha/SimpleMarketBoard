using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;

using SimpleMarketBoard.UniversalisModels;
using Miosuke;


namespace SimpleMarketBoard;

public class PriceChecker
{
    private readonly Plugin plugin;

    public PriceChecker(Plugin plugin)
    {
        this.plugin = plugin;
    }

    public void Dispose()
    {
        ItemCancellationTokenSource?.Dispose();
    }


    // -------------------------------- game item --------------------------------
    public List<GameItem> GameItemCacheList = new List<GameItem>();

    public class GameItem
    {
        public ulong Id { get; set; }
        public bool IsHQ { get; set; }
        public string Name { get; set; } = "";
        public string TargetRegion { get; set; } = "";
        public ulong PlayerWorldId { get; set; }
        public uint VendorSelling { get; set; }
        public Item InGame { get; set; } = null!;
        public ulong FetchTimestamp { get; set; }
        public UniversalisResponse UniversalisResponse { get; set; } = new UniversalisResponse();
        public Dictionary<string, long> WorldOutOfDate { get; set; } = new Dictionary<string, long>();
        public double AvgPrice { get; set; }
        public ulong Result { get; set; } = GameItemResult.Init;
    }

    public class GameItemResult
    {
        public const ulong Init = 0;
        public const ulong Success = 1;
        public const ulong InGameError = 10;
        public const ulong Unmarketable = 11;
        public const ulong UnknownUserWorld = 12;
        public const ulong APIError = 20;
    }


    // -------------------------------- others --------------------------------
    public CancellationTokenSource? ItemCancellationTokenSource;


    // -------------------------------- price checker --------------------------------
    public void CheckNewAsync(ulong itemId, bool isHQ)
    {
        Service.PluginLog.Debug($"[PriceChecker] Start CheckNewAsync: {itemId}, {isHQ}");

        Task.Run(() =>
        {
            try
            {
                CheckNew(itemId, isHQ);
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error($"[PriceChecker] CheckNewAsync failed, {ex.Message}");
                plugin.MainWindow.CurrentItemLabel = "Error";
                ItemCancellationTokenSource = null;
            }
        });
    }

    public void CheckNew(ulong itemId, bool isHQ)
    {
        // handle hover delay
        if (ItemCancellationTokenSource != null)
        {
            if (!ItemCancellationTokenSource.IsCancellationRequested)
                ItemCancellationTokenSource.Cancel();
            ItemCancellationTokenSource.Dispose();
        }
        // +10 to ensure it's not cancelled before the task starts
        ItemCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(plugin.Config.HoverDelayIn100MS * 100 + 10));

        // start checking task
        var gameItem = new GameItem();

        // if in cache, return
        List<ulong> _cacheIds = GameItemCacheList.Select(i => i.Id).ToList();
        if (_cacheIds.Contains(itemId))
        {
            Service.PluginLog.Debug($"[PriceChecker] {itemId} found in cache.");
            gameItem = GameItemCacheList.Single(i => i.Id == itemId);
            plugin.MainWindow.CurrentItemUpdate(gameItem);
            return;
        }

        // create new game item object
        gameItem.Id = itemId;
        gameItem.IsHQ = isHQ;

        gameItem.InGame = plugin.ItemSheet.Single(i => i.RowId == (uint)gameItem.Id);

        // check if marketable
        if (gameItem.InGame.ItemSearchCategory.Row == 0)
        {
            Service.PluginLog.Debug($"[PriceChecker] {itemId} is unmarketable.");
            gameItem.Result = GameItemResult.Unmarketable;
            return;
        }

        // check if player in game
        gameItem.PlayerWorldId = Service.ClientState.LocalPlayer?.HomeWorld.Id ?? 0;
        if (gameItem.PlayerWorldId == 0)
        {
            Service.PluginLog.Debug($"[PriceChecker] {itemId} but unknown user world.");
            gameItem.Result = GameItemResult.UnknownUserWorld;
            return;
        }

        gameItem.VendorSelling = 0;
        var valid_vendors = Service.Data.Excel.GetSheet<GilShopItem>()?.Where(i => i.Item.Row == (uint)gameItem.Id).ToList();
        if (valid_vendors is { Count: > 0 })
        {
            gameItem.VendorSelling = gameItem.InGame.PriceMid;
        }

        // run price check
        if (plugin.Config.HoverDelayIn100MS > 0)
        {
            Task.Run(async () =>
            {
                await Task.Delay(plugin.Config.HoverDelayIn100MS * 100, ItemCancellationTokenSource!.Token).ConfigureAwait(false);
            });
        }
        Check(gameItem);
    }

    public void CheckRefreshAsync(GameItem gameItem)
    {
        Service.PluginLog.Debug($"[PriceChecker] Start CheckRefreshAsync: {gameItem.Id}, {gameItem.IsHQ}");

        Task.Run(() =>
        {
            try
            {
                CheckRefresh(gameItem);
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error($"[PriceChecker] CheckRefreshAsync failed, {ex.Message}");
                plugin.MainWindow.CurrentItemLabel = "Error";
            }
        });
    }

    public void CheckRefresh(GameItem gameItem)
    {
        Check(gameItem);
    }

    private void Check(GameItem gameItem)
    {
        plugin.MainWindow.LoadingQueue += 1;

        gameItem.Name = gameItem.InGame.Name.ToString();
        gameItem.TargetRegion = plugin.Config.selectedWorld;

        // lookup market data
        plugin.MainWindow.CurrentItemLabel = "Loading";
        plugin.MainWindow.CurrentItemIcon = Service.TextureProvider.GetIcon(gameItem.InGame.Icon)!;
        var UniversalisResponse = plugin.Universalis.GetDataAsync(gameItem).Result;

        // validate
        if (UniversalisResponse.Status == UniversalisResponseStatus.ServerError)
        {
            plugin.MainWindow.CurrentItemLabel = "API error";
            gameItem.Result = GameItemResult.APIError;
            Service.PluginLog.Warning($"[PriceChecker] ServerError, {gameItem.Id}.");
            plugin.MainWindow.LoadingQueue -= 1;
            return;
        }
        if (UniversalisResponse.Status == UniversalisResponseStatus.InvalidItemId)
        {
            plugin.MainWindow.CurrentItemLabel = "API error";
            gameItem.Result = GameItemResult.APIError;
            Service.PluginLog.Error($"[PriceChecker] InvalidItemId, {gameItem.Id}.");
            plugin.MainWindow.LoadingQueue -= 1;
            return;
        }
        if (UniversalisResponse.Status == UniversalisResponseStatus.UserCancellation)
        {
            plugin.MainWindow.CurrentItemLabel = "Timed out";
            gameItem.Result = GameItemResult.InGameError;
            Service.PluginLog.Debug($"[PriceChecker] UserCancellation, {gameItem.Id}.");
            plugin.MainWindow.LoadingQueue -= 1;
            return;
        }
        if (UniversalisResponse.Status == UniversalisResponseStatus.UnknownError)
        {
            plugin.MainWindow.CurrentItemLabel = "Error";
            gameItem.Result = GameItemResult.APIError;
            Service.PluginLog.Error($"[PriceChecker] UnknownError, {gameItem.Id}.");
            plugin.MainWindow.LoadingQueue -= 1;
            return;
        }

        // update game item
        // plugin.MainWindow.CurrentItemLabel = plugin.MainWindow.CurrentItem.InGame.Name.ToString();
        gameItem.Result = GameItemResult.Success;
        gameItem.FetchTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        gameItem.UniversalisResponse = UniversalisResponse;
        gameItem.WorldOutOfDate = gameItem.UniversalisResponse.WorldOutOfDate;


        // add avg price
        // it's not perfect but I'll take it for now
        gameItem.AvgPrice = gameItem.UniversalisResponse.AveragePrice;

        if (plugin.Config.EnableChatLog) SendChatMessage(gameItem);
        if (plugin.Config.EnableToastLog) SendToast(gameItem);

        // update the main window
        plugin.MainWindow.CurrentItemUpdate(gameItem);

        // inset into search history
        SearchHistoryUpdate(gameItem);

        plugin.MainWindow.LoadingQueue -= 1;
    }


    // -------------------------------- search history --------------------------------
    public void SearchHistoryUpdate(GameItem gameItem)
    {
        SearchHistoryClean();
        GameItemCacheList.RemoveAll(i => i.Id == gameItem.Id);
        GameItemCacheList.Insert(0, gameItem);
    }

    public void SearchHistoryClean()
    {
        Service.PluginLog.Debug($"[Cache] Items in cache {GameItemCacheList.Count}");

        if (GameItemCacheList.Count < plugin.Config.MaxCacheItems) return;

        if (plugin.Config.CleanCacheAsYouGo || (!plugin.Config.CleanCacheAsYouGo && !plugin.MainWindow.IsOpen))
        {
            GameItemCacheList.RemoveRange(
                plugin.Config.MaxCacheItems - 1,
                GameItemCacheList.Count - plugin.Config.MaxCacheItems + 1
            );
            Service.PluginLog.Debug($"[Cache] Cache cleaned. Current items in cache {GameItemCacheList.Count}");
        }
    }


    // -------------------------------- notification --------------------------------
    public enum PriceToPrint : uint
    {
        UniversalisAverage = 0,
        CurrentLow = 1,
        HistoricalLow = 2,
    }


    public void SendChatMessage(GameItem gameItem)
    {
        double price;
        if (plugin.Config.priceToPrint == PriceToPrint.CurrentLow)
        {
            price = gameItem.UniversalisResponse.Listings[0].PricePerUnit;
        }
        else if (plugin.Config.priceToPrint == PriceToPrint.HistoricalLow)
        {
            price = gameItem.UniversalisResponse.Entries.OrderBy(entry => entry.PricePerUnit).First().PricePerUnit;
        }
        else
        {
            price = gameItem.AvgPrice;
        }

        Miosuke.PrintMessage.Chat(
            plugin.Config.ChatLogChannel,
            $"[{plugin.NameShort}] ",
            557,
            new List<Payload>
            {
                new UIForegroundPayload(39),
                new ItemPayload((uint)gameItem.Id, gameItem.IsHQ),
                new TextPayload($"{(char)SeIconChar.LinkMarker} {gameItem.InGame.Name}"),
                RawPayload.LinkTerminator,
                new TextPayload($" [{gameItem.TargetRegion}] {(char)SeIconChar.Gil} {price:N0}"),
                new UIForegroundPayload(0)
            });
    }

    public void SendToast(GameItem gameItem)
    {
        Miosuke.PrintMessage.ToastNormal($"{gameItem.InGame.Name} [{gameItem.TargetRegion}] {(char)SeIconChar.Gil} {gameItem.AvgPrice:N0}");
    }
}
