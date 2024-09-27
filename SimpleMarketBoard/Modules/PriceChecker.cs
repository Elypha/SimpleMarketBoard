using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Dalamud.Interface.ImGuiNotification;
using SimpleMarketBoard.UniversalisModels;
using Miosuke;
using Miosuke.Messages;
using Dalamud.Interface.Textures;


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
    }


    // -------------------------------- game item --------------------------------
    public List<GameItem> GameItemCacheList = [];

    public class GameItem
    {
        public ulong Id { get; set; }
        public string Name { get; set; } = "";
        public string TargetRegion { get; set; } = "";
        public ulong PlayerWorldId { get; set; }
        public uint VendorSelling { get; set; }
        public Item InGame { get; set; } = null!;
        public ulong FetchTimestamp { get; set; }
        public UniversalisResponse UniversalisResponse { get; set; } = new UniversalisResponse();
        public Dictionary<string, long> WorldOutOfDate { get; set; } = [];
        public double AvgPrice { get; set; }
    }


    // -------------------------------- price checker --------------------------------
    public void DoCheckAsync(ulong itemId)
    {
        Service.Log.Debug($"[PriceChecker] Start CheckNewAsync: {itemId}");

        Task.Run(() =>
        {
            try
            {
                DoCheck(itemId);
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[PriceChecker] CheckNewAsync failed, {ex.Message}");
                plugin.MainWindow.CurrentItemLabel = "Error";
            }
        });
    }

    public void DoCheck(ulong itemId)
    {
        // if in cache, return
        var _cacheIds = GameItemCacheList.Select(i => i.Id).ToList();
        if (_cacheIds.Contains(itemId))
        {
            Service.Log.Debug($"[PriceChecker] {itemId} found in cache.");
            var cached_gameItem = GameItemCacheList.Single(i => i.Id == itemId);
            plugin.MainWindow.CurrentItemUpdate(cached_gameItem);
            return;
        }

        // prepare game item for check
        var gameItem = new GameItem()
        {
            Id = itemId,
            InGame = plugin.ItemSheet.Single(i => i.RowId == (uint)itemId),
            VendorSelling = 0,
        };
        gameItem.Name = gameItem.InGame.Name.ToString();

        // if marketable
        if (gameItem.InGame.ItemSearchCategory.Row == 0)
        {
            Service.NotificationManager.AddNotification(new Notification
            {
                Content = $"{gameItem.Name} [{gameItem.Id}] is unmarketable.",
                Type = NotificationType.Warning,
            });
            return;
        }

        // if player in game
        gameItem.PlayerWorldId = Service.ClientState.LocalPlayer?.HomeWorld.Id ?? 0;
        if (gameItem.PlayerWorldId == 0)
        {
            Service.NotificationManager.AddNotification(new Notification
            {
                Content = $"Player World ID is unknown.",
                Type = NotificationType.Warning,
            });
            return;
        }

        // if vendor sells cheaper
        var valid_vendors = Service.Data.Excel.GetSheet<GilShopItem>()?.Where(i => i.Item.Row == (uint)gameItem.Id).ToList();
        if (valid_vendors is { Count: > 0 })
        {
            gameItem.VendorSelling = gameItem.InGame.PriceMid;
        }

        CheckGameItem(gameItem);
    }

    public void DoCheckRefreshAsync(GameItem gameItem)
    {
        Service.Log.Debug($"[PriceChecker] Start CheckRefreshAsync: {gameItem.Id}");

        Task.Run(() =>
        {
            try
            {
                CheckGameItem(gameItem);
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[PriceChecker] CheckRefreshAsync failed, {ex.Message}");
                plugin.MainWindow.CurrentItemLabel = "Error";
            }
        });
    }


    private void CheckGameItem(GameItem gameItem)
    {
        // lookup market data
        plugin.MainWindow.LoadingQueue += 1;
        plugin.MainWindow.CurrentItemLabel = gameItem.Name;
        plugin.MainWindow.CurrentItemIcon = Service.Texture.GetFromGameIcon(new GameIconLookup(gameItem.InGame.Icon))!;
        gameItem.TargetRegion = plugin.Config.selectedWorld;
        gameItem.FetchTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var UniversalisResponse = plugin.Universalis.GetDataAsync(gameItem).Result;

        // validate
        if (UniversalisResponse.Status == UniversalisResponseStatus.ServerError)
        {
            Service.NotificationManager.AddNotification(new Notification
            {
                Content = $"API server error for {gameItem.Name} [{gameItem.Id}].",
                Type = NotificationType.Warning,
            });
        }
        else if (UniversalisResponse.Status == UniversalisResponseStatus.InvalidData)
        {
            Service.NotificationManager.AddNotification(new Notification
            {
                Content = $"Invalid data received for {gameItem.Name} [{gameItem.Id}].",
                Type = NotificationType.Warning,
            });
        }
        else if (UniversalisResponse.Status == UniversalisResponseStatus.UserCancellation)
        {
            Service.NotificationManager.AddNotification(new Notification
            {
                Content = $"Request cancelled (timeout) for {gameItem.Name} [{gameItem.Id}].",
                Type = NotificationType.Warning,
            });
        }
        else if (UniversalisResponse.Status == UniversalisResponseStatus.UnknownError)
        {
            Service.NotificationManager.AddNotification(new Notification
            {
                Content = $"Unknown error for {gameItem.Name} [{gameItem.Id}].",
                Type = NotificationType.Warning,
            });
        }
        else
        {
            gameItem.UniversalisResponse = UniversalisResponse;
            gameItem.WorldOutOfDate = gameItem.UniversalisResponse.WorldOutOfDate;

            // add avg price
            // it's not perfect but I'll take it for now
            gameItem.AvgPrice = gameItem.UniversalisResponse.AveragePrice;

            if (plugin.Config.EnableChatLog) SendChatMessage(gameItem);
            if (plugin.Config.EnableToastLog) SendToast(gameItem);
        }

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
        Service.Log.Debug($"[Cache] Items in cache {GameItemCacheList.Count}");

        if (GameItemCacheList.Count < plugin.Config.MaxCacheItems) return;

        if (plugin.Config.CleanCacheASAP || (!plugin.Config.CleanCacheASAP && !plugin.MainWindow.IsOpen))
        {
            GameItemCacheList.RemoveRange(
                plugin.Config.MaxCacheItems - 1,
                GameItemCacheList.Count - plugin.Config.MaxCacheItems + 1
            );
            Service.Log.Debug($"[Cache] Cache cleaned. Current items in cache {GameItemCacheList.Count}");
        }
    }


    // -------------------------------- notification --------------------------------
    public enum PriceToPrint : uint
    {
        UniversalisAverage = 0,
        SellingLow = 1,
        SoldLow = 2,
    }


    public void SendChatMessage(GameItem gameItem)
    {
        double price;
        if (plugin.Config.priceToPrint == PriceToPrint.SellingLow)
        {
            price = gameItem.UniversalisResponse.Listings[0].PricePerUnit;
        }
        else if (plugin.Config.priceToPrint == PriceToPrint.SoldLow)
        {
            price = gameItem.UniversalisResponse.Entries.OrderBy(entry => entry.PricePerUnit).First().PricePerUnit;
        }
        else
        {
            price = gameItem.AvgPrice;
        }

        Chat.PluginMessage(
            plugin.Config.ChatLogChannel,
            $"[{plugin.NameShort}]",
            [
                new TextPayload($" [{gameItem.TargetRegion}]"),
                new UIForegroundPayload(39),
                new ItemPayload((uint)gameItem.Id),
                new TextPayload($"{(char)SeIconChar.LinkMarker} {gameItem.InGame.Name}"),
                RawPayload.LinkTerminator,
                new TextPayload($": {price:N0} {(char)SeIconChar.Gil}"),
                new UIForegroundPayload(0)
            ],
            plugin.PluginPayload);
    }

    public void SendToast(GameItem gameItem)
    {
        Toast.Normal(
            $"[{gameItem.TargetRegion}] {gameItem.InGame.Name}: {gameItem.AvgPrice:N0} {(char)SeIconChar.Gil}",
            Dalamud.Game.Gui.Toast.ToastPosition.Bottom);
    }
}
