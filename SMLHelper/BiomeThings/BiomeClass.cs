﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = QModManager.Utility.Logger;
using UWE;
using WorldStreaming;
using System.Runtime.CompilerServices;
namespace SMLHelper.V2.BiomeThings
{
    ///    <summary>
    /// A class for adding biomes.
    ///   </summary>
    public abstract class Biome
    {
        /// <summary>
        /// The sky of your biome.
        /// </summary>
        public abstract mset.Sky Sky { get; }
        /// <summary>
        /// The Waterscape Volume Settings of your biome.
        /// </summary>
        public abstract WaterscapeVolume.Settings WaterScapeSettings { get; }
        /// <summary>
        /// SpawnInfos for what you want to initially spawn into your biome without initializing a coordinated spawn class.
        /// </summary>
        public abstract List<SMLHelper.V2.Handlers.SpawnInfo> SpawnInfos { get; }
        
        /// <summary>
        /// The name of the added biome, must be a string.
        /// </summary>
        public abstract string BiomeName { get; }
        /// <summary>
        /// The Rich Presence Message of the biome, must be a string.
        /// </summary>
        public virtual string BiomeRichPresence { get; }
        /// <summary>
        /// The Ambient Light Settings of your biome.
        /// </summary>
        public abstract AmbientLightSettings amblightsettings { get; }
        /// <summary>
        /// The Fog Settings for the biome.
        /// </summary>
        public abstract FogSettings fogsettings { get; }
        /// <summary>
        /// Sunlight Settings for the biome.
        /// </summary>
        public abstract SunlightSettings sunsettings { get; }
        /// <summary>
        /// The batch Ids, of the biome, must be the same as the ones in your .optoctrees files
        /// </summary>
        public abstract List<Int3> batchIds { get; }
        internal Dictionary<Int3, GameObject> batchroots = new Dictionary<Int3, GameObject>();
        /// <summary>
        /// Call this method to Finialize setting values, and add the biome to the game.
        /// </summary>
        public void Patch()
        { 
           
            Variables.biomes.Add(this);
            
            QModManager.Utility.Logger.Log(QModManager.Utility.Logger.Level.Info, $"Patched biome {BiomeName}");
        }
        /// <summary>
        /// The BiomeMap you have edited.
        /// </summary>
        /// <returns>An Array of Color32</returns>
        public abstract Texture2D EditedBiomeMap(out int height, out int width);
        /// <summary>
        /// The gameobject with the collider for registering when biome is entered
        /// </summary>
        /// <returns>A gameobject with collider applied</returns>
        public abstract GameObject GetCollider();
        /// <summary>
        /// The Terrain for each batch of your biome, in format (BatchId,Terrain)
        /// </summary>
        public abstract Dictionary<Int3, GameObject> batchTerrains { get; }
    }

    internal static class Variables
    {
        internal static readonly List<Biome> biomes = new List<Biome>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static List<Int3> GetAllBiomeBatchIds()
        {
            var result = new List<Int3>();
            for (var i = 0; i < biomes.Count; i++)
            {
                var biome = biomes[i];
                for (var e = 0; i < biome.batchIds.Count; e++)
                {
                    result.Add(biome.batchIds[e]);
                }
            }
            return result;
        }
        internal static Texture2D finalbiomemap = SMLHelper.V2.Utility.ImageUtils.LoadTextureFromFile(Path.Combine(SNUtils.unmanagedDataDir,"Build18","biomeMap.png"));
        internal static int finalbiomemapheight => finalbiomemap.height;
        internal static int finalbiomemapwidth => finalbiomemap.width;
        internal static bool ignoreHeader = false;
        internal static List<Color> usedrandomcolors = new List<Color>();
    }
}
