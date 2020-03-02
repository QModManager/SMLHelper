﻿namespace SMLHelper.V2.Patchers
{
    using Harmony;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    internal class DevConsolePatcher
    {
        public static List<CommandInfo> commands = new List<CommandInfo>();

        public static void Patch(HarmonyInstance harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(DevConsole), nameof(DevConsole.Submit)),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(DevConsolePatcher), nameof(DevConsolePatcher.Postfix))));

            Logger.Log("DevConsolePatcher is done.", LogLevel.Debug);
        }

        internal static void Postfix(bool __result, string value)
        {
            var separator = new char[]
            {
                ' ',
                '\t'
            };

            string text = value.Trim();
            string[] args = text.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            if (args.Length != 0)
            {
                foreach (CommandInfo command in commands)
                {
                    if (command.Name.Contains(args[0]))
                    {
                        List<string> argsList = new List<string>(args);
                        argsList.RemoveAt(0);
                        string[] newArgs = argsList.ToArray();
                        command.CommandHandler.Invoke(null, new object[] { newArgs });
                        __result = true;
                        return;
                    }
                }
            }

            __result = false;
        }
    }

    internal class CommandInfo
    {
        public MethodInfo CommandHandler;
        public string Name;
        public bool CaseSensitive;
        public bool CombineArgs;
    }
}
