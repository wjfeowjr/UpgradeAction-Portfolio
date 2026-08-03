// 스킬 · 스킬 특성 런타임 데이터

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Skill
{
    public string skillId;
    public List<string> attributeList = new List<string>();
}

[Serializable]
public class SkillAttributeCopy
{
    public string id;
    public string skill;
    public string targetObject;
    public int cost;
    public List<string> passiveId = new List<string>();
    
    public string addObjectId;
    public string objectId;
    public int objectCount;
    
    public List<string> upgradeId = new List<string>();
    public List<int> upgradeValue = new List<int>();
    public string buffId;
    public string deBuffId;
    public float buffTime;
    public int buffValue;
    public int talk;
    public int explainTalk;
    public bool firstLock;
}

[Serializable]
public class SkillAttributeAddObjectInfo
{
    public string addObjectId;
    public string objectId;
    public int objectCount;
}

[Serializable]
public class SkillAttributeUpgradeInfo
{
    public string upgradeId;
    public int upgradeValue;
}
[Serializable]
public class SkillAttributeBuffInfo
{
    public string buffId;
    public float buffTime;
    public int buffValue;
}

[Serializable]
public class SkillKey
{
    public string skillId;
    public KeyCode keyCode;
}
[Serializable]
public class SettingSkill
{
    public string skillId;
    public KeyCode keyCode;
    public PlayerSkill playerSkill;
}
