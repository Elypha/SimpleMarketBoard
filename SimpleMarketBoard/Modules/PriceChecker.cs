using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text;
using Lumina.Excel.Sheets;
using Dalamud.Interface.ImGuiNotification;
using Miosuke.Messages;
using Dalamud.Interface.Textures;
using SimpleMarketBoard.Assets;
using SimpleMarketBoard.API;


namespace SimpleMarketBoard.Modules;

public class PriceChecker
{

    public PriceChecker()
    {
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
        public Item InGame { get; set; }
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
                P.MainWindow.LoadingQueue += 1;
                DoCheck(itemId);
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[PriceChecker] CheckNewAsync failed, {ex.Message}");
                P.MainWindow.CurrentItemLabel = "Error";
            }
            finally
            {
                P.MainWindow.LoadingQueue -= 1;
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
            P.MainWindow.CurrentItemUpdate(cached_gameItem);
            return;
        }

        // prepare game item for check
        var gameItem = new GameItem()
        {
            Id = itemId,
            InGame = Data.ItemSheet.Single(i => i.RowId == (uint)itemId),
            VendorSelling = 0,
        };
        gameItem.Name = gameItem.InGame.Name.ToString();

        // if marketable
        if (gameItem.InGame.ItemSearchCategory.RowId == 0)
        {
            Service.NotificationManager.AddNotification(new Notification
            {
                Content = $"{gameItem.Name} [{gameItem.Id}] is unmarketable.",
                Type = NotificationType.Warning,
            });
            return;
        }

        // if player in game
        gameItem.PlayerWorldId = Service.ClientState.LocalPlayer?.HomeWorld.RowId ?? 0;
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
        var valid_vendors = Service.Data.GetSubrowExcelSheet<GilShopItem>().Flatten().Where(i => i.Item.RowId == (uint)gameItem.Id).ToList();
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
                P.MainWindow.LoadingQueue += 1;
                CheckGameItem(gameItem);
            }
            catch (Exception ex)
            {
                Service.Log.Error($"[PriceChecker] CheckRefreshAsync failed, {ex.Message}");
                P.MainWindow.CurrentItemLabel = "Error";
            }
            finally
            {
                P.MainWindow.LoadingQueue -= 1;
            }
        });
    }


    private void CheckGameItem(GameItem gameItem)
    {
        // lookup market data
        P.MainWindow.CurrentItemLabel = gameItem.Name;
        P.MainWindow.CurrentItemIcon = Service.Texture.GetFromGameIcon(new GameIconLookup(gameItem.InGame.Icon))!;
        gameItem.TargetRegion = P.Config.selectedWorld;
        gameItem.FetchTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var UniversalisResponse = P.Universalis.GetDataAsync(gameItem).Result;

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

            if (P.Config.EnableChatLog) SendChatMessage(gameItem);
            if (P.Config.EnableToastLog) SendToast(gameItem);
        }

        // update the main window
        P.MainWindow.CurrentItemUpdate(gameItem);

        // inset into search history
        SearchHistoryUpdate(gameItem);

        P.MainWindow.LoadingQueue -= 1;
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

        if (GameItemCacheList.Count < P.Config.MaxCacheItems) return;

        if (P.Config.CleanCacheASAP || !P.Config.CleanCacheASAP && !P.MainWindow.IsOpen)
        {
            GameItemCacheList.RemoveRange(
                P.Config.MaxCacheItems - 1,
                GameItemCacheList.Count - P.Config.MaxCacheItems + 1
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
        if (P.Config.priceToPrint == PriceToPrint.SellingLow)
        {
            price = gameItem.UniversalisResponse.Listings[0].PricePerUnit;
        }
        else if (P.Config.priceToPrint == PriceToPrint.SoldLow)
        {
            price = gameItem.UniversalisResponse.Entries.OrderBy(entry => entry.PricePerUnit).First().PricePerUnit;
        }
        else
        {
            price = gameItem.AvgPrice;
        }

        Chat.PluginMessage(
            P.Config.ChatLogChannel,
            $"[{NameShort}]",
            [
                new TextPayload($" [{gameItem.TargetRegion}]"),
                new UIForegroundPayload(39),
                new ItemPayload((uint)gameItem.Id),
                new TextPayload($"{(char)SeIconChar.LinkMarker} {gameItem.InGame.Name}"),
                RawPayload.LinkTerminator,
                new TextPayload($": {price:N0} {(char)SeIconChar.Gil}"),
                new UIForegroundPayload(0)
            ],
            P.PluginPayload);
    }

    public void SendToast(GameItem gameItem)
    {
        Toast.Normal(
            $"[{gameItem.TargetRegion}] {gameItem.InGame.Name}: {gameItem.AvgPrice:N0} {(char)SeIconChar.Gil}",
            Dalamud.Game.Gui.Toast.ToastPosition.Bottom);
    }
}
