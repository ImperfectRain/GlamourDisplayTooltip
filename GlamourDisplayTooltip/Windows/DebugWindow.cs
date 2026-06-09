using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GlamourDisplayTooltip.Windows;

public class DebugWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string report = string.Empty;

    public DebugWindow(Plugin plugin)
        : base("Glamour Display Tooltip Debug###GlamourDisplayTooltipDebug")
    {
        Size = new Vector2(760, 620);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(460, 320),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void OnOpen()
    {
        report = plugin.BuildDebugReport();
    }

    public override void Draw()
    {
        if (ImGui.Button("Refresh"))
        {
            report = plugin.BuildDebugReport();
        }

        ImGui.SameLine();
        if (ImGui.Button("Copy"))
        {
            ImGui.SetClipboardText(report);
        }

        ImGui.SameLine();
        ImGui.TextDisabled("Hover the problem item first, then refresh and copy.");

        ImGui.Separator();
        var size = ImGui.GetContentRegionAvail();
        ImGui.InputTextMultiline("###GdtDebugReport", ref report, 1024 * 128, size, ImGuiInputTextFlags.ReadOnly);
    }
}
