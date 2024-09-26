using System;
using System.Threading;
using System.Threading.Tasks;
using Miosuke;

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
    public ulong HoverItemId = 0;
    public ulong SavedItemId = 0;
    public ulong LastCheckItemId = 0;
    public CancellationTokenSource? ItemCts;

    private void OnHoveredItemChanged(object? sender, ulong thisItemId)
    {
        HoverItemId = thisItemId % 1000000;

        // cancel existing request
        if (ItemCts is not null && !ItemCts.IsCancellationRequested) ItemCts.Cancel();
        // reset if not hovering
        if (HoverItemId == 0) { SavedItemId = 0; return; }

        if (plugin.Config.SearchHotkeyEnabled)
        {
            SavedItemId = HoverItemId;
        }
        else
        {
            if (!plugin.Config.HoverBackgroundSearchEnabled && !plugin.MainWindow.IsOpen) return;
            Service.Log.Debug($"Check {HoverItemId} {ItemCts?.IsCancellationRequested}");
            CheckItem(HoverItemId);
        }
    }

    public void CheckItem(ulong itemId)
    {
        // cancel existing request
        if (ItemCts is not null && !ItemCts.IsCancellationRequested) ItemCts.Cancel();

        ItemCts = new CancellationTokenSource();
        CheckItemAsync(itemId, ItemCts.Token);
    }

    private async void CheckItemAsync(ulong itemId, CancellationToken token)
    {
        // wait for hover delay, raise TaskCanceledException if cancelled
        try
        {
            await Task.Delay(plugin.Config.HoverDelayMs, token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            Service.Log.Debug($"Cancel {itemId} {SavedItemId}");
            return;
        }

        // show window if needed before search
        if (plugin.Config.ShowWindowOnSearch && !plugin.MainWindow.IsOpen)
        {
            plugin.MainWindow.IsOpen = true;
        }

        // check new item
        plugin.PriceChecker.DoCheckAsync(itemId);

        // clean up
        SavedItemId = 0;
        LastCheckItemId = itemId;
    }

}
