using Dalamud.Configuration;
using Dalamud.ContextMenu;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using Dalamud.Interface.Colors;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using SimpleMarketBoard.UniversalisModels;
using FFXIVClientStructs.Havok;

namespace SimpleMarketBoard;

public class PriceChecker
{

    private readonly Plugin plugin;

    public CancellationTokenSource? ItemCancellationTokenSource;

    public PriceChecker(Plugin plugin)
    {
        this.plugin = plugin;
    }

    public void Dispose()
    {
        ItemCancellationTokenSource?.Dispose();
    }

    public void CheckNewAsync(ulong itemId, bool isHQ)
    {
        Task.Run(() => CheckNew(itemId, isHQ));
    }

    public void CheckNew(ulong itemId, bool isHQ)
    {
        try
        {
            Service.PluginLog.Debug($"[Checker] Check: {itemId}, {isHQ}");

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
            var gameItem = new Plugin.GameItem();

            // if in cache, return
            List<ulong> _cacheIds = plugin.GameItemCacheList.Select(i => i.Id).ToList();
            if (_cacheIds.Contains(itemId))
            {
                Service.PluginLog.Debug($"[Checker] {itemId} found in cache.");
                gameItem = plugin.GameItemCacheList.Single(i => i.Id == itemId);
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
                Service.PluginLog.Debug($"[Checker] {itemId} is unmarketable.");
                gameItem.Result = Plugin.GameItemResult.Unmarketable;
                return;
            }

            // check if player in game
            gameItem.PlayerWorldId = Service.ClientState.LocalPlayer?.HomeWorld.Id ?? 0;
            if (gameItem.PlayerWorldId == 0)
            {
                Service.PluginLog.Debug($"[Checker] Unknown user world.");
                gameItem.Result = Plugin.GameItemResult.UnknownUserWorld;
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
        catch (Exception ex)
        {
            Service.PluginLog.Error($"[Checker] Check failed, {ex.Message}");
            plugin.MainWindow.CurrentItem.Name = "Plugin error";
            ItemCancellationTokenSource = null;
            plugin.HoveredItem.ResetLastItem();
        }
    }

    public void CheckRefreshAsync(Plugin.GameItem gameItem)
    {
        Task.Run(() => CheckRefresh(gameItem));
    }

    public void CheckRefresh(Plugin.GameItem gameItem)
    {
        Check(gameItem);
    }

    private void Check(Plugin.GameItem gameItem)
    {
        gameItem.Name = gameItem.InGame.Name.ToString();
        gameItem.TargetRegion = plugin.Config.selectedWorld;

        // lookup market data
        plugin.MainWindow.CurrentItem.Name = "Loading...";
        plugin.MainWindow.LoadingQueue += 1;
        plugin.MainWindow.CurrentItemIcon = Service.TextureProvider.GetIcon(gameItem.InGame.Icon)!;
        var UniversalisResponse = plugin.Universalis.GetDataAsync(gameItem).Result;

        // validate
        if (UniversalisResponse.ItemId == 0)
        {
            plugin.MainWindow.CurrentItem.Name = "Timed out";
            gameItem.Result = Plugin.GameItemResult.APIError;
            Service.PluginLog.Warning($"[Check] Timed out, {gameItem.Id}.");
            return;
        }
        if (gameItem.Id != UniversalisResponse.ItemId)
        {
            plugin.MainWindow.CurrentItem.Name = "API error";
            gameItem.Result = Plugin.GameItemResult.APIError;
            Service.PluginLog.Error($"[Check] API error, {gameItem.Id}.");
            return;
        }

        // update game item
        gameItem.Result = Plugin.GameItemResult.Success;
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
        plugin.SearchHistoryUpdate(gameItem);

        plugin.MainWindow.LoadingQueue -= 1;
    }

    public void SendChatMessage(Plugin.GameItem gameItem)
    {
        plugin.PrintMessage.PrintMessageChat(new List<Payload>
            {
                new UIForegroundPayload(39),
                new ItemPayload((uint)gameItem.Id, gameItem.IsHQ),
                new TextPayload($"{(char)SeIconChar.LinkMarker} {gameItem.InGame.Name}"),
                RawPayload.LinkTerminator,
                new TextPayload($" [{gameItem.TargetRegion}] {(char)SeIconChar.Gil} {gameItem.AvgPrice:N0}"),
                new UIForegroundPayload(0)
            });
    }

    public void SendToast(Plugin.GameItem gameItem)
    {
        plugin.PrintMessage.PrintMessageToast($"{gameItem.InGame.Name} [{gameItem.TargetRegion}] {(char)SeIconChar.Gil} {gameItem.AvgPrice:N0}");
    }
}
