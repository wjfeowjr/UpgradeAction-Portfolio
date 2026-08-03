using System;
using UnityEngine;
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
using System.Runtime.InteropServices;
#endif

// 창모드에서 창 테두리에 마우스를 올렸을 때 크기 조절 커서(↔)가 표시되도록 하는 매니저
// 유니티 플레이어는 WM_SETCURSOR 메시지를 가로채 테두리 위에서도 게임 커서를 강제하므로,
// WndProc을 서브클래싱해서 비클라이언트 영역(테두리, 제목줄)의 커서 처리만 OS 기본 동작으로 돌려준다.
// 씬에 배치할 필요 없이 게임 시작 시 자동 생성된다.
public class WindowFrameWatcher : MonoBehaviour
{
    private static WindowFrameWatcher instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        if (instance)
            return;

        var go = new GameObject("WindowFrameWatcher");
        instance = go.AddComponent<WindowFrameWatcher>();
        DontDestroyOnLoad(go);
#endif
    }

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
    private const int  GWLP_WNDPROC = -4;
    private const uint WM_SETCURSOR = 0x0020;
    private const int  HTCLIENT     = 1;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // GC가 델리게이트를 수거하지 않도록 static으로 유지 (수거되면 네이티브 콜백이 죽음)
    private static WndProcDelegate _wndProcDelegate;
    private static IntPtr _originalWndProc;
    private static IntPtr _hookedWindow;

    private void Update()
    {
        // 창 핸들이 아직 없거나 바뀐 경우(창 재생성 등) 후킹
        IntPtr hWnd = GetActiveWindow();
        if (hWnd == IntPtr.Zero || hWnd == _hookedWindow)
            return;

        HookWindow(hWnd);
    }

    private static void HookWindow(IntPtr hWnd)
    {
        _wndProcDelegate = WndProc;
        _originalWndProc = SetWindowLongPtr(hWnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
        _hookedWindow    = hWnd;
    }

    [AOT.MonoPInvokeCallback(typeof(WndProcDelegate))]
    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_SETCURSOR)
        {
            // lParam 하위 워드 = 히트 테스트 결과. 클라이언트 영역(게임 화면)이 아니면
            // OS 기본 처리에 맡겨 크기 조절 화살표 등 표준 커서가 표시되게 한다
            int hitTest = unchecked((int)(long)lParam) & 0xFFFF;
            if (hitTest != HTCLIENT)
                return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }

    private void OnDestroy()
    {
        // 원래 WndProc 복원
        if (_hookedWindow != IntPtr.Zero && _originalWndProc != IntPtr.Zero)
        {
            SetWindowLongPtr(_hookedWindow, GWLP_WNDPROC, _originalWndProc);
            _hookedWindow    = IntPtr.Zero;
            _originalWndProc = IntPtr.Zero;
        }
    }
#endif
}
