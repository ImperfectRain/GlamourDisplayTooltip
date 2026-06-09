using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GlamourDisplayTooltip.Windows;
using Lumina.Excel.Sheets;

namespace GlamourDisplayTooltip;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/gdt";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private static readonly Regex GearsetLinkRegex = new("""href=["'](?<href>/gearset/[^"'?#]+)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GearsetCardRegex = new("""href=["']/gearset/(?<slug>[^"'?#]+)["'][\s\S]{0,2500}?src=["'](?<image>https://gearsets\.eorzeacollection\.com/[^"']+/hyur-(?:male|female)-front\.png)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ImageUrlRegex = new("""(?<url>https://(?:ffxiv|gearsets|glamours)\.eorzeacollection\.com/[^"'\s<>]+\.(?:jpg|jpeg|png|webp))""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PaginationPageRegex = new("""href=["']\?page=(?<page>\d+)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RelativeImageUrlRegex = new("""(?:src|href)=["'](?<url>/[^"']+\.(?:jpg|jpeg|png|webp))["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] EorzeaCollectionCdnPrefixes =
    [
        "",
        "battle",
        "glamour",
        "other",
        "quest/new-job",
        "quest/job",
        "quest/artifact",
        "quest",
        "job",
        "shop",
        "tomes/primal",
        "tomes/philosophy",
        "tomes/mythology",
        "tomes/soldiery",
        "tomes/poetics",
        "tomes/law",
        "tomes/esoterics",
        "tomes/lore",
        "tomes/scripture",
        "tomes/verity",
        "tomes/creation",
        "tomes/mendacity",
        "tomes/genesis",
        "tomes/goetia",
        "tomes/phantasmagoria",
        "tomes/allegory",
        "tomes/revelation",
        "tomes/aphorism",
        "tomes/astronomy",
        "tomes/causality",
        "tomes/comedy",
        "tomes/aesthetics",
        "tomes/heliometry",
        "tomes",
        "scrips",
        "scrips/crafters",
        "scrips/gatherers",
        "token",
        "token/a-realm-reborn",
        "token/heavensward",
        "token/stormblood",
        "token/shadowbringers",
        "token/endwalker",
        "token/dawntrail",
        "raid",
        "raid/binding-coil",
        "raid/alexander",
        "raid/omega",
        "raid/eden",
        "raid/pandaemonium",
        "raid/arcadion",
        "raid/a-realm-reborn",
        "raid/heavensward",
        "raid/stormblood",
        "raid/shadowbringers",
        "raid/endwalker",
        "raid/dawntrail",
        "alliance",
        "alliance/crystal-tower",
        "alliance/shadow-of-mhachi",
        "alliance/return-to-ivalice",
        "alliance/yorha",
        "alliance/myths-of-the-realm",
        "alliance/echoes-of-vana-diel",
        "alliance/a-realm-reborn",
        "alliance/heavensward",
        "alliance/stormblood",
        "alliance/shadowbringers",
        "alliance/endwalker",
        "alliance/dawntrail",
        "dungeon",
        "dungeon/a-realm-reborn",
        "dungeon/heavensward",
        "dungeon/stormblood",
        "dungeon/shadowbringers",
        "dungeon/endwalker",
        "dungeon/dawntrail",
        "trial",
        "trial/a-realm-reborn",
        "trial/heavensward",
        "trial/stormblood",
        "trial/shadowbringers",
        "trial/endwalker",
        "trial/dawntrail",
        "crafted",
        "crafted/crafters",
        "crafted/gatherers",
        "crafted/tanks",
        "crafted/healers",
        "crafted/melee",
        "crafted/ranged",
        "crafted/casters",
        "crafted/a-realm-reborn",
        "crafted/heavensward",
        "crafted/stormblood",
        "crafted/shadowbringers",
        "crafted/endwalker",
        "crafted/dawntrail",
        "pvp",
        "pvp/tanks",
        "pvp/healers",
        "pvp/melee",
        "pvp/ranged",
        "pvp/casters",
        "pvp/a-realm-reborn",
        "pvp/heavensward",
        "pvp/stormblood",
        "pvp/shadowbringers",
        "pvp/endwalker",
        "pvp/dawntrail",
        "treasure-hunt",
        "treasure-hunt/a-realm-reborn",
        "treasure-hunt/heavensward",
        "treasure-hunt/stormblood",
        "treasure-hunt/shadowbringers",
        "treasure-hunt/endwalker",
        "treasure-hunt/dawntrail",
        "criterion",
        "variant-dungeon",
        "deep-dungeon",
        "deep-dungeon/palace-of-the-dead",
        "deep-dungeon/heaven-on-high",
        "deep-dungeon/eureka-orthos",
        "grand-company",
        "grand-company/maelstrom",
        "grand-company/twin-adder",
        "grand-company/immortal-flames",
        "gold-saucer",
        "achievement",
        "island-sanctuary",
        "mogstation",
        "seasonal",
    ];

    private static readonly string[] EorzeaCollectionFallbackImageNames =
    [
        "hyur-male-front.png",
        "hyur-female-front.png",
    ];
    private static readonly TimeSpan EorzeaCollectionRetryDelay = TimeSpan.FromMinutes(2);
    private const int EorzeaCollectionProbeBatchSize = 16;
    private const int EorzeaCollectionPrefetchMaxCandidatesPerItem = 192;

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    public readonly WindowSystem WindowSystem = new("GlamourDisplayTooltip");

    private readonly ConfigWindow configWindow;
    private readonly PreviewWindow previewWindow;
    private readonly ModelViewerWindow modelViewerWindow;
    private readonly EorzeaCollectionDownloadWindow ecDownloadWindow;
    private readonly DebugWindow debugWindow;
    private readonly Dictionary<string, uint> itemIdsByName = [];
    private readonly Dictionary<uint, EcPreviewState> ecPreviewStates = [];
    private readonly List<(string Name, uint ItemId)> itemNamesByLength = [];
    private readonly string ecCacheDirectory;

    private uint currentItemId;
    private string currentItemName = string.Empty;
    private uint lastItemId;
    private string lastItemName = string.Empty;
    private Vector2 currentTooltipPosition;
    private Vector2 currentTooltipSize;
    private bool hasTooltipPosition;
    private uint nativeTryOnItemId;
    private DateTime nativeTryOnRequestUtc = DateTime.MinValue;
    private NativeGameTextureWrap? nativeTryOnTextureWrap;
    private string nativeTryOnStatus = "Native Try On has not been requested yet.";
    private CancellationTokenSource? ecPrefetchCancellation;
    private EcPrefetchState ecPrefetchState = new();
    private IReadOnlyDictionary<string, EcGearsetIndexEntry> ecGearsetIndex = new Dictionary<string, EcGearsetIndexEntry>(StringComparer.OrdinalIgnoreCase);
    private string lastEcIndexFetchStatus = "EC index has not been fetched yet.";

    public Configuration Configuration { get; }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
        })
        {
            Timeout = TimeSpan.FromSeconds(10),
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0 Safari/537.36");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        return client;
    }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        ecCacheDirectory = Path.Combine(PluginInterface.ConfigDirectory.FullName, "EorzeaCollectionCache");
        Directory.CreateDirectory(ecCacheDirectory);

        configWindow = new ConfigWindow(this);
        previewWindow = new PreviewWindow(this);
        modelViewerWindow = new ModelViewerWindow(this);
        ecDownloadWindow = new EorzeaCollectionDownloadWindow(this);
        debugWindow = new DebugWindow(this);
        WindowSystem.AddWindow(configWindow);
        WindowSystem.AddWindow(previewWindow);
        WindowSystem.AddWindow(modelViewerWindow);
        WindowSystem.AddWindow(ecDownloadWindow);
        WindowSystem.AddWindow(debugWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Glamour Display Tooltip settings."
        });

        AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "ItemDetail", OnItemDetailUpdate);
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "ItemDetail", OnItemDetailClose);
        GameGui.HoveredItemChanged += OnHoveredItemChanged;

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw += DrawPreviewOverlay;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        Log.Information("Glamour Display Tooltip loaded.");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw -= DrawPreviewOverlay;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "ItemDetail", OnItemDetailUpdate);
        AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "ItemDetail", OnItemDetailClose);
        GameGui.HoveredItemChanged -= OnHoveredItemChanged;

        CommandManager.RemoveHandler(CommandName);

        WindowSystem.RemoveAllWindows();
        configWindow.Dispose();
        previewWindow.Dispose();
        modelViewerWindow.Dispose();
        ecDownloadWindow.Dispose();
        debugWindow.Dispose();
        nativeTryOnTextureWrap?.Dispose();
        ecPrefetchCancellation?.Cancel();
        ecPrefetchCancellation?.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        if (args.Trim().Equals("config", StringComparison.OrdinalIgnoreCase))
        {
            ToggleConfigUi();
            return;
        }

        ToggleMainUi();
    }

    public void ToggleConfigUi() => configWindow.Toggle();

    public void ToggleMainUi() => previewWindow.Toggle();

    public void ToggleModelViewerUi() => modelViewerWindow.Toggle();

    public void ToggleEcDownloadUi() => ecDownloadWindow.Toggle();

    public void ToggleDebugUi() => debugWindow.Toggle();

    public string NativeTryOnStatus => nativeTryOnStatus;

    internal EcPrefetchState EcPrefetchProgress => ecPrefetchState;

    internal bool IsEcPrefetchRunning => ecPrefetchCancellation != null;

    internal void StartEorzeaCollectionPrefetch()
    {
        if (ecPrefetchCancellation != null)
        {
            ToggleEcDownloadUi();
            return;
        }

        ecPrefetchCancellation = new CancellationTokenSource();
        ecPrefetchState = new EcPrefetchState
        {
            IsRunning = true,
            Status = "Building EC image queue...",
            StartedUtc = DateTime.UtcNow,
        };
        ecDownloadWindow.IsOpen = true;
        _ = RunEorzeaCollectionPrefetchAsync(ecPrefetchCancellation.Token);
    }

    internal void CancelEorzeaCollectionPrefetch()
    {
        ecPrefetchCancellation?.Cancel();
    }

    internal string BuildDebugReport()
    {
        var builder = new StringBuilder();
        builder.AppendLine("GlamourDisplayTooltip Debug Report");
        builder.AppendLine($"GeneratedUtc: {DateTime.UtcNow:O}");
        builder.AppendLine($"AssemblyLocation: {PluginInterface.AssemblyLocation.FullName}");
        builder.AppendLine($"ConfigDirectory: {PluginInterface.ConfigDirectory.FullName}");
        builder.AppendLine($"EcCacheDirectory: {ecCacheDirectory}");
        builder.AppendLine();

        builder.AppendLine("[Config]");
        builder.AppendLine($"Enabled: {Configuration.Enabled}");
        builder.AppendLine($"PreviewSource: {Configuration.PreviewSource}");
        builder.AppendLine($"PreferEorzeaCollection: {Configuration.PreferEorzeaCollection}");
        builder.AppendLine($"ShowItemMetadata: {Configuration.ShowItemMetadata}");
        builder.AppendLine($"PreviewSize: {Configuration.PreviewSize}");
        builder.AppendLine($"ActivationVirtualKey: {Configuration.ActivationVirtualKey}");
        builder.AppendLine();

        builder.AppendLine("[Detection]");
        builder.AppendLine($"GameGui.HoveredItemRaw: {GameGui.HoveredItem}");
        builder.AppendLine($"GameGui.HoveredItemNormalized: {NormalizeHoveredItemId(GameGui.HoveredItem)}");
        builder.AppendLine($"CurrentItemId: {currentItemId}");
        builder.AppendLine($"CurrentItemName: {currentItemName}");
        builder.AppendLine($"LastItemId: {lastItemId}");
        builder.AppendLine($"LastItemName: {lastItemName}");
        builder.AppendLine($"HasTooltipPosition: {hasTooltipPosition}");
        builder.AppendLine($"TooltipPosition: {currentTooltipPosition}");
        builder.AppendLine($"TooltipSize: {currentTooltipSize}");
        builder.AppendLine($"KnownNameCacheCount: {itemIdsByName.Count}");
        builder.AppendLine();

        var rawHoveredItemId = NormalizeHoveredItemId(GameGui.HoveredItem);
        if (rawHoveredItemId != 0 && DataManager.GetExcelSheet<Item>().TryGetRow(rawHoveredItemId, out var rawHoveredItem))
        {
            builder.AppendLine("[RawHoveredItem]");
            builder.AppendLine($"RowId: {rawHoveredItem.RowId}");
            builder.AppendLine($"Name: {rawHoveredItem.Name}");
            builder.AppendLine($"Previewable: {IsPreviewableItem(rawHoveredItem)}");
            builder.AppendLine($"EcSlotName: {GetEorzeaCollectionSlotName(rawHoveredItem) ?? "(none)"}");
            builder.AppendLine($"SlotLabel: {GetSlotLabel(rawHoveredItem)}");
            builder.AppendLine();
        }

        if (!TryGetPreviewItem(out var item, out var source))
        {
            builder.AppendLine("[Item]");
            builder.AppendLine("No preview item is currently available.");
            return builder.ToString();
        }

        AppendItemDebugReport(builder, item, source);
        AppendEorzeaCollectionDebugReport(builder, item);
        AppendNativePreviewDebugReport(builder);
        AppendPrefetchDebugReport(builder);
        return builder.ToString();
    }

    private void AppendItemDebugReport(StringBuilder builder, Item item, string source)
    {
        var slot = item.EquipSlotCategory.Value;
        builder.AppendLine("[Item]");
        builder.AppendLine($"Source: {source}");
        builder.AppendLine($"RowId: {item.RowId}");
        builder.AppendLine($"Name: {item.Name}");
        builder.AppendLine($"Icon: {item.Icon}");
        builder.AppendLine($"ItemLevel: {item.LevelItem.RowId}");
        builder.AppendLine($"EquipSlotCategory.RowId: {item.EquipSlotCategory.RowId}");
        builder.AppendLine($"Previewable: {IsPreviewableItem(item)}");
        builder.AppendLine($"EcSlotName: {GetEorzeaCollectionSlotName(item) ?? "(none)"}");
        builder.AppendLine($"SlotLabel: {GetSlotLabel(item)}");
        builder.AppendLine($"Slots.MainHand: {slot.MainHand}");
        builder.AppendLine($"Slots.OffHand: {slot.OffHand}");
        builder.AppendLine($"Slots.Head: {slot.Head}");
        builder.AppendLine($"Slots.Body: {slot.Body}");
        builder.AppendLine($"Slots.Gloves: {slot.Gloves}");
        builder.AppendLine($"Slots.Legs: {slot.Legs}");
        builder.AppendLine($"Slots.Feet: {slot.Feet}");
        builder.AppendLine($"Slots.Ears: {slot.Ears}");
        builder.AppendLine($"Slots.Neck: {slot.Neck}");
        builder.AppendLine($"Slots.Wrists: {slot.Wrists}");
        builder.AppendLine($"Slots.FingerL: {slot.FingerL}");
        builder.AppendLine($"Slots.FingerR: {slot.FingerR}");
        builder.AppendLine();
    }

    private void AppendEorzeaCollectionDebugReport(StringBuilder builder, Item item)
    {
        var itemName = item.Name.ToString();
        var normalizedName = NormalizeEorzeaCollectionName(itemName);
        var strippedName = StripEquipmentSlotWords(normalizedName);
        var slotName = GetEorzeaCollectionSlotName(item);
        var slugCandidates = BuildEorzeaCollectionSlugCandidates(itemName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var roleFolders = BuildEorzeaCollectionRoleFolderCandidates(itemName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var imageNames = BuildEorzeaCollectionImageNames(slotName).ToList();
        var directCandidates = BuildDirectEorzeaCollectionCdnImageUrls(item, slotName).Take(80).ToList();
        var cachePath = GetExistingEcCachePath(item.RowId, slotName);
        ecPreviewStates.TryGetValue(item.RowId, out var state);

        builder.AppendLine("[EorzeaCollection]");
        builder.AppendLine($"RawName: {itemName}");
        builder.AppendLine($"NormalizedName: {normalizedName}");
        builder.AppendLine($"SlotWordsStrippedName: {strippedName}");
        builder.AppendLine($"RoleSuffix: {GetEorzeaCollectionRoleSuffix(normalizedName) ?? "(none)"}");
        builder.AppendLine($"CachePathExisting: {cachePath ?? "(none)"}");
        builder.AppendLine($"PreviewState.Exists: {state != null}");
        if (state != null)
        {
            builder.AppendLine($"PreviewState.LocalImagePath: {state.LocalImagePath ?? "(none)"}");
            builder.AppendLine($"PreviewState.IsLoading: {state.IsLoading}");
            builder.AppendLine($"PreviewState.LastAttemptUtc: {state.LastAttemptUtc:O}");
            builder.AppendLine($"PreviewState.Status: {state.Status}");
        }

        builder.AppendLine($"IndexGearsetCount: {ecGearsetIndex.Count}");
        builder.AppendLine("SlugCandidates:");
        foreach (var slug in slugCandidates)
        {
            var indexed = ecGearsetIndex.TryGetValue(slug, out var entry);
            builder.AppendLine($"- {slug} | indexed={indexed} | front={entry?.FrontImageUrl ?? "(none)"} | slotUrl={(indexed && slotName != null ? ReplaceEorzeaCollectionImageName(entry!.FrontImageUrl, $"hyur-male-{slotName}.png") : "(none)")}");
        }

        builder.AppendLine("RoleFolderCandidates:");
        foreach (var folder in roleFolders)
        {
            builder.AppendLine($"- {folder}");
        }

        builder.AppendLine("ImageNameCandidates:");
        foreach (var imageName in imageNames)
        {
            builder.AppendLine($"- {imageName}");
        }

        builder.AppendLine("FirstDirectCdnCandidates:");
        for (var i = 0; i < directCandidates.Count; i++)
        {
            builder.AppendLine($"{i + 1}. {directCandidates[i]}");
        }

        builder.AppendLine();
    }

    private void AppendNativePreviewDebugReport(StringBuilder builder)
    {
        builder.AppendLine("[NativeTryOn]");
        builder.AppendLine($"NativeTryOnItemId: {nativeTryOnItemId}");
        builder.AppendLine($"NativeTryOnRequestUtc: {nativeTryOnRequestUtc:O}");
        builder.AppendLine($"NativeTryOnStatus: {nativeTryOnStatus}");
        builder.AppendLine($"NativeTextureWrap: {(nativeTryOnTextureWrap == null ? "(none)" : $"{nativeTryOnTextureWrap.Width}x{nativeTryOnTextureWrap.Height} handle={nativeTryOnTextureWrap.NativeHandle}")}");
        builder.AppendLine();
    }

    private void AppendPrefetchDebugReport(StringBuilder builder)
    {
        builder.AppendLine("[EcPrefetch]");
        builder.AppendLine($"IsRunning: {ecPrefetchState.IsRunning}");
        builder.AppendLine($"Status: {ecPrefetchState.Status}");
        builder.AppendLine($"TotalImages: {ecPrefetchState.TotalImages}");
        builder.AppendLine($"CompletedImages: {ecPrefetchState.CompletedImages}");
        builder.AppendLine($"DownloadedImages: {ecPrefetchState.DownloadedImages}");
        builder.AppendLine($"MissingImages: {ecPrefetchState.MissingImages}");
        builder.AppendLine($"CurrentIndexPage: {ecPrefetchState.CurrentIndexPage}");
        builder.AppendLine($"TotalIndexPages: {ecPrefetchState.TotalIndexPages}");
        builder.AppendLine($"IndexedGearsets: {ecPrefetchState.IndexedGearsets}");
        builder.AppendLine($"CurrentCandidateIndex: {ecPrefetchState.CurrentCandidateIndex}");
        builder.AppendLine($"CurrentCandidateCount: {ecPrefetchState.CurrentCandidateCount}");
        builder.AppendLine($"DownloadedBytes: {ecPrefetchState.DownloadedBytes}");
        builder.AppendLine($"KnownTotalBytes: {ecPrefetchState.KnownTotalBytes}");
        builder.AppendLine($"CurrentItemName: {ecPrefetchState.CurrentItemName}");
        builder.AppendLine($"LastIndexFetchStatus: {lastEcIndexFetchStatus}");
        builder.AppendLine($"StartedUtc: {ecPrefetchState.StartedUtc:O}");
        builder.AppendLine($"FinishedUtc: {ecPrefetchState.FinishedUtc:O}");
        builder.AppendLine();
    }

    private async Task RunEorzeaCollectionPrefetchAsync(CancellationToken cancellationToken)
    {
        try
        {
            var items = DataManager.GetExcelSheet<Item>()
                .Where(item => IsPreviewableItem(item) && GetEorzeaCollectionSlotName(item) != null)
                .Where(item => GetExistingEcCachePath(item.RowId, GetEorzeaCollectionSlotName(item)) == null)
                .GroupBy(item => $"{NormalizeEorzeaCollectionName(item.Name.ToString())}|{GetEorzeaCollectionSlotName(item)}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            ecPrefetchState = ecPrefetchState with
            {
                TotalImages = items.Count,
                Status = items.Count == 0 ? "EC cache is already complete for supported item slots." : "Building EC gearset index...",
            };

            var gearsetIndex = await BuildEorzeaCollectionGearsetIndexAsync(cancellationToken);
            ecGearsetIndex = gearsetIndex;
            if (gearsetIndex.Count == 0)
            {
                ecPrefetchState = ecPrefetchState with
                {
                    IsRunning = false,
                    FinishedUtc = DateTime.UtcNow,
                    Status = $"Could not build EC gearset index. {lastEcIndexFetchStatus}",
                };
                return;
            }

            ecPrefetchState = ecPrefetchState with
            {
                Status = $"Indexed {gearsetIndex.Count} EC gearsets. Downloading images...",
            };

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await PrefetchEorzeaCollectionItemAsync(item, gearsetIndex, cancellationToken);
            }

            ecPrefetchState = ecPrefetchState with
            {
                IsRunning = false,
                FinishedUtc = DateTime.UtcNow,
                Status = "EC image download complete.",
            };
        }
        catch (OperationCanceledException)
        {
            ecPrefetchState = ecPrefetchState with
            {
                IsRunning = false,
                FinishedUtc = DateTime.UtcNow,
                Status = "EC image download cancelled.",
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "EC image prefetch failed.");
            ecPrefetchState = ecPrefetchState with
            {
                IsRunning = false,
                FinishedUtc = DateTime.UtcNow,
                Status = $"EC image download failed: {ex.Message}",
            };
        }
        finally
        {
            ecPrefetchCancellation?.Dispose();
            ecPrefetchCancellation = null;
        }
    }

    private async Task<IReadOnlyDictionary<string, EcGearsetIndexEntry>> BuildEorzeaCollectionGearsetIndexAsync(CancellationToken cancellationToken)
    {
        var entries = new Dictionary<string, EcGearsetIndexEntry>(StringComparer.OrdinalIgnoreCase);
        var firstPageResult = await TryGetStringDetailedAsync("https://ffxiv.eorzeacollection.com/gearsets", cancellationToken);
        lastEcIndexFetchStatus = firstPageResult.Status;
        if (firstPageResult.Html == null)
        {
            return entries;
        }

        AddEorzeaCollectionGearsetIndexEntries(firstPageResult.Html, entries);
        lastEcIndexFetchStatus = $"{firstPageResult.Status}; extracted={entries.Count}";
        var totalPages = GetEorzeaCollectionLastPage(firstPageResult.Html);
        ecPrefetchState = ecPrefetchState with
        {
            TotalIndexPages = totalPages,
            IndexedGearsets = entries.Count,
            CurrentIndexPage = 1,
            Status = $"Indexed EC gearsets page 1/{totalPages}...",
        };

        for (var page = 2; page <= totalPages; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var html = await TryGetStringAsync($"https://ffxiv.eorzeacollection.com/gearsets?page={page}", cancellationToken);
            if (html != null)
            {
                AddEorzeaCollectionGearsetIndexEntries(html, entries);
            }

            ecPrefetchState = ecPrefetchState with
            {
                CurrentIndexPage = page,
                IndexedGearsets = entries.Count,
                Status = $"Indexed EC gearsets page {page}/{totalPages}...",
            };
        }

        return entries;
    }

    private static void AddEorzeaCollectionGearsetIndexEntries(string html, Dictionary<string, EcGearsetIndexEntry> entries)
    {
        foreach (Match match in GearsetCardRegex.Matches(html))
        {
            var slug = WebUtility.HtmlDecode(match.Groups["slug"].Value);
            var frontImageUrl = WebUtility.HtmlDecode(match.Groups["image"].Value);
            if (!entries.ContainsKey(slug))
            {
                entries[slug] = new EcGearsetIndexEntry(slug, frontImageUrl);
            }
        }
    }

    private static int GetEorzeaCollectionLastPage(string html)
    {
        return PaginationPageRegex.Matches(html)
            .Select(match => int.TryParse(match.Groups["page"].Value, out var page) ? page : 1)
            .DefaultIfEmpty(1)
            .Max();
    }

    private async Task PrefetchEorzeaCollectionItemAsync(Item item, IReadOnlyDictionary<string, EcGearsetIndexEntry> gearsetIndex, CancellationToken cancellationToken)
    {
        var itemName = item.Name.ToString();
        var slotName = GetEorzeaCollectionSlotName(item);
        var imageUrl = TryBuildIndexedEorzeaCollectionImageUrl(item, slotName, gearsetIndex);
        var candidates = imageUrl == null
            ? BuildDirectEorzeaCollectionCdnImageUrls(item, slotName).Take(EorzeaCollectionPrefetchMaxCandidatesPerItem).ToList()
            : [];

        ecPrefetchState = ecPrefetchState with
        {
            CurrentItemName = itemName,
            CurrentCandidateCount = candidates.Count,
            CurrentCandidateIndex = 0,
            Status = imageUrl == null ? $"Finding EC image for {itemName}..." : $"Downloading indexed EC image for {itemName}...",
        };

        if (imageUrl == null)
        {
            imageUrl = await FindDirectEorzeaCollectionCdnImageUrlAsync(
                candidates,
                cancellationToken,
                checkedCount =>
                {
                    ecPrefetchState = ecPrefetchState with
                    {
                        CurrentCandidateIndex = checkedCount,
                        Status = $"Finding EC image for {itemName} ({checkedCount}/{candidates.Count} fallback candidates)...",
                    };
                });
        }

        if (imageUrl == null)
        {
            ecPrefetchState = ecPrefetchState with
            {
                CompletedImages = ecPrefetchState.CompletedImages + 1,
                MissingImages = ecPrefetchState.MissingImages + 1,
            };
            SetEcPreviewState(item.RowId, null, false, "No EC image found during prefetch.");
            return;
        }

        using var response = await HttpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            ecPrefetchState = ecPrefetchState with
            {
                CompletedImages = ecPrefetchState.CompletedImages + 1,
                MissingImages = ecPrefetchState.MissingImages + 1,
            };
            return;
        }

        var contentLength = response.Content.Headers.ContentLength ?? 0;
        if (contentLength > 0)
        {
            ecPrefetchState = ecPrefetchState with
            {
                KnownTotalBytes = ecPrefetchState.KnownTotalBytes + contentLength,
            };
        }

        var cachePath = GetEcCachePath(item.RowId, imageUrl);
        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var destination = File.Create(cachePath))
        {
            var buffer = new byte[64 * 1024];
            while (true)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                ecPrefetchState = ecPrefetchState with
                {
                    DownloadedBytes = ecPrefetchState.DownloadedBytes + read,
                };
            }
        }

        ecPrefetchState = ecPrefetchState with
        {
            CompletedImages = ecPrefetchState.CompletedImages + 1,
            DownloadedImages = ecPrefetchState.DownloadedImages + 1,
            Status = $"Downloaded {itemName}.",
        };
        SetEcPreviewState(item.RowId, cachePath, false, "Loaded EC image from prefetch cache.");
    }

    private void OnHoveredItemChanged(object? sender, ulong hoveredItem)
    {
        TrySetHoveredItem(hoveredItem);
    }

    private bool TrySetHoveredItem(ulong hoveredItem)
    {
        var itemId = NormalizeHoveredItemId(hoveredItem);

        if (itemId == 0)
        {
            currentItemId = 0;
            currentItemName = string.Empty;
            return false;
        }

        if (!DataManager.GetExcelSheet<Item>().TryGetRow(itemId, out var item) || !IsPreviewableItem(item))
        {
            currentItemId = 0;
            currentItemName = string.Empty;
            return false;
        }

        currentItemId = itemId;
        currentItemName = item.Name.ToString();
        lastItemId = currentItemId;
        lastItemName = currentItemName;
        return true;
    }

    private static uint NormalizeHoveredItemId(ulong hoveredItem)
    {
        if (hoveredItem == 0)
        {
            return 0;
        }

        if (hoveredItem > 1_000_000)
        {
            hoveredItem -= 1_000_000;
        }

        return hoveredItem > uint.MaxValue ? 0 : (uint)hoveredItem;
    }

    private unsafe void OnItemDetailUpdate(AddonEvent type, AddonArgs args)
    {
        if (args.Addon.Address == nint.Zero)
        {
            ClearCurrentItem();
            return;
        }

        var addon = (AtkUnitBase*)args.Addon.Address;
        if (!addon->IsVisible)
        {
            ClearCurrentItem();
            return;
        }

        currentTooltipPosition = new Vector2(addon->X, addon->Y);
        currentTooltipSize = new Vector2(addon->GetScaledWidth(true), addon->GetScaledHeight(true));
        hasTooltipPosition = true;
        if (GameGui.HoveredItem != 0)
        {
            if (IsKnownNonPreviewableHoveredItem(GameGui.HoveredItem))
            {
                ClearCurrentItem();
                return;
            }

            if (!TrySetHoveredItem(GameGui.HoveredItem))
            {
                TrySetHoveredItemFromTooltip(addon);
            }
        }
        else
        {
            TrySetHoveredItemFromTooltip(addon);
        }
    }

    private static bool IsKnownNonPreviewableHoveredItem(ulong hoveredItem)
    {
        var itemId = NormalizeHoveredItemId(hoveredItem);
        return itemId != 0 &&
               DataManager.GetExcelSheet<Item>().TryGetRow(itemId, out var item) &&
               !IsPreviewableItem(item);
    }

    private void OnItemDetailClose(AddonEvent type, AddonArgs args) => ClearCurrentItem();

    private void ClearCurrentItem()
    {
        currentItemId = 0;
        currentItemName = string.Empty;
        hasTooltipPosition = false;
    }

    private unsafe bool TrySetHoveredItemFromTooltip(AtkUnitBase* addon)
    {
        if (addon->UldManager.NodeList == null)
        {
            return false;
        }

        EnsureItemNameCache();

        for (var i = 0; i < addon->UldManager.NodeListCount; i++)
        {
            var node = addon->UldManager.NodeList[i];
            if (node == null || (NodeType)node->Type != NodeType.Text)
            {
                continue;
            }

            var text = CleanTooltipText(((AtkTextNode*)node)->NodeText.ToString());
            if (text.Length == 0)
            {
                continue;
            }

            if (TryResolveVisualItemFromTooltipText(text, out var itemId))
            {
                return TrySetCurrentItem(itemId);
            }
        }

        return false;
    }

    private void EnsureItemNameCache()
    {
        if (itemIdsByName.Count > 0)
        {
            return;
        }

        foreach (var item in DataManager.GetExcelSheet<Item>())
        {
            if (!IsPreviewableItem(item))
            {
                continue;
            }

            var name = CleanTooltipText(item.Name.ToString());
            if (name.Length == 0 || itemIdsByName.ContainsKey(name))
            {
                continue;
            }

            itemIdsByName[name] = item.RowId;
            itemNamesByLength.Add((name, item.RowId));
        }

        itemNamesByLength.Sort((left, right) => right.Name.Length.CompareTo(left.Name.Length));
    }

    private bool TryResolveVisualItemFromTooltipText(string text, out uint itemId)
    {
        if (itemIdsByName.TryGetValue(text, out itemId) && IsVisualItemId(itemId))
        {
            return true;
        }

        foreach (var candidate in itemNamesByLength)
        {
            if (text.StartsWith(candidate.Name, StringComparison.OrdinalIgnoreCase) &&
                IsVisualItemId(candidate.ItemId))
            {
                itemId = candidate.ItemId;
                return true;
            }
        }

        itemId = 0;
        return false;
    }

    private static bool IsVisualItemId(uint itemId)
    {
        return DataManager.GetExcelSheet<Item>().TryGetRow(itemId, out var item) && IsPreviewableItem(item);
    }

    private bool TrySetCurrentItem(uint itemId)
    {
        if (itemId == currentItemId)
        {
            return currentItemId != 0;
        }

        if (!DataManager.GetExcelSheet<Item>().TryGetRow(itemId, out var item))
        {
            return false;
        }

        if (!IsPreviewableItem(item))
        {
            return false;
        }

        currentItemId = itemId;
        currentItemName = item.Name.ToString();
        lastItemId = currentItemId;
        lastItemName = currentItemName;
        return true;
    }

    private static string CleanTooltipText(string text)
    {
        Span<char> buffer = stackalloc char[text.Length];
        var length = 0;

        foreach (var c in text)
        {
            if (char.IsControl(c) ||
                c == '\uFFFD' ||
                char.GetUnicodeCategory(c) == UnicodeCategory.PrivateUse)
            {
                continue;
            }

            buffer[length++] = c;
        }

        return new string(buffer[..length]).Trim();
    }

    internal bool TryGetPreviewItem(out Item item, out string source)
    {
        var itemId = currentItemId != 0 ? currentItemId : lastItemId;
        source = currentItemId != 0 ? "Current tooltip" : "Last detected tooltip";

        if (itemId != 0 && DataManager.GetExcelSheet<Item>().TryGetRow(itemId, out item))
        {
            return true;
        }

        item = default;
        source = string.Empty;
        return false;
    }

    internal string CurrentDetectionText => currentItemName.Length > 0
        ? currentItemName
        : lastItemName.Length > 0
            ? lastItemName
            : "No item tooltip detected yet.";

    internal void DrawItemPreview(Item item, float previewSize)
    {
        ImGui.TextUnformatted(item.Name.ToString());
        ImGui.Separator();

        var cursor = ImGui.GetCursorScreenPos();
        ImGui.GetWindowDrawList().AddRectFilled(cursor, cursor + new Vector2(previewSize), ImGui.GetColorU32(new Vector4(0.08f, 0.08f, 0.09f, 1f)), 4f);

        if (Configuration.PreviewSource == PreviewSource.NativeModelViewer)
        {
            DrawNativeModelViewerPreview(item, previewSize);
        }
        else if (Configuration.PreviewSource == PreviewSource.EorzeaCollection && TryDrawEorzeaCollectionImage(item, previewSize))
        {
            // Image drawn by provider.
        }
        else
        {
            var icon = TextureProvider.GetFromGameIcon(new GameIconLookup(item.Icon)).GetWrapOrEmpty();
            ImGui.Image(icon.Handle, new Vector2(previewSize));
        }

        if (!Configuration.ShowItemMetadata)
        {
            return;
        }

        ImGui.Spacing();
        ImGui.TextDisabled(GetSlotLabel(item));
        ImGui.SameLine();
        ImGui.TextDisabled($"Item level {item.LevelItem.RowId}");
        ImGui.TextDisabled(BuildEorzeaCollectionSearchUrl(item));
    }

    private void DrawNativeModelViewerPreview(Item item, float previewSize)
    {
        var cursor = ImGui.GetCursorScreenPos();
        ImGui.GetWindowDrawList().AddRectFilled(cursor, cursor + new Vector2(previewSize), ImGui.GetColorU32(new Vector4(0.05f, 0.06f, 0.07f, 1f)), 4f);

        RequestNativeTryOnRender(item.RowId);
        if (TryGetNativeTryOnTexture(out var nativeTexture))
        {
            DrawTextureFit(nativeTexture, cursor, new Vector2(previewSize));
            SuppressNativeTryOnAddon();
            return;
        }

        ImGui.Dummy(new Vector2(previewSize));
        ImGui.SetCursorScreenPos(cursor + new Vector2(12f, 12f));
        ImGui.TextUnformatted("Native Try On preview");
        ImGui.TextDisabled("Game fitting room render");

        ImGui.SetCursorScreenPos(cursor + new Vector2(12f, 58f));
        ImGui.TextWrapped(nativeTryOnStatus);

        ImGui.SetCursorScreenPos(cursor + new Vector2(12f, previewSize - 70f));
        ImGui.TextDisabled($"Item ID {item.RowId}");
        ImGui.TextDisabled($"Slot {GetSlotLabel(item)}");
        ImGui.TextDisabled(nativeTryOnStatus);
    }

    private void RequestNativeTryOnRender(uint itemId)
    {
        if (nativeTryOnItemId == itemId)
        {
            return;
        }

        nativeTryOnItemId = itemId;
        nativeTryOnRequestUtc = DateTime.UtcNow;
        nativeTryOnTextureWrap?.Dispose();
        nativeTryOnTextureWrap = null;

        try
        {
            var success = AgentTryon.TryOn(0, itemId);
            nativeTryOnStatus = success
                ? $"Requested native Try On render for item {itemId}."
                : $"Native Try On rejected item {itemId}.";

            if (success)
            {
                SuppressNativeTryOnAddon();
            }
        }
        catch (Exception ex)
        {
            nativeTryOnStatus = $"Native Try On failed for item {itemId}: {ex.Message}";
            Log.Warning(ex, nativeTryOnStatus);
        }
    }

    private unsafe bool TryGetNativeTryOnTexture(out IDalamudTextureWrap textureWrap)
    {
        textureWrap = null!;

        var agent = AgentTryon.Instance();
        if (agent == null || agent->Texture == null)
        {
            nativeTryOnStatus = "Native Try On render target is not ready yet.";
            return false;
        }

        var texture = agent->Texture;
        if (texture->D3D11ShaderResourceView == null || texture->ActualWidth == 0 || texture->ActualHeight == 0)
        {
            var elapsed = DateTime.UtcNow - nativeTryOnRequestUtc;
            nativeTryOnStatus = elapsed < TimeSpan.FromSeconds(2)
                ? "Native Try On texture is rendering..."
                : "Native Try On texture did not become ready.";
            return false;
        }

        var handle = (nint)texture->D3D11ShaderResourceView;
        var width = checked((int)texture->ActualWidth);
        var height = checked((int)texture->ActualHeight);

        if (nativeTryOnTextureWrap == null ||
            nativeTryOnTextureWrap.NativeHandle != handle ||
            nativeTryOnTextureWrap.Width != width ||
            nativeTryOnTextureWrap.Height != height)
        {
            nativeTryOnTextureWrap?.Dispose();
            nativeTryOnTextureWrap = new NativeGameTextureWrap(handle, width, height);
        }

        nativeTryOnStatus = $"Rendering native Try On texture {width}x{height}.";
        textureWrap = nativeTryOnTextureWrap;
        return true;
    }

    private unsafe void SuppressNativeTryOnAddon()
    {
        try
        {
            var addon = GameGui.GetAddonByName<AtkUnitBase>("Tryon");
            if (addon == null)
            {
                return;
            }

            addon->SetAlpha(0);
            addon->SetPosition(short.MinValue / 2, short.MinValue / 2);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not suppress native Try On addon.");
        }
    }

    private static void DrawTextureFit(IDalamudTextureWrap texture, Vector2 boxMin, Vector2 boxSize)
    {
        var textureSize = texture.Size;
        if (textureSize.X <= 0 || textureSize.Y <= 0)
        {
            ImGui.Dummy(boxSize);
            return;
        }

        var scale = MathF.Min(boxSize.X / textureSize.X, boxSize.Y / textureSize.Y);
        var drawSize = new Vector2(textureSize.X * scale, textureSize.Y * scale);
        var drawMin = boxMin + (boxSize - drawSize) * 0.5f;
        var drawMax = drawMin + drawSize;

        ImGui.GetWindowDrawList().AddImage(texture.Handle, drawMin, drawMax);
        ImGui.Dummy(boxSize);
    }

    private bool TryDrawEorzeaCollectionImage(Item item, float previewSize)
    {
        if (GetEorzeaCollectionSlotName(item) == null)
        {
            return false;
        }

        var state = GetOrStartEorzeaCollectionPreview(item);

        if (state.LocalImagePath is { Length: > 0 } && File.Exists(state.LocalImagePath))
        {
            var texture = TextureProvider.GetFromFile(state.LocalImagePath).GetWrapOrDefault();
            if (texture != null)
            {
                ImGui.Image(texture.Handle, new Vector2(previewSize));
                return true;
            }
        }

        ImGui.Image(TextureProvider.GetFromGameIcon(new GameIconLookup(item.Icon)).GetWrapOrEmpty().Handle, new Vector2(previewSize));
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 24f);
        ImGui.TextDisabled(state.Status);
        return true;
    }

    private EcPreviewState GetOrStartEorzeaCollectionPreview(Item item)
    {
        if (ecPreviewStates.TryGetValue(item.RowId, out var state))
        {
            if (!state.IsLoading &&
                state.LocalImagePath == null &&
                DateTime.UtcNow - state.LastAttemptUtc >= EorzeaCollectionRetryDelay)
            {
                var retrySlotName = GetEorzeaCollectionSlotName(item);
                state = new EcPreviewState
                {
                    IsLoading = true,
                    Status = "Retrying EC image lookup...",
                    LastAttemptUtc = DateTime.UtcNow,
                };
                ecPreviewStates[item.RowId] = state;
                _ = ResolveEorzeaCollectionPreviewAsync(item, retrySlotName);
            }

            return state;
        }

        var ecSlotName = GetEorzeaCollectionSlotName(item);
        var cachePath = GetExistingEcCachePath(item.RowId, ecSlotName);
        state = new EcPreviewState
        {
            LocalImagePath = cachePath,
            IsLoading = cachePath == null,
            LastAttemptUtc = DateTime.UtcNow,
            Status = cachePath == null ? "Queued EC image lookup..." : "Loaded EC image from cache.",
        };
        ecPreviewStates[item.RowId] = state;

        if (state.IsLoading)
        {
            _ = ResolveEorzeaCollectionPreviewAsync(item, ecSlotName);
        }

        return state;
    }

    private string? GetExistingEcCachePath(uint itemId, string? slotName)
    {
        var cacheBaseName = GetEcCacheBaseName(itemId, slotName);
        foreach (var extension in new[] { ".jpg", ".jpeg", ".png", ".webp" })
        {
            var path = Path.Combine(ecCacheDirectory, $"{cacheBaseName}{extension}");
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private string GetEcCachePath(uint itemId, string imageUrl)
    {
        var extension = Path.GetExtension(new Uri(imageUrl).AbsolutePath);
        if (extension.Length == 0)
        {
            extension = ".jpg";
        }

        var imageName = Path.GetFileNameWithoutExtension(new Uri(imageUrl).AbsolutePath);
        var slotName = imageName.StartsWith("hyur-male-", StringComparison.OrdinalIgnoreCase)
            ? imageName["hyur-male-".Length..]
            : imageName.StartsWith("hyur-female-", StringComparison.OrdinalIgnoreCase)
                ? imageName["hyur-female-".Length..]
                : null;

        return Path.Combine(ecCacheDirectory, $"{GetEcCacheBaseName(itemId, slotName)}{extension}");
    }

    private static string GetEcCacheBaseName(uint itemId, string? slotName)
    {
        return string.IsNullOrWhiteSpace(slotName) ? itemId.ToString() : $"{itemId}-{slotName}";
    }

    private async Task ResolveEorzeaCollectionPreviewAsync(Item item, string? slotName)
    {
        var itemId = item.RowId;
        var itemName = item.Name.ToString();

        try
        {
            var imageUrl = await FindEorzeaCollectionImageUrlAsync(item, slotName);
            if (imageUrl == null)
            {
                SetEcPreviewState(itemId, null, false);
                return;
            }

            using var response = await HttpClient.GetAsync(imageUrl);
            if (!response.IsSuccessStatusCode)
            {
                SetEcPreviewState(itemId, null, false);
                return;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length < 1024)
            {
                SetEcPreviewState(itemId, null, false);
                return;
            }

            var cachePath = GetEcCachePath(itemId, imageUrl);
            await File.WriteAllBytesAsync(cachePath, bytes);
            SetEcPreviewState(itemId, cachePath, false);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Eorzea Collection image lookup failed for {itemName}.");
            SetEcPreviewState(itemId, null, false);
        }
    }

    private void SetEcPreviewState(uint itemId, string? localPath, bool isLoading, string? status = null)
    {
        ecPreviewStates[itemId] = new EcPreviewState
        {
            LocalImagePath = localPath,
            IsLoading = isLoading,
            LastAttemptUtc = DateTime.UtcNow,
            Status = status ?? (localPath == null ? "No EC image found." : "Loaded EC image."),
        };
    }

    private async Task<string?> FindEorzeaCollectionImageUrlAsync(Item item, string? slotName)
    {
        var directImageUrl = await FindDirectEorzeaCollectionCdnImageUrlAsync(item, slotName);
        if (directImageUrl != null)
        {
            return directImageUrl;
        }

        var itemName = item.Name.ToString();
        foreach (var gearsetUrl in BuildEorzeaCollectionCandidateUrls(itemName))
        {
            var html = await TryGetStringAsync(gearsetUrl);
            if (html == null)
            {
                continue;
            }

            if (!PageMentionsItem(html, itemName))
            {
                continue;
            }

            var imageUrl = ExtractBestEorzeaCollectionImageUrl(html, slotName);
            if (imageUrl != null)
            {
                return imageUrl;
            }
        }

        foreach (var searchUrl in BuildEorzeaCollectionSearchUrls(itemName))
        {
            var html = await TryGetStringAsync(searchUrl);
            if (html == null)
            {
                continue;
            }

            foreach (var gearsetUrl in ExtractGearsetUrls(html))
            {
                var gearsetHtml = await TryGetStringAsync(gearsetUrl);
                if (gearsetHtml == null || !PageMentionsItem(gearsetHtml, itemName))
                {
                    continue;
                }

                var imageUrl = ExtractBestEorzeaCollectionImageUrl(gearsetHtml, slotName);
                if (imageUrl != null)
                {
                    return imageUrl;
                }
            }
        }

        return null;
    }

    private static async Task<string?> FindDirectEorzeaCollectionCdnImageUrlAsync(Item item, string? slotName)
    {
        var candidates = BuildDirectEorzeaCollectionCdnImageUrls(item, slotName)
            .ToList();

        return await FindDirectEorzeaCollectionCdnImageUrlAsync(candidates, CancellationToken.None, null);
    }

    private static string? TryBuildIndexedEorzeaCollectionImageUrl(Item item, string? slotName, IReadOnlyDictionary<string, EcGearsetIndexEntry> gearsetIndex)
    {
        if (string.IsNullOrWhiteSpace(slotName))
        {
            return null;
        }

        foreach (var slug in BuildEorzeaCollectionSlugCandidates(item.Name.ToString()))
        {
            if (!gearsetIndex.TryGetValue(slug, out var entry))
            {
                continue;
            }

            return ReplaceEorzeaCollectionImageName(entry.FrontImageUrl, $"hyur-male-{slotName}.png");
        }

        return null;
    }

    private static string ReplaceEorzeaCollectionImageName(string imageUrl, string imageName)
    {
        var lastSlash = imageUrl.LastIndexOf('/');
        return lastSlash < 0 ? imageUrl : $"{imageUrl[..(lastSlash + 1)]}{imageName}";
    }

    private static async Task<string?> FindDirectEorzeaCollectionCdnImageUrlAsync(
        IReadOnlyList<string> candidates,
        CancellationToken cancellationToken,
        Action<int>? progress)
    {
        for (var i = 0; i < candidates.Count; i += EorzeaCollectionProbeBatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = candidates
                .Skip(i)
                .Take(EorzeaCollectionProbeBatchSize)
                .ToList();
            var results = await Task.WhenAll(batch.Select(url => EorzeaCollectionImageExistsAsync(url, cancellationToken)));

            for (var resultIndex = 0; resultIndex < results.Length; resultIndex++)
            {
                if (results[resultIndex])
                {
                    return batch[resultIndex];
                }
            }

            progress?.Invoke(Math.Min(i + batch.Count, candidates.Count));
        }

        return null;
    }

    private static IEnumerable<string> BuildDirectEorzeaCollectionCdnImageUrls(Item item, string? slotName)
    {
        var itemName = item.Name.ToString();
        foreach (var folder in BuildEorzeaCollectionCdnFolderCandidates(itemName))
        {
            foreach (var imageName in BuildEorzeaCollectionImageNames(slotName))
            {
                yield return $"https://gearsets.eorzeacollection.com/{folder}/{imageName}";
            }
        }
    }

    private static IEnumerable<string> BuildEorzeaCollectionImageNames(string? slotName)
    {
        if (!string.IsNullOrWhiteSpace(slotName))
        {
            yield return $"hyur-male-{slotName}.png";
            yield return $"hyur-female-{slotName}.png";
        }

        foreach (var fallback in EorzeaCollectionFallbackImageNames)
        {
            yield return fallback;
        }
    }

    private static IEnumerable<string> BuildEorzeaCollectionCdnFolderCandidates(string itemName)
    {
        var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roleFolders = BuildEorzeaCollectionRoleFolderCandidates(itemName).ToList();

        foreach (var slug in BuildEorzeaCollectionSlugCandidates(itemName))
        {
            var familySlug = GetSetFamilySlug(slug);
            foreach (var prefix in EorzeaCollectionCdnPrefixes)
            {
                foreach (var folder in BuildEorzeaCollectionCdnFolderVariants(prefix, slug, familySlug, roleFolders))
                {
                    if (emitted.Add(folder))
                    {
                        yield return folder;
                    }
                }
            }
        }
    }

    private static IEnumerable<string> BuildEorzeaCollectionCdnFolderVariants(string prefix, string slug, string familySlug, IReadOnlyList<string> roleFolders)
    {
        if (prefix.Length == 0)
        {
            yield return slug;
            if (familySlug.Length > 0 && !string.Equals(familySlug, slug, StringComparison.OrdinalIgnoreCase))
            {
                yield return $"{familySlug}/{slug}";
            }

            foreach (var roleFolder in roleFolders)
            {
                yield return $"{roleFolder}/{slug}";
                if (familySlug.Length > 0 && !string.Equals(familySlug, slug, StringComparison.OrdinalIgnoreCase))
                {
                    yield return $"{roleFolder}/{familySlug}/{slug}";
                }
            }

            yield break;
        }

        yield return $"{prefix}/{slug}";

        if (familySlug.Length > 0 && !string.Equals(familySlug, slug, StringComparison.OrdinalIgnoreCase))
        {
            yield return $"{prefix}/{familySlug}/{slug}";
        }

        if (!PrefixSupportsRoleFolders(prefix))
        {
            yield break;
        }

        foreach (var roleFolder in roleFolders)
        {
            yield return $"{prefix}/{roleFolder}/{slug}";
            if (familySlug.Length > 0 && !string.Equals(familySlug, slug, StringComparison.OrdinalIgnoreCase))
            {
                yield return $"{prefix}/{roleFolder}/{familySlug}/{slug}";
            }
        }
    }

    private static bool PrefixSupportsRoleFolders(string prefix)
    {
        return prefix.Equals("crafted", StringComparison.OrdinalIgnoreCase) ||
               prefix.Equals("scrips", StringComparison.OrdinalIgnoreCase) ||
               prefix.Equals("pvp", StringComparison.OrdinalIgnoreCase) ||
               prefix.Equals("battle", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> BuildEorzeaCollectionRoleFolderCandidates(string itemName)
    {
        var cleanName = NormalizeEorzeaCollectionName(itemName);
        var roleSuffix = GetEorzeaCollectionRoleSuffix(cleanName);

        if (roleSuffix == null)
        {
            yield break;
        }

        yield return roleSuffix;

        switch (roleSuffix)
        {
            case "fending":
                yield return "tanks";
                break;
            case "healing":
                yield return "healers";
                break;
            case "casting":
                yield return "casters";
                break;
            case "aiming":
                yield return "ranged";
                break;
            case "maiming":
            case "striking":
            case "scouting":
            case "slaying":
                yield return "melee";
                break;
            case "crafting":
                yield return "crafters";
                break;
            case "gathering":
                yield return "gatherers";
                break;
        }
    }

    private static string GetSetFamilySlug(string slug)
    {
        foreach (var suffix in new[]
        {
            "-fending",
            "-maiming",
            "-striking",
            "-scouting",
            "-aiming",
            "-casting",
            "-healing",
            "-slaying",
            "-defending",
        })
        {
            if (slug.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return slug[..^suffix.Length];
            }
        }

        var firstDashIndex = slug.IndexOf('-');
        if (firstDashIndex > 0)
        {
            return slug[..firstDashIndex];
        }

        return slug;
    }

    private static string? GetEorzeaCollectionRoleSuffix(string itemName)
    {
        var lowerName = itemName.ToLowerInvariant();
        var roleIndex = lowerName.LastIndexOf(" of ", StringComparison.OrdinalIgnoreCase);
        if (roleIndex >= 0)
        {
            var suffix = lowerName[(roleIndex + 4)..].Trim();
            return suffix switch
            {
                "fending" or
                "maiming" or
                "striking" or
                "scouting" or
                "aiming" or
                "casting" or
                "healing" or
                "slaying" or
                "defending" => suffix == "defending" ? "fending" : suffix,
                _ => null,
            };
        }

        if (lowerName.Contains("gathering", StringComparison.OrdinalIgnoreCase))
        {
            return "gathering";
        }

        if (lowerName.Contains("crafting", StringComparison.OrdinalIgnoreCase))
        {
            return "crafting";
        }

        return null;
    }

    private static async Task<bool> EorzeaCollectionImageExistsAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            return response.IsSuccessStatusCode &&
                   response.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string?> TryGetStringAsync(string url)
        => await TryGetStringAsync(url, CancellationToken.None);

    private static async Task<string?> TryGetStringAsync(string url, CancellationToken cancellationToken)
    {
        var result = await TryGetStringDetailedAsync(url, cancellationToken);
        return result.Html;
    }

    private static async Task<EcHttpTextResult> TryGetStringDetailedAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Referrer = new Uri("https://ffxiv.eorzeacollection.com/");
            request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
            {
                NoCache = true,
            };

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "(none)";
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var text = System.Text.Encoding.UTF8.GetString(bytes);
            var sample = text.Length <= 240 ? text : text[..240];
            var status = $"GET {url} => {(int)response.StatusCode} {response.ReasonPhrase}; contentType={contentType}; bytes={bytes.Length}; sample={sample.ReplaceLineEndings(" ")}";

            if (!response.IsSuccessStatusCode)
            {
                return new EcHttpTextResult(null, status);
            }

            return new EcHttpTextResult(text, status);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new EcHttpTextResult(null, $"GET {url} failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static IEnumerable<string> BuildEorzeaCollectionCandidateUrls(string itemName)
    {
        foreach (var slug in BuildEorzeaCollectionSlugCandidates(itemName))
        {
            yield return $"https://ffxiv.eorzeacollection.com/gearset/{slug}";
        }
    }

    private static IEnumerable<string> BuildEorzeaCollectionSearchUrls(string itemName)
    {
        var cleanName = NormalizeEorzeaCollectionName(itemName);
        yield return $"https://ffxiv.eorzeacollection.com/gearsets?search={Uri.EscapeDataString(cleanName)}";

        var roleName = StripEquipmentSlotWords(cleanName);
        if (!string.Equals(roleName, cleanName, StringComparison.OrdinalIgnoreCase))
        {
            yield return $"https://ffxiv.eorzeacollection.com/gearsets?search={Uri.EscapeDataString(roleName)}";
        }
    }

    private static IEnumerable<string> BuildEorzeaCollectionSlugCandidates(string itemName)
    {
        var cleanName = NormalizeEorzeaCollectionName(itemName);
        var roleName = StripEquipmentSlotWords(cleanName);
        var roleSuffix = GetEorzeaCollectionRoleSuffix(cleanName);

        foreach (var name in new[] { roleName, cleanName }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var slug = Slugify(name);
            if (slug.Length > 0)
            {
                yield return slug;
            }
        }

        if (roleSuffix != null)
        {
            var firstWord = cleanName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstWord))
            {
                var familyRoleSlug = Slugify($"{firstWord} {roleSuffix}");
                if (familyRoleSlug.Length > 0)
                {
                    yield return familyRoleSlug;
                }
            }
        }
    }

    private static string NormalizeEorzeaCollectionName(string itemName)
    {
        var cleaned = itemName.Trim();

        foreach (var prefix in new[] { "Augmented ", "Antiquated ", "Replica " })
        {
            if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[prefix.Length..];
            }
        }

        cleaned = Regex.Replace(cleaned, @"\s*\([^)]*\)\s*$", string.Empty).Trim();
        return cleaned.Replace("'s", "s", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripEquipmentSlotWords(string itemName)
    {
        var roleIndex = itemName.LastIndexOf(" of ", StringComparison.OrdinalIgnoreCase);
        if (roleIndex < 0)
        {
            var simpleWords = itemName.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            while (simpleWords.Count > 1 && IsEquipmentSlotWord(simpleWords[^1]))
            {
                simpleWords.RemoveAt(simpleWords.Count - 1);
            }

            return string.Join(' ', simpleWords);
        }

        var left = itemName[..roleIndex];
        var role = itemName[(roleIndex + 4)..];
        var words = left.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

        while (words.Count > 0 && IsEquipmentSlotWord(words[^1]))
        {
            words.RemoveAt(words.Count - 1);
        }

        return words.Count == 0 ? role : $"{string.Join(' ', words)} {role}";
    }

    private static bool IsEquipmentSlotWord(string word)
    {
        return word.Equals("Mask", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Hachigane", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Togi", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Tekko", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Hakama", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Kyahan", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Circlet", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Headgear", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Cap", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Hat", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Helm", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Mail", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Chestpiece", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Coat", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Jacket", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Doublet", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Robe", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Armor", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Gloves", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Gauntlets", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Bracers", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Armguards", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Breeches", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Trousers", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Sarouel", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Pants", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Hose", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Boots", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Shoes", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Sandals", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Thighboots", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Caligae", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Greaves", StringComparison.OrdinalIgnoreCase) ||
               word.Equals("Sabatons", StringComparison.OrdinalIgnoreCase);
    }

    private static string Slugify(string value)
    {
        var chars = value
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();

        return Regex.Replace(new string(chars), "-+", "-").Trim('-');
    }

    private static bool PageMentionsItem(string html, string itemName)
    {
        return WebUtility.HtmlDecode(html).Contains(itemName, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ExtractGearsetUrls(string html)
    {
        return GearsetLinkRegex.Matches(html)
            .Select(match => $"https://ffxiv.eorzeacollection.com{match.Groups["href"].Value}")
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? ExtractBestEorzeaCollectionImageUrl(string html, string? slotName)
    {
        var absoluteUrls = ImageUrlRegex.Matches(html)
            .Select(match => WebUtility.HtmlDecode(match.Groups["url"].Value))
            .Where(IsUsableEorzeaCollectionPreviewImage);

        var relativeUrls = RelativeImageUrlRegex.Matches(html)
            .Select(match => $"https://ffxiv.eorzeacollection.com{WebUtility.HtmlDecode(match.Groups["url"].Value)}")
            .Where(IsUsableEorzeaCollectionPreviewImage);

        var urls = absoluteUrls
            .Concat(relativeUrls)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(slotName))
        {
            var maleSlot = urls.FirstOrDefault(url => UrlEndsWithImageName(url, $"hyur-male-{slotName}.png"));
            if (maleSlot != null)
            {
                return maleSlot;
            }

            var femaleSlot = urls.FirstOrDefault(url => UrlEndsWithImageName(url, $"hyur-female-{slotName}.png"));
            if (femaleSlot != null)
            {
                return femaleSlot;
            }
        }

        return urls.FirstOrDefault(url => UrlEndsWithImageName(url, "hyur-male-front.png")) ??
               urls.FirstOrDefault(url => UrlEndsWithImageName(url, "hyur-female-front.png")) ??
               urls.FirstOrDefault();
    }

    private static bool UrlEndsWithImageName(string url, string imageName)
    {
        return new Uri(url).AbsolutePath.EndsWith($"/{imageName}", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUsableEorzeaCollectionPreviewImage(string url)
    {
        return (url.Contains("gearsets.eorzeacollection.com", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("glamours.eorzeacollection.com", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("ffxiv.eorzeacollection.com", StringComparison.OrdinalIgnoreCase)) &&
               !url.Contains("/jobs/", StringComparison.OrdinalIgnoreCase) &&
               !url.Contains("/icons/", StringComparison.OrdinalIgnoreCase) &&
               !url.Contains("/flags/", StringComparison.OrdinalIgnoreCase) &&
               !url.Contains("/_nuxt/", StringComparison.OrdinalIgnoreCase);
    }

    private void DrawPreviewOverlay()
    {
        if (!Configuration.Enabled || currentItemId == 0 || !IsActivationKeyHeld())
        {
            return;
        }

        if (!DataManager.GetExcelSheet<Item>().TryGetRow(currentItemId, out var item) || !IsPreviewableItem(item))
        {
            return;
        }

        var previewSize = Math.Clamp(Configuration.PreviewSize, 140f, 360f);
        var windowSize = new Vector2(previewSize + 28f, Configuration.ShowItemMetadata ? previewSize + 116f : previewSize + 52f);
        var windowPos = hasTooltipPosition
            ? new Vector2(currentTooltipPosition.X + currentTooltipSize.X + 12f, currentTooltipPosition.Y)
            : ImGui.GetMousePos() + new Vector2(24f, 24f);
        var viewport = ImGui.GetMainViewport();

        if (windowPos.X + windowSize.X > viewport.Pos.X + viewport.Size.X)
        {
            windowPos.X = Math.Max(viewport.Pos.X + 8f, currentTooltipPosition.X - windowSize.X - 12f);
        }

        if (windowPos.Y + windowSize.Y > viewport.Pos.Y + viewport.Size.Y)
        {
            windowPos.Y = Math.Max(viewport.Pos.Y + 8f, viewport.Pos.Y + viewport.Size.Y - windowSize.Y - 8f);
        }

        ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.94f);

        var windowOpen = ImGui.Begin(
            "Glamour Display Tooltip Preview###GlamourDisplayTooltipPreview",
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoInputs);

        if (!windowOpen)
        {
            ImGui.End();
            return;
        }

        DrawItemPreview(item, previewSize);
        ImGui.End();
    }

    private bool IsActivationKeyHeld()
    {
        return Configuration.ActivationVirtualKey switch
        {
            0x10 => IsAnyModifierHeld(),
            0x11 => ImGui.GetIO().KeyCtrl || KeyState[0x11] || KeyState[0xA2] || KeyState[0xA3],
            0x12 => ImGui.GetIO().KeyAlt || KeyState[0x12] || KeyState[0xA4] || KeyState[0xA5],
            var key => KeyState[key],
        };
    }

    private bool IsAnyModifierHeld()
    {
        var io = ImGui.GetIO();

        return io.KeyShift ||
               io.KeyCtrl ||
               io.KeyAlt ||
               KeyState[0x10] ||
               KeyState[0xA0] ||
               KeyState[0xA1] ||
               KeyState[0x11] ||
               KeyState[0xA2] ||
               KeyState[0xA3] ||
               KeyState[0x12] ||
               KeyState[0xA4] ||
               KeyState[0xA5];
    }

    private static bool IsPreviewableItem(Item item)
    {
        var slot = item.EquipSlotCategory.Value;

        return slot.MainHand != 0 ||
               slot.OffHand != 0 ||
               slot.Head != 0 ||
               slot.Body != 0 ||
               slot.Gloves != 0 ||
               slot.Legs != 0 ||
               slot.Feet != 0 ||
               slot.Ears != 0 ||
               slot.Neck != 0 ||
               slot.Wrists != 0 ||
               slot.FingerL != 0 ||
               slot.FingerR != 0;
    }

    private static string GetSlotLabel(Item item)
    {
        var slot = item.EquipSlotCategory.Value;

        if (slot.MainHand != 0) return "Main Hand";
        if (slot.OffHand != 0) return "Off Hand";
        if (slot.Head != 0) return "Head";
        if (slot.Body != 0) return "Body";
        if (slot.Gloves != 0) return "Hands";
        if (slot.Legs != 0) return "Legs";
        if (slot.Feet != 0) return "Feet";
        if (slot.Ears != 0) return "Earrings";
        if (slot.Neck != 0) return "Necklace";
        if (slot.Wrists != 0) return "Bracelets";
        if (slot.FingerL != 0 || slot.FingerR != 0) return "Ring";

        return "Equipment";
    }

    internal static string GetSlotLabelForUi(Item item) => GetSlotLabel(item);

    private static string? GetEorzeaCollectionSlotName(Item item)
    {
        var slot = item.EquipSlotCategory.Value;

        if (slot.Head != 0) return "head";
        if (slot.Body != 0) return "body";
        if (slot.Gloves != 0) return "hands";
        if (slot.Legs != 0) return "legs";
        if (slot.Feet != 0) return "feet";

        return null;
    }

    private static string BuildEorzeaCollectionSearchUrl(Item item)
    {
        var escapedName = Uri.EscapeDataString(item.Name.ToString());
        return $"https://ffxiv.eorzeacollection.com/glamours?search={escapedName}";
    }

    internal sealed record EcPrefetchState
    {
        public bool IsRunning { get; init; }
        public int TotalImages { get; init; }
        public int CompletedImages { get; init; }
        public int DownloadedImages { get; init; }
        public int MissingImages { get; init; }
        public int CurrentIndexPage { get; init; }
        public int TotalIndexPages { get; init; }
        public int IndexedGearsets { get; init; }
        public int CurrentCandidateIndex { get; init; }
        public int CurrentCandidateCount { get; init; }
        public long DownloadedBytes { get; init; }
        public long KnownTotalBytes { get; init; }
        public string CurrentItemName { get; init; } = string.Empty;
        public string Status { get; init; } = "Not started.";
        public DateTime StartedUtc { get; init; }
        public DateTime FinishedUtc { get; init; }
    }

    private sealed record EcGearsetIndexEntry(string Slug, string FrontImageUrl);

    private sealed record EcHttpTextResult(string? Html, string Status);

    private sealed class EcPreviewState
    {
        public string? LocalImagePath { get; init; }
        public bool IsLoading { get; init; }
        public DateTime LastAttemptUtc { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
