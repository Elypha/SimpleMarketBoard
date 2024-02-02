#pragma warning disable CS8602

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

    private Plugin plugin;

    public ConfigWindow(Plugin plugin) : base(
        "SimpleMarketBoard Configuration"
    // ImGuiWindowFlags.NoResize |
    // ImGuiWindowFlags.NoCollapse |
    // ImGuiWindowFlags.NoScrollbar |
    // ImGuiWindowFlags.NoScrollWithMouse
    )
    {
        Size = new Vector2(400, 300);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
    }

    private string? currHotkeyBoxName;
    private string? currHotkeyName;
    public List<VirtualKey> NewHotkey = new();
    private ulong playerId = 0;
    public List<(string, string)> worldList = new List<(string, string)>();

    public void Dispose()
    {

    }

    public override void OnOpen()
    {
    }

    public void UpdateWorld()
    {
        Service.PluginLog.Debug($"[Config] Update player's current world");
        if (Service.ClientState.LocalContentId != 0 && playerId != Service.ClientState.LocalContentId)
        {
            var localPlayer = Service.ClientState.LocalPlayer;
            if (localPlayer == null) return;

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

    public override void OnClose()
    {
        plugin.Config.Save();
    }

    public override void Draw()
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        var padding = 0.8f;
        var fontsize = ImGui.GetFontSize();
        var titleColour = new Vector4(0.9f, 0.7f, 0.55f, 1);


        var suffix = $"###{plugin.Name}-";


        if (ImGui.CollapsingHeader("Features & UI Introduction", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextColored(titleColour, "- Use the collapse bar above to show/hide the content below. -");

            plugin.ImGuiHelper.BulletTextList(
                "Item Icon",
                "on the top left corner",
                [
                    "· Click: Copy the item name to clipboard.",
                ]
            );

            plugin.ImGuiHelper.BulletTextList(
                "Item Name",
                "on the top left corner",
                [
                    "This place is also used to display the status of the market data query request.\n" +
                    "If it says 'timedout' or 'failed', just use the Refresh button and usually it will be fine.",
                ]
            );

            plugin.ImGuiHelper.BulletTextList(
                "Refresh Button",
                "the two-arrow button under the item name",
                new List<string> {
                    "· Click: (Force) Refresh the market data of the current item.",
                }
            );

            plugin.ImGuiHelper.BulletTextList(
                "HQ Filter Button",
                "the star button under the item name",
                new List<string> {
                    "HQ items are coloured in yellow in the market data table.",
                    "· Click: Toggle whether to show HQ items only in your current listings.",
                }
            );

            plugin.ImGuiHelper.BulletTextList(
                "History Panel",
                "on the right side, the 1st button from left",
                new List<string> {
                    "Items you searched are stored in the cache. If this button is on (by default) you will see a list under it. You can click in the list to quick review them without having to wait for the request again.\n" +
                    "I'm still planning what to present in that area when this panel is switched off.\n" +
                    "To refresh the data, use the Refresh button.\n" +
                    "To remove from the history, use the Remove button.",
                }
            );

            plugin.ImGuiHelper.BulletTextList(
                "Delete Button",
                "on the right side, the 2nd button from left",
                new List<string> {
                    "· Click: Remove the current item from the history panel.",
                    "· Ctrl + Click: Remove all items from the history panel.",
                }
            );

            plugin.ImGuiHelper.BulletTextList(
                "Config Button",
                "on the right side, the 3rd button from left",
                new List<string> {
                    "· Click: Toggle whether to show the configuration window.",
                }
            );

            plugin.ImGuiHelper.BulletTextList(
                "Market Data Table",
                "on the left bottom",
                new List<string> {
                    "· Selling and history prices are tax excluded (i.e., the price as you see in the game).",
                    "This serves trading and comparison purposes. There's an option to include tax in the total price which gives you a rough idea of how much you will pay.",
                    "· The price can be coloured in red if it's higher than the vendor price from NPC, and in green if it's lower (both via options).",
                }
            );

            plugin.ImGuiHelper.BulletTextList(
                "Popularity",
                "on the right side, the one-line text in between the two tables",
                new List<string> {
                    "· This number incicates how many items are sold in a certain period of time.",
                    "It is calculated based on the data per request, so theoretically the more data you require, the more accurate it is. To help reduce the impact of the API server this plugin limits the size to 75. Therefore, for popular items this number may be sampled from a much shorter period of time and thus this number is not proportional across items. However, it's still true that the higher the number, the more popular the item is.",
                }
            );

            plugin.ImGuiHelper.BulletTextList(
                "Update Information per World",
                "on the right side, the table under the Popularity",
                new List<string> {
                    "· The table shows how long ago the market data was updated for each world in hours.",
                    "This is to help you understand how fresh the data is. You can check any world on your decision but you are always encouraged to visit the least updated worlds so that the public data can get updated which benefits us all.",
                }
            );

            ImGui.Text("");

            var _ending_width = ImGui.GetContentRegionAvail().X;
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + _ending_width * 0.9f);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + _ending_width * 0.1f);
            ImGui.TextColored(titleColour, "This plugin is still in active development.\nIf you have any suggestions or issues, please let me know on Discord or GitHub, and I will appreciate it.\nElypha.");
            ImGui.PopTextWrapPos();

            ImGui.Text("");

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize("- END -").X / 2);
            ImGui.TextColored(titleColour, "- END -");
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (4 * ImGui.GetTextLineHeight()));
        }

        // ----------------- General -----------------
        ImGui.TextColored(titleColour, "General");
        ImGui.Separator();


        // HoverDelayIn100MS
        ImGui.Text("Hover delay");
        ImGui.SameLine();

        var HoverDelayIn100MS = plugin.Config.HoverDelayIn100MS;
        ImGui.SetNextItemWidth(200 * scale);
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
        ImGui.SetNextItemWidth(200 * scale);
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
        ImGui.TextColored(titleColour, "Key bindings");
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

        ImGui.SetNextItemWidth(150 * scale);
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
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));


        // ----------------- API -----------------
        ImGui.TextColored(titleColour, "API settings");
        ImGui.Separator();


        // RequestTimeout
        ImGui.Text("Request timeout");
        ImGui.SameLine();
        var RequestTimeout = plugin.Config.RequestTimeout;
        ImGui.SetNextItemWidth(150 * scale);
        if (ImGui.InputInt($"seconds{suffix}RequestTimeout", ref RequestTimeout))
        {
            plugin.Config.RequestTimeout = RequestTimeout;
            plugin.Universalis.ReloadHttpClient();
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("How long to wait in seconds, before the plugin gives up on the market data query.");


        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));


        // ----------------- Message -----------------
        ImGui.TextColored(titleColour, "UI settings");
        ImGui.Separator();


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


        // ChatLogChannel
        ImGui.Text("Channel");
        ImGui.SameLine();
        var ChatLogChannel = plugin.Config.ChatLogChannel;
        // ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 3);
        ImGui.SetNextItemWidth(200 * scale);
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

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));


        // ----------------- Cache -----------------
        ImGui.TextColored(titleColour, "Cache");
        ImGui.Separator();


        // selectedWorld
        ImGui.Text("Currently selected world: ");
        ImGui.SameLine();
        var selectedWorld = plugin.Config.selectedWorld;
        ImGui.SetNextItemWidth(150 * scale);
        ImGui.Text(plugin.Config.selectedWorld);
        // if (ImGui.InputText($"{suffix}selectedWorld", ref selectedWorld, 100, ImGuiInputTextFlags.ReadOnly))
        // {
        //     selectedWorld = plugin.Config.selectedWorld;
        // }
        ImGuiComponents.HelpMarker("The world you selected from the drop down menu.");

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));

    }
}
