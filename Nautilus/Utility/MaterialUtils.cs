using Nautilus.Utility.MaterialModifiers;
using System.Collections;
using UnityEngine;
using UWE;

namespace Nautilus.Utility;

/// <summary>
/// Utilities related to Materials and Shaders.
/// </summary>
public static partial class MaterialUtils
{
    private static readonly int _emissionMap = Shader.PropertyToID("_EmissionMap");
    private static readonly int _specInt = Shader.PropertyToID("_SpecInt");
    private static readonly int _shininess = Shader.PropertyToID("_Shininess");
    private static readonly int _fresnel = Shader.PropertyToID("_Fresnel");
    private static readonly int _bumpMap = Shader.PropertyToID("_BumpMap");

    internal static void Patch()
    {
        CoroutineHost.StartCoroutine(LoadReferences());
    }

    private static IEnumerator LoadReferences()
    {
        yield return PatchInternal();

        IsReady = true;
        
        yield break;
    }

    /// <summary>
    /// Only returns true once all relevant materials/shaders have been loaded by the MaterialUtils class and are safe to be used.
    /// </summary>
    public static bool IsReady { get; private set; }

    /// <summary>
    /// Gets the basic glass material
    /// </summary>
    public static Material GlassMaterial { get; private set; }

    /// <summary>
    /// Gets the material for the outside of glass, such as for base windows
    /// </summary>
    public static Material ExteriorGlassMaterial { get; private set; }

    /// <summary>
    /// Gets the material for the inside of glass, such as the inside of the Cyclops windshield
    /// </summary>
    public static Material InteriorGlassMaterial { get; private set; }

    /// <summary>
    /// Gets a shiny glass material
    /// </summary>
    public static Material ShinyGlassMaterial { get; private set; }

    /// <summary>
    /// Contains references to various Shaders.
    /// </summary>
    public static class Shaders
    {
        private static Shader _marmosetUber;

        /// <summary>
        /// The <see cref="Shader"/> that is used for most materials in the game.
        /// </summary>
        public static Shader MarmosetUBER
        {
            get
            {
                // Shader.Find("MarmosetUBER") returns the wrong shader, so we need to get it in this way:
                if (_marmosetUber == null)
                {
                    _marmosetUber = AddressablesUtility.LoadAsync<Shader>("Assets/Marmoset/Shader/Uber/MarmosetUber.shader").WaitForCompletion();
                }
                return _marmosetUber;
            }
        }

        private static Shader _particlesUber;

        /// <summary>
        /// The <see cref="Shader"/> that is used for most particle systems.
        /// </summary>
        public static Shader ParticlesUBER
        {
            get
            {
                if (_particlesUber == null)
                {
                    _particlesUber = Shader.Find("UWE/Particles/UBER");
                }
                return _particlesUber;
            }
        }

        private static Shader _ionCube;

        /// <summary>
        /// The <see cref="Shader"/> that is used for Ion Cubes.
        /// </summary>
        public static Shader IonCube
        {
            get
            {
                if (_ionCube == null)
                {
                    _ionCube = Shader.Find("UWE/Marmoset/IonCrystal");
                }
                return _ionCube;
            }
        }
    }

    /// <summary>
    /// Enum for some generic types of Materials.
    /// </summary>
    public enum MaterialType
    {
        /// <summary>
        /// Solid material with no transparency.
        /// </summary>
        Opaque,
        /// <summary>
        /// See-through material.
        /// </summary>
        Transparent,
        /// <summary>
        /// Transparent pixels on the texture are not rendered. Useful for decals.
        /// </summary>
        Cutout
    }

    /// <summary>
    /// Render queue used by opaque objects.
    /// </summary>
    public const int kOpaqueRenderQueue = 2000;
    /// <summary>
    /// Render queue used by transparent objects.
    /// </summary>
    public const int kTransparencyRenderQueue = 3101;

    /// <summary>
    /// <para>Applies the necessary settings for Subnautica's standard shader (<see cref="Shaders.MarmosetUBER"/>) to the passed <see cref="GameObject"/>.</para>
    /// <para><b>The specific changes to each material are influenced by certain (case-insensitive) keywords in their asset names:</b>
    /// <br/>"TRANSPARENT": Enables transparency.
    /// <br/>"CUTOUT": Enables alpha clipping.</para>
    /// </summary>
    /// <param name="gameObject">The <see cref="GameObject"/> to apply the shaders to (includes all children).</param>
    /// <param name="shininess">'_Shininess' value of the shader. Recommended range of 1.0f-8.0f.</param>
    /// <param name="specularIntensity">'_SpecularInt' value of the shader. Values around 1f are recommended.</param>
    /// <param name="glowStrength">'_GlowStrength' and '_GlowStrengthNight' value of the shader. Should not be absurdly high.</param>
    /// <param name="modifiers">Optional array of classes that inherit from the <see cref="MaterialModifier"/> class. This allows for extensive customization of the method. Called in ascending order on every material.</param>
    public static void ApplySNShaders(GameObject gameObject, float shininess = 4f, float specularIntensity = 1f, float glowStrength = 1f, params MaterialModifier[] modifiers)
    {
        var renderers = gameObject.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            for (var j = 0; j < renderers[i].materials.Length; j++)
            {
                var material = renderers[i].materials[j];

                var matNameLower = material.name.ToLower();
                bool transparent = matNameLower.Contains("transparent");
                bool alphaClip = matNameLower.Contains("cutout");

                var materialType = MaterialType.Opaque;
                if (transparent)
                    materialType = MaterialType.Transparent;
                else if (alphaClip)
                    materialType = MaterialType.Cutout;

                // check each material modifier first. determine if any want to block the shader conversion
                bool blockShaderConversion = false;
                if (modifiers != null)
                {
                    foreach (var modifier in modifiers)
                    {
                        if (modifier.BlockShaderConversion(material, renderers[i], materialType))
                        {
                            blockShaderConversion = true;
                        }
                    }
                }

                // apply the shader conversions, if possible
                if (!blockShaderConversion)
                {
                    ApplyUBERShader(material, shininess, specularIntensity, glowStrength, materialType);
                }

                // apply all modifiers
                if (modifiers != null)
                {
                    foreach (var modifier in modifiers)
                    {
                        modifier.EditMaterial(material, renderers[i], j, materialType);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Applies the <see cref="Shaders.MarmosetUBER"/> Shader to the given material.
    /// </summary>
    /// <param name="material">The material to apply changes to.</param>
    /// <param name="shininess">'_Shininess' value of the shader. Recommended range of 1.0f-8.0f.</param>
    /// <param name="specularIntensity">'_SpecularInt' value of the shader. Values around 1f are recommended.</param>
    /// <param name="glowStrength">'_GlowStrength' and '_GlowStrengthNight' value of the shader. Should not be absurdly high.</param>
    /// <param name="materialType">Controls various settings including alpha clipping and transparency.</param>
    public static void ApplyUBERShader(Material material, float shininess, float specularIntensity, float glowStrength, MaterialType materialType)
    {
        // Grab existing references to textures & colors using property names from the standard shader
        var specularTexture = material.HasProperty(ShaderPropertyID._SpecGlossMap) ? material.GetTexture(ShaderPropertyID._SpecGlossMap) : null;
        var emissionTexture = material.HasProperty(_emissionMap) ? material.GetTexture(_emissionMap) : null;
        var emissionColor = material.HasProperty(ShaderPropertyID._EmissionColor) ? material.GetColor(ShaderPropertyID._EmissionColor) : Color.black;

        // Change the shader to MarmosetUBER
        material.shader = Shaders.MarmosetUBER;

        // Disable keywords that were added by Unity
        material.DisableKeyword("_SPECGLOSSMAP");
        material.DisableKeyword("_NORMALMAP");

        // Apply specular settings if there is a specular texture (otherwise it will appear bright white)
        if (specularTexture != null)
        {
            material.SetTexture(ShaderPropertyID._SpecTex, specularTexture);
            material.SetFloat(_specInt, specularIntensity);
            material.SetFloat(_shininess, shininess);
            material.EnableKeyword("_ZWRITE_ON");
            material.EnableKeyword("MARMO_SPECMAP");
            material.SetColor(ShaderPropertyID._SpecColor, new Color(1f, 1f, 1f, 1f));
            material.SetFloat(_fresnel, 0.24f);
        }

        // Apply emission if it was enabled in the standard shader
        if (material.IsKeywordEnabled("_EMISSION"))
        {
            material.EnableKeyword("MARMO_EMISSION");
            material.SetFloat(ShaderPropertyID._EnableGlow, 1f);
            material.SetTexture(ShaderPropertyID._Illum, emissionTexture);
            material.SetColor(ShaderPropertyID._GlowColor, emissionColor);
            material.SetFloat(ShaderPropertyID._GlowStrength, glowStrength);
            material.SetFloat(ShaderPropertyID._GlowStrengthNight, glowStrength);
        }

        // Apply normal map if it was applied in the standard shader
        if (material.GetTexture(_bumpMap))
        {
            material.EnableKeyword("MARMO_NORMALMAP");
        }
        
        material.enableInstancing = true;

        switch (materialType)
        {
            case MaterialType.Transparent:
                SetMaterialCutout(material, false);
                SetMaterialTransparent(material, true);
                break;
            case MaterialType.Cutout:
                SetMaterialTransparent(material, false);
                SetMaterialCutout(material, true);
                break;
            default:
                SetMaterialTransparent(material, false);
                SetMaterialCutout(material, false);
                break;
        }
    }

    /// <summary>
    /// Applies base-game materials to custom <see cref="GameObject"/>s by matching the name of every material in every
    /// renderer on your custom object (and any of its children) against those on the base-game object's renderers,
    /// and any of its children. To use this class, go in-game, and lookup the name of the material you wish to apply
    /// to your custom object using runtime editor. In Unity, rename the materials on your object(s) to match the name
    /// of the material(s) in-game. Then, pass both your custom object, and the base-game object that the material(s)
    /// you want to apply are on, to this method, and the game's material(s) will replace the ones you made. Passing
    /// in parent-objects to those with the materials on them will also work.
    /// </summary>
    /// <param name="customPrefab">The custom <see cref="GameObject"/> you want to apply base-game materials to.</param>
    /// <param name="baseGamePrefab">The base-game prefab which contains the material(s) you want to apply.</param>
    public static void ApplyGameMaterials(GameObject customPrefab, GameObject baseGamePrefab)
    {
        if (customPrefab is null || baseGamePrefab is null)
        {
            InternalLogger.Error("Couldn't apply game materials because a null object was passed into the method.");
            return;
        }
        
        var customRenderers = customPrefab.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < customRenderers.Length; i++)
        {
            var newMaterialList = customRenderers[i].materials;

            for (int j = 0; j < newMaterialList.Length; j++)
            {
                bool matFound = false;

                foreach (var baseGameRenderer in baseGamePrefab.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var baseGameMat in baseGameRenderer.materials)
                    {
                        string baseMatName = baseGameMat.name.ToLower();
                        string plainBaseMatName = "";
                        
                        //Removes the (instance) portion of the mat name, if it's present.
                        if (baseMatName.Contains("(instance)"))
                            plainBaseMatName = baseMatName.Substring(0, baseMatName.LastIndexOf('(') - 1);

                        string customMatName = newMaterialList[j].name.ToLower();
                        if (customMatName.Equals(baseMatName) || customMatName.Equals(plainBaseMatName))
                        {
                            newMaterialList[j] = baseGameMat;
                            
                            matFound = true;
                            break;
                        }
                    }

                    if (matFound)
                        break;
                }
            }
            
            customRenderers[i].materials = newMaterialList;
        }
    }

    /// <summary>
    /// Toggle the transparency on a material.
    /// </summary>
    /// <param name="material"></param>
    /// <param name="transparent"></param>
    /// <returns></returns>
    public static void SetMaterialTransparent(Material material, bool transparent)
    {
        if (transparent)
        {
            material.EnableKeyword("WBOIT");
            material.SetInt(ShaderPropertyID._ZWrite, 0);
            material.SetInt(ShaderPropertyID._Cutoff, 0);
            material.SetFloat(ShaderPropertyID._SrcBlend, 1f);
            material.SetFloat(ShaderPropertyID._DstBlend, 1f);
            material.SetFloat(ShaderPropertyID._SrcBlend2, 0f);
            material.SetFloat(ShaderPropertyID._DstBlend2, 10f);
            material.SetFloat(ShaderPropertyID._AddSrcBlend, 1f);
            material.SetFloat(ShaderPropertyID._AddDstBlend, 1f);
            material.SetFloat(ShaderPropertyID._AddSrcBlend2, 0f);
            material.SetFloat(ShaderPropertyID._AddDstBlend2, 10f);
        }
        else
        {
            material.SetInt(ShaderPropertyID._ZWrite, 1);
            material.SetFloat(ShaderPropertyID._SrcBlend, 1f);
            material.SetFloat(ShaderPropertyID._DstBlend, 0f);
            material.SetFloat(ShaderPropertyID._SrcBlend2, 1f);
            material.SetFloat(ShaderPropertyID._DstBlend2, 0f);
            material.SetFloat(ShaderPropertyID._AddSrcBlend, 1f);
            material.SetFloat(ShaderPropertyID._AddDstBlend, 0f);
            material.SetFloat(ShaderPropertyID._AddSrcBlend2, 1f);
            material.SetFloat(ShaderPropertyID._AddDstBlend2, 0f);
            material.DisableKeyword("WBOIT");
        }
        material.renderQueue = transparent ? kTransparencyRenderQueue : kOpaqueRenderQueue;
    }

    /// <summary>
    /// Toggle alpha clipping on a material. Incompatbile with transparency.
    /// </summary>
    /// <param name="material"></param>
    /// <param name="cutout"></param>
    /// <returns></returns>
    public static void SetMaterialCutout(Material material, bool cutout)
    {
        if (cutout)
        {
            material.EnableKeyword("MARMO_ALPHA_CLIP");
        }
        else
        {
            material.DisableKeyword("MARMO_ALPHA_CLIP");
        }
    }
}
