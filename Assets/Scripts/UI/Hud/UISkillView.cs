using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public interface IUISkillView
{
    void SetSkillInfo(KeyCode keyCode, string skillId, List<float> coolTime = null, List<float> maxCoolTime = null);
    void UpdateCoolTimeText(List<float> coolTime, List<float> maxCoolTime);
    event Action OnSkillDropped;
}

public class UISkillModel
{
    public SettingSkill changeSkill;
    public SettingSkill potionSkill;
    public List<SettingSkill> settingSkillList = new List<SettingSkill>();
}

// 스킬 슬롯 · 교체 · 물약 View 를 하나의 모델로 조율한다.
// View 하나는 자기 슬롯만 알기 때문에, 스킬이 바뀌어 전체를 다시 그려야 할 때
// 그 판단을 내릴 수 있는 것이 없다. 이 Presenter 가 그 역할을 한다.
public class UISkillPresenter
{
    private readonly IUISkillView _changeView;
    private readonly IUISkillView _potionView;
    private readonly List<IUISkillView> _views;

    // 모델을 "어떻게 만드는지"를 주입받는다.
    // 이전에는 Presenter 안에서 GameManager.Instance 를 직접 읽어 새 모델을 만들었다.
    private readonly Func<UISkillModel> _modelSource;
    private UISkillModel _model;

    public UISkillPresenter(IUISkillView changeView, IUISkillView potionView,
                            List<IUISkillView> views, Func<UISkillModel> modelSource)
    {
        _changeView = changeView;
        _potionView = potionView;
        _views = views;
        _modelSource = modelSource;
        _model = modelSource();

        _changeView.OnSkillDropped += OnSkillDropped;
        for (int i = 0; i < _views.Count; i++)
            _views[i].OnSkillDropped += OnSkillDropped;
    }
    public void OnSkillDroppedCleanUp()
    {
        _changeView.OnSkillDropped -= OnSkillDropped;
        for (int i = 0; i < _views.Count; i++)
            _views[i].OnSkillDropped -= OnSkillDropped;
    }
    
    private void OnSkillDropped()
    {
        Refresh();
    }

    // 모델을 다시 읽어 표시를 갱신한다 (스킬 교체·키 설정 변경 등)
    public void Refresh()
    {
        RefreshModel();
        // UI 전체 갱신
        SetSkillInfo();
    }
    
    private void RefreshModel() => _model = _modelSource();

    public void SetSkillInfo()
    {
        _changeView.SetSkillInfo(_model.changeSkill.keyCode, _model.changeSkill.skillId, _model.changeSkill.playerSkill.curCoolTime);
        _potionView.SetSkillInfo(_model.potionSkill.keyCode, _model.potionSkill.skillId, _model.potionSkill.playerSkill.curCoolTime);
        
        for (int i = 0; i < _model.settingSkillList.Count; i++)
        {
            var playerSkill = _model.settingSkillList[i].playerSkill;
            if (playerSkill == null)
            {
                _views[i].SetSkillInfo(_model.settingSkillList[i].keyCode, default);
            }
            else
            {
                var settingSkill = _model.settingSkillList[i];
                _views[i].SetSkillInfo(settingSkill.keyCode, settingSkill.skillId, settingSkill.playerSkill.curCoolTime);
            }
        }
    }

    // 모델의 스킬 리스트만큼 순회하며 각 뷰를 업데이트
    public void UpdateSkillCoolTime()
    {
        _changeView.UpdateCoolTimeText(_model.changeSkill.playerSkill.GetRemainingCooldown(), _model.changeSkill.playerSkill.GetMaxCoolTime());
        _potionView.UpdateCoolTimeText(_model.potionSkill.playerSkill.GetRemainingCooldown(), _model.potionSkill.playerSkill.GetMaxCoolTime());
        
        for (int i = 0; i < _model.settingSkillList.Count; i++)
        {
            var skill = _model.settingSkillList[i].playerSkill;
            if(skill == null)
                continue;
            
            _views[i].UpdateCoolTimeText(skill.GetRemainingCooldown(), skill.GetMaxCoolTime());
        }
    }

    public void ResetSkillCoolTime()
    {
        _changeView.UpdateCoolTimeText(_model.changeSkill.playerSkill.ResetCooldown(), _model.changeSkill.playerSkill.GetMaxCoolTime());
        _potionView.UpdateCoolTimeText(_model.potionSkill.playerSkill.ResetCooldown(), _model.potionSkill.playerSkill.GetMaxCoolTime());
        
        for (int i = 0; i < _model.settingSkillList.Count; i++)
        {
            var skill = _model.settingSkillList[i].playerSkill;
            if(skill == null)
                continue;
            
            _views[i].UpdateCoolTimeText(skill.ResetCooldown(), skill.GetMaxCoolTime());
        }
    }
}

public class UISkillView : MonoBehaviour, IUISkillView
{
    private string mySkillId;
    private KeyCode myKeyCode;
    private bool isChanging;
    
    [SerializeField] private Image skillImage;
    [SerializeField] private Image coolTimeImage;
    [SerializeField] private Image stackCoolTimeImage;

    [SerializeField] private TMP_Text skillKey;
    [SerializeField] private TMP_Text coolTimeText;
    [SerializeField] private TMP_Text stackText;
    
    [SerializeField] private GameObject coolTimeObject;
    [SerializeField] private GameObject cantSkillObject;

    public event Action OnSkillDropped;

    // ── 표시 캐시 ──────────────────────────────────────
    // UpdateCoolTimeText 는 매 프레임 불린다.
    // 쿨타임은 소수점 한 자리라 초당 10번만 바뀌는데, 이전에는 프레임마다
    // ToString 으로 문자열을 새로 만들어 버리고 있었다 (슬롯 수만큼 × 60fps).
    // 마지막으로 표시한 값을 들고 있다가 실제로 바뀔 때만 만든다.
    private const int NoCache = int.MinValue;
    private int cachedCoolTimeTenth = NoCache;   // 쿨타임 × 10 (표시 단위)
    private int cachedStack = NoCache;

    // 스택은 한 자리 수라 미리 만들어 두면 할당이 아예 없다
    private static readonly string[] StackNumbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

    // View 는 풀링되어 재사용되므로 다른 스킬이 들어오면 캐시를 버려야 한다.
    // 버리지 않으면 이전 스킬의 표시값과 같다고 판단해 갱신을 건너뛴다.
    private void ClearDisplayCache()
    {
        cachedCoolTimeTenth = NoCache;
        cachedStack = NoCache;
    }

    private void SetCoolTimeText(float value)
    {
        // 표시되는 자리수(0.1)까지만 비교한다
        int tenth = Mathf.RoundToInt(value * 10f);
        if (tenth == cachedCoolTimeTenth)
            return;

        cachedCoolTimeTenth = tenth;
        coolTimeText.text = value.ToString("F1");
    }

    private void SetStackText(int stack)
    {
        if (stack == cachedStack)
            return;

        cachedStack = stack;
        stackText.text = (stack >= 0 && stack < StackNumbers.Length) ? StackNumbers[stack] : stack.ToString();
    }

    private static void SetActiveIfChanged(GameObject target, bool active)
    {
        if (target.activeSelf != active)
            target.SetActive(active);
    }

    public bool IsChangeCharacter()
    {
        return myKeyCode == GameManager.Instance.changeCharacterKey;
    }
    
    public bool IsDash()
    {
        return myKeyCode == GameManager.Instance.dashKey;
    }
    
    public string GetSkillId()
    {
        return mySkillId;
    }

    public Sprite GetSprite()
    {
        return skillImage.sprite;
    }

    public void SetSkillInfo(KeyCode keyCode, string skillId, List<float> coolTime = null, List<float> maxCoolTime = null)
    {
        myKeyCode = keyCode;
        mySkillId = skillId;

        // 슬롯에 다른 스킬이 들어오므로 이전 표시값을 버린다.
        // 남겨두면 값이 같다고 판단해 첫 갱신을 건너뛴다.
        ClearDisplayCache();

        skillKey.text = GameManager.Instance.GetKeyCode(keyCode);
        
        skillImage.gameObject.SetActive(!string.IsNullOrEmpty(skillId));
        coolTimeText.gameObject.SetActive(!string.IsNullOrEmpty(skillId));
        coolTimeObject.SetActive(!string.IsNullOrEmpty(skillId));
        
        if(keyCode != GameManager.Instance.potionKey)
            stackText.gameObject.SetActive(maxCoolTime != null && maxCoolTime.Count > 1);
        
        stackCoolTimeImage.gameObject.SetActive(maxCoolTime != null && maxCoolTime.Count > 1);

        if (string.IsNullOrEmpty(skillId))
            return;

        if (skillId == ConstValues.ChangeCharacter)
        {
            if (GameManager.Instance.PlayerList.Count <= 1)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                
                if(GameManager.Instance.CurPlayer.BasicStat.id == GameManager.Instance.PlayerList[0])
                    skillImage.sprite = GameManager.Instance.GetAtlasSprite($"{GameManager.Instance.PlayerList[1]}_{ConstValues.Face}");
                else if(GameManager.Instance.CurPlayer.BasicStat.id == GameManager.Instance.PlayerList[1])
                    skillImage.sprite = GameManager.Instance.GetAtlasSprite($"{GameManager.Instance.PlayerList[0]}_{ConstValues.Face}");
            }
        }
        else
        {
            skillImage.sprite = GameManager.Instance.GetAtlasSprite(skillId);
        }
    }

    public void UpdateCoolTimeText(List<float> coolTime, List<float> maxCoolTime)
    {
        if (isChanging)
            return;
        
        // 스택형 쿨타임 표시
        if (coolTime.Count > 1)
        {
            int stack = (int)coolTime[2];

            if (maxCoolTime[1] <= 0)
            {
                SetActiveIfChanged(coolTimeImage.gameObject, false);
                SetActiveIfChanged(stackCoolTimeImage.gameObject, false);
                SetActiveIfChanged(coolTimeText.gameObject, false);
                SetActiveIfChanged(coolTimeObject, false);
                SetActiveIfChanged(stackText.gameObject, stack > 0);
                SetStackText(stack);
                SetActiveIfChanged(cantSkillObject, stack == 0);
            }
            else
            {
                // 모든 스택을 소모하지 않았다면, 기본 쿨타임을 보여준다
                var finalCoolTime = maxCoolTime[0] - coolTime[0];
                var finalStackCoolTime = maxCoolTime[1] - coolTime[1];

                if (stack > 0)
                {
                    SetActiveIfChanged(coolTimeText.gameObject, finalCoolTime > 0);
                    SetActiveIfChanged(coolTimeObject, finalCoolTime > 0);
                    SetCoolTimeText(finalCoolTime);
                    coolTimeImage.fillAmount = finalCoolTime / maxCoolTime[0];
                }
                // 모든 스택을 소모하였다면, 스택 쿨타임을 기본 쿨타임으로 보여준다
                else
                {
                    SetActiveIfChanged(coolTimeText.gameObject, finalStackCoolTime > 0);
                    SetActiveIfChanged(coolTimeObject, finalStackCoolTime > 0);
                    SetCoolTimeText(finalStackCoolTime);
                    coolTimeImage.fillAmount = finalStackCoolTime / maxCoolTime[1];
                }

                // 기본 쿨타임이 돌아가는동안은 스택 쿨타임이 보이지 않는다.
                SetActiveIfChanged(stackText.gameObject, stack > 0);
                SetActiveIfChanged(stackCoolTimeImage.gameObject, finalCoolTime <= 0 && stack > 0);

                SetStackText(stack);
                stackCoolTimeImage.fillAmount = finalStackCoolTime / maxCoolTime[1];

                SetActiveIfChanged(cantSkillObject, false);
            }
        }
        // 일반형 쿨타임 표시, 기본 쿨타임을 보여준다
        else
        {
            var finalCoolTime = maxCoolTime[0] - coolTime[0];
            SetActiveIfChanged(coolTimeText.gameObject, finalCoolTime > 0);
            SetActiveIfChanged(coolTimeObject, finalCoolTime > 0);
            SetCoolTimeText(finalCoolTime);
            coolTimeImage.fillAmount = finalCoolTime / maxCoolTime[0];

            SetActiveIfChanged(stackText.gameObject, false);
            SetActiveIfChanged(stackCoolTimeImage.gameObject, false);
            SetActiveIfChanged(cantSkillObject, false);
        }
    }

    public void ExecuteSkillAction(string skillId)
    {
        GameManager.Instance.SetSkillId(myKeyCode, skillId);
        OnSkillDropped?.Invoke();  // Presenter에게 알림
    }
}
