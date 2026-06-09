using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GlamourDisplayTooltip.Windows;

public class EorzeaCollectionDownloadWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public EorzeaCollectionDownloadWindow(Plugin plugin)
        : base("Eorzea Collection Cache###GlamourDisplayTooltipEcDownload")
    {
        Size = new Vector2(430, 210);
        SizeCondition = ImGuiCond.FirstUseEver;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var state = plugin.EcPrefetchProgress;
        var imageProgress = state.TotalImages > 0
            ? Math.Clamp((float)state.CompletedImages / state.TotalImages, 0f, 1f)
            : 0f;
        var byteProgress = state.KnownTotalBytes > 0
            ? Math.Clamp((float)state.DownloadedBytes / state.KnownTotalBytes, 0f, 1f)
            : 0f;
        var candidateProgress = state.CurrentCandidateCount > 0
            ? Math.Clamp((float)state.CurrentCandidateIndex / state.CurrentCandidateCount, 0f, 1f)
            : 0f;
        var indexProgress = state.TotalIndexPages > 0
            ? Math.Clamp((float)state.CurrentIndexPage / state.TotalIndexPages, 0f, 1f)
            : 0f;

        ImGui.TextUnformatted(state.Status);
        ImGui.Spacing();

        ImGui.ProgressBar(imageProgress, new Vector2(-1, 20), $"{state.CompletedImages}/{state.TotalImages} images");
        ImGui.ProgressBar(indexProgress, new Vector2(-1, 20), $"{state.CurrentIndexPage}/{state.TotalIndexPages} index pages ({state.IndexedGearsets} gearsets)");
        ImGui.ProgressBar(byteProgress, new Vector2(-1, 20), $"{FormatBytes(state.DownloadedBytes)}/{FormatBytes(state.KnownTotalBytes)}");
        ImGui.ProgressBar(candidateProgress, new Vector2(-1, 20), $"{state.CurrentCandidateIndex}/{state.CurrentCandidateCount} candidates");

        ImGui.Spacing();
        ImGui.TextDisabled($"Downloaded: {state.DownloadedImages}");
        ImGui.SameLine();
        ImGui.TextDisabled($"Missing: {state.MissingImages}");

        if (!string.IsNullOrWhiteSpace(state.CurrentItemName))
        {
            ImGui.TextWrapped($"Current: {state.CurrentItemName}");
        }

        ImGui.Spacing();
        if (state.IsRunning)
        {
            if (ImGui.Button("Cancel"))
            {
                plugin.CancelEorzeaCollectionPrefetch();
            }
        }
        else if (ImGui.Button("Start"))
        {
            plugin.StartEorzeaCollectionPrefetch();
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 MB";
        }

        return $"{bytes / 1024f / 1024f:0.00} MB";
    }
}
