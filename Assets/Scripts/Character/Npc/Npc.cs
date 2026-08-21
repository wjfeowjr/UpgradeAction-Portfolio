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

interface IQuestClearAction
{
    public UniTask QuestClearAction();
}

public class Npc : Character
{
    [SerializeField] private NpcInfo npcInfo;
    [SerializeField] private Npc[] anotherNpc;

    // Character 가 InteractionController 를 상속하지 않게 되면서 컴포넌트로 바뀌었다.
    // 인스펙터 연결을 빠뜨려도 같은 오브젝트에서 찾아 쓴다.
    [SerializeField] private InteractionController interaction;

    private InteractionController Interaction
    {
        get
        {
            if (!interaction)
                interaction = GetComponent<InteractionController>();
            return interaction;
        }
    }

    // Player 의 OnTriggerStay2D / OnTriggerExit2D 가 부르던 이름을 그대로 유지한다.
    public bool IsPlayerTouch
    {
        get => Interaction && Interaction.IsPlayerTouch;
        set { if (Interaction) Interaction.IsPlayerTouch = value; }
    }

    public void SpawnInteractionObject() => Interaction.SpawnInteractionObject();
    public void ReduceInteractionObject() => Interaction.ReduceInteractionObject();

    // Room.RefreshTalk / RefreshKey 가 부른다(언어 변경, 키 재설정 직후)
    public void RefreshTalkText() => Interaction.RefreshTalkText();
    public void RefreshKeyText(KeyCode key) => Interaction.RefreshKeyText(key);
    
    private NpcCopy npcCopyData;
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
        if(npcCopyData == null)
            npcCopyData = GameManager.Instance.GetNpcCopy(name);
    }

    public void SetInteractionAction()
    {
        Interaction.SetInteractionAction(StartDialogue, 30016, GameManager.Instance.upKey);
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
        var data = GameManager.Instance.NpcInfoList.Find(x => x.id == name);
        if (data == null)
        {
            NpcInfo npc = new NpcInfo();
            npc.id = name;
            foreach (var key in npcCopyData.dialogKey)
            {
                DialogKey dialogKey = new DialogKey
                {
                    id = key,
                    isUse = false
                };
                npc.dialogKey.Add(dialogKey);
            }
            npc.isFirstDialogFinish = string.IsNullOrWhiteSpace(npcCopyData.firstDialog);
            GameManager.Instance.NpcInfoList.Add(npc);
            npcInfo = GameManager.Instance.NpcInfoList.Find(x => x.id == name);
        }
        else
        {
            npcInfo = data;
            if (npcInfo.dialogKey.Count < npcCopyData.dialogKey.Count)
            {
                foreach (var key in npcCopyData.dialogKey)
                {
                    if (npcInfo.dialogKey.Exists(x => x.id == key))
                        continue;
                    
                    DialogKey dialogKey = new DialogKey
                    {
                        id = key,
                        isUse = false
                    };
                    npcInfo.dialogKey.Add(dialogKey);
                }
            }
            
            if (string.IsNullOrWhiteSpace(npcCopyData.firstDialog))
                npcInfo.isFirstDialogFinish = true;
        }
    }

    private async void SetDialogueAction(string choice)
    {
        Interaction.ActiveInteractionSelect(false);

        var choiceSplit = choice.Split('_');
        var choiceType = choiceSplit[0];

        if (choiceType == ConstValues.Open)
        {
            string popupId = choiceSplit[1];
            GameLog.Info($"{popupId} 팝업이 뜬다!");

            // TODO: Popup_Store 생성 및 MVP 초기화
            var uiBase = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_Store, Vector3.zero).GetComponent<UIBase>();
            uiBase.ExpansionOpen(true, true).Forget();
            
            if (uiBase is Popup_Store popupStore)
            {
                
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
                        uiBase.ReductionClose(true, true).Forget();
                        IsPlayerTouch = false;
                        GameManager.Instance.CurPlayer.MyRigidbody.WakeUp();
                    }
                };
                
                popupStore.StoreView.SetData(storeModel, popupId);
            }
        }
        else
        {
            await GameManager.Instance.NpcDialogue(choice, anotherNpc, npcInfo, RefreshInteractionSelect);
        }
    }

    private async UniTask SetFirstDialogueAction(string choice)
    {
        Interaction.ActiveInteractionSelect(false);
        await GameManager.Instance.NpcDialogue(choice, anotherNpc, npcInfo, RefreshInteractionSelect);
    }

    // endEvent 발생 시 선택지를 현재 dialogKey 상태 기준으로 재구성
    private void RefreshInteractionSelect()
    {
        Interaction.SpawnInteractionSelect(npcCopyData, npcInfo);
    }

    protected virtual async void StartDialogue()
    {
        Interaction.ReduceInteractionObject();
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
            // 미션 NPC: 모든 재료 충족 시 InteractionSelect 건너뛰고 클리어 연출 자동 진행
            if (IsQuestReadyToClear())
            {
                // SpawnInteractionObject
                await GameManager.Instance.NpcDialogue(npcCopyData.questClearChoice, anotherNpc, npcInfo, RefreshInteractionSelect);

                // NPC별 클리어 후속 연출 (해당 인터페이스가 구현돼 있을 때만 실행)
                var clearAction = GetComponent<IQuestClearAction>();
                if (clearAction != null)
                    await clearAction.QuestClearAction();
                
                return;
            }

            if (!string.IsNullOrWhiteSpace(npcCopyData.startDialog) && !isFirstTalk)
            {
                await GameManager.Instance.NpcFirstTalk(npcCopyData.startDialog, speechPos);
                isFirstTalk = true;
            }
            Interaction.SetActionInteractionSelect(SetDialogueAction, Interaction.SetInteractionSelectCloseAction);
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
        Interaction.SpawnInteractionSelect(npcCopyData, npcInfo);
    }

    // 미션 NPC가 모든 재료를 충족했고, 아직 클리어 처리가 안 된 상태인지 확인
    private bool IsQuestReadyToClear()
    {
        if (npcCopyData == null || npcInfo == null)
            return false;
        if (npcCopyData.questItemId == null || npcCopyData.questItemId.Count == 0)
            return false;
        if (string.IsNullOrWhiteSpace(npcCopyData.questClearChoice))
            return false;

        // 퀘스트 클리어 상태를 추적하는 키 = questClearChoice 라인의 checkKey
        var clearLine = TableManager.Instance.dialogueTable.Dialogue.Find(x => x.choiceGroupId == npcCopyData.questClearChoice);
        if (clearLine == null || string.IsNullOrWhiteSpace(clearLine.checkKey))
            return false;

        var questKey = npcInfo.dialogKey.Find(k => k.id == clearLine.checkKey);
        if (questKey == null || questKey.isUse)
            return false;

        for (int i = 0; i < npcCopyData.questItemId.Count; i++)
        {
            var requireId = npcCopyData.questItemId[i];
            var requireCount = i < npcCopyData.questItemCount.Count ? npcCopyData.questItemCount[i] : 0;
            var owned = GameManager.Instance.ItemList.Find(x => x.id == requireId);
            if (owned == null || owned.count < requireCount)
                return false;
        }
        return true;
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
            
            if (await FixedYieldDelay(stateCancellation).SuppressCancellationThrow())
                return;
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
