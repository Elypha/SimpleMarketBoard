using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System;


namespace SimpleMarketBoard;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public ConfigWindow(Plugin plugin) : base(
        "SimpleMarketBoard Configuration"
    )
    {
        Size = new Vector2(450, 600);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
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


    private string? currHotkeyBoxName;
    private string? currHotkeyName;
    public List<VirtualKey> NewHotkey = new();


    public override void Draw()
    {
        var padding = 0.8f;
        string suffix;


        if (ImGui.CollapsingHeader("Features & UI Introduction"))
        {
            ImGui.TextColored(
                new Vector4(245f, 220f, 80f, 255f) / 255f,
                "Below is a detailed manual. Read the sections you are interested.\n" +
                "It covers my design intention and helps you use this plugin to its fullest.\n" +
                "In the Changelog below you find brief points on what's been recently added."
            );

            if (ImGui.Button($"Open Changelog Window"))
            {
                plugin.ChangelogWindow.Toggle();
            }

            ImGui.Text("");

            plugin.UiHelper.BulletTextList(
                "Keybinding",
                "you can configure it below",
                new List<string> {
                    "This is intended to cater your preference. By design there are 3 ways to start a check.",
                    "1. Hover over an item, then press the keybinding.",
                    "2. Press the keybinding first, then hover over an item.",
                    "3. Just hover over an item.",
                    "All these can be configured with an optional delay.",
                    "With that said, my recommendation in terms of efficiency is to set the delay to 0, and use a simple keybinding like 'Ctrl/Tab'. Then the work flow is to point on an item and press the trigger key (then you get the data).",
                    "Remember you can search multiple items without waiting for the previous ones to finish. All your query will be added to the cache sequentially.",
                }
            );

            plugin.UiHelper.BulletTextList(
                "Item Icon",
                "on the top left corner",
                new List<string> {
                    "· Click: Copy the item name to clipboard.",
                    "· Alt + Click: Search for a new item based on the text from your clipboard. It should be the item's full name. If you use another plugin that allows you Ctrl+C to copy the item name, this feature can be useful.",
                }
            );

            plugin.UiHelper.BulletTextList(
                "Item Name",
                "to the right of the Item Icon",
                new List<string> {
                    "The item name is followed by an orange loading icon when there are still requests going on.",
                    "This place is also used to display the status of the market data query request.",
                    "If it says 'timedout' or 'failed', just use the Refresh button and usually it will be fine.",
                }
            );

            plugin.UiHelper.BulletTextList(
                "Refresh Button",
                "the two-arrow button under the item name",
                new List<string> {
                    "· Click: (Force) Refresh the market data of the current item, or the selected item on the History Panel.",
                }
            );

            plugin.UiHelper.BulletTextList(
                "HQ Filter Button",
                "the star button under the item name",
                new List<string> {
                    "HQ items are by default coloured in orange in the market data table. You can further filter the results by",
                    "· Click: Toggle whether to show only HQ items in the table. When enabled the icon will be coloured in orange.",
                    "· Ctrl + Click: Toggle whether to request only HQ items from the server. When enabled the icon will be coloured in cyan. This has a higher priority. It can be helpful when you are looking for HQ items but the table is flooded with low-priced NQs, e.g., Commanding Craftsman's Draught.",
                }
            );

            plugin.UiHelper.BulletTextList(
                "History Panel",
                "on the right side, the 1st button from left",
                new List<string> {
                    "Items you searched are stored in the cache. If this button is on (by default) you will see a list under it. You can click in the list to quick review them without having to wait for the request again.",
                    "I'm still planning what to present in that area when this panel is switched off.",
                }
            );

            plugin.UiHelper.BulletTextList(
                "Delete Button",
                "on the right side, the 2nd button from left",
                new List<string> {
                    "· Click: Remove the current item from the History Panel.",
                    "· Ctrl + Click: Remove all items from the History Panel.",
                }
            );

            plugin.UiHelper.BulletTextList(
                "Config Button",
                "on the right side, the 3rd button from left",
                new List<string> {
                    "· Click: Show/Hide the configuration window.",
                }
            );

            plugin.UiHelper.BulletTextList(
                "Market Data Table",
                "on the left bottom",
                new List<string> {
                    "· Selling and history prices are tax excluded (i.e., the price as you see in the game).",
                    "This serves trading and comparison purposes. There's an option to include tax in the total price which gives you a rough idea of how much you will pay.",
                    "· The price can be coloured in red if it's higher than the vendor price from NPC (enable via option, takes priority over HQ colouring).",
                }
            );

            plugin.UiHelper.BulletTextList(
                "Popularity",
                "on the right side, the one-line text in between the two tables",
                new List<string> {
                    "· This number incicates how many items are sold in a certain period of time.",
                    "It is calculated based on the data per request, so theoretically the more data you require, the more accurate it is. To help reduce the impact of the API server this plugin limits the size to 75. Therefore, for popular items this number may be sampled from a much shorter period of time and thus this number is not proportional across items. However, it's still true that the higher the number, the more popular the item is.",
                }
            );

            plugin.UiHelper.BulletTextList(
                "Update Information per World",
                "on the right side, the table under the Popularity",
                new List<string> {
                    "· The table shows how long ago the market data was updated for each world in hours.",
                    "This is to help you understand how fresh the data is. You can check any world on your decision but you are always encouraged to visit the least updated worlds so that the public data can get updated which benefits us all.",
                }
            );

            ImGui.Text("");

            plugin.UiHelper.BulletTextList(
                "PS",
                null,
                new List<string> {
                    "This plugin is still in active development.",
                    "If you have any suggestions or issues please let me know on Discord (XIVLauncher/plugin-help-forum/Simple Market Board) or GitHub (Elypha/SimpleMarketBoard). I appreciate it!",
                    "Elypha"
                }
            );

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize("- END -").X / 2);
            ImGui.TextColored(plugin.UiHelper.ColourKhaki, "- END -");
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (4 * ImGui.GetTextLineHeight()));
        }

        // ----------------- General -----------------
        suffix = $"###{plugin.Name}[General]";
        ImGui.TextColored(plugin.UiHelper.ColourKhaki, "General");
        ImGui.Separator();


        // HoverDelayIn100MS
        ImGui.Text("Hover delay");
        ImGui.SameLine();

        var HoverDelayIn100MS = plugin.Config.HoverDelayIn100MS;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderInt($"{suffix}HoverDelayIn100MS", ref HoverDelayIn100MS, 0, 20, $"{HoverDelayIn100MS * 100} ms"))
        {
            plugin.Config.HoverDelayIn100MS = HoverDelayIn100MS;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("How long to wait in ms, after you hover over an item, before the plugin starts to check the market data.");


        // MaxCacheItems
        ImGui.Text("Cache size");
        ImGui.SameLine();

        var MaxCacheItems = plugin.Config.MaxCacheItems;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderInt($"{suffix}MaxCacheItems", ref MaxCacheItems, 1, 30, $"{MaxCacheItems} items"))
        {
            plugin.Config.MaxCacheItems = MaxCacheItems;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("How many items you want to keep in cache. Items more than this number will be removed from the oldest when you close the window.");




        // EnableRecentHistory
        var EnableRecentHistory = plugin.Config.EnableRecentHistory;
        if (ImGui.Checkbox($"Include recent history entries{suffix}EnableRecentHistory", ref EnableRecentHistory))
        {
            plugin.Config.EnableRecentHistory = EnableRecentHistory;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Display recent history entries under current listings.\n" +
            "Disable: The above will not happen."
        );


        // TotalIncludeTax
        var TotalIncludeTax = plugin.Config.TotalIncludeTax;
        if (ImGui.Checkbox($"Total price include tax{suffix}TotalIncludeTax", ref TotalIncludeTax))
        {
            plugin.Config.TotalIncludeTax = TotalIncludeTax;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Total price will include tax.\n" +
            "Disable: The above will not happen."
        );

        // MarkHigherThanVendor
        var MarkHigherThanVendor = plugin.Config.MarkHigherThanVendor;
        if (ImGui.Checkbox($"Colour red if higher than vendor{suffix}MarkHigherThanVendor", ref MarkHigherThanVendor))
        {
            plugin.Config.MarkHigherThanVendor = MarkHigherThanVendor;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: The listing will be coloured in red if it sells higher than vendor NPC in game.\n" +
            "Disable: The above will not happen."
        );

        // CleanCacheAsYouGo
        var CleanCacheAsYouGo = plugin.Config.CleanCacheAsYouGo;
        if (ImGui.Checkbox($"Clean cache ASAP{suffix}CleanCacheAsYouGo", ref CleanCacheAsYouGo))
        {
            plugin.Config.CleanCacheAsYouGo = CleanCacheAsYouGo;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: The cache clean will also run right after a new one comes in.\n" +
            "Disable: The above will not happen."
        );

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));



        // ----------------- Keybinding -----------------
        suffix = $"###{plugin.Name}[Keybinding]";
        ImGui.TextColored(plugin.UiHelper.ColourKhaki, "Key bindings");
        ImGui.Separator();


        // KeybindingEnabled
        var KeybindingEnabled = plugin.Config.KeybindingEnabled;
        if (ImGui.Checkbox($"Use a keybinding{suffix}KeybindingEnabled", ref KeybindingEnabled))
        {
            plugin.Config.KeybindingEnabled = KeybindingEnabled;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: You need to press the keybinding before you hover over an item to get the market data.\n" +
            "Disable: Pressing a keybinding will not be required."
        );


        // AllowKeybindingAfterHover
        var AllowKeybindingAfterHover = plugin.Config.AllowKeybindingAfterHover;
        if (ImGui.Checkbox($"Allow keybinding after hover{suffix}AllowKeybindingAfterHover", ref AllowKeybindingAfterHover))
        {
            plugin.Config.AllowKeybindingAfterHover = AllowKeybindingAfterHover;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: In addition, you can also hover over an item first, then press the keybinding to get the market data of it.\n" +
            "Disable: The above will not work."
        );


        // BindingHotkey
        ImGui.Text("Check market data");
        ImGui.SameLine();
        var strBindingHotkey = string.Join("+", plugin.Config.BindingHotkey.Select(k => k.GetKeyName()));

        if (currHotkeyName == "BindingHotkey")
        {
            if (ImGui.GetIO().KeyAlt && !NewHotkey.Contains(VirtualKey.MENU)) NewHotkey.Add(VirtualKey.MENU);
            if (ImGui.GetIO().KeyShift && !NewHotkey.Contains(VirtualKey.SHIFT)) NewHotkey.Add(VirtualKey.SHIFT);
            if (ImGui.GetIO().KeyCtrl && !NewHotkey.Contains(VirtualKey.CONTROL)) NewHotkey.Add(VirtualKey.CONTROL);

            for (var k = 0; k < ImGui.GetIO().KeysDown.Count && k < 160; k++)
            {
                if (ImGui.GetIO().KeysDown[k])
                {
                    if (!NewHotkey.Contains((VirtualKey)k))
                    {
                        if ((VirtualKey)k == VirtualKey.ESCAPE)
                        {
                            currHotkeyName = null;
                            NewHotkey.Clear();
                            currHotkeyBoxName = null;
                            break;
                        }
                        NewHotkey.Add((VirtualKey)k);
                    }
                }
            }

            NewHotkey.Sort();
            strBindingHotkey = string.Join("+", NewHotkey.Select(k => k.GetKeyName()));
        }

        if (currHotkeyName == "BindingHotkey")
        {
            ImGui.PushStyleColor(ImGuiCol.Border, 0xFF00A5FF);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 2);
        }

        ImGui.SetNextItemWidth(150);
        ImGui.InputText($"{suffix}BindingHotkey-input-init", ref strBindingHotkey, 100, ImGuiInputTextFlags.ReadOnly);
        var active = ImGui.IsItemActive();
        if (currHotkeyName == "BindingHotkey")
        {

            ImGui.PopStyleColor(1);
            ImGui.PopStyleVar();

            if (currHotkeyBoxName != "BindingHotkey")
            {
                ImGui.SetKeyboardFocusHere(-1);
                currHotkeyBoxName = "BindingHotkey";
            }
            else
            {
                ImGui.SameLine();
                if (ImGui.Button(NewHotkey.Count > 0 ? $"Confirm{suffix}BindingHotkey-button-confirm" : $"Cancel{suffix}BindingHotkey-button-cancel"))
                {
                    currHotkeyName = null;
                    if (NewHotkey.Count > 0) plugin.Config.BindingHotkey = NewHotkey.ToArray();
                    plugin.Config.Save();
                    NewHotkey.Clear();
                }
                else
                {
                    if (!active)
                    {
                        currHotkeyBoxName = null;
                        currHotkeyName = null;
                        if (NewHotkey.Count > 0) plugin.Config.BindingHotkey = NewHotkey.ToArray();
                        plugin.Config.Save();
                        NewHotkey.Clear();
                    }
                }
            }
        }
        else
        {
            ImGui.SameLine();
            if (ImGui.Button($"Set Keybinding{suffix}BindingHotkey-button-change"))
            {
                currHotkeyName = "BindingHotkey";
            }
        }
        ImGuiComponents.HelpMarker(
            "Press the button to set a keybinding. Press ESC to cancel."
        );


        // KeybindingToOpenWindow
        var KeybindingToOpenWindow = plugin.Config.KeybindingToOpenWindow;
        if (ImGui.Checkbox($"Keybinding can open main window{suffix}KeybindingToOpenWindow", ref KeybindingToOpenWindow))
        {
            plugin.Config.KeybindingToOpenWindow = KeybindingToOpenWindow;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: If you choose to use a keybinding, now the main window will show up automatically when you press the keybinding to check an item\n" +
            "Disable: The above will not work."
        );

        // KeybindingToCloseWindow
        var KeybindingToCloseWindow = plugin.Config.KeybindingToCloseWindow;
        if (ImGui.Checkbox($"Keybinding can close main window{suffix}KeybindingToCloseWindow", ref KeybindingToCloseWindow))
        {
            plugin.Config.KeybindingToCloseWindow = KeybindingToCloseWindow;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: If you choose to use a keybinding, now the main window will close when you press the keybinding without hovering an item\n" +
            "Disable: The above will not work."
        );


        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));


        // ----------------- API -----------------
        suffix = $"###{plugin.Name}[API]";
        ImGui.TextColored(plugin.UiHelper.ColourKhaki, "API settings");
        ImGui.Separator();


        // RequestTimeout
        ImGui.Text("Request timeout");
        ImGui.SameLine();
        var RequestTimeout = plugin.Config.RequestTimeout;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt($"seconds{suffix}RequestTimeout", ref RequestTimeout))
        {
            plugin.Config.RequestTimeout = RequestTimeout;
            plugin.Universalis.ReloadHttpClient();
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "How long to wait in seconds, before the plugin gives up on the market data query.\n" +
            "Please note that query the whole region (e.g., Japan) may take longer time, so please set a reasonable value."
            );


        // UniversalisListings
        ImGui.Text("Current listings to request");
        ImGui.SameLine();
        var UniversalisListings = plugin.Config.UniversalisListings;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt($"{suffix}UniversalisListings", ref UniversalisListings))
        {
            plugin.Config.UniversalisListings = UniversalisListings;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "How many listings to request from Universalis.\n" +
            "Please set a reasonable value. Default is 70.\n"
            );


        // UniversalisEntries
        ImGui.Text("Historical entries to request");
        ImGui.SameLine();
        var UniversalisEntries = plugin.Config.UniversalisEntries;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt($"{suffix}UniversalisEntries", ref UniversalisEntries))
        {
            plugin.Config.UniversalisEntries = UniversalisEntries;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "How many historical entries to request from Universalis.\n" +
            "Please set a reasonable value. Default is 70.\n" +
            "The more you request, the more accurate the popularity number is."
            );


        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));


        // ----------------- UI -----------------
        suffix = $"###{plugin.Name}[UI]";
        ImGui.TextColored(plugin.UiHelper.ColourKhaki, "UI settings");
        ImGui.Separator();


        // EnableTheme
        var EnableTheme = plugin.Config.EnableTheme;
        if (ImGui.Checkbox($"Enable bundled theme{suffix}EnableTheme", ref EnableTheme))
        {
            plugin.Config.EnableTheme = EnableTheme;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: A bundled theme will apply to this plugin so that it can be more compact and compatible.\n" +
            "Disable: Your own (default) dalamud theme will be used."
        );


        // EnableSearchFromClipboard
        var EnableSearchFromClipboard = plugin.Config.EnableSearchFromClipboard;
        if (ImGui.Checkbox($"Enable search from clipboard{suffix}EnableSearchFromClipboard", ref EnableSearchFromClipboard))
        {
            plugin.Config.EnableSearchFromClipboard = EnableSearchFromClipboard;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: When you hold 'Alt' and left-click the item icon, the plugin will use the item name from your clipboard and fetch its data.\n" +
            "Disable: The above will not happen."
        );


        // EnableChatLog
        var EnableChatLog = plugin.Config.EnableChatLog;
        if (ImGui.Checkbox($"Print to Chat{suffix}EnableChatLog", ref EnableChatLog))
        {
            plugin.Config.EnableChatLog = EnableChatLog;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Your query will print to your game chat (make sure you select the right channel so that only you can see it).\n" +
            "Disable: The above will not happen."
        );


        // EnableToastLog
        var EnableToastLog = plugin.Config.EnableToastLog;
        if (ImGui.Checkbox($"Print to Toast{suffix}EnableToastLog", ref EnableToastLog))
        {
            plugin.Config.EnableToastLog = EnableToastLog;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Your query will print to your game toast.\n" +
            "Disable: The above will not happen."
        );


        // priceToPrint
        ImGui.Text("Price data to print");
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
            "Which price data you want to print to your Chat/Toast log.\n" +
            "PS UniversalisAverage is an original value returned from Universalis by their algorithm."
        );


        // ChatLogChannel
        ImGui.Text("Channel to print");
        ImGui.SameLine();
        var ChatLogChannel = plugin.Config.ChatLogChannel;
        // ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 3);
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
            "Which in-game chat channel you want to print your query to.\n" +
            "PS \"None\" is also a channel name."
        );


        // rightColWidth
        ImGui.Text("Main window right panel width");
        ImGui.SameLine();
        var rightColWidth = plugin.Config.rightColWidth;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt($"px{suffix}rightColWidth", ref rightColWidth))
        {
            plugin.Config.rightColWidth = rightColWidth;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("The width of the column on the right of the main window.");


        // WorldUpdateColWidthOffset
        ImGui.Text("World last update column width offset");
        ImGui.SameLine();
        var WorldUpdateColWidthOffset = plugin.Config.WorldUpdateColWidthOffset;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt2($"px{suffix}WorldUpdateColWidthOffset", ref WorldUpdateColWidthOffset[0]))
        {
            plugin.Config.WorldUpdateColWidthOffset = WorldUpdateColWidthOffset;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("The width of the columns of the last updated time for each world on the right bottom. Try changing the two values yourself and you'll know what it does.");


        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));


        // ----------------- Cache -----------------
        suffix = $"###{plugin.Name}[Cache]";
        ImGui.TextColored(plugin.UiHelper.ColourKhaki, "Cache");
        ImGui.Separator();


        // selectedWorld
        ImGui.Text("Currently selected world: ");
        ImGui.SameLine();
        var selectedWorld = plugin.Config.selectedWorld;
        ImGui.SetNextItemWidth(150);
        ImGui.Text(plugin.Config.selectedWorld);
        // if (ImGui.InputText($"{suffix}selectedWorld", ref selectedWorld, 100, ImGuiInputTextFlags.ReadOnly))
        // {
        //     selectedWorld = plugin.Config.selectedWorld;
        // }
        ImGuiComponents.HelpMarker("The world you selected from the drop down menu.");

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));

    }
}
