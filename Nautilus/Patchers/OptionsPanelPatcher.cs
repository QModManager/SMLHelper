using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nautilus.Handlers;
using Nautilus.Options;
using Nautilus.Utility;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nautilus.Patchers;

using static ModKeybindOption;

internal class OptionsPanelPatcher
{
    internal static SortedList<string, ModOptions> modOptions = new();

    private static int _modsTabIndex = -1;

    private static Color _headerColor = new(1f, 0.777f, 0f);

    internal static void Patch(Harmony harmony)
    {
        harmony.PatchAll(typeof(OptionsPanelPatcher));
        harmony.PatchAll(typeof(ScrollPosKeeper));
        harmony.PatchAll(typeof(ModOptionsHeadingsToggle));
    }

    // 'Mods' tab also added in QModManager, so we can't rely on 'modsTab' in AddTabs_Postfix
    [HarmonyPostfix]
    [HarmonyPatch(typeof(uGUI_TabbedControlsPanel), nameof(uGUI_TabbedControlsPanel.AddTab))]
    internal static void AddTab_Postfix(uGUI_TabbedControlsPanel __instance, string label, int __result)
    {
        if(__instance is not uGUI_OptionsPanel)
            return;

        if (label == "Mods")
        {
            _modsTabIndex = __result;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(uGUI_Binding), nameof(uGUI_Binding.RefreshValue))]
    internal static bool RefreshValue_Prefix(uGUI_Binding __instance)
    {
        if (__instance.gameObject.GetComponent<ModBindingTag>() is null)
        {
            return true;
        }

        __instance.currentText.text = (__instance.active || __instance.value == null) ? "" : __instance.value;
        __instance.UpdateState();
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.AddTabs))]
    internal static void AddTabs_Postfix(uGUI_OptionsPanel __instance)
    {
        uGUI_OptionsPanel optionsPanel = __instance;

        // Start the modsTab index at a value of -1
        int modsTab = -1;
        // Loop through all of the tabs
        for (int i = 0; i < optionsPanel.tabsContainer.childCount; i++)
        {
            // Check if they are named "Mods"
            TextMeshProUGUI text = optionsPanel.tabsContainer.GetChild(i).GetComponentInChildren<TextMeshProUGUI>(true);

            if (text != null && text.text == "Mods")
            {
                // Set the tab index to the found one and break
                modsTab = i;
                break;
            }
        }

        // If no tab was found, create one
        if (modsTab == -1)
        {
            modsTab = optionsPanel.AddTab("Mods");
        }

        var optionsToAdd = new List<ModOptions>(modOptions.Values);

        var nautilusOptions = optionsToAdd.FirstOrDefault(options => options.Name == "Nautilus");
        optionsToAdd.Remove(nautilusOptions);

        nautilusOptions.AddOptionsToPanel(optionsPanel, modsTab);

        // adding all other options here
        optionsToAdd.ForEach(options => options.AddOptionsToPanel(optionsPanel, modsTab));
    }

    // Class for collapsing/expanding options in 'Mods' tab
    // Options can be collapsed/expanded by clicking on mod's title or arrow button
    private static class ModOptionsHeadingsToggle
    {
        private enum HeadingState { Collapsed, Expanded };

        private static GameObject _headingPrefab = null;

        private static class StoredHeadingStates
        {
            private static readonly string _configPath = Path.Combine(Path.Combine(BepInEx.Paths.ConfigPath, Assembly.GetExecutingAssembly().GetName().Name), "headings_states.json");

            private class StatesConfig
            {
                [JsonProperty]
                private readonly Dictionary<string, HeadingState> _states = new();

                public HeadingState this[string name]
                {
                    get => _states.TryGetValue(name, out HeadingState state) ? state : HeadingState.Expanded;

                    set
                    {
                        _states[name] = value;
                        File.WriteAllText(_configPath, JsonConvert.SerializeObject(this, Formatting.Indented));
                    }
                }
            }
            private static readonly StatesConfig _statesConfig = CreateConfig();

            private static StatesConfig CreateConfig()
            {
                if (File.Exists(_configPath))
                {
                    return JsonConvert.DeserializeObject<StatesConfig>(File.ReadAllText(_configPath));
                }
                else
                {
                    return new StatesConfig();
                }
            }

            public static HeadingState get(string name)
            {
                return _statesConfig[name];
            }

            public static void store(string name, HeadingState state)
            {
                _statesConfig[name] = state;
            }
        }

        // we add arrow button from Choice ui element to the options headings for collapsing/expanding 
        private static void InitHeadingPrefab(uGUI_TabbedControlsPanel panel)
        {
            if (_headingPrefab)
            {
                return;
            }

            _headingPrefab = Object.Instantiate(panel.headingPrefab);
            _headingPrefab.name = "OptionHeadingToggleable";
            _headingPrefab.AddComponent<HeadingToggle>();

            var option = _headingPrefab.AddComponent<uGUI_OptionSelection>();
            option.selectionBackground = Object.Instantiate(panel.toggleOptionPrefab.GetComponentInChildren<uGUI_OptionSelection>(true).selectionBackground);
            option.selectionBackground.transform.SetParent(_headingPrefab.transform);
            option.hoverSound = AudioUtils.GetFmodAsset("event:/tools/pda/select");

            Transform captionTransform = _headingPrefab.transform.Find("Caption");
            captionTransform.localPosition = new Vector3(45f, 0f, 0f);
            captionTransform.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject button = Object.Instantiate(panel.choiceOptionPrefab.transform.Find("Choice/Background/NextButton").gameObject);
            button.name = "HeadingToggleButton";
            button.AddComponent<ToggleButtonClickHandler>();
            Object.Destroy(button.GetComponent<Button>());

            var textComponent = captionTransform.GetComponent<TextMeshProUGUI>();
            textComponent.fontStyle = FontStyles.Bold;

            RectTransform buttonTransform = button.transform as RectTransform;
            buttonTransform.SetParent(_headingPrefab.transform);
            buttonTransform.SetAsFirstSibling();
            buttonTransform.localEulerAngles = new Vector3(0f, 0f, -90f);
            buttonTransform.localPosition = new Vector3(15f, -13f, 0f);
            buttonTransform.pivot = new Vector2(0.25f, 0.5f);
            buttonTransform.anchorMin = buttonTransform.anchorMax = new Vector2(0f, 0.5f);
        }

        #region components
        // main component for headings toggling
        private class HeadingToggle: Selectable, IPointerClickHandler
        {
            private HeadingState _headingState = HeadingState.Expanded;
            private string _headingName = null;
            private List<GameObject> _childOptions = null;

#if SUBNAUTICA
            protected override void OnEnable()
#elif BELOWZERO
            public override void OnEnable()
#endif
            {
                base.OnEnable();
                transform.Find("Caption").GetComponent<TextMeshProUGUI>().color = _headerColor;
            }

            private void Init()
            {
                if (_childOptions != null)
                {
                    return;
                }

                _headingName = transform.Find("Caption")?.GetComponent<TextMeshProUGUI>()?.text ?? "";

                _childOptions = new List<GameObject>();

                for (int i = transform.GetSiblingIndex() + 1; i < transform.parent.childCount; i++)
                {
                    GameObject option = transform.parent.GetChild(i).gameObject;

                    if (option.GetComponent<HeadingToggle>())
                    {
                        break;
                    }

                    _childOptions.Add(option);
                }
            }

            public void EnsureState() // for setting previously saved state
            {
                Init();

                HeadingState storedState = StoredHeadingStates.get(_headingName);

                if (_headingState != storedState)
                {
                    SetState(storedState);
                    GetComponentInChildren<ToggleButtonClickHandler>()?.SetStateInstant(storedState);
                }
            }

            public void SetState(HeadingState state)
            {
                Init();

                _childOptions.ForEach(option => option.SetActive(state == HeadingState.Expanded));
                _headingState = state;

                StoredHeadingStates.store(_headingName, state);
            }
            
            public void OnPointerClick(PointerEventData _)
            {
                var buttonHandler = GetComponentInChildren<ToggleButtonClickHandler>();
                if (buttonHandler.isRotating)
                    return;

                _headingState = _headingState == HeadingState.Expanded ? HeadingState.Collapsed : HeadingState.Expanded;
                StartCoroutine(buttonHandler.SetState(_headingState));

                SetState(_headingState);
            }
        }

        // change handler for arrow button
        private class ToggleButtonClickHandler: MonoBehaviour
        {
            private const float TimeRotate = 0.1f;

            public bool isRotating = false;

            public void SetStateInstant(HeadingState state)
            {
                transform.localEulerAngles = new Vector3(0, 0, state == HeadingState.Expanded? -90: 0);
            }

            internal IEnumerator SetState(HeadingState state)
            {
                isRotating = true;

                float angle = state == HeadingState.Expanded ? -90 : 90;

                Quaternion startRotation = transform.localRotation;
                Quaternion endRotation = Quaternion.Euler(new Vector3(0f, 0f, angle)) * startRotation;

                float timeStart = Time.realtimeSinceStartup; // Time.deltaTime works only in main menu

                while (timeStart + TimeRotate > Time.realtimeSinceStartup)
                {
                    transform.localRotation = Quaternion.Lerp(startRotation, endRotation, (Time.realtimeSinceStartup - timeStart) / TimeRotate);
                    yield return null;
                }

                transform.localRotation = endRotation;
                isRotating = false;
            }
        }
#endregion

        #region patches for uGUI_TabbedControlsPanel
        [HarmonyPrefix]
        [HarmonyPatch(typeof(uGUI_TabbedControlsPanel), nameof(uGUI_TabbedControlsPanel.AddHeading))]
        private static bool AddHeading_Prefix(uGUI_TabbedControlsPanel __instance, int tabIndex, string label)
        {
            if (tabIndex != _modsTabIndex || __instance is not uGUI_OptionsPanel)
                return true;

            __instance.AddItem(tabIndex, _headingPrefab, label);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_TabbedControlsPanel), nameof(uGUI_TabbedControlsPanel.OnEnable))]
        private static void Awake_Postfix(uGUI_TabbedControlsPanel __instance)
        {
            if(__instance is not uGUI_OptionsPanel)
                return;

            InitHeadingPrefab(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(uGUI_TabbedControlsPanel), nameof(uGUI_TabbedControlsPanel.SetVisibleTab))]
        private static void SetVisibleTab_Prefix(uGUI_TabbedControlsPanel __instance, int tabIndex)
        {
            if (tabIndex != _modsTabIndex || __instance is not uGUI_OptionsPanel)
                return;

            // just in case, for changing vertical spacing between ui elements
            //__instance.tabs[tabIndex].container.GetComponent<VerticalLayoutGroup>().spacing = 15f; // default is 15f

            Transform options = __instance.tabs[tabIndex].container.transform;

            for (int i = 0; i < options.childCount; i++)
            {
                options.GetChild(i).GetComponent<HeadingToggle>()?.EnsureState();
            }
        }
        #endregion
    }


    // Patch class for saving scroll positions for tabs in options menu
    // Restores positions after switching between tabs and after reopening menu
    private static class ScrollPosKeeper
    {
        // key - tab index, value - scroll position
        private static readonly Dictionary<int, float> _devMenuScrollPos = new();
        private static readonly Dictionary<int, float> _optionsScrollPos = new();

        private static void StorePos(uGUI_TabbedControlsPanel panel, int tabIndex)
        {
            Dictionary<int, float> scrollPos = panel is uGUI_DeveloperPanel ? _devMenuScrollPos : _optionsScrollPos;
            if (tabIndex >= 0 && tabIndex < panel.tabs.Count)
            {
                scrollPos[tabIndex] = panel.tabs[tabIndex].pane.GetComponent<ScrollRect>().verticalNormalizedPosition;
            }
        }

        private static void RestorePos(uGUI_TabbedControlsPanel panel, int tabIndex)
        {
            Dictionary<int, float> scrollPos = panel is uGUI_DeveloperPanel ? _devMenuScrollPos : _optionsScrollPos;
            if (tabIndex >= 0 && tabIndex < panel.tabs.Count && scrollPos.TryGetValue(tabIndex, out float pos))
            {
                panel.tabs[tabIndex].pane.GetComponent<ScrollRect>().verticalNormalizedPosition = pos;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(uGUI_TabbedControlsPanel), nameof(uGUI_TabbedControlsPanel.RemoveTabs))]
        private static void RemoveTabs_Prefix(uGUI_TabbedControlsPanel __instance)
        {
            if(__instance is not uGUI_OptionsPanel)
                return;
            StorePos(__instance, __instance.currentTab);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_TabbedControlsPanel), nameof(uGUI_TabbedControlsPanel.HighlightCurrentTab))]
        private static void HighlightCurrentTab_Postfix(uGUI_TabbedControlsPanel __instance)
        {
            if(__instance is not uGUI_OptionsPanel)
                return;
            __instance.StartCoroutine(_restorePos());

            IEnumerator _restorePos()
            {
                yield return null;
                RestorePos(__instance, __instance.currentTab);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(uGUI_TabbedControlsPanel), nameof(uGUI_TabbedControlsPanel.SetVisibleTab))]
        private static void SetVisibleTab_Prefix(uGUI_TabbedControlsPanel __instance, int tabIndex)
        {
            if(__instance is not uGUI_OptionsPanel)
                return;
            if (tabIndex != __instance.currentTab)
            {
                StorePos(__instance, __instance.currentTab);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_TabbedControlsPanel), nameof(uGUI_TabbedControlsPanel.SetVisibleTab))]
        private static void SetVisibleTab_Postfix(uGUI_TabbedControlsPanel __instance, int tabIndex)
        {
            if(__instance is not uGUI_OptionsPanel)
                return;
            RestorePos(__instance, tabIndex);
        }
    }
}