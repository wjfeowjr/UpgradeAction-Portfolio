using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presenter 안쪽(클래스 상단)에 추가
public enum UpgradeTarget
{
    Plus,
    Minus
}

public interface IPopupAttributeView
{
    void SetModel(SkillCollection playerSkill, string character);
    void SetAction(Action closeAction);

    void CloseAction();

    // ▼ 추가: Presenter가 View의 상태에 접근/지시하기 위한 API
    int GetActiveItemCount();

    void MoveSelectTo(int index, float tweenTime = 0.1f); // DoTween으로 selectFrame 이동

    // 🔽 추가: 스크롤 이동용
    // 변경: +1/-1 방향 대신 "변화한 행 수(rowSteps)"로 스크롤
    void ScrollContentRows(int rowSteps); // 양수: 아래로 rowSteps행, 음수: 위로 rowSteps행

    // 🔽 추가: Presenter가 유효 타겟 여부를 판단하기 위한 API
    int GetFrameCount(); // attributeFrameArray.Length
    bool IsNavigable(int index); // index 범위 내 && attributeFrameArray[index].isHaveSkill == true

    // ▼ 추가: 업그레이드 모드 시 필요 API
    void SetUpgradeActive(int index, bool active); // index의 upgradeFrame On/Off
    void PositionUpgradeImit(int index, UpgradeTarget target);
    void PositionUpgradeTo(int index, UpgradeTarget target, float t = 0.1f); // upgradeFrame을 plus/minus 위치로 트윈
    bool ShouldStartOnMinus(int index); // 규칙 3 판단용
    void RefreshSkillInfo(int index); // SetSkillInfo() 래핑

    // ▼ 추가: Reset 모드용
    void SetResetActive(bool active); // resetFrame On/Off
    void RefreshAllSkillInfo(); // attributeFrameArray 전체에 SetSkillInfo()

    void LevelUp(int index);
    void LevelDown(int index);
    
    // Reset/업그레이드 등 특수 모드에서 커서를 숨기거나 다시 보이게
    void SetSelectFrameActive(bool active);
}

public class PopupAttributeModel
{
    public SkillCollection playerSkill;
    public Action closeAction;
}

public class PopupAttributePresenter
{
    private IPopupAttributeView _attributeView;
    private PopupAttributeModel _model;

    // ▼ 추가 상태
    private int _currentIndex = 0;
    private const int _cols = 3; // 3열 고정

    // 키 홀드(네비게이션) 상태
    private bool _holding = false;
    private KeyCode _holdKey;
    private float _repeatTimer = 0f;
    private const float _firstDelay = 0.3f; // 최초 지연
    private const float _repeatDelay = 0.2f; // 반복 지연

    // 업그레이드 모드 상태
    private bool _inUpgradeMode = false;
    private UpgradeTarget _upgradeTarget = UpgradeTarget.Plus; // 기본 plus

    // ▼ 추가: Reset 모드
    private bool _inResetMode = false;


    public PopupAttributePresenter(IPopupAttributeView guideView, PopupAttributeModel model)
    {
        _attributeView = guideView;
        _model = model;
    }

    private void CloseAction()
    {
        _attributeView.CloseAction();
    }

    public void CloseAttribute()
    {
        if (Input.GetKeyDown(KeyCode.I) || (!_inUpgradeMode && Input.GetKeyDown(KeyCode.Escape)))
            CloseAction();
    }

    public void SetModel(SkillCollection playerSkill)
    {
        _model.playerSkill = playerSkill;
        _attributeView.SetModel(_model.playerSkill, GameManager.Instance.CurPlayer.name);

        _currentIndex = SnapToNearestValid(0, +1);
        if (_currentIndex >= 0)
        {
            _attributeView.MoveSelectTo(_currentIndex, 0f);
        }

        // 시작 시 모드 패널 Off
        _attributeView.SetUpgradeActive(_currentIndex, false);
        _attributeView.SetResetActive(false);
    }

    public void SetAction(Action closeAction)
    {
        _model.closeAction = closeAction;
        _attributeView.SetAction(_model.closeAction);
    }

    // ▼ 매 프레임 호출: 방향키 네비게이션 처리 (Unscaled)
    public void UpdateNavigation()
    {
        // ===== Reset 모드 중 =====
        if (_inResetMode)
        {
            // Enter → Reset 실행
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                string characterName = GameManager.Instance.CurPlayer.name;
                GameManager.Instance.PlayerSkill.ResetAttribute(characterName);
                SoundManager.Instance.PlaySoundNotCondition(ConstValues.NormalButton2);
                
                // 모든 프레임 최신화
                _attributeView.RefreshAllSkillInfo();
                ExitResetMode();
                return;
            }

            // Esc → 취소
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                SoundManager.Instance.PlaySoundNotCondition(ConstValues.NormalButton);
                ExitResetMode();
                return;
            }

            // Reset 모드에선 방향키/기타 입력 무시 (잠금)
            return;
        }

        // ===== 업그레이드 모드 중 =====
        if (_inUpgradeMode)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitUpgradeMode();
                SoundManager.Instance.PlaySoundNotCondition(ConstValues.NormalButton2);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_upgradeTarget == UpgradeTarget.Plus)
                    _attributeView.LevelUp(_currentIndex);
                else
                    _attributeView.LevelDown(_currentIndex);

                _attributeView.RefreshSkillInfo(_currentIndex);
                SoundManager.Instance.PlaySoundNotCondition(ConstValues.NormalButton2);
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (_upgradeTarget != UpgradeTarget.Minus)
                {
                    _upgradeTarget = UpgradeTarget.Minus;
                    _attributeView.PositionUpgradeTo(_currentIndex, _upgradeTarget, 0.1f);
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (_upgradeTarget != UpgradeTarget.Plus)
                {
                    _upgradeTarget = UpgradeTarget.Plus;
                    _attributeView.PositionUpgradeTo(_currentIndex, _upgradeTarget, 0.1f);
                }

                return;
            }

            return; // 업그레이드 모드에선 선택 이동 잠금
        }

        // ===== 일반 모드 (선택 이동 가능) =====

        // Enter → 업그레이드 모드 진입
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (_attributeView.IsNavigable(_currentIndex))
                EnterUpgradeMode();
            return;
        }

        // 🔸 "첫 번째 열에서 ↑" → Reset 모드 진입
        // 첫 번째 열 판정: (index % _cols == 0)
        if (Input.GetKeyDown(KeyCode.UpArrow) && (_currentIndex % _cols == 0))
        {
            EnterResetMode();
            return;
        }

        // 기존 네비게이션 (좌/우/상/하)
        if (Input.GetKeyDown(KeyCode.LeftArrow)) StartHold(KeyCode.LeftArrow, -1);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) StartHold(KeyCode.RightArrow, +1);
        else if (Input.GetKeyDown(KeyCode.UpArrow)) StartHold(KeyCode.UpArrow, -_cols);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) StartHold(KeyCode.DownArrow, +_cols);

        if (_holding)
        {
            if (Input.GetKey(_holdKey))
            {
                _repeatTimer -= Time.unscaledDeltaTime;
                if (_repeatTimer <= 0f)
                {
                    Step(_holdKey);
                    _repeatTimer = _repeatDelay;
                }
            }
            else _holding = false;
        }
    }

    // ==== Reset 모드 진입/해제 ====
    private void EnterResetMode()
    {
        _inResetMode = true;
        _attributeView.SetResetActive(true);
        _attributeView.SetSelectFrameActive(false); // ⬅️ 커서 숨김
        // 보조: 업그레이드 패널은 꺼 둔다
        _attributeView.SetUpgradeActive(_currentIndex, false);
        SoundManager.Instance.PlaySoundNotCondition(ConstValues.NormalButton);
    }

    private void ExitResetMode()
    {
        _inResetMode = false;
        _attributeView.SetResetActive(false);
        _attributeView.SetSelectFrameActive(true);  // ⬅️ 커서 복원
    }

    // ==== 업그레이드 모드 진입/해제 (기존) ====
    private void EnterUpgradeMode()
    {
        _inUpgradeMode = true;

        // 규칙 2: 선택된 프레임의 upgradeFrame만 활성
        _attributeView.SetUpgradeActive(_currentIndex, true);

        // 규칙 3: 기본 plus, 단 ShouldStartOnMinus면 minus에서 시작
        _upgradeTarget = _attributeView.ShouldStartOnMinus(_currentIndex) ? UpgradeTarget.Minus : UpgradeTarget.Plus;
        _attributeView.PositionUpgradeImit(_currentIndex, _upgradeTarget);
        
        SoundManager.Instance.PlaySoundNotCondition(ConstValues.NormalButton2);
    }

    private void ExitUpgradeMode()
    {
        _inUpgradeMode = false;
        _attributeView.SetUpgradeActive(_currentIndex, false); // 비활성화
    }

    private void StartHold(KeyCode key, int delta)
    {
        if (TryMove(delta, out _))
        {
            _holding = true;
            _holdKey = key;
            _repeatTimer = _firstDelay;
        }
        else _holding = false;
    }

    private void Step(KeyCode key)
    {
        int delta = 0;
        if (key == KeyCode.LeftArrow)
            delta = -1;
        else if (key == KeyCode.RightArrow)
            delta = +1;
        else if (key == KeyCode.UpArrow)
            delta = -_cols;
        else if (key == KeyCode.DownArrow)
            delta = +_cols;

        TryMove(delta, out _);
    }

    // ====== 아래 이동/래핑/스냅 유효칸 탐색은 기존 구현을 재사용 ======
    private bool TryMove(int delta, out int rowSteps)
    {
        rowSteps = 0;
        int count = _attributeView.GetFrameCount();
        if (count <= 0) return false;

        int prev = _currentIndex;

        if (!_attributeView.IsNavigable(prev))
        {
            int snapped = SnapToNearestValid(prev, +1);
            if (snapped < 0)
                return false;
            prev = _currentIndex = snapped;
            _attributeView.MoveSelectTo(_currentIndex, 0f);
        }

        bool vertical = (delta == _cols) || (delta == -_cols);
        int target = -1;

        if (vertical)
        {
            int proposed = prev + delta;
            if (proposed < 0)
                target = SnapToNearestValid(0, +1);
            else if (proposed >= count)
                target = SnapToNearestValid(count - 1, -1);
            else
                target = FindNextValid(prev, delta, count);
        }
        else
        {
            // 좌/우: 먼저 방향 검색 → 없으면 래핑(좌: 마지막 유효칸 / 우: 첫 유효칸)
            target = FindNextValid(prev, delta, count);
            if (target < 0)
                target = (delta == -1) ? GetLastValidIndex() : GetFirstValidIndex();
        }

        if (target < 0 || target == prev)
            return false;

        int prevRow = prev / _cols;
        int nextRow = target / _cols;
        rowSteps = nextRow - prevRow;

        _currentIndex = target;
        _attributeView.MoveSelectTo(_currentIndex);
        if (rowSteps != 0)
            _attributeView.ScrollContentRows(rowSteps);

        // 일반 모드에서는 upgradeFrame 항상 꺼 둠(규칙 2 충족: 선택되지 않은 프레임은 비활성)
        _attributeView.SetUpgradeActive(_currentIndex, false);
        SoundManager.Instance.PlaySoundNotCondition(ConstValues.NormalButton);
        return true;
    }

    // start에서 step(±1 또는 ±_cols)로 진행하며 첫 유효 칸을 찾는다.
    private int FindNextValid(int start, int step, int count)
    {
        int i = start + step;
        while (i >= 0 && i < count)
        {
            if (_attributeView.IsNavigable(i))
                return i;
            i += step;
        }

        return -1;
    }

    // 기준점에서 dir(+1: 앞으로, -1: 뒤로)로 진행하며 가장 가까운 유효 칸을 찾는다.
    // 기준점 자체가 유효하면 그 자체를 반환.
    private int SnapToNearestValid(int pivot, int dir)
    {
        int count = _attributeView.GetFrameCount();
        if (count <= 0)
            return -1;

        if (pivot >= 0 && pivot < count && _attributeView.IsNavigable(pivot))
            return pivot;

        if (dir >= 0)
        {
            for (int i = Mathf.Max(0, pivot); i < count; i++)
                if (_attributeView.IsNavigable(i))
                    return i;
        }

        for (int i = Mathf.Min(count - 1, pivot); i >= 0; i--)
            if (_attributeView.IsNavigable(i))
                return i;

        return -1;
    }

    private int GetFirstValidIndex()
    {
        int count = _attributeView.GetFrameCount();
        for (int i = 0; i < count; i++)
            if (_attributeView.IsNavigable(i))
                return i;
        return -1;
    }

    private int GetLastValidIndex()
    {
        int count = _attributeView.GetFrameCount();
        for (int i = count - 1; i >= 0; i--)
            if (_attributeView.IsNavigable(i))
                return i;
        return -1;
    }
}


public class PopupAttributeView : MonoBehaviour, IPopupAttributeView
{
    private string targetCharacter;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text leftPointText;
    [SerializeField] private TMP_Text leftPoint;
    [SerializeField] private TMP_Text resetText;
    [SerializeField] private Button closeButton;
    [SerializeField] private RectTransform content;
    [SerializeField] private Image selectFrame;
    [SerializeField] private GameObject resetFrame;
    [SerializeField] private AttributeFrame[] attributeFrameArray;

    private Tweener moveTween;
    private Tweener scrollTween;
    private Tweener upgradeTween;

    private Action closeAction;
    private SkillCollection skillCollection;

    public int GetFrameCount() => attributeFrameArray?.Length ?? 0;

    // isHaveSkill 여부로 네비게이션 가능성 판단
    public bool IsNavigable(int index)
    {
        if (attributeFrameArray == null || index < 0 || index >= attributeFrameArray.Length)
            return false;

        var f = attributeFrameArray[index];
        // 필요하면 activeSelf도 조건에 추가 가능:
        // return f.gameObject.activeSelf && f.isHaveSkill;
        return f != null && f.isHaveSkill;
    }

    public async void SetModel(SkillCollection playerSkill, string character)
    {
        skillCollection = playerSkill;

        titleText.text = "특성";
        leftPointText.text = "남은 포인트";
        leftPoint.text = skillCollection.berserkerSkillSetting.attributePoint.ToString();
        resetText.text = "초기화";

        foreach (var attributeFrame in attributeFrameArray)
            attributeFrame.gameObject.SetActive(false);

        switch (character)
        {
            case ConstValues.Berserker:
                for (int i = 0; i < skillCollection.berserkerSkillSetting.skillList.Count; i++)
                {
                    attributeFrameArray[i].gameObject.SetActive(true);
                    attributeFrameArray[i].SetSkillInfo(skillCollection.berserkerSkillSetting.skillList[i].skillId);
                }

                break;

            case ConstValues.Gunner:
                for (int i = 0; i < skillCollection.gunnerSkillSetting.skillList.Count; i++)
                {
                    attributeFrameArray[i].gameObject.SetActive(true);
                    attributeFrameArray[i].SetSkillInfo(skillCollection.gunnerSkillSetting.skillList[i].skillId);
                }

                break;
        }

        // 자동으로 맨 위쪽 스크롤로 맞추고, 그곳에 따라서 셀렉트 프레임의 위치를 재설정
        await UniTask.Yield();
        content.anchoredPosition = new Vector2(0, -content.sizeDelta.y);

        await UniTask.Yield();
        var targetRT = attributeFrameArray[0].GetComponent<RectTransform>();
        var frameRT = selectFrame.GetComponent<RectTransform>();
        frameRT.anchoredPosition = targetRT.anchoredPosition;
    }

    public int GetActiveItemCount()
    {
        int count = 0;
        foreach (var f in attributeFrameArray)
            if (f.gameObject.activeSelf)
                count++;
        return count;
    }

    public void MoveSelectTo(int index, float tweenTime = 0.1f)
    {
        if (!IsNavigable(index))
            return;

        var targetRT = attributeFrameArray[index].GetComponent<RectTransform>();
        var frameRT = selectFrame.rectTransform;

        moveTween?.Kill();
        moveTween = frameRT.DOAnchorPos(targetRT.anchoredPosition, tweenTime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        
        if(tweenTime > 0)
            SoundManager.Instance.PlaySoundNotCondition(ConstValues.NormalButton);
    }

    // 🔽 새로 추가되는 부분
    // ⬇️ 추가/변경: 행 수(rowSteps)만큼 스크롤
    public void ScrollContentRows(int rowSteps)
    {
        if (rowSteps == 0 || content == null || selectFrame == null) return;

        float oneRow = selectFrame.GetComponent<RectTransform>().sizeDelta.y;
        Vector2 target = content.anchoredPosition;
        target.y += oneRow * rowSteps;

        scrollTween?.Kill();
        scrollTween = content.DOAnchorPos(target, 0.2f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    // ---------- 업그레이드 모드 관련 구현 ----------
    public void SetUpgradeActive(int index, bool active)
    {
        if (attributeFrameArray == null)
            return;

        // 전체 끄고, 선택 index만 켜기
        for (int i = 0; i < attributeFrameArray.Length; i++)
        {
            var f = attributeFrameArray[i];
            if (f == null) continue;
            f.SetUpgradeFrameActive(i == index && active);
        }
    }

    public void PositionUpgradeImit(int index, UpgradeTarget target)
    {
        if (attributeFrameArray == null || index < 0 || index >= attributeFrameArray.Length)
            return;

        var f = attributeFrameArray[index];
        if (f == null)
            return;

        var frameRT = f.upgradeFrame; // 업그레이드 핸들(하이라이트) RectTransform
        var plusRT = f.plusButton;
        var minusRT = f.minusButton;

        if (frameRT == null || plusRT == null || minusRT == null)
            return;

        Vector2 dest = (target == UpgradeTarget.Plus) ? plusRT.anchoredPosition : minusRT.anchoredPosition;
        frameRT.anchoredPosition = dest;
    }

    public void PositionUpgradeTo(int index, UpgradeTarget target, float t = 0.1f)
    {
        if (attributeFrameArray == null || index < 0 || index >= attributeFrameArray.Length)
            return;

        var f = attributeFrameArray[index];
        if (f == null)
            return;

        var frameRT = f.upgradeFrame; // 업그레이드 핸들(하이라이트) RectTransform
        var plusRT = f.plusButton;
        var minusRT = f.minusButton;

        if (frameRT == null || plusRT == null || minusRT == null)
            return;

        Vector2 dest = (target == UpgradeTarget.Plus) ? plusRT.anchoredPosition : minusRT.anchoredPosition;

        upgradeTween?.Kill();
        upgradeTween = frameRT.DOAnchorPos(dest, t)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        
        if(t > 0)
            SoundManager.Instance.PlaySoundNotCondition(ConstValues.NormalButton);
    }

    // 규칙 3: "기본은 plus, 단 attributeData[level]와 같은 레벨이면 minus"
    public bool ShouldStartOnMinus(int index)
    {
        if (attributeFrameArray == null || index < 0 || index >= attributeFrameArray.Length) return false;
        var f = attributeFrameArray[index];
        if (f == null)
            return false;

        return f.ShouldStartOnMinus();
    }

    public void LevelUp(int index)
    {
        if (attributeFrameArray == null || index < 0 || index >= attributeFrameArray.Length)
            return;

        var f = attributeFrameArray[index];
        if (f == null)
            return;

        f.AttributeLvUp();
        leftPoint.text = skillCollection.berserkerSkillSetting.attributePoint.ToString();
    }

    public void LevelDown(int index)
    {
        if (attributeFrameArray == null || index < 0 || index >= attributeFrameArray.Length)
            return;

        var f = attributeFrameArray[index];
        if (f == null)
            return;

        f.AttributeLvDown();
        leftPoint.text = skillCollection.berserkerSkillSetting.attributePoint.ToString();
    }

    public void RefreshSkillInfo(int index)
    {
        if (attributeFrameArray == null || index < 0 || index >= attributeFrameArray.Length)
            return;

        var f = attributeFrameArray[index];
        if (f == null)
            return;

        f.SetSkillInfo(attributeFrameArray[index].id); // AttributeFrame 내부의 기존 갱신 루틴 사용
    }

    // Reset 패널 On/Off
    public void SetResetActive(bool active)
    {
        if (resetFrame != null)
            resetFrame.SetActive(active);
    }

    // 전체 프레임 최신화
    public void RefreshAllSkillInfo()
    {
        if (attributeFrameArray == null)
            return;

        for (int i = 0; i < attributeFrameArray.Length; i++)
        {
            var f = attributeFrameArray[i];
            if (f != null)
                f.SetSkillInfo(f.id);
        }
        leftPoint.text = skillCollection.berserkerSkillSetting.attributePoint.ToString();
    }
    
    public void SetSelectFrameActive(bool active)
    {
        if (selectFrame != null)
            selectFrame.gameObject.SetActive(active);
    }

    // CloseAction/SetAction/SetModel 등 기존 메서드 유지
    public void SetAction(Action action)
    {
        closeAction = action;
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => { closeAction(); });
    }

    public void CloseAction()
    {
        Time.timeScale = 1.0f;
        closeAction();
    }
}