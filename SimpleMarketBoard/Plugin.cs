using Dalamud.Configuration;
using Dalamud.ContextMenu;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Lumina.Excel;
using SimpleMarketBoard.UniversalisModels;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace SimpleMarketBoard;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "SimpleMarketBoard";
    private const string CommandMainWindow = "/mb";
    private const string CommandConfigWindow = "/mbc";


    public GameFontHandle AxisTitle { get; set; }
    public readonly ExcelSheet<Item> ItemSheet;
    public readonly ExcelSheet<World> WorldSheet;


    public SimpleMarketBoardConfig Config { get; init; }
    public WindowSystem WindowSystem = new("SimpleMarketBoard");
    public ConfigWindow ConfigWindow { get; init; }
    public MainWindow MainWindow { get; init; }


    public PrintMessage PrintMessage { get; set; } = null!;
    public PluginHotkey PluginHotkey { get; init; }
    public Universalis Universalis { get; set; } = null!;
    public HoveredItem HoveredItem { get; set; } = null!;
    public PriceChecker PriceChecker { get; set; } = null!;
    public ImGuiHelper ImGuiHelper { get; set; } = null!;





    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        Service.PluginLog.Info($"[General] Plugin loading...");

        AxisTitle = Service.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamily.Axis, 20.0f));
        ItemSheet = Service.Data.GetExcelSheet<Item>()!;
        WorldSheet = Service.Data.GetExcelSheet<World>()!;

        Config = Service.PluginInterface.GetPluginConfig() as SimpleMarketBoardConfig ?? new SimpleMarketBoardConfig();
        Config.Initialize(Service.PluginInterface);
        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        PrintMessage = new PrintMessage(this);
        PluginHotkey = new PluginHotkey(this);
        Universalis = new Universalis(this);
        HoveredItem = new HoveredItem(this);
        PriceChecker = new PriceChecker(this);
        ImGuiHelper = new ImGuiHelper();

        Service.PluginInterface.UiBuilder.Draw += DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;
        Service.ClientState.Login += OnLogin;
        Service.ClientState.TerritoryChanged += OnTerritoryChanged;
        HoveredItem.Enable();
        ConfigWindow.UpdateWorld();

        Service.Commands.AddHandler(CommandConfigWindow, new CommandInfo(OnCommandConfigWindow) { HelpMessage = "Open the configuration window." });
        Service.Commands.AddHandler(CommandMainWindow, new CommandInfo(OnCommandMainWindow) { HelpMessage = "Open the main window." });

        Service.PluginLog.Info($"[General] Plugin initialised");
    }

    public void Dispose()
    {
        Service.Commands.RemoveHandler(CommandConfigWindow);
        Service.Commands.RemoveHandler(CommandMainWindow);

        Service.PluginInterface.UiBuilder.Draw -= DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi -= DrawMainUI;
        Service.ClientState.Login -= OnLogin;
        Service.ClientState.TerritoryChanged -= OnTerritoryChanged;

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        MainWindow.Dispose();

        PrintMessage.Dispose();
        PluginHotkey.Dispose();
        Universalis.Dispose();
        HoveredItem.Dispose();
        PriceChecker.Dispose();
        ImGuiHelper.Dispose();

        AxisTitle.Dispose();
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

    public void OnCommandMainWindow(string command, string args)
    {
        MainWindow.Toggle();
    }

    public void OnCommandConfigWindow(string command, string args)
    {
        ConfigWindow.Toggle();
    }

    public void OnLogin()
    {
        ConfigWindow.UpdateWorld();
    }
    public void OnTerritoryChanged(ushort territoryId)
    {
        ConfigWindow.UpdateWorld();
    }


    // ----------------- game item stuff -----------------

    public List<GameItem> GameItemCacheList = new List<GameItem>();

    public class GameItem
    {
        public ulong Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsHQ { get; set; }
        public ulong PlayerWorldId { get; set; }
        public double AvgPrice { get; set; }
        public ulong FetchTimestamp { get; set; }
        public ulong Result { get; set; } = GameItemResult.Init;
        public Dictionary<string, ulong> WorldOutOfDate { get; set; } = null!;
        // gameItem.InGame.PriceLow;
        public Item InGame { get; set; } = null!;
        public UniversalisResponse UniversalisResponse { get; set; } = null!;
    }

    public class GameItemResult
    {
        public const ulong Init = 0;
        public const ulong Success = 1;
        public const ulong InGameError = 10;
        public const ulong Unmarketable = 11;
        public const ulong UnknownUserWorld = 12;
        public const ulong APIError = 20;
    }


    public void SearchHistoryUpdate(Plugin.GameItem gameItem, bool checkRemove = false)
    {
        SearchHistoryClean();
        if (checkRemove) GameItemCacheList.RemoveAll(i => i.Id == gameItem.Id);
        GameItemCacheList.Insert(0, gameItem);
    }

    public void SearchHistoryClean()
    {
        Service.PluginLog.Debug($"[Cache] Items in cache {GameItemCacheList.Count}");

        if (GameItemCacheList.Count < Config.MaxCacheItems) return;

        if (Config.CleanCacheAsYouGo || (!Config.CleanCacheAsYouGo && !MainWindow.IsOpen))
        {
            GameItemCacheList.RemoveRange(
                Config.MaxCacheItems - 1,
                GameItemCacheList.Count - Config.MaxCacheItems + 1
            );
            Service.PluginLog.Debug($"[Cache] Cache cleaned. Current items in cache {GameItemCacheList.Count}");
        }
    }


}
