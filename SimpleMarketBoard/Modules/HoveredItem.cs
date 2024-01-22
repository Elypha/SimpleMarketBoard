using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;


namespace SimpleMarketBoard
{
    public class HoveredItem
    {
        public ulong LastItemId;
        public bool LastItemIsHQ;

        private Plugin plugin;

        public HoveredItem(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Enable()
        {
            Service.GameGui.HoveredItemChanged += OnHoveredItemChanged;
        }

        public void ResetItemData()
        {
            LastItemId = 0;
            LastItemIsHQ = false;
        }

        public void Dispose()
        {
            Service.GameGui.HoveredItemChanged -= OnHoveredItemChanged;
        }

        private void OnHoveredItemChanged(object? sender, ulong thisItemId)
        {
            try
            {
                // cancel in-flight request
                if (plugin.ItemCancellationTokenSource != null)
                {
                    if (!plugin.ItemCancellationTokenSource.IsCancellationRequested)
                        plugin.ItemCancellationTokenSource.Cancel();
                    plugin.ItemCancellationTokenSource.Dispose();
                }

                // stop if invalid itemId
                if (thisItemId == 0)
                {
                    ResetItemData();
                    plugin.ItemCancellationTokenSource = null;
                    return;
                };

                // capture itemId/quality
                ulong realItemId;
                bool itemIsHQ;
                if (thisItemId >= 1000000)
                {
                    realItemId = thisItemId - 1000000;
                    itemIsHQ = true;
                }
                else
                {
                    realItemId = thisItemId;
                    itemIsHQ = false;
                }

                var rowid = plugin.ItemSheet.GetRow((uint)realItemId);
                Service.PluginLog.Information($"hovered Id/realId/RouId: {thisItemId}/{realItemId}/{rowid?.RowId}");

                // check if keybinding is pressed
                bool isKeybindingPressed = plugin.PluginHotkey.CheckHotkeyState(plugin.Config.BindingHotkey);

                if (plugin.Config.KeybindingEnabled)
                {
                    if (plugin.Config.AllowKeybindingAfterHover)
                    {
                        if (isKeybindingPressed)
                        {
                            // call immediately
                            Service.PluginLog.Information($"checker A: {realItemId}, {itemIsHQ}");
                            plugin.PriceChecker.CheckAsync(realItemId, itemIsHQ);
                            return;
                        }
                        else
                        {
                            // save for next keybinding press
                            LastItemId = realItemId;
                            LastItemIsHQ = itemIsHQ;
                        }
                    }
                    else
                    {
                        if (!isKeybindingPressed) return;
                        Service.PluginLog.Information($"checker B: {realItemId}, {itemIsHQ}");
                        plugin.PriceChecker.CheckAsync(realItemId, itemIsHQ);
                        return;
                    }
                }
                else
                {
                    Service.PluginLog.Information($"checker C: {realItemId}, {itemIsHQ}");
                    plugin.PriceChecker.CheckAsync(realItemId, itemIsHQ);
                }
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error(ex, "Failed to price check.");
                ResetItemData();
                plugin.ItemCancellationTokenSource = null;
            }
        }

        public void CheckAsyncLastItem()
        {
            if (plugin.Config.KeybindingEnabled && plugin.Config.AllowKeybindingAfterHover && plugin.PluginHotkey.CheckHotkeyState(plugin.Config.BindingHotkey))
            {
                if (LastItemId != 0)
                {
                    Service.PluginLog.Information($"checker rn: {LastItemId}, {LastItemIsHQ}");
                    plugin.PriceChecker.CheckAsync(LastItemId, LastItemIsHQ);
                    LastItemId = 0;
                }
            }
        }
    }
}
