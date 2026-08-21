using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public partial class GameManager
{

    public void SaveGame()
    {
        // 저장 시각 갱신 (UTC, 로케일 무관 round-trip 포맷)
        saveData.lastSavedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        // 보스 처치 집계 갱신
        RefreshBossCount();

        // json화
        SaveSystem.Save(curSaveFileName, saveData);
    }

    // 전체 보스 수(bossCount)와 처치한 보스 수(curBossCount)를 다시 집계한다
    public void RefreshBossCount()
    {
        if (!RoomManager.Instance)
            return;

        var roomArray = RoomManager.Instance.RoomArray;
        if (roomArray == null || roomArray.Length == 0)
            return;

        saveData.bossCount = 0;
        saveData.curBossCount = 0;

        foreach (var room in roomArray)
        {
            // 보스방/미니보스방만 집계한다
            if (!room.name.StartsWith("Room_Boss") && !room.name.StartsWith("Room_MiniBoss"))
                continue;

            // 1. 보스방/미니보스방의 bosses 배열 크기를 더한다
            saveData.bossCount += room.BossCount;

            // 2. 첫 연출(보스전)이 끝났으면 처치한 것으로 더한다
            var roomInfo = saveData.roomInfoList.Find(x => x.roomId == room.name);
            if (roomInfo?.roomProduct.Count > 0 && roomInfo.roomProduct[0].isFinish)
                saveData.curBossCount += room.BossCount;
        }
    }

    public SaveData LoadGame(string fileName)
    {
        SaveData data = null;
        // json화
        if(SaveSystem.TryLoad(fileName, out SaveData loadData))
            data = loadData;

        return data;
    }

    // saveData.roomInfoList의 정렬 순서를 RoomManager의 TotalRoom.RoomArray 순서와 동일하게 맞춘다.
    public void SortRoomInfo()
    {
        if (!RoomManager.Instance)
            return;

        var roomArray = RoomManager.Instance.RoomArray;
        if (roomArray == null || roomArray.Length == 0)
            return;

        var sortedList = new List<RoomInfo>();

        // RoomArray 순서대로 매칭되는 RoomInfo를 먼저 채운다.
        foreach (var room in roomArray)
        {
            var info = saveData.roomInfoList.Find(x => x.roomId == room.name);
            if (info != null && !sortedList.Contains(info))
                sortedList.Add(info);
        }

        // RoomArray에 매칭되지 않는 데이터는 유실 방지를 위해 뒤에 그대로 유지한다.
        foreach (var info in saveData.roomInfoList)
        {
            if (!sortedList.Contains(info))
                sortedList.Add(info);
        }

        saveData.roomInfoList = sortedList;
    }

    private void DataPatch(SaveData data)
    {
        // 특성 개편 패치: 한국 시간 2026-07-21 00:00(UTC+9) 이전에 저장된 세이브가 대상
        // lastSavedAt이 없는 구버전 세이브도 개편 이전 저장본이므로 패치 대상에 포함한다
        DateTime patchTimeUtc = new DateTime(2026, 7, 21, 0, 0, 0, DateTimeKind.Utc).AddHours(-9);
        bool hasSavedTime = DateTime.TryParse(data.lastSavedAt, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out DateTime lastSaved);
        if (hasSavedTime && lastSaved.ToUniversalTime() >= patchTimeUtc)
            return;

        ResetSkillAttribute();
    }

    public void DeleteData()
    {
        // json화
        SaveSystem.Delete(curSaveFileName);
    }

    public void CopyData(int srcIdx, int dstIdx)
    {
        string srcName = $"{ConstValues.User}_{srcIdx}";
        string dstName = $"{ConstValues.User}_{dstIdx}";

        if (isDemo)
        {
            srcName = $"{ConstValues.User}_{srcIdx}_{ConstValues.Demo}";
            dstName = $"{ConstValues.User}_{dstIdx}_{ConstValues.Demo}";
        }
        
#if UNITY_EDITOR
        srcName = $"{ConstValues.User}_{srcIdx}_{ConstValues.Editor}";
        dstName = $"{ConstValues.User}_{dstIdx}_{ConstValues.Editor}";
#endif
        if (!SaveSystem.Exists(srcName))
            return;
        
        SaveSystem.Copy(srcName, dstName);
    }

    private void FirstStart()
    {
        // 이전 파일의 데이터가 메모리에 남아 새 파일로 새어 들어가지 않도록 통째로 교체
        saveData = new SaveData();
        DefaultSkillSetting();
        DefaultRelicSetting();
        DefaultMapSetting();
        DefaultNpcSetting();
        AddPlayer(ConstValues.Berserker);
        SaveGame();
    }

    public async void GameStart()
    {
        controlStart = true;
        CreatePlayer();
        GameStartSetting();
        InitPlayer();
        InitChangeSkill();
        InitPotionSkill();
        SetPotionCount();
        
        BgmManager.Instance.Stop();
        SoundManager.Instance.PlaySound(ConstValues.Upgrade, true);
        if (await Fading(0, 1, 0.75f, false, ConstValues.BlackColor).SuppressCancellationThrow())
            return;
        
        GoScene(ConstValues.BattleScene);
    }

    public string SaveFileName(int idx)
    {
        string fileName = $"{ConstValues.User}_{idx}";
        
        if(isDemo)
            fileName = $"{ConstValues.User}_{idx}_{ConstValues.Demo}";
        
#if UNITY_EDITOR
        fileName = $"{ConstValues.User}_{idx}_{ConstValues.Editor}";
#endif
        curSaveFileName = fileName;
        
        if (!SaveSystem.Exists(fileName))
            fileName = default;
        
        return fileName;
    }

    private void DefaultSkillSetting()
    {
        FirstGetSkill = false;
        FirstGetAttribute = false;
        FirstGetPotion = false;
        FirstGetRelic = false;
        saveData.playerInfoList = new List<PlayerInfo>();
        
        // 캐릭터가 3마리니까 이것도 3개
        for (int i = 0; i < 3; i++)
        {
            PlayerInfo playerInfo = new PlayerInfo();
            playerInfo.attributePoint = 0;
            playerInfo.skillList = new List<Skill>();
            playerInfo.skillKeyList = new List<SkillKey>();
            
            switch (i)
            {
                case 0:
                    playerInfo.playerId = ConstValues.Berserker;
                    AddDefaultSkill(ConstValues.BerserkerDash, playerInfo);
                    playerInfo.skillKeyList.Add(SetSkillKey(ConstValues.BerserkerDash, dashKey));
                    playerInfo.skillKeyList.Add(SetSkillKey(default, skillKey1));
                    playerInfo.skillKeyList.Add(SetSkillKey(default, skillKey2));
                    playerInfo.skillKeyList.Add(SetSkillKey(default, skillKey3));
                    playerInfo.skillKeyList.Add(SetSkillKey(default, skillKey4));
                    break;
                
                case 1:
                    playerInfo.playerId = ConstValues.Gunner;
                    AddDefaultSkill(ConstValues.GunnerDash, playerInfo);
                    AddDefaultSkill(ConstValues.GunnerGrenade, playerInfo);
                    playerInfo.skillKeyList.Add(SetSkillKey(ConstValues.GunnerDash, dashKey));
                    playerInfo.skillKeyList.Add(SetSkillKey(ConstValues.GunnerGrenade, skillKey1));
                    playerInfo.skillKeyList.Add(SetSkillKey(default, skillKey2));
                    playerInfo.skillKeyList.Add(SetSkillKey(default, skillKey3));
                    playerInfo.skillKeyList.Add(SetSkillKey(default, skillKey4));
                    break;
                
                case 2:
                    playerInfo.playerId = ConstValues.Fighter;
                    AddDefaultSkill(ConstValues.FighterDash, playerInfo);
                    AddDefaultSkill(ConstValues.FighterLightningKick, playerInfo);
                    AddDefaultSkill(ConstValues.FighterLightningPunch, playerInfo);
                    playerInfo.skillKeyList.Add(SetSkillKey(ConstValues.FighterDash, dashKey));
                    playerInfo.skillKeyList.Add(SetSkillKey(ConstValues.FighterLightningKick, skillKey1));
                    playerInfo.skillKeyList.Add(SetSkillKey(ConstValues.FighterLightningPunch, skillKey2));
                    playerInfo.skillKeyList.Add(SetSkillKey(default, skillKey3));
                    playerInfo.skillKeyList.Add(SetSkillKey(default, skillKey4));
                    break;
            }
            saveData.playerInfoList.Add(playerInfo);
        }
    }

    private void LockAttributeSetting()
    {
        if (skillAttributeCopyList.Count > saveData.lockAttributeList.Count)
        {
            foreach (var skillAttributeCopy in skillAttributeCopyList)
            {
                if (!saveData.lockAttributeList.Exists(x => x.id == skillAttributeCopy.id))
                {
                    var attributeLockInfo = new AttributeLockInfo();
                    attributeLockInfo.id = skillAttributeCopy.id;
                    attributeLockInfo.isLock = skillAttributeCopy.firstLock;
                    saveData.lockAttributeList.Add(attributeLockInfo);
                }
            }
        }
        else
        {
            foreach (var skillAttributeCopy in skillAttributeCopyList)
            {
                var targetAttribute = saveData.lockAttributeList.Find(x => x.id == skillAttributeCopy.id);
                if (targetAttribute == null)
                    continue;
                
                if (targetAttribute.isLock && !skillAttributeCopy.firstLock)
                    targetAttribute.isLock = false;
            }
        }
    }

    private void DefaultKeySetting()
    {
        settings.LoadAll();

        // 언어는 LocalizationService 가 소유한다
        language = SettingStringBinding.LoadSetting(ConstValues.Language, Application.systemLanguage.ToString());
    }

    public void SetDefaultGame()
    {
        SettingStringBinding.SaveGameSetting(ConstValues.Language, Application.systemLanguage.ToString());
        language = SettingStringBinding.LoadSetting(ConstValues.Language, Application.systemLanguage.ToString());

        settings.ResetGame();
    }

    public void SetDefaultAudio()
    {
        settings.ResetAudio();
    }

    public void SetDefaultVideo()
    {
        settings.ResetVideo();
    }

    public void SetDefaultKeyboard()
    {
        settings.ResetKeyboard();

        // 세이브에 기록된 스킬 슬롯 keyCode 도 함께 되돌린다
        SyncSkillKeyCode();
    }

    private void DefaultRelicSetting()
    {
        // 최초에 슬롯 두 개 추가
        foreach (var playerInfo in saveData.playerInfoList)
        {
            playerInfo.relicList.Add(default);
            playerInfo.relicList.Add(default);
        }
    }

    private void DefaultMapSetting()
    {
        MiniMapCheckers.Clear();
        RoomInfoList.Clear();
        SavePoint = default;
    }

    private void DefaultNpcSetting()
    {
        NpcInfoList.Clear();
    }

    // 모든 엘리베이터를 최초 시작 인덱스로 되돌린다
    public void ResetElevatorIdx()
    {
        foreach (var roomInfo in saveData.roomInfoList)
            foreach (var elevator in roomInfo.elevators)
                elevator.idx = elevator.startIdx;
    }

    // 게임오버 후 재시작 시: 모든 엘리베이터를 최초 시작 위치로 되돌리고 포션을 다시 채운다
    public void GameOverReset()
    {
        ResetElevatorIdx();
        SetPotionCount();
    }
}
