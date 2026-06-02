# 보유 스킬·패시브 열람 페이지 설계안

> 캐릭터 정보 팝업에 "보유 스킬 + 클래스 고유 패시브"를 확인하는 **읽기 전용** 페이지를 추가한다.
> 패시브 범위: **클래스 고유 패시브만** (`PlayerStat.passive` / `passiveComment`).

---

## 1. 설계 원칙

- **열람(보기)과 편집(특성 구매)은 목적이 다른 행위** → 별도 페이지로 분리한다.
- `스킬 특성` 창은 `eStep` 3단계 상태머신(스킬선택→특성선택→포인트조정)으로 이미 복잡 → 열람 모드를 끼우지 않는다.
- 두 화면의 **관점(altitude)을 다르게** 두어 "효과 겹침"을 방지한다.

| | 스킬 특성 창 | 보유 스킬·패시브 창 (신규) |
|---|---|---|
| 행위 | 편집 (포인트로 특성 구매/판매) | 열람 (지금 무엇을 가졌나 확인) |
| 단위 | 개별 특성 (비용·해금 여부) | 스킬/패시브 본체 (기본 스펙) |
| 설명 텍스트 | `GetTalk(skillAttributeCopy.explainTalk)` (특성 강화 설명) | `GetTalk(skillData.explainTalk)` (스킬 기본 설명) |

→ 서로 다른 talk id를 사용하므로 내용이 겹치지 않는다.

---

## 2. 데이터 소스 (검증 완료)

| 표시 항목 | 데이터 경로 |
|---|---|
| 보유 스킬 목록 | `playerInfo.skillList` (캐릭터별). 스킬 테이블 `TableManager.Instance.skillTable.Skill`에서 `caster == curPlayerId && type != ConstValues.Dash` 필터 |
| 스킬 이름 | `GameManager.Instance.GetTalk(skillData.talk)` |
| 스킬 기본 설명 | `GameManager.Instance.GetTalk(skillData.explainTalk)` |
| 스킬 아이콘 | `GameManager.Instance.GetAtlasSprite(skillData.id)` (RoomSkillAndPassive와 동일 방식) |
| 클래스 고유 패시브 키 | `GameManager.Instance.GetPlayer(playerId).BasicStat.passive` (예: `"Rage"`, `"KnockDownGun"`) |
| 클래스 패시브 설명 | `GameManager.Instance.GetTalk(GetPlayer(playerId).BasicStat.passiveComment)` (예: talk id `502001`) |

- `GetPlayer(id).BasicStat` 접근 경로는 `PopupCharacterView`가 이미 동일하게 사용 중 → 검증됨.
- 보유 스킬 필터 로직은 `PopupAttributeView.SetSkillList()` 그대로 재사용.

### 미확정 1건 — 패시브 아이콘
`GetAtlasSprite("Rage")`로 패시브 아이콘을 가져올 수 있는지(아틀라스 존재 여부) Unity 에디터에서 확인 필요.
없으면 **아이콘 없이 이름+설명만** 표시하거나 아틀라스에 스프라이트를 추가한다.

---

## 3. 화면 구성 (읽기 전용)

```
┌─ 보유 스킬 · 패시브 ──────────── ◀ Q [캐릭터 탭] E ▶ ─┐
│                                                          │
│  ┌─ 스킬 목록(좌) ─┐   ┌─ 상세(우) ────────────────┐   │
│  │ ▣ 올려베기  ◀선택│   │  [아이콘]  올려베기          │   │
│  │ □ 불덩이 날리기  │   │  ───────────────────────    │   │
│  │ □ 황소 반격      │   │  GetTalk(skill.explainTalk) │   │
│  └─────────────────┘   │  (스킬 기본 효과 설명)       │   │
│                         └────────────────────────────┘   │
│  ┌─ 클래스 고유 패시브 (항상 표시) ───────────────────┐ │
│  │ [아이콘]  광폭화   GetTalk(passiveComment)          │ │
│  └──────────────────────────────────────────────────┘ │
│                                                          │
│             선택 이동: ↑↓ · 뒤로가기: Esc                │
└────────────────────────────────────────────────────────┘
```

- 좌측 스킬 목록만 `↑↓`로 탐색, 선택 시 우측 상세 갱신.
- **클래스 패시브는 하단 고정 패널** (선택 대상 아님). "이 캐릭터가 항상 지닌 것"이라는 의미 전달.
- `Q/E`로 캐릭터 변경 (Popup_Character가 기존대로 처리).

---

## 4. 코드 연결 설계

### 4.1 enum 변경 — `Popup_Character.cs`
```csharp
[Serializable]
public enum ePopupState
{
    Character = 0,
    SkillInfo = 1,   // 신규: 보유 스킬·패시브 열람
    Attribute = 2,
    Relic     = 3,
    Item      = 4,
}
```

### 4.2 `Popup_Character.cs` 와이어링
- 필드 추가: `[SerializeField] private PopupSkillInfoView skillInfoView;` + `private PopupSkillInfoPresenter _skillInfoPresenter;`
- `InitPresenters()`에 모델 생성:
  ```csharp
  var skillInfoModel = new PopupSkillInfoModel
  {
      playerId       = curPlayerId,
      skillDataList  = TableManager.Instance.skillTable.Skill,
      playerInfoList = playerInfoList,
      commonActions  = common,
      closeAction    = () => SetState(ePopupState.Character),
  };
  _skillInfoPresenter = new PopupSkillInfoPresenter(skillInfoView, skillInfoModel);
  ```
- `SetState()` switch + `gameObject.SetActive` 블록에 `skillInfoView` 추가:
  ```csharp
  case ePopupState.SkillInfo:
      popupText.text = GameManager.Instance.GetTalk(/* 신규 타이틀 talk id */);
      break;
  ...
  skillInfoView.gameObject.SetActive(popupState == ePopupState.SkillInfo);
  ```
- `OnExpansionStateSelected()`에 `case ePopupState.SkillInfo: SetState(ePopupState.SkillInfo); break;`
- `RefreshAll()`에 `_skillInfoPresenter.UpdatePlayerInfo(curPlayerId);`
- `Q/E` 캐릭터 변경: `popupState != Relic && != Item` 조건이라 **자동 허용**됨 (수정 불필요).

### 4.3 메뉴 항목 추가 — `PopupCharacterView.cs`
```csharp
// 팝업 선택 순서: SkillInfo → Attribute → Relic → Item
private readonly ePopupState[] _popupStateOrder =
    { ePopupState.SkillInfo, ePopupState.Attribute, ePopupState.Relic, ePopupState.Item };
```
- `choiceFrameObjects`를 4슬롯으로 확장, 첫 항목에 "보유 스킬·패시브" 텍스트 세팅(신규 menu talk id).

### 4.4 신규 스크립트 `PopupSkillInfoView.cs`
- `PopupCharacterView`를 템플릿으로 MVP 패턴 구성: `PopupSkillInfoModel` / `PopupSkillInfoPresenter` / `IPopupSkillInfoView`(`SetModel`/`SetPlayerInfo`/`SetAction`).
- 내부 로직:
  - `SetSkillList()` — `PopupAttributeView.SetSkillList()` 필터 재사용으로 좌측 스킬 프레임 채우기.
  - `Update()` — `↑↓`로 스킬 인덱스 이동, 선택 시 우측 상세(`이름`/`설명`/`아이콘`) 갱신, `Esc` → `closeAction`.
  - `RefreshPassivePanel()` — `GetPlayer(playerId).BasicStat.passive/passiveComment`로 하단 패시브 패널 1회 세팅.

---

## 5. Unity 에디터 작업 (코드 외)
1. `Popup_Character` 프리팹에 `SkillInfoView` GameObject 추가 (AttributeView 복제 후 가공이 가장 빠름).
2. 좌측 스킬 프레임 배열 / 우측 상세(이름·설명·아이콘) / 하단 패시브 패널 연결.
3. `PopupCharacterView`의 `choiceFrameObjects` 슬롯 4개로 확장 + SerializeField 연결.
4. **Talk.json**에 신규 text id 2종 추가: 메뉴명("보유 스킬·패시브"), 페이지 타이틀.
5. 패시브 아이콘 아틀라스 존재 여부 확인(섹션 2 참고).

---

## 6. 작업 체크리스트
- [ ] `ePopupState`에 `SkillInfo = 1` 추가 (enum 재정렬)
- [ ] `Popup_Character.cs` 필드/프리젠터/SetState/OnExpansionStateSelected/RefreshAll 수정
- [ ] `PopupCharacterView._popupStateOrder` 및 메뉴 텍스트 추가
- [ ] `PopupSkillInfoView.cs` 신규 작성
- [ ] Talk.json 신규 text id 2종
- [ ] Unity 프리팹: SkillInfoView GameObject + SerializeField 연결
- [ ] 패시브 아이콘 아틀라스 확인
