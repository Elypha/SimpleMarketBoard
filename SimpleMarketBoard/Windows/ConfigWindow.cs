﻿using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System;
using Miosuke;
using Dalamud.Interface;
using Dalamud.Interface.Style;
using Dalamud.Interface.ImGuiNotification;


namespace SimpleMarketBoard;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly HotkeyUi search_hotkey_helper;
    private readonly HotkeyUi window_hotkey_helper;
    private string newAdditionalWorld = string.Empty;

    public ConfigWindow(Plugin plugin) : base(
        "SimpleMarketBoard Configuration"
    )
    {
        Size = new Vector2(450, 600);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
        search_hotkey_helper = new HotkeyUi();
        window_hotkey_helper = new HotkeyUi();
    }

    public override void PreDraw()
    {
        if (plugin.Config.EnableTheme)
        {
            plugin.PluginTheme.Push();
            plugin.NotoSansJpMedium.Push();
            plugin.PluginThemeEnabled = true;
        }
    }

    public override void PostDraw()
    {
        if (plugin.PluginThemeEnabled)
        {
            plugin.PluginTheme.Pop();
            plugin.NotoSansJpMedium.Pop();
            plugin.PluginThemeEnabled = false;
        }
    }

    public override void OnOpen()
    {
    }

    public override void OnClose()
    {
        plugin.Config.Save();
    }

    public void Dispose()
    {
    }

    public List<VirtualKey> NewHotkey = new();


    public override void Draw()
    {
        var padding = 0.8f;

        if (ImGui.Button("Open Manual in browser"))
        {
            var url = $"https://github.com/Elypha/SimpleMarketBoard#manual";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
            {
                UseShellExecute = true,
            });
        }
        ImGuiComponents.HelpMarker(
            "Click to open in your browser.\n" +
            "Check the 'Manual' section for a detailed introduction of all the features and how to use them."
        );

        // General
        DrawGeneral(padding);

        // Data
        DrawData(padding);

        // UI
        DrawUi(padding);
    }

    private void DrawGeneral(float padding)
    {
        // setup
        // float table_width = ImGui.GetWindowSize().X;
        // float table_height = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y * 2;
        // float col_name_width = ImGui.CalcTextSize("　Action name translation　").X + 2 * ImGui.GetStyle().ItemSpacing.X;
        // float col_value_width = 150.0f;
        // float col_value_content_width = 120.0f;
        var suffix = $"###{plugin.Name}[General]";
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));
        ImGui.TextColored(UI.ColourTitle, "General");
        ImGui.Separator();



        ImGui.TextColored(UI.ColourSubtitle, "Search");

        // HoverDelayMs
        ImGui.Text("Hover delay");
        ImGuiComponents.HelpMarker("you hover over an item > wait for this time > plugin starts to fetch the market data.");
        ImGui.SameLine();

        var HoverDelayMs = plugin.Config.HoverDelayMs;
        ImGui.SetNextItemWidth(250);
        if (ImGui.SliderInt($"{suffix}HoverDelayMS", ref HoverDelayMs, 0, 2000, $"{HoverDelayMs} ms"))
        {
            plugin.Config.HoverDelayMs = HoverDelayMs;
            plugin.Config.Save();
        }

        // SearchHotkeyEnabled
        var SearchHotkeyEnabled = plugin.Config.SearchHotkeyEnabled;
        if (ImGui.Checkbox($"Require a search hotkey{suffix}SearchHotkeyEnabled", ref SearchHotkeyEnabled))
        {
            plugin.Config.SearchHotkeyEnabled = SearchHotkeyEnabled;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: You need to press the hotkey before you hover over an item to get the market data."
        );
        ImGui.SameLine();
        var SearchHotkey = plugin.Config.SearchHotkey;
        if (search_hotkey_helper.DrawConfigUi("SearchHotkey", ref SearchHotkey, 100))
        {
            plugin.Config.SearchHotkey = SearchHotkey;
            plugin.Config.Save();
        }

        // SearchHotkeyLoose
        ImGui.Text("┗");
        ImGui.SameLine();
        var SearchHotkeyLoose = plugin.Config.SearchHotkeyLoose;
        if (ImGui.Checkbox($"Loose match{suffix}SearchHotkeyLoose", ref SearchHotkeyLoose))
        {
            plugin.Config.SearchHotkeyLoose = SearchHotkeyLoose;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: The hotkey will work no matter other keys are pressed or not. For example, you can trigger the hotkey while you are pressing W (to walk).\n" +
            "Disable: Your hotkey is checked strictly. It won't work if any extra keys are pressed."
        );

        // SearchHotkeyCanHide
        ImGui.Text("┗");
        ImGui.SameLine();
        var SearchHotkeyCanHide = plugin.Config.SearchHotkeyCanHide;
        if (ImGui.Checkbox($"... to hide the window{suffix}SearchHotkeyCanHide", ref SearchHotkeyCanHide))
        {
            plugin.Config.SearchHotkeyCanHide = SearchHotkeyCanHide;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: the hotkey will also hide the main window, if it's already shown and you are not hovering over an item.\n" +
            "Disable: the hotkey will not be able to hide the main window."
        );

        // ShowWindowOnSearch
        var ShowWindowOnSearch = plugin.Config.ShowWindowOnSearch;
        if (ImGui.Checkbox($"Show main window on search{suffix}ShowWindowOnSearch", ref ShowWindowOnSearch))
        {
            plugin.Config.ShowWindowOnSearch = ShowWindowOnSearch;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: The main window will show up, if it's not already shown, when you trigger a search."
        );

        // HotkeyBackgroundSearchEnabled
        var HotkeyBackgroundSearchEnabled = plugin.Config.HotkeyBackgroundSearchEnabled;
        if (ImGui.Checkbox($"Hotkey search can start from background{suffix}HotkeyBackgroundSearchEnabled", ref HotkeyBackgroundSearchEnabled))
        {
            plugin.Config.HotkeyBackgroundSearchEnabled = HotkeyBackgroundSearchEnabled;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: You can trigger a search even when the main window is not shown, by pressing the hotkey and, if configured, waiting for the hover delay."
        );

        // HoverBackgroundSearchEnabled
        var HoverBackgroundSearchEnabled = plugin.Config.HoverBackgroundSearchEnabled;
        if (ImGui.Checkbox($"Non-hotkey search can start from background{suffix}HoverBackgroundSearchEnabled", ref HoverBackgroundSearchEnabled))
        {
            plugin.Config.HoverBackgroundSearchEnabled = HoverBackgroundSearchEnabled;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: You can trigger a search even when the main window is not shown, by hovering over an item and waiting for the hover delay, if hotkey is not required."
        );



        ImGui.TextColored(UI.ColourSubtitle, "Data window");

        // WindowHotkeyEnabled
        var WindowHotkeyEnabled = plugin.Config.WindowHotkeyEnabled;
        if (ImGui.Checkbox($"Use a window hotkey{suffix}WindowHotkeyEnabled", ref WindowHotkeyEnabled))
        {
            plugin.Config.WindowHotkeyEnabled = WindowHotkeyEnabled;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: You can use a hotkey to show/hide the plugin window. Unlike the one above, this works in any situation."
        );
        ImGui.SameLine();
        var WindowHotkey = plugin.Config.WindowHotkey;
        if (window_hotkey_helper.DrawConfigUi("WindowHotkey", ref WindowHotkey, 100))
        {
            plugin.Config.WindowHotkey = WindowHotkey;
            plugin.Config.Save();
        }

        // WindowHotkeyCanShow
        ImGui.Text("┗");
        ImGui.SameLine();
        var WindowHotkeyCanShow = plugin.Config.WindowHotkeyCanShow;
        if (ImGui.Checkbox($"... to show the window{suffix}WindowHotkeyCanShow", ref WindowHotkeyCanShow))
        {
            plugin.Config.WindowHotkeyCanShow = WindowHotkeyCanShow;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: the hotkey will bring up the main window, if it's not already shown.\n" +
            "Disable: the hotkey will not be able to show the main window."
        );

        // WindowHotkeyCanHide
        ImGui.Text("┗");
        ImGui.SameLine();
        var WindowHotkeyCanHide = plugin.Config.WindowHotkeyCanHide;
        if (ImGui.Checkbox($"... to hide the window{suffix}WindowHotkeyCanHide", ref WindowHotkeyCanHide))
        {
            plugin.Config.WindowHotkeyCanHide = WindowHotkeyCanHide;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: the hotkey will hide the main window, if it's already shown.\n" +
            "Disable: the hotkey will not be able to hide the main window."
        );



        ImGui.TextColored(UI.ColourSubtitle, "Worlds");

        // OverridePlayerHomeWorld
        var OverridePlayerHomeWorld = plugin.Config.OverridePlayerHomeWorld;
        if (ImGui.Checkbox($"Override player home world{suffix}OverridePlayerHomeWorld", ref OverridePlayerHomeWorld))
        {
            plugin.Config.OverridePlayerHomeWorld = OverridePlayerHomeWorld;
        }
        ImGuiComponents.HelpMarker(
            "Your home world is used when populate the target world drop down menu.\n" +
            "Enable: The plugin will use the world you set below as your home world, no matter what your actual home world is.\n" +
            "Disable: The plugin will use the world your current character is in as your home world, and this gets updated when you login and change maps."
        );

        // PlayerHomeWorld
        ImGui.Text("┗");
        ImGui.SameLine();
        ImGui.Text("... using");
        ImGuiComponents.HelpMarker(
            "The WORLD you want to always use as your home world. Fill its full name in game, e.g., Hades"
        );
        ImGui.SameLine();
        ImGui.SetNextItemWidth(180);
        var PlayerHomeWorld = plugin.Config.PlayerHomeWorld;
        if (ImGui.InputText($"{suffix}-PlayerHomeWorld", ref PlayerHomeWorld, 32))
        {
            plugin.Config.PlayerHomeWorld = PlayerHomeWorld;
        }
        ImGui.SameLine();
        if (ImGui.Button("Apply"))
        {
            if (plugin.Config.OverridePlayerHomeWorld && plugin.Config.PlayerHomeWorld != "")
            {
                plugin.Config.Save();
                plugin.MainWindow.UpdateWorld(true);
            }
        }

        // AdditionalWorlds
        ImGui.Text("Additional Worlds/DCs/Regions");
        ImGuiComponents.HelpMarker(
            "Use this to add extra options to the target world dropdown menu to search price in.\n" +
            "To add a world or datacentre, use its full name in game, e.g., Hades, Mana\n" +
            "To add a region (supported by Universalis), e.g., Japan, North-America, Europe, Oceania\n" +
            "Your manually added worlds will be denoted by a star (*) in the dropdown menu."
        );
        for (var i = 0; i < plugin.Config.AdditionalWorlds.Count; i++)
        {
            ImGui.PushID($"{suffix}-AdditionalWorlds-{i}");
            var additionalWorld = plugin.Config.AdditionalWorlds[i];
            ImGui.SetNextItemWidth(180);
            if (ImGui.InputText($"{suffix}-AdditionalWorlds", ref additionalWorld, 32))
            {
                plugin.Config.AdditionalWorlds[i] = additionalWorld;
                plugin.Config.Save();
                plugin.MainWindow.UpdateWorld(true);
            }
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}{suffix}-del"))
            {
                plugin.Config.AdditionalWorlds.RemoveAt(i--);
                plugin.Config.Save();
                plugin.MainWindow.UpdateWorld(true);
            }
            ImGui.PopFont();
            ImGui.PopID();
            if (i < 0) break;
        }
        ImGui.SetNextItemWidth(180);
        ImGui.InputText($"{suffix}-AdditionalWorlds-new", ref newAdditionalWorld, 32);
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{FontAwesomeIcon.Plus.ToIconString()}{suffix}-add"))
        {
            plugin.Config.AdditionalWorlds.Add(newAdditionalWorld);
            plugin.Config.Save();
            plugin.MainWindow.UpdateWorld(true);
            newAdditionalWorld = string.Empty;
        }
        ImGui.PopFont();



        ImGui.TextColored(UI.ColourSubtitle, "Notification");

        // priceToPrint
        ImGui.Text("Use the price of");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        if (ImGui.BeginCombo($"{suffix}priceToPrint", plugin.Config.priceToPrint.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(PriceChecker.PriceToPrint)).Cast<PriceChecker.PriceToPrint>())
            {
                if (ImGui.Selectable(type.ToString(), type == plugin.Config.priceToPrint))
                {
                    plugin.Config.priceToPrint = type;
                    plugin.Config.Save();
                }
            }

            ImGui.EndCombo();
        }
        ImGuiComponents.HelpMarker(
            "Which price you want to use for the print.\n" +
            "PS UniversalisAverage is directly from Universalis by their algorithm."
        );

        // EnableChatLog
        var EnableChatLog = plugin.Config.EnableChatLog;
        if (ImGui.Checkbox($"Print to Chat log{suffix}EnableChatLog", ref EnableChatLog))
        {
            plugin.Config.EnableChatLog = EnableChatLog;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Log the result to your game chat.\n" +
            "Make sure you select the right channel so that only you can see it."
        );

        // ChatLogChannel
        ImGui.Text("┗");
        ImGui.SameLine();
        ImGui.Text("... in channel");
        ImGui.SameLine();
        var ChatLogChannel = plugin.Config.ChatLogChannel;
        ImGui.SetNextItemWidth(200);
        if (ImGui.BeginCombo($"{suffix}ChatLogChannel", ChatLogChannel.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(XivChatType)).Cast<XivChatType>())
            {
                if (ImGui.Selectable(type.ToString(), type == ChatLogChannel))
                {
                    plugin.Config.ChatLogChannel = type;
                    plugin.Config.Save();
                }
            }
            ImGui.EndCombo();
        }
        ImGuiComponents.HelpMarker(
            "The chat channel to print the result.\n" +
            "PS 'None' is also a name of a channel."
        );


        // EnableToastLog
        var EnableToastLog = plugin.Config.EnableToastLog;
        if (ImGui.Checkbox($"Print to Toast{suffix}EnableToastLog", ref EnableToastLog))
        {
            plugin.Config.EnableToastLog = EnableToastLog;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: The result will print in common toast."
        );

    }


    private void DrawData(float padding)
    {
        // setup
        float table_width = ImGui.GetWindowSize().X;
        float table_height = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y * 2;
        float col_name_width = ImGui.CalcTextSize("　Selling records numbers　").X + 2 * ImGui.GetStyle().ItemSpacing.X;
        float col_value_width = 150.0f;
        float col_value_content_width = 120.0f;
        var suffix = $"###{plugin.Name}[Data]";
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));
        ImGui.TextColored(UI.ColourTitle, "Data");
        ImGui.Separator();


        // TotalIncludeTax
        var TotalIncludeTax = plugin.Config.TotalIncludeTax;
        if (ImGui.Checkbox($"Include tax in total price{suffix}TotalIncludeTax", ref TotalIncludeTax))
        {
            plugin.Config.TotalIncludeTax = TotalIncludeTax;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Total price will include tax, i.e., how much you will pay in fact.\n" +
            "Disable: Total price will simply be amount*price_per_unit."
        );


        // MarkHigherThanVendor
        var MarkHigherThanVendor = plugin.Config.MarkHigherThanVendor;
        if (ImGui.Checkbox($"Red if higher than vendor{suffix}MarkHigherThanVendor", ref MarkHigherThanVendor))
        {
            plugin.Config.MarkHigherThanVendor = MarkHigherThanVendor;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: A record will be in red if the price is higher than vendor NPC."
        );

        ImGui.TextColored(UI.ColourSubtitle, "Cache");

        // MaxCacheItems
        ImGui.Text("Cached items");
        ImGuiComponents.HelpMarker(
            "How many items you want to keep in cache.\n" +
            "Cache clean will run when you close the window, and items more than this number will be removed from the oldest."
        );
        ImGui.SameLine();

        var MaxCacheItems = plugin.Config.MaxCacheItems;
        ImGui.SetNextItemWidth(250);
        if (ImGui.SliderInt($"{suffix}MaxCacheItems", ref MaxCacheItems, 1, 50, $"{MaxCacheItems} items"))
        {
            plugin.Config.MaxCacheItems = MaxCacheItems;
            plugin.Config.Save();
        }

        // CleanCacheASAP
        var CleanCacheASAP = plugin.Config.CleanCacheASAP;
        if (ImGui.Checkbox($"Clean cache ASAP{suffix}CleanCacheASAP", ref CleanCacheASAP))
        {
            plugin.Config.CleanCacheASAP = CleanCacheASAP;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Cache clean will also run immediately when a new item is added."
        );

        ImGui.TextColored(UI.ColourSubtitle, "Universalis");


        ImGui.BeginChild("table DrawData Universalis", new Vector2(table_width, table_height * 3), false);
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, col_name_width);
        ImGui.SetColumnWidth(1, col_value_width);

        // RequestTimeout
        ImGui.TextColored(UI.ColourText, "　Request timeout");
        ImGui.NextColumn();
        var RequestTimeout = plugin.Config.RequestTimeout;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}RequestTimeout", ref RequestTimeout))
        {
            plugin.Config.RequestTimeout = RequestTimeout;
            plugin.Universalis.ReloadHttpClient();
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "How long to wait in seconds, before the plugin gives up a web request if it's not finished.\n" +
            "Search on the whole region (e.g., Japan) or during peak times can take notably longer time. Please use a reasonable value.\n" +
            "Default: 20."
        );
        ImGui.NextColumn();

        // UniversalisListings
        ImGui.TextColored(UI.ColourText, "　Selling records numbers");
        ImGui.NextColumn();
        var UniversalisListings = plugin.Config.UniversalisListings;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}UniversalisListings", ref UniversalisListings))
        {
            plugin.Config.UniversalisListings = UniversalisListings;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "How many lines of selling records to request from Universalis.\n" +
            "More results take longer time to search. Please use a reasonable value.\n" +
            "Default: 70."
        );
        ImGui.NextColumn();

        // UniversalisEntries
        ImGui.TextColored(UI.ColourText, "　Sold records numbers");
        ImGui.NextColumn();
        var UniversalisEntries = plugin.Config.UniversalisEntries;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}UniversalisEntries", ref UniversalisEntries))
        {
            plugin.Config.UniversalisEntries = UniversalisEntries;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "How many lines of sold records to request from Universalis.\n" +
            "The more you request, the more accurate the Popularity is.\n" +
            "More results take longer time to search. Please set a reasonable value.\n" +
            "Default: 70."
        );
        ImGui.NextColumn();

        ImGui.Columns(1);
        ImGui.EndChild();
    }


    private void DrawUi(float padding)
    {
        // setup
        float table_width = ImGui.GetWindowSize().X;
        float table_height = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y * 2;
        float col_name_width = ImGui.CalcTextSize("　Selling records numbers　").X + 2 * ImGui.GetStyle().ItemSpacing.X;
        float col_value_width = 150.0f;
        float col_value_content_width = 120.0f;
        var suffix = $"###{plugin.Name}[UI]";
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));
        ImGui.TextColored(UI.ColourTitle, "UI");
        ImGui.Separator();


        // EnableTheme
        var EnableTheme = plugin.Config.EnableTheme;
        if (ImGui.Checkbox($"Use a theme for this plugin{suffix}EnableTheme", ref EnableTheme))
        {
            plugin.Config.EnableTheme = EnableTheme;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Use a bundled/custom theme (more compact and compatible) for this plugin.\n" +
            "Disable: Use your global Dalamud theme."
        );

        // CustomTheme
        ImGui.Text("┗");
        ImGui.SameLine();
        ImGui.Text("but a custom one");
        ImGuiComponents.HelpMarker(
            "Leave it empty to use the bundled theme (for better compatibility).\n" +
            "Fill this to use your custom theme for this plugin. You can export a theme from: Dalamud Settings > Look & Feel > Open Style Editor > Copy style to clipboard."
        );
        ImGui.SameLine();
        var CustomTheme = plugin.Config.CustomTheme;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputText($"{suffix}-CustomTheme", ref CustomTheme, 4096))
        {
            plugin.Config.CustomTheme = CustomTheme;
            plugin.Config.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("Apply"))
        {
            try
            {
                var _theme = StyleModel.Deserialize(CustomTheme) ?? throw new System.ArgumentException("Custom theme failed to deserialize.");
                plugin.PluginTheme = _theme;
            }
            catch (System.Exception e)
            {
                plugin.Config.CustomTheme = "";
                plugin.Config.Save();
                Service.NotificationManager.AddNotification(new Notification
                {
                    Content = $"Your custom theme is invalid and has been reset: {e.Message}",
                    Type = NotificationType.Error,
                });
            }
        }

        // NumbersAlignRight
        var NumbersAlignRight = plugin.Config.NumbersAlignRight;
        if (ImGui.Checkbox($"Numbers in table align right{suffix}NumbersAlignRight", ref NumbersAlignRight))
        {
            plugin.Config.NumbersAlignRight = NumbersAlignRight;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Numbers in the table, e.g., price, quantity, will align right.\n" +
            "Disable: Numbers will align left."
        );

        // NumbersAlignRightOffset
        ImGui.Text("┗");
        ImGui.SameLine();
        ImGui.Text("with X offset");
        ImGui.SameLine();
        var NumbersAlignRightOffset = plugin.Config.NumbersAlignRightOffset;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat($"{suffix}NumbersAlignRightOffset", ref NumbersAlignRightOffset))
        {
            plugin.Config.NumbersAlignRightOffset = NumbersAlignRightOffset;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Offset the alignment of numbers in the table. New position X' = X + offset."
        );


        // EnableRecentHistory
        var EnableRecentHistory = plugin.Config.EnableRecentHistory;
        if (ImGui.Checkbox($"Show table of sold records (history){suffix}EnableRecentHistory", ref EnableRecentHistory))
        {
            plugin.Config.EnableRecentHistory = EnableRecentHistory;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Display the table of sold records under selling records.\n" +
            "Disable: Display selling records only."
        );


        // soldTableOffset
        ImGui.Text("┗");
        ImGui.SameLine();
        ImGui.Text("with Y offset");
        ImGui.SameLine();
        var soldTableOffset = plugin.Config.soldTableOffset;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}soldTableOffset", ref soldTableOffset))
        {
            plugin.Config.soldTableOffset = soldTableOffset;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "For sold table, the new position Y' = Y + offset."
        );


        // spaceBetweenTables
        ImGui.Text("┗");
        ImGui.SameLine();
        ImGui.Text("with space between tables");
        ImGui.SameLine();
        var spaceBetweenTables = plugin.Config.spaceBetweenTables;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat($"{suffix}spaceBetweenTables", ref spaceBetweenTables))
        {
            if (spaceBetweenTables < 0) spaceBetweenTables = 0;
            plugin.Config.spaceBetweenTables = spaceBetweenTables;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "The space between the selling table and the sold table.\n" +
            "0 means no space, and the two tables are sticked together."
        );


        ImGui.TextColored(UI.ColourSubtitle, "Position Offset");


        ImGui.BeginChild("table DrawUi Position Offset", new Vector2(table_width, table_height * 8), false);
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, col_name_width);
        ImGui.SetColumnWidth(1, col_value_width);


        // WorldComboWidth
        ImGui.Text("World menu width");
        ImGui.NextColumn();
        var WorldComboWidth = plugin.Config.WorldComboWidth;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat($"{suffix}WorldComboWidth", ref WorldComboWidth))
        {
            plugin.Config.WorldComboWidth = WorldComboWidth;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("The width of the target world dropdown menu.");
        ImGui.NextColumn();


        // tableRowHeightOffset
        ImGui.Text("Table row height");
        ImGui.NextColumn();
        var tableRowHeightOffset = plugin.Config.tableRowHeightOffset;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat($"{suffix}tableRowHeightOffset", ref tableRowHeightOffset))
        {
            plugin.Config.tableRowHeightOffset = tableRowHeightOffset;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("Offset the height of each row in the table. New height H' = H + offset.");
        ImGui.NextColumn();


        // sellingColWidthOffset
        ImGui.Text("Selling column width");
        ImGui.NextColumn();
        var sellingColWidthOffset = plugin.Config.sellingColWidthOffset;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt4($"{suffix}sellingColWidthOffset", ref sellingColWidthOffset[0]))
        {
            plugin.Config.sellingColWidthOffset = sellingColWidthOffset;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("Offset the width of each column in the table. New width W' = W + offset.");
        ImGui.NextColumn();


        // soldColWidthOffset
        ImGui.Text("Sold column width");
        ImGui.NextColumn();
        var soldColWidthOffset = plugin.Config.soldColWidthOffset;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt4($"{suffix}soldColWidthOffset", ref soldColWidthOffset[0]))
        {
            plugin.Config.soldColWidthOffset = soldColWidthOffset;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("Offset the width of each column in the table. New width W' = W + offset.");
        ImGui.NextColumn();


        // rightColWidth
        ImGui.Text("Right panel width");
        ImGui.NextColumn();
        var rightColWidth = plugin.Config.rightColWidth;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}rightColWidth", ref rightColWidth))
        {
            plugin.Config.rightColWidth = rightColWidth;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("The width of the panel on the right of the main window.");
        ImGui.NextColumn();


        // WorldUpdateColWidthOffset
        ImGui.Text("World Updated width");
        ImGui.NextColumn();
        var WorldUpdateColWidthOffset = plugin.Config.WorldUpdateColWidthOffset;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt2($"{suffix}WorldUpdateColWidthOffset", ref WorldUpdateColWidthOffset[0]))
        {
            plugin.Config.WorldUpdateColWidthOffset = WorldUpdateColWidthOffset;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("The width of the columns of the last updated time for each world on the right bottom. Try changing the values to see what it does.");
        ImGui.NextColumn();


        // WorldUpdateColPaddingOffset
        ImGui.Text("World Updated padding");
        ImGui.NextColumn();
        var WorldUpdateColPaddingOffset = plugin.Config.WorldUpdateColPaddingOffset;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt2($"{suffix}WorldUpdateColPaddingOffset", ref WorldUpdateColPaddingOffset[0]))
        {
            plugin.Config.WorldUpdateColPaddingOffset = WorldUpdateColPaddingOffset;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("The padding of the columns of the last updated time for each world on the right bottom. Try changing the values to see what it does.");
        ImGui.NextColumn();

        // ButtonSizeOffset
        ImGui.Text("Button size");
        ImGui.NextColumn();
        var ButtonSizeOffset = new Vector2(plugin.Config.ButtonSizeOffset[0], plugin.Config.ButtonSizeOffset[1]);
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat2($"{suffix}ButtonSizeOffset", ref ButtonSizeOffset))
        {
            plugin.Config.ButtonSizeOffset = [ButtonSizeOffset.X, ButtonSizeOffset.Y];
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("The size of the buttons in the main window. Try changing the values to see what it does.");
        ImGui.NextColumn();

        ImGui.Columns(1);
        ImGui.EndChild();

    }
}
