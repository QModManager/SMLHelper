﻿namespace SMLHelper.V2.Patchers
{
    using System;
    using System.Collections.Generic;
    using Assets;
    using HarmonyLib;
    using SMLHelper.V2.Patchers.EnumPatching;
    using SMLHelper.V2.Utility;

    internal partial class CraftDataPatcher
    {
        #region Internal Fields

        private static readonly Func<TechType, string> AsStringFunction = (t) => t.AsString();

        #endregion

        #region Group Handling

        internal static void AddToCustomGroup(TechGroup group, TechCategory category, TechType techType, TechType after)
        {
            if (!CraftData.groups.TryGetValue(group, out Dictionary<TechCategory, List<TechType>> techGroup))
            {
                // Should never happen, but doesn't hurt to add it.
                InternalLogger.Log("Invalid TechGroup!", LogLevel.Error);
                return;
            }

            if (!techGroup.TryGetValue(category, out List<TechType> techCategory))
            {
                InternalLogger.Log($"{group} does not contain {category} as a registered group. Please ensure to register your TechCategory to the TechGroup using the TechCategoryHandler before using the combination.", LogLevel.Error);
                return;
            }

            if(techCategory.Contains(techType))
            {
                InternalLogger.Log($"\"{techType.AsString():G}\" Already exists at \"{group:G}->{category:G}\", Skipping Duplicate Entry", LogLevel.Debug);
                return;
            }

            int index = techCategory.IndexOf(after);

            if (index == -1) // Not found
            {
                techCategory.Add(techType);
                InternalLogger.Log($"Added \"{techType.AsString():G}\" to groups under \"{group:G}->{category:G}\"", LogLevel.Debug);
            }
            else
            {
                techCategory.Insert(index + 1, techType);

                InternalLogger.Log($"Added \"{techType.AsString():G}\" to groups under \"{group:G}->{category:G}\" after \"{after.AsString():G}\"", LogLevel.Debug);
            }
        }

        internal static void RemoveFromCustomGroup(TechGroup group, TechCategory category, TechType techType)
        {
            if(!CraftData.groups.TryGetValue(group, out Dictionary<TechCategory, List<TechType>> techGroup))
                return;

            if(!techGroup.TryGetValue(category, out List<TechType> techCategory))
                return;

            if(!techCategory.Contains(techType))
                return;

            techCategory.Remove(techType);
            InternalLogger.Log($"Successfully Removed \"{techType.AsString():G}\" from groups under \"{group:G}->{category:G}\"", LogLevel.Debug);
        }

        #endregion

        #region Patching

        internal static void Patch(Harmony harmony)
        {
#if SUBNAUTICA
            PatchForSubnautica(harmony);
#elif BELOWZERO
            PatchForBelowZero(harmony);
#endif
            harmony.Patch(AccessTools.Method(typeof(CraftData), nameof(CraftData.PreparePrefabIDCache)),
               postfix: new HarmonyMethod(AccessTools.Method(typeof(CraftDataPatcher), nameof(CraftDataPrefabIDCachePostfix))));

            InternalLogger.Log("CraftDataPatcher is done.", LogLevel.Debug);
        }

        private static void CraftDataPrefabIDCachePostfix()
        {
            if (ModPrefab.ModPrefabsPatched) return;

            Dictionary<TechType, string> techMapping = CraftData.techMapping;
            Dictionary<string, TechType> entClassTechTable = CraftData.entClassTechTable;
            foreach (ModPrefab prefab in ModPrefab.Prefabs)
            {
                techMapping[prefab.TechType] = prefab.ClassID;
                entClassTechTable[prefab.ClassID] = prefab.TechType;
            }
            ModPrefab.ModPrefabsPatched = true;
        }
        #endregion
    }
}
