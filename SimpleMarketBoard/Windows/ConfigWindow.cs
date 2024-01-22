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
using Dalamud.Plugin.Services;
using System;

namespace SimpleMarketBoard
{
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
            this.Size = new Vector2(400, 300);
            this.SizeCondition = ImGuiCond.FirstUseEver;

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
            Service.PluginLog.Debug($"UpdateWorld");
            if (Service.ClientState.LocalContentId != 0 && this.playerId != Service.ClientState.LocalContentId)
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

                // foreach (var w in this.worldList)
                // {
                //     Service.PluginLog.Debug($"worldList: {w}");
                // }

                // if (this.plugin.Config.CrossDataCenter)
                // {
                //     this.plugin.Config.selectedWorld = 0;
                // }
                // else if (this.plugin.Config.CrossWorld)
                // {
                //     this.selectedWorld = 1;
                // }
                // else
                // {
                //     this.selectedWorld = worldList.FindIndex(w => w.Item1 == localPlayer.CurrentWorld.GameData.Name);
                // }

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
            var titleColour = new Vector4(0.9f, 0.7f, 0.55f, 1);
            var suffix = $"###{plugin.Name}-";


            // ----------------- General -----------------
            ImGui.TextColored(titleColour, "General");
            ImGui.Separator();


            // HoverDelayMS
            ImGui.Text("Hover delay");
            ImGui.SameLine();
            var HoverDelayMS = plugin.Config.HoverDelayMS;
            ImGui.SetNextItemWidth(150 * scale);
            if (ImGui.SliderInt($"ms{suffix}HoverDelayMS", ref HoverDelayMS, 0, 2000))
            {
                plugin.Config.HoverDelayMS = HoverDelayMS;
                plugin.Config.Save();
            }
            ImGuiComponents.HelpMarker("you hover over an item > [wait this time in ms] > start to check and show the market data");

            // MaxCacheItems
            ImGui.Text("Max cached items");
            ImGui.SameLine();
            var MaxCacheItems = plugin.Config.MaxCacheItems;
            ImGui.SetNextItemWidth(150 * scale);
            if (ImGui.InputInt($"{suffix}MaxCacheItems", ref MaxCacheItems))
            {
                plugin.Config.MaxCacheItems = MaxCacheItems;
                plugin.Config.Save();
            }

            // EnableRecentHistory
            var EnableRecentHistory = plugin.Config.EnableRecentHistory;
            if (ImGui.Checkbox($"Show recent history entries{suffix}EnableRecentHistory", ref EnableRecentHistory))
            {
                plugin.Config.EnableRecentHistory = EnableRecentHistory;
                plugin.Config.Save();
            }
            ImGuiComponents.HelpMarker(
                "enable: display both current listings and history entries\n" +
                "disable: display only current listings"
            );


            // TotalIncludeTax
            var TotalIncludeTax = plugin.Config.TotalIncludeTax;
            if (ImGui.Checkbox($"Total price include tax{suffix}TotalIncludeTax", ref TotalIncludeTax))
            {
                plugin.Config.TotalIncludeTax = TotalIncludeTax;
                plugin.Config.Save();
            }
            ImGuiComponents.HelpMarker(
                "enable: Include tax in the total price\n" +
                "disable: Not include tax in the total prices"
            );

            // CleanCacheAsYouGo
            var CleanCacheAsYouGo = plugin.Config.CleanCacheAsYouGo;
            if (ImGui.Checkbox($"Clean cache ASAP{suffix}CleanCacheAsYouGo", ref CleanCacheAsYouGo))
            {
                plugin.Config.CleanCacheAsYouGo = CleanCacheAsYouGo;
                plugin.Config.Save();
            }
            ImGuiComponents.HelpMarker(
                "enable: The oldest cache will be removed when a new one comes in and when you close the window\n" +
                "disable: The oldest cache will only be removed when you close the window"
            );

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));
            // ImGui.Spacing();
            // ImGui.Spacing();


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
                "enable: [press the keybinding] > hover over an item > get the market data\n" +
                "disable: [nothing required] > hover over an item > get the market data"
            );


            // AllowKeybindingAfterHover
            var AllowKeybindingAfterHover = plugin.Config.AllowKeybindingAfterHover;
            if (ImGui.Checkbox($"Allow keybinding after hover{suffix}AllowKeybindingAfterHover", ref AllowKeybindingAfterHover))
            {
                plugin.Config.AllowKeybindingAfterHover = AllowKeybindingAfterHover;
                plugin.Config.Save();
            }
            ImGuiComponents.HelpMarker(
                "enable: this will also work, hover over an item > [press a keybinding] > get the market data\n" +
                "disable: the above will not work"
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
            var RequestTimeoutMS = plugin.Config.RequestTimeoutMS;
            ImGui.SetNextItemWidth(150 * scale);
            if (ImGui.InputInt($"ms{suffix}RequestTimeout", ref RequestTimeoutMS))
            {
                plugin.Config.RequestTimeoutMS = RequestTimeoutMS;
                plugin.Config.Save();
            }

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
                "enable: Market data will print to your game chat (only you can see)\n" +
                "disable: the above will not happen"
            );


            // EnableToastLog
            var EnableToastLog = plugin.Config.EnableToastLog;
            if (ImGui.Checkbox($"Print to Toast{suffix}EnableToastLog", ref EnableToastLog))
            {
                plugin.Config.EnableToastLog = EnableToastLog;
                plugin.Config.Save();
            }
            ImGuiComponents.HelpMarker(
                "enable: You will receive a toast in game about the market data query" +
                "disable: the above will not happen"
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
            ImGuiComponents.HelpMarker("set the chat channel to send messages");

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));


            // ----------------- Cache -----------------
            ImGui.TextColored(titleColour, "Cache");
            ImGui.Separator();


            // selectedWorld
            ImGui.Text("Currently selected world");
            ImGui.SameLine();
            var selectedWorld = plugin.Config.selectedWorld;
            ImGui.SetNextItemWidth(150 * scale);
            if (ImGui.InputText($"{suffix}selectedWorld", ref selectedWorld, 100, ImGuiInputTextFlags.ReadOnly))
            {
                selectedWorld = plugin.Config.selectedWorld;
            }
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));

        }
    }
}