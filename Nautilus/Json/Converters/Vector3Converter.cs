﻿using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Nautilus.Json.Converters;

/// <summary>
/// A Vector3 json converter that simplifies the Vector3 to only x,y,z serialization.
/// </summary>
public class Vector3Converter : JsonConverter
{
    /// <summary>
    /// A method that determines when this converter should process.
    /// </summary>
    /// <param name="objectType">the current object type</param>
    /// <returns></returns>
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector3);
    }

    /// <summary>
    /// A method that tells Newtonsoft how to Serialize the current object.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="serializer"></param>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Vector3 vector3 = (Vector3)value;
        serializer.Serialize(writer, (Vector3Json)vector3);
    }

    /// <summary>
    /// A method that tells Newtonsoft how to Deserialize and read the current object.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="objectType"></param>
    /// <param name="existingValue"></param>
    /// <param name="serializer"></param>
    /// <returns></returns>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return (Vector3)serializer.Deserialize<Vector3Json>(reader);
    }
}

internal record Vector3Json(float X, float Y, float Z)
{
    public static explicit operator Vector3(Vector3Json v) => new(v.X, v.Y, v.Z);
    public static explicit operator Vector3Json(Vector3 v) => new(v.x, v.y, v.z);
}