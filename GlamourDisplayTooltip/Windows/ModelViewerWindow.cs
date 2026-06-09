using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GlamourDisplayTooltip.Windows;

public class ModelViewerWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public ModelViewerWindow(Plugin plugin) : base("Glamour Model Viewer###GlamourDisplayTooltipModelViewer")
    {
        Size = new Vector2(360, 260);
        SizeCondition = ImGuiCond.FirstUseEver;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (!plugin.TryGetPreviewItem(out var item, out var source))
        {
            ImGui.TextWrapped("Hover a previewable item tooltip first.");
            return;
        }

        ImGui.TextDisabled(source);
        ImGui.TextUnformatted(item.Name.ToString());
        ImGui.Separator();

        ImGui.TextDisabled($"Item ID: {item.RowId}");
        ImGui.TextDisabled($"Slot: {Plugin.GetSlotLabelForUi(item)}");
        ImGui.TextDisabled($"Icon: {item.Icon}");

        ImGui.Spacing();
        ImGui.TextWrapped("Uses the game's native Try On render target for the hovered item.");
        ImGui.TextDisabled(plugin.NativeTryOnStatus);
    }
}
