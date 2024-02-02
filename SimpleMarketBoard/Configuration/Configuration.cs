using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using Dalamud.Game.ClientState.Keys;
using System.Collections.Generic;
using Dalamud.Game.Text;

namespace SimpleMarketBoard;

[Serializable]
public class SimpleMarketBoardConfig : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // ----------------- General -----------------
    public int HoverDelayIn100MS { get; set; } = 10;
    public int MaxCacheItems { get; set; } = 30;

    public bool EnableRecentHistory { get; set; } = true;
    public bool TotalIncludeTax { get; set; } = true;
    public bool CleanCacheAsYouGo { get; set; } = true;

    // ----------------- Keybinding -----------------
    public bool KeybindingEnabled { get; set; } = true;
    public bool AllowKeybindingAfterHover { get; set; } = true;
    public VirtualKey[] BindingHotkey { get; set; } = new VirtualKey[] { VirtualKey.CONTROL, VirtualKey.X };

    // ----------------- API -----------------
    public int RequestTimeout { get; set; } = 10;


    // ----------------- Message -----------------
    public bool EnableChatLog { get; set; } = true;
    public bool EnableToastLog { get; set; } = false;
    public XivChatType ChatLogChannel { get; set; } = XivChatType.None;

    // ----------------- Cache -----------------
    public List<ulong> SearchHistoryId { get; set; } = new List<ulong>();
    public string selectedWorld { get; set; } = "";

    public bool FilterHQ { get; set; } = false;

    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private DalamudPluginInterface? pluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        this.pluginInterface!.SavePluginConfig(this);
    }
}
