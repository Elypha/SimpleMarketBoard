using Dalamud.Game.Command;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Style;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using Lumina.Excel;
using System.Collections.Generic;
using Miosuke;

using SimpleMarketBoard.UniversalisModels;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling.Payloads;


namespace SimpleMarketBoard;

public sealed class Plugin : IDalamudPlugin
{
    // dalamud plugin
    public string Name => "SimpleMarketBoard";
    public string NameShort => "SMB";
    private const string CommandMain = "/smb";
    private const string CommandMainAlt = "/mb";

    // fonts and data resources
    public IFontHandle Axis20 { get; set; }
    public IFontHandle NotoSansJpMedium { get; set; }
    public readonly ExcelSheet<Item> ItemSheet;
    public readonly ExcelSheet<World> WorldSheet;
    public StyleModel PluginTheme { get; set; }
    public bool PluginThemeEnabled { get; set; }

    // configuration
    public SimpleMarketBoardConfig Config { get; init; }

    // windows
    public WindowSystem WindowSystem = new("SimpleMarketBoard");
    public MainWindow MainWindow { get; init; }
    public ConfigWindow ConfigWindow { get; init; }

    // modules
    public Universalis Universalis { get; set; } = null!;
    public HoveredItem HoveredItem { get; set; } = null!;
    public PriceChecker PriceChecker { get; set; } = null!;
    public Miosuke.MiosukeHelper MiosukeHelper { get; set; } = null!;


    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        // load plugin services
        pluginInterface.Create<Service>();
        Service.Log.Info($"[General] Plugin loading...");

        // load configuration
        Config = Service.PluginInterface.GetPluginConfig() as SimpleMarketBoardConfig ?? new SimpleMarketBoardConfig();
        Config.Initialize(Service.PluginInterface);
        if (Config.Version < 1)
        {
            Miosuke.PrintMessage.Chat(
                XivChatType.None,
                $"[{NameShort}] ",
                557,
                [
                    new UIForegroundPayload(39),
                    new TextPayload($"Your current configuration version is outdated due to a refactor of this plugin. Please consider resetting your configuration to start fresh. You can do this in the plugin installer. Find this plugin, and right click > Reset plugin data and reload."),
                    new UIForegroundPayload(0),
                ]);
        }

        // load fonts and data resources
        PluginTheme = StyleModel.Deserialize("DS1H4sIAAAAAAAACq1XS3PbOAz+LzpnMnpQlORbE2+bQ7uTadLpbm+MzdiqFcsry27TTP97CZIgIcr2etLqIojChzdB8CUS0SS5jC+ih2jyEv0TTTh8/KvfPy+iWTRhsDCPJjG8peWKLVd8mSuuRyXjIlrY5aWVWNtv8WCJrxbMLDjTKlbRJIWFxnI9BYYYrnWAZXq1DVZTvboZGQmr/6nf2q5O2ad/bS2wVwtKy0W0s6bs7Y9v9vu7lfXsBOfE+x8H1Qlhl7OBH7NW+fkS3cvvvf2f2P/6/cW+P+u34gfGab0VD42cj7VrgH47wOd6PW+/XS28UUXJsiQrEES+NZh8KyHxZZlmeVoxJep6WTdzKgklINIiQO1tu9ltKG+JzCVyl5Zdy75qu7nsHDvLLDsQxCcTYcN8txTKs7OseduJJ0msSSvLDITmBsLIZ57/pt3LjsaZYaAZWsUsjKDezPp67zcGRxBHEEcQLyCjdd9Q25Kk4KyIuUX5T431n8baEtKTeTGB8qxicVGlGMw85knpjGdVnFTc5WEk67ptGrHZkgC8TtwHud5diY76iDEBwvjFSM3ezTql+mEAOZVex/+ug+aCxiYWAoTGADFWAqAw0wyxQBjvjmKDkOcIBcIUCYVeL+Vs9UF0KwcoMdVAaAAQXldTq2IfeHa0DkNEWIoFlmKBpVgQ3NWu71tsrPFlgY4AobmBcNVuuMPAZVWa5LxERdA5kgS3PE8LZoMB2z6PqzL2osLSZbrWsW8wBH8hxTUWdSMF7SM5bnQgDBI3epY49tE+P9hPVV4cIgwsxhUdpdmQG9GJvj23uTn+14fWdnBGpY0awyvy9FFu6x/yXVf7E7XAAANhigQDnOYDSOhOgZkFwiBL2hc98k+Yfk+2T4rxB8J0f6ykkhve34g9tOe8MmJebflAyqf1Yzvb0T4ccwxeonquelBIwspYPW5jFCkLZAQmJVlu8SYiXIvDOmYp7SvTdraq14vbTu5r6Q/elId7DMQZNzzqr6dN/0yPYNSIKSCKbpu2f1+v5dZvMOxFQJhwJYcAw7zhKEV2WpYHsJt627cLdXI7Xa6iByfNAcQxZRi9wdwGE5vpHfQcRF1AjMKgMXbW6bt2vTh1tOWHge/rxbI/Vfkj3MfhtHji2PXsb5r/m16hZu34eicbOeslnSRPlFAGXWTaicW0azf3olvIY6q8cbBv/hb7G+V7M/T/mB7jv8KYcVnVawg+7lgRIKf1E3ENdxbuUPQrhcGonYvG4M4DqWDA3UeNmdEkks3zZikidRkzdwgxKsRhXIyPvg0WQZ3TUUDd887gOu8GIs+62Dx6593JoCkTAHs2GN7FyNMixoRSmctRGfMDmusRl+ummX4IL15ZQSPMIOxEpFdH65Q21cZLDENdcRpG36BQqRu8CZcf39T0dHB7GD68KXunWWZCOAyOP+k5/DfNI3YWDoKjbtRBWvIY+xOV6Seh0s2+6l6BjT2nqfZHQOkOKUyPlm45Fatqxmr15y+4vrSZwxAAAA==")!;
        if (Config.CustomTheme != "")
        {
            try
            {
                var _PluginTheme = StyleModel.Deserialize(Config.CustomTheme);
                if (_PluginTheme is not null) PluginTheme = _PluginTheme;
            }
            catch (System.Exception e)
            {
                Config.CustomTheme = "";
                Config.Save();
                Service.NotificationManager.AddNotification(new Notification
                {
                    Content = $"Your custom theme is invalid and has been reset: {e.Message}",
                    Type = NotificationType.Error,
                });
            }
        }

        Axis20 = Service.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Axis, 20.0f));
        NotoSansJpMedium = Service.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e =>
        {
            e.OnPreBuild(tk => tk.AddDalamudAssetFont(Dalamud.DalamudAsset.NotoSansJpMedium, new()
            {
                SizePx = 17.0f,
            }));
        });

        ItemSheet = Service.Data.GetExcelSheet<Item>()!;
        WorldSheet = Service.Data.GetExcelSheet<World>()!;

        // load windows
        MainWindow = new MainWindow(this);
        WindowSystem.AddWindow(MainWindow);
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        // load modules
        Universalis = new Universalis(this);
        HoveredItem = new HoveredItem(this);
        PriceChecker = new PriceChecker(this);
        MiosukeHelper = new Miosuke.MiosukeHelper(Service.PluginInterface, this);

        // load event handlers
        Service.PluginInterface.UiBuilder.Draw += DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;
        Service.ClientState.Login += OnLogin;
        Service.ClientState.TerritoryChanged += OnTerritoryChanged;
        Service.Framework.Update += OnFrameUpdateWindow;
        Service.Framework.Update += OnFrameUpdateSearch;
        MainWindow.UpdateWorld();

        // load command handlers
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

        Service.Log.Info($"[General] Plugin initialised");
    }

    public void Dispose()
    {
        // unload command handlers
        Service.Commands.RemoveHandler(CommandMain);

        // unload windows
        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();
        ConfigWindow.Dispose();

        // unload fonts and data resources
        Axis20.Dispose();
        NotoSansJpMedium.Dispose();

        // unload event handlers
        Service.PluginInterface.UiBuilder.Draw -= DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi -= DrawMainUI;
        Service.ClientState.Login -= OnLogin;
        Service.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Service.Framework.Update -= OnFrameUpdateWindow;
        Service.Framework.Update -= OnFrameUpdateSearch;

        // unload modules
        Universalis.Dispose();
        HoveredItem.Dispose();
        PriceChecker.Dispose();
        MiosukeHelper.Dispose();
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
        if (!Miosuke.Hotkey.IsActive(Config.WindowHotkey, true))
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
        if (!Miosuke.Hotkey.IsActive(Config.SearchHotkey, !Config.SearchHotkeyLoose))
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
}
