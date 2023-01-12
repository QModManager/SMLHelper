﻿namespace SMLHelper.Patchers
{
    using System;
    using System.Collections.Generic;
    using Assets;
    using BepInEx.Logging;
    using HarmonyLib;
    using Utility;
    using UnityEngine;

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
            {
                return;
            }

            if(!techGroup.TryGetValue(category, out List<TechType> techCategory))
            {
                return;
            }

            if(!techCategory.Contains(techType))
            {
                return;
            }

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
            PatchUtils.PatchClass(harmony);

            InternalLogger.Log("CraftDataPatcher is done.", LogLevel.Debug);
        }

        [PatchUtils.Prefix]
        [HarmonyPatch(typeof(CraftData), nameof(CraftData.GetTechType), new Type[] { typeof(GameObject), typeof(GameObject) }, argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out })]
        private static void CraftDataGetTechTypePrefix(GameObject obj, out GameObject go, ref TechType __result)
        {
            CraftData.PreparePrefabIDCache();
            Transform transform = obj.transform;
            TechTag techTag = null;
            PrefabIdentifier prefabIdentifier = null;

            while(transform != null && !transform.TryGetComponent(out prefabIdentifier) && !transform.TryGetComponent(out techTag))
            {
                transform = transform.parent;
            }

            if(prefabIdentifier != null)
            {
                go = prefabIdentifier.gameObject;
                __result = CraftData.entClassTechTable.GetOrDefault(prefabIdentifier.ClassId, TechType.None);
                return;
            }

            if(techTag != null)
            {
                go = techTag.gameObject;
                __result = techTag.type;
                return;
            }

            go = null;
            __result = TechType.None;
            return;
        }

        private static void CraftDataPrefabIDCachePostfix()
        {
            if (ModPrefabCache.ModPrefabsPatched)
            {
                return;
            }

            Dictionary<TechType, string> techMapping = CraftData.techMapping;
            Dictionary<string, TechType> entClassTechTable = CraftData.entClassTechTable;
            foreach (ModPrefab prefab in ModPrefabCache.Prefabs)
            {
                techMapping[prefab.TechType] = prefab.ClassID;
                entClassTechTable[prefab.ClassID] = prefab.TechType;
            }
            ModPrefabCache.ModPrefabsPatched = true;
        }
        #endregion
    }
}
