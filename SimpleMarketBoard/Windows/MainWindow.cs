using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Textures;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using Miosuke.Configuration;
using Miosuke.UiHelper;
using SimpleMarketBoard.Assets;
using SimpleMarketBoard.Modules;
using System.Globalization;
using System.Threading.Tasks;


namespace SimpleMarketBoard.Windows;

public class MainWindow : Window, IDisposable
{

    public MainWindow() : base(
        "SimpleMarketBoard",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(350, 450);
        SizeCondition = ImGuiCond.FirstUseEver;


        CurrentItem.Id = 4691;
        CurrentItem.InGame = Data.ItemSheet.GetRow(4691)!;
        CurrentItemLabel = "(/ω＼)";
        CurrentItemIcon = Service.Texture.GetFromGameIcon(new GameIconLookup(CurrentItem.InGame.Icon));
        if (P.Config.selectedWorld != "") lastSelectedWorld = P.Config.selectedWorld;
    }

    public override void PreDraw()
    {
        if (P.Config.EnableTheme)
        {
            P.PluginTheme.Push();
            Data.NotoSans17.Push();
            P.PluginThemeEnabled = true;
        }
    }

    public override void PostDraw()
    {
        if (P.PluginThemeEnabled)
        {
            P.PluginTheme.Pop();
            Data.NotoSans17.Pop();
            P.PluginThemeEnabled = false;
        }
    }

    public override void OnOpen()
    {
        UpdateWorld();
    }

    public override void OnClose()
    {
        P.PriceChecker.SearchHistoryClean();
    }

    public void Dispose()
    {
    }


    public PriceChecker.GameItem CurrentItem { get; set; } = new PriceChecker.GameItem();
    public ISharedImmediateTexture CurrentItemIcon = null!;
    public string CurrentItemLabel = "";

    public void CurrentItemUpdate(PriceChecker.GameItem gameItem)
    {
        CurrentItem = gameItem;
        CurrentItemIcon = Service.Texture.GetFromGameIcon(new GameIconLookup(CurrentItem.InGame.Icon))!;
        CurrentItem.Name = CurrentItem.InGame.Name.ToString();
        CurrentItemLabel = CurrentItem.Name;
    }

    public string lastSelectedWorld = "";
    private bool searchHistoryOpen = true;

    private int selectedListing = -1;
    private int selectedHistory = -1;

    public int LoadingQueue = 0;

    public List<(string, string)> worldList = [];
    public string playerHomeWorld = "";


    public override void Draw()
    {
        // -------------------------------- [  ui settings  ] --------------------------------
        // global
        var spacing = ImGui.GetStyle().ItemSpacing;

        // user
        var rightColWidth = P.Config.rightColWidth;
        var LeftColWidth = ImGui.GetWindowWidth() - rightColWidth;

        // -------------------------------- [  run check  ] --------------------------------
        // plugin.HoveredItem.CheckLastItem();


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

        ImGui.SetCursorPosY(ImGui.GetTextLineHeightWithSpacing() + 1.1f * spacing.Y + P.Config.ButtonSizeOffset[1]);
        ImGui.SetCursorPosX(
            ImGui.GetCursorPosX()
            + ImGui.GetContentRegionAvail().X
            - P.Config.WorldComboWidth
            - 2 * (P.Config.ButtonSizeOffset[0] + 0.5f * spacing.X)
        );
        DrawRefreshButton(P.Config.ButtonSizeOffset[0]);
        ImGui.SameLine();

        // HQ filter button
        ImGui.SetCursorPosY(ImGui.GetTextLineHeightWithSpacing() + 1.1f * spacing.Y + P.Config.ButtonSizeOffset[1]);
        ImGui.SetCursorPosX(
            ImGui.GetCursorPosX()
            + ImGui.GetContentRegionAvail().X
            - P.Config.WorldComboWidth
            - 1 * (P.Config.ButtonSizeOffset[0] + 0.5f * spacing.X)
        );
        DrawHqFilterButton(P.Config.ButtonSizeOffset[0]);
        ImGui.SameLine();

        // world selection dropdown
        ImGui.SetCursorPosY(ImGui.GetTextLineHeightWithSpacing() + 1 * spacing.Y + P.Config.ButtonSizeOffset[1]);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - P.Config.WorldComboWidth);
        DrawWorldCombo(P.Config.WorldComboWidth);


        // price table
        if (CurrentItem.Id > 0)
        {
            // set the size for the tables
            var tileToRender = P.Config.EnableRecentHistory ? 2 : 1;
            var priceTableHeight = ImGui.GetContentRegionAvail().Y / tileToRender;

            DrawCurrentListingTable(priceTableHeight + P.Config.soldTableOffset);

            if (P.Config.EnableRecentHistory)
            {
                if (P.Config.spaceBetweenTables > 0)
                {
                    ImGui.Separator();
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.spaceBetweenTables);
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
        var rightColTableWidth = rightColWidth - 2 * spacing.X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 0.5f * spacing.X);
        ImGui.BeginChild("col_right buttons", new Vector2(rightColTableWidth, 24 + 2 * spacing.Y + P.Config.ButtonSizeOffset[1]), true, ImGuiWindowFlags.NoScrollbar);

        // buttons
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 0.7f * spacing.Y);  // move the cursor up a bit for all buttons

        // history button
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 0.3f * spacing.X);
        DrawHistoryButton();
        ImGui.SameLine();

        // bin button
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 0.2f * spacing.X);
        DrawBinButton();
        ImGui.SameLine();

        // config button
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 0.2f * spacing.X);
        DrawConfigButton();

        ImGui.EndChild();


        // -------------------------------- [  item lists  ] --------------------------------
        var worldOutOfDateCount = CurrentItem.WorldOutOfDate.Count;
        var dataColHeight =
            Math.Max(worldOutOfDateCount, 1.25f) * ImGui.GetTextLineHeight()
            + (worldOutOfDateCount - 1) * spacing.Y
            + ImGui.GetTextLineHeightWithSpacing()
            + 2f * spacing.Y;

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

    private static readonly List<string> PublicRegions = [
        "Japan",
        "North-America",
        "Europe",
        "Oceania"
    ];
    private static readonly List<string> PublicDataCentres = [
        // North American Data Center
        "Aether", "Crystal", "Dynamis", "Primal",
        // European Data Center
        "Chaos", "Light",
        // Oceanian Data Center
        "Materia",
        // Japanese Data Center
        "Elemental", "Gaia", "Mana", "Meteor"
    ];
    private static readonly List<string> PublicWorlds = Service.Data.GetExcelSheet<World>().Where(x => x.IsPublic).Select(x => x.Name.ToString()).ToList();


    private static string getRegionStr(int region) => region switch
    {
        1 => "Japan",
        2 => "North-America",
        3 => "Europe",
        4 => "Oceania",
        _ => "",
    };



    public void UpdateWorld()
    {
        if (!P.IsInGame) return;

        if (P.Config.OverridePlayerHomeWorld)
        {
            var world = Service.Data.GetExcelSheet<World>().First(x => x.Name.ToString() == P.Config.PlayerHomeWorld);
            var dataCentre = world.DataCenter;
            var otherWorldsInDc = Service.Data.GetExcelSheet<World>()!
                .Where(x => x.DataCenter.RowId == dataCentre.RowId && x.IsPublic && x.Name != world.Name)
                .OrderBy(x => x.Name.ToString())
                .Select(x => x.Name.ToString());
            var regionStr = getRegionStr(world.Region);
            updateWorldList(regionStr, dataCentre.Value!.Name.ToString(), world.Name.ToString(), [.. otherWorldsInDc]);
        }
        else
        {
            if (P.LocalPlayerCurrentWorld is null) return;

            var dataCentre = P.LocalPlayerCurrentWorld.Value.DataCenter;
            var otherWorldsInDc = Service.Data.GetExcelSheet<World>()!
                .Where(x => x.DataCenter.RowId == dataCentre.RowId && x.IsPublic && x.Name != P.LocalPlayerCurrentWorld.Value.Name)
                .OrderBy(x => x.Name.ToString())
                .Select(x => x.Name.ToString());
            var regionStr = getRegionStr(dataCentre.Value!.Region);
            updateWorldList(regionStr, dataCentre.Value.Name.ToString(), P.LocalPlayerCurrentWorld.Value.Name.ToString(), [.. otherWorldsInDc]);
        }
    }

    private void updateWorldList(string region, string dataCentre, string homeWorld, List<string> worldsInDc)
    {
        var additionalWorlds = P.Config.AdditionalWorlds.ToList();
        worldList.Clear();

        string suffix;

        // add region
        suffix = additionalWorlds.Contains(region) ? "*" : "";
        worldList.Add((region, $"{(char)SeIconChar.ExperienceFilled}  {region}{suffix}"));
        worldList.AddRange(additionalWorlds
            .Where(x => PublicRegions.Contains(x) && !string.Equals(x, region, StringComparison.OrdinalIgnoreCase))
            .Select(x => (x, $"{(char)SeIconChar.ExperienceFilled}  {x}*"))
        );
        // data centres
        suffix = additionalWorlds.Contains(dataCentre) ? "*" : "";
        worldList.Add((dataCentre, $"{(char)SeIconChar.Experience}  {dataCentre}{suffix}"));
        worldList.AddRange(additionalWorlds
            .Where(x => PublicDataCentres.Contains(x) && !string.Equals(x, dataCentre, StringComparison.OrdinalIgnoreCase))
            .Select(x => (x, $"{(char)SeIconChar.Experience}  {x}*"))
        );
        // home world
        suffix = additionalWorlds.Contains(homeWorld) ? "*" : "";
        worldList.Add((homeWorld, $"{homeWorld}{suffix}"));
        // additional worlds
        worldList.AddRange(additionalWorlds
            .Where(x => PublicWorlds.Contains(x) && !string.Equals(x, homeWorld, StringComparison.OrdinalIgnoreCase))
            .Select(x => (x, $"{x}*"))
        );
        worldList.AddRange(worldsInDc
            .Where(x => !additionalWorlds.Contains(x))
            .Select(x => (x, $"{x}"))
        );

        playerHomeWorld = homeWorld;
        if (P.Config.selectedWorld == "")
        {
            P.Config.selectedWorld = dataCentre;
        }
    }

    public static ulong ParseItemId(string clipboardText)
    {
        var clipboardTextTrimmed = clipboardText.Trim();
        var inGame = Data.ItemSheet.Single(i => i.Name == clipboardTextTrimmed);
        Service.Log.Info($"Clipboard text: {clipboardTextTrimmed}, Item ID: {inGame.RowId}");
        return inGame.RowId;
        // debug
        // if (inGame is not null)
        // {
        //     return inGame.RowId;
        // }
        // return 0;
    }

    private void DrawItemIcon()
    {
        ImGui.SetCursorPosY(0);

        if (ImGui.ImageButton(CurrentItemIcon.GetWrapOrEmpty().ImGuiHandle, new Vector2(40, 40), Vector2.Zero, Vector2.One, 2))
        {
            if (Miosuke.Action.Hotkey.IsActive([VirtualKey.CONTROL], !P.Config.SearchHotkeyLoose))
            {
                var clipboardItemId = ParseItemId(ImGui.GetClipboardText());
                P.PriceChecker.DoCheckAsync(clipboardItemId);
            }
            else
            {
                ImGui.LogToClipboard();
                ImGui.LogText(CurrentItem.Name);
                ImGui.LogFinish();
            }
        }
        if (ImGui.BeginPopupContextItem($"testiconcontextmenu##{CurrentItem.Id}"))
        {
            if (ImGui.Selectable("Copy Name (LClick)"))
            {
                ImGui.SetClipboardText(CurrentItem.Name);
            }
            if (ImGui.Selectable("Copy ID"))
            {
                ImGui.SetClipboardText(CurrentItem.Id.ToString());
            }
            if (ImGui.Selectable("New search from clipboard (Ctrl+LClick)"))
            {
                var clipboardItemId = ParseItemId(ImGui.GetClipboardText());
                P.PriceChecker.DoCheckAsync(clipboardItemId);
            }
            ImGui.EndPopup();
        }
    }

    private void DrawItemName()
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY());

        Data.Axis20.Push();

        ImGui.Text(CurrentItemLabel);
        if (LoadingQueue > 0)
        {
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.PushStyleColor(ImGuiCol.Text, Ui.ColourHq);
            ImGui.Text($"{(char)FontAwesomeIcon.Spinner}");
            ImGui.PopStyleColor();
            ImGui.PopFont();
        }

        Data.Axis20.Pop();
    }

    private void DrawRefreshButton(float size)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Repeat}", new Vector2(size, size)))
        {
            P.PriceChecker.DoCheckRefreshAsync(CurrentItem);
        }
        ImGui.PopFont();
    }

    private void DrawHqFilterButton(float size)
    {
        var _iconColour = Ui.ColourWhite;
        if (P.Config.FilterHq) _iconColour = Ui.ColourHq;
        if (P.Config.UniversalisHqOnly) _iconColour = Ui.ColourBlue;
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.PushStyleColor(ImGuiCol.Text, _iconColour);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Splotch}", new Vector2(size, size)))
        {
            if (Miosuke.Action.Hotkey.IsActive([VirtualKey.CONTROL], !P.Config.SearchHotkeyLoose))
            {
                P.Config.UniversalisHqOnly = !P.Config.UniversalisHqOnly;
            }
            else
            {
                P.Config.FilterHq = !P.Config.FilterHq;
            }
        }
        ImGui.PopStyleColor();
        ImGui.PopFont();
    }

    private void DrawWorldCombo(float width)
    {
        if (P.PluginThemeEnabled)
        {
            Data.NotoSans17.Pop();
        }
        ImGui.SetNextItemWidth(width);
        if (ImGui.BeginCombo($"###{Name}selectedWorld", P.Config.selectedWorld))
        {
            foreach (var world in worldList)
            {
                if (world.Item1 == playerHomeWorld) ImGui.PushStyleColor(ImGuiCol.Text, Ui.ColourHq);

                var isSelected = world.Item1 == P.Config.selectedWorld;
                if (ImGui.Selectable(world.Item2, isSelected))
                {
                    P.Config.selectedWorld = world.Item1;
                    P.Config.Save();

                    if (P.Config.selectedWorld != lastSelectedWorld)
                    {
                        Service.Log.Debug($"Fetch data of {P.Config.selectedWorld}");
                        P.PriceChecker.DoCheckRefreshAsync(CurrentItem);
                    }

                    lastSelectedWorld = P.Config.selectedWorld;
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }

                if (world.Item1 == playerHomeWorld) ImGui.PopStyleColor();
            }

            ImGui.EndCombo();
        }
        if (P.PluginThemeEnabled)
        {
            Data.NotoSans17.Push();
        }
    }

    private void DrawCurrentListingTable(float height)
    {
        ImGui.BeginChild("col_left current_listings", new Vector2(0, height));

        ImGui.Columns(4, "col_left current_listings table");
        ImGui.SetColumnWidth(0, 70.0f + P.Config.sellingColWidthOffset[0]);
        ImGui.SetColumnWidth(1, 40.0f + P.Config.sellingColWidthOffset[1]);
        ImGui.SetColumnWidth(2, 80.0f + P.Config.sellingColWidthOffset[2]);
        ImGui.SetColumnWidth(3, 80.0f + P.Config.sellingColWidthOffset[3]);

        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
        if (P.Config.NumbersAlignRight)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + P.Config.NumbersAlignRightOffset);
            Ui.AlignRight("Selling");
        }
        ImGui.TextColored(Ui.ColourCyan, "Selling");
        ImGui.NextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
        if (P.Config.NumbersAlignRight)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + P.Config.NumbersAlignRightOffset);
            Ui.AlignRight("Q");
        }
        ImGui.TextColored(Ui.ColourCyan, "Q");
        ImGui.NextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
        if (P.Config.NumbersAlignRight)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + P.Config.NumbersAlignRightOffset);
            Ui.AlignRight("Total");
        }
        ImGui.TextColored(Ui.ColourCyan, "Total");
        ImGui.NextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
        ImGui.TextColored(Ui.ColourCyan, "World");
        ImGui.NextColumn();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
        ImGui.Separator();

        // prepare the data
        var marketDataListings = CurrentItem.UniversalisResponse.Listings;
        if (P.Config.FilterHq)
        {
            marketDataListings = marketDataListings.Where(l => l.Hq == true).OrderBy(l => l.PricePerUnit).ToList();
        }
        else
        {
            marketDataListings = marketDataListings.OrderBy(l => l.PricePerUnit).ToList();
        }

        if (marketDataListings is not null)
        {
            bool isColourPushed;
            foreach (var listing in marketDataListings)
            {
                isColourPushed = false;
                if (P.Config.MarkHigherThanVendor && CurrentItem.VendorSelling > 0 && listing.PricePerUnit >= CurrentItem.VendorSelling)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Ui.ColourCrimson);
                    isColourPushed = true;
                }
                else if (listing.Hq)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Ui.ColourHq);
                    isColourPushed = true;
                }

                // Selling
                var index = marketDataListings.IndexOf(listing);
                var selling = $"{listing.PricePerUnit}";
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
                if (P.Config.NumbersAlignRight)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + P.Config.NumbersAlignRightOffset);
                    Ui.AlignRight(selling);
                }
                if (ImGui.Selectable($"{selling}##listing{index}", selectedListing == index, ImGuiSelectableFlags.SpanAllColumns))
                {
                    selectedListing = index;
                }
                ImGui.NextColumn();

                // Q
                var quantity = $"{listing.Quantity:##,###}";
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
                if (P.Config.NumbersAlignRight)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + P.Config.NumbersAlignRightOffset);
                    Ui.AlignRight(quantity);
                }
                ImGui.Text(quantity);
                ImGui.NextColumn();

                // Total
                double totalPrice = P.Config.TotalIncludeTax
                  ? listing.PricePerUnit * listing.Quantity + listing.Tax
                  : listing.PricePerUnit * listing.Quantity;
                var total = totalPrice.ToString("N0", CultureInfo.CurrentCulture);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
                if (P.Config.NumbersAlignRight)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + P.Config.NumbersAlignRightOffset);
                    Ui.AlignRight(total);
                }
                ImGui.Text(total);
                ImGui.NextColumn();

                if (isColourPushed) ImGui.PopStyleColor();

                // World
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
                ImGui.Text($"{(CurrentItem.UniversalisResponse.IsCrossWorld ? listing.WorldName : P.Config.selectedWorld)}");
                ImGui.NextColumn();

                // Finish
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
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
        ImGui.SetColumnWidth(0, 70.0f + P.Config.soldColWidthOffset[0]);
        ImGui.SetColumnWidth(1, 40.0f + P.Config.soldColWidthOffset[1]);
        ImGui.SetColumnWidth(2, 80.0f + P.Config.soldColWidthOffset[2]);
        ImGui.SetColumnWidth(3, 80.0f + P.Config.soldColWidthOffset[3]);

        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
        if (P.Config.NumbersAlignRight)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + P.Config.NumbersAlignRightOffset);
            Ui.AlignRight("Sold");
        }
        ImGui.TextColored(Ui.ColourCyan, "Sold");
        ImGui.NextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
        if (P.Config.NumbersAlignRight)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + P.Config.NumbersAlignRightOffset);
            Ui.AlignRight("Q");
        }
        ImGui.TextColored(Ui.ColourCyan, "Q");
        ImGui.NextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
        ImGui.TextColored(Ui.ColourCyan, "Date");
        ImGui.NextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
        ImGui.TextColored(Ui.ColourCyan, "World");
        ImGui.NextColumn();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
        ImGui.Separator();

        // prepare the data
        var marketDataEntries = CurrentItem.UniversalisResponse.Entries;
        if (P.Config.FilterHq)
        {
            marketDataEntries = marketDataEntries.Where(l => l.Hq == true).OrderByDescending(l => l.Timestamp).ToList();
        }
        else
        {
            marketDataEntries = marketDataEntries.OrderByDescending(l => l.Timestamp).ToList();
        }

        if (marketDataEntries is not null)
        {
            foreach (var entry in marketDataEntries)
            {
                if (entry.Hq) ImGui.PushStyleColor(ImGuiCol.Text, Ui.ColourHq);

                // Sold
                var index = marketDataEntries.IndexOf(entry);
                var sold = $"{entry.PricePerUnit}";
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
                if (P.Config.NumbersAlignRight)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + P.Config.NumbersAlignRightOffset);
                    Ui.AlignRight(sold);
                }
                if (ImGui.Selectable($"{entry.PricePerUnit}##history{index}", selectedHistory == index, ImGuiSelectableFlags.SpanAllColumns))
                {
                    selectedHistory = index;
                }
                ImGui.NextColumn();

                // Q
                var quantity = $"{entry.Quantity:##,###}";
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
                if (P.Config.NumbersAlignRight)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + P.Config.NumbersAlignRightOffset);
                    Ui.AlignRight(quantity);
                }
                ImGui.Text(quantity);
                ImGui.NextColumn();

                // Date
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
                ImGui.Text($"{DateTimeOffset.FromUnixTimeSeconds(entry.Timestamp).LocalDateTime:MM-dd HH:mm}");
                ImGui.NextColumn();

                // World
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
                ImGui.Text($"{(CurrentItem.UniversalisResponse.IsCrossWorld ? entry.WorldName : P.Config.selectedWorld)}");
                ImGui.NextColumn();

                // Finish
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + P.Config.tableRowHeightOffset);
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
        ImGui.PushStyleColor(ImGuiCol.Text, searchHistoryOpen ? Ui.ColourHq : Ui.ColourWhite);
        if (ImGui.Button($"{(char)FontAwesomeIcon.List}", new Vector2(P.Config.ButtonSizeOffset[0], ImGui.GetItemRectSize().Y)))
        {
            searchHistoryOpen = !searchHistoryOpen;
        }
        ImGui.PopStyleColor();
        ImGui.PopFont();
    }

    private void DrawBinButton()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Trash}", new Vector2(P.Config.ButtonSizeOffset[0], ImGui.GetItemRectSize().Y)))
        {
            if (Miosuke.Action.Hotkey.IsActive([VirtualKey.CONTROL], !P.Config.SearchHotkeyLoose))
            {
                P.PriceChecker.GameItemCacheList.Clear();
            }
            else
            {
                P.PriceChecker.GameItemCacheList.RemoveAll(i => i.Id == CurrentItem.Id);
            }
        }
        ImGui.PopFont();
    }

    private static void DrawConfigButton()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Cog}", new Vector2(P.Config.ButtonSizeOffset[0], ImGui.GetItemRectSize().Y)))
        {
            P.DrawConfigUI();
        }
        ImGui.PopFont();
    }

    private void DrawWorldOutdated(Vector2 spacing, float rightColTableWidth, int worldOutOfDateCount)
    {
        ImGui.Columns(2, "col_right world_outdated table");

        // ImGui.SetColumnWidth(0, rightColTableWidth - ImGui.CalcTextSize("0000").X - (2 * spacing.X) + plugin.Config.WorldUpdateColWidthOffset[0]);
        // ImGui.SetColumnWidth(1, ImGui.CalcTextSize("0000").X + plugin.Config.WorldUpdateColWidthOffset[1]);
        ImGui.SetColumnWidth(0, ImGui.CalcTextSize("0000").X + P.Config.WorldUpdateColWidthOffset[0]);
        ImGui.SetColumnWidth(1, rightColTableWidth - ImGui.CalcTextSize("0000").X + P.Config.WorldUpdateColWidthOffset[1]);

        var worldOutOfDateIndex = 0;
        foreach (var i in CurrentItem.WorldOutOfDate)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + P.Config.WorldUpdateColPaddingOffset[0]);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 0.5f * spacing.Y);

            Ui.AlignRight($"{(int)i.Value}");
            ImGui.Text($"{(int)i.Value}");
            ImGui.NextColumn();


            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + P.Config.WorldUpdateColPaddingOffset[1]);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 0.5f * spacing.Y);

            ImGui.Text($"{i.Key}");
            ImGui.NextColumn();

            worldOutOfDateIndex += 1;
            if (worldOutOfDateIndex < worldOutOfDateCount)
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 0.5f * spacing.Y);
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
            foreach (var item in P.PriceChecker.GameItemCacheList)
            {
                if (ImGui.Selectable($"{item.Name}", (uint)CurrentItem.Id == item.Id))
                {
                    P.PriceChecker.DoCheckAsync(item.Id);
                }
            }
        }
    }
}
