using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using Nautilus.Utility;
using UnityEngine;

namespace Nautilus.Patchers;

internal partial class CraftDataPatcher
{
    #region Internal Fields

    private static readonly Func<TechType, string> AsStringFunction = (t) => t.AsString();

    #endregion

    #region Group Handling

    internal static bool ModPrefabsPatched;

    internal static void AddToGroup(TechGroup group, TechCategory category, TechType techType, TechType target, bool after)
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

        techCategory.Remove(techType);

        int index = techCategory.IndexOf(target);

        if (index == -1) // Not found
        {
            techCategory.Insert(after ? techCategory.Count : 0, techType);
            InternalLogger.Log($"{(after ? "Add" : "Insert")}ed \"{techType:G}\" {(after ? "" : "in")}to groups under \"{group:G}->{category:G}\"", LogLevel.Debug);
        }
        else
        {
            techCategory.Insert(index + (after ? 1 : 0), techType);
            InternalLogger.Log($"{(after ? "Add" : "Insert")}ed \"{techType:G}\" {(after ? "" : "in")}to groups under \"{group:G}->{category:G}\" {(after ? "after" : "before")} \"{target:G}\"", LogLevel.Debug);
        }
    }

    internal static void RemoveFromGroup(TechGroup group, TechCategory category, TechType techType)
    {
        if (CraftData.groups.TryGetValue(group, out var techGroup)
            && techGroup.TryGetValue(category, out var techCategory)
            && techCategory.Remove(techType))
        {
            InternalLogger.Log($"Successfully Removed \"{techType:G}\" from groups under \"{group:G}->{category:G}\"", LogLevel.Debug);
        }
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
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CraftDataPatcher), nameof(CraftDataPrefabIDCachePrefix))),
            postfix: new HarmonyMethod(AccessTools.Method(typeof(CraftDataPatcher), nameof(CraftDataPrefabIDCachePostfix))));

        InternalLogger.Log("CraftDataPatcher is done.", LogLevel.Debug);
    }
    
    [HarmonyPatch(typeof(uGUI_CraftingMenu), nameof(uGUI_CraftingMenu.IsGrid))] 
    [HarmonyPrefix]
    private static bool ShouldGridPostfix(uGUI_CraftingMenu.Node node, ref bool __result)
    { 
        __result = ShouldGrid();
        return false;

        bool ShouldGrid()
        {
            var craftings = 0;
            var tabs = 0;

            foreach (var child in node)
            {
                if (child.action == TreeAction.Expand)
                {
                    tabs++;
                }
                else if (child.action == TreeAction.Craft)
                {
                    craftings++;
                }
            }

            return craftings > tabs;
        }
    }

    [HarmonyPatch(typeof(uGUI_CraftingMenu), nameof(uGUI_CraftingMenu.Collapse))] 
    [HarmonyPostfix]
    private static void CollapsePostfix(uGUI_CraftingMenu.Node parent)
    {
        if (parent == null) return;

        if (parent.action != TreeAction.Craft) return;

        parent.icon.SetActive(false);
    }

    [HarmonyPatch(typeof(uGUI_CraftingMenu), nameof(uGUI_CraftingMenu.Expand))]
    [HarmonyPostfix]
    private static void uGUI_CraftingMenuExpandPostfix(uGUI_CraftingMenu __instance, uGUI_CraftingMenu.Node node)
    {
        uGUI_CraftingMenu.Node parent = node.parent as uGUI_CraftingMenu.Node;
        if (node.parent == null)
        {
            return;
        }
        if (__instance.IsGrid(node))
        {
            using (IEnumerator<uGUI_CraftingMenu.Node> childEnumerator = parent.GetEnumerator())
            {
                while (childEnumerator.MoveNext())
                {
                    uGUI_CraftingMenu.Node thisChild = childEnumerator.Current;
                    if (thisChild == node)
                    {
                        continue;
                    }
                    if (thisChild.action == TreeAction.Expand)
                    {
                        thisChild.icon.SetActive(false);
                    }
                }
            }
        }
    }
    [HarmonyPrefix]
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

    private static bool NeedsPatching = true;

    private static void CraftDataPrefabIDCachePrefix()
    {
        NeedsPatching = CraftData.cacheInitialized;
    }

    private static void CraftDataPrefabIDCachePostfix()
    {
        if(!NeedsPatching && ModPrefabsPatched)
            return;

        Dictionary<TechType, string> techMapping = CraftData.techMapping;
        Dictionary<string, TechType> entClassTechTable = CraftData.entClassTechTable;
        foreach (var prefab in PrefabHandler.Prefabs)
        {
            if (prefab.Key.TechType is TechType.None)
                continue;
                
            techMapping[prefab.Key.TechType] = prefab.Key.ClassID;
            entClassTechTable[prefab.Key.ClassID] = prefab.Key.TechType;
        }
        ModPrefabsPatched = true;
    }
    #endregion
}