using UnityEngine;

// Alt+Enter 전체화면 토글과 일반 Enter 입력이 충돌하지 않도록 하는 입력 헬퍼
public static class InputHelper
{
    // Alt(좌/우)가 눌려있는지
    public static bool IsAltPressed => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

    // Alt가 눌린 상태에서는 false 반환 (메뉴 선택 등 기존 Enter 액션 차단)
    public static bool GetEnterDown() => !IsAltPressed && Input.GetKeyDown(KeyCode.Return);

    public static bool GetKeypadEnterDown() => !IsAltPressed && Input.GetKeyDown(KeyCode.KeypadEnter);
}
