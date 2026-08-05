// UI · 팝업 프리팹 식별자
//
// 값을 명시적으로 고정했다.
// UIBase 가 이 enum 을 [SerializeField] 로 들고 있어 프리팹마다 정수로 저장되는데,
// 중간 항목을 지우면 뒤쪽 값이 하나씩 밀려 모든 프리팹의 uiType 이 어긋난다.
// 항목을 추가할 때는 뒤에 새 번호로 붙이고, 지운 번호는 재사용하지 않는다.

public enum eUIType
{
    None = 0,

    // UI
    UI_Interface   = 1,
    UI_Episode     = 2,
    UI_BossMessage = 3,
    // 4 = UI_StageClear (Stage 계열 제거와 함께 삭제)

    // 팝업
    Popup_GameOver   = 5,
    Popup_Guide      = 6,
    Popup_Minimap    = 7,
    Popup_Warning    = 8,
    Popup_Character  = 9,
    Popup_Select     = 10,
    Popup_Store      = 11,
    Popup_Pause      = 12,
    Popup_Setting    = 13,
    Popup_FastTravel = 14,
}
