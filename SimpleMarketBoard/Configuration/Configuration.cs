using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using System.Collections.Generic;
using System;
using System.Numerics;


namespace SimpleMarketBoard;

[Serializable]
public class SimpleMarketBoardConfig : IPluginConfiguration
{
    public int Version { get; set; } = 0;


    // -------------------------------- General --------------------------------
    public int HoverDelayMs { get; set; } = 10;
    public int MaxCacheItems { get; set; } = 30;
    public bool EnableRecentHistory { get; set; } = true;
    public bool TotalIncludeTax { get; set; } = true;
    public bool MarkHigherThanVendor { get; set; } = false;
    public bool CleanCacheASAP { get; set; } = true;


    // -------------------------------- Keybinding --------------------------------
    public bool SearchHotkeyEnabled { get; set; } = true;
    public VirtualKey[] SearchHotkey { get; set; } = [VirtualKey.CONTROL, VirtualKey.X];
    public bool SearchHotkeyAfterHover { get; set; } = true;
    public bool SearchHotkeyLoose { get; set; } = false;
    public bool WindowHotkeyEnabled { get; set; } = true;
    public VirtualKey[] WindowHotkey { get; set; } = [VirtualKey.CONTROL, VirtualKey.X];
    public bool WindowHotkeyCanShow { get; set; } = false;
    public bool WindowHotkeyCanHide { get; set; } = false;


    // -------------------------------- API --------------------------------
    public int RequestTimeout { get; set; } = 20;
    public bool UniversalisHqOnly { get; set; } = false;
    public int UniversalisListings { get; set; } = 70;
    public int UniversalisEntries { get; set; } = 70;


    // -------------------------------- UI --------------------------------
    public bool EnableTheme { get; set; } = true;
    public bool NumbersAlignRight { get; set; } = false;
    public float NumbersAlignRightOffset { get; set; } = -4.0f;
    public bool EnableChatLog { get; set; } = true;
    public bool EnableToastLog { get; set; } = false;
    public PriceChecker.PriceToPrint priceToPrint { get; set; } = PriceChecker.PriceToPrint.UniversalisAverage;
    public XivChatType ChatLogChannel { get; set; } = XivChatType.None;
    public int rightColWidth { get; set; } = 102;
    public int[] WorldUpdateColWidthOffset { get; set; } = [4, 0];
    public int[] WorldUpdateColPaddingOffset { get; set; } = [-2, -2];
    public int soldTableOffset { get; set; } = 0;
    public float tableRowHeightOffset { get; set; } = 0;
    public float spaceBetweenTables { get; set; } = 0;
    public int[] sellingColWidthOffset { get; set; } = [0, 0, 0, 0];
    public int[] soldColWidthOffset { get; set; } = [0, 0, 0, 0];


    // -------------------------------- Cache --------------------------------
    public List<ulong> SearchHistoryId { get; set; } = [];
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
