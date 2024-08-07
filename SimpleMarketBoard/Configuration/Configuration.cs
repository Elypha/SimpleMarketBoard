﻿using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using System.Collections.Generic;
using System;


namespace SimpleMarketBoard;

[Serializable]
public class SimpleMarketBoardConfig : IPluginConfiguration
{
    public int Version { get; set; } = 0;


    // -------------------------------- General --------------------------------
    public int HoverDelayIn100MS { get; set; } = 10;
    public int MaxCacheItems { get; set; } = 30;
    public bool EnableRecentHistory { get; set; } = true;
    public bool TotalIncludeTax { get; set; } = true;
    public bool MarkHigherThanVendor { get; set; } = false;
    public bool CleanCacheAsYouGo { get; set; } = true;


    // -------------------------------- Keybinding --------------------------------
    public bool KeybindingEnabled { get; set; } = true;
    public bool KeybindingLooseEnabled { get; set; } = false;
    public bool AllowKeybindingAfterHover { get; set; } = true;
    public VirtualKey[] BindingHotkey { get; set; } = new VirtualKey[] { VirtualKey.CONTROL, VirtualKey.X };
    public bool KeybindingToOpenWindow { get; set; } = false;
    public bool KeybindingToCloseWindow { get; set; } = false;


    // -------------------------------- API --------------------------------
    public int RequestTimeout { get; set; } = 20;
    public bool UniversalisHqOnly { get; set; } = false;
    public int UniversalisListings { get; set; } = 70;
    public int UniversalisEntries { get; set; } = 70;


    // -------------------------------- UI --------------------------------
    public bool EnableTheme { get; set; } = true;
    public bool EnableSearchFromClipboard { get; set; } = false;
    public bool EnableChatLog { get; set; } = true;
    public bool EnableToastLog { get; set; } = false;
    public PriceChecker.PriceToPrint priceToPrint { get; set; } = PriceChecker.PriceToPrint.UniversalisAverage;
    public XivChatType ChatLogChannel { get; set; } = XivChatType.None;
    public int rightColWidth { get; set; } = 102;
    public int[] WorldUpdateColWidthOffset { get; set; } = new int[] { 0, 0 };


    // -------------------------------- Cache --------------------------------
    public List<ulong> SearchHistoryId { get; set; } = new List<ulong>();
    public string selectedWorld { get; set; } = "";

    public bool FilterHq { get; set; } = false;


    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        this.pluginInterface!.SavePluginConfig(this);
    }
}
