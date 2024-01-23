using System;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using ImGuiNET;

using Dalamud.Interface;
using System.Linq;
using System.Globalization;

namespace SimpleMarketBoard;

public class MainWindow : Window, IDisposable
{
    // private IDalamudTextureWrap goatImage;
    private Plugin plugin;
    public ulong LastItemId = 0;
    public Plugin.GameItem CurrentItem { get; set; } = null!;
    public IDalamudTextureWrap CurrentItemIcon = null!;

    public void CurrentItemUpdate(Plugin.GameItem gameItem)
    {
        LastItemId = CurrentItem?.Id ?? 0;
        CurrentItem = gameItem;
        // CurrentItemIcon?.Dispose();
        CurrentItemIcon = Service.TextureProvider.GetIcon(CurrentItem.InGame.Icon)!;
    }

    public string lastSelectedWorld = "";
    private bool searchHistoryOpen = true;

    private int selectedListing = -1;
    private int selectedHistory = -1;

    private Vector4 textColourHQ = new Vector4(247f, 202f, 111f, 255f) / 255f;
    private Vector4 textColourWhite = new Vector4(1f, 1f, 1f, 1f);

    public MainWindow(Plugin plugin) : base(
        "SimpleMarketBoard",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        // var imagePath = Path.Combine(Service.PluginInterface.AssemblyLocation.Directory?.FullName!, "Data", "goat.png");
        // goatImage = Service.PluginInterface.UiBuilder.LoadImage(imagePath);

        this.plugin = plugin;
        CurrentItem = new Plugin.GameItem();
        CurrentItem.Id = 4691;
        CurrentItem.InGame = plugin.ItemSheet.GetRow(4691)!;
        CurrentItem.Name = "(/ω＼)";
        CurrentItemIcon = Service.TextureProvider.GetIcon(CurrentItem.InGame.Icon)!;
        if (plugin.Config.selectedWorld != "") lastSelectedWorld = plugin.Config.selectedWorld;

    }

    public void Dispose()
    {
        CurrentItemIcon?.Dispose();
    }

    public override void OnClose()
    {
        plugin.SearchHistoryClean();
    }



    public override void Draw()
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        var fontsize = ImGui.GetFontSize();
        var suffix = $"###{plugin.Name}-";
        // HQ yellow colour: ARGB

        var rightColWidth = fontsize * 6;
        var LeftColWidth = ImGui.GetWindowWidth() - rightColWidth;

        plugin.HoveredItem.CheckAsyncLastItem();





        // get 50% of the current window width for the tab bar

        ImGui.BeginChild("left-col-1", new Vector2(LeftColWidth, 0), false, ImGuiWindowFlags.NoScrollbar);



        var topY = ImGui.GetCursorPosY();


        ImGui.SetCursorPosY(topY + ImGui.GetTextLineHeightWithSpacing() + (1.1f * ImGui.GetStyle().ItemSpacing.Y));
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - (130 * scale) - (24 * ImGui.GetIO().FontGlobalScale) - (0.5f * ImGui.GetStyle().ItemSpacing.X));

        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.PushStyleColor(ImGuiCol.Text, plugin.Config.FilterHQ ? textColourHQ : textColourWhite);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Splotch}", new Vector2(24 * ImGui.GetIO().FontGlobalScale, ImGui.GetItemRectSize().Y)))
        {
            plugin.Config.FilterHQ = !plugin.Config.FilterHQ;
        }
        ImGui.PopStyleColor();
        ImGui.PopFont();
        ImGui.SameLine();


        ImGui.SetCursorPosY(topY + ImGui.GetTextLineHeightWithSpacing() + (1 * ImGui.GetStyle().ItemSpacing.Y));
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - (130 * scale));

        ImGui.SetNextItemWidth(130 * scale);
        if (ImGui.BeginCombo($"{suffix}worldCombo", plugin.Config.selectedWorld))
        {
            foreach (var world in plugin.ConfigWindow.worldList)
            {
                var isSelected = world.Item1 == plugin.Config.selectedWorld;
                if (ImGui.Selectable(world.Item2, isSelected))
                {
                    plugin.Config.selectedWorld = world.Item1;
                    plugin.PriceChecker.CheckAsyncRefresh();
                    Service.PluginLog.Info($"selected world: previous: {lastSelectedWorld}, config: {plugin.Config.selectedWorld}");
                    if (plugin.Config.selectedWorld != lastSelectedWorld)
                    {
                        Service.PluginLog.Info($"refresh previous: {lastSelectedWorld}, config: {plugin.Config.selectedWorld}");
                    }
                    lastSelectedWorld = plugin.Config.selectedWorld;
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }










        if (CurrentItem?.Id > 0)
        {
            ImGui.SetCursorPosY(topY);

            if (ImGui.ImageButton(CurrentItemIcon.ImGuiHandle, new Vector2(40, 40)))
            {
                ImGui.LogToClipboard();
                ImGui.LogText(CurrentItem.Name);
                ImGui.LogFinish();
            }

            ImGui.SameLine();
            // ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (ImGui.GetFontSize() / 2.0f) + (20 * scale));
            ImGui.SetCursorPosY(ImGui.GetCursorPosY());
            ImGui.PushFont(plugin.AxisTitle.ImFont);
            ImGui.Text(CurrentItem.Name);
            ImGui.PopFont();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY());


            int usedTile = plugin.Config.EnableRecentHistory ? 2 : 1;
            var priceTableHeight = ImGui.GetContentRegionAvail().Y / usedTile;


            ImGui.BeginChild("left-col-1-current-listings", new Vector2(0, priceTableHeight));
            ImGui.Columns(4, "current-listings-columns");

            ImGui.SetColumnWidth(0, 70.0f);
            ImGui.SetColumnWidth(1, 40.0f);
            ImGui.SetColumnWidth(2, 80.0f);
            ImGui.SetColumnWidth(3, 80.0f);

            ImGui.Separator();
            ImGui.Text("Selling");
            ImGui.NextColumn();
            ImGui.Text("Qty");
            ImGui.NextColumn();
            ImGui.Text("Total");
            ImGui.NextColumn();
            ImGui.Text("World");
            ImGui.NextColumn();
            ImGui.Separator();


            var marketDataListings = CurrentItem?.UniversalisResponse?.Listings;
            if (plugin.Config.FilterHQ)
            {
                marketDataListings = marketDataListings?.Where(l => l.Hq == true).OrderBy(l => l.PricePerUnit).ToList();
            }
            else
            {
                marketDataListings = marketDataListings?.OrderBy(l => l.PricePerUnit).ToList();
            }
            if (marketDataListings != null)
            {
                foreach (var listing in marketDataListings)
                {
                    if (listing.Hq) ImGui.PushStyleColor(ImGuiCol.Text, textColourHQ);

                    var index = marketDataListings.IndexOf(listing);
                    // float posX;

                    // posX = ImGui.GetCursorPosX()
                    //     + ImGui.GetColumnWidth()
                    //     - ImGui.CalcTextSize($"{listing.PricePerUnit}").X
                    //     - ImGui.GetScrollX()
                    //     - (2 * ImGui.GetStyle().ItemSpacing.X);
                    // if (posX > ImGui.GetCursorPosX()) ImGui.SetCursorPosX(posX);
                    if (ImGui.Selectable($"{listing.PricePerUnit}##listing{index}", selectedListing == index, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        selectedListing = index;
                    }
                    ImGui.NextColumn();

                    // posX = ImGui.GetCursorPosX()
                    //     + ImGui.GetColumnWidth()
                    //     - ImGui.CalcTextSize($"{listing.Quantity}").X
                    //     - ImGui.GetScrollX()
                    //     - (2 * ImGui.GetStyle().ItemSpacing.X);
                    // if (posX > ImGui.GetCursorPosX()) ImGui.SetCursorPosX(posX);
                    ImGui.Text($"{listing.Quantity:##,###}");
                    ImGui.NextColumn();

                    double totalPrice = plugin.Config.TotalIncludeTax
                      ? listing.Quantity + listing.Tax
                      : listing.Quantity;
                    // posX = ImGui.GetCursorPosX()
                    //     + ImGui.GetColumnWidth()
                    //     - ImGui.CalcTextSize($"{totalPrice}").X
                    //     - ImGui.GetScrollX()
                    //     - (2 * ImGui.GetStyle().ItemSpacing.X);
                    // if (posX > ImGui.GetCursorPosX()) ImGui.SetCursorPosX(posX);
                    ImGui.Text(totalPrice.ToString("N0", CultureInfo.CurrentCulture));
                    ImGui.NextColumn();


                    ImGui.Text($"{(CurrentItem!.UniversalisResponse.IsCrossWorld ? listing.WorldName : plugin.Config.selectedWorld)}");
                    ImGui.NextColumn();

                    ImGui.Separator();

                    if (listing.Hq) ImGui.PopStyleColor();
                }
            }
            ImGui.EndChild();


            if (plugin.Config.EnableRecentHistory)
            {
                ImGui.Separator();
                ImGui.Text("History");

                ImGui.BeginChild("left-col-1-history-entries", new Vector2(0, priceTableHeight));
                ImGui.Columns(4, "history-entries-columns");

                ImGui.SetColumnWidth(0, 70.0f);
                ImGui.SetColumnWidth(1, 40.0f);
                ImGui.SetColumnWidth(2, 80.0f);
                ImGui.SetColumnWidth(3, 80.0f);

                ImGui.Separator();
                ImGui.Text("Sold");
                ImGui.NextColumn();
                ImGui.Text("Qty");
                ImGui.NextColumn();
                ImGui.Text("Date");
                ImGui.NextColumn();
                ImGui.Text("World");
                ImGui.NextColumn();
                ImGui.Separator();

                var marketDataEntries = CurrentItem?.UniversalisResponse?.Entries;
                if (plugin.Config.FilterHQ)
                {
                    marketDataEntries = marketDataEntries?.Where(l => l.Hq == true).OrderByDescending(l => l.Timestamp).ToList();
                }
                else
                {
                    marketDataEntries = marketDataEntries?.OrderByDescending(l => l.Timestamp).ToList();
                }

                if (marketDataEntries != null)
                {
                    foreach (var entry in marketDataEntries)
                    {
                        if (entry.Hq) ImGui.PushStyleColor(ImGuiCol.Text, textColourHQ);

                        var index = marketDataEntries.IndexOf(entry);

                        if (ImGui.Selectable($"{entry.PricePerUnit}##history{index}", selectedHistory == index, ImGuiSelectableFlags.SpanAllColumns))
                        {
                            selectedHistory = index;
                        }
                        ImGui.NextColumn();

                        ImGui.Text($"{entry.Quantity:##,###}");
                        ImGui.NextColumn();

                        ImGui.Text($"{DateTimeOffset.FromUnixTimeSeconds(entry.Timestamp).LocalDateTime:MM-dd HH:mm}");
                        ImGui.NextColumn();

                        ImGui.Text(
                          $"{(CurrentItem!.UniversalisResponse.IsCrossWorld ? entry.WorldName : plugin.Config.selectedWorld)}");
                        ImGui.NextColumn();

                        ImGui.Separator();

                        if (entry.Hq) ImGui.PopStyleColor();
                    }
                }

                ImGui.EndChild();
            }
        }




        ImGui.EndChild();




        ImGui.SameLine();


        ImGui.BeginGroup();


        var rightColTableWidth = rightColWidth - 2 * ImGui.GetStyle().ItemSpacing.X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.5f * ImGui.GetStyle().ItemSpacing.X));
        // ImGui.GetContentRegionAvail().Y
        ImGui.BeginChild("right-col-1-buttons", new Vector2(rightColTableWidth, 24 * ImGui.GetIO().FontGlobalScale + 2 * ImGui.GetStyle().ItemSpacing.Y), true, ImGuiWindowFlags.NoScrollbar);


        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (0.7f * ImGui.GetStyle().ItemSpacing.Y));

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.3f * ImGui.GetStyle().ItemSpacing.X));
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.PushStyleColor(ImGuiCol.Text, searchHistoryOpen ? textColourHQ : textColourWhite);
        if (ImGui.Button($"{(char)FontAwesomeIcon.History}", new Vector2(24 * ImGui.GetIO().FontGlobalScale, ImGui.GetItemRectSize().Y)))
        {
            searchHistoryOpen = !searchHistoryOpen;
        }
        ImGui.PopStyleColor();
        ImGui.PopFont();


        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.2f * ImGui.GetStyle().ItemSpacing.X));
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Trash}", new Vector2(24 * ImGui.GetIO().FontGlobalScale, ImGui.GetItemRectSize().Y)))
        {
            plugin.GameItemCacheList.RemoveAll(i => i.Id == CurrentItem?.Id);
        }
        ImGui.PopFont();


        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.2f * ImGui.GetStyle().ItemSpacing.X));
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Cog}", new Vector2(24 * ImGui.GetIO().FontGlobalScale, ImGui.GetItemRectSize().Y)))
        {
            plugin.DrawConfigUI();
        }
        ImGui.PopFont();



        ImGui.EndChild();




        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.5f * ImGui.GetStyle().ItemSpacing.X));
        ImGui.BeginChild("right-col-2", new Vector2(rightColTableWidth, 1), false, ImGuiWindowFlags.NoScrollbar);
        ImGui.Separator();
        ImGui.EndChild();

        var WorldOutOfDate = CurrentItem?.UniversalisResponse?.WorldOutOfDate ?? new Dictionary<string, long>();
        var worldOutOfDateCount = WorldOutOfDate.Count;

        var dataColHeight =
            Math.Max(worldOutOfDateCount, 1.25f) * ImGui.GetTextLineHeight()
            + (worldOutOfDateCount - 1) * ImGui.GetStyle().ItemSpacing.Y
            + ImGui.GetTextLineHeightWithSpacing()
            + 2f * ImGui.GetStyle().ItemSpacing.Y;

        ImGui.SetCursorPosX(ImGui.GetCursorPosX());
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1);

        ImGui.BeginChild("right-col-3", new Vector2(rightColTableWidth - ImGui.GetStyle().ItemSpacing.X, ImGui.GetContentRegionAvail().Y - dataColHeight), false, ImGuiWindowFlags.HorizontalScrollbar);


        ImGui.SameLine();
        ImGui.Spacing();




        if (searchHistoryOpen)
        {
            // ImGui.Text("History");
            // ImGui.Separator();
            List<ulong> SearchHistoryIds = plugin.GameItemCacheList.Select(i => i.Id).ToList();

            foreach (var id in SearchHistoryIds)
            {
                var item = plugin.ItemSheet.GetRow((uint)id);
                if (item == null) continue;

                if (ImGui.Selectable($"{item.Name}", (uint)CurrentItem?.Id! == item.RowId))
                {
                    plugin.PriceChecker.CheckAsync(id, true);
                }
            }
        }

        ImGui.EndChild();

        ImGui.Separator();
        // ImGui.SameLine();

        var velocity = CurrentItem?.UniversalisResponse?.Velocity;
        ImGui.Text($"{(int?)velocity}");
        ImGui.Separator();


        // var dataColHeight = ImGui.GetContentRegionAvail().Y;


        ImGui.BeginChild("right-col-4", new Vector2(rightColTableWidth - ImGui.GetStyle().ItemSpacing.X, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.NoScrollbar);
        ImGui.Columns(2, "world-out-of-date-columns");

        ImGui.SetColumnWidth(0, rightColTableWidth - ImGui.CalcTextSize("0000").X - 2 * ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetColumnWidth(1, ImGui.CalcTextSize("0000").X);



        var worldOutOfDateIndex = 0;
        foreach (var i in WorldOutOfDate)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 0.5f * ImGui.GetStyle().ItemSpacing.Y);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetStyle().ItemSpacing.X);
            ImGui.Text($"{i.Key}");
            ImGui.NextColumn();
            alignRight($"{(int?)i.Value}");
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 0.5f * ImGui.GetStyle().ItemSpacing.Y);
            ImGui.Text($"{(int?)i.Value}");
            ImGui.NextColumn();

            worldOutOfDateIndex += 1;
            if (worldOutOfDateIndex < worldOutOfDateCount)
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 0.5f * ImGui.GetStyle().ItemSpacing.Y);
                ImGui.Separator();
            }
        }



        ImGui.EndChild();

        ImGui.EndGroup();
    }

    public void alignRight(string text)
    {
        var posX = ImGui.GetCursorPosX()
            + ImGui.GetColumnWidth()
            - ImGui.CalcTextSize(text).X
            - ImGui.GetScrollX()
            - (1 * ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetCursorPosX(posX);
        // Service.PluginLog.Debug($"dataColHeight: {ImGui.GetCursorPosX()} {ImGui.GetColumnWidth()} {text}:{ImGui.CalcTextSize(text).X} {ImGui.GetScrollX()} {posX}");
    }
}
