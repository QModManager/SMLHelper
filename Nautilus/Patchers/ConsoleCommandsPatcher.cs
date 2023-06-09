﻿using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Commands;
using Nautilus.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Nautilus.Patchers;

internal static class ConsoleCommandsPatcher
{
    private static Dictionary<string, ConsoleCommand> ConsoleCommands = new();

    private static Color CommandColor = new(1, 1, 0);
    private static Color ParameterTypeColor = new(0, 1, 1);
    private static Color ParameterInputColor = new(1, 0, 0);
    private static Color ParameterOptionalColor = new(0, 1, 0);
    private static Color ModOriginColor = new(0, 1, 0);
    private static Color ModConflictColor = new(0.75f, 0.75f, 0.75f);

    public static void Patch(Harmony harmony)
    {
        harmony.PatchAll(typeof(ConsoleCommandsPatcher));
        InternalLogger.Debug("ConsoleCommandsPatcher is done.");
    }

    /// <summary>
    /// Adds a custom console command from a target method/delegate.
    /// </summary>
    /// <param name="command">The command string that a user should enter.</param>
    /// <param name="targetMethod">The targeted method.</param>
    /// <param name="isDelegate">Whether the method is a delegate.</param>
    /// <param name="instance">The instance the method belongs to.</param>
    public static void AddCustomCommand(string command, MethodInfo targetMethod, bool isDelegate = false, object instance = null)
    {
        ConsoleCommand consoleCommand = new(command, targetMethod, isDelegate, instance);

        // if this command string was already registered, print an error and don't add it
        if (ConsoleCommands.TryGetValue(consoleCommand.Trigger, out ConsoleCommand alreadyDefinedCommand))
        {
            string error = $"Could not register custom command {GetColoredString(consoleCommand)} for mod " +
                           $"{GetColoredString(consoleCommand.ModName, ModOriginColor)}\n" +
                           $"{GetColoredString(alreadyDefinedCommand.ModName, ModConflictColor)} already registered this command!";

            LogAndAnnounce(error, LogLevel.Error);

            return;
        }

        // if this command's method is invalid (not a public static, for example), print an error and don't add it
        if (!consoleCommand.HasValidInvoke())
        {
            string error = $"Could not register custom command {GetColoredString(consoleCommand)} for mod " +
                           $"{GetColoredString(consoleCommand.ModName, ModOriginColor)}\n" +
                           "Target method must be static.";

            LogAndAnnounce(error, LogLevel.Error);

            return;
        }

        // if any of the parameter types of the method are unsupported, print an error and don't add it
        if (!consoleCommand.HasValidParameterTypes())
        {
            string error = $"Could not register custom command {GetColoredString(consoleCommand)} for mod " +
                           $"{GetColoredString(consoleCommand.ModName, ModOriginColor)}\n" +
                           "The following parameters have unsupported types:\n" +
                           consoleCommand.GetInvalidParameters().Select(param => GetColoredString(param)).Join(delimiter: "\n") +
                           "Supported parameter types:\n" +
                           Parameter.SupportedTypes.Select(type => type.Name).Join();

            LogAndAnnounce(error, LogLevel.Error);

            return;
        }

        ConsoleCommands.Add(consoleCommand.Trigger, consoleCommand);
    }

    /// <summary>
    /// Searches the given <paramref name="type"/> for methods decorated with the <see cref="ConsoleCommandAttribute"/> and
    /// passes them on to <see cref="AddCustomCommand(string, MethodInfo, bool, object)"/>.
    /// </summary>
    /// <param name="type">The type within which to search.</param>
    public static void ParseCustomCommands(Type type)
    {
        foreach (MethodInfo targetMethod in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            ConsoleCommandAttribute customCommandAttribute = targetMethod.GetCustomAttribute<ConsoleCommandAttribute>(false);
            if (customCommandAttribute != null)
            {
                AddCustomCommand(customCommandAttribute.Command, targetMethod);
            }
        }
    }

    /// <summary>
    /// Harmony patch on the <see cref="DevConsole"/> to intercept user submissions.
    /// </summary>
    /// <param name="value">The submitted value.</param>
    /// <param name="__result">Original result of the method, used to determine whether or not the string will be added to the
    /// <see cref="DevConsole.history"/>.</param>
    /// <returns>Whether or not to let the original method run.</returns>
    [HarmonyPatch(typeof(DevConsole), nameof(DevConsole.Submit))]
    [HarmonyPrefix]
    private static bool DevConsole_Submit_Prefix(string value, out bool __result)
    {
        if (HandleCommand(value)) // We have handled the command, whether the parameters were valid or not
        {
            __result = true; // Command should be added to the history
            return false; // Don't run original method
        }

        __result = false; // Default value
        return true; // Let the original method try to handle the command
    }

    /// <summary>
    /// Forces history to always be saved in the command line, regardless of whether it was successful or not.
    /// </summary>
    /// <param name="__result"></param>
    [HarmonyPatch(typeof(DevConsole), nameof(DevConsole.OnSubmit))]
    [HarmonyPostfix]
    private static void DevConsole_OnSubmit_Postfix(out bool __result)
    {
        __result = true;
    }

    /// <summary>
    /// Attempts to handle a user command.
    /// </summary>
    /// <param name="input">The command input.</param>
    /// <returns>Whether we have handled the command. Will return <see langword="true"/> if the command is in our list of
    /// watched commands, whether or not the parameters were valid.</returns>
    private static bool HandleCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        InternalLogger.Debug($"Attempting to handle console command: {input}");

        input = input.Trim();
        string[] components = input.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        string trigger = components[0].ToLowerInvariant();

        if (!ConsoleCommands.TryGetValue(trigger, out ConsoleCommand command))
        {
            InternalLogger.Debug($"No command listener registered for [{trigger}].");
            return false;
        }

        IEnumerable<string> parameters = components.Skip(1);

        // If the parameters couldn't be parsed by the command, print a user and developer-friendly error message both
        // on-screen and in the log.
        if (!command.TryParseParameters(parameters, out object[] parsedParameters))
        {
            if (parsedParameters != null)
            {
                // Find the first invalid parameter
                string invalidParameter = null;
                string parameterTypeName = null;
                for (int i = 0; i < parsedParameters.Length; i++)
                {
                    if (parsedParameters[i] == null)
                    {
                        invalidParameter = parameters.ElementAt(i);
                        parameterTypeName = command.ParameterTypes[i].Name;
                        break;
                    }
                }

                // Print a message about why it is invalid
                string error = $"{GetColoredString(invalidParameter, ParameterInputColor)} is not a valid " +
                               $"{GetColoredString(parameterTypeName, ParameterTypeColor)}!";

                LogAndAnnounce(error, LogLevel.Error);
            }

            // Print a message about what parameters the command expects
            string parameterInfoString = $"{GetColoredString(command.Trigger, CommandColor)} " +
                                         "expects the following parameters\n" +
                                         command.Parameters.Select(param => GetColoredString(param)).Join(delimiter: "\n");

            LogAndAnnounce(parameterInfoString, LogLevel.Error);

            // Print a message detailing all received parameters.
            if (parameters.Any())
            {
                InternalLogger.Announce($"Received parameters: {parameters.Join()}", LogLevel.Error, true);
            }

            return true; // We've handled the command insofar as we've handled and reported the user error to them.
        }

        InternalLogger.Debug($"Handing command [{trigger}] to [{command.ModName}]...");

        string result = command.Invoke(parsedParameters); // Invoke the command with the parameters parsed from user input.

        if (!string.IsNullOrEmpty(result)) // If the command has a return, print it.
        {
            LogAndAnnounce($"{GetColoredString($"[{command.ModName}]", ModOriginColor)} {result}", LogLevel.Info);
        }

        InternalLogger.Debug($"Command [{trigger}] handled successfully by [{command.ModName}].");

        return true;
    }

    /// <summary>
    /// Logs the message after stripping XML tags (colors), but announces to the user with XML tags intact.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="level">Log level.</param>
    private static void LogAndAnnounce(string message, LogLevel level)
    {
        InternalLogger.Announce(message);
        InternalLogger.Log(message.StripXML(), level);
    }

    private static string GetColoredString(ConsoleCommand command)
    {
        return GetColoredString(command.Trigger, CommandColor);
    }

    private static string GetColoredString(Parameter parameter)
    {
        return $"{parameter.Name}: {GetColoredString(parameter.ParameterType.Name, ParameterTypeColor)}" +
               (parameter.IsOptional ? $" {GetColoredString("(optional)", ParameterOptionalColor)}" : string.Empty);
    }

    private static string GetColoredString(string str, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{str}</color>";
    }

    private static Regex xmlRegex = new("<.*?>", RegexOptions.Compiled);
    public static string StripXML(this string source)
    {
        return xmlRegex.Replace(source, string.Empty);
    }
}