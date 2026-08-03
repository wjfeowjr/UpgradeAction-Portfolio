// NPC · 대화 런타임 데이터

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NpcCopy
{
    public string id;
    public int talk;
    public string firstDialog;
    public string startDialog;
    public List<string> dialogKey = new List<string>();
    public List<string> questItemId = new List<string>();
    public List<int> questItemCount = new List<int>();
    public string questClearChoice;
}

[Serializable]
public class DialogueChoiceCopy
{
    public string id;
    public string npc;
    public int talk;
    public List<string> checkKey = new List<string>();
    public List<bool> checkKeyValue = new List<bool>();
}

[Serializable]
public class NpcInfo
{
    public string id;
    public List<DialogKey> dialogKey = new List<DialogKey>();
    public bool isFirstDialogFinish;
}

[Serializable]
public class DialogKey
{
    public string id;
    public bool isUse;
}
