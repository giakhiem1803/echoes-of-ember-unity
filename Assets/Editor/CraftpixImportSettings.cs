using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps imported CraftPix pixel assets crisp and ready for 2D use.
/// Sprite slicing is deliberately left manual because backgrounds and
/// animation sheets require different slice settings.
/// </summary>
public sealed class CraftpixImportSettings : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
    }
}
