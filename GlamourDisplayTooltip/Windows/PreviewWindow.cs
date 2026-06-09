using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GlamourDisplayTooltip.Windows;

public class PreviewWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public PreviewWindow(Plugin plugin) : base("Glamour Display Tooltip###GlamourDisplayTooltipMain")
    {
        Size = new Vector2(310, 390);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(260, 260),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (!plugin.TryGetPreviewItem(out var item, out var source))
        {
            ImGui.TextWrapped(plugin.CurrentDetectionText);
            ImGui.Spacing();
            ImGui.TextDisabled("Hover a gear tooltip, then open this window or hold the modifier key.");

            if (ImGui.Button("Open Settings"))
            {
                plugin.ToggleConfigUi();
            }

            return;
        }

        ImGui.TextDisabled(source);
        ImGui.Spacing();

        var available = ImGui.GetContentRegionAvail();
        var previewSize = Math.Clamp(Math.Min(available.X, available.Y - 96f), 140f, 360f);
        plugin.DrawItemPreview(item, previewSize);

        ImGui.Spacing();
        if (ImGui.Button("Settings"))
        {
            plugin.ToggleConfigUi();
        }

        ImGui.SameLine();
        if (ImGui.Button("Try On Preview"))
        {
            plugin.ToggleModelViewerUi();
        }

        ImGui.SameLine();
        if (ImGui.Button("Debug"))
        {
            plugin.ToggleDebugUi();
        }
    }
}
