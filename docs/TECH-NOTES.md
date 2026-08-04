# 기술 노트 — 문제, 고민, 그리고 선택

> 개발하면서 실제로 막혔던 지점과, 그것을 어떻게 판단하고 해결했는지에 대한 기록입니다.
> 각 항목은 **실제 코드와 커밋 이력에 근거**합니다.
>
> 시스템의 최종 형태는 [`ARCHITECTURE.md`](ARCHITECTURE.md) 를 참고해 주세요.

## 목차

**전투 설계**
1. [방어 타입을 7단계까지 나눈 이유](#1-방어-타입을-7단계까지-나눈-이유)
2. [실드가 겹칠 때, 무엇부터 깎을 것인가](#2-실드가-겹칠-때-무엇부터-깎을-것인가)
3. [상태 폭발을 막기 위한 3축 분리](#3-상태-폭발을-막기-위한-3축-분리)

**버그와 그 원인**

4. [부활 후 미사일 — 재현이 안 되던 고질적 버그](#4-부활-후-미사일--재현이-안-되던-고질적-버그)
5. [새 게임인데 이전 유물이 남아 있다](#5-새-게임인데-이전-유물이-남아-있다)
6. [함정에 두 번 맞는다 — 무적 관통 공격의 사각](#6-함정에-두-번-맞는다--무적-관통-공격의-사각)
7. [죽은 몬스터가 계속 움직인다](#7-죽은-몬스터가-계속-움직인다)

**규모에 대한 대응**

8. [룸을 손으로 배치할 수 없게 되었을 때](#8-룸을-손으로-배치할-수-없게-되었을-때)
9. [JsonUtility의 한계와 세미콜론 규약](#9-jsonutility의-한계와-세미콜론-규약)
10. [8개 언어 — 로직에서 언어를 몰아내기](#10-8개-언어--로직에서-언어를-몰아내기)
11. [코루틴을 버리고 UniTask로](#11-코루틴을-버리고-unitask로)

**구조 개선 — 측정하고 뜯기**

12. [God Object를 어디부터 뜯을지 — 감이 아니라 측정으로](#12-god-object를-어디부터-뜯을지--감이-아니라-측정으로)
13. [520배 빨라진 이유는 알고리즘이 아니었다](#13-520배-빨라진-이유는-알고리즘이-아니었다)
14. [Dictionary로 바꿨더니 데이터 버그가 나왔다](#14-dictionary로-바꿨더니-데이터-버그가-나왔다)
15. [관리자를 하나 더 만들 뻔했다](#15-관리자를-하나-더-만들-뻔했다)

**외부 리뷰 이후**

16. [소수점이 쉼표인 나라에서만 터지는 버그](#16-소수점이-쉼표인-나라에서만-터지는-버그)
17. [팝업 위에 팝업이 뜨면 시간이 풀린다](#17-팝업-위에-팝업이-뜨면-시간이-풀린다)
18. [4,444줄에서 무엇을 뽑고 무엇을 남길 것인가](#18-4444줄에서-무엇을-뽑고-무엇을-남길-것인가)
19. [고치지 않기로 한 것 — 연출 1,036줄](#19-고치지-않기로-한-것--연출-1036줄)
20. [일괄 치환이 세 번 문맥을 놓친 이야기](#20-일괄-치환이-세-번-문맥을-놓친-이야기)

**남은 것**

21. [아직 풀지 못한 문제들](#21-아직-풀지-못한-문제들)

---

## 1. 방어 타입을 7단계까지 나눈 이유

### 문제

액션 게임에서 "때렸는데 안 밀린다" 혹은 "보스가 내 공격에 계속 경직된다"는
곧바로 손맛의 문제가 됩니다.

초기에는 `bool isSuperArmor` 하나로 처리했습니다.
하지만 컨텐츠가 늘면서 요구사항이 쪼개지기 시작했습니다.

- 보스는 잡몹 공격엔 안 밀려야 하는데, 플레이어 궁극기엔 밀려야 한다
- 돌진 중인 몬스터는 경직은 안 되지만 넉백은 되어야 한다
- 어떤 스킬은 시전 중 **아무것에도** 반응하지 않아야 한다
- 상태이상으로 방어 타입이 바뀌면 안 되는 구간이 있다

`bool` 하나로는 이 네 가지가 표현되지 않았습니다.

### 고민

두 가지 선택지가 있었습니다.

| 방식 | 장점 | 단점 |
|---|---|---|
| **플래그 조합** (`ignoreStagger`, `ignoreKnockback`, …) | 유연함 | 조합이 무한대라 밸런싱 시 무엇이 켜져 있는지 추적 불가 |
| **등급 enum** | 데이터 시트에서 한 눈에 읽힘, 조합 실수 없음 | 새 요구사항마다 등급 추가 필요 |

**밸런싱을 JSON에서 하는 구조**였기 때문에 후자를 택했습니다.
플래그 조합은 코드에선 우아하지만, 시트에서 `ignoreStagger: true, ignoreKnockback: false, …` 를
몬스터 21종 × 패턴마다 채우는 건 현실적으로 관리가 안 됩니다.

### 결과

```csharp
public enum EBodyType
{
    Normal,       // 모든 타격에 경직
    SuperArmor,   // 경직 무시, 넉백은 적용
    HeavyArmor,   // 일정 강도 이상에만 반응
    StrongArmor,
    HyperArmor,   // 완전 무경직
    UnChange,     // 외부 요인으로 방어 타입 변경 불가
    Counter,      // 피격 시 반격으로 전환
}
```

시트에는 `"skillArmor": "SuperArmor"` 한 칸만 채우면 됩니다.

`UnChange` 는 특히 나중에 추가된 등급인데,
"방어 타입을 바꾸는 효과"와 "바뀌면 안 되는 구간"이 충돌하면서 필요해졌습니다.
등급 체계였기에 **기존 데이터를 건드리지 않고 하나만 추가**하면 됐습니다.

### 저스트 카운터

`Counter` 는 단순 반격이 아니라 **0.15초의 저스트 프레임 판정**을 가집니다.

```csharp
private async UniTask<bool> SwordCounter()
{
    var skillId = ConstValues.BerserkerSwordCounter;
    bool vibratingSteel = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.VibratingSteel);
    bool bullCharge     = GameManager.Instance.IsHaveAttribute(skillId, ConstValues.BullCharge);

    float delay1   = 0.2f;
    float delay2   = 0.5f;
    float delay3   = 0.2f;
    float justTime = 0.15f;    // 저스트 판정 창

    if (landingState == ELandingState.Ground)
        myRigidbody.linearVelocity = Vector2.zero;

    StateSetting(ENormalState.Skill, ConstValues.BerserkerSwordCounter, …);
    BodyTypeSetting(EBodyType.Counter);
    PlaySound(ConstValues.BerserkerSwordCounterGuard);

    float counterTime = 0.5f;
    // …
}
```

`justTime` 안에 맞으면 반격, 그 외 `counterTime` 구간은 일반 가드입니다.
**"성공하면 크게 보상, 실패해도 최소한의 보호"** 라는 설계 의도를 수치 두 개로 표현했습니다.

이 값은 한 번에 나온 게 아닙니다. 몬스터 반격 스킬은
`2026-02-24 황소 반격 추가` 이후 약 5개월 뒤인
`2026-07-22 황소반격: 후딜 없애기` 에서 후딜을 제거했습니다.
**반격 성공 후 경직이 남아 반격의 보상감이 죽는 문제**였습니다.

### 배운 것

밸런싱 데이터를 누가 만지느냐가 자료구조를 결정합니다.
코드 안에서만 쓰이면 플래그가 낫지만, **시트로 나가는 순간 등급이 낫습니다.**

---

## 2. 실드가 겹칠 때, 무엇부터 깎을 것인가

### 문제

성장 요소가 누적되는 구조라 실드를 주는 소스가 여럿입니다.
스킬 실드, 유물 실드, 특성이 부여하는 실드가 **동시에 걸립니다.**

처음엔 리스트 앞에서부터 깎았습니다. 그런데 이런 상황이 생겼습니다.

> 3초 뒤 사라지는 실드 50 + 지속시간 무한 실드 30 을 들고 있다.
> 데미지 40을 맞았는데 **무한 실드가 먼저 깎였다.**
> 3초 뒤, 남은 50짜리 실드는 그대로 증발했다.

플레이어 입장에서는 손해를 봤는데 이유를 알 수 없습니다.
버그처럼 보이지 않고 **"이 게임 실드는 쓸모없다"** 로 인식되는 게 더 문제였습니다.

### 고민

정렬 기준 후보:

1. **먼저 걸린 순** — 직관적이지만 위 문제를 못 막음
2. **잔여 시간 짧은 순** — 낭비를 막지만, 설계상 "먼저 소비되어야 할" 실드를 제어할 수 없음
3. **명시적 우선순위** — 제어는 되지만 모든 실드에 값을 정해줘야 함

결론은 **3 + 2의 조합**이었습니다.
기획적으로 순서를 강제해야 할 때는 `priority` 로 누르고,
동순위끼리는 **곧 사라질 것부터** 쓰게 합니다.

### 결과

```csharp
[Serializable]
public class Shield
{
    public string sourceId;   // 출처(스킬/유물 id) — 식별·디버깅용
    public int amount;
    public float duration;    // 0 이하 = 무한
    public float currentTime;
    public int priority;      // 작을수록 먼저 소비
    public Action endAction;
}
```

```csharp
public int ConsumeShield(int damage)
{
    if (damage <= 0 || shieldList.Count == 0)
        return damage;

    shieldList.Sort((a, b) =>
    {
        int p = a.priority.CompareTo(b.priority);
        if (p != 0)
            return p;

        // 무한(duration <= 0) 실드는 가장 나중에 소비
        float at = a.duration > 0 ? a.currentTime : float.MaxValue;
        float bt = b.duration > 0 ? b.currentTime : float.MaxValue;
        return at.CompareTo(bt);
    });

    for (int i = 0; i < shieldList.Count && damage > 0; i++)
    {
        // 앞에서부터 소진하고, 남은 데미지를 반환
    }
}
```

**무한 실드를 `float.MaxValue` 로 치환**해 정렬에 태운 게 핵심입니다.
`if (duration <= 0)` 예외 분기를 따로 두지 않고 정렬식 하나로 흡수했습니다.

`sourceId` 는 게임 로직에 쓰이지 않습니다.
**어떤 실드가 왜 남아 있는지 디버깅할 때만** 필요해서 넣은 필드입니다.
실드가 여러 개 겹치면 로그만으로는 출처를 못 찾습니다.

### 배운 것

이런 종류의 버그는 **크래시도 아니고 경고도 안 뜹니다.**
플레이해 보고 "손해 본 것 같은데?" 하고 느껴야만 발견됩니다.
자기 게임을 계속 플레이하는 게 디버깅의 일부라는 걸 알게 됐습니다.

---

## 3. 상태 폭발을 막기 위한 3축 분리

### 문제

캐릭터 상태를 enum 하나로 관리하다가 조합이 터졌습니다.

`Idle` / `Move` / `Jump` 만 있어도,
"공중에서 이동 중" / "지상에서 정지 중" / "공중 정지" 를 표현하려면
`JumpMove`, `JumpIdle`, `GroundIdle` … 이 필요합니다.
여기에 `Attack`, `Skill`, `Dash` 가 곱해지면 감당이 안 됩니다.

### 해결

**서로 독립적인 축을 분리**했습니다.

```csharp
public enum ENormalState   // 무엇을 하고 있는가 (21종)
{
    Normal, Idle, Move, Jump, Landing, Leap,
    Attack, JumpAttack, Dash, Skill, Potion,
    Grabbed, Airborne, Down, Stun, Damaged,
    Appear, AppearEnd, Die, Stagger, Frozen
}

public enum EMoveState     // 실제로 움직이는가
{
    Stopping, Moving
}

public enum ELandingState  // 땅에 붙어 있는가
{
    Ground, Air
}
```

"공중에서 이동하며 스킬 시전"은 `Skill` × `Moving` × `Air` 로 표현됩니다.
단일 enum이었다면 **21 × 2 × 2 = 84개**의 상태가 필요했을 것입니다.

이 분리 덕분에 조건 판정이 읽기 쉬워집니다.

```csharp
if (landingState == ELandingState.Ground)
    myRigidbody.linearVelocity = Vector2.zero;
```

`Animations.json` 도 이 축을 그대로 사용합니다.
애니메이션별로 `canMove`(이동 가능 여부), `canFlip`(반전 가능 여부), `moveRatio`(이동 감쇠)를
데이터로 지정해, **모션마다 이동 제약을 코드가 아닌 시트에서** 정합니다.

### 배운 것

상태가 늘어날 때 enum에 항목을 추가하기 전에,
**"이게 정말 같은 축의 값인가"** 를 먼저 묻게 됐습니다.
다른 축이면 곱하지 말고 나눠야 합니다.

---

## 4. 부활 후 미사일 — 재현이 안 되던 고질적 버그

> 커밋: `2025-11-29 부활 후 미사일 고질적 버그 수정`
> (다음 날 `2025-11-30 부활 후 몬스터 미사일 버그 수정` 으로 잔여 케이스를 마저 정리했습니다)

### 증상

플레이어가 죽고 부활한 뒤, 몬스터가 쏜 미사일이 **이상하게 동작**했습니다.
폭발 연출이 안 나오거나, 엉뚱한 시점에 터지거나, 이미 사라진 대상을 참조했습니다.

재현이 불안정했습니다. 부활을 몇 번 해야 나타나고, 어떤 몬스터에선 안 나타났습니다.
그래서 오래 방치됐고, 커밋 메시지에 **"고질적"** 이라고 쓰게 됐습니다.

### 원인

오브젝트 풀과 초기화 코드의 조합이었습니다.

```csharp
// 수정 전
public void SetInfo(MissileData missileData, Vector2 missileDir, Action action)
{
    if (/* 최초 생성이거나 데이터가 바뀐 경우 */)
    {
        missileInfo.spawnObject     = missileData.spawnObject;
        missileInfo.hitSpawn        = missileData.hitSpawn;
        missileInfo.afterImage      = missileData.afterImage;
        missileInfo.explosionAction = action;      // ← if 블록 안에 있었다
    }

    dir = missileDir;
    SetLimit();
}
```

정적 데이터(`spawnObject`, `hitSpawn`, `afterImage`)는
같은 미사일이면 값이 같으니 **한 번만 세팅하면 되는 게 맞습니다.**
그래서 `if` 블록으로 묶어 불필요한 대입을 줄였습니다.

문제는 `explosionAction` 이 **정적 데이터가 아니라 콜백**이라는 점이었습니다.
발사할 때마다 달라지는 값인데, 최적화한다고 같은 블록에 넣어버린 것입니다.

그 결과:

1. 몬스터 A가 미사일을 쏨 → `explosionAction` 에 A의 콜백이 들어감
2. 플레이어 사망 → 미사일이 풀로 반환됨
3. 부활 후 몬스터 B가 **같은 미사일 인스턴스를 재사용**
4. `if` 블록이 실행되지 않아 **A의 콜백이 그대로 남아 있음**

즉, 죽은 상황의 콜백을 물고 다니는 미사일이었습니다.

재현이 불안정했던 이유도 여기서 나옵니다.
**풀에서 어떤 인스턴스가 재활용되느냐에 따라** 증상이 달랐던 것입니다.

### 해결

```csharp
// 수정 후
public void SetInfo(MissileData missileData, Vector2 missileDir, Action action)
{
    if (/* … */)
    {
        missileInfo.spawnObject = missileData.spawnObject;
        missileInfo.hitSpawn    = missileData.hitSpawn;
        missileInfo.afterImage  = missileData.afterImage;
    }
    missileInfo.explosionAction = action;    // ← 블록 밖으로 이동. 항상 갱신

    dir = missileDir;
    SetLimit();
}
```

**한 줄을 블록 밖으로 옮긴 것**이 전부입니다.

### 배운 것

이 버그 이후로 풀링 대상 오브젝트를 볼 때 기준이 생겼습니다.

> **재사용 시 반드시 갱신해야 하는 값과, 한 번만 세팅해도 되는 값을 명확히 구분한다.**
> 콜백·타겟 참조·소유자는 전자다.

관련해서 파티클 잔상 문제도 같은 계열이라, 풀 반환 시 초기화를 명시적으로 처리합니다.

```csharp
go.SetActive(true);
ResetParticles(go);      // 이전 재생 상태가 남지 않도록
```

풀링은 "`Instantiate` 를 줄인다"가 전부가 아니라,
**"살아 있는 상태를 물려받는다"는 리스크를 관리하는 일**이라는 걸 알게 됐습니다.

---

## 5. 새 게임인데 이전 유물이 남아 있다

> 커밋: `2026-07-10 새로운 파일로 시작하면 유물들이 그대로 남아있는 버그 수정`

### 증상

세이브 파일 A로 플레이하다가 타이틀로 나간 뒤,
**새 게임**을 시작하면 A에서 먹은 유물이 그대로 붙어 있었습니다.

### 원인

새 게임 시작 시 `DefaultDataSetting()` 으로 데이터를 초기화하고 있었습니다.

```csharp
private void FirstStart()
{
    DefaultDataSetting();        // ← 필드별로 하나씩 초기화
    DefaultSkillSetting();
    DefaultRelicSetting();
    DefaultMapSetting();
    // …
}
```

문제는 이게 **"필드를 하나씩 되돌리는" 방식**이라는 점입니다.
`saveData` 에 필드를 새로 추가할 때마다 `DefaultDataSetting()` 에도
초기화 코드를 같이 넣어줘야 합니다.

유물 관련 필드가 늘어나는 과정에서 **초기화 목록에 빠진 필드**가 생겼고,
그 값이 이전 세션의 메모리에 그대로 남아 새 게임으로 흘러들어갔습니다.

매니저가 `DontDestroyOnLoad` 라 씬을 바꿔도 메모리가 안 지워진다는 점이 겹쳤습니다.

### 해결

부분 초기화를 포기하고 **객체를 통째로 교체**했습니다.

```csharp
private void FirstStart()
{
    // 이전 파일의 데이터가 메모리에 남아 새 파일로 새어 들어가지 않도록 통째로 교체
    saveData = new SaveData();
    DefaultSkillSetting();
    DefaultRelicSetting();
    DefaultMapSetting();
    // …
}
```

새 인스턴스는 모든 필드가 기본값이므로,
**앞으로 필드를 추가해도 초기화를 빠뜨릴 수 없습니다.**

### 배운 것

**"빠뜨릴 수 있는 구조"를 "빠뜨릴 수 없는 구조"로 바꾸는 게 개별 버그를 잡는 것보다 낫습니다.**
초기화 목록을 꼼꼼히 채우는 건 사람의 주의력에 의존하지만,
`new` 한 번은 컴파일러가 보장합니다.

이 판단은 [ARCHITECTURE — 원본 불변 · 복제본 가변 원칙](ARCHITECTURE.md#원본-불변--복제본-가변-원칙)과
같은 문제의식에서 나왔습니다. 세이브 파일 단위로 성장 상태가 갈리는 구조에서,
**데이터의 수명 경계를 명확히 긋는 일**이 반복적으로 중요했습니다.

---

## 6. 함정에 두 번 맞는다 — 무적 관통 공격의 사각

> 커밋: `2026-07-10` (위와 동일 커밋에 포함)

### 문제

낙사 함정에 빠지면 플레이어가 데미지를 입고 안전한 위치로 리스폰됩니다.
그런데 리스폰 연출 도중에 **함정 판정에 한 번 더 걸리는** 경우가 있었습니다.

일반적으로는 피격 후 무적 시간이 있어 막힙니다.
문제는 함정이 `ignoreImmortal`(무적 관통) 속성을 가지고 있었다는 점입니다.

이 속성은 원래 **"무적 시간으로 함정을 무시하고 지나가는 꼼수"를 막으려고** 넣은 것이었습니다.
그런데 같은 속성이 리스폰 연출 중인 플레이어까지 때리고 있었습니다.

의도한 방어책이 다른 곳에서 구멍이 된 사례입니다.

### 해결

무적 판정과는 **별개의 상태**로 분리했습니다.

```csharp
// 피격이 가능한 상태인지(무적/사망 필터)
private bool IsHittable(Character hitTarget)
{
    // 함정 리스폰 연출 중인 플레이어는 ignoreImmortal 공격(함정 포함)도 통하지 않는다 — 함정 2회 피격 방지
    if (hitTarget is Player { TrapRespawning: true })
        return false;

    if ((hitTarget.Immortal || hitTarget.Dodge) && !attackInfo.ignoreImmortal)
        return false;

    if (hitTarget.IsDie)
        return false;

    // …
}
```

핵심은 `TrapRespawning` 검사를 **`ignoreImmortal` 검사보다 위에** 둔 것입니다.
"무적을 관통하는 공격"이라도 이 상태는 뚫지 못합니다.

### 배운 것

무적을 `bool` 하나로 두면, "이 무적은 뚫려도 되는가"를 표현할 수 없습니다.
`Immortal` / `Dodge` / `TrapRespawning` 은 전부 "안 맞는 상태"지만
**관통 규칙이 서로 다릅니다.**

이건 [1번 방어 타입 이야기](#1-방어-타입을-7단계까지-나눈-이유)와 정확히 같은 교훈입니다.
`bool` 로 시작한 개념이 요구사항을 만나면 등급이나 축으로 쪼개집니다.

---

## 7. 죽은 몬스터가 계속 움직인다

> 커밋: `2025-06-14 … 포도몬스터가 계속 달리는 모션으로 있는 현상 수정`

### 문제

원거리 몬스터는 플레이어와의 거리에 따라 대기 모션을 바꿉니다.
그런데 죽은 뒤에도 이 갱신이 계속 돌아서, **사망 모션 위에 달리기 모션이 덮어씌워졌습니다.**

### 원인

```csharp
// 수정 전
private void UpdateStandingCheck()
{
    if (!myStat.standMotion || (normalState != ENormalState.Move && normalState != ENormalState.Idle))
        return;

    if (patternInfo[0].playerInAttackRange)
        // 모션 갱신
}
```

조기 종료 조건이 `Move` / `Idle` 상태만 검사하고 있었습니다.
사망 처리가 `ENormalState.Die` 로 즉시 전환되지 않는 프레임이 존재했고,
그 틈에 모션 갱신이 들어갔습니다.

### 해결

```csharp
private void UpdateStandingCheck()
{
    if (!myStat.standMotion
        || (normalState != ENormalState.Move && normalState != ENormalState.Idle)
        || isDie)                                    // ← 추가
        return;
    // …
}
```

`normalState` 라는 **애니메이션 축**과 `isDie` 라는 **생존 축**이 별개였던 것이 원인입니다.
상태 머신을 축으로 나눈 대가로, **가드 조건도 축마다 걸어줘야 합니다.**

### 배운 것

[3번의 축 분리](#3-상태-폭발을-막기-위한-3축-분리)는 조합 폭발을 막아주지만,
공짜가 아닙니다. **한 축만 보고 판단하면 다른 축의 상태를 놓칩니다.**

이후 `Update` 계열 함수를 작성할 때 조기 종료 조건에
"이 함수가 죽은 대상에게도 돌아도 되는가"를 먼저 확인하게 됐습니다.

---

## 8. 룸을 손으로 배치할 수 없게 되었을 때

### 문제

룸이 늘어나면서(현재 59개) 에디터에서 타일을 직접 찍는 작업이 병목이 됐습니다.

한 룸에는 바닥 타일, 플랫폼, 함정, 레이저가 있고,
그 위에 몬스터와 프롭이 배치됩니다.
룸 하나를 만드는 데 걸리는 시간보다, **만든 뒤 수정하는 비용**이 더 컸습니다.

"3번째 플랫폼을 한 칸 위로" 같은 요청이 오면 마우스로 찾아 옮겨야 하고,
그 과정에서 다른 타일을 실수로 건드립니다.

### 고민

세 가지 안이 있었습니다.

1. **그냥 계속 손으로 한다** — 룸이 더 늘어나면 붕괴
2. **런타임 절차적 생성** — 수동 배치의 의도(연출·동선)를 잃음
3. **데이터 → 에디터 조립 툴** — 데이터로 관리하되, 결과물은 일반 프리팹

3번을 택했습니다.
이 게임의 룸은 **손으로 설계한 레벨**이지 랜덤 생성이 아니기 때문에,
런타임 생성으로 가면 게임의 성격 자체가 바뀝니다.
필요한 건 자동 생성이 아니라 **자동 조립**이었습니다.

### 결과 — `RoomAssemblerWindow`

```csharp
// Assets/Editor/RoomAssemblerWindow.cs
// Map assembler supporting Ground/Platforms/Traps/Lasers + Transform markers (monsters/traps/props).
// - Reads: ground[], platforms[], traps[], lasers[]  (Tile layers)
// - Reads: transforms[] -> { "name": "<prefabOrMarkerName>", "grid": {"x":int,"y":int} }
```

입력 데이터:

```jsonc
{
  "roomId": "A3",
  "cellSize":   { "x": 1.28, "y": 1.28 },
  "gridOrigin": { "x": 0,    "y": 0    },

  "ground":    [ { "grid": { "x": 0,  "y": 0 } } ],
  "platforms": [ { "grid": { "x": 5,  "y": 3 } } ],
  "traps":     [ … ],
  "lasers":    [ … ],
  "transforms":[ { "name": "Monster_Bat", "grid": { "x": 12, "y": 4 } } ]
}
```

설계 시 신경 쓴 점:

- **`cellSize` / `gridOrigin` 을 데이터로 노출** — 타일 크기(1.28)를 코드에 박지 않음
- **타일과 오브젝트를 분리** (`ground[]` vs `transforms[]`) — 타일맵과 프리팹은 배치 방식이 다름
- **격자 좌표(`grid`) 입력** — 월드 좌표로 받으면 소수점 오차로 타일이 어긋남

마지막 항목이 특히 중요했습니다.
격자 정수로 받고 툴이 월드 좌표로 변환하면, **어긋날 수가 없습니다.**

### 함께 만든 툴들

같은 이유로 반복 작업을 하나씩 툴로 옮겼습니다.

| 툴 | 옮긴 반복 작업 |
|---|---|
| `SpriteAtlasGeneratorTool` | 아틀라스 수동 구성 (누락·중복 실수) |
| `SpriteAtlasUncompressTool` | 압축 아틀라스 디버깅 시 일괄 해제 |
| `SyncPrefabInstanceNames` | 프리팹 인스턴스 이름 어긋남 → 참조 깨짐 |
| `MoveSelectedObjectsWindow` | 다수 오브젝트 정밀 이동 |
| `ScreenshotCaptureTool` | `F5` 캡처 (데모 빌드에선 자동 차단) |

`ScreenshotCaptureTool` 에 데모 차단을 넣은 이유는,
데모 빌드에서 미공개 컨텐츠가 스크린샷으로 유출되는 걸 막기 위해서입니다.

```csharp
if (GameManager.Instance.isDemo)
{
    Debug.Log($"데모버전은 스크린샷이 찍히지 않음");
    return;
}
```

### 배운 것

**같은 작업을 세 번 이상 하면 툴을 만든다**는 기준이 생겼습니다.
1인 개발이라 툴 제작 시간이 온전히 내 손해인데도,
59개 룸 시점에서 돌아보면 `RoomAssemblerWindow` 없이는 불가능했습니다.

에디터 툴은 게임에 들어가는 코드가 아니라서 우선순위가 밀리기 쉽지만,
**개발 속도를 결정하는 건 결국 이쪽**이었습니다.

---

## 9. JsonUtility의 한계와 세미콜론 규약

### 문제

Unity의 `JsonUtility` 는 가볍고 빠르지만 제약이 많습니다.

- 최상위 배열 미지원 (래퍼 클래스 필요)
- `Dictionary` 미지원
- 중첩 배열 표현이 제한적
- `null` 과 기본값 구분 불가

외부 JSON 라이브러리를 넣을 수도 있었지만,
**의존성을 늘리는 것보다 제약 안에서 규약을 만드는 쪽**을 택했습니다.
데이터 구조 자체가 복잡하지 않았기 때문입니다.

### 규약 1 — 래퍼 클래스

```csharp
[Serializable]
public class AttackData      // 항목
{
    public string id;
    public string effectType;
    // …
}

[Serializable]
public class AttackDataList  // 래퍼
{
    public List<AttackData> Attack;
}
```

JSON 최상위 키(`"Attack"`)와 래퍼 필드명을 일치시켜 규칙을 통일했습니다.
20종 테이블이 전부 같은 패턴이라, 새 테이블 추가 시 헷갈릴 여지가 없습니다.

### 규약 2 — 세미콜론 다중 값

중첩 배열 대신 **`;` 구분 문자열**을 씁니다.

```jsonc
{ "id": "PotionDrink", "coolTime": "0.1;0;0" }    // 3직업 각각의 쿨타임
{ "passiveId": "SuperArmor;ArmorBreak" }           // 다중 패시브
```

```csharp
var passiveIdSplit = skillAttribute.passiveId.Split(';');
foreach (var passiveId in passiveIdSplit)
    data.passiveId.Add(passiveId);
```

### 규약 3 — 문자열 시트, 타입 런타임

시트에서는 문자열로 두고, 로드 시점에 런타임 타입으로 변환합니다.

```csharp
// Attack.json 에서 읽히는 형태
public class AttackData
{
    public string effectType;        // "Damaged" / "Airborne"
    public string upperPower;        // "1.5;3.0"
    public string deBuff;
    // …
}

// 게임 로직이 쓰는 형태
public class AttackInfo
{
    public EEffectType effectType;   // enum
    public Vector2 upperPower;       // Vector2
    public List<DeBuffInfo> deBuffInfoList;
    // …
}
```

**시트는 편집 편의를, 런타임은 타입 안정성을** 갖습니다.
오타는 변환 시점에 한 번만 걸리면 되므로, 게임 로직 전체가 문자열 비교로 오염되지 않습니다.

### 배운 것

제약이 있는 도구를 만났을 때 **바로 교체하는 것이 항상 정답은 아닙니다.**
`JsonUtility` 로도 20종 테이블을 문제없이 굴렸고,
대신 규약 문서화(이 항목)가 필요해졌습니다.

다만 팀 작업이었다면 판단이 달랐을 것 같습니다.
`;` 규약은 **아는 사람에게만 자명하기 때문**입니다.
혼자라 통했던 선택이라는 자각은 있습니다.

---

## 10. 8개 언어 — 로직에서 언어를 몰아내기

### 문제

Steam 출시를 목표로 하면서 다국어 지원이 필요해졌습니다.
현재 한국어·영어·일본어·중국어(간체/번체)·스페인어·러시아어·포르투갈어 **8개**입니다.

기존 코드에는 문자열이 여기저기 박혀 있었습니다.

```csharp
string dialog9  = "안 보면 못 살려 ㅋ";
string dialog12 = "오잉? 나 살아났네?";
```

이 상태로 언어를 추가하면 **번역할 문자열을 코드에서 찾아 헤매야** 합니다.

### 해결 — 모든 텍스트를 `idx` 로

```jsonc
// Talk.json
{
  "idx": 10000,
  "kr": "망할 모험",
  "en": "Damn Adventure",
  "ja": "クソったれな冒険",
  "cn": "该死的冒险",
  "tw": "該死的冒險",
  "es": "Una Maldita Aventura",
  "ru": "Чёртово приключение",
  "pt": "Uma Maldita Aventura"
}
```

다른 테이블은 **문자열이 아니라 `idx` 만** 들고 있습니다.

```jsonc
// Skill.json — 스킬 이름/설명을 직접 갖지 않는다
{ "id": "PotionDrink", "talk": 60002, "explainTalk": 70002 }

// SkillAttribute.json
{ "id": "SwordBeam", "talk": 80000, "explainTalk": 90000 }
```

`idx` 대역을 용도별로 나눠 관리합니다.

| 대역 | 용도 |
|---|---|
| `10000~` | 에피소드 · 타이틀 |
| `60000~` | 스킬 이름 |
| `70000~` | 스킬 설명 |
| `80000~` | 특성 이름 |
| `90000~` | 특성 설명 |

이 구조의 이점은 **게임 로직이 언어의 존재를 모른다**는 것입니다.
언어 추가는 `Talk.json` 에 컬럼 하나를 늘리는 작업으로 끝납니다.

실제로 스페인어와 일본어는 나중에 점검·추가됐는데,
(`2026-07-23 텍스트 언어 통일 및 스페인어 테스트 완료`, `2026-07-24 일본어 점검`)
**C# 코드는 건드리지 않았습니다.**

### 남은 문제

`SkillAttribute.json` 에 `kExplain`(한국어 설명) 필드가 아직 남아 있습니다.

```jsonc
{ "id": "SwiftSlash", "kExplain": "돌진베기 및 슈퍼아머", "talk": 80000, … }
```

작업용 메모라 게임에는 표시되지 않지만, `talk` 과 내용이 어긋날 위험이 있습니다.
**같은 정보가 두 곳에 있는 상태**라 정리 대상입니다.

연출 스크립트에도 하드코딩 문자열이 일부 남아 있어 순차적으로 `idx` 로 옮기는 중입니다.
(가장 많이 남아 있던 `Stage` 계열은 2챕터 컨셉 변경으로 제거되면서 함께 정리됐습니다)

### 폰트 문제

언어를 늘리면서 예상 못 한 비용이 폰트였습니다.
CJK와 키릴 문자를 한 폰트로 감당할 수 없어, `TextFont` 컴포넌트가 언어별 폰트 교체를 담당합니다.

TMP SDF 아틀라스는 문자 수에 비례해 커지므로,
중국어를 넣는 순간 폰트 에셋 용량이 급증합니다.
이 부분은 아직 최적화 여지가 있습니다.

### 배운 것

로컬라이징은 **나중에 붙이기 가장 비싼 기능** 중 하나입니다.
`idx` 참조 구조로 옮긴 뒤에는 언어 추가가 거의 무료가 됐지만,
그 전에 박아둔 문자열을 걷어내는 작업이 대부분의 시간을 먹었습니다.

---

## 11. 코루틴을 버리고 UniTask로

### 문제

액션 게임의 스킬은 대부분 **시간축 위의 시퀀스**입니다.

```
선딜 0.2초 → 판정 활성 → 후딜 0.5초 → 상태 복귀
```

코루틴으로 짜면 두 가지가 걸립니다.

1. **반환값이 없다** — 스킬이 중간에 끊겼는지 호출부가 알 수 없음
2. **취소가 번거롭다** — `StopCoroutine` 은 어디까지 진행됐는지 모른 채 끊음

특히 1번이 문제였습니다.
캐릭터 교체가 핵심 메커닉이라 **스킬 도중 캐릭터가 바뀌는 상황**이 일상적인데,
스킬이 정상 종료했는지 중단됐는지에 따라 후처리가 달라야 합니다.

> 관련 버그: `2025-06-14 거너로 죽을때, 광전사캐릭터인데 거너스킬이 나오는 현상 수정`
> — 캐릭터 전환 시점과 스킬 종료 시점이 어긋나며 생긴 문제였습니다.

### 해결

모든 스킬을 `UniTask<bool>` 로 통일했습니다.

```csharp
private async UniTask<bool> SwordCounter()
private async UniTask<bool> LightningKick()
private async UniTask<bool> ElementalInfusion()
```

`bool` 반환값이 **"끝까지 수행됐는가"** 를 알려줍니다.
카운터 실패, 자원 부족, 상태이상 중단이 모두 이 값으로 표현됩니다.

취소 토큰은 헬퍼로 통일했습니다.

```csharp
protected async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
{
    await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
}
```

지연 대기를 이 함수로만 하면 **토큰을 빠뜨릴 수 없습니다.**
[5번의 교훈](#5-새-게임인데-이전-유물이-남아-있다)과 같은 접근입니다 —
주의력에 기대지 말고 구조로 막습니다.

취소 여부 확인은 `SuppressCancellationThrow()` 로 처리합니다.

```csharp
if (await NormalDelay(dialogDelay2, dialogCancellation).SuppressCancellationThrow())
    return;    // 취소됐으면 조용히 빠져나감
```

예외를 던지지 않고 `bool` 로 받으므로, 연출 취소가 `try-catch` 없이 깔끔합니다.

### `Forget()` 으로 의도 표현

```csharp
popupPause.FadeOpen(true, true, 0.2f, false).Forget();   // 안 기다림 (의도적)
await popupPause.FadeClose(true, true, 0.2f, true);      // 기다림
```

`Forget()` 을 명시하면 **"await를 깜빡한 것"과 "일부러 안 기다린 것"이 코드에서 구분**됩니다.
비동기 코드가 수십 개 파일로 늘어난 시점에서 이 구분이 큰 도움이 됐습니다.

### 현재 적용 범위

| 지표 | 수치 |
|---|---|
| UniTask 사용 파일 | 58개 |
| `CancellationToken` 전파 파일 | 46개 |

### 배운 것

코루틴 대비 UniTask의 진짜 이점은 성능(GC 할당)보다
**반환값과 취소 전파**였습니다.

성능 때문에 도입했지만, 실제로 코드를 바꾼 건 `UniTask<bool>` 의 `bool` 하나였습니다.

---

## 12. God Object를 어디부터 뜯을지 — 감이 아니라 측정으로

### 문제

`GameManager.cs` 가 3,840줄이었습니다. 나눠야 한다는 건 알았지만 **어디부터**가 문제였습니다.

흔히 하는 방식은 "이름으로 봐서 관련 있어 보이는 것끼리" 묶는 것입니다.
그런데 그렇게 뜯다가 중간에 다른 필드에 걸려 되돌린 적이 있었습니다.
**이름이 비슷한 것과 실제로 독립적인 것은 다릅니다.**

### 접근

두 단계로 나눴습니다.

**1단계 — 타입 분리 (위험 0)**

파일을 열어보니 3,840줄 중 **앞 641줄이 GameManager가 아니었습니다.**
`SaveSystem`, `KeyBinding`, 데이터 클래스 26개 등 **32개 타입**이 한 파일에 있었습니다.

C#은 파일 위치와 타입이 무관하므로 **옮기기만 하면 끝**입니다. 코드 변경 0줄.

```
GameManager.cs  3,840줄 → 3,213줄
Core/Save/, Core/Data/, UI/Common/ 으로 9개 파일 분리
```

**2단계 — `partial` 로 갈라보고 의존도 측정**

여기서 `partial class` 를 썼습니다. 다만 **목적이 정리가 아니라 측정**이었습니다.

`partial` 은 파일만 나눌 뿐 클래스는 하나라, **결합도를 전혀 낮추지 못합니다.**
필드는 여전히 전부 공유됩니다. 그걸 알면서도 쓴 이유는,
메서드를 그룹으로 묶어두면 **"각 그룹이 어떤 필드를 쓰는가"를 셀 수 있기** 때문입니다.

176개 메서드를 9개 그룹으로 나누고, 그룹별 필드 접근을 측정했습니다.

### 측정 결과

필드 86개 기준입니다.

| 공유 범위 | 필드 수 | 대표 |
|---|---|---|
| 6개 그룹이 공유 | 1 | **`saveData`** |
| 4개 그룹 | 1 | `curPlayer` |
| 3개 그룹 | 6 | `objectList`, `players`, `uiInterface` |
| 2개 그룹 | 15 | |
| **1개 그룹 전용** | **52 (60%)** | |

**60%가 한 그룹만 씁니다.** 생각보다 결합이 심하지 않았습니다.
그리고 결합의 중심이 `saveData` 하나라는 게 드러났습니다.

그룹별 독점 필드 비율로 추출 난이도가 나왔습니다.

| 그룹 | 독점/사용 | 판정 |
|---|---|---|
| Text | 0 / **1** | 사실상 무상태 |
| **Pool** | 10 / 14 (71%) | **`saveData`·`curPlayer` 를 전혀 안 씀** |
| Progression | 5 / 17 (29%) | `saveData` 의존 깊음 |
| **Player** | 1 / 8 (12%) | `curPlayer` 를 변경하는 주체 |

### 이 데이터가 순서를 정해줬습니다

- **Text** 는 `language` 필드 하나만 씁니다. 가장 쉬운데, 감으로는 몰랐습니다.
- **Pool** 은 공유 필드를 아예 안 씁니다. 들고 나가기만 하면 됩니다.
- **Player** 는 필드 수는 적지만 전부 공유 필드입니다. **여기부터 시작했으면 막혔을 겁니다.**

`Text` → `Pool` 순으로 추출했고, 둘 다 중간에 막히지 않았습니다.

### 배운 것

**`partial` 은 해결책이 아니라 측정 도구로 쓸 때 값어치가 있었습니다.**

파일이 작아 보이는 건 착시입니다. 클래스가 하나면 God Object 그대로입니다.
다만 **뜯기 전에 어디가 얼마나 얽혀 있는지 재보는 용도**로는 위험이 0이면서 효과적이었습니다.

---

## 13. 520배 빨라진 이유는 알고리즘이 아니었다

### 발단

`ObjectPoolService` 를 추출하면서 조회 방식을 바꿨습니다.

```csharp
// 이전
var objectName = $"{id}(Clone)";
var isSearch = objectList.FindAll(x => x.name == objectName);
var recycle = isSearch.Find(x => !x.activeSelf);

// 이후
instancesById.TryGetValue(id, out var list);   // 해당 id 의 인스턴스만
```

"선형 탐색을 Dictionary로 바꿨다"고 쓰려다가, **정말 얼마나 차이 나는지 모른다**는 걸 깨달았습니다.
그래서 조회 로직만 격리한 벤치마크를 만들었습니다. `Instantiate` 비용은 양쪽 동일하니 제외했습니다.

### 결과

프리팹 200종 / 인스턴스 3,000개 / 조회 20,000회.

| | 시간 | 총 할당 | 호출당 할당 |
|---|---|---|---|
| 이전 | 36,122 ms | 69.6 MB | 3,565 B |
| 이후 | 69 ms | ~0 | **0 B** |

520배였습니다. **선형 탐색만으로는 이 숫자가 안 나옵니다.**

### 진짜 원인

`GameObject.name` 이었습니다.

```csharp
objectList.FindAll(x => x.name == objectName)
//                       ^^^^^
```

`.name` 은 평범한 C# 프로퍼티가 아니라 **C++ 엔진에서 문자열을 가져오는 네이티브 호출**이고,
**접근할 때마다 새 string 을 할당**합니다.

즉 이 한 줄이 인스턴스 수만큼 `네이티브 interop + 문자열 할당 + 비교` 를 반복합니다.
20,000회 조회면 6천만 번입니다. 시간과 할당이 동시에 터진 이유가 이겁니다.

**문제는 자료구조가 아니라 "이름으로 오브젝트를 식별한 것" 자체**였습니다.
`(Clone)` 접미사에 의존하고 있어서, 프리팹 이름을 바꾸면 컴파일 에러 없이 조용히 깨지는 문제도 같이 있었습니다.

### 이 수치로 말할 수 있는 것과 없는 것

정직하게 적어둡니다.

- **말할 수 있는 것** — 조회 경로의 힙 할당이 0이 되었다. 이건 게임 규모와 무관합니다.
- **말할 수 없는 것** — "게임이 빨라졌다". 이 게임은 인스턴스가 3,000개까지 가지 않으므로
  시간 차이는 체감되지 않습니다. 프로파일러로 실제 플레이를 재지도 않았습니다.

GC 최적화는 평균 FPS를 올리는 작업이 아니라 **가끔 튀는 것을 없애는** 작업입니다.
그 구분 없이 "최적화했다"고 쓰면 과장이 됩니다.

### 배운 것

느린 코드를 보면 알고리즘부터 의심했는데, **엔진 API의 숨은 비용이 더 컸습니다.**
Unity에서 `.name`, `.tag`, `transform` 접근 같은 것들이 여기 해당합니다.

수치보다 **원인을 짚을 수 있는 것**이 더 중요하다는 것도 알게 됐습니다.
520배는 조건을 바꾸면 달라지지만, "`.name` 이 네이티브 할당을 한다"는 사실은 변하지 않습니다.

---

## 14. Dictionary로 바꿨더니 데이터 버그가 나왔다

### 같은 패턴을 전체에 적용하기

풀에서 효과를 확인한 뒤, 같은 패턴이 코드베이스 전체에 있는지 찾아봤습니다.
`.Find(x => ...)` 가 **132곳**이었습니다.

전부 바꾸려다 멈췄습니다. **바꾸면 안 되는 것이 섞여 있었습니다.**

| 분류 | 대상 크기 | 판단 |
|---|---|---|
| 정적 테이블 (`SpawnedObject` 428개 등) | 큼 | 인덱스 도입 |
| 런타임 복제본 (`skillAttributeCopyList` 48개) | 중간 | 인덱스 도입 |
| **세이브 데이터** (`playerInfoList` 등) | 3~20개 | **불가** |
| 런타임 상태 (`buffList`, `players`) | 10개 미만 | 불필요 |

세이브 데이터를 `Dictionary` 로 바꾸면 **`JsonUtility` 가 직렬화하지 못해 기존 세이브가 전부 깨집니다.**
게다가 `playerInfoList` 는 항목이 3개(캐릭터 수)라 얻을 것도 없습니다.

항목 10개 미만에서는 해시 계산 오버헤드 때문에 **선형 탐색이 더 빠릅니다.**
"전부 바꾸기"가 아니라 **크기를 보고 고르는 것**이 맞았습니다.

### 그리고 데이터 버그가 드러났습니다

인덱스를 만들자마자 경고가 떴습니다.

```
[Table] MonsterData 에 중복된 id 가 있습니다: Monster_Hand
[Table] MonsterData 에 중복된 id 가 있습니다: Monster_FireWizard
```

확인해보니 **같은 id인데 완전히 다른 몬스터**였습니다.

| id | 첫 번째 (사용 중) | 두 번째 |
|---|---|---|
| `Monster_FireWizard` | HP 100 / 공격 40 | HP **450** / 호버링 |
| `Monster_Hand` | HP 120 / 공격 40 | HP **720** |

`Find` 는 **첫 번째만 반환**합니다. 그래서 뒤쪽 몬스터 2종이
**id가 겹친 순간부터 한 번도 게임에 나오지 않고 있었습니다.**
아무도 몰랐던 이유는, `Find` 가 조용히 첫 번째를 돌려줬기 때문입니다.

`SpawnedObject` 테이블에서도 **id가 비어 조회 자체가 불가능한 행 11건**이 나왔습니다.

### 배운 것

**자료구조를 바꾸면 그 자료구조의 제약이 데이터를 검사해줍니다.**

`List` 는 중복을 허용하지만 `Dictionary` 는 거부합니다.
성능을 위해 바꾼 건데, 결과적으로 **몇 달간 숨어 있던 데이터 버그가 즉시 드러났습니다.**

이후로는 인덱스를 만들 때 중복을 경고로 남기고,
`인덱스 개수 == id 가 있는 행의 수` 를 테스트로 고정했습니다. 시트에 중복이 들어오면 바로 실패합니다.

### 일괄 치환에서 낸 실수도 적어둡니다

65곳을 스크립트로 치환했는데, 실행하자마자 `NullReferenceException` 이 났습니다.

```csharp
private void SetCopyData()
{
    foreach (...) itemCopyList.Add(data);      // ① 복제본 생성

    foreach (var relic in ...)
        var itemData = GetItemCopy(relic.id);  // ② 인덱스 조회 → 아직 없음 ❌

    BuildCopyIndexes();                         // ③ 인덱스는 여기서 생성
}
```

**복제본을 만드는 코드 안에서 그 복제본의 인덱스를 조회**하게 만든 것입니다.
치환 스크립트는 "조회하는 곳"과 "만드는 곳"을 구분하지 못합니다.

일괄 치환은 빠르지만 **생성 순서를 보지 못한다**는 걸 배웠습니다.
지금은 그 지점에 왜 인덱스를 쓰면 안 되는지 주석이 달려 있습니다.

---

## 15. 관리자를 하나 더 만들 뻔했다

### 문서에 이렇게 적어뒀었습니다

> "MVP 인프라(인터페이스 33개)는 갖췄으나 이를 조립하는 관리자가 없음
> → **UI 전용 관리자를 새로 만들어 생성 경로 일원화**"

`UIManager` 는 원래 있었습니다. 기존 팝업이 이미 `GameManager` 의 풀 API로 돌아가고 있어
전환을 미뤘고, 통째로 주석 처리해 둔 채 1년 넘게 방치하다 결국 제거했습니다.
그래서 "다시 만들어야 한다"고 적어두고 있었습니다.

### 그런데 지적을 받았습니다

> "어차피 UI든 일반 오브젝트든 `GameManager` 의 스폰 메서드에서 생성되잖아?"

맞는 말이었습니다. 확인해보니 **생성 경로는 이미 하나**였습니다.

```csharp
SpawnToObjectPool(id, pos)   → pool.Spawn(id, objectPool, pos)
SpawnToUIPool(type, pos)     → pool.Spawn(id, uiPool,     pos)
SpawnToPopupPool(type, pos)  → pool.Spawn(id, popupPool,  pos)
```

부모 `Transform` 만 다를 뿐 전부 `ObjectPoolService.Spawn()` 을 탑니다.
여기에 관리자를 더하면 **위임 껍데기가 하나 늘어날 뿐**이었습니다.

제가 문제를 잘못 진단하고 있었던 것입니다.

### 진짜 문제를 다시 찾았습니다

세어보니 Presenter 를 만드는 코드가 **35곳**에 흩어져 있었습니다.

| 파일 | 조립 지점 |
|---|---|
| `GameManager.Ui.cs` | 11 |
| `RoomManager.cs` | 6 |
| `Popup_Setting.cs` | 5 |
| `Popup_Character.cs` | 5 |
| 그 외 6개 파일 | 8 |

그리고 매번 같은 다섯 단계를 손으로 반복했습니다.

```csharp
var goodsInterface = uiInterface.GoodsView.ConvertTo<IUIGoodsView>();   // ① 인터페이스 변환
var goodsModel = new UIGoodsModel() { totalGold = Gold };               // ② 모델 생성
var goodsPresenter = new UIGoodsPresenter(goodsInterface, goodsModel);  // ③ 프레젠터 생성
uiInterface.SetGoodsPresenter(goodsPresenter);                          // ④ 역주입
goodsPresenter.SetGoldText();                                           // ⑤ 실행
```

**문제는 "생성"이 아니라 "조립"이었습니다.** 그리고 조립에는 주인이 없었습니다.

### 해결 — 관리자가 아니라 주인을 정했다

조립의 주인은 View 가 맞습니다. 자기 Presenter 를 자기가 만들면 됩니다.

```csharp
public class UIGoodsView : MonoBehaviour, IUIGoodsView
{
    private UIGoodsPresenter presenter;
    public UIGoodsPresenter Presenter => presenter;

    public UIGoodsPresenter Bind(UIGoodsModel model)
    {
        presenter = new UIGoodsPresenter(this, model);
        return presenter;
    }
}
```

호출부는 한 줄이 됐습니다.

```csharp
uiInterface.GoodsView.Bind(new UIGoodsModel { totalGold = Gold }).SetGoldText();
```

| | 전 | 후 |
|---|---|---|
| Presenter 직접 조립 | 35곳 | **2곳** |
| `ConvertTo` 인터페이스 변환 | 32회 | **0회** |
| `SetXxxPresenter` 역주입 | 7개 | **0개** |
| `SpawnGameInterface()` | 42줄 | **17줄** |

`Bind` 가 `this` 를 넘기므로 `ConvertTo` 가 필요 없어졌고,
View 가 Presenter 를 들고 있으므로 역주입도 사라졌습니다.
**`UI_Interface` 와 호출부가 같은 Presenter 를 각자 보관하던 이중 구조**도 정리됐습니다.

남긴 2곳은 View 하나로는 조립할 수 없는 경우입니다.
`BindBossHp` 는 Model 이 없고, `BindSkill` 은 교체·포션·일반 스킬 View 3종을 한꺼번에 다룹니다.
세 View 를 모두 가진 `UI_Interface` 가 조립하는 것이 자연스럽습니다.

### 배운 것

**"관리자가 없다"는 진단이 틀렸습니다.** 없는 건 관리자가 아니라 **책임의 주인**이었습니다.

구조가 허전해 보일 때 계층을 하나 추가하는 건 쉽습니다.
그런데 그 계층이 실제로 하는 일이 위임뿐이라면, 코드는 늘고 문제는 그대로입니다.

지금도 팝업 스택·ESC 우선순위 같은 요구가 생기면 얇은 계층이 필요할 것입니다.
다만 그건 **"여러 팝업의 관계를 한 곳에서 봐야 한다"는 실제 요구가 생겼을 때**의 이야기지,
지금 미리 만들 이유는 없었습니다.

### 덧 — 일괄 치환이 또 걸렸다

35곳을 스크립트로 바꾸다 컴파일 에러가 났습니다.
팝업 컨테이너 7개가 View 를 **인터페이스 타입으로 노출**하고 있었습니다.

```csharp
public IPopupStoreView StoreView => storeView;      // 인터페이스로 노출
[SerializeField] private PopupStoreView storeView;  // 실제 타입
```

`Bind` 는 구체 클래스에 있으니 인터페이스로는 부를 수 없습니다.
[14번](#14-dictionary로-바꿨더니-데이터-버그가-나왔다)에서는 생성 순서를 놓쳤는데, 이번엔 타입이었습니다.

**치환 스크립트는 문맥을 보지 못합니다.** 빠른 대신 그 대가를 두 번 치렀습니다.

---

## 16. 소수점이 쉼표인 나라에서만 터지는 버그

외부에 코드 리뷰를 받았습니다. `int.Parse` / `float.Parse` 를 예외 처리 없이 쓰고 있어
테이블에 오타가 하나 들어가면 게임이 죽는다는 지적이었습니다. 맞는 말이라 고치러 갔는데,
고치다가 **더 나쁜 문제**를 찾았습니다.

```csharp
// 83곳에서 이렇게 쓰고 있었다
float speed = float.Parse(monsterData.moveSpeed);
```

`float.Parse` 는 인자를 안 주면 **실행 환경의 로케일**을 씁니다.
독일·프랑스·스페인·러시아·포르투갈은 소수점 구분자가 쉼표(`,`)이고, 그 환경에서는

| 테이블 값 | 한국/미국 | 독일/프랑스 |
|---|---:|---:|
| `"1.5"` | 1.5 | **15** |
| `"0.05"` | 0.05 | **5** |

`"1.5"` 가 **15로 읽힙니다.** 점을 소수점이 아니라 자릿수 구분자로 해석하기 때문입니다.

이게 왜 심각하냐면 — **이 게임은 8개 언어로 스팀에 출시합니다.**
지원 언어 8개 중 5개가 쉼표 로케일 지역입니다. 몬스터 이동 속도가 10배가 되고,
저스트 카운터 슬로우모션 `0.05` 가 `5`가 되어 시간이 5배로 흐릅니다.

그런데 **한국에서 개발하고 한국에서 테스트하면 절대 재현되지 않습니다.**
크래시도 아니고 예외도 아니라, 로그에도 안 남습니다. 그 지역 유저만 "게임이 이상하다"고 합니다.

```csharp
// TableParse — 로케일 고정 + 예외 대신 경고와 기본값
public static float Float(string value, float fallback = 0f)
{
    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        return result;

    GameLog.Warning($"[TableParse] float 변환 실패: \"{value}\" → {fallback} 사용");
    return fallback;
}
```

83곳을 전부 이 경로로 옮겼습니다. 테스트에는 `new CultureInfo("de-DE")` 를 강제로 걸어
독일 로케일에서도 `"1.5"` 가 1.5로 읽히는지 검증합니다.

**배운 것.** 리뷰어가 지적한 것은 "예외 처리가 없다"였고, 그건 사실 부차적인 문제였습니다.
같은 줄에 있던 진짜 문제는 **내 개발 환경에서는 영원히 드러나지 않는 종류**였습니다.
지적을 받으면 지적당한 것만 고치고 끝내기 쉬운데, 그 코드를 다시 읽어보는 게 더 중요했습니다.

---

## 17. 팝업 위에 팝업이 뜨면 시간이 풀린다

같은 리뷰에서 "`Time.timeScale` 을 여러 곳에서 직접 건드리고 있다,
지금은 운으로 버티는 중일 가능성이 높다"는 지적이 있었습니다.
세어 보니 **21곳**이었습니다.

```csharp
// UIBase — 팝업이 열리고 닫힐 때
if (timeStop)  Time.timeScale = 0f;
if (timeReset) Time.timeScale = 1f;
```

팝업이 하나만 뜰 때는 맞습니다. 문제는 겹칠 때입니다.

```
① 특성 팝업 열림       timeScale = 0
② 구매 확인 팝업 열림   timeScale = 0
③ 확인 팝업 닫힘       timeScale = 1   ← 특성 팝업은 열려 있는데 게임이 돌아간다
```

"이론상 가능"인지 확인해 봤더니, `PopupAttributeView` 가 구매 시 `SpawnSelect`(확인 팝업)를
호출합니다. **실제로 도달하는 경로**였습니다. 특성을 사려고 확인창을 띄웠다 취소하면,
특성 팝업이 열린 채로 뒤에서 몬스터가 움직입니다.

입력 잠금(`ControlStart`)도 구조가 똑같았습니다.

### 깊이를 세지 않고 요청자를 센 이유

떠오르는 첫 해법은 카운터입니다. 열면 `+1`, 닫으면 `-1`, `0`이 되면 해제.
하지만 카운터는 **같은 팝업이 실수로 두 번 해제하면 음수로 내려갑니다.**
그러면 아직 열려 있는 다른 팝업의 정지까지 풀립니다. 원래 버그와 같은 증상이 다른 경로로 재발합니다.

그래서 **"누가 멈춰달라고 했는지"** 를 집합으로 들고 있게 했습니다.

```csharp
private readonly HashSet<object> timeHolders = new HashSet<object>();

public void StopTime(object owner)
{
    if (owner == null || !timeHolders.Add(owner)) return;   // 중복 요청은 걸러진다
    Apply();
}

public void ResumeTime(object owner)
{
    if (owner == null || !timeHolders.Remove(owner)) return; // 없는 요청 해제는 무시된다
    Apply();
}

private void Apply() => Time.timeScale = timeHolders.Count > 0 ? 0f : baseTimeScale;
```

`HashSet` 을 고른 건 조회 속도 때문이 아닙니다. 동시에 열리는 팝업은 많아야 서너 개라
`List` 로도 충분히 빠릅니다. **`Add` 가 중복을 걸러 주는 집합 의미** 때문에 고른 것입니다.
자료구조를 성능이 아니라 의미로 고른 경우입니다.

### 슬로우모션과 정지를 분리한 이유

저스트 카운터(`0.05`)와 아레나 연출(`0.2`)이 `Time.timeScale` 을 쓰고 있었습니다.
정지와 같은 변수를 공유하니 서로 덮어썼습니다 — 슬로우모션 중에 팝업을 열었다 닫으면
슬로우모션이 사라지고 정상 속도로 돌아갔습니다.

`baseTimeScale` 을 따로 두어, 정지 중에는 `0`이 우선하고 정지가 풀리면 슬로우모션 값으로 돌아갑니다.

### 결과

| | 이전 | 현재 |
|---|---:|---:|
| `Time.timeScale` 직접 쓰기 | 21곳 | **0곳** |
| 테스트 | 0개 | 10개 |

핵심 테스트는 버그를 그대로 재현한 것입니다.

```csharp
[Test]
public void 마지막_요청자가_풀려야_시간이_돌아온다()
{
    flow.StopTime(PopupA);
    flow.StopTime(PopupB);

    flow.ResumeTime(PopupB);        // 위쪽 팝업만 닫힘
    Assert.IsTrue(flow.IsTimeStopped, "아래 팝업이 남아 있으면 계속 멈춰 있어야 한다");

    flow.ResumeTime(PopupA);
    Assert.IsFalse(flow.IsTimeStopped);
}
```

**모든 곳에 적용하지는 않았습니다.** 컷신처럼 순차적으로만 일어나는 연출은
`ControlStart` 를 직접 씁니다. 겹칠 일이 없어 요청 방식이 필요 없고,
필요 없는 곳까지 바르면 읽는 사람이 "여긴 왜 이렇게 했지"를 고민하게 됩니다.

---

## 18. 4,444줄에서 무엇을 뽑고 무엇을 남길 것인가

`Room.cs` 가 4,444줄이었습니다. [12번](#12-god-object를-어디부터-뜯을지--감이-아니라-측정으로)에서
`GameManager` 에 썼던 방법을 그대로 적용했습니다 — 감이 아니라 측정.

먼저 메서드 113개를 책임별로 묶었습니다.

| 책임 | 줄수 | 비중 |
|---|---:|---:|
| 스토리 연출 | 1,085 | 31% |
| 기타 | 539 | 15% |
| 보스 이벤트 | 421 | 12% |
| 문 / 방 이동 | 414 | 12% |
| 미니맵 | 391 | 11% |
| 방 셋업 | 327 | 9% |

"방 하나를 관리한다"가 아니라 **방과 관련된 모든 것**이 모여 있었습니다.

### 파일을 옮기는 것과 갈라지는 것은 다르다

여기서 멈추고 파일만 나눌 수도 있었습니다. 하지만 `GameManager` 때 배운 것이 있습니다 —
`partial` 은 파일을 가를 뿐 **결합을 줄이지 않습니다.** 필드를 공유하면 여전히 한 덩어리입니다.

그래서 각 책임이 84개 필드 중 무엇을 쓰는지 셌습니다.

| 책임 | 쓰는 필드 | 공유 상황 | 판정 |
|---|---:|---|---|
| 보스 이벤트 | 14개 | `bosses` · `npc` · `roomInfo` · `customObjects` 를 다른 책임과 공유 | 분리 불가 |
| 문 / 방 이동 | 16개 | `roomInfo` · `roomsData` · `shortCutObjects` 공유 | 분리 불가 |
| 스토리 연출 | 11개 | 공유는 적지만 재사용 없는 일회성 코드 | 파일만 분리 |
| **미니맵** | **21개 (전용 9개)** | — | **클래스로 추출** |

미니맵 전용 필드가 정말 전용인지 하나씩 확인했습니다.

```
visitedFrameCells        미니맵 4회 / 그 외 0회
visitedInCells           미니맵 4회 / 그 외 1회  (VisitedPlace)
minimapFrameTilemap      미니맵 2회 / 그 외 1회  (Awake — 초기화뿐)
originalFrameTiles       미니맵 2회 / 그 외 1회  (Awake)
shortcutFrameTileMaps    미니맵 2회 / 그 외 1회  (Awake)
```

**바깥 사용처가 전부 `Awake` 의 초기화였습니다.** 생성자 인자로 넘기면 끝나는 관계입니다.
우연이 아니라, 미니맵이 원래 독립된 기능인데 자리만 `Room` 안이었다는 뜻이었습니다.

### 테스트가 붙는지도 확인했다

가장 큰 미니맵 메서드 `RevealCellsInView` 180줄에서 Unity API 사용량을 셌습니다.

```
transform  5회
SetTile    3회
```

**180줄 중 8줄만 Unity에 의존했습니다.** 나머지는 "플레이어 시야 안의 셀이 어디인가"를
계산하는 격자 수학이었습니다. `MonoBehaviour` 밖으로 빼면 테스트가 붙는다는 뜻입니다.

### 뽑는 김에 드러난 중복

옮기면서 같은 코드가 여러 벌 있다는 걸 알게 됐습니다.

**세이브 직렬화가 4곳.** 미니맵 방문 셀은 `"x_y_z;"` 형식으로 저장하는데,
`JsonUtility` 가 `Vector3Int` 리스트를 못 다뤄서 문자열로 눌러 담습니다.
그 인코딩/디코딩이 테두리·내부·숏컷(`Room`)과 숨겨진 구역(`HiddenArea`)에 복사돼 있었습니다.
**세이브 파일 형식인데 네 곳에 흩어져 있으면 한 곳만 고쳐도 세이브가 깨집니다.**

**겹침 판정이 7곳.** "카메라 시야에 조금이라도 걸치면 공개"라는 판정이
미니맵 셀 3종과 마커 4종에 그대로 복사돼 있었습니다.

둘 다 한 곳으로 모으고 테스트를 붙였습니다.

### 무엇을 남겼는지가 더 중요하다

미니맵 마커(세이브 포인트·포탈·상인·획득물)는 **`Room` 에 남겼습니다.**
미니맵 전용 데이터가 아니라 방이 소유한 오브젝트이고,
공개 조건이 "이미 먹었는가" 같은 방 상태에 걸려 있어 옮기면 결합이 오히려 늘어납니다.

보스 이벤트와 문 이동도 남겼습니다. 줄수만 보면 각각 400줄이 넘어 뽑을 가치가 있어 보이지만,
**필드 공유가 커서 뽑아도 생성자 인자가 10개를 넘어갑니다.** 그건 분리가 아니라 이사입니다.

### 결과

```
Room.cs   4,444줄  →  1,469줄
```

| 산출물 | 줄수 | 성격 |
|---|---:|---|
| `RoomMinimap` | 324 | 클래스 추출 · `MonoBehaviour` 아님 |
| `MinimapCellCodec` | 72 | 순수 정적 · 테스트 9개 |
| `Room.Product.cs` | 2,005 | `partial` — 파일만 분리 |
| `Room.InfoSetting.cs` | 775 | `partial` — 파일만 분리 |

**같은 판정 기준을 네 번째로 적용한 사례입니다.**
`LocalizationService`, `ObjectPoolService`, `GameFlowService`, 그리고 `RoomMinimap`.
한 번은 운이지만 네 번이면 방법입니다.

### 동작을 바꾸지 않기 위해 조심한 것

옮기는 중에 "고치고 싶은" 코드가 두 개 보였지만 그대로 뒀습니다.

판정 사각형을 위로 넓히는 보정이 테두리 → 내부 순으로 **누적**되고,
그 누적값을 마커·숏컷·숨겨진 구역이 함께 씁니다.
읽기 좋게 각자 계산하도록 바꾸면 미니맵이 칠해지는 타이밍이 미세하게 달라집니다.
그래서 보정된 사각형을 **반환해서 넘기는** 다소 어색한 형태를 유지했습니다.

숏컷 판정이 숏컷 타일맵이 아니라 **테두리 셀 크기**를 쓰고 있었습니다.
버그처럼 보이지만 현재 미니맵이 그 크기 기준으로 그려져 있어, 고치면 그림이 바뀝니다.
주석만 남기고 뒀습니다.

**리팩터링 중에 발견한 버그는 리팩터링과 같은 커밋에서 고치지 않는 게 낫습니다.**
동작이 바뀌었을 때 원인이 둘 중 무엇인지 알 수 없게 되기 때문입니다.

---

## 19. 고치지 않기로 한 것 — 연출 1,036줄

`Room.cs` 의 31%가 스토리 연출 14종이었습니다.
README에는 "데이터 주도 설계 — 밸런스는 코드를 건드리지 않는다"라고 써 놓고,
정작 연출은 C#에 하드코딩돼 있으니 **명백한 모순**입니다.

JSON 명령 목록으로 옮기는 방안을 먼저 생각했습니다.

```json
{ "id": 6, "steps": [
  { "cmd": "Delay", "value": 1.0 },
  { "cmd": "Speech", "talkId": 10104 },
  { "cmd": "CameraShake", "x": 0.2, "y": 0.1 }
] }
```

그런데 착수 전에 **실제로 얼마나 줄어드는지 재봤습니다.**

| | 값 |
|---|---:|
| 연출 14종 합계 | 1,036줄 |
| 대사 출력 쌍 | 69개 |
| 그중 연속된 구간 | **50개** |
| 평균 연속 길이 | **1.4개** (최대 3개) |
| 대사를 묶었을 때 감소 | **86줄 (8%)** |

대사가 몰려 있을 거라 짐작했는데 아니었습니다. 문장 사이사이에
딜레이·캐릭터 이동·카메라·조건 분기가 끼어 있어 **압축할 중복 자체가 없었습니다.**

전체 문장 구성을 보면 더 분명합니다.

```
기타 16.4%   제어 흐름 13.5%   대기 12.5%
대사 21.4%   위치 조회 9.1%    정형구 8.5%
```

대사는 21%뿐이고, 나머지는 비트 단위로 짜인 안무였습니다.
**1,000줄은 그냥 1,000줄어치 서로 다른 연출입니다.**

### 업계가 실제로 쓰는 방법을 확인했다

그리고 JSON 명령 해석기가 흔한 방법인지 다시 생각해 봤습니다. 아니었습니다.
2D 액션 게임에서 방별 이벤트는 보통 이렇게 처리합니다.

| 방식 | 저장 위치 |
|---|---|
| 씬/프리팹에 저작 — 트리거 콜라이더 + 인스펙터 연결 | 씬 파일 |
| 비주얼 FSM (PlayMaker 등) | 에셋 |
| 대사 전용 DSL (Ink, Yarn) | 텍스트 |
| C# 코루틴/async 시퀀스 | 코드 |

공통점은 **연출 로직 자체는 어딘가에 절차적으로 적힌다**는 것입니다.
JSON 배열로 게임 로직을 표현하면 분기·변수·조건에서 표현력이 금방 바닥나
결국 코드 안에 못생긴 인터프리터가 남습니다.

그리고 확인해 보니 **이 프로젝트의 트리거는 이미 표준 방식**이었습니다.

```csharp
// ProductTrigger — 방 프리팹에 배치된다
private void OnTriggerEnter2D(Collider2D col)
{
    if (col.CompareTag(ConstValues.Player) && !col.isTrigger)
    {
        myAction?.Invoke();
        triggerCollider.enabled = false;   // 1회성
    }
}

// Room — 프리팹을 훑어 수집한다
productTriggers = gridObject.GetComponentsInChildren<ProductTrigger>();
```

게다가 발동 여부가 세이브와 물려 있어, 이미 본 연출은 트리거 자체가 비활성화됩니다.

### 결론

**데이터화하지 않고 파일만 분리했습니다.**

데이터화의 이득은 **변경 빈도**에서 나옵니다. 밸런스 수치는 출시까지 수십 번 바뀌지만
연출은 한 번 만들면 거의 고치지 않습니다. 명령 20~30종을 정의하는 대가로
타입 검사와 디버거를 잃는 거래는 수지가 맞지 않았습니다.

**배운 것.** 처음에는 "모순이니까 고쳐야 한다"고 생각했습니다.
하지만 측정해 보니 고치는 쪽이 더 나쁜 선택이었습니다.
일관성은 그 자체로 목적이 아니라, **같은 기준을 적용한 결과**여야 합니다.
모든 곳에 같은 패턴을 바르는 것보다, 바르지 않은 이유를 설명할 수 있는 편이 낫습니다.

---

## 20. 일괄 치환이 세 번 문맥을 놓친 이야기

구조를 정리하면서 "같은 패턴 N곳을 한 번에 바꾸는" 작업을 여러 번 했습니다.
`Find` → 인덱스 조회 65곳, `Parse` → `TableParse` 83곳, `Debug.Log` → `GameLog` 96곳.

전부 스크립트로 처리했고, **매번 무언가를 놓쳤습니다.** 네 가지 유형이었습니다.

### ① 생성 순서를 못 본다

`Find(x => x.id == …)` 를 인덱스 조회로 바꾸는 작업에서 게임이 죽었습니다.

```csharp
// SetCopyData() 안 — 복제본을 "만드는 중"인 코드
var itemData = itemCopyList.Find(x => x.id == relic.id);
```

인덱스는 `SetCopyData()` 가 **끝난 뒤에** 만들어집니다.
이 줄은 그 인덱스를 만드는 데 필요한 데이터를 준비하는 중이라,
인덱스를 쓰면 아직 존재하지 않는 것을 조회하게 됩니다.

구문만 보면 다른 64곳과 똑같이 생겼습니다. 다른 건 **실행 시점**입니다.

```csharp
// 되돌리고 주석을 남겼다
// 인덱스는 SetCopyData 가 끝난 뒤에 만들어지므로 여기서는 쓸 수 없다.
// 복제본을 만드는 중이라 원본 리스트를 직접 조회한다.
var itemData = itemCopyList.Find(x => x.id == relic.id);
```

### ② 수신자 타입을 못 본다

MVP 조립을 정리할 때 팝업 7종이 컴파일 에러를 냈습니다.
View 를 인터페이스 타입으로 들고 있는 곳이 있었는데, `Bind()` 는 구현 클래스에만 있었습니다.
치환기는 `.Bind(` 라는 모양만 보고 타입은 보지 못합니다.

### ③ 변수의 생존 범위를 못 본다

639줄짜리 `InfoSetting()` 을 24개 메서드로 나눌 때,
앞부분에서 선언한 변수를 뒷부분에서 쓰고 있었습니다.
한 메서드일 때는 문제가 없었지만 나누는 순간 스코프를 벗어났습니다.

### ④ 파일 인코딩을 못 본다

죽은 주석을 정리하는 스크립트를 돌렸더니 diff에 **5,391줄 삭제**가 찍혔습니다.
실제로는 800줄쯤만 지웠는데, 스크립트가 파일을 다시 쓰면서 CRLF를 LF로 바꿔
**모든 줄이 변경으로 잡힌 것**이었습니다.

되돌리고 BOM과 줄바꿈을 보존하도록 I/O를 다시 썼습니다.
같은 실수를 미니맵 작업에서도 반복하지 않으려고, 파일을 건드리는 스크립트는
전부 `utf-8-sig` 로 읽고 원래 줄바꿈을 유지하도록 고정했습니다.

### 정리

> **스크립트는 구문을 보지만 의미는 못 봅니다.**

그렇다고 손으로 하는 게 답은 아닙니다. 96곳을 손으로 고치면 다른 종류의 실수가 납니다.
결국 이렇게 굳혔습니다.

1. 치환은 스크립트로 하되 **범위를 좁게** 잡는다 (`Assets/Scripts` 만, 테스트·에디터 제외)
2. 치환 후 **diff의 줄 수가 예상과 맞는지** 먼저 본다 — ④는 이 단계에서 잡혔다
3. 컴파일이 통과해도 **실행**해 본다 — ①은 컴파일을 통과했다
4. 되돌린 곳에는 **왜 예외인지 주석을 남긴다** — 다음에 또 같은 스크립트를 돌릴 것이므로

3번이 특히 중요했습니다. ①번 버그는 `NullReferenceException` 으로 게임 시작 직후 터졌는데,
컴파일러는 아무 말도 하지 않았습니다.

---

## 21. 아직 풀지 못한 문제들

숨기는 것보다 적어두는 편이 낫다고 판단했습니다.

### `GameManager` 가 아직 하나의 클래스다

한때 3,840줄 / public 메서드 199개였습니다.
세이브, 재화, 스탯, 오브젝트 풀, 아틀라스 캐시, 키 바인딩이 한 클래스에 있었습니다.
지금은 9개 파일 3,167줄이고 본체는 403줄이지만, **여전히 클래스는 하나**입니다.

**왜 이렇게 됐나:** "일단 여기 두고 나중에 나누자"가 16개월 쌓였습니다.
`DontDestroyOnLoad` 매니저라 어디서든 접근 가능하니 계속 붙였습니다.

**지금까지 한 것:** [12번](#12-god-object를-어디부터-뜯을지--감이-아니라-측정으로) 참고.
타입 32개 분리 → `partial` 역할 분할 → 의존도 측정 → 서비스 3개 추출
(`LocalizationService` · `ObjectPoolService` · `GameFlowService`).

**남은 문제:** 파일은 9개로 나뉘었지만 **클래스는 여전히 하나**입니다.
`partial` 은 결합도를 낮추지 못하므로 God Object 자체는 그대로입니다.
특히 `Progression`(1,172줄)은 `saveData` 에 깊게 묶여 있어,
그 안의 인덱스들은 **`MonoBehaviour` 안에 있다는 이유만으로 테스트할 수 없습니다.**

**계획:** `saveData` 를 감싸는 `GameState` 를 만들어 주입하고 `ProgressionService` 를 추출합니다.
측정에서 `Player` 그룹의 독점 필드가 12%로 가장 낮게 나왔으므로, 캐릭터 교체 쪽은 마지막에 다룹니다.

### 테스트 범위가 아직 좁다

EditMode 테스트 67개가 생겼지만, **전부 `MonoBehaviour` 밖으로 꺼낸 코드에만** 붙어 있습니다.

| 대상 | 테스트 |
|---|---:|
| `LocalizationService` | 10개 |
| `ObjectPoolService` | 13개 |
| `GameFlowService` | 10개 |
| `TableParse` | 10개 |
| `MinimapCellCodec` | 9개 |
| `TableManager` 인덱스 | 7개 |
| `RoomMinimap` 겹침 판정 | 7개 |
| 풀 조회 A/B 벤치마크 | 1개 |

**Presenter 테스트는 아직 0개입니다.** MVP 구조를 만든 목적이 검증인데
정작 그 검증을 붙이지 않았습니다. View 인터페이스를 가짜로 구현하면 바로 가능한 상태라,
남은 것은 설계가 아니라 작성입니다. 구조가 테스트를 *가능하게* 한 것과
실제로 테스트가 *있는* 것은 다르므로 여기에 적어 둡니다.

정작 검증하고 싶은 전투 로직은 여전히 `Character`(`MonoBehaviour`) 안에 있어 손댈 수 없습니다.

- `ConsumeShield()` — 정렬 순서와 잔여 데미지
- 버프 만료 · 틱 계산
- 데미지 계수 · 치명타 계산
- `EBodyType` 별 경직 판정

이 네 개는 **순수 계산이라 원래 테스트하기 가장 쉬운 것들**인데,
`MonoBehaviour` 에 얹혀 있다는 이유만으로 불가능합니다.

**계획:** 실드·버프 계산을 별도 클래스로 분리한 뒤 테스트를 붙입니다.
`ObjectPoolService` 때와 같은 순서입니다 — **분리해야 검증할 수 있고, 검증하려다 보면 문제가 드러납니다.**
(실제로 풀을 분리하면서 없는 프리팹에 크래시하던 버그를 찾았습니다)

### 연출 2,005줄의 `async void` 26개

`Room.Product.cs` 의 연출은 대부분 `async void` 입니다.
반환값이 `void` 인 `async` 는 **예외가 조용히 삼켜지고**, 호출한 쪽이 완료를 기다릴 수 없습니다.

연출이 시작될 때 `UIOff()` 와 `ForceProduct()` 로 UI를 끄고 조작을 잠그는데,
중간에 예외가 나면 그 상태가 **복구되지 않습니다.** 플레이어는 화면만 보이고
아무것도 누를 수 없는 상태로 갇힙니다.

**계획:** `UniTaskVoid` 로 바꾸고 예외 처리를 넣습니다.
연출이 실패해도 최소한 UI와 조작은 되돌려야 합니다.
[19번](#19-고치지-않기로-한-것--연출-1036줄)에서 연출을 데이터화하지 않기로 했으므로,
이쪽이 연출 코드에 남은 유일한 실질적 개선 과제입니다.

### 주석 처리된 코드 약 430줄

한때 5,200줄이었습니다. 이력은 Git이 보관하는데도 지우지 못했고,
"혹시 되돌릴까 봐"가 쌓인 결과였습니다.

정리하면서 **주석에도 두 종류가 있다**는 걸 확실히 알게 됐습니다.
죽은 코드와 설명 주석이 섞여 있어서, 일괄 삭제하면 코드베이스의 문서가 통째로 날아갑니다.
실제로 자동 정리를 시도했다가 세이브 마이그레이션 로직을 설명하는 주석이
삭제 대상에 잡히는 걸 발견했습니다. 바로 위에 죽은 코드가 있었다는 이유만으로요.

결국 이런 규칙으로 정리했습니다.

- 연속된 주석을 **블록**으로 묶고, 코드 신호(`;` `{` `}`)가 절반 이상이면 죽은 코드로 판정
- 5줄 이상 블록은 주석 처리된 메서드로 보고 전체 삭제
- 4줄 이하 블록은 코드 줄만 삭제 — 살아있는 코드를 설명하는 주석일 수 있으므로
- **한글이 포함된 주석은 문서로 간주해 보존** (이 코드베이스는 한국어 주석이 규약)

832줄을 삭제했습니다.

이후 다시 세어 보니 남은 것은 **약 430줄**이었고, 흩어진 실험 흔적이 아니라
**한 기능**이었습니다 — 8개 파일에 걸친 `SetMouseInteraction()`, 도입하다 만 마우스 조작입니다.
"2,023줄의 대안 구현"이라고 적어 뒀던 것은 어림수였고, 실제로는 되살릴지 지울지만
결정하면 되는 단일 항목이었습니다.

**어림수를 문서에 적으면 코드보다 문서가 먼저 낡습니다.**
이후로는 수치를 적을 때 측정 방법을 함께 남기고 있습니다.

---

## 마치며

16개월간 혼자 개발하면서, 기능을 만드는 것보다
**이전에 내린 결정이 나중에 발목을 잡는 순간**이 더 많았습니다.

- `bool` 로 시작한 개념은 대부분 등급이나 축으로 쪼개졌습니다 ([1](#1-방어-타입을-7단계까지-나눈-이유), [6](#6-함정에-두-번-맞는다--무적-관통-공격의-사각))
- "빠뜨릴 수 있는 구조"는 결국 빠뜨렸습니다 ([5](#5-새-게임인데-이전-유물이-남아-있다), [11](#11-코루틴을-버리고-unitask로))
- 재사용하는 오브젝트는 이전 상태를 물려받았습니다 ([4](#4-부활-후-미사일--재현이-안-되던-고질적-버그))
- 반복 작업을 툴로 옮긴 결정은 예외 없이 남는 이득이었습니다 ([8](#8-룸을-손으로-배치할-수-없게-되었을-때))

구조를 정리하면서는 다른 것을 배웠습니다.

- 어디부터 뜯을지는 **감이 아니라 측정**으로 정해야 했습니다 ([12](#12-god-object를-어디부터-뜯을지--감이-아니라-측정으로), [18](#18-4444줄에서-무엇을-뽑고-무엇을-남길-것인가))
- 느린 원인은 알고리즘이 아니라 **엔진 API의 숨은 비용**이었습니다 ([13](#13-520배-빨라진-이유는-알고리즘이-아니었다))
- 자료구조를 바꾸자 **그 제약이 데이터를 검사해줬습니다** ([14](#14-dictionary로-바꿨더니-데이터-버그가-나왔다))
- 구조가 허전할 때 **계층을 더하는 대신 책임의 주인을 찾아야 했습니다** ([15](#15-관리자를-하나-더-만들-뻔했다))
- 분리해야 검증할 수 있고, **검증하려다 보면 문제가 드러났습니다**

외부 리뷰를 받고 나서 배운 것은 조금 결이 달랐습니다.

- 지적받은 것만 고치면 **같은 줄에 있는 더 큰 문제를 놓칩니다** ([16](#16-소수점이-쉼표인-나라에서만-터지는-버그))
- "이론상 가능"과 "실제로 도달한다"는 다르고, **확인은 코드를 따라가면 됩니다** ([17](#17-팝업-위에-팝업이-뜨면-시간이-풀린다))
- 자료구조는 성능이 아니라 **의미로 고를 때도 있습니다** ([17](#17-팝업-위에-팝업이-뜨면-시간이-풀린다))
- 무엇을 뽑았는지보다 **무엇을 왜 남겼는지**가 설명하기 어렵고, 그래서 더 중요합니다 ([18](#18-4444줄에서-무엇을-뽑고-무엇을-남길-것인가))
- 모순을 발견해도 **고치는 쪽이 더 나쁠 수 있습니다.** 측정 없이 착수했으면 며칠을 썼을 겁니다 ([19](#19-고치지-않기로-한-것--연출-1036줄))
- 스크립트는 구문을 보지만 **의미는 못 봅니다** ([20](#20-일괄-치환이-세-번-문맥을-놓친-이야기))

[21번](#21-아직-풀지-못한-문제들)에 적은 것들은
**혼자였기 때문에 미룰 수 있었던 부채**입니다.
God Object도 이원화된 팝업 경로도, 나 혼자 알고 있으면 굴러갔습니다.

로케일 파싱 버그와 팝업 중첩 버그는 둘 다 **혼자 테스트해서는 드러나지 않는 종류**였습니다.
전자는 내 개발 환경에서 재현되지 않았고, 후자는 "그렇게 안 쓰니까" 넘어갔던 경로였습니다.
외부 시선이 필요했던 이유가 거기에 있었습니다.

지금 가장 하고 싶은 작업은 새 기능이 아니라 21번 목록을 지우는 일입니다.

---

- 시스템 설계 상세: [`ARCHITECTURE.md`](ARCHITECTURE.md)
- 프로젝트 개요: [`../README.md`](../README.md)
