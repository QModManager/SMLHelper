using System;
using System.Linq;
using FMOD;
using Nautilus.Extensions;
using Nautilus.FMod.Interfaces;
using Nautilus.Handlers;
using Nautilus.Utility;
using UnityEngine;

namespace Nautilus.FMod;

/// <summary>
/// Allows for easy creation and registration of sounds from an Asset Bundle.
/// </summary>
public class FModSoundBuilder : IFModSoundBuilder
{
    // - Persistent data -

    private readonly AssetBundle _bundle;

    // - Builder class data -

    private string _id;
    private string _bus;
    private MODE? _mode;
    private (float, float)? _minAndMaxDistances; // Item1: min, Item2: max
    private float? _fadeDuration;
    private string _clipName;
    private string[] _clipNamesForMultipleSounds;
    private bool _randomizeSoundOrder;

    public FModSoundBuilder(AssetBundle assetBundle)
    {
        _bundle = assetBundle;
    }

    public IFModSoundBuilder CreateNewEvent(string id, string bus)
    {
        _id = id;
        _bus = bus;
        return this;
    }

    IFModSoundBuilder IFModSoundBuilder.SetMode(MODE mode)
    {
        _mode = mode;
        return this;
    }

    IFModSoundBuilder IFModSoundBuilder.SetMode3D(float minDistance, float maxDistance)
    {
        _minAndMaxDistances = (minDistance, maxDistance);
        return ((IFModSoundBuilder)this).SetMode(AudioUtils.StandardSoundModes_3D);
    }

    IFModSoundBuilder IFModSoundBuilder.SetMode2D()
    {
        return ((IFModSoundBuilder)this).SetMode(AudioUtils.StandardSoundModes_2D);
    }

    IFModSoundBuilder IFModSoundBuilder.SetModeMusic()
    {
        return ((IFModSoundBuilder)this).SetMode(AudioUtils.StandardSoundModes_Stream);
    }

    IFModSoundBuilder IFModSoundBuilder.SetFadeDuration(float fadeDuration)
    {
        _fadeDuration = fadeDuration;
        return this;
    }

    IFModSoundBuilder IFModSoundBuilder.SetSound(string clipName)
    {
        _clipNamesForMultipleSounds = null;
        _clipName = clipName;
        return this;
    }

    IFModSoundBuilder IFModSoundBuilder.SetSounds(bool randomizeOrder, params string[] clipNames)
    {
        _clipName = null;
        _clipNamesForMultipleSounds = clipNames;
        _randomizeSoundOrder = randomizeOrder;
        return this;
    }

    void IFModSoundBuilder.Register()
    {
        // Throw exceptions and print warnings for common mistakes
        if (string.IsNullOrEmpty(_id))
            throw new SoundBuilderException("Cannot register a sound with no ID!");

        if (string.IsNullOrEmpty(_bus))
            throw new SoundBuilderException("Cannot register a sound with no bus!");

        if (_mode is null or MODE.DEFAULT)
            throw new SoundBuilderException("Cannot register a sound with no mode assigned!");

        if (_mode.Value.HasFlag(MODE._2D) && _minAndMaxDistances.HasValue)
            InternalLogger.Warn(
                $"Sound '{_id}' is 2D but has a max distance assigned! This may have unexpected results.");

        // Registration
        // For single clips
        if (_clipName != null)
        {
            var sound = AudioUtils.CreateSound(_bundle.LoadAsset<AudioClip>(_clipName), _mode.Value);
            AssignSoundData(sound);
            CustomSoundHandler.RegisterCustomSound(_id, sound, _bus);
        }
        // For multiple clips
        else if (_clipNamesForMultipleSounds != null)
        {
            var sounds = AudioUtils.CreateSounds(_clipNamesForMultipleSounds, _mode.Value).ToArray();
            foreach (var sound in sounds)
            {
                AssignSoundData(sound);
            }

            var multiSoundsEvent = new FModMultiSounds(sounds, _bus, _randomizeSoundOrder);
            CustomSoundHandler.RegisterCustomSound(_id, multiSoundsEvent);
        }
        else
        {
            throw new SoundBuilderException("Cannot register a sound with no sound clip names assigned!");
        }

        // Reset the builder's data
        Reset();
    }

    private void AssignSoundData(Sound sound)
    {
        if (_minAndMaxDistances.HasValue)
        {
            sound.set3DMinMaxDistance(_minAndMaxDistances.Value.Item1, _minAndMaxDistances.Value.Item2);
        }

        if (_fadeDuration is > 0)
        {
            sound.AddFadeOut(_fadeDuration.Value);
        }
    }

    private void Reset()
    {
        _id = null;
        _bus = null;
        _mode = null;
        _minAndMaxDistances = null;
        _fadeDuration = null;
        _randomizeSoundOrder = false;
    }

    private class SoundBuilderException : Exception
    {
        public SoundBuilderException(string message) : base(message)
        {
        }
    }
}