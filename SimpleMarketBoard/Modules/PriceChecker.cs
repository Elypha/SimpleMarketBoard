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

namespace SimpleMarketBoard
{

    public class PriceChecker
    {

        private readonly Plugin plugin;
        public Plugin.GameItem gameItem = null!;

        public CancellationTokenSource? ItemCancellationTokenSource;

        public PriceChecker(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Dispose()
        {
            ItemCancellationTokenSource?.Dispose();
        }

        public void CheckAsync(ulong itemId, bool isHQ, bool cleanCache = true)
        {
            try
            {
                Service.PluginLog.Debug($"[Checker] Check: {itemId}, {isHQ}");

                // cancel in-flight request
                if (ItemCancellationTokenSource != null)
                {
                    if (!ItemCancellationTokenSource.IsCancellationRequested)
                        ItemCancellationTokenSource.Cancel();
                    ItemCancellationTokenSource.Dispose();
                }

                // create new cancel token
                // +10 to ensure it's not cancelled before the task starts
                ItemCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(plugin.Config.HoverDelayIn100MS * 100 + 10));

                List<ulong> SearchHistoryIds = plugin.GameItemCacheList.Select(i => i.Id).ToList();
                if (SearchHistoryIds.Contains(itemId))
                {
                    Service.PluginLog.Debug($"[Checker] {itemId} found in cache.");
                    gameItem = plugin.GameItemCacheList.Single(i => i.Id == itemId);
                    plugin.MainWindow.CurrentItemUpdate(gameItem);
                    // plugin.SearchHistoryUpdate(gameItem, cleanCache);
                    return;
                }

                // create new game item object
                gameItem = new Plugin.GameItem();
                gameItem.Id = itemId;
                gameItem.InGame = plugin.ItemSheet.Single(i => i.RowId == (uint)gameItem.Id);

                // check if marketable
                if (gameItem.InGame.ItemSearchCategory.Row == 0)
                {
                    gameItem.Result = Plugin.GameItemResult.Unmarketable;
                    return;
                }

                // check if in game
                gameItem.PlayerWorldId = Service.ClientState.LocalPlayer?.HomeWorld.Id ?? 0;
                if (gameItem.PlayerWorldId == 0)
                {
                    gameItem.Result = Plugin.GameItemResult.UnknownUserWorld;
                    return;
                }

                gameItem.IsHQ = isHQ;

                gameItem.Name = gameItem.InGame.Name.ToString();


                // run price check
                Task.Run(async () =>
                {
                    await Task.Delay(plugin.Config.HoverDelayIn100MS * 100, ItemCancellationTokenSource!.Token).ConfigureAwait(false);
                    await Task.Run(() => Check(true));
                });
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error($"[Checker] Check failed, {ex.Message}");
                plugin.MainWindow.CurrentItem.Name = "Please try again";
                ItemCancellationTokenSource = null;
                plugin.HoveredItem.ResetItemData();
            }
        }

        public void CheckAsyncRefresh()
        {
            Task.Run(async () =>
            {
                await Task.Run(() => Check(true));
            });
        }

        private void Check(bool cleanCache = true)
        {
            // lookup market data
            plugin.MainWindow.CurrentItem.Name = "Loading...";
            plugin.MainWindow.CurrentItemIcon = Service.TextureProvider.GetIcon(gameItem.InGame.Icon)!;
            var UniversalisResponse = plugin.Universalis.CheckPriceAsync().Result;

            // validate
            if (UniversalisResponse.ItemId == 0)
            {
                plugin.MainWindow.CurrentItem.Name = "Timed out";
                gameItem.Result = Plugin.GameItemResult.APIError;
                Service.PluginLog.Warning($"[Check] Timed out, {gameItem.Id}.");
                return;
            }
            if (plugin.PriceChecker.gameItem.Id != UniversalisResponse.ItemId)
            {
                gameItem.Result = Plugin.GameItemResult.APIError;
                Service.PluginLog.Error($"[Check] API error, {gameItem.Id}.");
                return;
            }

            // update game item
            gameItem.Result = Plugin.GameItemResult.Success;
            gameItem.UniversalisResponse = UniversalisResponse;
            gameItem.FetchTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();


            // add avg price
            // it's not perfect but I'll take it for now
            gameItem.AvgPrice = gameItem.UniversalisResponse.AveragePrice;

            if (plugin.Config.EnableChatLog) SendChatMessage(gameItem);
            if (plugin.Config.EnableToastLog) SendToast(gameItem);

            // update the main window
            plugin.MainWindow.CurrentItemUpdate(gameItem);

            // inset into search history
            plugin.SearchHistoryUpdate(gameItem, cleanCache);
        }

        public void SendChatMessage(Plugin.GameItem gameItem)
        {
            plugin.PrintMessage.PrintMessageChat(new List<Payload>
            {
                new UIForegroundPayload(39),
                new ItemPayload((uint)gameItem.Id, gameItem.IsHQ),
                new TextPayload($"{(char)SeIconChar.LinkMarker} {gameItem.InGame.Name}"),
                RawPayload.LinkTerminator,
                new TextPayload($" [{gameItem.UniversalisResponse.RegionName}] {(char)SeIconChar.Gil} {gameItem.AvgPrice:N0}"),
                new UIForegroundPayload(0)
            });
        }

        public void SendToast(Plugin.GameItem gameItem)
        {
            plugin.PrintMessage.PrintMessageToast($"{gameItem.InGame.Name} [{gameItem.UniversalisResponse.RegionName}] {(char)SeIconChar.Gil} {gameItem.AvgPrice:N0}");
        }
    }
}
