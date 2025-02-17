using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Nautilus.Json;
using Nautilus.Utility;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

namespace Nautilus.Options.Attributes;

internal class ConfigFileMetadata<T> where T : ConfigFile, new()
{
    public T Config { get; } = new T();

    public bool Registered { get; set; } = false;

    public string ModName { get; } = Assembly.GetAssembly(typeof(T)).GetName().Name;

    /// <summary>
    /// The <see cref="MenuAttribute"/> relating to this <see cref="ModOptions"/> menu.
    /// </summary>
    public MenuAttribute MenuAttribute { get; private set; }

    /// <summary>
    /// A dictionary of <see cref="ModOptionAttributeMetadata{T}"/>, indexed by <see cref="OptionItem.Id"/>.
    /// </summary>
    public Dictionary<string, ModOptionAttributeMetadata<T>> ModOptionAttributesMetadata { get; private set; }

    /// <summary>
    /// Process metadata for members of <typeparamref name="T"/>.
    /// </summary>
    public void ProcessMetadata()
    {
        Stopwatch stopwatch = new();

        if (InternalLogger.EnableDebugging)
        {
            stopwatch.Start();
        }

        MenuAttribute = typeof(T).GetCustomAttribute<MenuAttribute>(true) ?? new MenuAttribute(ModName);
        ModOptionAttributesMetadata = new Dictionary<string, ModOptionAttributeMetadata<T>>();

        processMetadata();

        if (InternalLogger.EnableDebugging)
        {
            stopwatch.Stop();
            InternalLogger.Debug($"[{ModName}] [{typeof(T).Name}] OptionsMenuBuilder metadata parsed in {stopwatch.ElapsedMilliseconds}ms.");
        }
    }

    private void processMetadata()
    {
        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;
        
        foreach (MemberInfo member in typeof(T).GetMembers(bindingFlags)
                     .Where(memberIsDeclaredInConfigFileSubclass)
                     .Where(memberIsNotIgnored))
        {
            switch (member)
            {
                case FieldInfo field:
                    if (!field.IsStatic)
                        processFieldOrProperty(field, MemberType.Field, field.FieldType);
                    break;
                case PropertyInfo property:
                    if (!property.GetGetMethod(true).IsStatic)
                        processFieldOrProperty(property, MemberType.Property, property.PropertyType);
                    break;
                case MethodInfo method:
                    processMethod(method);
                    break;
            }
        }

        InternalLogger.Debug($"[{ModName}] [{typeof(T).Name}] Found {ModOptionAttributesMetadata.Count()} options to add to the menu.");
    }

    /// <summary>
    /// Checks whether a given <see cref="MemberInfo"/> is declared in any subclass of <see cref="ConfigFile"/>.
    /// </summary>
    /// <param name="memberInfo">The <see cref="MemberInfo"/> to check.</param>
    /// <returns>Whether the given <see cref="MemberInfo"/> is declared in any subclass of <see cref="ConfigFile"/>.</returns>
    private static bool memberIsDeclaredInConfigFileSubclass(MemberInfo memberInfo)
    {
        return memberInfo.DeclaringType.IsSubclassOf(typeof(ConfigFile));
    }

    /// <summary>
    /// Checks whether a given <see cref="MemberInfo"/> should be ignored when generating the options menu, based on whether
    /// the member has a declared <see cref="IgnoreMemberAttribute"/>, or the <see cref="MenuAttribute"/>'s
    /// <see cref="MenuAttribute.MemberProcessing"/> property.
    /// </summary>
    /// <param name="memberInfo">The <see cref="MemberInfo"/> to check.</param>
    /// <returns>Whether the given <see cref="MemberInfo"/> member should be ignored when generating the options menu.</returns>
    private bool memberIsNotIgnored(MemberInfo memberInfo)
    {
        if (Attribute.IsDefined(memberInfo, typeof(IgnoreMemberAttribute)))
        {
            return false;
        }

        switch (MenuAttribute.MemberProcessing)
        {
            case MenuAttribute.Members.Implicit:
                if (memberInfo is MethodInfo)
                {
                    if (Attribute.IsDefined(memberInfo, typeof(ButtonAttribute), true))
                    {
                        return true;
                    }

                    IEnumerable<MemberInfoMetadata<T>> eventMetadatas
                        = ModOptionAttributesMetadata.Values.SelectMany(modOptionsMetadata =>
                        {
                            IEnumerable<MemberInfoMetadata<T>> result = new List<MemberInfoMetadata<T>>();

                            if (modOptionsMetadata.OnChangeMetadata != null)
                            {
                                result.Concat(modOptionsMetadata.OnChangeMetadata);
                            }

                            if (modOptionsMetadata.OnGameObjectCreatedMetadata != null)
                            {
                                result.Concat(modOptionsMetadata.OnGameObjectCreatedMetadata);
                            }

                            return result;
                        });
                    return eventMetadatas.Any(memberInfoMetadata => memberInfoMetadata.Name == memberInfo.Name);
                }
                return true;

            case MenuAttribute.Members.Explicit:
                return Attribute.IsDefined(memberInfo, typeof(ModOptionAttribute), true) ||
                       Attribute.IsDefined(memberInfo, typeof(ModOptionEventAttribute), true);

            default: throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Processes the given field or property and hands off to
    /// <see cref="addModOptionMetadata{TAttribute}(MemberInfo, MemberType, Type)"/> to generate a <see cref="ModOptionAttributeMetadata{T}"/>
    /// and add it to the <see cref="ModOptionAttributesMetadata"/> dictionary.
    /// </summary>
    /// <param name="memberInfo">The <see cref="MemberInfo"/> of the member.</param>
    /// <param name="memberType">The <see cref="MemberType"/> of the member.</param>
    /// <param name="underlyingType">The underlying <see cref="Type"/> of the member.</param>
    private void processFieldOrProperty(MemberInfo memberInfo, MemberType memberType, Type underlyingType)
    {
        if (underlyingType == typeof(bool))
        {
            addModOptionMetadata<ToggleAttribute>(memberInfo, memberType, underlyingType);
        }
        else if (underlyingType == typeof(KeyCode))
        {
            addModOptionMetadata<KeybindAttribute>(memberInfo, memberType, underlyingType);
        }
        else if (underlyingType == typeof(Color) || Attribute.IsDefined(memberInfo, typeof(ColorPickerAttribute)))
        {
            addModOptionMetadata<ColorPickerAttribute>(memberInfo, memberType, underlyingType);
        }
        else if (underlyingType.IsEnum || Attribute.IsDefined(memberInfo, typeof(ChoiceAttribute), true))
        {
            addModOptionMetadata<ChoiceAttribute>(memberInfo, memberType, underlyingType);
        }
        else if (underlyingType == typeof(float) ||
                 underlyingType == typeof(double) ||
                 underlyingType == typeof(int) ||
                 Attribute.IsDefined(memberInfo, typeof(SliderAttribute), true))
        {
            addModOptionMetadata<SliderAttribute>(memberInfo, memberType, underlyingType);
        }
    }

    /// <summary>
    /// Processes the given method and hands off to <see cref="addModOptionMetadata{TAttribute}(MemberInfo, MemberType, Type)"/>
    /// to generate a <see cref="ModOptionAttributeMetadata{T}"/> and add it to the <see cref="ModOptionAttributesMetadata"/> dictionary.
    /// </summary>
    /// <param name="methodInfo">The <see cref="MethodInfo"/> of the method.</param>
    private void processMethod(MethodInfo methodInfo)
    {
        addModOptionMetadata<ButtonAttribute>(methodInfo, MemberType.Method);
    }

    /// <summary>
    /// Generates a <see cref="ModOptionAttributeMetadata{T}"/> based on the member and its attributes, then adds it to the
    /// <see cref="ModOptionAttributesMetadata"/> dictionary.
    /// </summary>
    /// <typeparam name="TAttribute">The type of the <see cref="OptionItem"/> to generate for this member.</typeparam>
    /// <param name="memberInfo">The <see cref="MemberInfo"/> of the member.</param>
    /// <param name="memberType">The <see cref="MemberType"/> of the member.</param>
    /// <param name="underlyingType">The underlying <see cref="Type"/> of the member.</param>
    private void addModOptionMetadata<TAttribute>(MemberInfo memberInfo, MemberType memberType,
        Type underlyingType = null) where TAttribute : ModOptionAttribute, new()
    {
        try
        {
            // Get the ModOptionAttribute
            ModOptionAttribute modOptionAttribute = memberInfo.GetCustomAttribute<ModOptionAttribute>(true)
                                                    ?? new TAttribute();

            // If there is no label specified, just use the member's name.
            if (string.IsNullOrEmpty(modOptionAttribute.Label))
            {
                modOptionAttribute.Label = memberInfo.Name;
            }

            // ModOptionMetadata needed for all ModOptions
            ModOptionAttributeMetadata<T> modOptionMetadata = new()
            {
                ModOptionAttribute = modOptionAttribute,
                MemberInfoMetadata = new MemberInfoMetadata<T>
                {
                    MemberType = memberType,
                    Name = memberInfo.Name,
                    ValueType = underlyingType
                },
                OnGameObjectCreatedMetadata = GetEventMetadata<OnGameObjectCreatedAttribute>(memberInfo)
            };

            if (memberType == MemberType.Method)
            {
                modOptionMetadata.MemberInfoMetadata.ParseMethodParameterTypes(memberInfo as MethodInfo);
            }

            if (typeof(TAttribute) != typeof(ButtonAttribute))
            {
                modOptionMetadata.OnChangeMetadata = GetEventMetadata<OnChangeAttribute>(memberInfo);
            }

            ModOptionAttributesMetadata.Add(modOptionAttribute.Id, modOptionMetadata);
        }
        catch (Exception ex)
        {
            InternalLogger.Error($"[OptionsMenuBuilder] {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the metadata of every <typeparamref name="TAttribute"/> defined for a member.
    /// </summary>
    /// <typeparam name="TAttribute">
    /// The type of <see cref="ModOptionEventAttribute"/> attribute defined on the member to gather metadata for.
    /// </typeparam>
    /// <param name="memberInfo">The member to gather attribute metadata for.</param>
    /// <returns></returns>
    private IEnumerable<MemberInfoMetadata<T>> GetEventMetadata<TAttribute>(MemberInfo memberInfo)
        where TAttribute : ModOptionEventAttribute
    {
        List<MemberInfoMetadata<T>> metadatas = new();
        foreach (TAttribute attribute in memberInfo.GetCustomAttributes<TAttribute>(true))
        {
            MemberInfoMetadata<T> methodMetadata = new()
            {
                MemberType = MemberType.Method,
                Name = attribute.MethodName
            };
            methodMetadata.ParseMethodParameterTypes();
            metadatas.Add(methodMetadata);
        }
        return metadatas;
    }

    public bool TryGetMetadata(string id, out ModOptionAttributeMetadata<T> modOptionAttributeMetadata)
    {
        return ModOptionAttributesMetadata.TryGetValue(id, out modOptionAttributeMetadata);
    }

    #region Events
    public void BindEvents()
    {
        Config.OnStartedLoading += OptionsMenuBuilder_Config_OnStartedLoading;
        Config.OnFinishedLoading += OptionsMenuBuilder_Config_OnFinishedLoading;
    }

    private string jsonConfig;
    public void OptionsMenuBuilder_Config_OnStartedLoading(object sender, ConfigFileEventArgs e)
    {
        jsonConfig = JsonConvert.SerializeObject(e.Instance as T, Config.AlwaysIncludedJsonConverters);
    }

    private void OptionsMenuBuilder_Config_OnFinishedLoading(object sender, ConfigFileEventArgs e)
    {
        T oldConfig = JsonConvert.DeserializeObject<T>(jsonConfig, Config.AlwaysIncludedJsonConverters);
        T currentConfig = e.Instance as T;

        foreach (ModOptionAttributeMetadata<T> modOptionMetadata in ModOptionAttributesMetadata.Values)
        {
            if (modOptionMetadata.MemberInfoMetadata.MemberType != MemberType.Field &&
                modOptionMetadata.MemberInfoMetadata.MemberType != MemberType.Property)
            {
                continue;
            }

            if (!modOptionMetadata.MemberInfoMetadata.GetValue(oldConfig)
                    .Equals(modOptionMetadata.MemberInfoMetadata.GetValue(currentConfig)))
            {
                if (!Registered)
                {
                    // if we haven't marked the options menu as being registered yet, its too soon to fire the events,
                    // so run a coroutine that waits until the first frame where Registered == true
                    // before routing the events
                    UWE.CoroutineHost.StartCoroutine(DeferredInvokeOnChangeEventsRoutine(modOptionMetadata, sender));
                }
                else
                {
                    // otherwise, route the events immediately
                    InvokeOnChangeEvents(modOptionMetadata, sender);
                }
            }
        }
    }

    /// <summary>
    /// Invokes the method for a given <see cref="ButtonAttribute"/> and passes parameters when the button is clicked.
    /// </summary>
    /// <param name="sender">The sender of the original button click event.</param>
    /// <param name="e">The <see cref="ButtonClickedEventArgs"/> for the click event.</param>
    public void HandleButtonClick(object sender, ButtonClickedEventArgs e)
    {
        if (TryGetMetadata(e.Id, out ModOptionAttributeMetadata<T> modOptionMetadata)
            && modOptionMetadata.MemberInfoMetadata.MethodParameterTypes is Type[] parameterTypes)
        {
            object[] parameters = new object[parameterTypes.Length];
            bool senderFound = false;
            bool eventArgsFound = false;

            for (int i = 0; i < parameters.Length; i++)
            {
                Type type = parameterTypes[i];
                if (!senderFound && type == typeof(object))
                {
                    senderFound = true;
                    parameters[i] = sender;
                }
                else if (!eventArgsFound && type == typeof(ButtonClickedEventArgs))
                {
                    eventArgsFound = true;
                    parameters[i] = e;
                }

                if (senderFound && eventArgsFound)
                {
                    break;
                }
            }

            modOptionMetadata.MemberInfoMetadata.InvokeMethod(Config, parameters);
        }
    }

    /// <summary>
    /// Sets the value in the <see cref="Config"/>, optionally saving the <see cref="Config"/> to disk if the
    /// <see cref="MenuAttribute.SaveEvents.ChangeValue"/> flag is set, before passing off to
    /// <see cref="InvokeOnChangeEvents{TSource}(ModOptionAttributeMetadata{T}, object, TSource)"/>
    /// to invoke any methods specified with an <see cref="OnChangeAttribute"/>.
    /// </summary>
    /// <param name="sender">The sender of the original choice changed event.</param>
    /// <param name="e">The <see cref="ChoiceChangedEventArgs{T}"/> for the choice changed event.</param>
    public void HandleChoiceChanged<Any>(object sender, ChoiceChangedEventArgs<Any> e)
    {
        if (TryGetMetadata(e.Id, out ModOptionAttributeMetadata<T> modOptionMetadata))
        {
            // Set the value in the Config
            MemberInfoMetadata<T> memberInfoMetadata = modOptionMetadata.MemberInfoMetadata;
            ChoiceAttribute choiceAttribute = modOptionMetadata.ModOptionAttribute as ChoiceAttribute;

            if (memberInfoMetadata.ValueType.IsEnum && (choiceAttribute.Options == null || !choiceAttribute.Options.Any()))
            {
                // Enum-based choice where the values are parsed from the enum type
                object value = Enum.Parse(memberInfoMetadata.ValueType, e.Value.ToString());
                memberInfoMetadata.SetValue(Config, value);
            }
            else if (memberInfoMetadata.ValueType.IsEnum)
            {
                // Enum-based choice where the values are defined as custom strings
                object value = Enum.Parse(memberInfoMetadata.ValueType, Enum.GetNames(memberInfoMetadata.ValueType)[e.Index]);
                memberInfoMetadata.SetValue(Config, value);
            }
            else if (memberInfoMetadata.ValueType == typeof(string))
            {
                // string-based choice value
                string value = e.Value.ToString();
                memberInfoMetadata.SetValue(Config, value);
            }
            else if (memberInfoMetadata.ValueType == typeof(int))
            {
                // index-based choice value
                int value = e.Index;
                memberInfoMetadata.SetValue(Config, value);
            }

            // Optionally save the Config to disk
            if (MenuAttribute.SaveOn.HasFlag(MenuAttribute.SaveEvents.ChangeValue))
            {
                Config.Save();
            }

            // Invoke any OnChange methods specified
            InvokeOnChangeEvents(modOptionMetadata, sender, e);
        }
    }

    /// <summary>
    /// Sets the value in the <see cref="Config"/>, optionally saving the <see cref="Config"/> to disk if the
    /// <see cref="MenuAttribute.SaveEvents.ChangeValue"/> flag is set, before passing off to
    /// <see cref="InvokeOnChangeEvents{TSource}(ModOptionAttributeMetadata{T}, object, TSource)"/>
    /// to invoke any methods specified with an <see cref="OnChangeAttribute"/>.
    /// </summary>
    /// <param name="sender">The sender of the original keybind changed event.</param>
    /// <param name="e">The <see cref="ColorChangedEventArgs"/> for the keybind changed event.</param>
    public void HandleColorChanged(object sender, ColorChangedEventArgs e)
    {
        if (TryGetMetadata(e.Id, out ModOptionAttributeMetadata<T> modOptionMetadata))
        {
            modOptionMetadata.MemberInfoMetadata.SetValue(Config, e.Value);

            if (MenuAttribute.SaveOn.HasFlag(MenuAttribute.SaveEvents.ChangeValue))
            {
                Config.Save();
            }

            InvokeOnChangeEvents(modOptionMetadata, sender, e);
        }
    }

    /// <summary>
    /// Sets the value in the <see cref="Config"/>, optionally saving the <see cref="Config"/> to disk if the
    /// <see cref="MenuAttribute.SaveEvents.ChangeValue"/> flag is set, before passing off to
    /// <see cref="InvokeOnChangeEvents{TSource}(ModOptionAttributeMetadata{T}, object, TSource)"/>
    /// to invoke any methods specified with an <see cref="OnChangeAttribute"/>.
    /// </summary>
    /// <param name="sender">The sender of the original keybind changed event.</param>
    /// <param name="e">The <see cref="KeybindChangedEventArgs"/> for the keybind changed event.</param>
    public void HandleKeybindChanged(object sender, KeybindChangedEventArgs e)
    {
        if (TryGetMetadata(e.Id, out ModOptionAttributeMetadata<T> modOptionMetadata))
        {
            // Set the value in the Config
            modOptionMetadata.MemberInfoMetadata.SetValue(Config, e.Value);

            // Optionally save the Config to disk
            if (MenuAttribute.SaveOn.HasFlag(MenuAttribute.SaveEvents.ChangeValue))
            {
                Config.Save();
            }

            // Invoke any OnChange methods specified
            InvokeOnChangeEvents(modOptionMetadata, sender, e);
        }
    }

    /// <summary>
    /// Sets the value in the <see cref="Config"/>, optionally saving the <see cref="Config"/> to disk if the
    /// <see cref="MenuAttribute.SaveEvents.ChangeValue"/> flag is set, before passing off to
    /// <see cref="InvokeOnChangeEvents{TSource}(ModOptionAttributeMetadata{T}, object, TSource)"/>
    /// to invoke any methods specified with an <see cref="OnChangeAttribute"/>.
    /// </summary>
    /// <param name="sender">The sender of the original slider changed event.</param>
    /// <param name="e">The <see cref="SliderChangedEventArgs"/> for the slider changed event.</param>
    public void HandleSliderChanged(object sender, SliderChangedEventArgs e)
    {
        if (TryGetMetadata(e.Id, out ModOptionAttributeMetadata<T> modOptionMetadata))
        {
            // Set the value in the Config
            MemberInfoMetadata<T> memberInfoMetadata = modOptionMetadata.MemberInfoMetadata;
            object value = Convert.ChangeType(e.Value, memberInfoMetadata.ValueType);
            memberInfoMetadata.SetValue(Config, value);

            // Optionally save the Config to disk
            if (MenuAttribute.SaveOn.HasFlag(MenuAttribute.SaveEvents.ChangeValue))
            {
                Config.Save();
            }

            // Invoke any OnChange methods specified
            InvokeOnChangeEvents(modOptionMetadata, sender, e);
        }
    }

    /// <summary>
    /// Sets the value in the <see cref="Config"/>, optionally saving the <see cref="Config"/> to disk if the
    /// <see cref="MenuAttribute.SaveEvents.ChangeValue"/> flag is set, before passing off to
    /// <see cref="InvokeOnChangeEvents{TSource}(ModOptionAttributeMetadata{T}, object, TSource)"/>
    /// to invoke any methods specified with an <see cref="OnChangeAttribute"/>.
    /// </summary>
    /// <param name="sender">The sender of the original toggle changed event.</param>
    /// <param name="e">The <see cref="ToggleChangedEventArgs"/> for the toggle changed event.</param>
    public void HandleToggleChanged(object sender, ToggleChangedEventArgs e)
    {
        if (TryGetMetadata(e.Id, out ModOptionAttributeMetadata<T> modOptionMetadata))
        {
            // Set the value in the Config
            modOptionMetadata.MemberInfoMetadata.SetValue(Config, e.Value);

            // Optionally save the Config to disk
            if (MenuAttribute.SaveOn.HasFlag(MenuAttribute.SaveEvents.ChangeValue))
            {
                Config.Save();
            }

            // Invoke any OnChange methods specified
            InvokeOnChangeEvents(modOptionMetadata, sender, e);
        }
    }

    /// <summary>
    /// Generates tooltips as required for each <see cref="OptionItem"/>, before
    /// invoking any relevant method(s) specified with <see cref="OnGameObjectCreatedAttribute"/>(s) and passes
    /// parameters when a <see cref="GameObject"/> is created in the options menu.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void HandleGameObjectCreated(object sender, GameObjectCreatedEventArgs e)
    {
        if (TryGetMetadata(e.Id, out ModOptionAttributeMetadata<T> modOptionMetadata))
        {
            string tooltip = Language.main.TryGet(modOptionMetadata.ModOptionAttribute.TooltipLanguageId, out string languageTooltip) switch
            {
                true => languageTooltip,
                false => modOptionMetadata.ModOptionAttribute.Tooltip
            };

            // Create a tooltip if specified
            if (tooltip is not null)
            {
                e.Value.GetComponentInChildren<TextMeshProUGUI>().gameObject.AddComponent<ModOptionTooltip>().Tooltip = tooltip;
            }

            if (modOptionMetadata.OnGameObjectCreatedMetadata == null)
            {
                return; // Skip attempting to invoke events if there are no OnGameObjectCreatedAttributes set for the member.
            }

            foreach (MemberInfoMetadata<T> onGameObjectCreatedMetadata in modOptionMetadata.OnGameObjectCreatedMetadata)
            {
                InvokeEvent(onGameObjectCreatedMetadata, sender, e);
            }
        }
    }

    private IEnumerator DeferredInvokeOnChangeEventsRoutine(ModOptionAttributeMetadata<T> modOptionMetadata, object sender)
    {
        yield return new WaitUntil(() => Registered);
        InvokeOnChangeEvents(modOptionMetadata, sender);
    }

    /// <summary>
    /// Invokes the relevant method(s) specified with the <see cref="OnChangeAttribute"/>(s)
    /// and passes parameters when a value is changed when loaded from disk.
    /// </summary>
    /// <param name="modOptionMetadata">The metadata for the mod option.</param>
    /// <param name="sender">The sender of the event.</param>
    private void InvokeOnChangeEvents(ModOptionAttributeMetadata<T> modOptionMetadata, object sender)
    {
        string id = modOptionMetadata.ModOptionAttribute.Id;
        MemberInfoMetadata<T> memberInfoMetadata = modOptionMetadata.MemberInfoMetadata;

        switch (modOptionMetadata.ModOptionAttribute)
        {
            case ChoiceAttribute choiceAttribute when memberInfoMetadata.ValueType.IsEnum &&
                                                      (choiceAttribute.Options == null || !choiceAttribute.Options.Any()):
                // Enum-based choice where the values are parsed from the enum type
            {
                string[] options = Enum.GetNames(memberInfoMetadata.ValueType);
                var value = (T)memberInfoMetadata.GetValue(Config);
                ChoiceChangedEventArgs<T> eventArgs = new(id, Array.IndexOf(options, value), value);
                InvokeOnChangeEvents(modOptionMetadata, sender, eventArgs);
            }
                break;
            case ChoiceAttribute _ when memberInfoMetadata.ValueType.IsEnum:
                // Enum-based choice where the values are defined as custom strings
            {
                string value = memberInfoMetadata.GetValue(Config).ToString();
                int index = Math.Max(Array.IndexOf(Enum.GetValues(memberInfoMetadata.ValueType), value), 0);
                ChoiceChangedEventArgs<string> eventArgs = new(id, index, value);
                InvokeOnChangeEvents(modOptionMetadata, sender, eventArgs);
            }
                break;
            case ChoiceAttribute choiceAttribute when memberInfoMetadata.ValueType == typeof(string):
                // string-based choice value
            {
                string[] options = choiceAttribute.Options;
                string value = memberInfoMetadata.GetValue<string>(Config);
                ChoiceChangedEventArgs<string> eventArgs = new(id, Array.IndexOf(options, value), value);
                InvokeOnChangeEvents(modOptionMetadata, sender, eventArgs);
            }
                break;
            case ChoiceAttribute choiceAttribute when memberInfoMetadata.ValueType == typeof(int):
                // index-based choice value
            {
                string[] options = choiceAttribute.Options;
                int index = memberInfoMetadata.GetValue<int>(Config);
                ChoiceChangedEventArgs<string> eventArgs = new(id, index, options[index]);
                InvokeOnChangeEvents(modOptionMetadata, sender, eventArgs);
            }
                break;
            case ColorPickerAttribute _:
            {
                ColorChangedEventArgs eventArgs = new(id, memberInfoMetadata.GetValue<Color>(Config));
                InvokeOnChangeEvents(modOptionMetadata, sender, eventArgs);
            }
                break;

            case KeybindAttribute _:
            {
                KeybindChangedEventArgs eventArgs = new(id, memberInfoMetadata.GetValue<KeyCode>(Config));
                InvokeOnChangeEvents(modOptionMetadata, sender, eventArgs);
            }
                break;

            case SliderAttribute _:
            {
                SliderChangedEventArgs eventArgs = new(id, Convert.ToSingle(memberInfoMetadata.GetValue(Config)));
                InvokeOnChangeEvents(modOptionMetadata, sender, eventArgs);
            }
                break;

            case ToggleAttribute _:
            {
                ToggleChangedEventArgs eventArgs = new(id, memberInfoMetadata.GetValue<bool>(Config));
                InvokeOnChangeEvents(modOptionMetadata, sender, eventArgs);
            }
                break;
        }
    }

    /// <summary>
    /// Invokes the relevant method(s) specified with <see cref="OnChangeAttribute"/>(s)
    /// and passes parameters when a value is changed.
    /// </summary>
    /// <typeparam name="TSource">The type of the event args.</typeparam>
    /// <param name="modOptionMetadata">The metadata for the mod option.</param>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event args from the OnChange event.</param>
    private void InvokeOnChangeEvents<TSource>(ModOptionAttributeMetadata<T> modOptionMetadata, object sender, TSource e)
        where TSource : EventArgs
    {
        if (modOptionMetadata.OnChangeMetadata == null)
        {
            return; // Skip attempting to invoke events if there are no OnChangeAttributes set for the member.
        }

        foreach (MemberInfoMetadata<T> onChangeMetadata in modOptionMetadata.OnChangeMetadata)
        {
            InvokeEvent(onChangeMetadata, sender, e);
        }
    }

    /// <summary>
    /// Invoke the relevant method specified by a <see cref="ModOptionEventAttribute"/>
    /// and passes relevant parameters.
    /// </summary>
    /// <typeparam name="TSource">The type of the event args.</typeparam>
    /// <param name="memberInfoMetadata">The metadata for the method.</param>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event args from the event.</param>
    private void InvokeEvent<TSource>(MemberInfoMetadata<T> memberInfoMetadata, object sender, TSource e)
        where TSource : EventArgs
    {
        if (!memberInfoMetadata.MethodValid)
        {
            // Method not found, log error and skip.
            InternalLogger.Error($"[OptionsMenuBuilder] Could not find the specified method: {typeof(T)}.{memberInfoMetadata.Name}");
            return;
        }

        if (memberInfoMetadata.MethodParameterTypes is Type[] parameterTypes)
        {
            object[] parameters = new object[parameterTypes.Length];
            bool senderFound = false;
            bool eventArgsFound = false;
            bool modOptionEventFound = false;

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (!senderFound && parameterTypes[i] == typeof(object))
                {
                    senderFound = true;
                    parameters[i] = sender;
                }
                else if (!eventArgsFound && parameterTypes[i] == typeof(TSource))
                {
                    eventArgsFound = true;
                    parameters[i] = e;
                }
                else if (!modOptionEventFound && parameterTypes[i] == typeof(EventArgs))
                {
                    modOptionEventFound = true;
                    parameters[i] = e;
                }

                if (senderFound && eventArgsFound && modOptionEventFound)
                {
                    break;
                }
            }

            memberInfoMetadata.InvokeMethod(Config, parameters);
        }
    }
    #endregion
}