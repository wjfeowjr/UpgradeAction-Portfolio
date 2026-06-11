using UnityEngine;

// 16:9 기준 레터박스 매니저
// 씬에 배치할 필요 없이 게임 시작 시 자동 생성된다.
// 화면 비율이 16:9가 아니면 모든 카메라의 viewport rect를 16:9 영역으로 제한하고,
// 남는 영역(위아래 또는 좌우)은 검은색으로 채운다.
public class LetterboxManager : MonoBehaviour
{
    private const float TargetAspect = 16f / 9f;

    private static LetterboxManager instance;

    private Camera clearCamera; // 레터박스 영역을 검은색으로 칠하는 카메라
    private Rect targetRect = new Rect(0, 0, 1, 1);
    private int lastWidth;
    private int lastHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance)
            return;

        var go = new GameObject("LetterboxManager");
        instance = go.AddComponent<LetterboxManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        CreateClearCamera();
        RefreshTargetRect();
    }

    private void Update()
    {
        // 해상도 변경 감지 (설정 변경, 전체화면 전환 등)
        if (Screen.width != lastWidth || Screen.height != lastHeight)
            RefreshTargetRect();

        ApplyToAllCameras();
    }

    // 레터박스 영역을 칠할 전용 카메라 생성 (아무것도 렌더링하지 않고 검은색 클리어만 수행)
    private void CreateClearCamera()
    {
        var go = new GameObject("LetterboxClearCamera");
        go.transform.SetParent(transform);

        clearCamera = go.AddComponent<Camera>();
        clearCamera.depth = -100;
        clearCamera.clearFlags = CameraClearFlags.SolidColor;
        clearCamera.backgroundColor = Color.black;
        clearCamera.cullingMask = 0;
        clearCamera.rect = new Rect(0, 0, 1, 1);
        clearCamera.orthographic = true;
        clearCamera.useOcclusionCulling = false;
        clearCamera.allowHDR = false;
        clearCamera.allowMSAA = false;
    }

    private void RefreshTargetRect()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float windowAspect = (float)Screen.width / Screen.height;
        float scale = windowAspect / TargetAspect;

        if (scale < 1f)
        {
            // 16:9보다 세로가 긴 화면 (4:3, 16:10 등) → 위아래 레터박스
            targetRect = new Rect(0, (1f - scale) / 2f, 1f, scale);
        }
        else
        {
            // 16:9보다 가로가 긴 화면 (21:9 등) → 좌우 필러박스
            float scaleWidth = 1f / scale;
            targetRect = new Rect((1f - scaleWidth) / 2f, 0, scaleWidth, 1f);
        }
    }

    // 화면에 직접 렌더링하는 모든 카메라에 적용 (렌더 텍스처 카메라는 제외)
    // 씬 전환이나 카메라 활성화 시점을 따로 추적하지 않도록 매 프레임 검사한다.
    private void ApplyToAllCameras()
    {
        foreach (var cam in Camera.allCameras)
        {
            if (cam == clearCamera || cam.targetTexture)
                continue;

            if (cam.rect != targetRect)
                cam.rect = targetRect;
        }
    }
}
