// GameManager - 다국어 텍스트 조회
//
// 실제 로직은 LocalizationService 에 있다.
// GetTalk 만 293곳에서 호출되고 있어, 호출부를 건드리지 않도록
// 기존 시그니처를 그대로 두고 위임만 한다.
// 언어는 LocalizationService 가 소유하므로 인자로 넘기지 않는다.

using UnityEngine;

public partial class GameManager
{
    public string GetTalk(int idx)
        => localization.GetTalk(idx);

    public string GetCharacterTalk(string id)
        => localization.GetCharacterTalk(id);

    public string GetItemTalk(string id)
        => localization.GetItemTalk(id);

    public string GetItemExplain(string id)
        => localization.GetItemExplain(id);

    public string GetStatName(string statId)
        => localization.GetStatName(statId);

    public string GetPlaceName(ePlace place)
        => localization.GetPlaceName(place);

    public string GetKeyCode(KeyCode keycode)
        => LocalizationService.GetKeyCodeText(keycode);
}
