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

        public PriceChecker(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void CheckAsync(ulong itemId, bool isHQ, bool cleanCache = true)
        {
            try
            {
                Service.PluginLog.Debug($"Check: {itemId}, {isHQ}");

                // cancel in-flight request
                if (plugin.ItemCancellationTokenSource != null)
                {
                    if (!plugin.ItemCancellationTokenSource.IsCancellationRequested)
                        plugin.ItemCancellationTokenSource.Cancel();
                    plugin.ItemCancellationTokenSource.Dispose();
                }

                // create new cancel token
                plugin.ItemCancellationTokenSource = new CancellationTokenSource(plugin.Config.RequestTimeoutMS * 2);

                List<ulong> SearchHistoryIds = plugin.GameItemCacheList.Select(i => i.Id).ToList();
                if (SearchHistoryIds.Contains(itemId))
                {
                    Service.PluginLog.Debug($"Check: {itemId} found in cache.");
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
                gameItem.FetchTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                gameItem.Name = gameItem.InGame.Name.ToString();


                // run price check
                Task.Run(async () =>
                {
                    await Task.Delay(plugin.Config.HoverDelayMS, plugin.ItemCancellationTokenSource!.Token).ConfigureAwait(false);
                    await Task.Run(() => Check(true));
                });
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error(ex, "ProcessItemAsync failed.");
                plugin.ItemCancellationTokenSource = null;
                plugin.HoveredItem.ResetItemData();
            }
        }

        public void CheckAsyncRefresh()
        {
            gameItem.FetchTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Task.Run(async () =>
            {
                await Task.Run(() => Check(true));
            });
        }

        private void Check(bool cleanCache = true)
        {
            // lookup market data
            gameItem.UniversalisResponse = plugin.Universalis.CheckPrice().Result;

            // validate
            if (plugin.PriceChecker.gameItem.Id != plugin.PriceChecker.gameItem.UniversalisResponse.ItemId)
            {
                gameItem.Result = Plugin.GameItemResult.APIError;
                Service.PluginLog.Error($"CheckPrice failed, {gameItem.Id}.");
                return;
            }
            gameItem.Result = Plugin.GameItemResult.Success;

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
                new UIForegroundPayload(25),
                new ItemPayload((uint)gameItem.Id, gameItem.IsHQ),
                new TextPayload($"{(char)SeIconChar.LinkMarker}"),
                new TextPayload(" " + gameItem.Name),
                RawPayload.LinkTerminator,
                new TextPayload(" " + (char)SeIconChar.ArrowRight + " " + gameItem.AvgPrice.ToString("N0", CultureInfo.InvariantCulture)),
                new UIForegroundPayload(0)
            });
        }

        public void SendToast(Plugin.GameItem gameItem)
        {
            plugin.PrintMessage.PrintMessageToast($"{gameItem.Name} {(char)SeIconChar.ArrowRight} {gameItem.AvgPrice:N0}");
        }
    }
}
