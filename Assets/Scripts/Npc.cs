using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

interface IFirstDialogAction
{
    public UniTask DialogStartAction();
    public void DialogEndAction();
}

public class Npc : Character
{
    [SerializeField] private NpcInfo npcInfo;
    [SerializeField] private Npc[] anotherNpc;
    
    private NpcData npcData;
    private bool isFirstTalk;

    protected override void OnEnable()
    {
        base.OnEnable();
        DataSetting();
    }
    
    protected override void Update()
    {
        if (isDie || basicStat.hp <= 0 || !GameManager.Instance.ControlStart)
            return;

        base.Update();
    }

    private void DataSetting()
    {
        if(npcData == null)
            npcData = TableManager.Instance.npcTable.Npc.Find(x => x.id == name);
    }

    public void SetInteractionAction()
    {
        SetInteractionAction(StartDialogue, GameManager.Instance.GetTalk(30016), GameManager.Instance.GetKeyCode(GameManager.Instance.interactionKey));
    }

    public void SetAnotherNpc(Npc[] npc)
    {
        anotherNpc = npc;
    }
    
    public void SetStartTalkAction()
    {
        isFirstTalk = false;
    }

    public void AddData()
    {
        var data = GameManager.Instance.NpcInfoInfoList.Find(x => x.id == name);
        if (data == null)
        {
            NpcInfo npc = new NpcInfo();
            npc.id = name;
            npc.dialogKey.id = npcData.dialogKey;
            npc.isFirstDialogFinish = string.IsNullOrWhiteSpace(npcData.firstDialog);
            GameManager.Instance.NpcInfoInfoList.Add(npc);
            npcInfo = GameManager.Instance.NpcInfoInfoList.Find(x => x.id == name);
        }
        else
        {
            npcInfo = data;
            if (string.IsNullOrWhiteSpace(npcData.firstDialog))
                npcInfo.isFirstDialogFinish = true;
        }
    }

    private async void SetDialogueAction(string choice)
    {
        ActiveInteractionSelect(false);

        var choiceSplit = choice.Split('_');
        var choiceType = choiceSplit[0];

        if (choiceType == ConstValues.Open)
        {
            string popupId = choiceSplit[1];
            Debug.Log($"{popupId} 팝업이 뜬다!");

            // TODO: Popup_Store 생성 및 MVP 초기화
            var uiBase = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Store, Vector3.zero).GetComponent<UIBase>();
            uiBase.ExpansionOpen(true, true);
            
            if (uiBase is Popup_Store popupStore)
            {
                var storeInterface = popupStore.StoreView.ConvertTo<IPopupStoreView>();
                
                var common = new PopupCommonActions
                {
                    PlayMoveSound   = () => SoundManager.Instance.PlaySound(ConstValues.Jump1,         true),
                    PlaySelectSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton2,  true),
                    PlayCancelSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton,   true),
                };
                var storeModel = new PopupStoreModel()
                {
                    commonActions = common,
                    closeAction = () =>
                    {
                        uiBase.ReductionClose(true, true);
                        SpawnInteractionObject();
                    }
                };
                
                var storePresenter = new PopupStorePresenter(storeInterface, storeModel);
                popupStore.SetStorePresenter(storePresenter);
                storePresenter.SetModel(popupId);
                storePresenter.SetItem();
                storePresenter.SetAction();
            }
        }
        else
        {
            await GameManager.Instance.NpcDialogue(choice, anotherNpc, npcInfo, SpawnInteractionObject);
        }
    }
    
    private async UniTask SetFirstDialogueAction(string choice)
    {
        ActiveInteractionSelect(false);
        await GameManager.Instance.NpcDialogue(choice, anotherNpc, npcInfo, SpawnInteractionObject);
        
    }
    
    private void SetCloseAction()
    {
        ActiveInteractionSelect(false);
        GameManager.Instance.ControlStart = true;
        SpawnInteractionObject();
    }

    protected virtual async void StartDialogue()
    {
        ReduceInteractionObject();
        GameManager.Instance.InitProductCancellation();
        GameManager.Instance.ControlStart = false;
        
        // 최초응답
        if (!npcInfo.isFirstDialogFinish)
        {
            var actionInterface = GetComponent<IFirstDialogAction>();
            if (actionInterface != null)
                await actionInterface.DialogStartAction();

            await SetFirstDialogueAction($"{name}_{ConstValues.First}");

            actionInterface?.DialogEndAction();

            npcInfo.isFirstDialogFinish = true;
            GameManager.Instance.SaveGame();
        }
        else
        {
            if (!isFirstTalk)
            {
                await GameManager.Instance.NpcFirstTalk(npcData.startDialog, speechPos);
                isFirstTalk = true;
            }
            SetActionInteractionSelect(SetDialogueAction, SetCloseAction);
        }
    }

    protected override void StateSetting(ENormalState changeNormalState, string triggerName, string animId)
    {
        normalState = changeNormalState;
        SetTriggerAnimator(triggerName);
    }
    
    protected override void StateCheck()
    {
        
    }
    protected override void StateRecovery()
    {
        
    }
    
    public void SetSelectAction()
    {
        SpawnInteractionSelect(npcData);
    }

    // 커스텀
    public async UniTask EpisodeMove_X(Vector2 movePos, float speed, int finishDir)
    {
        Stop();
        StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
        
        Vector2 dir = Vector2.left;
        transform.localScale = reverseScale;
        if (transform.position.x < movePos.x)
        {
            dir = Vector2.right;
            transform.localScale = defaultScale;
        }

        stateCancellation = new CancellationTokenSource();
        while (Math.Abs(transform.position.x - movePos.x) > 0.1f)
        {
            // basicStat.moveSpeed
            if(normalState == ENormalState.Idle)
                StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
            
            CustomMoving_X(dir, speed);
            await FixedYieldDelay(stateCancellation);
        }

        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        switch (finishDir)
        {
            case -1:
                transform.localScale = reverseScale;
                break;
            case 1:
                transform.localScale = defaultScale;
                break;
        }
        Stop();
        StopVelocity_X();
    }
    public async UniTask EpisodeMove_Y(Vector2 movePos, float speed)
    {
        Stop();
        StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
        
        Vector2 dir = Vector2.down;
        if (transform.position.y < movePos.y)
            dir = Vector2.up;

        stateCancellation = new CancellationTokenSource();
        while (Math.Abs(transform.position.y - movePos.y) > 0.1f)
        {
            if(normalState == ENormalState.Idle)
                StateSetting(ENormalState.Move, ConstValues.Move, ConstValues.Move);
            
            CustomMoving_Y(dir, speed);
            await FixedYieldDelay(stateCancellation);
        }
        transform.position = new Vector2(transform.position.x, movePos.y);

        StateSetting(ENormalState.Idle, ConstValues.Idle, ConstValues.Idle);
        Stop();
        StopVelocity_Y();
    }

    protected virtual void OnCollisionEnter2D(Collision2D col)
    {
        // 착지
        if ((col.gameObject.CompareTag(ConstValues.Ground) || col.gameObject.CompareTag(ConstValues.Platform)) && landingState == ELandingState.Air)
        {
            if (myRigidbody.gravityScale == 0 || myRigidbody.linearVelocityY is >= 0.05f or <= -0.05f)
                return;
            
            LandingStateSetting(ELandingState.Ground);
            myRigidbody.bodyType = RigidbodyType2D.Dynamic;
            myRigidbody.linearVelocity = Vector2.zero;
            groundObject = col.gameObject;
            
            // 점프도중, 또는 에어본 도중 지면에 닿았을 경우의 애니메이션 처리
            switch (normalState)
            {
                case ENormalState.Airborne:
                    DownAndStand();
                    break;
            }
        }
    }
    
    protected void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag(ConstValues.Ground) || col.gameObject.CompareTag(ConstValues.Platform))
        {
            LandingStateSetting(ELandingState.Air);
        }
    }
}
