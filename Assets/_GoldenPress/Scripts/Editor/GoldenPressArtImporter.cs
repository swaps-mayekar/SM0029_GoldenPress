using System.IO;
using UnityEditor;
using UnityEngine;

namespace GoldenPress.EditorTools
{
    public static class GoldenPressArtImporter
    {
        private const string ArtRoot = "Assets/_GoldenPress/Art";
        private const string ResourcesArt = "Assets/_GoldenPress/Resources/Art";

        [MenuItem("Golden Press/Configure Art Import Settings")]
        public static void ConfigureArtImportSettings()
        {
            ConfigureFolder(ArtRoot);
            ConfigureFolder(ResourcesArt);
            AssetDatabase.Refresh();
            Debug.Log("Golden Press art import settings configured as Sprite (UI).");
        }

        private static void ConfigureFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }

                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    dirty = true;
                }

                if (importer.alphaIsTransparency == false)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }

                var isBackground = path.Contains("Backgrounds") || path.Contains("splash_") || path.Contains("hub_bg") || path.Contains("production_bg");
                var maxSize = isBackground ? 2048 : 1024;
                if (importer.maxTextureSize != maxSize)
                {
                    importer.maxTextureSize = maxSize;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
