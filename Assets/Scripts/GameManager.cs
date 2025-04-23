using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.U2D;

public static class KeyBinding
{
    // 저장할 때
    public static void SaveKey(string prefKey, KeyCode key)
    {
        PlayerPrefs.SetInt(prefKey, (int)key);
        PlayerPrefs.Save();
    }

    // 불러올 때 (디폴트 키도 지정 가능)
    public static KeyCode LoadKey(string prefKey, KeyCode defaultKey)
    {
        if (PlayerPrefs.HasKey(prefKey))
        {
            Debug.Log($"저장된 키값 {PlayerPrefs.GetInt(prefKey)}을 불러왔습니다");
            return (KeyCode)PlayerPrefs.GetInt(prefKey);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"최초 키 설정: {prefKey}를 {defaultKey}로 저장");
            SaveKey(prefKey, defaultKey);
            return defaultKey;
        }
    }
}

public static class SkillBinding
{
    // 저장할 때
    public static void SaveKey(string skillCollection)
    {
        PlayerPrefs.SetString(ConstValues.PlayerSkill, skillCollection);
        PlayerPrefs.Save();
    }

    // 불러올 때
    public static string LoadSkillCollection(string defaultCollection)
    {
        if (PlayerPrefs.HasKey(ConstValues.PlayerSkill))
        {
            Debug.Log($"저장된 스킬 리스트가 존재");
            return PlayerPrefs.GetString(ConstValues.PlayerSkill);
        }
        else
        {
            // 처음 실행 시 디폴트 키를 저장
            Debug.Log($"스킬 리스트 최초 생성");
            SaveKey(defaultCollection);
            return defaultCollection;
        }
    }
}

[Serializable]
public class SkillKey
{
    public string skillId;
    public KeyCode keyCode;
}

[Serializable]
public class SkillKeyCollection
{
    public List<SkillKey> berserkerSkillKeyList;
}

[Serializable]
public class SettingSkill
{
    public string skillId;
    public KeyCode keyCode;
    public PlayerSkill playerSkill;
}

public class GameManager : Singleton<GameManager>
{
    public Material defaultMaterial;
    public Material hitMaterial;
    
    public KeyCode leftMoveKey;
    public KeyCode rightMoveKey;
    public KeyCode attackKey;
    public KeyCode jumpKey;
    
    public KeyCode dashKey;
    public KeyCode skillKey1;
    public KeyCode skillKey2;
    public KeyCode skillKey3;
    public KeyCode skillKey4;
    public KeyCode skillKey5;
    public KeyCode skillKey6;
    public KeyCode skillKey7;
    public KeyCode skillKey8;

    [SerializeField] private SpriteAtlas uiAtlas;
    private Sprite[] cloneSprites;
    private Dictionary<string, Sprite> atlasDic = new Dictionary<string, Sprite>();

    [SerializeField] private Player player;
    [SerializeField] private Transform objectPool;
    [SerializeField] private Transform uiPool;
    [SerializeField] private List<GameObject> prefabList;
    [SerializeField] private List<GameObject> uiList = new List<GameObject>();
    [SerializeField] private List<GameObject> objectList = new List<GameObject>();
    
    // 등록된 스킬 목록
    public SkillKeyCollection playerSkillKeyCollection;

    // 매니저들
    public TableManager tableManager;
    public UIManager uiManager;
    public ResourceManager resourceManager;
    
    protected override void Awake()
    {
        base.Awake();
        Application.targetFrameRate = 60;
        DefaultKeySetting(); 
        InitManager();
        InitAtlas();
    }

    public void GoScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public Player GetPlayer()
    {
        return player;
    }
    public void SetPlayer(Player targetPlayer)
    {
        player = targetPlayer;
    }

    private void DefaultKeySetting()
    {
        //PlayerPrefs.DeleteAll();
        
        leftMoveKey = KeyBinding.LoadKey(ConstValues.LeftMoveKey, KeyCode.LeftArrow);
        rightMoveKey = KeyBinding.LoadKey(ConstValues.RightMoveKey, KeyCode.RightArrow);
        attackKey = KeyBinding.LoadKey(ConstValues.AttackKey, KeyCode.X);
        jumpKey = KeyBinding.LoadKey(ConstValues.JumpKey, KeyCode.C);
        dashKey = KeyBinding.LoadKey(ConstValues.DashKey, KeyCode.Z);
        
        skillKey1 = KeyBinding.LoadKey(ConstValues.SkillKey1, KeyCode.Q);
        skillKey2 = KeyBinding.LoadKey(ConstValues.SkillKey2, KeyCode.W);
        skillKey3 = KeyBinding.LoadKey(ConstValues.SkillKey3, KeyCode.E);
        skillKey4 = KeyBinding.LoadKey(ConstValues.SkillKey4, KeyCode.R);
        skillKey5 = KeyBinding.LoadKey(ConstValues.SkillKey5, KeyCode.A);
        skillKey6 = KeyBinding.LoadKey(ConstValues.SkillKey6, KeyCode.S);
        skillKey7 = KeyBinding.LoadKey(ConstValues.SkillKey7, KeyCode.D);
        skillKey8 = KeyBinding.LoadKey(ConstValues.SkillKey8, KeyCode.F);

        InitBerserkerSkillKey();
        //SkillTest();
    }

    private void InitBerserkerSkillKey()
    {
        List<SkillKey> berserkerSkillKeyList = new List<SkillKey>();
        
        SkillKey dash = new SkillKey()
        {
            skillId = ConstValues.BerserkerDash,
            keyCode = dashKey,
        };
        berserkerSkillKeyList.Add(dash);
        
        SkillKey skill1 = new SkillKey()
        {
            skillId = default,
            keyCode = skillKey1,
        };
        berserkerSkillKeyList.Add(skill1);
        
        SkillKey skill2 = new SkillKey()
        {
            skillId = default,
            keyCode = skillKey2,
        };
        berserkerSkillKeyList.Add(skill2);
        
        SkillKey skill3 = new SkillKey()
        {
            skillId = default,
            keyCode = skillKey3,
        };
        berserkerSkillKeyList.Add(skill3);
        
        SkillKey skill4 = new SkillKey()
        {
            skillId = default,
            keyCode = skillKey4,
        };
        berserkerSkillKeyList.Add(skill4);
        
        SkillKey skill5 = new SkillKey()
        {
            skillId = default,
            keyCode = skillKey5,
        };
        berserkerSkillKeyList.Add(skill5);
        
        SkillKey skill6 = new SkillKey()
        {
            skillId = ConstValues.BerserkerUpperSlash,
            keyCode = skillKey6,
        };
        berserkerSkillKeyList.Add(skill6);
        
        SkillKey skill7 = new SkillKey()
        {
            skillId = ConstValues.BerserkerCrash,
            keyCode = skillKey7,
        };
        berserkerSkillKeyList.Add(skill7);
        
        SkillKey skill8 = new SkillKey()
        {
            skillId = ConstValues.BerserkerFireStrike,
            keyCode = skillKey8,
        };
        berserkerSkillKeyList.Add(skill8);
        playerSkillKeyCollection.berserkerSkillKeyList = berserkerSkillKeyList;
        
        // json화
        string json = JsonUtility.ToJson(playerSkillKeyCollection, true);
        var loadJson = SkillBinding.LoadSkillCollection(json);
        
        var loadedSkillKeyCollection = JsonUtility.FromJson<SkillKeyCollection>(loadJson);
        playerSkillKeyCollection = loadedSkillKeyCollection;
    }
    public void SetBerserkerSkillId(KeyCode keyCode, string skillId)
    {
        playerSkillKeyCollection.berserkerSkillKeyList.Find(x => x.keyCode == keyCode).skillId = skillId;

        // 저장
        string json = JsonUtility.ToJson(playerSkillKeyCollection, true);
        SkillBinding.SaveKey(json);
    }
    public List<SettingSkill> GetSettingSkillList()
    {
        var keyList = playerSkillKeyCollection.berserkerSkillKeyList;
        if(player.GetBasicStat().id == ConstValues.Berserker)
            keyList = playerSkillKeyCollection.berserkerSkillKeyList;

        List<SettingSkill> settingSkillList = new List<SettingSkill>();
        foreach (var key in keyList)
        {
            SettingSkill settingSkill = new SettingSkill()
            {
                skillId = key.skillId,
                keyCode = key.keyCode,
            };
            settingSkillList.Add(settingSkill);
        }

        var playerSkillList = player.GetSkillList();
        foreach (var playerSkill in playerSkillList)
        {
            var matchSkillList = settingSkillList.FindAll(x => x.skillId == playerSkill.skillName);
            foreach (var matchSkill in matchSkillList)
            {
                matchSkill.playerSkill = playerSkill;
            }
        }

        return settingSkillList;
    }

    public List<SkillKey> GetBerserkerSkillKeyList()
    {
        return playerSkillKeyCollection.berserkerSkillKeyList;
    }

    private void InitAtlas()
    {
        // Atlas 안에 들어있는 스프라이트 개수만큼 배열 생성
        cloneSprites = new Sprite[uiAtlas.spriteCount];

        // GetSprites 호출 시 배열에 모두 채워진다
        uiAtlas.GetSprites(cloneSprites);

        foreach (var sprite in cloneSprites)
        {
            sprite.name = sprite.name.Split(ConstValues.AtlasClone)[0];
            atlasDic.Add(sprite.name, sprite);
        }
    }
    public Sprite GetUISprite(string id)
    {
        return atlasDic[id];
    }

    private async void InitManager() 
    {
        tableManager = TableManager.Instance;
        resourceManager = ResourceManager.Instance;
        uiManager = UIManager.Instance;
        
        tableManager.Init();
        uiManager.Init();
        await resourceManager.Init();
    }
    
    private async void OpenUI()
    {
        //await UIManager.Instance.OpenAsync(eUIType.UI_Skill, model);
    }

    public GameObject SpawnToObjectPool(string id, Transform objTransform)
    {
        var objectName = $"{id}(Clone)";
        var isSearch = objectList.FindAll(x => x.name == objectName);
        
        GameObject go;
        if (isSearch.Count == 0)
        {
            go = Instantiate(prefabList.Find(x => x.name == id).gameObject, objectPool);
            objectList.Add(go);
        }
        else
        {
            var recycleObj = isSearch.Find(x => !x.activeSelf);
            if (recycleObj == null)
            {
                go = Instantiate(prefabList.Find(x => x.name == id).gameObject, objectPool);
                objectList.Add(go);
            }
            else
            {
                go = recycleObj;
                go.SetActive(true);
            }
        }
        go.transform.position = objTransform.position;
        return go;
    }
    public GameObject SpawnToObjectPool(string id, Vector2 objVector)
    { 
        var objectName = $"{id}(Clone)";
        var isSearch = objectList.FindAll(x => x.name == objectName);
        
        GameObject go;
        if (isSearch.Count == 0)
        {
            go = Instantiate(prefabList.Find(x => x.name == id).gameObject, objectPool);
            objectList.Add(go);
        }
        else
        {
            var recycleObj = isSearch.Find(x => !x.activeSelf);
            if (recycleObj == null)
            {
                go = Instantiate(prefabList.Find(x => x.name == id).gameObject, objectPool);
                objectList.Add(go);
            }
            else
            {
                go = recycleObj;
                go.SetActive(true);
            }
        }
        go.transform.position = objVector;
        return go;
    }
    
    public GameObject SpawnToUIPool(string id, Transform uiTransform = null)
    {
        var objectName = $"{id}(Clone)"; 
        var isSearch = objectList.FindAll(x => x.name == objectName);
        
        GameObject go;
        if (isSearch.Count == 0)
        {
            go = Instantiate(uiList.Find(x => x.name == id).gameObject, uiPool);
            objectList.Add(go);
        }
        else
        {
            var recycleObj = isSearch.Find(x => !x.activeSelf);
            if (recycleObj == null)
            {
                go = Instantiate(uiList.Find(x => x.name == id).gameObject, uiPool);
                objectList.Add(go);
            }
            else
            {
                go = recycleObj;
                go.SetActive(true);
            }
        }
        if(uiTransform != null)
            go.transform.position = uiTransform.position;
        return go;
    } 
}
