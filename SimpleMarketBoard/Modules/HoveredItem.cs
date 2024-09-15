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
            if (thisItemId >= 1000000)
            {
                realItemId = thisItemId - 1000000;
            }
            else
            {
                realItemId = thisItemId;
            }

            var rowid = plugin.ItemSheet.GetRow((uint)realItemId);
            Service.PluginLog.Verbose($"[UI] ItemID, RealID, RowID: | {thisItemId,7} | {realItemId,7} | {rowid?.RowId,7} |");

            // check if keybinding is pressed
            var isKeybindingPressed = Miosuke.Hotkey.IsActive(plugin.Config.SearchHotkey, !plugin.Config.SearchHotkeyLoose);

            if (plugin.Config.SearchHotkeyEnabled)
            {
                if (plugin.Config.SearchHotkeyAfterHover)
                {
                    if (isKeybindingPressed)
                    {
                        // call immediately
                        Service.PluginLog.Debug($"[UI] (A) Check by keybinding after hover: {realItemId}");
                        plugin.PriceChecker.CheckNewAsync(realItemId);
                        return;
                    }
                    else
                    {
                        // save for next keybinding press
                        Service.PluginLog.Verbose($"[UI] Save for keybinding after hover: {realItemId}");
                        LastItemId = realItemId;
                    }
                }
                else
                {
                    if (!isKeybindingPressed) return;
                    Service.PluginLog.Debug($"[UI] (B) Check by hover after keybinding: {realItemId}");
                    plugin.PriceChecker.CheckNewAsync(realItemId);
                    return;
                }
            }
            else
            {
                Service.PluginLog.Debug($"[UI] (C) Check by hover without keybinding: {realItemId}");
                plugin.PriceChecker.CheckNewAsync(realItemId);
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
        if (plugin.Config.SearchHotkeyEnabled && plugin.Config.SearchHotkeyAfterHover && Miosuke.Hotkey.IsActive(plugin.Config.SearchHotkey, !plugin.Config.SearchHotkeyLoose))
        {
            if (LastItemId != 0)
            {
                Service.PluginLog.Verbose($"[UI] (D) Check by keybinding after hover for the current item: {LastItemId}");
                plugin.PriceChecker.CheckNewAsync(LastItemId);
                ResetLastItem();
            }
        }
    }

    public void ResetLastItem()
    {
        LastItemId = 0;
    }
}
