﻿namespace SMLHelper.V2.Options
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// Contains all the information about a slider changed event.
    /// </summary>
    public class SliderChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The ID of the <see cref="ModSliderOption"/> that was changed.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The new value for the <see cref="ModSliderOption"/>.
        /// </summary>
        public float Value { get; }

        /// <summary>
        /// The new value for the <see cref="ModSliderOption"/> parsed as an <see cref="int"/>
        /// </summary>
        public int IntegerValue { get; }

        /// <summary>
        /// Constructs a new <see cref="SliderChangedEventArgs"/>.
        /// </summary>
        /// <param name="id">The ID of the <see cref="ModSliderOption"/> that was changed.</param>
        /// <param name="value">The new value for the <see cref="ModSliderOption"/>.</param>
        public SliderChangedEventArgs(string id, float value)
        {
            this.Id = id;
            this.Value = value;
            this.IntegerValue = Mathf.RoundToInt(value);
        }
    }

    public abstract partial class ModOptions
    {
        /// <summary>
        /// The event that is called whenever a slider is changed. Subscribe to this in the constructor.
        /// </summary>
        protected event EventHandler<SliderChangedEventArgs> SliderChanged;

        /// <summary>
        /// Notifies a slider change to all subsribed event handlers.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        internal void OnSliderChange(string id, float value)
        {
            SliderChanged(this, new SliderChangedEventArgs(id, value));
        }

        /// <summary>
        /// Adds a new <see cref="ModSliderOption"/> to this instance.
        /// </summary>
        /// <param name="id">The internal ID for the slider option.</param>
        /// <param name="label">The display text to use in the in-game menu.</param>
        /// <param name="minValue">The minimum value for the range.</param>
        /// <param name="maxValue">The maximum value for the range.</param>
        /// <param name="value">The starting value.</param>
        protected void AddSliderOption(string id, string label, float minValue, float maxValue, float value)
        {
            AddOption(new ModSliderOption(id, label, minValue, maxValue, value));
        }
    }

    /// <summary>
    /// A mod option class for handling an option that can have any floating point value between a minimum and maximum.
    /// </summary>
    public class ModSliderOption : ModOption
    {
        /// <summary>
        /// The minimum value of the <see cref="ModSliderOption"/>.
        /// </summary>
        public float MinValue { get; }

        /// <summary>
        /// The maximum value of the <see cref="ModSliderOption"/>.
        /// </summary>
        public float MaxValue { get; }

        /// <summary>
        /// The current value of the <see cref="ModSliderOption"/>.
        /// </summary>
        public float Value { get; }

        internal override void AddToPanel(uGUI_TabbedControlsPanel panel, int tabIndex)
        {
            panel.AddSliderOption(tabIndex, Label, Value, MinValue, MaxValue, Value,
                new UnityAction<float>((float value) => parentOptions.OnSliderChange(Id, value)));

            // AddSliderOption for some reason doesn't return created GameObject, so we need this little hack
            Transform options = panel.tabs[tabIndex].container.transform;
            OptionGameObject = options.GetChild(options.childCount - 1).gameObject; // last added game object
        }

        /// <summary>
        /// Instantiates a new <see cref="ModSliderOption"/> for handling an option that can have any floating point value between a minimum and maximum.
        /// </summary>
        /// <param name="id">The internal ID of this option.</param>
        /// <param name="label">The display text to show on the in-game menus.</param>
        /// <param name="minValue">The minimum value for the range.</param>
        /// <param name="maxValue">The maximum value for the range.</param>
        /// <param name="value">The starting value.</param>
        internal ModSliderOption(string id, string label, float minValue, float maxValue, float value) : base(label, id)
        {
            this.MinValue = minValue;
            this.MaxValue = maxValue;
            this.Value = value;
        }
    }
}
