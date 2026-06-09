using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GlamourDisplayTooltip.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin) : base("Glamour Display Tooltip Settings###GlamourDisplayTooltipConfig")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(390, 220);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var enabled = configuration.Enabled;
        if (ImGui.Checkbox("Enable hover preview", ref enabled))
        {
            configuration.Enabled = enabled;
            configuration.Save();
        }

        var showMetadata = configuration.ShowItemMetadata;
        if (ImGui.Checkbox("Show item metadata", ref showMetadata))
        {
            configuration.ShowItemMetadata = showMetadata;
            configuration.Save();
        }

        var preferEc = configuration.PreferEorzeaCollection;
        if (ImGui.Checkbox("Prefer Eorzea Collection images when available", ref preferEc))
        {
            configuration.PreferEorzeaCollection = preferEc;
            configuration.PreviewSource = preferEc ? PreviewSource.EorzeaCollection : PreviewSource.GameIcon;
            configuration.Save();
        }

        var previewSource = (int)configuration.PreviewSource;
        var previewSourceNames = new[] { "Game icon", "Eorzea Collection image", "Native model viewer" };
        if (ImGui.Combo("Preview source", ref previewSource, previewSourceNames, previewSourceNames.Length))
        {
            configuration.PreviewSource = (PreviewSource)previewSource;
            configuration.PreferEorzeaCollection = configuration.PreviewSource == PreviewSource.EorzeaCollection;
            configuration.Save();
        }

        var previewSize = configuration.PreviewSize;
        if (ImGui.SliderFloat("Preview size", ref previewSize, 140f, 360f, "%.0f px"))
        {
            configuration.PreviewSize = previewSize;
            configuration.Save();
        }

        var key = configuration.ActivationVirtualKey;
        if (ImGui.InputInt("Virtual key", ref key))
        {
            configuration.ActivationVirtualKey = Math.Clamp(key, 1, 255);
            configuration.Save();
        }

        if (ImGui.Button("Download image cache"))
        {
            plugin.StartEorzeaCollectionPrefetch();
        }

        ImGui.SameLine();
        if (ImGui.Button("Download status"))
        {
            plugin.ToggleEcDownloadUi();
        }

        ImGui.SameLine();
        if (ImGui.Button("Debug output"))
        {
            plugin.ToggleDebugUi();
        }

        ImGui.TextDisabled("16 accepts Shift, Ctrl, or Alt. Specific keys: Ctrl 17, Alt 18.");
    }
}
