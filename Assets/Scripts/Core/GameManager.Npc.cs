using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class GameManager
{

    // 대화 세팅 연출
    public async UniTask NpcDialogue(string choice, Npc[] npc, NpcInfo npcInfo, Action onEndEvent = null)
    {
        bool handedOffToRoom = false;
        var talkDataList = TableManager.Instance.dialogueTable.Dialogue.FindAll(x => x.choiceGroupId == choice && IsDialogKeyMatched(x.checkKey, x.checkKeyValue, npcInfo));

        foreach (var talkData in talkDataList)
        {
            // 프레임 이름을 먼저 정한 뒤 한 번만 스폰 (선스폰 후 교체 시 SpeechFrame1이 활성 상태로 누적되는 버그 방지)
            var frameName = talkData.speechFrame switch
            {
                ConstValues.SpeechFrame2 => ConstValues.SpeechFrame2,
                ConstValues.SpeechFrame3 => ConstValues.SpeechFrame3,
                _ => ConstValues.SpeechFrame1,
            };
            var speechFrame = GetSpeechFrame(frameName);

            var speechCharacter = GetCharacter(talkData.speaker, npc);
            List<Character> poseCharacterList = new List<Character>();

            var poseCharacters = talkData.poseCharacter.Split(';');
            foreach (var poseCharacter in poseCharacters)
            {
                if (!string.IsNullOrWhiteSpace(poseCharacter))
                {
                    poseCharacterList.Add(GetCharacter(poseCharacter, npc));
                }
            }
            
            List<string> speechPoseList = new List<string>();
            var speechPoses = talkData.speechPose.Split(';');
            foreach (var speechPose in speechPoses)
            {
                if (!string.IsNullOrWhiteSpace(speechPose))
                {
                    speechPoseList.Add(speechPose);
                }
            }
            
            var speechPos = speechCharacter.SpeechPos;

            for (var i = 0; i < poseCharacterList.Count; i++)
                poseCharacterList[i].CustomAnimTrigger(ENormalState.Idle, speechPoseList[i], ConstValues.Idle);

            if(!string.IsNullOrWhiteSpace(talkData.sound))
                SoundManager.Instance.PlaySound(talkData.sound);
            
            var cameraShakeArray = talkData.cameraShake.Split(';');
            var cameraShake = new Vector2(float.Parse(cameraShakeArray[0]), float.Parse(cameraShakeArray[1]));
            if(cameraShake != Vector2.zero)
                CameraShake(cameraShake.x, cameraShake.y, talkData.shakeTime);
            
            SpawnSpeechFrame(speechFrame, speechPos.position, GetTalk(talkData.talk));
            await NextDialog(speechFrame);
            
            string endEvent = talkData.endEvent;
            string eventReward = talkData.reward;
            if (!string.IsNullOrWhiteSpace(endEvent))
            {
                if (PlayEndEvent(npcInfo, endEvent, eventReward, talkData.checkKey))
                    handedOffToRoom = true;
                onEndEvent?.Invoke();
            }

            if (talkData.isEnd)
                break;
        }

        // Room 연출로 위임된 경우 컨트롤 복귀와 finishAction은 Room 측에서 책임짐
        if (handedOffToRoom)
            return;

        ControlStart = true;
        foreach (var person in npc)
            person.IsPlayerTouch = false;
        curPlayer.MyRigidbody.WakeUp();
    }

    // Room 연출로 위임됐으면 true (호출자는 ControlStart/finishAction을 스킵해야 함)
    private bool PlayEndEvent(NpcInfo npcInfo, string eventKey, string reward, string dialogKeyId)
    {
        var targetKey = string.IsNullOrWhiteSpace(dialogKeyId) ? null : npcInfo?.dialogKey?.Find(k => k.id == dialogKeyId);

        switch (eventKey)
        {
            case ConstValues.GetSkill:
                if (targetKey != null)
                    targetKey.isUse = true;
                AddNewSkill(reward);
                GetSkillProduct(reward, ProductObjectInfo);
                SaveGame();
                return false;
            case ConstValues.QuestClear:
                if (targetKey != null)
                    targetKey.isUse = true;
                ConsumeQuestItems(npcInfo.id);
                return false;
            default:
                // GameManager가 처리하지 않는 endEvent는 현재 Room에 위임
                // (BossEvent 등 Room 연출은 Room.PlayRoomEndEvent에서 분기)
                if (targetKey != null)
                    targetKey.isUse = true;
                RoomManager.Instance.CurrentRoom.PlayRoomEndEvent(eventKey);
                return true;
        }
    }

    // NpcInfo의 dialogKey 리스트에서 checkKey 매칭 여부 판단
    private bool IsDialogKeyMatched(string checkKey, bool checkKeyValue, NpcInfo npcInfo)
    {
        if (string.IsNullOrWhiteSpace(checkKey))
            return true;
        if (npcInfo?.dialogKey == null)
            return false;
        
        var key = npcInfo.dialogKey.Find(k => k.id == checkKey);
        return key != null && key.isUse == checkKeyValue;
    }

    private void ConsumeQuestItems(string npcId)
    {
        var data = GetNpcCopy(npcId);
        if (data == null || data.questItemId == null)
            return;

        for (int i = 0; i < data.questItemId.Count; i++)
        {
            var itemId = data.questItemId[i];
            var consumeCount = i < data.questItemCount.Count ? data.questItemCount[i] : 0;
            var owned = ItemList.Find(x => x.id == itemId);
            if (owned == null)
                continue;

            owned.count -= consumeCount;
            if (owned.count <= 0)
                ItemList.Remove(owned);
        }
    }

    public async UniTask NpcFirstTalk(string startDialog, Transform speechPos)
    {
        var firstTalk = TableManager.Instance.GetDialogue(startDialog);
        // 프레임 이름을 먼저 정한 뒤 한 번만 스폰 (선스폰 후 교체 시 SpeechFrame1이 활성 상태로 누적되는 버그 방지)
        var frameName = firstTalk.speechFrame switch
        {
            ConstValues.SpeechFrame2 => ConstValues.SpeechFrame2,
            ConstValues.SpeechFrame3 => ConstValues.SpeechFrame3,
            _ => ConstValues.SpeechFrame1,
        };
        var speechFrame = GetSpeechFrame(frameName);

        SpawnSpeechFrame(speechFrame, speechPos.position, GameManager.Instance.GetTalk(firstTalk.talk));
        await NextDialog(speechFrame);
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
        if (await UniTask.WaitUntil(() => Input.GetKeyDown(enterKey), cancellationToken: GameManager.Instance.ProductCancellation.Token).SuppressCancellationThrow())
        {
            speechFrame.SpeechEnd();
            return;
        }
        speechFrame.SpeechEnd();
    }

    public async UniTask DialogueMove(float xPos)
    {
        // 항상 광전사가 맨 앞에 있어야 함
        var berserker = GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
        var gunner = GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();
        var fighter = GetPlayer(ConstValues.Fighter).GetComponent<Player_Fighter>();

        var berserkerPos = curPlayer.transform.position;
        var gunnerPos = new Vector2(berserkerPos.x + xPos, berserkerPos.y);
        var fighterPos = new Vector2(gunnerPos.x + xPos, berserkerPos.y);

        if (saveData.playerList.Contains(ConstValues.Berserker))
        {
            berserker.gameObject.SetActive(true);
            berserker.transform.position = berserkerPos;
            if(curPlayer.transform.localScale.x >= 0)
                berserker.Flip(1);
            else
                berserker.Flip(-1);
        }

        if (saveData.playerList.Contains(ConstValues.Gunner))
        {
            gunner.gameObject.SetActive(true);
            gunner.transform.position = berserkerPos;
            if(xPos >= 0)
                gunner.Flip(1);
            else
                gunner.Flip(-1);
        }

        if (saveData.playerList.Contains(ConstValues.Fighter))
        {
            fighter.gameObject.SetActive(true);
            fighter.transform.position = berserkerPos;
            if(xPos >= 0)
                fighter.Flip(1);
            else
                fighter.Flip(-1);
        }

        if (gunner.gameObject.activeSelf)
        {
            int finishDir = 1;
            if (xPos < 0)
                finishDir = -1;

            if (await gunner.EpisodeMove(gunnerPos, gunner.BasicStat.moveSpeed, finishDir).SuppressCancellationThrow())
                return;
            
            gunner.Flip(-finishDir);
        }

        if (fighter.gameObject.activeSelf)
        {
            int finishDir = 1;
            if (xPos < 0)
                finishDir = -1;

            if (await fighter.EpisodeMove(fighterPos, fighter.BasicStat.moveSpeed, finishDir).SuppressCancellationThrow())
                return;
            
            fighter.Flip(-finishDir);
        }
    }

    public void DialogueEnd()
    {
        var berserker = GetPlayer(ConstValues.Berserker).GetComponent<Player_Berserker>();
        var gunner = GetPlayer(ConstValues.Gunner).GetComponent<Player_Gunner>();
        var fighter = GetPlayer(ConstValues.Fighter).GetComponent<Player_Fighter>();

        if (berserker.gameObject.activeSelf)
        {
            if (curPlayer != berserker)
            { 
                berserker.SpawnObject(ConstValues.BangEffect, berserker.CenterPos.position);
                berserker.gameObject.SetActive(false);
            }
        }
        if (gunner.gameObject.activeSelf)
        {
            if (curPlayer != gunner)
            { 
                gunner.SpawnObject(ConstValues.BangEffect, gunner.CenterPos.position);
                gunner.gameObject.SetActive(false);
            }
        }
        if (fighter.gameObject.activeSelf)
        {
            if (curPlayer != fighter)
            { 
                fighter.SpawnObject(ConstValues.BangEffect, fighter.CenterPos.position);
                fighter.gameObject.SetActive(false);
            }
        }
    }
}
