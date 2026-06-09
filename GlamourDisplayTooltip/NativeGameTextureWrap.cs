using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;

namespace GlamourDisplayTooltip;

internal sealed class NativeGameTextureWrap(nint nativeHandle, int width, int height) : IDalamudTextureWrap
{
    public nint NativeHandle { get; } = nativeHandle;

    public ImTextureID Handle { get; } = new(nativeHandle);

    public int Width { get; } = width;

    public int Height { get; } = height;

    public Vector2 Size => new(Width, Height);

    public IDalamudTextureWrap CreateWrapSharingLowLevelResource()
        => new NativeGameTextureWrap(NativeHandle, Width, Height);

    public void Dispose()
    {
        // The game owns AgentTryon.Texture. This wrapper only borrows the SRV for ImGui.
    }
}
