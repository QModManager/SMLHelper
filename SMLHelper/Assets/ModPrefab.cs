﻿namespace SMLHelper.Assets
{
    using System.Reflection;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// The abstract class to inherit when you want to add new PreFabs into the game.
    /// </summary>
    public abstract class ModPrefab
    {

        internal readonly Assembly Mod;

        /// <summary>
        /// The class identifier used for the <see cref="PrefabIdentifier" /> component whenever applicable.
        /// </summary>
        public string ClassID { get; protected set; }

        /// <summary>
        /// Name of the prefab file.
        /// </summary>
        public string PrefabFileName { get; protected set; }

        /// <summary>
        /// The <see cref="TechType"/> of the corresponding item.
        /// Used for <see cref="TechTag" />, and <see cref="Constructable" /> components whenever applicable.
        /// </summary>
        public TechType TechType { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModPrefab" /> class.
        /// </summary>
        /// <param name="classId">The class identifier used for the <see cref="PrefabIdentifier" /> component whenever applicable.</param>
        /// <param name="prefabFileName">Name of the prefab file.</param>
        /// <param name="techType">The techtype of the corresponding item. 
        /// Used for the <see cref="TechTag" /> and <see cref="Constructable" /> components whenever applicable.
        /// Can also be set later in the constructor if it is not yet provided.</param>
        protected ModPrefab(string classId, string prefabFileName, TechType techType = TechType.None)
        {
            this.ClassID = classId;
            this.PrefabFileName = prefabFileName;
            this.TechType = techType;

            Mod = GetType().Assembly;
        }

        internal GameObject GetGameObjectInternal()
        {
            GameObject go = GetGameObject();
            if (go == null)
            {
                return null;
            }

            ProcessPrefab(go);
            return go;
        }

        internal IEnumerator GetGameObjectInternalAsync(IOut<GameObject> gameObject)
        {
            TaskResult<GameObject> taskResult = new();
            yield return GetGameObjectAsync(taskResult);

            GameObject go = taskResult.Get();
            if (go == null)
            {
                yield break;
            }

            ProcessPrefab(go);
            gameObject.Set(go);
        }

        /// <summary>
        /// Caches the prefab, then sets its TechType and ClassID to a default set of values applicable to most mods.<br/>
        /// FOR ADVANCED MODDING ONLY. Do not override unless you know exactly what you are doing.
        /// </summary>
        /// <param name="go"></param>
        protected virtual void ProcessPrefab(GameObject go)
        {
            if (go.activeInHierarchy) // inactive prefabs don't need to be removed by cache
            {
                ModPrefabCache.AddPrefab(go);
            }

            go.name = this.ClassID;

            if (this.TechType != TechType.None)
            {
                if (go.GetComponent<TechTag>() is TechTag tag)
                {
                    tag.type = this.TechType;
                }

                if (go.GetComponent<Constructable>() is Constructable cs)
                {
                    cs.techType = this.TechType;
                }
            }

            if (go.GetComponent<PrefabIdentifier>() is PrefabIdentifier pid)
            {
                pid.ClassId = this.ClassID;
            }
        }


        /// <summary>
        /// Gets the prefab game object. Set up your prefab components here.
        /// The <see cref="TechType"/> and ClassID are already handled.
        /// </summary>
        /// <returns>The game object to be instantiated into a new in-game entity.</returns>
        public virtual GameObject GetGameObject()
        {
            return null;
        }

        /// <summary>
        /// Gets the prefab game object asynchronously. Set up your prefab components here.
        /// The <see cref="TechType"/> and ClassID are already handled.
        /// </summary>
        /// <param name="gameObject"> The game object to be instantiated into a new in-game entity. </param>
        public virtual IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            return null;
        }
    }
}
