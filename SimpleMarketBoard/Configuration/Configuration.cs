using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Miosuke.Configuration;
using SimpleMarketBoard.Modules;


namespace SimpleMarketBoard.Configuration;

public class SimpleMarketBoardConfig : IMioConfig
{
    public int Version = 1;


    // -------------------------------- General --------------------------------
    // search
    public int HoverDelayMs = 0;
    public bool SearchHotkeyEnabled = true;
    public VirtualKey[] SearchHotkey = [VirtualKey.TAB];
    public bool SearchHotkeyLoose = true;
    public bool SearchHotkeyCanHide = true;
    public bool ShowWindowOnSearch = true;
    public bool HoverBackgroundSearchEnabled = true;
    public bool HotkeyBackgroundSearchEnabled = true;

    // data window
    public bool WindowHotkeyEnabled = false;
    public VirtualKey[] WindowHotkey = [VirtualKey.CONTROL, VirtualKey.X];
    public bool WindowHotkeyCanShow = true;
    public bool WindowHotkeyCanHide = true;

    // worlds
    public bool OverridePlayerHomeWorld = false;
    public string PlayerHomeWorld = "";
    public List<string> AdditionalWorlds = new List<string>();

    // notification
    public PriceChecker.PriceToPrint priceToPrint = PriceChecker.PriceToPrint.SoldLow;
    public bool EnableChatLog = true;
    public XivChatType ChatLogChannel = XivChatType.None;
    public bool EnableToastLog = false;


    // -------------------------------- Data --------------------------------
    public bool TotalIncludeTax = true;
    public bool MarkHigherThanVendor = true;

    // cache
    public int MaxCacheItems = 30;
    public bool CleanCacheASAP = true;

    // universalis
    public int RequestTimeout = 20;
    public int UniversalisListings = 70;
    public int UniversalisEntries = 70;


    // -------------------------------- UI --------------------------------
    public bool EnableTheme = true;
    public string CustomTheme = "";
    public bool NumbersAlignRight = true;
    public float NumbersAlignRightOffset = -4.0f;
    public bool EnableRecentHistory = true;
    public int soldTableOffset = 25;
    public float spaceBetweenTables = 0;

    // position offset
    public float WorldComboWidth = 130.0f;
    public float tableRowHeightOffset = -2.0f;
    public int[] sellingColWidthOffset = [0, 0, 0, 0];
    public int[] soldColWidthOffset = [0, 0, 0, 0];
    public int rightColWidth = 102;
    public int[] WorldUpdateColWidthOffset = [4, 0];
    public int[] WorldUpdateColPaddingOffset = [-2, -2];
    public float[] ButtonSizeOffset = [24.0f, 0.0f];


    // -------------------------------- Internal --------------------------------
    // HQ filter
    public bool FilterHq = false;
    public bool UniversalisHqOnly = false;
    // world
    public string selectedWorld = "";

}
