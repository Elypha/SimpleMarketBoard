using System;


namespace SimpleMarketBoard;

public class HoveredItem
{
    private readonly Plugin plugin;

    public HoveredItem(Plugin plugin)
    {
        this.plugin = plugin;
        Service.GameGui.HoveredItemChanged += OnHoveredItemChanged;
    }

    public void Dispose()
    {
        Service.GameGui.HoveredItemChanged -= OnHoveredItemChanged;
    }


    // -------------------------------- hovered item --------------------------------
    public ulong HoverItemId;
    public ulong LastItemId;
    public bool LastItemIsHQ;

    private void OnHoveredItemChanged(object? sender, ulong thisItemId)
    {
        try
        {
            HoverItemId = thisItemId;

            // cancel in-flight request
            if (plugin.PriceChecker.ItemCancellationTokenSource != null)
            {
                if (!plugin.PriceChecker.ItemCancellationTokenSource.IsCancellationRequested)
                    plugin.PriceChecker.ItemCancellationTokenSource.Cancel();
                plugin.PriceChecker.ItemCancellationTokenSource.Dispose();
            }

            // stop if invalid itemId
            if (thisItemId == 0)
            {
                ResetLastItem();
                plugin.PriceChecker.ItemCancellationTokenSource = null;
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
            Service.PluginLog.Verbose($"[UI] ItemID, RealID, RowID: | {thisItemId,7} | {realItemId,7} | {rowid?.RowId,7} |");

            // check if keybinding is pressed
            var isKeybindingPressed = plugin.PluginHotkey.CheckHotkeyState(plugin.Config.BindingHotkey);

            if (plugin.Config.KeybindingEnabled)
            {
                if (plugin.Config.AllowKeybindingAfterHover)
                {
                    if (isKeybindingPressed)
                    {
                        // call immediately
                        Service.PluginLog.Debug($"[UI] (A) Check by keybinding after hover: {realItemId}, {itemIsHQ}");
                        plugin.PriceChecker.CheckNewAsync(realItemId, itemIsHQ);
                        return;
                    }
                    else
                    {
                        // save for next keybinding press
                        Service.PluginLog.Verbose($"[UI] Save for keybinding after hover: {realItemId}, {itemIsHQ}");
                        LastItemId = realItemId;
                        LastItemIsHQ = itemIsHQ;
                    }
                }
                else
                {
                    if (!isKeybindingPressed) return;
                    Service.PluginLog.Debug($"[UI] (B) Check by hover after keybinding: {realItemId}, {itemIsHQ}");
                    plugin.PriceChecker.CheckNewAsync(realItemId, itemIsHQ);
                    return;
                }
            }
            else
            {
                Service.PluginLog.Debug($"[UI] (C) Check by hover without keybinding: {realItemId}, {itemIsHQ}");
                plugin.PriceChecker.CheckNewAsync(realItemId, itemIsHQ);
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error($"[UI] Failed to do price check, {ex.Message}");
            ResetLastItem();
            plugin.PriceChecker.ItemCancellationTokenSource = null;
        }
    }

    public void CheckLastItem()
    {
        if (plugin.Config.KeybindingEnabled && plugin.Config.AllowKeybindingAfterHover && plugin.PluginHotkey.CheckHotkeyState(plugin.Config.BindingHotkey))
        {
            if (LastItemId != 0)
            {
                Service.PluginLog.Verbose($"[UI] (D) Check by keybinding after hover for the current item: {LastItemId}, {LastItemIsHQ}");
                plugin.PriceChecker.CheckNewAsync(LastItemId, LastItemIsHQ);
                ResetLastItem();
            }
        }
    }

    public void ResetLastItem()
    {
        LastItemId = 0;
        LastItemIsHQ = false;
    }
}
