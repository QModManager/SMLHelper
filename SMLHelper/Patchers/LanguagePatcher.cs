﻿namespace SMLHelper.V2.Patchers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using HarmonyLib;
    using SMLHelper.V2.Utility;

    internal class LanguagePatcher
    {
        private static readonly string LanguageDir = Path.Combine(Path.Combine(BepInEx.Paths.ConfigPath, Assembly.GetExecutingAssembly().GetName().Name), "Language");
        private static readonly string LanguageOrigDir = Path.Combine(LanguageDir, "Originals");
        private static readonly string LanguageOverDir = Path.Combine(LanguageDir, "Overrides");
        private const char KeyValueSeparator = ':';
        private const int SplitCount = 2;

        private static readonly Dictionary<string, Dictionary<string, string>> originalCustomLines = new Dictionary<string, Dictionary<string, string>>();
        private static readonly Dictionary<string, string> customLines = new Dictionary<string, string>();

        internal static void RepatchCheck(ref Language __instance, string key)
        {
            if (string.IsNullOrEmpty(key) || !customLines.TryGetValue(key, out string customValue))
                return;

            if (!__instance.strings.TryGetValue(key, out string currentValue) || customValue != currentValue)
                InsertCustomLines(ref __instance);
        }

        internal static void InsertCustomLines(ref Language __instance)
        {
            // Direct access to private fields made possible by https://github.com/CabbageCrow/AssemblyPublicizer/
            // See README.md for details.
            Dictionary<string, string> strings = __instance.strings;
            foreach (KeyValuePair<string, string> a in customLines)
            {
                strings[a.Key] = a.Value;
            }
        }

        internal static void Patch(Harmony harmony)
        {
            if (!Directory.Exists(LanguageDir))
                Directory.CreateDirectory(LanguageDir);

            WriteOriginalCustomLines();

            ReadOverrideCustomLines();


            HarmonyMethod repatchCheckMethod = new HarmonyMethod(AccessTools.Method(typeof(LanguagePatcher), nameof(LanguagePatcher.RepatchCheck)));
            HarmonyMethod insertLinesMethod = new HarmonyMethod(AccessTools.Method(typeof(LanguagePatcher), nameof(LanguagePatcher.InsertCustomLines)));

            harmony.Patch(AccessTools.Method(typeof(Language), nameof(Language.ParseMetaData)), prefix: insertLinesMethod);
            harmony.Patch(AccessTools.Method(typeof(Language), nameof(Language.GetKeysFor)), prefix: insertLinesMethod);
            harmony.Patch(AccessTools.Method(typeof(Language), nameof(Language.TryGet)), prefix: repatchCheckMethod);
            harmony.Patch(AccessTools.Method(typeof(Language), nameof(Language.Contains)), prefix: repatchCheckMethod);

            InternalLogger.Log("LanguagePatcher is done.", LogLevel.Debug);
        }

        private static void WriteOriginalCustomLines()
        {
            if (!Directory.Exists(LanguageOrigDir))
                Directory.CreateDirectory(LanguageOrigDir);

            if (originalCustomLines.Count == 0)
                return;

            int filesWritten = 0;
            foreach (string modKey in originalCustomLines.Keys)
            {
                if (!FileNeedsRewrite(modKey))
                    continue; // File is identical to captured lines. No need to rewrite it.

                InternalLogger.Log($"Writing original language lines file for {modKey}", LogLevel.Debug);
                if (WriteOriginalLinesFile(modKey))
                    filesWritten++;
                else
                    InternalLogger.Log($"Error writing language lines file for {modKey}", LogLevel.Warn);
            }

            if (filesWritten > 0)
                InternalLogger.Log($"Updated {filesWritten} of {originalCustomLines.Count} original language files.", LogLevel.Debug);
        }

        private static bool WriteOriginalLinesFile(string modKey)
        {
            if (string.IsNullOrEmpty(modKey))
                return false;

            Dictionary<string, string> modCustomLines = originalCustomLines[modKey];
            var text = new StringBuilder();
            foreach (string langLineKey in modCustomLines.Keys)
            {
                if (!modCustomLines.TryGetValue(langLineKey, out string line) || string.IsNullOrEmpty(line))
                    continue;

                string valueToWrite = line.Replace("\n", "\\n").Replace("\r", "\\r");
                text.AppendLine($"{langLineKey}{KeyValueSeparator}{valueToWrite}");
            }

            if (text.Length > 0)
            {
                File.WriteAllText(Path.Combine(LanguageOrigDir, $"{modKey}.txt"), text.ToString(), Encoding.UTF8);
                return true;
            }

            return false;            
        }

        private static void ReadOverrideCustomLines()
        {
            if (!Directory.Exists(LanguageOverDir))
                Directory.CreateDirectory(LanguageOverDir);

            string[] files = Directory.GetFiles(LanguageOverDir);

            InternalLogger.Log($"{files.Length} language override files found.", LogLevel.Debug);

            if (files.Length == 0)
                return;

            foreach (string file in files)
            {
                string modName = Path.GetFileNameWithoutExtension(file);

                if (!originalCustomLines.ContainsKey(modName))
                    continue; // Not for a mod we know about

                string[] languageLines = File.ReadAllLines(file, Encoding.UTF8);

                Dictionary<string, string> originalLines = originalCustomLines[modName];

                int overridesApplied = ExtractOverrideLines(modName, languageLines, originalLines);

                InternalLogger.Log($"Applied {overridesApplied} language overrides to mod {modName}.", LogLevel.Info);
            }
        }

        internal static int ExtractOverrideLines(string modName, string[] languageLines, Dictionary<string, string> originalLines)
        {
            int overridesApplied = 0;
            for (int lineIndex = 0; lineIndex < languageLines.Length; lineIndex++)
            {
                string line = languageLines[lineIndex];
                if (string.IsNullOrEmpty(line))
                    continue; // Skip empty lines

                string[] split = line.Split(new[] { KeyValueSeparator }, SplitCount, StringSplitOptions.RemoveEmptyEntries);

                string key = split[0];

                if (split.Length != SplitCount)
                {
                    InternalLogger.Log($"Line '{lineIndex}' in language override file for '{modName}' was incorrectly formatted.", LogLevel.Warn);
                    continue; // Not correctly formatted
                }

                if (!originalLines.ContainsKey(key))
                {
                    InternalLogger.Log($"Key '{key}' on line '{lineIndex}' in language override file for '{modName}' did not match an original key.", LogLevel.Warn);
                    continue; // Skip keys we don't recognize.
                }

                string value = RemoveOptionalDelimiters(split[1]);

                customLines[key] = value.Replace("\\n", "\n").Replace("\\r", "\r");
                overridesApplied++;
            }

            return overridesApplied;
        }

        private static string RemoveOptionalDelimiters(string value)
        {
            const int firstChar = 0;
            int lastChar = value.Length - 1;

            if (value[firstChar] == '{' && value[lastChar] == '}' &&
                value[firstChar + 2] != '}' && value[lastChar - 2] != '{')
            {
                value = value.Substring(1, lastChar - 1);
            }

            return value;
        }

        private static bool FileNeedsRewrite(string modKey)
        {
            Dictionary<string, string> modCustomLines = originalCustomLines[modKey];
            string fileName = Path.Combine(LanguageOrigDir, $"{modKey}.txt");

            if (!File.Exists(fileName))
                return true; // File not found

            string[] lines = File.ReadAllLines(fileName, Encoding.UTF8);

            if (lines.Length != modCustomLines.Count)
                return true; // Difference in line count

            // Confirm if the file actually needs to be updated
            foreach (string line in lines)
            {
                string[] split = line.Split(new[] { KeyValueSeparator }, SplitCount, StringSplitOptions.RemoveEmptyEntries);

                if (split.Length != SplitCount)
                {
                    return true; // Not correctly formatted
                }

                string lineKey = split[0];
                string lineValue = split[1].Replace("\\n", "\n").Replace("\\r", "\r");

                if (modCustomLines.TryGetValue(lineKey, out string origValue))
                {
                    if (origValue != lineValue)
                    {
                        return true; // Difference in line content
                    }
                }
                else
                {
                    return true; // Key not found
                }
            }

            return false; // All lines matched and valid
        }

        internal static void AddCustomLanguageLine(string modAssemblyName, string lineId, string text)
        {
            if (!originalCustomLines.ContainsKey(modAssemblyName))
                originalCustomLines.Add(modAssemblyName, new Dictionary<string, string>());

            originalCustomLines[modAssemblyName][lineId] = text;
            customLines[lineId] = text;
        }

        internal static string GetCustomLine(string key)
        {
            return customLines[key];
        }
    }
}
