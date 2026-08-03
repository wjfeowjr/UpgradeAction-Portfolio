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

**남은 것**

15. [아직 풀지 못한 문제들](#15-아직-풀지-못한-문제들)

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
  "addCompositeCollider": false,

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
- **`addCompositeCollider` 옵션** — 룸마다 콜라이더 병합 여부가 다름
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

## 15. 아직 풀지 못한 문제들

숨기는 것보다 적어두는 편이 낫다고 판단했습니다.

### `GameManager` 가 아직 하나의 클래스다

한때 3,840줄 / public 메서드 199개였습니다.
세이브, 재화, 스탯, 오브젝트 풀, 아틀라스 캐시, 키 바인딩이 한 클래스에 있었습니다.
지금은 9개 파일 3,208줄이고 본체는 378줄이지만, **여전히 클래스는 하나**입니다.

**왜 이렇게 됐나:** "일단 여기 두고 나중에 나누자"가 16개월 쌓였습니다.
`DontDestroyOnLoad` 매니저라 어디서든 접근 가능하니 계속 붙였습니다.

**지금까지 한 것:** [12번](#12-god-object를-어디부터-뜯을지--감이-아니라-측정으로) 참고.
타입 32개 분리 → `partial` 역할 분할 → 의존도 측정 → 서비스 2개 추출.
본체는 **378줄**이 됐습니다.

**남은 문제:** 파일은 9개로 나뉘었지만 **클래스는 여전히 하나**입니다.
`partial` 은 결합도를 낮추지 못하므로 God Object 자체는 그대로입니다.
특히 `Progression`(1,178줄)은 `saveData` 에 깊게 묶여 있어,
그 안의 인덱스들은 **`MonoBehaviour` 안에 있다는 이유만으로 테스트할 수 없습니다.**

**계획:** `saveData` 를 감싸는 `GameState` 를 만들어 주입하고 `ProgressionService` 를 추출합니다.
측정에서 `Player` 그룹의 독점 필드가 12%로 가장 낮게 나왔으므로, 캐릭터 교체 쪽은 마지막에 다룹니다.

### UI 관리자가 비어 있다

MVP 인프라(인터페이스 33개 + Model + Presenter)는 만들어 뒀는데,
이를 조립하고 팝업의 생성·소멸을 관리하는 계층이 없습니다.

**왜 이렇게 됐나:** 기존 팝업이 이미 `GameManager` 의 풀 API로 동작하고 있었고,
새 구조로 전부 옮기는 것보다 기능 추가가 급했습니다.
`UIManager` 를 만들어 두긴 했지만 전환을 미루면서 통째로 주석 처리해 뒀고,
1년 넘게 방치된 끝에 결국 제거했습니다.

남은 건 **인프라만 있고 조립하는 쪽이 없는 상태**입니다.
Presenter가 `MonoBehaviour` 를 상속하지 않도록 설계해 둔 것은 살아 있으므로,
관리자만 다시 세우면 이관을 시작할 수 있습니다.

**배운 것:** "일단 만들어두고 나중에 옮기자"는 대부분 옮겨지지 않습니다.
새 구조를 도입할 거라면 **신규 작업분만이라도 즉시 그 경로로 태워야** 합니다.
그러지 않으면 인프라만 남고 아무도 쓰지 않는 상태가 됩니다.

**계획:** UI 전용 관리자를 다시 만들고, 신규 팝업부터 MVP 경로로 태웁니다.

### 테스트 범위가 아직 좁다

EditMode 테스트 31개가 생겼지만, **전부 `MonoBehaviour` 밖으로 꺼낸 코드에만** 붙어 있습니다.

| 대상 | 테스트 |
|---|---|
| `LocalizationService` | 10개 |
| `ObjectPoolService` | 13개 |
| `TableManager` 인덱스 | 7개 |
| 풀 조회 A/B 벤치마크 | 1개 |

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

### 주석 처리된 코드 2,023줄

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

832줄을 삭제했고, 남은 2,023줄은 대안 구현·실험 흔적이라 파일별 판단이 필요합니다.

---

## 마치며

16개월간 혼자 개발하면서, 기능을 만드는 것보다
**이전에 내린 결정이 나중에 발목을 잡는 순간**이 더 많았습니다.

- `bool` 로 시작한 개념은 대부분 등급이나 축으로 쪼개졌습니다 ([1](#1-방어-타입을-7단계까지-나눈-이유), [6](#6-함정에-두-번-맞는다--무적-관통-공격의-사각))
- "빠뜨릴 수 있는 구조"는 결국 빠뜨렸습니다 ([5](#5-새-게임인데-이전-유물이-남아-있다), [11](#11-코루틴을-버리고-unitask로))
- 재사용하는 오브젝트는 이전 상태를 물려받았습니다 ([4](#4-부활-후-미사일--재현이-안-되던-고질적-버그))
- 반복 작업을 툴로 옮긴 결정은 예외 없이 남는 이득이었습니다 ([8](#8-룸을-손으로-배치할-수-없게-되었을-때))

구조를 정리하면서는 다른 것을 배웠습니다.

- 어디부터 뜯을지는 **감이 아니라 측정**으로 정해야 했습니다 ([12](#12-god-object를-어디부터-뜯을지--감이-아니라-측정으로))
- 느린 원인은 알고리즘이 아니라 **엔진 API의 숨은 비용**이었습니다 ([13](#13-520배-빨라진-이유는-알고리즘이-아니었다))
- 자료구조를 바꾸자 **그 제약이 데이터를 검사해줬습니다** ([14](#14-dictionary로-바꿨더니-데이터-버그가-나왔다))
- 분리해야 검증할 수 있고, **검증하려다 보면 문제가 드러났습니다**

[15번](#15-아직-풀지-못한-문제들)에 적은 것들은
**혼자였기 때문에 미룰 수 있었던 부채**입니다.
God Object도 이원화된 팝업 경로도, 나 혼자 알고 있으면 굴러갔습니다.

지금 가장 하고 싶은 작업은 새 기능이 아니라 15번 목록을 지우는 일입니다.

---

- 시스템 설계 상세: [`ARCHITECTURE.md`](ARCHITECTURE.md)
- 프로젝트 개요: [`../README.md`](../README.md)
