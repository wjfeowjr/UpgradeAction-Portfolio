// 세이브 파일에 저장되는 최상위 데이터

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerInfo
{
    public string playerId;
    public int attributePoint;
    public List<string> relicList = new List<string>();
    public List<Skill> skillList = new List<Skill>();
    public List<SkillKey> skillKeyList = new List<SkillKey>();
}

[Serializable]
public class AttributeLockInfo
{
    public string id;
    public bool isLock;
}

[Serializable]
public class SaveData
{
    // 재화
    public int gold;
    public int additionPotionCount;

    // 세이브 포인트
    public string savePoint;

    // 마지막 저장 시각 (UTC, ISO 8601 round-trip 포맷)
    public string lastSavedAt;

    public bool firstGetSkill;
    public bool firstGetAttribute;
    public bool firstGetPotion;
    public bool firstGetRelic;
    public bool firstDamaged;
    public bool firstPortal;

    // 위시리스트 유도 팝업에서 "예"를 눌러 스토어로 이동한 적이 있는지
    public bool isWishlistAccepted;
    // 1차 유도(보스 연출 Product6 종료 직후)를 이미 띄웠는지
    public bool isFirstWishlistShown;
    // 2차 유도(데모 마지막 구역 세이브 포인트)를 이미 띄웠는지
    public bool isSecondWishlistShown;

    // 전체 보스 수 (모든 Room의 bosses 배열 크기 합)
    public int bossCount;
    // 처치한 보스 수 (보스방/미니보스방의 첫 연출이 끝난 방의 bosses 크기 합)
    public int curBossCount;

    public List<string> playerList = new List<string>();
    public List<HaveItemInfo> itemList = new List<HaveItemInfo>();
    public List<string> relicList = new List<string>();
    public List<AttributeLockInfo> lockAttributeList = new List<AttributeLockInfo>();
    
    // 플레이어 개별로 만들기(스킬, 스킬 키, 유물)
    public int totalAttributePoint;
    public List<PlayerInfo> playerInfoList = new List<PlayerInfo>();
    public List<Vector2> miniMapCheckers = new List<Vector2>();
    public List<NpcInfo> npcInfoList = new List<NpcInfo>();
    public List<RoomInfo> roomInfoList = new List<RoomInfo>();
}
