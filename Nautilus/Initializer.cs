using System;
using BepInEx;
using HarmonyLib;
using Nautilus.Patchers;
using Nautilus.Utility;
using UnityEngine;
#if BELOWZERO
using UnityEngine.U2D;
#endif

namespace Nautilus;

/// <summary>
/// WARNING: This class is for use only by BepInEx.
/// </summary>
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class Initializer : BaseUnityPlugin
{
    private static readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);

#if BELOWZERO
    static Initializer()
    {
        var handle = AddressablesUtility.LoadAllAsync<SpriteAtlas>("SpriteAtlases");
        handle.Completed += SpriteManager.OnLoadedSpriteAtlases;
        // Please dont use this method. I hate using it but we have no other choice.
        _ = handle.WaitForCompletion();
    }
#endif

    /// <summary>
    /// WARNING: This method is for use only by BepInEx.
    /// </summary>
    [Obsolete("This method is for use only by Bepinex.", true)]
    Initializer()
    {
        GameObject obj = UWE.Utils.GetEntityRoot(this.gameObject) ?? this.gameObject;
        obj.EnsureComponent<SceneCleanerPreserve>();

        InternalLogger.Initialize(Logger);
#if SUBNAUTICA
        InternalLogger.Info($"Loading v{PluginInfo.PLUGIN_VERSION} for Subnautica");
#elif BELOWZERO
        InternalLogger.Info($"Loading v{PluginInfo.PLUGIN_VERSION} for BelowZero");
#endif
        PrefabDatabasePatcher.PrePatch(_harmony);
        EnumPatcher.Patch(_harmony);
        CraftDataPatcher.Patch(_harmony);
        CraftTreePatcher.Patch(_harmony);
        ConsoleCommandsPatcher.Patch(_harmony);
        LanguagePatcher.Patch(_harmony);
        PrefabDatabasePatcher.PostPatch(_harmony);
        KnownTechPatcher.Patch(_harmony);
        OptionsPanelPatcher.Patch(_harmony);
        SMLHelperCompatibilityPatcher.Patch(_harmony);
        ItemsContainerPatcher.Patch(_harmony);
        PDALogPatcher.Patch(_harmony);
        PDAPatcher.Patch(_harmony);
        PDAEncyclopediaPatcher.Patch(_harmony);
        ItemActionPatcher.Patch(_harmony);
        LootDistributionPatcher.Patch(_harmony);
        WorldEntityDatabasePatcher.Patch(_harmony);
        LargeWorldStreamerPatcher.Patch(_harmony);
        SaveUtilsPatcher.Patch(_harmony);
        TooltipPatcher.Patch(_harmony);
        SurvivalPatcher.Patch(_harmony);
        CustomSoundPatcher.Patch(_harmony);
        MaterialUtils.Patch();
        FontReferencesPatcher.Patch(_harmony);
        VehicleUpgradesPatcher.Patch(_harmony);
        StoryGoalPatcher.Patch(_harmony);
        PDAEncyclopediaTabPatcher.Patch(_harmony);
        NewtonsoftJsonPatcher.Patch(_harmony);
        InventoryPatcher.Patch(_harmony);
    }
}