// GameManager - 다국어 텍스트 조회
//
// 실제 로직은 LocalizationService 에 있다.
// GetTalk 만 293곳에서 호출되고 있어, 호출부를 건드리지 않도록
// 기존 시그니처를 그대로 두고 위임만 한다.
// 호출부를 점진적으로 Localization.GetTalk(...) 으로 옮긴 뒤 이 파일을 지운다.

using UnityEngine;

public partial class GameManager
{
    public string GetTalk(int idx)
        => localization.GetTalk(idx, language);

    public string GetCharacterTalk(string id)
        => localization.GetCharacterTalk(id, language);

    public string GetItemTalk(string id)
        => localization.GetItemTalk(id, language);

    public string GetItemExplain(string id)
        => localization.GetItemExplain(id, language);

    public string GetStatName(string statId)
        => localization.GetStatName(statId, language);

    public string GetPlaceName(ePlace place)
        => localization.GetPlaceName(place, language);

    public string GetKeyCode(KeyCode keycode)
        => LocalizationService.GetKeyCodeText(keycode);
}
