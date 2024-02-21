using Dalamud.Interface.Components;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;


namespace SimpleMarketBoard;

public class ImGuiHelper
{
    public Vector4 titleColour;
    public Vector4 textColour;
    public Vector4 bulletTitleColour;
    public Vector4 bulletListColour;

    public ImGuiHelper()
    {
        titleColour = new Vector4(0.9f, 0.7f, 0.55f, 1);
        textColour = new Vector4(245f, 245f, 245f, 255f) / 255f;
        bulletTitleColour = new Vector4(0.95f, 0.8f, 0.6f, 1);
        bulletListColour = new Vector4(0.8f, 0.8f, 0.8f, 1);
    }

    public void Dispose()
    {
    }

    public void BulletTextList(string title, string description, List<string> list)
    {

        ImGui.TextColored(bulletTitleColour, title);

        ImGui.SameLine();
        ImGuiComponents.HelpMarker(description);

        ImGui.Indent();

        ImGui.PushTextWrapPos();
        ImGui.PushStyleColor(ImGuiCol.Text, bulletListColour);
        foreach (var text in list)
        {
            ImGui.Text(text);
        }
        ImGui.PopStyleColor();
        ImGui.PopTextWrapPos();

        ImGui.Unindent();
    }

    public void ChangelogList(string title, List<string> list)
    {
        ImGui.TextColored(titleColour, title);
        ImGui.Separator();
        ImGui.Indent();

        ImGui.PushTextWrapPos();
        ImGui.PushStyleColor(ImGuiCol.Text, textColour);
        foreach (var text in list)
        {
            ImGui.Text(text);
        }
        ImGui.PopStyleColor();
        ImGui.PopTextWrapPos();

        ImGui.Unindent();
        ImGui.Text("");
    }
}
