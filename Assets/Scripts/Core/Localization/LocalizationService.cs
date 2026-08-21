// 다국어 텍스트 조회 서비스
//
// GameManager 에서 분리했다. MonoBehaviour 가 아니므로 Unity 런타임 없이 테스트할 수 있다.
//
// 분리하면서 조회 방식을 바꿨다.
//   이전: 호출마다 Talk.Find(x => x.idx == idx) 로 502개 항목을 선형 탐색
//   현재: 생성 시 Dictionary<int, TalkData> 로 한 번 펼쳐두고 O(1) 조회
// GetTalk 은 코드베이스 293곳에서 호출되므로 조회 비용이 그대로 UI 비용이 된다.

using System.Collections.Generic;
using UnityEngine;

public class LocalizationService
{
    private readonly Dictionary<int, TalkData> talkById = new Dictionary<int, TalkData>();
    private readonly Dictionary<string, ItemData> itemById = new Dictionary<string, ItemData>();

    /// <summary>
    /// 테이블을 주입받아 조회용 캐시를 만든다.
    /// 테이블을 인자로 받으므로 테스트에서 임의의 데이터를 넣을 수 있다.
    /// </summary>
    public LocalizationService(TalkDataList talkTable, ItemDataList itemTable)
    {
        if (talkTable?.Talk != null)
        {
            foreach (var talk in talkTable.Talk)
            {
                // 같은 idx 가 중복 정의된 경우 먼저 나온 것을 쓴다(기존 Find 동작과 동일)
                if (!talkById.ContainsKey(talk.idx))
                    talkById.Add(talk.idx, talk);
            }
        }

        if (itemTable?.Item != null)
        {
            foreach (var item in itemTable.Item)
            {
                if (!itemById.ContainsKey(item.id))
                    itemById.Add(item.id, item);
            }
        }
    }

    public int TalkCount => talkById.Count;

    /// <summary>
    /// 현재 언어. 이전에는 GameManager.language 가 들고 있어서 조회할 때마다 넘겨줘야 했다.
    /// 언어는 이 서비스의 상태이므로 여기서 소유한다.
    /// 저장/불러오기와 옵션 팝업은 GameManager.language 를 통해 이 값을 읽고 쓴다.
    /// </summary>
    public string CurrentLanguage { get; set; }

    public string GetTalk(int idx) => GetTalk(idx, CurrentLanguage);
    public string GetCharacterTalk(string id) => GetCharacterTalk(id, CurrentLanguage);
    public string GetItemTalk(string id) => GetItemTalk(id, CurrentLanguage);
    public string GetItemExplain(string id) => GetItemExplain(id, CurrentLanguage);
    public string GetStatName(string statId) => GetStatName(statId, CurrentLanguage);
    public string GetPlaceName(ePlace place) => GetPlaceName(place, CurrentLanguage);

    /// <summary>
    /// idx 에 해당하는 텍스트를 현재 언어로 반환한다.
    /// 없는 idx 면 null 을 반환한다(기존 동작은 예외였다).
    /// </summary>
    public string GetTalk(int idx, string language)
    {
        if (!talkById.TryGetValue(idx, out var talk))
        {
            Debug.LogWarning($"[Localization] Talk 테이블에 idx {idx} 가 없습니다");
            return null;
        }

        return language switch
        {
            ConstValues.Korean => talk.kr,
            ConstValues.English => talk.en,
            ConstValues.Japanese => talk.ja,
            ConstValues.ChineseSimplified => talk.cn,
            ConstValues.ChineseTraditional => talk.tw,
            ConstValues.Spanish => talk.es,
            ConstValues.Russian => talk.ru,
            ConstValues.PortugueseBrazil => talk.pt,
            _ => null,
        };
    }

    // 직업 이름
    public string GetCharacterTalk(string id, string language)
    {
        return id switch
        {
            ConstValues.Berserker => GetTalk(50000, language),
            ConstValues.Gunner => GetTalk(50001, language),
            ConstValues.Fighter => GetTalk(50002, language),
            _ => default,
        };
    }

    // 아이템 이름
    public string GetItemTalk(string id, string language)
    {
        return itemById.TryGetValue(id, out var item) ? GetTalk(item.name, language) : null;
    }

    // 아이템 설명
    public string GetItemExplain(string id, string language)
    {
        return itemById.TryGetValue(id, out var item) ? GetTalk(item.explain, language) : null;
    }

    // 스탯 이름
    public string GetStatName(string statId, string language)
    {
        return statId switch
        {
            ConstValues.CritPercent => GetTalk(50105, language),
            _ => "Null!",
        };
    }

    // 지역 이름
    public string GetPlaceName(ePlace place, string language)
    {
        return place switch
        {
            ePlace.Forest => GetTalk(130000, language),
            ePlace.BaseCamp => GetTalk(130001, language),
            ePlace.Dungeon => GetTalk(130002, language),
            ePlace.Mine => GetTalk(130003, language),
            ePlace.SnowField => GetTalk(130004, language),
            _ => "Non",
        };
    }

    /// <summary>
    /// 키 표시용 문자열. 언어와 무관하므로 static 이다.
    /// </summary>
    public static string GetKeyCodeText(KeyCode keycode)
    {
        return keycode switch
        {
            KeyCode.LeftArrow => "←",
            KeyCode.RightArrow => "→",
            KeyCode.UpArrow => "↑",
            KeyCode.DownArrow => "↓",
            KeyCode.Escape => "Esc",
            KeyCode.Return => "Enter",
            KeyCode.LeftShift => "Shift",
            KeyCode.LeftControl => "Ctrl",
            KeyCode.LeftAlt => "Alt",
            KeyCode.BackQuote => "`",
            _ => keycode.ToString(),
        };
    }
}
