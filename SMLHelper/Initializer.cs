﻿namespace SMLHelper.V2
{
    using System;
    using System.Reflection;
    using Harmony;
    using Patchers;
    using QModManager.API.ModLoading;

    /// <summary>
    /// WARNING: This class is for use only by QModManager.
    /// </summary>
    [QModCore]
    [Obsolete("This class is for use only by QModManager.", true)]
    public class Initializer
    {
        /// <summary>
        /// WARNING: This method is for use only by QModManager.
        /// </summary>
        [QModPrePatch]
        [Obsolete("This method is for use only by QModManager.", true)]
        public static void PrePatch()
        {
            Logger.Initialize(); 
#if SUBNAUTICA
            Logger.Log($"Loading v{Assembly.GetExecutingAssembly().GetName().Version} for Subnautica", LogLevel.Info);
#elif BELOWZERO
            Logger.Log($"Loading v{Assembly.GetExecutingAssembly().GetName().Version} for BelowZero", LogLevel.Info);
#endif
            TechTypePatcher.cacheManager.LoadCache();
            CraftTreeTypePatcher.cacheManager.LoadCache();
        }

        /// <summary>
        /// WARNING: This method is for use only by QModManager.
        /// </summary>
        [QModPostPatch("E3DC72597463233E62D01BD222AD0C96")]
        [Obsolete("This method is for use only by QModManager.", true)]
        public static void PostPatch()
        {
            try
            {
                Initialize();
            }
            catch (Exception e)
            {
                Logger.Error($"Caught exception while trying to initialize SMLHelper{Environment.NewLine}{e}");
            }
        }
        

        private static void Initialize()
        {
            var harmony = HarmonyInstance.Create("com.ahk1221.smlhelper");
            FishPatcher.Patch(harmony);
            TechTypePatcher.Patch(harmony);
            CraftTreeTypePatcher.Patch(harmony);
            CraftDataPatcher.Patch(harmony);
            CraftTreePatcher.Patch(harmony);
            DevConsolePatcher.Patch(harmony);
            LanguagePatcher.Patch(harmony);
            PrefabDatabasePatcher.Patch(harmony);
            SpritePatcher.Patch();
            KnownTechPatcher.Patch(harmony);
            BioReactorPatcher.Patch(harmony);
            OptionsPanelPatcher.Patch(harmony);
            ItemsContainerPatcher.Patch(harmony);
            PDAPatcher.Patch(harmony);
            PDAEncyclopediaPatcher.Patch(harmony);
            ItemActionPatcher.Patch(harmony);
            LootDistributionPatcher.Patch(harmony);
            WorldEntityDatabasePatcher.Patch(harmony);
            IngameMenuPatcher.Patch(harmony);
            TooltipPatcher.Patch(harmony);
            TechTypePatcher.cacheManager.SaveCache();
            CraftTreeTypePatcher.cacheManager.SaveCache();
        }
    }
}
