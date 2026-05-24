using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class InteractionSelect : MonoBehaviour
{
    [SerializeField] private TMP_Text selectKey;
    [SerializeField] private TMP_Text[] selectTexts;
    
    private Vector3 basicScale = Vector3.one;
    private Vector3 expansionScale = new Vector3(1.05f, 1.05f, 1.05f);
    private float delay = 0.2f;
    private float duration = 0.1f;

    private bool choiceReady;
    [SerializeField] private int currentIdx;
    private List<int> talkIdxList = new List<int>();
    private List<string> choiceIdList = new List<string>();

    private Tween[] expansionTween;
    private Action<string> interactionAction;
    private Action closeAction;
    private CancellationTokenSource waitCancellation;

    private void Awake()
    {
        selectKey.text = GameManager.Instance.GetKeyCode(GameManager.Instance.spaceKey);
    }

    private void OnEnable()
    {
        ChoiceDelay();
    }

    private void Update()
    {
        if(!choiceReady)
            return;
        
        if (Input.GetKeyDown(GameManager.Instance.upKey))
        {
            CurrentIdxMinus();
            SelectChoice(currentIdx);
            SoundManager.Instance.PlaySound(ConstValues.Popup);
        }
        if (Input.GetKeyDown(GameManager.Instance.downKey))
        {
            CurrentIdxPlus();
            SelectChoice(currentIdx);
            SoundManager.Instance.PlaySound(ConstValues.Popup);
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            interactionAction.Invoke(choiceIdList[currentIdx]);
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            closeAction.Invoke();
        }
    }

    private async void ChoiceDelay()
    {
        choiceReady = false;
        
        waitCancellation = new CancellationTokenSource();
        if (await NormalDelay(delay, waitCancellation).SuppressCancellationThrow())
            return;
        
        choiceReady = true;
    }
    
    private async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }

    private void CurrentIdxPlus()
    {
        currentIdx++;
        if (currentIdx > talkIdxList.Count - 1)
            currentIdx = 0;
    }
    private void CurrentIdxMinus()
    {
        currentIdx--;
        if (currentIdx < 0)
            currentIdx = talkIdxList.Count - 1;
    }

    public void SetDelay()
    {
        currentIdx = 0;
        SelectChoice(currentIdx);
    }

    public void SetAction(Action<string> interaction, Action close)
    {
        interactionAction = interaction;
        closeAction = close;
    }
    
    public void StartSetting(List<int> selectList, List<string> idList)
    {
        currentIdx = 0;
        talkIdxList = selectList;
        choiceIdList = idList;
        expansionTween = new Tween[selectTexts.Length];

        foreach (var selectText in selectTexts)
        {
            selectText.color = ConstValues.WhiteColor;
            selectText.transform.localScale = Vector3.one;
            selectText.gameObject.SetActive(false);
        }

        for (int i = 0; i < talkIdxList.Count; i++)
        {
            selectTexts[i].text = GameManager.Instance.GetTalk(talkIdxList[i]);
            selectTexts[i].gameObject.SetActive(true);
        }

        SelectChoice(currentIdx);
    }

    public void RefreshTalk()
    {
        for (int i = 0; i < talkIdxList.Count; i++)
            selectTexts[i].text = GameManager.Instance.GetTalk(talkIdxList[i]);
    }

    private void SelectChoice(int idx)
    {
        for (int i = 0; i < expansionTween.Length; i++)
        {
            if (i == idx)
            {
                Expansion(i);
            }
            else
            {
                Reduce(i);
            }
        }
    }

    private void Expansion(int idx)
    {
        selectTexts[idx].color = ConstValues.YellowColor;
        if (selectTexts[idx].transform.localScale == expansionScale)
            return;
        
        if(expansionTween[idx] != null)
            expansionTween[idx].Kill();
        
        expansionTween[idx] = selectTexts[idx].transform.DOScale(expansionScale, duration).SetEase(Ease.Linear);
    }

    private void Reduce(int idx)
    {
        selectTexts[idx].color = ConstValues.WhiteColor;
        if (selectTexts[idx].transform.localScale == basicScale)
            return;
        
        if(expansionTween[idx] != null)
            expansionTween[idx].Kill();
        
        expansionTween[idx] = selectTexts[idx].transform.DOScale(basicScale, duration).SetEase(Ease.Linear);
    }
}
