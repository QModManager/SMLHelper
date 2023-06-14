﻿using System.Collections.Generic;
using HarmonyLib;
using Nautilus.Utility;
using UWE;

namespace Nautilus.Patchers;

internal class WorldEntityDatabasePatcher
{
    internal static readonly SelfCheckingDictionary<string, WorldEntityInfo> CustomWorldEntityInfos = new("CustomWorldEntityInfo");

    internal static void Patch(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(WorldEntityDatabase), nameof(WorldEntityDatabase.TryGetInfo)),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(WorldEntityDatabasePatcher), nameof(WorldEntityDatabasePatcher.Prefix))));
    }

    private static bool Prefix(string classId, ref WorldEntityInfo info, ref bool __result)
    {
        foreach (KeyValuePair<string, WorldEntityInfo> entry in CustomWorldEntityInfos)
        {
            if (entry.Key == classId)
            {
                __result = true;
                info = entry.Value;
                return false;
            }
        }

        return true;
    }
}