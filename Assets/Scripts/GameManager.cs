using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : Singleton<GameManager>
{
    public Material defaultMaterial;
    public Material hitMaterial;
    
    public KeyCode moveLeftKey;
    public KeyCode moveRightKey;
    public KeyCode attackKey;
    public KeyCode jumpKey;
    public KeyCode dashKey;
    public KeyCode skillKey1;
    public KeyCode skillKey2;
    public KeyCode skillKey3;
    public KeyCode skillKey4;
    public KeyCode skillKey5;
    
    [SerializeField] private Player player;
    [SerializeField] private Transform objectPool;
    [SerializeField] private Transform uiPool;
    [SerializeField] private List<GameObject> prefabList;
    [SerializeField] private List<GameObject> uiList = new List<GameObject>();
    [SerializeField] private List<GameObject> objectList = new List<GameObject>();

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
    }

    private void Start()
    {
        OpenUI();
    }

    public Player GetPlayer()
    {
        return player;
    }

    private void DefaultKeySetting()
    {
        moveLeftKey = KeyCode.LeftArrow;
        moveRightKey = KeyCode.RightArrow;
        attackKey = KeyCode.X;
        jumpKey = KeyCode.C; 
        dashKey = KeyCode.Z;
        skillKey1 = KeyCode.A;
        skillKey2 = KeyCode.S;
        skillKey3 = KeyCode.D;
        skillKey4 = KeyCode.F;
        skillKey5 = KeyCode.E;
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
        var model = new UICommonModel
        {
            skillList = player.GetSkillList(), 
        };
        await UIManager.Instance.OpenAsync(eUIType.UI_Skill, model);
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
