using Dalamud.Configuration;
using System;

namespace GlamourDisplayTooltip;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool Enabled { get; set; } = true;
    public int ActivationVirtualKey { get; set; } = 0x10; // VK_SHIFT
    public float PreviewSize { get; set; } = 220f;
    public bool ShowItemMetadata { get; set; } = true;
    public bool PreferEorzeaCollection { get; set; } = false;
    public PreviewSource PreviewSource { get; set; } = PreviewSource.EorzeaCollection;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

public enum PreviewSource
{
    GameIcon = 0,
    EorzeaCollection = 1,
    NativeModelViewer = 2,
}
