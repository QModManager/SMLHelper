﻿namespace SMLHelper.Assets
{
    using SMLHelper.Handlers;

    /// <summary>
    /// An item that can be built into the game world using the builder tool.
    /// </summary>
    /// <seealso cref="PdaItem" />
    /// <seealso cref="Spawnable"/>
    public abstract class Buildable : PdaItem
    {
        /// <summary>
        /// Initializes a new <see cref="Buildable"/>, the basic class for any item that can built using the Habitat Builder Tool.
        /// </summary>
        /// <param name="classId">The main internal identifier for this item. Your item's <see cref="TechType" /> will be created using this name.</param>
        /// <param name="friendlyName">The name displayed in-game for this item whether in the open world or in the inventory.</param>
        /// <param name="description">The description for this item; Typically seen in the PDA, inventory, or crafting screens.</param>
        protected Buildable(string classId, string friendlyName, string description)
            : base(classId, friendlyName, description)
        {
            CorePatchEvents += ()=> ModPrefabBuilder.Create(this).SetBuildableFlag();
        }
    }
}
