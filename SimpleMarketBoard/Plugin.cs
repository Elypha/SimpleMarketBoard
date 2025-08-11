#pragma warning disable CS8618

using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Style;
using Dalamud.Plugin.Services;
using Miosuke.Configuration;
using Miosuke.Messages;
using SimpleMarketBoard.API;
using SimpleMarketBoard.Assets;
using SimpleMarketBoard.Configuration;
using SimpleMarketBoard.Modules;
using SimpleMarketBoard.Windows;


namespace SimpleMarketBoard;

public sealed class SimpleMarketBoardPlugin : IDalamudPlugin
{
    public static string Name => "SimpleMarketBoard";
    public static string NameShort => "SMB";
    private const string CommandMain = "/smb";
    private const string CommandMainAlt = "/mb";

    // PLUGIN
    internal static SimpleMarketBoardPlugin P;
    internal SimpleMarketBoardConfig Config;
    public DalamudLinkPayload? PluginPayload;
    public StyleModel PluginTheme { get; set; }
    public bool PluginThemeEnabled { get; set; }

    // MODULES
    public HoveredItem HoveredItem { get; set; } = null!;
    public PriceChecker PriceChecker { get; set; } = null!;
    public Universalis Universalis { get; set; } = null!;

    // WINDOWS
    public ConfigWindow ConfigWindow { get; init; }
    public MainWindow MainWindow { get; init; }
    public WindowSystem WindowSystem = new("SimpleMarketBoard");





    public SimpleMarketBoardPlugin(IDalamudPluginInterface pluginInterface)
    {
        // PLUGIN

        // dalamud service
        Service.Init(pluginInterface);
        // plugin payload
        PluginPayload = Service.Chat.AddChatLinkHandler(1, pluginPayloadHandler);
        // lib
        MiosukeHelper.Init(
            pluginInterface,
            this,
            $"[{NameShort}] ",
            PluginPayload
        );


        // PLUGIN

        // plugin init
        P = this;
        // config init
        MioConfig.Setup(MainConfigFileName: "main.json");
        if (Service.PluginInterface.ConfigFile.Exists) MioConfig.Migrate<SimpleMarketBoardConfig>(Service.PluginInterface.ConfigFile.FullName);
        Config = MioConfig.Init<SimpleMarketBoardConfig>();

        // theme
        ImGuiThemeLoadCustomOrDefault();

        // command handlers
        Service.Commands.AddHandler(CommandMain, new CommandInfo(OnCommandMain)
        {
            HelpMessage = "main command entry:\n" +
                "└ /smb → open the main window (market data table).\n" +
                "└ /smb c|config → open the configuration window."
        });
        Service.Commands.AddHandler(CommandMainAlt, new CommandInfo(OnCommandMain)
        {
            HelpMessage = "[ SAME AS ] → /smb"
        });


        // MODULES

        Universalis = new Universalis();
        HoveredItem = new HoveredItem();
        PriceChecker = new PriceChecker();


        // WINDOWS

        ConfigWindow = new ConfigWindow();
        MainWindow = new MainWindow();
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);


        // HANDLERS

        Service.PluginInterface.UiBuilder.Draw += DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;
        Service.ClientState.Login += OnLogin;
        Service.ClientState.TerritoryChanged += OnTerritoryChanged;
        Service.Framework.Update += OnFrameUpdateWindow;
        Service.Framework.Update += OnFrameUpdateSearch;
        Service.Framework.Update += OnFrameUpdateLocalContent;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        // unload command handlers
        Service.Commands.RemoveHandler(CommandMain);

        // unload modules
        HoveredItem.Dispose();
        PriceChecker.Dispose();
        Universalis.Dispose();

        // unload windows
        ConfigWindow.Dispose();
        MainWindow.Dispose();

        // unload event handlers
        Service.PluginInterface.UiBuilder.Draw -= DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi -= DrawMainUI;
        Service.ClientState.Login -= OnLogin;
        Service.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Service.Framework.Update -= OnFrameUpdateWindow;
        Service.Framework.Update -= OnFrameUpdateSearch;
        Service.Framework.Update -= OnFrameUpdateLocalContent;
    }

    private void pluginPayloadHandler(uint id, SeString text)
    {
        var payload = text.TextValue.Trim();
        if (string.Equals(payload, $"[{NameShort}]", StringComparison.OrdinalIgnoreCase))
        {
            MainWindow.Toggle();
        }
    }

    public void DrawUI()
    {
        WindowSystem.Draw();
    }

    public void DrawMainUI()
    {
        MainWindow.Toggle();
    }

    public void DrawConfigUI()
    {
        ConfigWindow.Toggle();
    }

    public static void ImGuiThemeLoadCustomOrDefault()
    {
        try
        {
            if (P.Config.CustomTheme != "")
            {
                var _theme = StyleModel.Deserialize(P.Config.CustomTheme);
                if (_theme is not null) P.PluginTheme = _theme;
                return;
            }
        }
        catch (Exception e)
        {
            P.Config.CustomTheme = "";
            P.Config.Save();
            Notice.Error($"Your custom theme is invalid and has been reset: {e.Message}");
        }
        finally
        {
            P.PluginTheme = Data.defaultTheme;
        }
    }


    public void OnCommandMain(string command, string args)
    {
        if (args == "c" || args == "config")
        {
            ConfigWindow.Toggle();
            return;
        }
        else
        {
            MainWindow.Toggle();
        }
    }

    public void OnLogin()
    {
        MainWindow.UpdateWorld();
    }

    public void OnTerritoryChanged(ushort territoryId)
    {
        MainWindow.UpdateWorld();
    }

    private bool windowHotkeyHandled = false;
    public void OnFrameUpdateWindow(IFramework framework)
    {
        if (!Config.WindowHotkeyEnabled) return;
        if (!Miosuke.Action.Hotkey.IsActive(Config.WindowHotkey, true))
        {
            windowHotkeyHandled = false;
            return;
        }

        if (!windowHotkeyHandled)
        {
            if (Config.WindowHotkeyCanShow && !MainWindow.IsOpen)
            {
                windowHotkeyHandled = true;
                MainWindow.IsOpen = true;
            }
            else if (Config.WindowHotkeyCanHide && MainWindow.IsOpen)
            {
                windowHotkeyHandled = true;
                MainWindow.IsOpen = false;
            }
        }
    }

    private bool searchHotkeyHandled = false;
    public void OnFrameUpdateSearch(IFramework framework)
    {
        if (!Config.SearchHotkeyEnabled) return;
        if (!Miosuke.Action.Hotkey.IsActive(Config.SearchHotkey, !Config.SearchHotkeyLoose))
        {
            searchHotkeyHandled = false;
            return;
        }

        if (!searchHotkeyHandled)
        {
            if (MainWindow.IsOpen)
            {
                if (Config.SearchHotkeyCanHide && (HoveredItem.HoverItemId == 0))
                {
                    searchHotkeyHandled = true;
                    MainWindow.IsOpen = false;
                }
                else if (HoveredItem.SavedItemId != 0)
                {
                    searchHotkeyHandled = true;
                    HoveredItem.CheckItem(HoveredItem.SavedItemId);
                }
            }
            else if (Config.HotkeyBackgroundSearchEnabled && (HoveredItem.HoverItemId != 0))
            {
                searchHotkeyHandled = true;
                HoveredItem.CheckItem(HoveredItem.HoverItemId);
            }
        }
    }


    public ulong LocalContentId = 0;
    public Lumina.Excel.Sheets.World? LocalPlayerHomeWorld = null;
    public Lumina.Excel.Sheets.World? LocalPlayerCurrentWorld = null;
    public bool IsInGame => (LocalContentId != 0) && (LocalPlayerHomeWorld is not null) && (LocalPlayerCurrentWorld is not null);
    public void OnFrameUpdateLocalContent(IFramework framework)
    {
        if (LocalContentId != Service.ClientState.LocalContentId)
        {
            UpdateLocalContentCache();
        }
    }
    public void UpdateLocalContentCache()
    {
        LocalPlayerHomeWorld = Service.ClientState.LocalPlayer?.HomeWorld.Value ?? null;
        LocalPlayerCurrentWorld = Service.ClientState.LocalPlayer?.CurrentWorld.Value ?? null;
        if (LocalPlayerHomeWorld is null || LocalPlayerCurrentWorld is null)
        {
            Service.Log.Debug($"[UpdateLocalContentCache] Reset LocalContentId to 0 because player is not in game.");
            LocalContentId = 0;
            return;
        }

        LocalContentId = Service.ClientState.LocalContentId;
        Service.Log.Debug($"[UpdateLocalContentCache] {LocalContentId}, {LocalPlayerHomeWorld.Value.Name}, {LocalPlayerCurrentWorld.Value.Name}");
    }
}
