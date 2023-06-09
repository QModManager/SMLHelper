﻿using Nautilus.Json;
using System;

namespace Nautilus.Options.Attributes;

/// <summary>
/// Attribute used to signify the decorated <see cref="UnityEngine.Color"/> should be represented in the mod's
/// option menu as a <see cref="ModColorOption"/>.
/// </summary>
/// <example>
/// <code>
/// using Nautilus.Json;
/// using Nautilus.Options;
/// 
/// [Menu("My Options Menu")]
/// public class Config : ConfigFile
/// {
///     [ColorPicker("My Toggle")]
///     public Color MyToggle;
/// }
/// </code>
/// </example>
/// <seealso cref="MenuAttribute"/>
/// <seealso cref="ConfigFile"/>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class ColorPickerAttribute : ModOptionAttribute
{
    /// <summary>
    /// Which type of color picker to use.
    /// </summary>
    public bool Advanced { get; set; } = false;

    /// <summary>
    /// Signifies the decorated <see cref="UnityEngine.Color"/> should be represented in the mod's option menu
    /// as a <see cref="ModColorOption"/>.
    /// </summary>
    /// <param name="label">The label for the toggle.</param>
    public ColorPickerAttribute(string label = null) : base(label) { }

    /// <summary>
    /// Signifies the decorated <see cref="UnityEngine.Color"/> should be represented in the mod's option menu
    /// as a <see cref="ModColorOption"/>.
    /// </summary>
    public ColorPickerAttribute() { }
}