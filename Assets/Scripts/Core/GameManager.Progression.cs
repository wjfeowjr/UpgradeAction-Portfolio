using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public partial class GameManager
{
    private void ResetSkillAttribute()
    {
        // 특성 잠금 목록을 새 테이블 기준으로 다시 구성
        saveData.lockAttributeList.Clear();
        LockAttributeSetting();
        // 모든 캐릭터의 특성을 초기화하고 특성 포인트를 전액 환급
        foreach (var playerInfo in saveData.playerInfoList)
        {
            playerInfo.attributePoint = saveData.totalAttributePoint;
            foreach (var skill in playerInfo.skillList)
                skill.attributeList.Clear();
        }
        SaveGame();
    }

    public void UnLockAttributeSlot(string attributeId)
    {
        var targetAttribute = saveData.lockAttributeList.Find(x => x.id == attributeId);
        targetAttribute.isLock = false;
    }

    private SkillKey SetSkillKey(string skillId, KeyCode keyCode)
    {
        var skillKey = new SkillKey()
        {
            skillId = skillId,
            keyCode = keyCode,
        };
        return skillKey;
    }

    // skillKeyList 는 [0]=대시, [1~4]=스킬1~4 순서로 생성된다(DefaultSkillSetting).
    // 키 설정이 바뀌면 세이브에 기록된 keyCode 도 같은 순서로 다시 맞춰 준다.
    // (스킬은 keyCode 로 skillId 를 역조회하므로, 동기화하지 않으면 키를 바꾼 스킬이 아예 발동되지 않는다)
    public void SyncSkillKeyCode()
    {
        // 캐릭터 교체·포션은 세이브가 아니라 런타임 SettingSkill 이 키를 들고 있다
        if (changeSkill != null)
            changeSkill.keyCode = changeCharacterKey;
        if (potionSkill != null)
            potionSkill.keyCode = potionKey;

        SyncSaveSkillKeyCode();

        // 배틀 씬에 HUD 가 떠 있으면 바뀐 키를 즉시 반영한다
        if (uiInterface)
            uiInterface.SkillPresenter?.Refresh();
    }

    private void SyncSaveSkillKeyCode()
    {
        if (saveData?.playerInfoList == null)
            return;

        foreach (var playerInfo in saveData.playerInfoList)
        {
            var skillKeyList = playerInfo?.skillKeyList;
            if (skillKeyList == null)
                continue;

            for (int i = 0; i < skillKeyList.Count; i++)
            {
                if (skillKeyList[i] == null)
                    continue;

                skillKeyList[i].keyCode = GetSkillSlotKeyCode(i);
            }
        }
    }

    private KeyCode GetSkillSlotKeyCode(int slotIdx)
        => slotIdx switch
        {
            0 => dashKey,
            1 => skillKey1,
            2 => skillKey2,
            3 => skillKey3,
            4 => skillKey4,
            _ => KeyCode.None,
        };

    public KeyCode GetSkillKey(string skillId)
    {
        KeyCode keyCode = default;
        
        foreach (var playerInfo in saveData.playerInfoList)
        {
            var targetSkill = playerInfo.skillKeyList.Find(x => x.skillId == skillId);
            if (targetSkill != null)
            {
                keyCode = targetSkill.keyCode;
                break;
            }
        }
        
        return keyCode;
    }

    private void AddDefaultSkill(string id, PlayerInfo playerInfo)
    {
        Skill newSkill = new Skill();
        newSkill.skillId = id;
        newSkill.attributeList = new List<string>();
        playerInfo.skillList.Add(newSkill);
    }

    public void AddNewSkill(string id)
    {
        // 키 저장
        var skillKeyData = tableManager.GetSkill(id);
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == skillKeyData.caster);
        
        // 이미 가지고 있는 스킬이라면 무시해버린다
        if (playerInfo.skillList.Exists(x => x.skillId == id))
            return;
                
        int idx = EmptySkillIdx(playerInfo.skillKeyList);
        playerInfo.skillKeyList[idx].skillId = id;

        Skill newSkill = new Skill();
        newSkill.skillId = id;
        newSkill.attributeList = new List<string>();
        playerInfo.skillList.Add(newSkill);
        
        RefreshSkill();
        
        // 게임 저장
        SaveGame();
    }

    // 스킬 제거(테스트용)
    public void RemoveSkill(string id)
    {
        foreach (var playerInfo in saveData.playerInfoList)
        {
            var targetKey = playerInfo.skillKeyList.Find(x => x.skillId == id);
            if (targetKey != null)
                targetKey.skillId = default;

            var targetSkill = playerInfo.skillList.Find(x => x.skillId == id);
            if (targetSkill != null)
                playerInfo.skillList.Remove(targetSkill);
        }
        
        RefreshSkill();
        // 게임 저장
        SaveGame();
    }

    private int EmptySkillIdx(List<SkillKey> skillKeyList)
    {
        int idx = 1;
        for (int i = 1; i < skillKeyList.Count; i++)
        {
            if (string.IsNullOrEmpty(skillKeyList[i].skillId))
            {
                idx = i;
                break;
            }
        }
        return idx;
    }

    // 스킬 칸 교체
    public void SetSkillId(KeyCode keyCode, string skillId)
    {
        PlayerInfo playerInfo = new PlayerInfo();

        playerInfo = saveData.playerInfoList.Find(x => x.playerId == curPlayer.BasicStat.id);
        
        var skillKey = playerInfo.skillKeyList.Find(x => x.keyCode == keyCode);
        if (skillKey != null)
            skillKey.skillId = skillId;

        // 저장
    }

    public List<SettingSkill> GetSettingSkillList()
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == curPlayer.BasicStat.id);

        List<SettingSkill> settingSkillList = new List<SettingSkill>();
        foreach (var skillKey in playerInfo.skillKeyList)
        {
            SettingSkill settingSkill = new SettingSkill()
            {
                skillId = skillKey.skillId,
                keyCode = skillKey.keyCode,
            };
            settingSkillList.Add(settingSkill);
        }
        
        var playerSkillList = curPlayer.GetSkillList();
        foreach (var playerSkill in playerSkillList)
        {
            var matchSkillList = settingSkillList.FindAll(x => x.skillId == playerSkill.id);
            foreach (var matchSkill in matchSkillList)
            {
                matchSkill.playerSkill = playerSkill;
            }
        }

        return settingSkillList;
    }

    public void EquipRelic(string playerId, string relicId)
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);

        if (playerInfo.relicList.Contains(relicId))
            return;
        
        for (var i = 0; i < playerInfo.relicList.Count; i++)
        {
            // 빈칸에 자동 장착
            if (string.IsNullOrWhiteSpace(playerInfo.relicList[i]))
            {
                playerInfo.relicList[i] = relicId;
                var itemData = GetItemCopy(relicId);
                var relicName = GetTalk(itemData.name);
                GameLog.Info($"{relicName}장착");
                break;
            }
        }

        foreach (var player in players)
            player.InitBonusStat();
        
        // 게임 저장
        SaveGame();
    }

    public void TargetEquipRelic(string playerId, string relicId, int idx)
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);

        playerInfo.relicList[idx] = relicId;
        var itemData = GetItemCopy(relicId);
        var relicName = GetTalk(itemData.name);
        GameLog.Info($"{relicName}장착");
        
        foreach (var player in players)
            player.InitBonusStat();
        
        // 게임 저장
        SaveGame();
    }

    public void UnEquipRelic(string playerId, string relicId)
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);

        if (!playerInfo.relicList.Contains(relicId))
        {
            GameLog.Info("해당 유물을 장착하고 있지 않음");
            return;
        }
        
        for (var i = 0; i < playerInfo.relicList.Count; i++)
        {
            if (playerInfo.relicList[i] == relicId)
            {
                playerInfo.relicList[i] = default;
                var itemData = GetItemCopy(relicId);
                var relicName = GetTalk(itemData.name);
                GameLog.Info($"{relicName}해제");
                
                foreach (var player in players)
                    player.InitBonusStat();
                break;
            }
        }
        
        // 게임 저장
        SaveGame();
    }

    public List<string> GetPlayerRelicList(string playerId)
    {
        return saveData.playerInfoList.Find(x => x.playerId == playerId).relicList;
    }

    public string GetEquipRelicPlayer(string relicId) 
    {
        string player = default;
        foreach (var playerInfo in saveData.playerInfoList)
        {
            if (playerInfo.relicList.Contains(relicId))
            {
                player = playerInfo.playerId;
                break;
            }
        }
        
        return player;
    }

    // 현재 캐릭터가 해당 유물을 장착하고 있는가
    public bool GetIsEquippedRelic(string curPlayerId, string relicId) 
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == curPlayerId);

        return playerInfo.relicList.Contains(relicId);
    }

    // 현재 캐릭터의 유물 장착슬롯에 공간이 있는가
    public bool GetCanEquipSlot(string playerId) 
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);
        bool canEquipSlot = false;
        foreach (var relic in playerInfo.relicList)
        {
            if (string.IsNullOrWhiteSpace(relic))
            {
                canEquipSlot = true;
                break;
            }
        }
        
        return canEquipSlot;
    }

    // 현재 캐릭터의 모든 유물칸이 비어있는가?
    public bool GetIsEmptyRelicList(string playerId)
    {
        bool isEmpty = true;
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);
        foreach (var relic in playerInfo.relicList)
        {
            if (!string.IsNullOrWhiteSpace(relic))
            {
                isEmpty = false;
                break;
            }
        }
        return isEmpty;
    }

    // 현재 캐릭터의 해당 인덱스에 있는 유물의 Id
    public string GetEquippedRelicId(string playerId, int idx)
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);

        if (playerInfo.relicList.Count < idx + 1)
            return ConstValues.Lock;
        
        return playerInfo.relicList[idx];
    }

    public string GetRelicStat(RelicCopy relicCopy, int idx)
    {
        StringBuilder sb = new StringBuilder();
        switch (relicCopy.statList[idx])
        {
            case eItemStat.Power:
                sb.Append(GetTalk(50101));
                break;
                
            case eItemStat.Defence:
                sb.Append(GetTalk(50102));
                break;
            
            case eItemStat.MoveSpeed:
                sb.Append(GetTalk(50103));
                break;
            
            case eItemStat.AttackSpeed:
                sb.Append(GetTalk(50104));
                break;
            
            case eItemStat.CriticalPercent:
                sb.Append(GetTalk(50105));
                break;
            
            case eItemStat.CriticalDamage:
                sb.Append(GetTalk(50106));
                break;
            
            case eItemStat.StaggerDamage:
                sb.Append(GetTalk(50107));
                break;
        }
                
        if(relicCopy.valueList[idx] > 0)
            sb.Append($" +{relicCopy.valueList[idx]}");
        else
            sb.Append($" -{relicCopy.valueList[idx]}");
                
        switch (relicCopy.statList[idx])
        {
            case eItemStat.MoveSpeed:
                sb.Append('%');
                break;
            case eItemStat.AttackSpeed:
                sb.Append('%');
                break;
            case eItemStat.CriticalPercent:
                sb.Append('%');
                break;
            case eItemStat.CriticalDamage:
                sb.Append('%');
                break;
            case eItemStat.StaggerDamage:
                sb.Append('%');
                break;
        }

        return sb.ToString();
    }

    public void UnLockRelicSlot(string playerId)
    {
        var playerInfo = saveData.playerInfoList.Find(x => x.playerId == playerId);
        playerInfo.relicList.Add(default);
    }

    // 구매 성공 시 true, 골드 부족 등으로 실패 시 false 반환
    public bool BuyItem(StoreItemData storeItemData)
    {
        var itemData = GetItemCopy(storeItemData.id);
        if (Gold < storeItemData.cost)
        {
            SpawnWarningPopup(GetTalk(30212)).Forget();
            SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true);
            return false;
        }

        switch (itemData.type)
        {
            case eItemType.Relic:
                GetRelic(storeItemData.id);
                break;
        }
        Gold -= storeItemData.cost;
        SoundManager.Instance.PlaySound(ConstValues.ProductMailDelivery, true);
        SpawnWarningPopup(GetTalk(30216)).Forget();
        SaveGame();
        return true;
    }


    private void InitChangeSkill()
    {
        changeSkill = new SettingSkill()
        {
            skillId = ConstValues.ChangeCharacter,
            keyCode = changeCharacterKey,
        };
        
        foreach (var skill in tableManager.skillTable.Skill)
        {
            if (skill.id != ConstValues.ChangeCharacter)
                continue;
            
            PlayerSkill addedSkill = new PlayerSkill();
            addedSkill.id = skill.id;
            var coolTimeArray = skill.coolTime.Split(';');
            foreach (var coolTime in coolTimeArray)
            {
                addedSkill.maxCoolTime.Add(TableParse.Float(coolTime));
                addedSkill.curCoolTime.Add(TableParse.Float(coolTime));
            }
            
            addedSkill.talk = GetTalk(skill.talk);
            addedSkill.explainTalk = GetTalk(skill.explainTalk);
            changeSkill.playerSkill = addedSkill;
            break;
        }
    }

    private void InitPotionSkill()
    {
        potionSkill = new SettingSkill()
        {
            skillId = ConstValues.PotionDrink,
            keyCode = potionKey,
        };
        
        foreach (var skill in tableManager.skillTable.Skill)
        {
            if (skill.id != ConstValues.PotionDrink)
                continue;
            
            PlayerSkill addedSkill = new PlayerSkill();
            addedSkill.id = skill.id;
            var coolTimeArray = skill.coolTime.Split(';');
            foreach (var coolTime in coolTimeArray)
            {
                addedSkill.maxCoolTime.Add(TableParse.Float(coolTime));
                addedSkill.curCoolTime.Add(TableParse.Float(coolTime));
            }
            
            addedSkill.talk = GetTalk(skill.talk);
            addedSkill.explainTalk = GetTalk(skill.explainTalk);
            potionSkill.playerSkill = addedSkill;
            break;
        }
    }

    public void SetPotionCount()
    {
        potionSkill.playerSkill.maxCoolTime[2] = saveData.additionPotionCount;
        potionSkill.playerSkill.curCoolTime[2] = saveData.additionPotionCount;
        RefreshPotionActive();
    }

    // 추가 포션을 하나라도 획득했을 때만 포션 UI를 노출
    public void RefreshPotionActive()
    {
        if (!uiInterface)
            return;

        uiInterface.PotionSkillView.gameObject.SetActive(saveData.additionPotionCount > 0);
    }

    // 해당 아이템을 가지고 있는가?
    public bool IsHaveItem(string id)
    {
        return ItemList.Find(x => x.id == id) != null;
    }

    public string GetSkillName(string id)
    {
        string skillName = default;
        foreach (var skill in tableManager.skillTable.Skill)
        {
            if (skill.id != id)
                continue;

            skillName = GetTalk(skill.talk);
            break;
        }

        return skillName;
    }

    private void RefreshSkill()
    {
        var uiInterfaceObj = GetUI(eUIType.UI_Interface);
        if (uiInterfaceObj == null)
            return;
        
        uiInterface.BindSkill(() => new UISkillModel
        {
            changeSkill = changeSkill,
            potionSkill = potionSkill,
            settingSkillList = GetSettingSkillList()
        }).SetSkillInfo();
    }

    public void PlusAttributePoint(int point)
    {
        saveData.totalAttributePoint += point;
        foreach (var playerInfo in saveData.playerInfoList)
            playerInfo.attributePoint += point;
    }

    public void PlusPotion()
    {
        saveData.additionPotionCount += 1;
        GameManager.Instance.SetPotionCount();
    }

    public bool IsHaveSkill(string skillId)
    {
        foreach (var playerInfo in saveData.playerInfoList)
        {
            var skill = playerInfo.skillList.Find(x => x.skillId == skillId);
            if (skill != null)
                return true;
        }
        
        return false;
    }

    public List<string> GetSkillAttribute(string skillId)
    {
        foreach (var playerInfo in saveData.playerInfoList)
        {
            var skillList = playerInfo.skillList.Find(x => x.skillId == skillId).attributeList;
            if (skillList != null)
                return skillList;
        }
        
        GameLog.Info("검색되는 특성 없음");
        return null;
    }

    public bool IsHaveAttribute(string skillId, string attributeId)
    {
        foreach (var playerInfo in saveData.playerInfoList)
        {
            var skill = playerInfo.skillList.Find(x => x.skillId == skillId);
            if (skill != null)
                return skill.attributeList.Contains(attributeId);
        }

        GameLog.Info($"{attributeId} 특성 자체가 없음");
        return false;
    }

    public async void BuyAttribute(string skillId, string attributeId, Vector3 effectPos)
    {
        var attributeData = GetAttributesById(attributeId);

        foreach (var playerInfo in saveData.playerInfoList)
        {
            var skill = playerInfo.skillList.Find(x => x.skillId == skillId);
            if (skill == null)
                continue;

            var isLock = saveData.lockAttributeList.Find(x => x.id == attributeId).isLock;
            if (isLock)
            {
                SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true);
                await SpawnWarningPopup(GetTalk(30213));
                return;
            }

            var targetAttribute = skill.attributeList.Contains(attributeId);
            if (!targetAttribute)
            {
                if (playerInfo.attributePoint < attributeData[0].cost)
                {
                    SoundManager.Instance.PlaySound(ConstValues.NormalButton2, true);
                    await SpawnWarningPopup(GetTalk(30202));
                }
                else
                {
                    skill.attributeList.Add(attributeId);
                    playerInfo.attributePoint -= attributeData[0].cost;
                    // 올리는 연출 넣기
                    SpawnHighestObject(ConstValues.AttributeUpEffect, effectPos);
                }
            }
            break;
        }
    }

    public void SellAttribute(string skillId, string attributeId, Vector3 effectPos)
    {
        var attributeData = GetAttributesById(attributeId);

        foreach (var playerInfo in saveData.playerInfoList)
        {
            var skill = playerInfo.skillList.Find(x => x.skillId == skillId);
            if (skill == null)
                continue;
            
            var targetAttribute = skill.attributeList.Contains(attributeId);
            if (!targetAttribute)
                continue;
            
            var attributeList = attributeData.FindAll(x => x.skill == skillId);
            var attribute = attributeList.Find(x => x.id == attributeId);
            playerInfo.attributePoint += attribute.cost;
            skill.attributeList.Remove(attributeId);
            // 내리는 연출 넣기
            SpawnHighestObject(ConstValues.AttributeDownEffect, effectPos);
        }
    }

    // 해당 스킬의 Id찾기(내가 해당 특성을 가지고 있어야 함)
    public List<string> GetAttributePassive(string id)
    {
        List<string> idList = new List<string>();
        string[] idSplit = id.Split('_');
        
        string skillId = id;
        if (idSplit.Length > 1)
            skillId = $"{idSplit[0]}_{idSplit[1]}";
        
        string targetId = id;
        if (idSplit.Length > 2)
            targetId = $"{idSplit[0]}_{idSplit[1]}_{idSplit[2]}";
        
        // 정확히 일치하는 타겟 데이터가 있는지 확인하고, 그 데이터는 파생기를 포함하여 효과를 적용함
        var attributeData = GetAttributesByTarget(targetId);
        if (attributeData.Count > 0)
        {
            foreach (var attribute in attributeData)
            {
                if (attribute.passiveId.Count == 0 || !IsHaveAttribute(skillId, attribute.id))
                    continue;
                
                foreach (var passive in attribute.passiveId)
                {
                    idList.Add(passive);
                }
            }
        }
        
        // 파생기도 효과를 적용받음
        attributeData = GetSkillOwnAttributes(skillId);
        foreach (var attribute in attributeData)
        {
            if (attribute.passiveId.Count == 0 || !IsHaveAttribute(skillId, attribute.id))
                continue;
                
            foreach (var passive in attribute.passiveId)
            {
                idList.Add(passive);
            }
        }
        return idList;
    }

    // 시전자(플레이어)에게 적용되는 passive 조회
    // targetObject 지정 여부와 무관하게, 해당 스킬의 특성이면 모두 가져온다(내가 해당 특성을 가지고 있어야 함)
    public List<string> GetAttributeCasterPassive(string skillId)
    {
        List<string> idList = new List<string>();

        var attributeData = GetAttributesBySkill(skillId);
        foreach (var attribute in attributeData)
        {
            if (attribute.passiveId.Count == 0 || !IsHaveAttribute(skillId, attribute.id))
                continue;

            foreach (var passive in attribute.passiveId)
                idList.Add(passive);
        }
        return idList;
    }

    // 해당 스킬의 추가 생성 리스트(내가 해당 특성을 가지고 있어야 함)
    public List<SkillAttributeAddObjectInfo> GetAttributeAddObject(string id)
    {
        var addObjectList = new List<SkillAttributeAddObjectInfo>();
        string[] idSplit = id.Split('_');
        
        string skillId = id;
        if (idSplit.Length > 1)
            skillId = $"{idSplit[0]}_{idSplit[1]}";
        
        string targetId = id;
        if (idSplit.Length > 2)
            targetId = $"{idSplit[0]}_{idSplit[1]}_{idSplit[2]}";
        
        // 정확히 일치하는 타겟 데이터가 있는지 확인하고, 그 데이터는 파생기를 포함하여 효과를 적용함
        var attributeData = GetAttributesByTarget(targetId);
        if (attributeData.Count > 0)
        {
            foreach (var attribute in attributeData)
            {
                if (string.IsNullOrWhiteSpace(attribute.addObjectId) || !IsHaveAttribute(skillId, attribute.id))
                    continue;
                
                var addObjectInfo = new SkillAttributeAddObjectInfo
                {
                    addObjectId = attribute.addObjectId,
                    objectId = attribute.objectId,
                    objectCount = attribute.objectCount,
                };
                addObjectList.Add(addObjectInfo);
            }
        }
        // 파생기도 효과를 적용받음
        attributeData = GetSkillOwnAttributes(skillId);
        foreach (var attribute in attributeData)
        {
            if (string.IsNullOrWhiteSpace(attribute.addObjectId) || !IsHaveAttribute(skillId, attribute.id))
                continue;
                
            var addObjectInfo = new SkillAttributeAddObjectInfo
            {
                addObjectId = attribute.addObjectId,
                objectId = attribute.objectId,
                objectCount = attribute.objectCount,
            };
            addObjectList.Add(addObjectInfo);
        }
        return addObjectList;
    }

    // 해당 스킬의 수치 특성 리스트(내가 해당 특성을 가지고 있어야 함)
    public List<SkillAttributeUpgradeInfo> GetAttributeUpgrade(string id)
    {
        var upgradeList = new List<SkillAttributeUpgradeInfo>();
        string[] idSplit = id.Split('_');
        
        string skillId = id;
        if (idSplit.Length > 1)
            skillId = $"{idSplit[0]}_{idSplit[1]}";
        
        string targetId = id;
        if (idSplit.Length > 2)
            targetId = $"{idSplit[0]}_{idSplit[1]}_{idSplit[2]}";
        
        // 정확히 일치하는 타겟 데이터가 있는지 확인
        var attributeData = GetAttributesByTarget(targetId);
        
        // 정확히 id가 일치하는 오브젝트만 효과를 적용받음
        if (attributeData.Count > 0)
        {
            foreach (var attribute in attributeData)
            {
                if (attribute.upgradeId.Count == 0 || !IsHaveAttribute(skillId, attribute.id))
                    continue;
                
                for (int i = 0; i < attribute.upgradeId.Count; i++)
                {
                    var upgradeInfo = new SkillAttributeUpgradeInfo
                    {
                        upgradeId = attribute.upgradeId[i],
                        upgradeValue = attribute.upgradeValue[i]
                    };
                    upgradeList.Add(upgradeInfo);
                }
            }
        }
        // 파생기도 해당 효과를 적용받음
        attributeData = GetSkillOwnAttributes(skillId);
        foreach (var attribute in attributeData)
        {
            if (attribute.upgradeId.Count == 0 || !IsHaveAttribute(skillId, attribute.id))
                continue;
                
            for (int i = 0; i < attribute.upgradeId.Count; i++)
            {
                var upgradeInfo = new SkillAttributeUpgradeInfo
                {
                    upgradeId = attribute.upgradeId[i],
                    upgradeValue = attribute.upgradeValue[i]
                };
                upgradeList.Add(upgradeInfo);
            }
        }
        return upgradeList;
    }

    // 해당 스킬의 버프 특성 리스트(내가 해당 특성을 가지고 있어야 함)
    public List<SkillAttributeBuffInfo> GetAttributeBuff(string id)
    {
        var buffList = new List<SkillAttributeBuffInfo>();
        string[] idSplit = id.Split('_');
        if (idSplit.Length > 1)
        {
            // 파생기도 해당 효과를 적용받음
            string skillId = $"{idSplit[0]}_{idSplit[1]}";
            var attributeData = GetAttributesBySkill(skillId);
            foreach (var attribute in attributeData)
            {
                if (string.IsNullOrWhiteSpace(attribute.buffId) || !IsHaveAttribute(skillId, attribute.id))
                    continue;
                
                var buffInfo = new SkillAttributeBuffInfo
                {
                    buffId = attribute.buffId,
                    buffTime = attribute.buffTime,
                    buffValue = attribute.buffValue,
                };
                buffList.Add(buffInfo);
            }
        }
        return buffList;
    }

    // 해당 스킬의 디버프 특성 리스트(내가 해당 특성을 가지고 있어야 함)
    public List<SkillAttributeBuffInfo> GetAttributeDeBuff(string id)
    {
        var buffList = new List<SkillAttributeBuffInfo>();
        string[] idSplit = id.Split('_');
        if (idSplit.Length > 1)
        {
            // 파생기도 해당 효과를 적용받음
            string skillId = $"{idSplit[0]}_{idSplit[1]}";
            var attributeData = GetAttributesBySkill(skillId);
            foreach (var attribute in attributeData)
            {
                if (string.IsNullOrWhiteSpace(attribute.deBuffId) || !IsHaveAttribute(skillId, attribute.id))
                    continue;
                
                var buffInfo = new SkillAttributeBuffInfo
                {
                    buffId = attribute.deBuffId,
                    buffTime = attribute.buffTime,
                    buffValue = attribute.buffValue,
                };
                buffList.Add(buffInfo);
            }
        }
        return buffList;
    }

    public void GetItem(string id, int count)
    {
        var itemInfo = new HaveItemInfo()
        {
            id = id,
            count = count,
        };
        // 해당 아이템을 포함하고 있지 않을 때만 추가
        if(!ItemList.Exists(x => x.id == id))
            ItemList.Add(itemInfo);
    }

    public void GetRelic(string id)
    {
        saveData.relicList.Add(id);
    }

    // 복제본 조회는 GameDataService 로 위임한다. 호출부는 기존 이름을 그대로 쓴다.
    public ItemCopy GetItemCopy(string id) => gameData.GetItemCopy(id);
    public RelicCopy GetRelicCopy(string id) => gameData.GetRelicCopy(id);
    public NpcCopy GetNpcCopy(string id) => gameData.GetNpcCopy(id);
    public GrenadeCopy GetGrenadeCopy(string id) => gameData.GetGrenadeCopy(id);
    public PassiveCopy GetPassiveCopy(string id) => gameData.GetPassiveCopy(id);

    public List<SkillAttributeCopy> GetAttributesById(string attributeId)
        => gameData.GetAttributesById(attributeId);

    public List<SkillAttributeCopy> GetAttributesBySkill(string skillId)
        => gameData.GetAttributesBySkill(skillId);

    public List<SkillAttributeCopy> GetAttributesByTarget(string targetId)
        => gameData.GetAttributesByTarget(targetId);

    public List<SkillAttributeCopy> GetSkillOwnAttributes(string skillId)
        => gameData.GetSkillOwnAttributes(skillId);

}
