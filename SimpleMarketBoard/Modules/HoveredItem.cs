using System.Threading;

namespace SimpleMarketBoard.Modules;

public class HoveredItem
{

    public HoveredItem()
    {
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

        if (P.Config.SearchHotkeyEnabled)
        {
            SavedItemId = HoverItemId;
        }
        else
        {
            if (!P.Config.HoverBackgroundSearchEnabled && !P.MainWindow.IsOpen) return;
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
            await Task.Delay(P.Config.HoverDelayMs, token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            Service.Log.Debug($"Cancel {itemId} {SavedItemId}");
            return;
        }

        // show window if needed before search
        if (P.Config.ShowWindowOnSearch && !P.MainWindow.IsOpen)
        {
            P.MainWindow.IsOpen = true;
        }

        // check new item
        P.PriceChecker.DoCheckAsync(itemId);

        // clean up
        SavedItemId = 0;
        LastCheckItemId = itemId;
    }

}
