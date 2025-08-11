using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Interface.Components;
using SimpleMarketBoard.Assets;
using Miosuke.UiHelper;
using Dalamud.Interface.Style;
using Dalamud.Interface.ImGuiNotification;
using Miosuke.Configuration;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using Dalamud.Bindings.ImGui;
using SimpleMarketBoard.Modules;
using Miosuke.Extensions;


namespace SimpleMarketBoard.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly HotkeyUi search_hotkey_helper;
    private readonly HotkeyUi window_hotkey_helper;
    private string newAdditionalWorld = "";

    public ConfigWindow() : base(
        "SimpleMarketBoard Configuration"
    )
    {
        Size = new Vector2(450, 600);
        SizeCondition = ImGuiCond.FirstUseEver;

        search_hotkey_helper = new HotkeyUi();
        window_hotkey_helper = new HotkeyUi();
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
    }

    public override void OnClose()
    {
        P.Config.Save();
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

        // Debug
#if DEBUG
        ImGui.TextColored(Ui.ColourCrimson, "Debug");
        if (ImGui.Button("Do test"))
        {
            for (var i = 0; i < 10; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Miosuke.Action.Hotkey.IsActive(P.Config.SearchHotkey, true);
                watch.Stop();
                Service.Log.Info($"[Hotkey] Test result: {watch.Elapsed}");
            }
        }
#endif

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
        // var table_width = ImGui.GetWindowSize().X;
        // var table_height = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y * 2;
        // var col_name_width = ImGui.CalcTextSize("　Action name translation　").X + 2 * ImGui.GetStyle().ItemSpacing.X;
        // var col_value_width = 150.0f;
        // var col_value_content_width = 120.0f;
        var suffix = $"###{Name}[General]";
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding * ImGui.GetTextLineHeight());
        ImGui.TextColored(Ui.ColourAccentLightAlt, "General");
        ImGui.Separator();



        ImGui.TextColored(Ui.ColourCyan, "Search");

        // HoverDelayMs
        ImGui.Text("Hover delay");
        ImGuiComponents.HelpMarker("you hover over an item > wait for this time > plugin starts to fetch the market data.");
        ImGui.SameLine();

        var HoverDelayMs = P.Config.HoverDelayMs;
        ImGui.SetNextItemWidth(250);
        if (ImGui.SliderInt($"{suffix}HoverDelayMS", ref HoverDelayMs, 0, 2000, $"{HoverDelayMs} ms"))
        {
            P.Config.HoverDelayMs = HoverDelayMs;
            P.Config.Save();
        }

        // SearchHotkeyEnabled
        var SearchHotkeyEnabled = P.Config.SearchHotkeyEnabled;
        if (ImGui.Checkbox($"Require a search hotkey{suffix}SearchHotkeyEnabled", ref SearchHotkeyEnabled))
        {
            P.Config.SearchHotkeyEnabled = SearchHotkeyEnabled;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: You need to press the hotkey before you hover over an item to get the market data."
        );
        ImGui.SameLine();
        var SearchHotkey = P.Config.SearchHotkey;
        if (search_hotkey_helper.DrawConfigUi("SearchHotkey", ref SearchHotkey, 100))
        {
            P.Config.SearchHotkey = SearchHotkey;
            P.Config.Save();
        }

        // SearchHotkeyLoose
        ImGui.Text("┗");
        ImGui.SameLine();
        var SearchHotkeyLoose = P.Config.SearchHotkeyLoose;
        if (ImGui.Checkbox($"Loose match{suffix}SearchHotkeyLoose", ref SearchHotkeyLoose))
        {
            P.Config.SearchHotkeyLoose = SearchHotkeyLoose;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: The hotkey will work no matter other keys are pressed or not. For example, you can trigger the hotkey while you are pressing W (to walk).\n" +
            "Disable: Your hotkey is checked strictly. It won't work if any extra keys are pressed."
        );

        // SearchHotkeyCanHide
        ImGui.Text("┗");
        ImGui.SameLine();
        var SearchHotkeyCanHide = P.Config.SearchHotkeyCanHide;
        if (ImGui.Checkbox($"... to hide the window{suffix}SearchHotkeyCanHide", ref SearchHotkeyCanHide))
        {
            P.Config.SearchHotkeyCanHide = SearchHotkeyCanHide;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: the hotkey will also hide the main window, if it's already shown and you are not hovering over an item.\n" +
            "Disable: the hotkey will not be able to hide the main window."
        );

        // ShowWindowOnSearch
        var ShowWindowOnSearch = P.Config.ShowWindowOnSearch;
        if (ImGui.Checkbox($"Show main window on search{suffix}ShowWindowOnSearch", ref ShowWindowOnSearch))
        {
            P.Config.ShowWindowOnSearch = ShowWindowOnSearch;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: The main window will show up, if it's not already shown, when you trigger a search."
        );

        // HotkeyBackgroundSearchEnabled
        var HotkeyBackgroundSearchEnabled = P.Config.HotkeyBackgroundSearchEnabled;
        if (ImGui.Checkbox($"Hotkey search can start from background{suffix}HotkeyBackgroundSearchEnabled", ref HotkeyBackgroundSearchEnabled))
        {
            P.Config.HotkeyBackgroundSearchEnabled = HotkeyBackgroundSearchEnabled;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: You can trigger a search even when the main window is not shown, by pressing the hotkey and, if configured, waiting for the hover delay."
        );

        // HoverBackgroundSearchEnabled
        var HoverBackgroundSearchEnabled = P.Config.HoverBackgroundSearchEnabled;
        if (ImGui.Checkbox($"Non-hotkey search can start from background{suffix}HoverBackgroundSearchEnabled", ref HoverBackgroundSearchEnabled))
        {
            P.Config.HoverBackgroundSearchEnabled = HoverBackgroundSearchEnabled;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: You can trigger a search even when the main window is not shown, by hovering over an item and waiting for the hover delay, if hotkey is not required."
        );



        ImGui.TextColored(Ui.ColourCyan, "Data window");

        // WindowHotkeyEnabled
        var WindowHotkeyEnabled = P.Config.WindowHotkeyEnabled;
        if (ImGui.Checkbox($"Use a window hotkey{suffix}WindowHotkeyEnabled", ref WindowHotkeyEnabled))
        {
            P.Config.WindowHotkeyEnabled = WindowHotkeyEnabled;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: You can use a hotkey to show/hide the plugin window. Unlike the one above, this works in any situation."
        );
        ImGui.SameLine();
        var WindowHotkey = P.Config.WindowHotkey;
        if (window_hotkey_helper.DrawConfigUi("WindowHotkey", ref WindowHotkey, 100))
        {
            P.Config.WindowHotkey = WindowHotkey;
            P.Config.Save();
        }

        // WindowHotkeyCanShow
        ImGui.Text("┗");
        ImGui.SameLine();
        var WindowHotkeyCanShow = P.Config.WindowHotkeyCanShow;
        if (ImGui.Checkbox($"... to show the window{suffix}WindowHotkeyCanShow", ref WindowHotkeyCanShow))
        {
            P.Config.WindowHotkeyCanShow = WindowHotkeyCanShow;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: the hotkey will bring up the main window, if it's not already shown.\n" +
            "Disable: the hotkey will not be able to show the main window."
        );

        // WindowHotkeyCanHide
        ImGui.Text("┗");
        ImGui.SameLine();
        var WindowHotkeyCanHide = P.Config.WindowHotkeyCanHide;
        if (ImGui.Checkbox($"... to hide the window{suffix}WindowHotkeyCanHide", ref WindowHotkeyCanHide))
        {
            P.Config.WindowHotkeyCanHide = WindowHotkeyCanHide;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: the hotkey will hide the main window, if it's already shown.\n" +
            "Disable: the hotkey will not be able to hide the main window."
        );



        ImGui.TextColored(Ui.ColourCyan, "Worlds");

        // OverridePlayerHomeWorld
        var OverridePlayerHomeWorld = P.Config.OverridePlayerHomeWorld;
        if (ImGui.Checkbox($"Override player home world to {suffix}OverridePlayerHomeWorld", ref OverridePlayerHomeWorld))
        {
            P.Config.OverridePlayerHomeWorld = OverridePlayerHomeWorld;
            P.MainWindow.UpdateWorld();
        }
        ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
        var PlayerHomeWorld = P.Config.PlayerHomeWorld;
        if (ImGui.InputText($"{suffix}-PlayerHomeWorld", ref PlayerHomeWorld, 32))
        {
            P.Config.PlayerHomeWorld = PlayerHomeWorld;
        }
        ImGui.SameLine();
        if (ImGui.Button("Apply"))
        {
            if (P.Config.OverridePlayerHomeWorld && P.Config.PlayerHomeWorld != "")
            {
                var world = Service.Data.GetExcelSheet<World>()
                    .FirstOrDefault(x => string.Equals(x.Name.ToString(), P.Config.PlayerHomeWorld, StringComparison.OrdinalIgnoreCase));

                if (world.Equals(default(World)))
                {
                    Service.NotificationManager.AddNotification(new Notification
                    {
                        Content = $"World cannot be determined from world name: {P.Config.PlayerHomeWorld}",
                        Type = NotificationType.Error,
                    });
                    P.Config.OverridePlayerHomeWorld = false;
                    P.Config.PlayerHomeWorld = "";
                }
                else
                {
                    P.Config.PlayerHomeWorld = world.Name.ToString();
                }
                P.Config.Save();
                P.MainWindow.UpdateWorld();
            }
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(
            "Your home world is used when populate the target world drop down menu.\n" +
            "Enable: The plugin will use the world you set below as your home world, no matter what your actual home world is.\n" +
            "Disable: The plugin will use the world your current character is in as your home world, and this gets updated when you login and change maps.\n" +
            "Fill the WORLD you want to always use as your home world. Use its full name in game, e.g., Hades, then click 'Apply'."
        );


        // AdditionalWorlds
        ImGui.Text("Additional Worlds/DCs/Regions");
        ImGuiComponents.HelpMarker(
            "Use this to add extra options to the target world dropdown menu to search price in.\n" +
            "To add a world or datacentre: fill its full name in game, e.g., Hades, Mana\n" +
            "To add a region: fill a name supported by Universalis, at the time of writing there are: Japan, North-America, Europe, Oceania\n" +
            "Your manually added worlds will be denoted by a star (*) in the dropdown menu."
        );
        for (var i = 0; i < P.Config.AdditionalWorlds.Count; i++)
        {
            ImGui.PushID($"{suffix}-AdditionalWorlds-{i}");
            var additionalWorld = P.Config.AdditionalWorlds[i];
            ImGui.SetNextItemWidth(180);
            if (ImGui.InputText($"{suffix}-AdditionalWorlds", ref additionalWorld, 32))
            {
                P.Config.AdditionalWorlds[i] = additionalWorld;
                P.Config.Save();
                P.MainWindow.UpdateWorld();
            }
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}{suffix}-del"))
            {
                P.Config.AdditionalWorlds.RemoveAt(i--);
                P.Config.Save();
                P.MainWindow.UpdateWorld();
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
            P.Config.AdditionalWorlds.Add(newAdditionalWorld);
            P.Config.Save();
            P.MainWindow.UpdateWorld();
            newAdditionalWorld = "";
        }
        ImGui.PopFont();



        ImGui.TextColored(Ui.ColourCyan, "Notification");

        // priceToPrint
        ImGui.Text("Use the price of");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        if (ImGui.BeginCombo($"{suffix}priceToPrint", P.Config.priceToPrint.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(PriceChecker.PriceToPrint)).Cast<PriceChecker.PriceToPrint>())
            {
                if (ImGui.Selectable(type.ToString(), type == P.Config.priceToPrint))
                {
                    P.Config.priceToPrint = type;
                    P.Config.Save();
                }
            }

            ImGui.EndCombo();
        }
        ImGuiComponents.HelpMarker(
            "Which price you want to use for the print.\n" +
            "PS UniversalisAverage is directly from Universalis by their algorithm."
        );

        // EnableChatLog
        var EnableChatLog = P.Config.EnableChatLog;
        if (ImGui.Checkbox($"Print to Chat log{suffix}EnableChatLog", ref EnableChatLog))
        {
            P.Config.EnableChatLog = EnableChatLog;
            P.Config.Save();
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
        var ChatLogChannel = P.Config.ChatLogChannel;
        ImGui.SetNextItemWidth(200);
        if (ImGui.BeginCombo($"{suffix}ChatLogChannel", ChatLogChannel.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(XivChatType)).Cast<XivChatType>())
            {
                if (ImGui.Selectable(type.ToString(), type == ChatLogChannel))
                {
                    P.Config.ChatLogChannel = type;
                    P.Config.Save();
                }
            }
            ImGui.EndCombo();
        }
        ImGuiComponents.HelpMarker(
            "The chat channel to print the result.\n" +
            "PS 'None' is also a name of a channel."
        );


        // EnableToastLog
        var EnableToastLog = P.Config.EnableToastLog;
        if (ImGui.Checkbox($"Print to Toast{suffix}EnableToastLog", ref EnableToastLog))
        {
            P.Config.EnableToastLog = EnableToastLog;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: The result will print in common toast."
        );

    }


    private void DrawData(float padding)
    {
        // setup
        var table_width = ImGui.GetWindowSize().X;
        var table_height = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y * 2;
        var col_name_width = ImGui.CalcTextSize("　Selling records numbers　").X + 2 * ImGui.GetStyle().ItemSpacing.X;
        var col_value_width = 150.0f;
        var col_value_content_width = 120.0f;
        var suffix = $"###{Name}[Data]";
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding * ImGui.GetTextLineHeight());
        ImGui.TextColored(Ui.ColourAccentLightAlt, "Data");
        ImGui.Separator();


        // TotalIncludeTax
        var TotalIncludeTax = P.Config.TotalIncludeTax;
        if (ImGui.Checkbox($"Include tax in total price{suffix}TotalIncludeTax", ref TotalIncludeTax))
        {
            P.Config.TotalIncludeTax = TotalIncludeTax;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Total price will include tax, i.e., how much you will pay in fact.\n" +
            "Disable: Total price will simply be amount*price_per_unit."
        );


        // MarkHigherThanVendor
        var MarkHigherThanVendor = P.Config.MarkHigherThanVendor;
        if (ImGui.Checkbox($"Red if higher than vendor{suffix}MarkHigherThanVendor", ref MarkHigherThanVendor))
        {
            P.Config.MarkHigherThanVendor = MarkHigherThanVendor;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: A record will be in red if the price is higher than vendor NPC."
        );

        ImGui.TextColored(Ui.ColourCyan, "Cache");

        // MaxCacheItems
        ImGui.Text("Cached items");
        ImGuiComponents.HelpMarker(
            "How many items you want to keep in cache.\n" +
            "Cache clean will run when you close the window, and items more than this number will be removed from the oldest."
        );
        ImGui.SameLine();

        var MaxCacheItems = P.Config.MaxCacheItems;
        ImGui.SetNextItemWidth(250);
        if (ImGui.SliderInt($"{suffix}MaxCacheItems", ref MaxCacheItems, 1, 50, $"{MaxCacheItems} items"))
        {
            P.Config.MaxCacheItems = MaxCacheItems;
            P.Config.Save();
        }

        // CleanCacheASAP
        var CleanCacheASAP = P.Config.CleanCacheASAP;
        if (ImGui.Checkbox($"Clean cache ASAP{suffix}CleanCacheASAP", ref CleanCacheASAP))
        {
            P.Config.CleanCacheASAP = CleanCacheASAP;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Cache clean will also run immediately when a new item is added."
        );

        ImGui.TextColored(Ui.ColourCyan, "Universalis");


        ImGui.BeginChild("table DrawData Universalis", new Vector2(table_width, table_height * 3), false);
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, col_name_width);
        ImGui.SetColumnWidth(1, col_value_width);

        // RequestTimeout
        ImGui.TextColored(Ui.ColourWhiteDim, "　Request timeout");
        ImGui.NextColumn();
        var RequestTimeout = P.Config.RequestTimeout;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}RequestTimeout", ref RequestTimeout))
        {
            P.Config.RequestTimeout = RequestTimeout;
            P.Universalis.ReloadHttpClient();
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "How long to wait in seconds, before the plugin gives up a web request if it's not finished.\n" +
            "Search on the whole region (e.g., Japan) or during peak times can take notably longer time. Please use a reasonable value.\n" +
            "Default: 20."
        );
        ImGui.NextColumn();

        // UniversalisListings
        ImGui.TextColored(Ui.ColourWhiteDim, "　Selling records numbers");
        ImGui.NextColumn();
        var UniversalisListings = P.Config.UniversalisListings;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}UniversalisListings", ref UniversalisListings))
        {
            P.Config.UniversalisListings = UniversalisListings;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "How many lines of selling records to request from Universalis.\n" +
            "More results take longer time to search. Please use a reasonable value.\n" +
            "Default: 70."
        );
        ImGui.NextColumn();

        // UniversalisEntries
        ImGui.TextColored(Ui.ColourWhiteDim, "　Sold records numbers");
        ImGui.NextColumn();
        var UniversalisEntries = P.Config.UniversalisEntries;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}UniversalisEntries", ref UniversalisEntries))
        {
            P.Config.UniversalisEntries = UniversalisEntries;
            P.Config.Save();
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
        var table_width = ImGui.GetWindowSize().X;
        var table_height = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y * 2;
        var col_name_width = ImGui.CalcTextSize("　Selling records numbers　").X + 2 * ImGui.GetStyle().ItemSpacing.X;
        var col_value_width = 150.0f;
        var col_value_content_width = 120.0f;
        var suffix = $"###{Name}[UI]";
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding * ImGui.GetTextLineHeight());
        ImGui.TextColored(Ui.ColourAccentLightAlt, "UI");
        ImGui.Separator();


        // EnableTheme
        var EnableTheme = P.Config.EnableTheme;
        if (ImGui.Checkbox($"Use a theme for this plugin{suffix}EnableTheme", ref EnableTheme))
        {
            P.Config.EnableTheme = EnableTheme;
            P.Config.Save();
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
        var CustomTheme = P.Config.CustomTheme;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputText($"{suffix}-CustomTheme", ref CustomTheme, 4096))
        {
            P.Config.CustomTheme = CustomTheme;
            P.Config.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("Apply"))
        {
            try
            {
                var _theme = StyleModel.Deserialize(CustomTheme) ?? throw new ArgumentException("Custom theme failed to deserialize.");
                P.PluginTheme = _theme;
            }
            catch (Exception e)
            {
                P.Config.CustomTheme = "";
                P.Config.Save();
                Service.NotificationManager.AddNotification(new Notification
                {
                    Content = $"Your custom theme is invalid and has been reset: {e.Message}",
                    Type = NotificationType.Error,
                });
            }
        }

        // NumbersAlignRight
        var NumbersAlignRight = P.Config.NumbersAlignRight;
        if (ImGui.Checkbox($"Numbers in table align right{suffix}NumbersAlignRight", ref NumbersAlignRight))
        {
            P.Config.NumbersAlignRight = NumbersAlignRight;
            P.Config.Save();
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
        var NumbersAlignRightOffset = P.Config.NumbersAlignRightOffset;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat($"{suffix}NumbersAlignRightOffset", ref NumbersAlignRightOffset))
        {
            P.Config.NumbersAlignRightOffset = NumbersAlignRightOffset;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Offset the alignment of numbers in the table. New position X' = X + offset."
        );


        // EnableRecentHistory
        var EnableRecentHistory = P.Config.EnableRecentHistory;
        if (ImGui.Checkbox($"Show table of sold records (history){suffix}EnableRecentHistory", ref EnableRecentHistory))
        {
            P.Config.EnableRecentHistory = EnableRecentHistory;
            P.Config.Save();
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
        var soldTableOffset = P.Config.soldTableOffset;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}soldTableOffset", ref soldTableOffset))
        {
            P.Config.soldTableOffset = soldTableOffset;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "For sold table, the new position Y' = Y + offset."
        );


        // spaceBetweenTables
        ImGui.Text("┗");
        ImGui.SameLine();
        ImGui.Text("with space between tables");
        ImGui.SameLine();
        var spaceBetweenTables = P.Config.spaceBetweenTables;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat($"{suffix}spaceBetweenTables", ref spaceBetweenTables))
        {
            if (spaceBetweenTables < 0) spaceBetweenTables = 0;
            P.Config.spaceBetweenTables = spaceBetweenTables;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "The space between the selling table and the sold table.\n" +
            "0 means no space, and the two tables are sticked together."
        );


        ImGui.TextColored(Ui.ColourCyan, "Position Offset");


        ImGui.BeginChild("table DrawUi Position Offset", new Vector2(table_width, table_height * 8), false);
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, col_name_width);
        ImGui.SetColumnWidth(1, col_value_width);


        // WorldComboWidth
        ImGui.Text("World menu width");
        ImGui.NextColumn();
        var WorldComboWidth = P.Config.WorldComboWidth;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat($"{suffix}WorldComboWidth", ref WorldComboWidth, 0.0f, 0.0f, "%.0f"))
        {
            P.Config.WorldComboWidth = WorldComboWidth;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker("The width of the target world dropdown menu.");
        ImGui.NextColumn();


        // tableRowHeightOffset
        ImGui.Text("Table row height");
        ImGui.NextColumn();
        var tableRowHeightOffset = P.Config.tableRowHeightOffset;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat($"{suffix}tableRowHeightOffset", ref tableRowHeightOffset, 0.0f, 0.0f, "%.0f"))
        {
            P.Config.tableRowHeightOffset = tableRowHeightOffset;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker("Offset the height of each row in the table. New height H' = H + offset.");
        ImGui.NextColumn();


        // sellingColWidthOffset
        ImGui.Text("Selling column width");
        ImGui.NextColumn();
        var sellingColWidthOffset = P.Config.sellingColWidthOffset.ToVector4();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat4($"{suffix}sellingColWidthOffset", ref sellingColWidthOffset, 0.0f, 0.0f, "%.0f"))
        {
            P.Config.sellingColWidthOffset = sellingColWidthOffset.ToArray();
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker("Offset the width of each column in the table. New width W' = W + offset.");
        ImGui.NextColumn();


        // soldColWidthOffset
        ImGui.Text("Sold column width");
        ImGui.NextColumn();
        var soldColWidthOffset = P.Config.soldColWidthOffset.ToVector4();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat4($"{suffix}soldColWidthOffset", ref soldColWidthOffset, 0.0f, 0.0f, "%.0f"))
        {
            P.Config.soldColWidthOffset = soldColWidthOffset.ToArray();
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker("Offset the width of each column in the table. New width W' = W + offset.");
        ImGui.NextColumn();


        // rightColWidth
        ImGui.Text("Right panel width");
        ImGui.NextColumn();
        var rightColWidth = P.Config.rightColWidth;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}rightColWidth", ref rightColWidth))
        {
            P.Config.rightColWidth = rightColWidth;
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker("The width of the panel on the right of the main window.");
        ImGui.NextColumn();


        // WorldUpdateColWidthOffset
        ImGui.Text("World Updated width");
        ImGui.NextColumn();
        var WorldUpdateColWidthOffset = P.Config.WorldUpdateColWidthOffset.ToVector2();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat2($"{suffix}WorldUpdateColWidthOffset", ref WorldUpdateColWidthOffset, 0.0f, 0.0f, "%.0f"))
        {
            P.Config.WorldUpdateColWidthOffset = WorldUpdateColWidthOffset.ToArray();
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker("The width of the columns of the last updated time for each world on the right bottom. Try changing the values to see what it does.");
        ImGui.NextColumn();


        // WorldUpdateColPaddingOffset
        ImGui.Text("World Updated padding");
        ImGui.NextColumn();
        var WorldUpdateColPaddingOffset = P.Config.WorldUpdateColPaddingOffset.ToVector2();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat2($"{suffix}WorldUpdateColPaddingOffset", ref WorldUpdateColPaddingOffset, 0.0f, 0.0f, "%.0f"))
        {
            P.Config.WorldUpdateColPaddingOffset = WorldUpdateColPaddingOffset.ToArray();
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker("The padding of the columns of the last updated time for each world on the right bottom. Try changing the values to see what it does.");
        ImGui.NextColumn();

        // ButtonSizeOffset
        ImGui.Text("Button size");
        ImGui.NextColumn();
        var ButtonSizeOffset = P.Config.ButtonSizeOffset.ToVector2();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat2($"{suffix}ButtonSizeOffset", ref ButtonSizeOffset, 0.0f, 0.0f, "%.0f"))
        {
            P.Config.ButtonSizeOffset = ButtonSizeOffset.ToArray();
            P.Config.Save();
        }
        ImGuiComponents.HelpMarker("The size of the buttons in the main window. Try changing the values to see what it does.");
        ImGui.NextColumn();

        ImGui.Columns(1);
        ImGui.EndChild();

    }
}
