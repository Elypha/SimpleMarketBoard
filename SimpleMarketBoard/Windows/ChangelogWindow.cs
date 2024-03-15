using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using System;


namespace SimpleMarketBoard;

public class ChangelogWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public ChangelogWindow(Plugin plugin) : base(
        "SimpleMarketBoard Changelog"
    )
    {
        Size = new Vector2(550, 400);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
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

    public override void OnOpen()
    {
    }

    public override void OnClose()
    {
    }

    public void Dispose()
    {
    }


    public override void Draw()
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        var fontsize = ImGui.GetFontSize();

        ImGui.TextWrapped(
            "If any of the descriptions are unclear, please refer to 'Features & UI Introduction' on the Config Window."
        );
        ImGui.Text("");

        plugin.UiHelper.ChangelogList(
            "1.4.0.0 - 15 March 2024",
            new List<string>
            {
                "· Notice: the main (stable) command entry has been renamed to /smb and /smb c|config. The previous '/mb' will continue to work, but may suffer sudden change/removal if there's any conflict in the future.",
                "· New function: [Config > UI > Enable search from clipboard] Alt + left-click the item icon to search for item by text from your clipboard.",
            }
        );

        plugin.UiHelper.ChangelogList(
            "1.3.0.0 - 22 February 2024",
            new List<string>
            {
                "· New option: how many listings to request from Universalis.",
                "· New option: how many entries to request from Universalis.",
                "· New option: only request HQ listings from Universalis (HQ Filter Button).",
                "· A changelog window is now available from the configuration window.",
            }
        );

        plugin.UiHelper.ChangelogList(
            "1.2.0.0 - 05 February 2024",
            new List<string>
            {
                "· Plugin now comes with a default theme for better compatibility.",
                "· New option: customise last update table width.",
                "· New option: use keybinding to open and close the main window.",
            }
        );

        plugin.UiHelper.ChangelogList(
            "1.1.0.0 - 03 February 2024",
            new List<string>
            {
                "· Internal improvements.",
            }
        );

        plugin.UiHelper.ChangelogList(
            "1.0.0.0 - 24 Janurary 2024",
            new List<string>
            {
                "· Initial release."
            }
        );
    }
}
