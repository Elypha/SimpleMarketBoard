using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System;
using Dalamud.Interface.Textures;
using Miosuke;


namespace SimpleMarketBoard;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin) : base(
        "SimpleMarketBoard",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(350, 450);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;

        CurrentItem.Id = 4691;
        CurrentItem.InGame = plugin.ItemSheet.GetRow(4691)!;
        CurrentItemLabel = "(/ω＼)";
        CurrentItemIcon = Service.TextureProvider.GetFromGameIcon(new GameIconLookup(CurrentItem.InGame.Icon));
        if (plugin.Config.selectedWorld != "") lastSelectedWorld = plugin.Config.selectedWorld;

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

    public override void OnClose()
    {
        plugin.PriceChecker.SearchHistoryClean();
    }

    public void Dispose()
    {
    }


    public ulong LastItemId = 0;
    public PriceChecker.GameItem CurrentItem { get; set; } = new PriceChecker.GameItem();
    public ISharedImmediateTexture CurrentItemIcon = null!;
    public string CurrentItemLabel = "";

    public void CurrentItemUpdate(PriceChecker.GameItem gameItem)
    {
        LastItemId = CurrentItem.Id;
        CurrentItem = gameItem;
        CurrentItemIcon = Service.TextureProvider.GetFromGameIcon(new GameIconLookup(CurrentItem.InGame.Icon))!;
        CurrentItem.Name = CurrentItem.InGame.Name.ToString();
        CurrentItemLabel = CurrentItem.Name;
    }

    public string lastSelectedWorld = "";
    private bool searchHistoryOpen = true;

    private int selectedListing = -1;
    private int selectedHistory = -1;

    public int LoadingQueue = 0;

    private ulong playerId = 0;
    public List<(string, string)> worldList = new List<(string, string)>();




    public override void Draw()
    {
        // -------------------------------- [  ui settings  ] --------------------------------
        // global
        var spacing = ImGui.GetStyle().ItemSpacing;

        // user
        var rightColWidth = plugin.Config.rightColWidth;
        var LeftColWidth = ImGui.GetWindowWidth() - rightColWidth;

        // -------------------------------- [  run check  ] --------------------------------
        plugin.HoveredItem.CheckLastItem();


        // -------------------------------- [  column left  ] --------------------------------
        ImGui.BeginChild("col_left", new Vector2(LeftColWidth, 0), false, ImGuiWindowFlags.NoScrollbar);

        // icon and name
        if (CurrentItem.Id > 0)
        {
            DrawItemIcon();
            ImGui.SameLine();
            DrawItemName();
        }

        // refresh button
        float _button_size = 24;
        float _world_combo_width = 130;

        ImGui.SetCursorPosY(ImGui.GetTextLineHeightWithSpacing() + (1.1f * spacing.Y));
        ImGui.SetCursorPosX(
            ImGui.GetCursorPosX()
            + ImGui.GetContentRegionAvail().X
            - _world_combo_width
            - 2 * (_button_size + 0.5f * spacing.X)
        );
        DrawRefreshButton(_button_size);
        ImGui.SameLine();

        // HQ filter button
        ImGui.SetCursorPosY(ImGui.GetTextLineHeightWithSpacing() + (1.1f * spacing.Y));
        ImGui.SetCursorPosX(
            ImGui.GetCursorPosX()
            + ImGui.GetContentRegionAvail().X
            - _world_combo_width
            - 1 * (_button_size + 0.5f * spacing.X)
        );
        DrawHqFilterButton(_button_size);
        ImGui.SameLine();

        // world selection dropdown
        ImGui.SetCursorPosY(ImGui.GetTextLineHeightWithSpacing() + (1 * spacing.Y));
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - _world_combo_width);
        DrawWorldCombo(_world_combo_width);


        // price table
        if (CurrentItem.Id > 0)
        {
            // set the size for the tables
            var tileToRender = plugin.Config.EnableRecentHistory ? 2 : 1;
            var priceTableHeight = ImGui.GetContentRegionAvail().Y / tileToRender;

            DrawCurrentListingTable(priceTableHeight + plugin.Config.soldTableOffset);

            if (plugin.Config.EnableRecentHistory)
            {
                if (plugin.Config.spaceBetweenTables > 0)
                {
                    ImGui.Separator();
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.spaceBetweenTables);
                }
                // header in between
                DrawHistoryEntryTable(priceTableHeight);
            }
        }

        ImGui.EndChild();
        ImGui.SameLine();


        // -------------------------------- [  column right  ] --------------------------------
        ImGui.BeginGroup();


        // -------------------------------- [  buttons  ] --------------------------------
        var rightColTableWidth = rightColWidth - (2 * spacing.X);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.5f * spacing.X));
        ImGui.BeginChild("col_right buttons", new Vector2(rightColTableWidth, 24 + (2 * spacing.Y)), true, ImGuiWindowFlags.NoScrollbar);

        // buttons
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (0.7f * spacing.Y));  // move the cursor up a bit for all buttons

        // history button
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.3f * spacing.X));
        DrawHistoryButton();
        ImGui.SameLine();

        // bin button
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.2f * spacing.X));
        DrawBinButton();
        ImGui.SameLine();

        // config button
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (0.2f * spacing.X));
        DrawCOnfigButton();

        ImGui.EndChild();


        // -------------------------------- [  item lists  ] --------------------------------
        var worldOutOfDateCount = CurrentItem.WorldOutOfDate.Count;
        var dataColHeight =
            (Math.Max(worldOutOfDateCount, 1.25f) * ImGui.GetTextLineHeight())
            + ((worldOutOfDateCount - 1) * spacing.Y)
            + ImGui.GetTextLineHeightWithSpacing()
            + (2f * spacing.Y);

        ImGui.BeginChild("col_right search_history", new Vector2(rightColTableWidth - spacing.X, ImGui.GetContentRegionAvail().Y - dataColHeight), false, ImGuiWindowFlags.HorizontalScrollbar);
        DrawSearchHistory();
        ImGui.EndChild();


        // -------------------------------- [  velocity  ] --------------------------------
        ImGui.Separator();
        DrawVelocity();

        ImGui.Separator();


        // -------------------------------- [  world outdated  ] --------------------------------
        ImGui.BeginChild("col_right world_outdated", new Vector2(rightColTableWidth - spacing.X, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.NoScrollbar);
        DrawWorldOutdated(spacing, rightColTableWidth, worldOutOfDateCount);
        ImGui.EndChild();


        // column right end
        ImGui.EndGroup();
    }




    public void UpdateWorld()
    {
        Service.PluginLog.Debug($"[Config] Update player's current world");
        if (Service.ClientState.LocalContentId != 0 && playerId != Service.ClientState.LocalContentId)
        {
            var localPlayer = Service.ClientState.LocalPlayer;
            if (localPlayer == null) return;
            if (localPlayer.CurrentWorld.GameData == null) return;
            if (localPlayer.CurrentWorld.GameData.DataCenter.Value == null) return;

            var currentDc = localPlayer.CurrentWorld.GameData.DataCenter;
            var currentDcName = currentDc.Value.Name;
            var currentWorldName = localPlayer.CurrentWorld.GameData.Name;

            var dcWorlds = Service.Data.GetExcelSheet<World>()!
                .Where(w => w.DataCenter.Row == currentDc.Row && w.IsPublic)
                .OrderBy(w => (string)w.Name)
                .Where(w => localPlayer.CurrentWorld.Id != w.RowId)
                .Select(w =>
                {
                    return ((string)w.Name, (string)w.Name);
                });

            var currentRegionName = localPlayer.CurrentWorld.GameData.DataCenter.Value.Region switch
            {
                1 => "Japan",
                2 => "North-America",
                3 => "Europe",
                4 => "Oceania",
                _ => string.Empty,
            };

            worldList.Clear();
            worldList.Add((currentRegionName, $"{(char)SeIconChar.EurekaLevel}  {currentRegionName} "));
            worldList.Add((currentDcName, $"{(char)SeIconChar.CrossWorld}  {currentDcName} "));
            worldList.Add((currentWorldName, $"{(char)SeIconChar.Hyadelyn}  {currentWorldName}"));
            worldList.AddRange(dcWorlds);

            if (plugin.Config.selectedWorld == "")
            {
                plugin.Config.selectedWorld = currentDcName;
            }

            if (worldList.Count > 1)
            {
                playerId = Service.ClientState.LocalContentId;
            }
        }

        if (Service.ClientState.LocalContentId == 0)
        {
            playerId = 0;
        }
    }

    public ulong ParseItemId(string clipboardText)
    {
        var clipboardTextTrimmed = clipboardText.Trim();
        var inGame = plugin.ItemSheet.Single(i => i.Name == clipboardTextTrimmed);
        if (inGame != null)
        {
            return inGame.RowId;
        }
        return 0;
    }

    private void DrawItemIcon()
    {
        ImGui.SetCursorPosY(0);

        if (ImGui.ImageButton(CurrentItemIcon.GetWrapOrEmpty().ImGuiHandle, new Vector2(40, 40), Vector2.Zero, Vector2.One, 2))
        {
            if (Miosuke.Hotkey.IsActive([VirtualKey.CONTROL], !plugin.Config.SearchHotkeyLoose))
            {
                var clipboardItemId = ParseItemId(ImGui.GetClipboardText());
                plugin.PriceChecker.CheckNewAsync(clipboardItemId);
            }
            else
            {
                ImGui.LogToClipboard();
                ImGui.LogText(CurrentItem.Name);
                ImGui.LogFinish();
            }
        }
    }

    private void DrawItemName()
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY());

        plugin.Axis20.Push();

        ImGui.Text(CurrentItemLabel);
        if (LoadingQueue > 0)
        {
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushStyleColor(ImGuiCol.Text, Miosuke.UI.ColourHq);
            ImGui.Text($"{(char)FontAwesomeIcon.Spinner}");
            ImGui.PopStyleColor();
            ImGui.PopFont();
        }

        plugin.Axis20.Pop();
    }

    private void DrawRefreshButton(float size)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Repeat}", new Vector2(size, size)))
        {
            plugin.PriceChecker.CheckRefreshAsync(CurrentItem);
        }
        ImGui.PopFont();
    }

    private void DrawHqFilterButton(float size)
    {
        var _iconColour = Miosuke.UI.ColourWhite;
        if (plugin.Config.FilterHq) _iconColour = Miosuke.UI.ColourHq;
        if (plugin.Config.UniversalisHqOnly) _iconColour = Miosuke.UI.ColourCyan;
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.PushStyleColor(ImGuiCol.Text, _iconColour);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Splotch}", new Vector2(size, size)))
        {
            if (Miosuke.Hotkey.IsActive([VirtualKey.CONTROL], !plugin.Config.SearchHotkeyLoose))
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
    }

    private void DrawWorldCombo(float width)
    {
        ImGui.SetNextItemWidth(width);
        if (ImGui.BeginCombo($"###{plugin.Name}selectedWorld", plugin.Config.selectedWorld))
        {
            foreach (var world in worldList)
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
    }

    private void DrawCurrentListingTable(float height)
    {
        ImGui.BeginChild("col_left current_listings", new Vector2(0, height));

        ImGui.Columns(4, "col_left current_listings table");
        ImGui.SetColumnWidth(0, 70.0f + plugin.Config.sellingColWidthOffset[0]);
        ImGui.SetColumnWidth(1, 40.0f + plugin.Config.sellingColWidthOffset[1]);
        ImGui.SetColumnWidth(2, 80.0f + plugin.Config.sellingColWidthOffset[2]);
        ImGui.SetColumnWidth(3, 80.0f + plugin.Config.sellingColWidthOffset[3]);

        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
        if (plugin.Config.NumbersAlignRight)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + plugin.Config.NumbersAlignRightOffset);
            Miosuke.UI.AlignRight("Selling");
        }
        ImGui.TextColored(UI.ColourSubtitle, "Selling");
        ImGui.NextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
        if (plugin.Config.NumbersAlignRight)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + plugin.Config.NumbersAlignRightOffset);
            Miosuke.UI.AlignRight("Q");
        }
        ImGui.TextColored(UI.ColourSubtitle, "Q");
        ImGui.NextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
        if (plugin.Config.NumbersAlignRight)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + plugin.Config.NumbersAlignRightOffset);
            Miosuke.UI.AlignRight("Total");
        }
        ImGui.TextColored(UI.ColourSubtitle, "Total");
        ImGui.NextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
        ImGui.TextColored(UI.ColourSubtitle, "World");
        ImGui.NextColumn();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
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
            bool isColourPushed;
            foreach (var listing in marketDataListings)
            {
                isColourPushed = false;
                if (plugin.Config.MarkHigherThanVendor && (CurrentItem.VendorSelling > 0) && (listing.PricePerUnit >= CurrentItem.VendorSelling))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Miosuke.UI.ColourRedLight);
                    isColourPushed = true;
                }
                else if (listing.Hq)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Miosuke.UI.ColourHq);
                    isColourPushed = true;
                }

                // Selling
                var index = marketDataListings.IndexOf(listing);
                var selling = $"{listing.PricePerUnit}";
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
                if (plugin.Config.NumbersAlignRight)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + plugin.Config.NumbersAlignRightOffset);
                    Miosuke.UI.AlignRight(selling);
                }
                if (ImGui.Selectable($"{selling}##listing{index}", selectedListing == index, ImGuiSelectableFlags.SpanAllColumns))
                {
                    selectedListing = index;
                }
                ImGui.NextColumn();

                // Q
                var quantity = $"{listing.Quantity:##,###}";
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
                if (plugin.Config.NumbersAlignRight)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + plugin.Config.NumbersAlignRightOffset);
                    Miosuke.UI.AlignRight(quantity);
                }
                ImGui.Text(quantity);
                ImGui.NextColumn();

                // Total
                double totalPrice = plugin.Config.TotalIncludeTax
                  ? (listing.PricePerUnit * listing.Quantity) + listing.Tax
                  : listing.PricePerUnit * listing.Quantity;
                var total = totalPrice.ToString("N0", CultureInfo.CurrentCulture);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
                if (plugin.Config.NumbersAlignRight)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + plugin.Config.NumbersAlignRightOffset);
                    Miosuke.UI.AlignRight(total);
                }
                ImGui.Text(total);
                ImGui.NextColumn();

                if (isColourPushed) ImGui.PopStyleColor();

                // World
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
                ImGui.Text($"{(CurrentItem.UniversalisResponse.IsCrossWorld ? listing.WorldName : plugin.Config.selectedWorld)}");
                ImGui.NextColumn();

                // Finish
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
                ImGui.Separator();
            }
        }

        // === item price table 1 ===
        ImGui.EndChild();
    }

    private void DrawHistoryEntryTable(float height)
    {
        // === item price table 2 ===
        ImGui.BeginChild("col_left history_entries", new Vector2(0, height));

        ImGui.Columns(4, "col_left history_entries table");
        ImGui.SetColumnWidth(0, 70.0f + plugin.Config.soldColWidthOffset[0]);
        ImGui.SetColumnWidth(1, 40.0f + plugin.Config.soldColWidthOffset[1]);
        ImGui.SetColumnWidth(2, 80.0f + plugin.Config.soldColWidthOffset[2]);
        ImGui.SetColumnWidth(3, 80.0f + plugin.Config.soldColWidthOffset[3]);

        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
        if (plugin.Config.NumbersAlignRight)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + plugin.Config.NumbersAlignRightOffset);
            Miosuke.UI.AlignRight("Sold");
        }
        ImGui.TextColored(UI.ColourSubtitle, "Sold");
        ImGui.NextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
        if (plugin.Config.NumbersAlignRight)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + plugin.Config.NumbersAlignRightOffset);
            Miosuke.UI.AlignRight("Q");
        }
        ImGui.TextColored(UI.ColourSubtitle, "Q");
        ImGui.NextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
        ImGui.TextColored(UI.ColourSubtitle, "Date");
        ImGui.NextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
        ImGui.TextColored(UI.ColourSubtitle, "World");
        ImGui.NextColumn();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
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
                if (entry.Hq) ImGui.PushStyleColor(ImGuiCol.Text, Miosuke.UI.ColourHq);

                // Sold
                var index = marketDataEntries.IndexOf(entry);
                var sold = $"{entry.PricePerUnit}";
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
                if (plugin.Config.NumbersAlignRight)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + plugin.Config.NumbersAlignRightOffset);
                    Miosuke.UI.AlignRight(sold);
                }
                if (ImGui.Selectable($"{entry.PricePerUnit}##history{index}", selectedHistory == index, ImGuiSelectableFlags.SpanAllColumns))
                {
                    selectedHistory = index;
                }
                ImGui.NextColumn();

                // Q
                var quantity = $"{entry.Quantity:##,###}";
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
                if (plugin.Config.NumbersAlignRight)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + plugin.Config.NumbersAlignRightOffset);
                    Miosuke.UI.AlignRight(quantity);
                }
                ImGui.Text(quantity);
                ImGui.NextColumn();

                // Date
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
                ImGui.Text($"{DateTimeOffset.FromUnixTimeSeconds(entry.Timestamp).LocalDateTime:MM-dd HH:mm}");
                ImGui.NextColumn();

                // World
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
                ImGui.Text($"{(CurrentItem.UniversalisResponse.IsCrossWorld ? entry.WorldName : plugin.Config.selectedWorld)}");
                ImGui.NextColumn();

                // Finish
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + plugin.Config.tableRowHeightOffset);
                ImGui.Separator();

                if (entry.Hq) ImGui.PopStyleColor();
            }
        }

        // === item price table 2 ===
        ImGui.EndChild();
    }

    private void DrawHistoryButton()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.PushStyleColor(ImGuiCol.Text, searchHistoryOpen ? Miosuke.UI.ColourHq : Miosuke.UI.ColourWhite);
        if (ImGui.Button($"{(char)FontAwesomeIcon.List}", new Vector2(24, ImGui.GetItemRectSize().Y)))
        {
            searchHistoryOpen = !searchHistoryOpen;
        }
        ImGui.PopStyleColor();
        ImGui.PopFont();
    }

    private void DrawBinButton()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Trash}", new Vector2(24, ImGui.GetItemRectSize().Y)))
        {
            if (Miosuke.Hotkey.IsActive([VirtualKey.CONTROL], !plugin.Config.SearchHotkeyLoose))
            {
                plugin.PriceChecker.GameItemCacheList.Clear();
            }
            else
            {
                plugin.PriceChecker.GameItemCacheList.RemoveAll(i => i.Id == CurrentItem.Id);
            }
        }
        ImGui.PopFont();
    }

    private void DrawCOnfigButton()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Cog}", new Vector2(24, ImGui.GetItemRectSize().Y)))
        {
            plugin.DrawConfigUI();
        }
        ImGui.PopFont();
    }

    private void DrawWorldOutdated(Vector2 spacing, float rightColTableWidth, int worldOutOfDateCount)
    {
        ImGui.Columns(2, "col_right world_outdated table");

        // ImGui.SetColumnWidth(0, rightColTableWidth - ImGui.CalcTextSize("0000").X - (2 * spacing.X) + plugin.Config.WorldUpdateColWidthOffset[0]);
        // ImGui.SetColumnWidth(1, ImGui.CalcTextSize("0000").X + plugin.Config.WorldUpdateColWidthOffset[1]);
        ImGui.SetColumnWidth(0, ImGui.CalcTextSize("0000").X + plugin.Config.WorldUpdateColWidthOffset[0]);
        ImGui.SetColumnWidth(1, rightColTableWidth - ImGui.CalcTextSize("0000").X + plugin.Config.WorldUpdateColWidthOffset[1]);

        var worldOutOfDateIndex = 0;
        foreach (var i in CurrentItem.WorldOutOfDate)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + plugin.Config.WorldUpdateColPaddingOffset[0]);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (0.5f * spacing.Y));

            Miosuke.UI.AlignRight($"{(int)i.Value}");
            ImGui.Text($"{(int)i.Value}");
            ImGui.NextColumn();


            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + plugin.Config.WorldUpdateColPaddingOffset[1]);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (0.5f * spacing.Y));

            ImGui.Text($"{i.Key}");
            ImGui.NextColumn();

            worldOutOfDateIndex += 1;
            if (worldOutOfDateIndex < worldOutOfDateCount)
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (0.5f * spacing.Y));
                ImGui.Separator();
            }
        }
    }

    private void DrawVelocity()
    {
        var velocity = CurrentItem.UniversalisResponse.Velocity;
        ImGui.Text($"{(int)velocity}");
    }

    private void DrawSearchHistory()
    {
        if (searchHistoryOpen)
        {
            foreach (var item in plugin.PriceChecker.GameItemCacheList)
            {
                if (ImGui.Selectable($"{item.Name}", (uint)CurrentItem.Id == item.Id))
                {
                    plugin.PriceChecker.CheckNewAsync(item.Id);
                }
            }
        }
    }
}
