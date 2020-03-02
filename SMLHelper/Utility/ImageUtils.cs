﻿namespace SMLHelper.V2.Utility
{
    using System.IO;
    using UnityEngine;
    using Logger = V2.Logger;

    /// <summary>
    /// A collection of image loading utility methods that can create Unity objects from image files at runtime.
    /// </summary>
    public static class ImageUtils
    {
        /// <summary>
        /// Creates a new <see cref="Texture2D" /> from an image file.
        /// </summary>
        /// <param name="filePathToImage">The path to the image file.</param>
        /// <param name="format">
        /// <para>The texture format. By default, this uses <see cref="TextureFormat.BC7" />.</para>
        /// <para>https://docs.unity3d.com/ScriptReference/TextureFormat.BC7.html</para>
        /// <para>Don't change this unless you really know what you're doing.</para>
        /// </param>
        /// <returns>Will return a new <see cref="Texture2D"/> instance if the file exists; Otherwise returns null.</returns>
        /// <remarks>
        /// Ripped from: https://github.com/RandyKnapp/SubnauticaModSystem/blob/master/SubnauticaModSystem/Common/Utility/ImageUtils.cs
        /// </remarks>
        public static Texture2D LoadTextureFromFile(string filePathToImage, TextureFormat format = TextureFormat.BC7)
        {
            if (File.Exists(filePathToImage))
            {
                byte[] imageBytes = File.ReadAllBytes(filePathToImage);
                var texture2D = new Texture2D(2, 2, format, false);
                try
                {
                    texture2D.LoadImage(imageBytes);
                    return texture2D;
                }
                catch (UnityException uex)
                {
                    Logger.Error("Error on LoadTextureFromFile call. Texture cannot be loaded: " + uex);
                }

            }
            else
            {
                Logger.Log("Error on LoadTextureFromFile call. File not found at " + filePathToImage, LogLevel.Error);
            }

            return null;
        }

#if SUBNAUTICA
        /// <summary>
        /// Creates a new <see cref="Atlas.Sprite" /> from an image file.
        /// </summary>
        /// <param name="filePathToImage">The path to the image file.</param>
        /// <param name="format">
        /// <para>The texture format. By default, this uses <see cref="TextureFormat.BC7" />.</para>
        /// <para>https://docs.unity3d.com/ScriptReference/TextureFormat.BC7.html</para>
        /// <para>Don't change this unless you really know what you're doing.</para>
        /// </param>
        /// <returns>Will return a new <see cref="Atlas.Sprite"/> instance if the file exists; Otherwise returns null.</returns>
        public static Atlas.Sprite LoadSpriteFromFile(string filePathToImage, TextureFormat format = TextureFormat.BC7)
        {
            Texture2D texture2D = LoadTextureFromFile(filePathToImage, TextureFormat.BC7);

            return new Atlas.Sprite(texture2D);
        }

        /// <summary>
        /// Creates a new <see cref="Atlas.Sprite" /> from an image file.
        /// </summary>
        /// <param name="texture2D">The 2D texture to convert into a sprite.</param>
        /// <returns>
        /// Will return a new <see cref="Atlas.Sprite" /> instance using the provided texture.
        /// </returns>
        public static Atlas.Sprite LoadSpriteFromTexture(Texture2D texture2D)
        {
            return new Atlas.Sprite(texture2D);
        }
#elif BELOWZERO

        /// <summary>
        /// Creates a new <see cref="Sprite" /> from an image file.
        /// </summary>
        /// <param name="filePathToImage">The path to the image file.</param>
        /// <param name="format">
        /// The texture format. By default, this uses <see cref="TextureFormat.BC7" />.<br/>
        /// https://docs.unity3d.com/ScriptReference/TextureFormat.BC7.html <br/>
        /// Don't change this unless you really know what you're doing.
        /// </param>
        /// <returns>Will return a new <see cref="Sprite"/> instance if the file exists; Otherwise returns null.</returns>
        public static Sprite LoadSpriteFromFile(string filePathToImage, TextureFormat format = TextureFormat.BC7)
        {
            Texture2D texture2D = LoadTextureFromFile(filePathToImage, format);
            return LoadSpriteFromTexture(texture2D);
        }


        /// <summary>
        /// Creates a new <see cref="Sprite" /> from an image file.
        /// </summary>
        /// <param name="texture2D">The 2D texture to convert into a sprite.</param>
        /// <returns>
        /// Will return a new <see cref="Sprite" /> instance using the provided texture.
        /// </returns>
        public static Sprite LoadSpriteFromTexture(Texture2D texture2D)
        {
            return Sprite.Create(texture2D, new Rect(0.0f, 0.0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
        }
#endif
    }
}
