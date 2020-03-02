﻿namespace SMLHelper.V2.Patchers
{
    using Harmony;
    using QModManager.API;
    using SMLHelper.V2.Handlers;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Text;
#if BELOWZERO
    using QModManager;
#endif

    internal class TooltipPatcher
    {
        internal static bool DisableEnumIsDefinedPatch = false;

        internal static void Patch(HarmonyInstance harmony)
        {
            Initialize();

            harmony.Patch(AccessTools.Method(typeof(TooltipFactory), nameof(TooltipFactory.InventoryItem)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TooltipPatcher), nameof(TooltipPatcher.Transpiler))));

            harmony.Patch(AccessTools.Method(typeof(TooltipFactory), nameof(TooltipFactory.InventoryItemView)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TooltipPatcher), nameof(TooltipPatcher.Transpiler))));

            harmony.Patch(AccessTools.Method(typeof(TooltipFactory), nameof(TooltipFactory.BuildTech)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TooltipPatcher), nameof(TooltipPatcher.Transpiler))));

            harmony.Patch(AccessTools.Method(typeof(TooltipFactory), nameof(TooltipFactory.Recipe)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TooltipPatcher), nameof(TooltipPatcher.Transpiler))));

            Logger.Log("TooltipPatcher is done.", LogLevel.Debug);
        }

        internal static void CustomTooltip(StringBuilder sb, TechType techType)
        {
            if (ExtraItemInfoOption == ExtraItemInfo.Nothing) return;

            if (ExtraItemInfoOption == ExtraItemInfo.ModNameAndItemID) WriteTechType(sb, techType);
            else WriteSpace(sb);

            if (IsVanillaTechType(techType))
                WriteModName(sb, "Subnautica");
            else if (TechTypePatcher.cacheManager.ContainsKey(techType))
                WriteModNameFromTechType(sb, techType);
            else
                WriteModNameError(sb, "Unknown Mod", "Item added without SMLHelper");
        }
        internal static void CustomTooltip(StringBuilder sb, InventoryItem item)
        {
            CustomTooltip(sb, item.item.GetTechType());
        }

        internal static void WriteTechType(StringBuilder sb, TechType techType)
        {
            sb.AppendFormat("\n\n<size=19><color=#808080FF>{0} ({1})</color></size>", techType.AsString(), (int)techType);
        }
        internal static void WriteModName(StringBuilder sb, string text)
        {
            sb.AppendFormat("\n<size=23><color=#00ffffff>{0}</color></size>", text);
        }
        internal static void WriteModNameError(StringBuilder sb, string text, string reason)
        {
            sb.AppendFormat("\n<size=23><color=#ff0000ff>{0}</color></size>\n<size=17><color=#808080FF>({1})</color></size>", text, reason);
        }
        internal static void WriteModNameFromTechType(StringBuilder sb, TechType type)
        {
            // if (MissingTechTypes.Contains(type)) WriteModNameError(sb, "Mod Missing");
            // This is for something else I am going to do

            if (TechTypeHandler.TechTypesAddedBy.TryGetValue(type, out Assembly assembly))
            {
                string modName = null;
                
                foreach (IQMod mod in QModServices.Main.GetAllMods())
                {
                    if (mod == null || mod.LoadedAssembly == null) continue;
                    if (mod.LoadedAssembly == assembly)
                    {
                        modName = mod.DisplayName;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(modName))
                {
                    WriteModNameError(sb, "Unknown Mod", "Mod could not be determined");
                }
                else
                {
                    WriteModName(sb, modName);
                }
            }
            else
            {
                WriteModNameError(sb, "Unknown Mod", "Assembly could not be determined");
            }
        }
        internal static void WriteSpace(StringBuilder sb)
        {
            sb.AppendFormat("\n<size=19></size>");
        }

        internal static bool IsVanillaTechType(TechType type)
        {
            DisableEnumIsDefinedPatch = true;
            bool result = Enum.IsDefined(typeof(TechType), type);
            DisableEnumIsDefinedPatch = false;
            return result;
        }

        #region Options

        internal enum ExtraItemInfo
        {
            ModName,
            ModNameAndItemID,
            Nothing
        }

        internal static ExtraItemInfo ExtraItemInfoOption { get; private set; }

        internal static void SetExtraItemInfo(ExtraItemInfo value)
        {
            string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ExtraItemInfo.txt");

            string text;
            switch (value)
            {
                case ExtraItemInfo.ModName:
                    text = "Mod name (default)";
                    break;
                case ExtraItemInfo.ModNameAndItemID:
                    text = "Mod name and item ID";
                    break;
                case ExtraItemInfo.Nothing:
                    text = "Nothing";
                    break;
                default:
                    return;
            }

            File.WriteAllText(configPath, text);
            ExtraItemInfoOption = value;
        }

        internal static bool Initialized = false;

        internal static void Initialize()
        {
            if (Initialized) return;
            Initialized = true;

            string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ExtraItemInfo.txt");

            if (!File.Exists(configPath))
            {
                File.WriteAllText(configPath, "Mod name (default)");
                ExtraItemInfoOption = ExtraItemInfo.ModName;

                return;
            }

            string fileContents = File.ReadAllText(configPath);

            switch (fileContents)
            {
                case "Mod name (default)":
                    ExtraItemInfoOption = ExtraItemInfo.ModName;
                    Logger.Log($"Extra item info set to: {fileContents}", LogLevel.Info);
                    break;
                case "Mod name and item ID":
                    ExtraItemInfoOption = ExtraItemInfo.ModNameAndItemID;
                    Logger.Log($"Extra item info set to: {fileContents}", LogLevel.Info);
                    break;
                case "Nothing":
                    ExtraItemInfoOption = ExtraItemInfo.Nothing;
                    Logger.Log($"Extra item info set to: {fileContents}", LogLevel.Info);
                    break;
                default:
                    File.WriteAllText(configPath, "Mod name (default)");
                    ExtraItemInfoOption = ExtraItemInfo.ModName;
                    Logger.Log("Error reading ExtraItemInfo.txt configuration file. Defaulted to mod name.", LogLevel.Warn);
                    break;
            }
        }

        #endregion

        #region Patches

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            // Turn the IEnumerable into a List
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            // Get the target op code based on what method is being patched
            // Callvirt for InventoryItem and InventoryItemView, Ldarg_2 for BuildTech and Recipe
            OpCode targetCode = method.Name.Contains("InventoryItem") ? OpCodes.Callvirt : OpCodes.Ldarg_2;
            // Get the last occurance of the code
            int entryIndex = codes.FindLastIndex(c => c.opcode == targetCode);

            // Get the fnction that will be injected based on what method is currently being patched
            // CustomTooltip(StringBuilder, InventoryItem) for InventoryItem and InventoryItemView, CustomTooltip(StringBuilder, TechType) for BuildTech and Recipe
            Expression<Action> methodInfo;
            if (method.Name.Contains("InventoryItem")) methodInfo = () => CustomTooltip(null, null);
            else methodInfo = () => CustomTooltip(null, TechType.None);

            // Remove all labels at the target op code
            List<Label> labelsToMove = codes[entryIndex].labels.ToArray().ToList();
            codes[entryIndex].labels.Clear();

            // Change first custom IL code based on what method is being patched
            // Dup for InventoryItem and InventoryItemView, Ldloc_0 for BuildTech and Recipe
            OpCode firstCode = method.Name.Contains("InventoryItem") ? OpCodes.Dup : OpCodes.Ldloc_0;
            
            // Insert custom IL codes
            CodeInstruction[] codesToInsert = new CodeInstruction[]
            {
                new CodeInstruction(firstCode)
                {
                    // Move labels accordingly
                    labels = labelsToMove
                },
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(methodInfo)),
            };
            codes.InsertRange(entryIndex, codesToInsert);

            // Return modified IL
            return codes.AsEnumerable();
        }

        #endregion
    }
}
