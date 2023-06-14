﻿using System;
using Nautilus.Json;
using UnityEngine;

namespace Nautilus.Options.Attributes;

/// <summary>
/// Attribute used to signify the decorated <see cref="KeyCode"/> should be represented in the mod's
/// option menu as a <see cref="ModKeybindOption"/>.
/// </summary>
/// <example>
/// <code>
/// using Nautilus.Json;
/// using Nautilus.Options;
/// using UnityEngine;
/// 
/// [Menu("My Options Menu")]
/// public class Config : ConfigFile
/// {
///     [Keybind("My Keybind")]
///     public KeyCode MyKeybind;
/// }
/// </code>
/// </example>
/// <seealso cref="MenuAttribute"/>
/// <seealso cref="ConfigFile"/>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class KeybindAttribute : ModOptionAttribute
{
    /// <summary>
    /// Signifies the decorated <see cref="KeyCode"/> should be represented in the mod's option menu
    /// as a <see cref="ModKeybindOption"/>.
    /// </summary>
    /// <param name="label">The label for the keybind. If none is set, the name of the member will be used.</param>
    public KeybindAttribute(string label = null) : base(label) { }

    /// <summary>
    /// Signifies the decorated <see cref="KeyCode"/> should be represented in the mod's option menu
    /// as a <see cref="ModKeybindOption"/>.
    /// </summary>
    public KeybindAttribute() { }
}