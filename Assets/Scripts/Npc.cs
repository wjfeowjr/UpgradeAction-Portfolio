using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Npc : Character
{
    [SerializeField] private Transform speechPos;
    [SerializeField] private Transform uiPos;
    
    private InteractionObject interactionObject;
    private InteractionSelect interactionSelect;

    private List<SpeechFrame> speechFrame1 = new List<SpeechFrame>();
    private List<SpeechFrame> speechFrame2 = new List<SpeechFrame>();
    private SpeechFrame speechFrameStrong;
    private SpeechFrame speechFrameTitle;
    
    private NpcData npcData;
    private List<Action> dialogueAction = new List<Action>();

    private bool isFirstTalk;

    public Transform SpeechPos => speechPos;

    protected override void OnEnable()
    {
        base.OnEnable();
        DataSetting();
        SpeechFrameSetting();
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
    
    private void SpeechFrameSetting()
    {
        speechFrame1 = RoomManager.Instance.SpeechFrame1;
        speechFrame2 = RoomManager.Instance.SpeechFrame2;
        speechFrameStrong = RoomManager.Instance.SpeechFrameStrong;
        speechFrameTitle = RoomManager.Instance.SpeechFrameTitle;
    }

    public void SpawnInteractionObject()
    {
        interactionObject.gameObject.SetActive(true);
        interactionObject.transform.position = uiPos.position;
        interactionObject.Expansion();
    }

    public void ReduceInteractionObject()
    {
        interactionObject.Reduce();
    }
    
    public void SetInteractionAction()
    {
        if (interactionObject == null && uiPos)
        {
            interactionObject = SpawnInteraction(ConstValues.InteractionUI, uiPos).GetComponent<InteractionObject>();
            interactionObject.SetInteractionAction(StartDialogue);
            interactionObject.SetText("대화", "↑");
            interactionObject.gameObject.SetActive(false);
        }
    }

    public void SetSelectAction()
    {
        if (interactionSelect == null && uiPos)
        {
            interactionSelect = SpawnInteraction(ConstValues.InteractionSelectUI, uiPos).GetComponent<InteractionSelect>();
            
            var selectList = TableManager.Instance.dialogueChoiceTable.DialogueChoice.FindAll(x => x.npc == npcData.id);
            
            List<string> choiceList = new List<string>();
            foreach (var select in selectList)
                choiceList.Add(select.choiceText);
            
            List<string> idList = new List<string>();
            foreach (var select in selectList)
                idList.Add(select.id);
            
            interactionSelect.StartSetting(choiceList, idList);
            interactionSelect.gameObject.SetActive(false);
        }
    }

    public void SetStartTalkAction()
    {
        isFirstTalk = false;
    }

    private async void SetDialogueAction(string choice)
    {
        interactionSelect.gameObject.SetActive(false);
        
        var talkDataList = TableManager.Instance.dialogueTable.Dialogue.FindAll(x => x.choiceGroupId == choice);
        string checkKey = talkDataList[0].checkKey;
        string endEvent = talkDataList[0].endEvent;
        string eventReward = talkDataList[0].reward;
        int checkKeyValue = GetCheckKey(checkKey);
        
        List<DialogueData> talkList = new List<DialogueData>();
        if (checkKey == ConstValues.None)
        {
            talkList.AddRange(talkDataList);
        }
        else
        {
            talkList.AddRange(talkDataList.FindAll(x => x.checkKey == checkKey && x.checkKeyValue == checkKeyValue));
        }
        
        foreach (var talk in talkList)
        {
            var speechFrame = speechFrame1[0];
            switch (talk.speechFrame)
            {
                case ConstValues.SpeechFrame2:
                    speechFrame = speechFrame2[0];
                    break;
            }

            var speechVector = speechPos.position;
            var speechPose = ConstValues.Idle;
            if (speechPose != ConstValues.None)
                speechPose = talk.speechPose;
            
            if (talk.isSpeaker)
            {
                CustomAnimTrigger(ENormalState.Idle, speechPose);
                // 포즈를 지었으면, 다시 원위치 시킴 다음컷에서
                GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Idle, ConstValues.Idle);
            }
            else
            {
                GameManager.Instance.CurPlayer.CustomAnimTrigger(ENormalState.Idle, speechPose);
                speechVector = GameManager.Instance.CurPlayer.FontPos.position;
            }

            if(talk.sound != ConstValues.None)
                SoundManager.Instance.PlaySound(talk.sound);
            
            var cameraShakeArray = talk.cameraShake.Split(';');
            var cameraShake = new Vector2(float.Parse(cameraShakeArray[0]), float.Parse(cameraShakeArray[1]));
            if(cameraShake != Vector2.zero)
                GameManager.Instance.CameraShake(cameraShake.x, cameraShake.y, talk.shakeTime);
            
            SpawnSpeechFrame(speechFrame, speechVector, talk.speechText);
            await NextDialog(speechFrame);
            if (talk.isEnd)
                break;
        }
        GameManager.Instance.ControlStart = true;

        if (endEvent == ConstValues.None)
        {
            SpawnInteractionObject();
        }
        else
        {
            if(checkKeyValue == 0)
                PlayEndEvent(checkKey, endEvent, eventReward);
            else
                SpawnInteractionObject();
        }
    }

    private int GetCheckKey(string checkKey)
    {
        if(!PlayerPrefs.HasKey(checkKey))
            PlayerPrefs.SetInt(checkKey, 0);
        
        Debug.Log($"키:{checkKey}, 값:{PlayerPrefs.GetInt(checkKey)}");
        return PlayerPrefs.GetInt(checkKey);
    }

    private void SetCheckKey(string checkKey, int value)
    {
        PlayerPrefs.SetInt(checkKey, value);
    }

    private void PlayEndEvent(string checkKey, string eventKey, string reward)
    {
        switch (eventKey)
        {
            case ConstValues.GetSkill:
                SetCheckKey(checkKey, 1);
                GameManager.Instance.AddNewSkill(reward);
                GameManager.Instance.GetSkillProduct(reward, GetSkillDialogue);
                break;
        }
    }
    
    private void SetCloseAction()
    {
        interactionSelect.gameObject.SetActive(false);
        GameManager.Instance.ControlStart = true;
        SpawnInteractionObject();
    }
    
    private void SpawnSpeechFrame(SpeechFrame speechFrame, Vector2 speechPos, string dialog)
    {
        speechFrame.SetPos(speechPos);
        speechFrame.Speech(dialog);
    }

    private async UniTask NextDialog(SpeechFrame speechFrame)
    {
        speechFrame.NextObjectActive();
        // 스페이스바를 누르면 넘어간다
        if (await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.Space), cancellationToken: GameManager.Instance.DialogCancellation.Token).SuppressCancellationThrow())
        {
            speechFrame.SpeechEnd();
            return;
        }
        speechFrame.SpeechEnd();
    }

    private async void StartDialogue()
    {
        //interactionObject.gameObject.SetActive(false);
        ReduceInteractionObject();
        GameManager.Instance.InitDialogueCancellation();
        GameManager.Instance.ControlStart = false;
        
        // 최초응답
        if (!isFirstTalk)
        {
            var firstTalk = TableManager.Instance.dialogueTable.Dialogue.Find(x => x.id == npcData.startDialog);
            var speechFrame = speechFrame1[0];
            switch (firstTalk.speechFrame)
            {
                case ConstValues.SpeechFrame2:
                    speechFrame = speechFrame2[0];
                    break;
            }
            
            SpawnSpeechFrame(speechFrame, speechPos.position, firstTalk.speechText);
            await NextDialog(speechFrame);
            isFirstTalk = true;
        }

        // 대화 선택지 및 선택 액션
        interactionSelect.gameObject.SetActive(true);
        interactionSelect.SetAction(SetDialogueAction, SetCloseAction);
        interactionSelect.SetDelay();
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
    
    private GameObject SpawnInteraction(string id, Transform uiTransform)
    {
        var obj = GameManager.Instance.SpawnToUIObjectPoolInstantiate(id, uiTransform);
        
        var uiData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);
        if (uiData == null)
            return obj;
        
        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();
        
        spawnedObject.SetupData(uiData, transform.localScale.x);
        spawnedObject.EnableSetting();
        
        if (spawnedObject.GetTrace())
        {
            var trace = obj.GetComponent<Trace>();
            if(!trace)
                trace = obj.AddComponent<Trace>();
            
            trace.SetTarget(uiTransform);
        }

        return obj;
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
    
    // 스킬을 획득 후 독백 이벤트
    private async void GetSkillDialogue(string skillName)
    {
        string getMessage = $"{skillName}을(를) 획득하였다!";
        await GameManager.Instance.SpawnWarningPopup(getMessage);
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
