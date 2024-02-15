using Dalamud.Interface.Components;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;


namespace SimpleMarketBoard;

public class ImGuiHelper
{
    public Vector4 titleColour;
    public Vector4 bulletColour;

    public ImGuiHelper()
    {
        titleColour = new Vector4(0.95f, 0.8f, 0.6f, 1);
        bulletColour = new Vector4(0.8f, 0.8f, 0.8f, 1);
    }

    public void Dispose()
    {
    }

    public void BulletTextList(string title, string description, List<string> list)
    {

        ImGui.TextColored(titleColour, title);

        ImGui.SameLine();
        ImGuiComponents.HelpMarker(description);

        ImGui.Indent();

        ImGui.PushTextWrapPos();
        ImGui.PushStyleColor(ImGuiCol.Text, bulletColour);
        foreach (var text in list)
        {
            ImGui.Text(text);
        }
        ImGui.PopStyleColor();
        ImGui.PopTextWrapPos();

        ImGui.Unindent();
    }
}
