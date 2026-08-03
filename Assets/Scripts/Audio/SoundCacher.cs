using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SoundCacher : MonoBehaviour
{
    // 인스펙터 우클릭 메뉴에 “Cache Sounds” 항목 추가
    [ContextMenu("Cache Sounds")]
    private void CacheSounds()
    {
#if UNITY_EDITOR
        // 1) 기존 리스트 비우기
        var soundList = SoundManager.Instance.GetSoundList();
        soundList.Clear();
        
        // 2) Sound 폴더 내부에서 AudioClip 타입만 검색
        //    ConstValues.SoundFolder 은 "Assets/YourGame/Sound" 같은 상대 경로이어야 합니다.
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { ConstValues.SoundFolder });

        // 3) GUID → 경로 → AudioClip 로드
        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip != null)
                soundList.Add(clip);
        }

        // 4) 변경 사항 마크 및 로그 출력
        EditorUtility.SetDirty(SoundManager.Instance);
        Debug.Log($"[SoundCacher] '{ConstValues.SoundFolder}'에서 {soundList.Count}개 오디오 클립을 캐싱했습니다.");
#else
        Debug.LogWarning("Cache Sounds는 에디터 모드에서만 동작합니다.");
#endif
    }
}
