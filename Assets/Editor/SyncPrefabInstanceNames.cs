using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 프리팹 에셋 이름을 변경한 뒤, 이미 배치된 인스턴스들의 이름을 일괄 동기화하는 툴
// 사용법: 프로젝트 창에서 프리팹을 선택 → Tools/프리팹 인스턴스 이름 동기화 메뉴 실행
public static class SyncPrefabInstanceNames
{
    // 열려있는 씬의 인스턴스만 동기화
    [MenuItem("Tools/프리팹 인스턴스 이름 동기화/열린 씬만")]
    private static void SyncOpenScenes()
    {
        var prefab = GetSelectedPrefab();
        if (!prefab)
            return;

        int count = SyncInOpenScenes(prefab);
        GameLog.Info($"[SyncPrefabInstanceNames] 열린 씬에서 {count}개 인스턴스 이름을 '{prefab.name}'(으)로 변경");
    }

    // 열려있는 씬 + 프로젝트의 모든 프리팹 에셋 내부(중첩 인스턴스)까지 동기화
    [MenuItem("Tools/프리팹 인스턴스 이름 동기화/열린 씬 + 모든 프리팹 내부")]
    private static void SyncScenesAndPrefabs()
    {
        var prefab = GetSelectedPrefab();
        if (!prefab)
            return;

        int sceneCount = SyncInOpenScenes(prefab);
        int prefabCount = SyncInAllPrefabAssets(prefab);
        GameLog.Info($"[SyncPrefabInstanceNames] 씬 {sceneCount}개, 프리팹 내부 {prefabCount}개 인스턴스 이름을 '{prefab.name}'(으)로 변경");
    }

    private static GameObject GetSelectedPrefab()
    {
        var prefab = Selection.activeObject as GameObject;
        if (!prefab || !AssetDatabase.Contains(prefab))
        {
            EditorUtility.DisplayDialog("프리팹 인스턴스 이름 동기화", "프로젝트 창에서 프리팹 에셋을 먼저 선택하세요.", "확인");
            return null;
        }
        return prefab;
    }

    private static int SyncInOpenScenes(GameObject prefab)
    {
        int count = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!TryRename(tr.gameObject, prefab, recordUndo: true))
                        continue;

                    count++;
                }
            }

            if (count > 0)
                EditorSceneManager.MarkSceneDirty(scene);
        }
        return count;
    }

    private static int SyncInAllPrefabAssets(GameObject prefab)
    {
        string targetPath = AssetDatabase.GetAssetPath(prefab);
        int count = 0;

        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        for (var i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path == targetPath)
                continue;

            EditorUtility.DisplayProgressBar("프리팹 인스턴스 이름 동기화", path, (float)i / guids.Length);

            var contents = PrefabUtility.LoadPrefabContents(path);
            int changed = 0;
            foreach (var tr in contents.GetComponentsInChildren<Transform>(true))
            {
                if (!TryRename(tr.gameObject, prefab, recordUndo: false))
                    continue;

                changed++;
            }

            if (changed > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                count += changed;
            }
            PrefabUtility.UnloadPrefabContents(contents);
        }

        EditorUtility.ClearProgressBar();
        return count;
    }

    // 해당 게임오브젝트가 대상 프리팹의 인스턴스 루트라면 이름을 프리팹 이름으로 맞춘다
    private static bool TryRename(GameObject go, GameObject prefab, bool recordUndo)
    {
        if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
            return false;

        var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
        if (source != prefab)
            return false;

        if (go.name == prefab.name)
            return false;

        if (recordUndo)
            Undo.RecordObject(go, "Sync Prefab Instance Name");

        go.name = prefab.name;
        EditorUtility.SetDirty(go);
        return true;
    }
}
