using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PrefabCacher : MonoBehaviour
{
    // 인스펙터 우클릭 메뉴에 “Cache Prefabs” 항목 추가
    [ContextMenu("Cache Prefabs")]
    private void CachePrefabs()
    {
#if UNITY_EDITOR
        GameManager.Instance.GetPrefabList().Clear();
        
        // .prefab 에셋만 찾기
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { ConstValues.PrefabFolder });

        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            // .prefab 에셋을 직접 로드
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
                GameManager.Instance.GetPrefabList().Add(prefab);
        }

        // 변경 사항 저장
        EditorUtility.SetDirty(this);
        Debug.Log($"[PrefabCacher] {GameManager.Instance.GetPrefabList().Count}개 프리팹을 '{ConstValues.PrefabFolder}'에서 캐싱했습니다.");
#else
        Debug.LogWarning("CachePrefabs는 에디터 모드에서만 동작합니다.");
#endif
    }
}