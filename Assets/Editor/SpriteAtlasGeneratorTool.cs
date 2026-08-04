using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SpriteAtlasGeneratorTool
{
    private const string MenuPath = "Tools/Sprite Atlas/선택한 폴더로 개별 SpriteAtlas 생성";

    [MenuItem(MenuPath)]
    private static void CreateAtlasesFromSelectedFolders()
    {
        var folders = new List<string>();
        foreach (var guid in Selection.assetGUIDs)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
                folders.Add(path);
        }

        if (folders.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Sprite Atlas 생성",
                "Project 창에서 폴더를 1개 이상 선택한 후 다시 실행하세요.",
                "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Sprite Atlas 생성",
            $"선택한 폴더 {folders.Count}개에 대해 SpriteAtlas를 생성합니다.\n\n" +
            "각 폴더 내부에 '<폴더이름>Atlas.spriteatlasv2' 파일이 생성됩니다.\n" +
            "textureCompression 은 Uncompressed (None) 로 설정됩니다.\n\n" +
            "진행할까요?",
            "진행", "취소"))
        {
            return;
        }

        int created = 0, skipped = 0;
        try
        {
            AssetDatabase.StartAssetEditing();

            int index = 0;
            foreach (var folderPath in folders)
            {
                index++;
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Sprite Atlas 생성",
                        $"{index}/{folders.Count}  {folderPath}",
                        (float)index / folders.Count))
                {
                    break;
                }

                var folderName = Path.GetFileName(folderPath);
                var atlasAssetPath = $"{folderPath}/{folderName}Atlas.spriteatlasv2";
                var atlasFsPath = Path.GetFullPath(atlasAssetPath);
                var atlasMetaFsPath = atlasFsPath + ".meta";

                if (File.Exists(atlasFsPath))
                {
                    GameLog.Info($"[SpriteAtlasGenerator] 이미 존재: {atlasAssetPath}");
                    skipped++;
                    continue;
                }

                var folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
                if (string.IsNullOrEmpty(folderGuid))
                {
                    Debug.LogWarning($"[SpriteAtlasGenerator] 폴더 GUID 조회 실패: {folderPath}");
                    skipped++;
                    continue;
                }

                var atlasGuid = Guid.NewGuid().ToString("N");

                File.WriteAllText(atlasFsPath, BuildAtlasYaml(folderGuid));
                File.WriteAllText(atlasMetaFsPath, BuildAtlasMetaYaml(atlasGuid));

                GameLog.Info($"[SpriteAtlasGenerator] 생성: {atlasAssetPath}");
                created++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog(
            "완료",
            $"생성: {created}개\n건너뜀(이미 존재/실패): {skipped}개",
            "확인");
    }

    private static string BuildAtlasYaml(string folderGuid)
    {
        return
            "%YAML 1.1\n" +
            "%TAG !u! tag:unity3d.com,2011:\n" +
            "--- !u!612988286 &1\n" +
            "SpriteAtlasAsset:\n" +
            "  m_ObjectHideFlags: 0\n" +
            "  m_CorrespondingSourceObject: {fileID: 0}\n" +
            "  m_PrefabInstance: {fileID: 0}\n" +
            "  m_PrefabAsset: {fileID: 0}\n" +
            "  m_Name: \n" +
            "  serializedVersion: 2\n" +
            "  m_MasterAtlas: {fileID: 0}\n" +
            "  m_ImporterData:\n" +
            "    packables:\n" +
            $"    - {{fileID: 102900000, guid: {folderGuid}, type: 3}}\n" +
            "  m_IsVariant: 0\n" +
            "  m_ScriptablePacker: {fileID: 0}\n";
    }

    private static string BuildAtlasMetaYaml(string atlasGuid)
    {
        return
            "fileFormatVersion: 2\n" +
            $"guid: {atlasGuid}\n" +
            "SpriteAtlasImporter:\n" +
            "  externalObjects: {}\n" +
            "  textureSettings:\n" +
            "    serializedVersion: 2\n" +
            "    anisoLevel: 1\n" +
            "    compressionQuality: 50\n" +
            "    maxTextureSize: 2048\n" +
            "    textureCompression: 0\n" +
            "    filterMode: 1\n" +
            "    generateMipMaps: 0\n" +
            "    readable: 0\n" +
            "    crunchedCompression: 0\n" +
            "    sRGB: 1\n" +
            "  platformSettings:\n" +
            "  - serializedVersion: 4\n" +
            "    buildTarget: DefaultTexturePlatform\n" +
            "    maxTextureSize: 4096\n" +
            "    resizeAlgorithm: 0\n" +
            "    textureFormat: -1\n" +
            "    textureCompression: 0\n" +
            "    compressionQuality: 50\n" +
            "    crunchedCompression: 0\n" +
            "    allowsAlphaSplitting: 0\n" +
            "    overridden: 0\n" +
            "    ignorePlatformSupport: 0\n" +
            "    androidETC2FallbackOverride: 0\n" +
            "    forceMaximumCompressionQuality_BC6H_BC7: 0\n" +
            "  packingSettings:\n" +
            "    serializedVersion: 2\n" +
            "    padding: 4\n" +
            "    blockOffset: 1\n" +
            "    allowAlphaSplitting: 0\n" +
            "    enableRotation: 0\n" +
            "    enableTightPacking: 0\n" +
            "    enableAlphaDilation: 0\n" +
            "  secondaryTextureSettings: {}\n" +
            "  variantMultiplier: 1\n" +
            "  bindAsDefault: 1\n" +
            "  userData: \n" +
            "  assetBundleName: \n" +
            "  assetBundleVariant: \n";
    }
}
