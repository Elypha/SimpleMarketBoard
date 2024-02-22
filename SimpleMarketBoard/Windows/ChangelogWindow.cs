using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System;


namespace SimpleMarketBoard;

public class ChangelogWindow : Window, IDisposable
{
    private Plugin plugin;

    public ChangelogWindow(Plugin plugin) : base(
        "SimpleMarketBoard Changelog"
    // ImGuiWindowFlags.NoResize |
    // ImGuiWindowFlags.NoCollapse |
    // ImGuiWindowFlags.NoScrollbar |
    // ImGuiWindowFlags.NoScrollWithMouse
    )
    {
        Size = new Vector2(550, 400);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
    }

    public void Dispose()
    {
    }

    public override void OnOpen()
    {
    }

    public override void OnClose()
    {
    }

    public override void PreDraw()
    {
        if (plugin.Config.EnableTheme)
        {
            plugin.PluginTheme.Push();
            plugin.PluginThemeEnabled = true;
        }
    }

    public override void PostDraw()
    {
        if (plugin.PluginThemeEnabled)
        {
            plugin.PluginTheme.Pop();
            plugin.PluginThemeEnabled = false;
        }
    }

    public override void Draw()
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        var fontsize = ImGui.GetFontSize();


        var suffix = $"###{plugin.Name}-";

        ImGui.TextWrapped(
            "This is to give you a quick overview of what has changed.\n" +
            "If any of the descriptions are unclear, please refer to the documentation on the plugin's configuration window."
        );
        ImGui.Text("");

        plugin.ImGuiHelper.ChangelogList(
            "1.3.0.0 - 22 February 2024",
            new List<string>
            {
                "· New option: how many listings to request from Universalis.",
                "· New option: how many entries to request from Universalis.",
                "· New option: only request HQ listings from Universalis (HQ Filter Button).",
                "· A changelog window is now available from the configuration window.",
            }
        );

        plugin.ImGuiHelper.ChangelogList(
            "1.2.0.0 - 05 February 2024",
            new List<string>
            {
                "· Plugin now comes with a default theme for better compatibility.",
                "· New option: customise last update table width.",
                "· New option: use keybinding to open and close the main window.",
            }
        );

        plugin.ImGuiHelper.ChangelogList(
            "1.1.0.0 - 03 February 2024",
            new List<string>
            {
                "· Internal improvements.",
            }
        );

        plugin.ImGuiHelper.ChangelogList(
            "1.0.0.0 - 24 Janurary 2024",
            new List<string>
            {
                "· Initial release."
            }
        );
    }
}
