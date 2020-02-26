﻿#if BELOWZERO
namespace SMLHelper.V2.Patchers
{
    using System.Collections.Generic;
    using Harmony;

    internal partial class CraftDataPatcher
    {
#region Internal Fields

        internal static IDictionary<TechType, JsonValue> CustomTechData = new SelfCheckingDictionary<TechType, JsonValue>("CustomTechData", AsStringFunction);

#endregion

        internal static void AddToCustomTechData(TechType techType, JsonValue techData)
        {
            CustomTechData.Add(techType, techData);
        }

        private static void PatchForBelowZero(HarmonyInstance harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(CraftData), nameof(CraftData.PreparePrefabIDCache)),
               postfix: new HarmonyMethod(AccessTools.Method(typeof(CraftDataPatcher), nameof(TechDataCachePostfix))));
            harmony.Patch(AccessTools.Method(typeof(TechData), nameof(TechData.Cache)),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(CraftDataPatcher), nameof(TechDataCachePostfix))));
        }

        private static void TechDataCachePostfix()
        {
            if (CustomTechData.Count > 0)
                AddCustomTechDataToOriginalDictionary();
        }

        private static void AddCustomTechDataToOriginalDictionary()
        {
            short added = 0;
            short replaced = 0;
            foreach (TechType techType in CustomTechData.Keys)
            {
                bool techDataExists = TechData.entries.ContainsKey(techType);
                if (techDataExists && TechData.entries[techType] != CustomTechData[techType])
                {
                    if (TechData.TryGetValue(techType, out JsonValue originalData))
                    {
                        foreach (int key in CustomTechData[techType].Keys)
                        {
                            TechData.entries[techType][key] = CustomTechData[techType][key];
                        }

                        Logger.Log($"{techType} TechType already existed in the CraftData.techData dictionary. Original value was replaced.", LogLevel.Warn);
                        replaced++;
                        Logger.Log($"Replaced Item: " + techType + " " + TechData.Contains(techType), LogLevel.Debug);
                    }
                }
                else if (!techDataExists)
                {
                    TechData.Add(techType, CustomTechData[techType]);
                    added++;
                    Logger.Log($"Added Item: " + techType + " " + TechData.Contains(techType), LogLevel.Debug);
                }
            }

            CustomTechData.Clear();
            if (added > 0)
                Logger.Log($"Added {added} new entries to the CraftData.techData dictionary.", LogLevel.Info);
            if (replaced > 0)
                Logger.Log($"Replaced {replaced} existing entries to the CraftData.techData dictionary.", LogLevel.Info);
        }
    }
}
#endif
