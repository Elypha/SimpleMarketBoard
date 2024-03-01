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

using SimpleMarketBoard.UniversalisModels;


namespace SimpleMarketBoard;

public sealed class Plugin : IDalamudPlugin
{
    // dalamud plugin
    public string Name => "SimpleMarketBoard";
    public string NameShort => "SMB";
    private const string CommandMain = "/smb";

    // fonts and data resources
    public IFontHandle AxisTitle { get; set; }
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
    public ChangelogWindow ChangelogWindow { get; init; }

    // modules
    public PrintMessage PrintMessage { get; set; } = null!;
    public PluginHotkey PluginHotkey { get; init; }
    public Universalis Universalis { get; set; } = null!;
    public HoveredItem HoveredItem { get; set; } = null!;
    public PriceChecker PriceChecker { get; set; } = null!;
    public ImGuiHelper ImGuiHelper { get; set; } = null!;


    public Plugin(DalamudPluginInterface pluginInterface)
    {
        // load plugin services
        pluginInterface.Create<Service>();
        Service.PluginLog.Info($"[General] Plugin loading...");

        // load fonts and data resources
        PluginTheme = StyleModel.Deserialize("DS1H4sIAAAAAAAACq1XS3PbOAz+LzpnMnpQlORbE2+bQ7uTadLpbm+MzdiqFcsry27TTP97CZIgIcr2etLqIojChxcBEHyJRDRJLuOL6CGavET/RBMOH//q98+LaBZNGCzMo0kMb2m5YssVX+aK61HJuIgWdnlpJdb2WzxY4qsFMwvOtIpVNElhobFcT4EhhmsdYJlebYPVVK9uRkbC6n/qt7arU/bpX1sL7NWC0nIR7awpe/vjm/3+bmU9O8E58f7HQXVC2OVs4MesVX6+RPfye2//J/a/fn+x78/6rfiBcVpvxUMj52PtGqDfDvC5Xs/bb1cLb1RRsizJCgSRbw0m30pIfGk/mZJ1vaybORWFIhBqIaD3tt3sNpS3ROYSuUvLXoHsq7aby86xs8yyA0GcMiE2zHdLoVw7y5q3nXiSxJq0ssxAaG4gjHzm+W/avexooBlGmqFVzMII6s2sr/e+MjiCOII4gngBW1r3DbUtSQrOiphblP/UWP9prC3LNMszLyZQnlUsLqoUg5nHPCmd8ayKk4q7fRjJum6bRmy2JACvE/dBrndXoqM+YkyAMH4xkrR3s06pfhhATm2v43/XQXdBYxMLAUJjgBgrAVC40wyxQBjvjmKDkOcIBcIkCYVeL+Vs9UF0KwcocauB0AAgvK6mVsk+8OxoHoaIMBULTMUCU7EguKtd37fYWVXloyNAaG4gXLYb7jBwWZUmOS9RUZ5WLEmw5HlaMBsMKPs8rsrYiwpTl+lcx77BEPyFJNdY1I0UtI/kWOhAGCQWepY49lGdH2yoal8cIgwsxhUdpbshN6ITfXtuc3P8rw8t6dlO2qgxvGKfPspt/UO+62p/pBYYYCBMkmCA03wACd0pcGeBMMiS9kWP/BOm35PySTH+QJjuj5lUcsP7G7GH9pxXRsyrLR9I+bR+bGc72odjjsFLVM9VDwpJWBmrxxVGkbJARmBSkuUWbyLCtTjMY5bSvjJtZ6t6vbjt5L6W/uBNeVhjIM644VF/PW36Z3oEo0bcAqLotmn79/Vabn2BYS8CwoQrOQQY7hvOUqTSsjyA3dTbvl2ok9vpchk9OGkOII4pw+gNBjcY2UzvoOcg6gJiFAaNsbNO37XrxamjLT8MfF8vlv2pzB/hPg7HxRPHrmd/0/zf+Ao5a+fXO9nIWS/pJHkihTLoItNOLKZdu7kX3UIeU+WNg7r5W+xvlO/N0P9jeoz/CmPmZZWvIfi4Y0WAnNZPxDWsLKxQ9CuFwaidi8bgzgOpYMDlR42Z0SSSzfNmKSJ1GzOXCDFKxGFcjI++DRZBntNRQF30zuA67woiz7rZPHrn3cmgKRMAezYY3sXI0yLGDaUyl6M05gc01yMu100z/RBevLOCRphB2IlIr47mKW2qjZcYhrriNIy+QaFSN3gTLj++qenpYHkYPrwqe6dZZkI4DI4/6Tn8N80jdhYOgqOu1MG25DH2JyrTT0Klm33VvQIbe0632h8BpTukcHu0dMupWFUzVqs/fwHiVFw/xBAAAA==")!;
        AxisTitle = Service.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Axis, 20.0f));
        ItemSheet = Service.Data.GetExcelSheet<Item>()!;
        WorldSheet = Service.Data.GetExcelSheet<World>()!;

        // load configuration
        Config = Service.PluginInterface.GetPluginConfig() as SimpleMarketBoardConfig ?? new SimpleMarketBoardConfig();
        Config.Initialize(Service.PluginInterface);

        // load windows
        MainWindow = new MainWindow(this);
        WindowSystem.AddWindow(MainWindow);
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);
        ChangelogWindow = new ChangelogWindow(this);
        WindowSystem.AddWindow(ChangelogWindow);

        // load modules
        PrintMessage = new PrintMessage(this);
        PluginHotkey = new PluginHotkey(this);
        Universalis = new Universalis(this);
        HoveredItem = new HoveredItem(this);
        PriceChecker = new PriceChecker(this);
        ImGuiHelper = new ImGuiHelper();

        // load event handlers
        Service.PluginInterface.UiBuilder.Draw += DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;
        Service.ClientState.Login += OnLogin;
        Service.ClientState.TerritoryChanged += OnTerritoryChanged;
        Service.Framework.Update += OnFrameUpdate;
        MainWindow.UpdateWorld();

        // load command handlers
        Service.Commands.AddHandler(CommandMain, new CommandInfo(OnCommandMain)
        {
            HelpMessage = "main command entry:\n" +
                "└ /smb → open the main window (market data table).\n" +
                "└ /smb c|config → open the configuration window."
        });

        Service.PluginLog.Info($"[General] Plugin initialised");
    }

    public void Dispose()
    {
        // unload command handlers
        Service.Commands.RemoveHandler(CommandMain);

        // unload windows
        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();
        ConfigWindow.Dispose();
        ChangelogWindow.Dispose();

        // unload fonts and data resources
        AxisTitle.Dispose();

        // unload event handlers
        Service.PluginInterface.UiBuilder.Draw -= DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi -= DrawMainUI;
        Service.ClientState.Login -= OnLogin;
        Service.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Service.Framework.Update -= OnFrameUpdate;

        // unload modules
        PrintMessage.Dispose();
        PluginHotkey.Dispose();
        Universalis.Dispose();
        HoveredItem.Dispose();
        PriceChecker.Dispose();
        ImGuiHelper.Dispose();
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

    public void OnFrameUpdate(IFramework framework)
    {
        if (!Config.KeybindingEnabled) return;
        if (!PluginHotkey.CheckHotkeyState(Config.BindingHotkey)) return;

        if (Config.KeybindingToOpenWindow && !MainWindow.IsOpen && (HoveredItem.HoverItemId != 0))
        {
            MainWindow.Toggle();
            return;
        }

        if (Config.KeybindingToCloseWindow && MainWindow.IsOpen && (HoveredItem.HoverItemId == 0))
        {
            MainWindow.Toggle();
            return;
        }
    }
}
