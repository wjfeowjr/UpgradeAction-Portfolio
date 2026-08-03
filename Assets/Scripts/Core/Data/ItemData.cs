// 아이템 · 유물 · 패시브 런타임 데이터

using System;
using System.Collections.Generic;
using UnityEngine;

public enum eItemRank
{
    Normal,
    Rare,
}

[Flags]
public enum eItemStat
{
    Power,
    Defence,
    MoveSpeed,
    AttackSpeed,
    CriticalPercent,
    CriticalDamage,
    StaggerDamage,
}

[Serializable]
public class RelicCopy
{
    public string id;
    public int name;
    public int explain;
    public eItemRank rank;
    public List<eItemStat> statList = new List<eItemStat>();
    public List<int> valueList = new List<int>();
    public string specialValue;
}

[Serializable]
public class PassiveCopy
{
    public string id;
    public int valueResource;
    public string resourceStat;
    public int resourceValue;
    public string resourceUnit;
    public int getBuffResource;
    public float buffTime;
    public string buffId;
    public int buffValue;
    public string buffUnit;
    public int penaltyValue;
    public int passiveName;
    public int passiveExplain;
}

public enum eItemType
{
    Normal,
    Relic,
}

[Serializable]
public class ItemCopy
{
    public string id;
    public int name;
    public int explain;
    public eItemRank rank;
    public eItemType type;
}

[Serializable]
public class HaveItemInfo
{
    public string id;
    public int count;
}
