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
    public int Version { get; set; } = 1;


    // -------------------------------- General --------------------------------
    // search
    public int HoverDelayMs { get; set; } = 0;
    public bool SearchHotkeyEnabled { get; set; } = true;
    public VirtualKey[] SearchHotkey { get; set; } = [VirtualKey.TAB];
    public bool SearchHotkeyLoose { get; set; } = true;
    public bool SearchHotkeyCanHide { get; set; } = true;
    public bool ShowWindowOnSearch { get; set; } = true;
    public bool HoverBackgroundSearchEnabled { get; set; } = true;
    public bool HotkeyBackgroundSearchEnabled { get; set; } = true;

    // data window
    public bool WindowHotkeyEnabled { get; set; } = false;
    public VirtualKey[] WindowHotkey { get; set; } = [VirtualKey.CONTROL, VirtualKey.X];
    public bool WindowHotkeyCanShow { get; set; } = true;
    public bool WindowHotkeyCanHide { get; set; } = true;

    // worlds
    public bool OverridePlayerHomeWorld { get; set; } = false;
    public string PlayerHomeWorld { get; set; } = "";
    public List<string> AdditionalWorlds { get; set; } = new List<string>();

    // notification
    public PriceChecker.PriceToPrint priceToPrint { get; set; } = PriceChecker.PriceToPrint.SoldLow;
    public bool EnableChatLog { get; set; } = true;
    public XivChatType ChatLogChannel { get; set; } = XivChatType.None;
    public bool EnableToastLog { get; set; } = false;


    // -------------------------------- Data --------------------------------
    public bool TotalIncludeTax { get; set; } = true;
    public bool MarkHigherThanVendor { get; set; } = true;

    // cache
    public int MaxCacheItems { get; set; } = 30;
    public bool CleanCacheASAP { get; set; } = true;

    // universalis
    public int RequestTimeout { get; set; } = 20;
    public int UniversalisListings { get; set; } = 70;
    public int UniversalisEntries { get; set; } = 70;


    // -------------------------------- UI --------------------------------
    public bool EnableTheme { get; set; } = true;
    public string CustomTheme { get; set; } = "";
    public bool NumbersAlignRight { get; set; } = true;
    public float NumbersAlignRightOffset { get; set; } = -4.0f;
    public bool EnableRecentHistory { get; set; } = true;
    public int soldTableOffset { get; set; } = 25;
    public float spaceBetweenTables { get; set; } = 0;

    // position offset
    public float WorldComboWidth { get; set; } = 130.0f;
    public float tableRowHeightOffset { get; set; } = -2.0f;
    public int[] sellingColWidthOffset { get; set; } = [0, 0, 0, 0];
    public int[] soldColWidthOffset { get; set; } = [0, 0, 0, 0];
    public int rightColWidth { get; set; } = 102;
    public int[] WorldUpdateColWidthOffset { get; set; } = [4, 0];
    public int[] WorldUpdateColPaddingOffset { get; set; } = [-2, -2];
    public float[] ButtonSizeOffset { get; set; } = [24.0f, 0.0f];


    // -------------------------------- Internal --------------------------------
    // HQ filter
    public bool FilterHq { get; set; } = false;
    public bool UniversalisHqOnly { get; set; } = false;
    // world
    public string selectedWorld { get; set; } = "";


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
