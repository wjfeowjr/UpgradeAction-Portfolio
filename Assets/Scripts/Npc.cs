using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class Npc : Character
{
    [SerializeField] private Transform speechPos;
    [SerializeField] private NpcInfo npcInfo;

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

    public void SetInteractionAction()
    {
        SetInteractionAction(StartDialogue, GameManager.Instance.GetTalk(30016), GameManager.Instance.GetKeyCode(GameManager.Instance.interactionKey));
    }

    public void SetStartTalkAction()
    {
        isFirstTalk = false;
    }

    private async void SetDialogueAction(string choice)
    {
        ActiveInteractionSelect(false);
        
        var talkDataList = TableManager.Instance.dialogueTable.Dialogue.FindAll(x => x.choiceGroupId == choice);
        string checkKey = talkDataList[0].checkKey;
        string endEvent = talkDataList[0].endEvent;
        string eventReward = talkDataList[0].reward;
        bool checkKeyValue = npcInfo.dialogKey.isUse;
        
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
            
            SpawnSpeechFrame(speechFrame, speechVector, GameManager.Instance.GetTalk(talk.talk));
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
            if(checkKeyValue)
                SpawnInteractionObject();
            else
                PlayEndEvent(endEvent, eventReward);
        }
    }

    public void AddData()
    {
        var data = GameManager.Instance.NpcInfoInfoList.Find(x => x.id == name);
        if (data == null)
        {
            NpcInfo npc = new NpcInfo();
            npc.id = name;
            npc.dialogKey.id = npcData.dialogKey;
            GameManager.Instance.NpcInfoInfoList.Add(npc);
            npcInfo = GameManager.Instance.NpcInfoInfoList.Find(x => x.id == name);
        }
        else
        {
            npcInfo = data;
        }
    }

    private void PlayEndEvent(string eventKey, string reward)
    {
        switch (eventKey)
        {
            case ConstValues.GetSkill:
                npcInfo.dialogKey.isUse = true;
                GameManager.Instance.AddNewSkill(reward);
                GameManager.Instance.GetSkillProduct(reward, GetSkillDialogue);
                break;
        }
    }
    
    private void SetCloseAction()
    {
        ActiveInteractionSelect(false);
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
        if (await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.Space), cancellationToken: GameManager.Instance.ProductCancellation.Token).SuppressCancellationThrow())
        {
            speechFrame.SpeechEnd();
            return;
        }
        speechFrame.SpeechEnd();
    }

    protected virtual async void StartDialogue()
    {
        //ActiveInteractionObject(false);
        ReduceInteractionObject();
        GameManager.Instance.InitProductCancellation();
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
            
            SpawnSpeechFrame(speechFrame, speechPos.position, GameManager.Instance.GetTalk(firstTalk.talk));
            await NextDialog(speechFrame);
            isFirstTalk = true;
        }
        SetActionInteractionSelect(SetDialogueAction, SetCloseAction);
    }

    protected override void StateSetting(ENormalState changeNormalState, string triggerName, string animId)
    {
        normalState = changeNormalState;
        if (triggerName == ConstValues.None)
            return;
        
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
    
    // 스킬을 획득 후 독백 이벤트
    private async void GetSkillDialogue(string skillName)
    {
        string getMessage = string.Format(GameManager.Instance.GetTalk(30200), skillName);
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
