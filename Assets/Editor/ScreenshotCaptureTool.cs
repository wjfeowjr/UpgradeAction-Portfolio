using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디터에서 F5를 누르면 게임 화면 스크린샷을 찍어 지정 경로에 저장하는 툴
/// </summary>
public static class ScreenshotCaptureTool
{
    private const string saveFolder = @"C:\Users\uutkr\Desktop\TextOutside";

    // 메뉴 이름 뒤의 "_F5"가 단축키(F5, 수정키 없음)를 의미한다
    [MenuItem("Tools/스크린샷 찍기 _F5")]
    private static void CaptureScreenshot()
    {
        if (GameManager.Instance.isDemo)
        {
            Debug.Log($"데모버전은 스크린샷이 찍히지 않음");
            return;
        }
        
        if (!Directory.Exists(saveFolder))
            Directory.CreateDirectory(saveFolder);

        string filePath = Path.Combine(saveFolder, $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        if (Application.isPlaying)
        {
            // 플레이 모드: Game 뷰 전체(UI 포함)를 그대로 캡처
            ScreenCapture.CaptureScreenshot(filePath);
            Debug.Log($"스크린샷 저장 완료: {filePath}");
        }
        else
        {
            // 에디트 모드: 메인 카메라를 직접 렌더링해서 캡처
            CaptureFromMainCamera(filePath);
        }
    }

    private static void CaptureFromMainCamera(string filePath)
    {
        Camera cam = Camera.main;
        if (!cam)
            cam = UnityEngine.Object.FindAnyObjectByType<Camera>();

        if (!cam)
        {
            Debug.LogWarning("씬에 카메라가 없어 스크린샷을 찍을 수 없습니다.");
            return;
        }

        int width = cam.pixelWidth > 0 ? cam.pixelWidth : 1920;
        int height = cam.pixelHeight > 0 ? cam.pixelHeight : 1080;

        RenderTexture rt = RenderTexture.GetTemporary(width, height, 24);
        RenderTexture prevTarget = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        try
        {
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            Debug.Log($"스크린샷 저장 완료: {filePath}");
        }
        finally
        {
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
        }
    }
}
