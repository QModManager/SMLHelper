﻿using Nautilus.Utility;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Nautilus.Json.Converters;

/// <summary>
/// A <see cref="JsonConverter"/> for handling <see cref="KeyCode"/>s.
/// </summary>
public class KeyCodeConverter : JsonConverter
{
    /// <summary>
    /// The method for writing the <paramref name="value"/> data to the <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="serializer"></param>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        KeyCode keyCode = (KeyCode) value;
        writer.WriteValue(KeyCodeUtils.KeyCodeToString(keyCode));
    }

    /// <summary>
    /// The method for reading the <see cref="KeyCode"/> data from the <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="objectType"></param>
    /// <param name="existingValue"></param>
    /// <param name="serializer"></param>
    /// <returns></returns>
    public override object ReadJson(JsonReader reader, Type objectType,
        object existingValue, JsonSerializer serializer)
    {
        string s = (string) reader.Value;
        return KeyCodeUtils.StringToKeyCode(s);
    }

    /// <summary>
    /// The method for determining whether the current <paramref name="objectType"/> can be processed by this
    /// <see cref="JsonConverter"/>.
    /// </summary>
    /// <param name="objectType"></param>
    /// <returns></returns>
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(KeyCode);
    }
}