using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using ImGuiNET;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System;


namespace SimpleMarketBoard;

public class MainWindow : Window, IDisposable
{
    // private IDalamudTextureWrap goatImage;
    private Plugin plugin;
    public ulong LastItemId = 0;
    public Plugin.GameItem CurrentItem { get; set; } = new Plugin.GameItem();
    public IDalamudTextureWrap CurrentItemIcon = null!;
    public string CurrentItemLabel = "";

    public void CurrentItemUpdate(Plugin.GameItem gameItem)
    {
        LastItemId = CurrentItem.Id;
        CurrentItem = gameItem;
        CurrentItemIcon = Service.TextureProvider.GetIcon(CurrentItem.InGame.Icon)!;
        CurrentItem.Name = CurrentItem.InGame.Name.ToString();
        CurrentItemLabel = CurrentItem.Name;
    }

    public string lastSelectedWorld = "";
    private bool searchHistoryOpen = true;

    private int selectedListing = -1;
    private int selectedHistory = -1;

    public int LoadingQueue = 0;

    private Vector4 textColourHq = new Vector4(247f, 202f, 111f, 255f) / 255f;
    private Vector4 textColourHqOnly = new Vector4(62f, 186f, 240f, 255f) / 255f;
    private Vector4 textColourHigherThanVendor = new Vector4(230f, 90f, 80f, 255f) / 255f;
    private Vector4 textColourWhite = new Vector4(1f, 1f, 1f, 1f);

    public MainWindow(Plugin plugin) : base(
        "SimpleMarketBoard",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(350, 450);
        SizeCondition = ImGuiCond.FirstUseEver;
        // SizeConstraints = new WindowSizeConstraints
        // {
        //     MinimumSize = new Vector2(100, 100),
        //     MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        // };

        this.plugin = plugin;
        CurrentItem.Id = 4691;
        CurrentItem.InGame = plugin.ItemSheet.GetRow(4691)!;
        CurrentItemLabel = "(/ω＼)";
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

    public override void PreDraw()
    {
        if (plugin.Config.EnableTheme)
        {
            plugin.PluginTheme.Push();
            plugin.PluginThemeEnabled = true;
        }
    }

    public override void PostDraw()
    {
        if (plugin.PluginThemeEnabled)
        {
            plugin.PluginTheme.Pop();
            plugin.PluginThemeEnabled = false;
        }
    }



    public override void Draw()
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        var fontsize = ImGui.GetFontSize();
        var suffix = $"###{plugin.Name}-";
        var _coloured = false;
        // HQ yellow colour: ARGB

        var rightColWidth = plugin.Config.rightColWidth;
        var LeftColWidth = ImGui.GetWindowWidth() - rightColWidth;

        plugin.HoveredItem.CheckLastItem();




        // === left column (main tables) ===
        ImGui.BeginChild("left-col-1", new Vector2(LeftColWidth, 0), false, ImGuiWindowFlags.NoScrollbar);

        var topY = ImGui.GetCursorPosY();  // store the Y pos which is the top of the window



        // refresh button
        ImGui.SetCursorPosY(topY + ImGui.GetTextLineHeightWithSpacing() + (1.1f * ImGui.GetStyle().ItemSpacing.Y));
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - (130 * scale) - 2 * (24 * ImGui.GetIO().FontGlobalScale + 0.5f * ImGui.GetStyle().ItemSpacing.X));

        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Repeat}", new Vector2(24 * ImGui.GetIO().FontGlobalScale, ImGui.GetItemRectSize().Y)))
        {
            plugin.PriceChecker.CheckRefreshAsync(CurrentItem);
        }
        ImGui.PopFont();
        ImGui.SameLine();


        // HQ filter button
        ImGui.SetCursorPosY(topY + ImGui.GetTextLineHeightWithSpacing() + (1.1f * ImGui.GetStyle().ItemSpacing.Y));
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - (130 * scale) - 1 * (24 * ImGui.GetIO().FontGlobalScale + 0.5f * ImGui.GetStyle().ItemSpacing.X));

        var _iconColour = textColourWhite;
        if (plugin.Config.FilterHq) _iconColour = textColourHq;
        if (plugin.Config.UniversalisHqOnly) _iconColour = textColourHqOnly;
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.PushStyleColor(ImGuiCol.Text, _iconColour);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Splotch}", new Vector2(24 * ImGui.GetIO().FontGlobalScale, ImGui.GetItemRectSize().Y)))
        {
            if (plugin.PluginHotkey.CheckHotkeyState(new VirtualKey[] { VirtualKey.CONTROL }))
            {
                plugin.Config.UniversalisHqOnly = !plugin.Config.UniversalisHqOnly;
            }
            else
            {
                plugin.Config.FilterHq = !plugin.Config.FilterHq;
            }
        }
        ImGui.PopStyleColor();
        ImGui.PopFont();
        ImGui.SameLine();


        // world selection dropdown
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
                    plugin.Config.Save();

                    Service.PluginLog.Debug($"[UI] Selected world: {lastSelectedWorld} -> {plugin.Config.selectedWorld}");
                    if (plugin.Config.selectedWorld != lastSelectedWorld)
                    {
                        Service.PluginLog.Info($"[UI] Fetch data of {plugin.Config.selectedWorld}");
                        plugin.PriceChecker.CheckRefreshAsync(CurrentItem);
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



        // main table 1
        if (CurrentItem.Id > 0)
        {


            // item icon
            ImGui.SetCursorPosY(topY);

            if (ImGui.ImageButton(CurrentItemIcon.ImGuiHandle, new Vector2(40, 40), Vector2.Zero, Vector2.One, 2))
            {
                ImGui.LogToClipboard();
                ImGui.LogText(CurrentItem.Name);
                ImGui.LogFinish();
            }


            // item name
            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY());

            plugin.AxisTitle.Push();
            ImGui.Text(CurrentItemLabel);
            if (LoadingQueue > 0)
            {
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.PushStyleColor(ImGuiCol.Text, textColourHq);
                ImGui.Text($"{(char)FontAwesomeIcon.Spinner}");
                ImGui.PopStyleColor();
                ImGui.PopFont();
            }

            plugin.AxisTitle.Pop();


            // set the size for the tables
            int usedTile = plugin.Config.EnableRecentHistory ? 2 : 1;
            var priceTableHeight = ImGui.GetContentRegionAvail().Y / usedTile;


            // === item price table 1 ===
            ImGui.SetCursorPosY(ImGui.GetCursorPosY());

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

            // prepare the data
            var marketDataListings = CurrentItem.UniversalisResponse.Listings;
            if (plugin.Config.FilterHq)
            {
                marketDataListings = marketDataListings.Where(l => l.Hq == true).OrderBy(l => l.PricePerUnit).ToList();
            }
            else
            {
                marketDataListings = marketDataListings.OrderBy(l => l.PricePerUnit).ToList();
            }

            if (marketDataListings != null)
            {
                foreach (var listing in marketDataListings)
                {
                    _coloured = false;
                    if (plugin.Config.MarkHigherThanVendor && (CurrentItem.VendorSelling > 0) && (listing.PricePerUnit >= CurrentItem.VendorSelling))
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, textColourHigherThanVendor);
                        _coloured = true;
                    }
                    else if (listing.Hq)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, textColourHq);
                        _coloured = true;
                    }

                    // Selling
                    var index = marketDataListings.IndexOf(listing);
                    if (ImGui.Selectable($"{listing.PricePerUnit}##listing{index}", selectedListing == index, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        selectedListing = index;
                    }
                    ImGui.NextColumn();

                    // Qty
                    ImGui.Text($"{listing.Quantity:##,###}");
                    ImGui.NextColumn();

                    // Total
                    double totalPrice = plugin.Config.TotalIncludeTax
                      ? listing.PricePerUnit * listing.Quantity + listing.Tax
                      : listing.PricePerUnit * listing.Quantity;
                    ImGui.Text(totalPrice.ToString("N0", CultureInfo.CurrentCulture));
                    ImGui.NextColumn();

                    if (_coloured) ImGui.PopStyleColor();

                    // World
                    ImGui.Text($"{(CurrentItem.UniversalisResponse.IsCrossWorld ? listing.WorldName : plugin.Config.selectedWorld)}");
                    ImGui.NextColumn();

                    // Finish
                    ImGui.Separator();
                }
            }

            // === item price table 1 ===
            ImGui.EndChild();



            if (plugin.Config.EnableRecentHistory)
            {
                // header in between
                ImGui.Separator();
                ImGui.Text("History");

                // === item price table 2 ===
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

                // prepare the data
                var marketDataEntries = CurrentItem.UniversalisResponse.Entries;
                if (plugin.Config.FilterHq)
                {
                    marketDataEntries = marketDataEntries.Where(l => l.Hq == true).OrderByDescending(l => l.Timestamp).ToList();
                }
                else
                {
                    marketDataEntries = marketDataEntries.OrderByDescending(l => l.Timestamp).ToList();
                }

                if (marketDataEntries != null)
                {
                    foreach (var entry in marketDataEntries)
                    {
                        if (entry.Hq) ImGui.PushStyleColor(ImGuiCol.Text, textColourHq);

                        // Sold
                        var index = marketDataEntries.IndexOf(entry);
                        if (ImGui.Selectable($"{entry.PricePerUnit}##history{index}", selectedHistory == index, ImGuiSelectableFlags.SpanAllColumns))
                        {
                            selectedHistory = index;
                        }
                        ImGui.NextColumn();

                        // Qty
                        ImGui.Text($"{entry.Quantity:##,###}");
                        ImGui.NextColumn();

                        // Date
                        ImGui.Text($"{DateTimeOffset.FromUnixTimeSeconds(entry.Timestamp).LocalDateTime:MM-dd HH:mm}");
                        ImGui.NextColumn();

                        // World
                        ImGui.Text(
                          $"{(CurrentItem.UniversalisResponse.IsCrossWorld ? entry.WorldName : plugin.Config.selectedWorld)}");
                        ImGui.NextColumn();

                        // Finish
                        ImGui.Separator();

                        if (entry.Hq) ImGui.PopStyleColor();
                    }
                }

                // === item price table 2 ===
                ImGui.EndChild();
            }
        }

        // === left column (main tables) ===
        ImGui.EndChild();



        // === right column ===
        ImGui.SameLine();

        ImGui.BeginGroup();  // for all elements in the right column


        // === right column group 1 ===
        var rightColTableWidth = rightColWidth - 2 * ImGui.GetStyle().ItemSpacing.X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.5f * ImGui.GetStyle().ItemSpacing.X));

        ImGui.BeginChild("right-col-1-buttons", new Vector2(rightColTableWidth, 24 * ImGui.GetIO().FontGlobalScale + 2 * ImGui.GetStyle().ItemSpacing.Y), true, ImGuiWindowFlags.NoScrollbar);


        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (0.7f * ImGui.GetStyle().ItemSpacing.Y));  // move the cursor up a bit for all buttons

        // history button
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.3f * ImGui.GetStyle().ItemSpacing.X));

        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.PushStyleColor(ImGuiCol.Text, searchHistoryOpen ? textColourHq : textColourWhite);
        if (ImGui.Button($"{(char)FontAwesomeIcon.List}", new Vector2(24 * ImGui.GetIO().FontGlobalScale, ImGui.GetItemRectSize().Y)))
        {
            searchHistoryOpen = !searchHistoryOpen;
        }
        ImGui.PopStyleColor();
        ImGui.PopFont();

        // bin button
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.2f * ImGui.GetStyle().ItemSpacing.X));
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Trash}", new Vector2(24 * ImGui.GetIO().FontGlobalScale, ImGui.GetItemRectSize().Y)))
        {
            if (plugin.PluginHotkey.CheckHotkeyState(new VirtualKey[] { VirtualKey.CONTROL }))
            {
                plugin.GameItemCacheList.Clear();
            }
            else
            {
                plugin.GameItemCacheList.RemoveAll(i => i.Id == CurrentItem.Id);
            }
        }
        ImGui.PopFont();


        // config button
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.2f * ImGui.GetStyle().ItemSpacing.X));
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Cog}", new Vector2(24 * ImGui.GetIO().FontGlobalScale, ImGui.GetItemRectSize().Y)))
        {
            plugin.DrawConfigUI();
        }
        ImGui.PopFont();


        // === right column group 1 ===
        ImGui.EndChild();



        // === separator ===
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.5f * ImGui.GetStyle().ItemSpacing.X));
        ImGui.BeginChild("right-col-2", new Vector2(rightColTableWidth, 1), false, ImGuiWindowFlags.NoScrollbar);
        ImGui.Separator();
        ImGui.EndChild();


        // === right column group 2 ===

        var worldOutOfDateCount = CurrentItem.WorldOutOfDate.Count;
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
            foreach (var item in plugin.GameItemCacheList)
            {
                if (ImGui.Selectable($"{item.Name}", (uint)CurrentItem.Id == item.Id))
                {
                    plugin.PriceChecker.CheckNewAsync(item.Id, item.IsHQ);
                }
            }
        }

        // === right column group 2 ===
        ImGui.EndChild();

        ImGui.Separator();



        // === right column group 3 ===
        var velocity = CurrentItem.UniversalisResponse.Velocity;
        ImGui.Text($"{(int)velocity}");
        ImGui.Separator();



        // === right column group 4 ===
        ImGui.BeginChild("right-col-4", new Vector2(rightColTableWidth - ImGui.GetStyle().ItemSpacing.X, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.NoScrollbar);

        ImGui.Columns(2, "world-out-of-date-columns");

        ImGui.SetColumnWidth(0, rightColTableWidth - ImGui.CalcTextSize("0000").X - 2 * ImGui.GetStyle().ItemSpacing.X + plugin.Config.WorldUpdateColWidthOffset[0]);
        ImGui.SetColumnWidth(1, ImGui.CalcTextSize("0000").X + plugin.Config.WorldUpdateColWidthOffset[1]);

        var worldOutOfDateIndex = 0;
        foreach (var i in CurrentItem.WorldOutOfDate)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 0.5f * ImGui.GetStyle().ItemSpacing.Y);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetStyle().ItemSpacing.X);

            ImGui.Text($"{i.Key}");
            ImGui.NextColumn();


            alignRight($"{(int)i.Value}");
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 0.5f * ImGui.GetStyle().ItemSpacing.Y);

            ImGui.Text($"{(int)i.Value}");
            ImGui.NextColumn();

            worldOutOfDateIndex += 1;
            if (worldOutOfDateIndex < worldOutOfDateCount)
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 0.5f * ImGui.GetStyle().ItemSpacing.Y);
                ImGui.Separator();
            }
        }

        // === right column group 4 ===
        ImGui.EndChild();

        ImGui.EndGroup();  // for all elements in the right column
    }

    public void alignRight(string text)
    {
        var posX = ImGui.GetCursorPosX()
            + ImGui.GetColumnWidth()
            - ImGui.CalcTextSize(text).X
            - ImGui.GetScrollX()
            - (1 * ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetCursorPosX(posX);
    }
}
