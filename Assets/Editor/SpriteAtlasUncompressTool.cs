using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public static class SpriteAtlasUncompressTool
{
    private const string MenuPath = "Tools/Sprite Atlas/패킹된 텍스처 압축 해제";

    [MenuItem(MenuPath)]
    private static void UncompressSelectedAtlasTextures()
    {
        var texturePaths = new HashSet<string>();
        int atlasCount = 0;

        foreach (var guid in Selection.assetGUIDs)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            var lower = path.ToLowerInvariant();
            if (!lower.EndsWith(".spriteatlas") && !lower.EndsWith(".spriteatlasv2"))
                continue;

            atlasCount++;
            CollectFromAtlasPath(path, texturePaths);
        }

        if (atlasCount == 0)
        {
            EditorUtility.DisplayDialog(
                "Sprite Atlas 압축 해제",
                "Project 창에서 SpriteAtlas(.spriteatlas / .spriteatlasv2) 에셋을 1개 이상 선택한 후 다시 실행하세요.",
                "확인");
            return;
        }

        if (texturePaths.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Sprite Atlas 압축 해제",
                "패킹 대상 텍스처를 찾지 못했습니다.",
                "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Sprite Atlas 압축 해제",
                $"선택한 Atlas {atlasCount}개의 패킹 대상 텍스처 {texturePaths.Count}개에 대해\nTextureImporter.textureCompression = Uncompressed 로 변경합니다.\n\n진행할까요?",
                "진행",
                "취소"))
        {
            return;
        }

        int changed = 0;
        int skipped = 0;
        try
        {
            AssetDatabase.StartAssetEditing();

            int index = 0;
            foreach (var path in texturePaths)
            {
                index++;
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Sprite Atlas 압축 해제",
                        $"{index}/{texturePaths.Count}  {path}",
                        (float)index / texturePaths.Count))
                {
                    break;
                }

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    skipped++;
                    continue;
                }
                if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    skipped++;
                    continue;
                }

                importer.textureCompression = TextureImporterCompression.Uncompressed;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                changed++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[SpriteAtlasUncompressTool] 변경 {changed}개 / 이미 무압축이거나 건너뜀 {skipped}개 / 검사 총 {texturePaths.Count}개.");
        EditorUtility.DisplayDialog(
            "완료",
            $"변경: {changed}개\n건너뜀(이미 무압축): {skipped}개\n총 검사: {texturePaths.Count}개",
            "확인");
    }

    private static void CollectFromAtlasPath(string atlasPath, HashSet<string> output)
    {
        // .spriteatlasv2 도 SpriteAtlas 런타임 타입으로 LoadAssetAtPath 가능
        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (atlas != null)
        {
            CollectPackables(atlas.GetPackables(), output);
            return;
        }

        Debug.LogWarning($"[SpriteAtlasUncompressTool] SpriteAtlas 로드 실패: {atlasPath}");
    }

    private static void CollectPackables(Object[] packables, HashSet<string> output)
    {
        if (packables == null) return;

        foreach (var packable in packables)
        {
            if (packable == null) continue;

            var path = AssetDatabase.GetAssetPath(packable);
            if (string.IsNullOrEmpty(path)) continue;

            if (AssetDatabase.IsValidFolder(path))
            {
                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                foreach (var guid in guids)
                {
                    var texPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(texPath))
                        output.Add(texPath);
                }
            }
            else
            {
                output.Add(path);
            }
        }
    }
}
