using UnityEngine;

public partial class GameManager
{

    public string GetTalk(int idx)
    {
        string talk = default;
        switch (language)
        {
            case ConstValues.Korean:
                talk = TableManager.Instance.talkTable.Talk.Find(x => x.idx == idx).kr;
                break;
            
            case ConstValues.English:
                talk = TableManager.Instance.talkTable.Talk.Find(x => x.idx == idx).en;
                break;

            case ConstValues.Japanese:
                talk = TableManager.Instance.talkTable.Talk.Find(x => x.idx == idx).ja;
                break;

            case ConstValues.ChineseSimplified:
                talk = TableManager.Instance.talkTable.Talk.Find(x => x.idx == idx).cn;
                break;

            case ConstValues.ChineseTraditional:
                talk = TableManager.Instance.talkTable.Talk.Find(x => x.idx == idx).tw;
                break;

            case ConstValues.Spanish:
                talk = TableManager.Instance.talkTable.Talk.Find(x => x.idx == idx).es;
                break;

            case ConstValues.Russian:
                talk = TableManager.Instance.talkTable.Talk.Find(x => x.idx == idx).ru;
                break;

            case ConstValues.PortugueseBrazil:
                talk = TableManager.Instance.talkTable.Talk.Find(x => x.idx == idx).pt;
                break;
        }
        
        return talk;
    }

    public string GetCharacterTalk(string id)
    {
        string talk = default;
        switch (id)
        {
            case ConstValues.Berserker:
                talk = GetTalk(50000);
                break;
            
            case ConstValues.Gunner:
                talk = GetTalk(50001);
                break;
            
            case ConstValues.Fighter:
                talk = GetTalk(50002);
                break;
        }

        return talk;
    }

    public string GetItemTalk(string id)
    {
        int itemName = TableManager.Instance.itemTable.Item.Find(x => x.id == id).name;
        return GetTalk(itemName);
    }

    public string GetItemExplain(string id)
    {
        int itemExplain = TableManager.Instance.itemTable.Item.Find(x => x.id == id).explain;
        return GetTalk(itemExplain);
    }

    public string GetStatName(string statId)
    {
        string value = "Null!";
        switch (statId)
        {
            case ConstValues.CritPercent:
                return GetTalk(50105);
        }

        return value;
    }

    public string GetKeyCode(KeyCode keycode)
    {
        return keycode switch
        {
            KeyCode.LeftArrow => "←",
            KeyCode.RightArrow => "→",
            KeyCode.UpArrow => "↑",
            KeyCode.DownArrow => "↓",
            KeyCode.Escape => "Esc",
            KeyCode.Return => "Enter",
            KeyCode.LeftShift => "Shift",
            KeyCode.BackQuote => "`",
            _ => keycode.ToString()
        };
    }

    public string GetPlaceName(ePlace place)
    {
        switch (place)
        {
            case ePlace.Forest:
                return GetTalk(130000);

            case ePlace.BaseCamp:
                return GetTalk(130001);

            case ePlace.Dungeon:
                return GetTalk(130002);

            case ePlace.Mine:
                return GetTalk(130003);

            case ePlace.SnowField:
                return GetTalk(130004);

            default:
                return "Non";
        }
    }
}
