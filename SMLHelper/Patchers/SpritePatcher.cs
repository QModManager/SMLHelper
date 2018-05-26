﻿using Harmony;
using System.Reflection;

namespace SMLHelper.Patchers
{
    public class SpritePatcher
    {
        public static void Patch(HarmonyInstance harmony)
        {
            var spriteManager = typeof(SpriteManager);
            var getFromResources = spriteManager.GetMethod("GetFromResources", BindingFlags.Public | BindingFlags.Static);

            harmony.Patch(getFromResources,
                new HarmonyMethod(typeof(SpritePatcher).GetMethod("Prefix")), null);
#if DEBUG
            Logger.Log("SpritePatcher is done.");
#endif
        }

        public static bool Prefix(ref Atlas.Sprite __result, string name)
        {
            foreach (var sprite in CustomSpriteHandler.customSprites)
            {
                if (sprite.TechType.AsString(true) == name.ToLowerInvariant())
                {
                    __result = sprite.Sprite;
                    return false;
                }
                else if(sprite.TechType == TechType.None && sprite.Id.ToLowerInvariant() == name.ToLowerInvariant())
                {
                    __result = sprite.Sprite;
                    return false;
                }
            }

            return true;
        }

    }
}
