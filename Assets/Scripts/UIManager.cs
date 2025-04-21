using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

public enum ePoolType
{
    None,
    UI,
    Popup,
}

public enum eUIType
{
    None,
    
    // UI
    UI_Skill,
    
    // 팝업
    Popup_Common,
}

public class UIManager : SingletonMono<UIManager>
{
    private bool isActive = false;
    
    private EventSystem eventSystem;
    private GameObject uiRootObject;

    [SerializeField] private List<UIBase> uiList = new List<UIBase>();
    private Dictionary<ePoolType, RectTransform> dicPool = new Dictionary<ePoolType, RectTransform>();

    private void Update()
    {
        
    }

    public void RefreshSkillUI()
    {
        
    }

    #region 초기화
    public void Init()
    {
        var uiRoot = GameObject.Find("UICanvas");
        if (uiRootObject == null)
        {
            uiRootObject = uiRoot;
            if (uiRootObject == null)
                return;
            
            uiRootObject.name = "UICanvas";

            var uiPool = Utils.Search<RectTransform>(uiRootObject, ConstValues.RepositoryUI);
            var popupPool = Utils.Search<RectTransform>(uiRootObject, ConstValues.RepositoryPopup);

            dicPool.Add(ePoolType.UI, uiPool);
            dicPool.Add(ePoolType.Popup, popupPool);

            eventSystem = EventSystem.current;
            //DontDestroyOnLoad(uiRootObject);
        }
        else
        {
            if (uiRoot != null)
                Destroy(uiRoot);
        }
    }
    #endregion

    #region UI 컨트롤
    // 해당 타입의 UI를 닫는다.
    public void Close(eUIType uiType)
    {
        if (IsExistUI(uiType))
            GetUIType(uiType).Close();
    }

    // UI와 팝업이 모두 열려있는지 확인
    private bool IsUIOpen()
    {
        if (uiList.Count > 0)
            return true;

        return false;
    }
    #endregion

    #region UI 생성
    // 바인딩
    private void BindPresenter(eUIType type, UIBase uiBase, object model)
    {
        switch (type)
        {
            case eUIType.Popup_Common:
                if (uiBase is IPopupCommonView v2 && model is PopupCommonModel m2)
                    new PopupCommonPresenter(v2, m2);
                break;
        }
    }
    // 비동기 방식
    public async UniTask<UIBase> OpenAsync(eUIType type, object model)
    {
        SetActive(false);
        var uiBase = await CreateAsync(type);
        if (uiBase != null)
        {
            BindPresenter(type, uiBase, model);
            SetActive(true);
            return uiBase;
        }
        Debug.LogError($"{type}타입의 UI를 찾을 수 없다");
        SetActive(true);
        return null;
    }

    public UIBase Open(eUIType type, object model)
    {
        var uiBase = Create(type);
        if (uiBase != null)
        {
            BindPresenter(type, uiBase, model);
            if (IsUIOpen())
                Input.multiTouchEnabled = false;
            return uiBase;
        }
        Debug.LogError($"{type}타입의 UI를 찾을 수 없다");
        SetActive(true);
        return null;
    }

    // 리스트에서 UI찾아 반환하기
    private UIBase FindUI(eUIType uiType)
    {
        var findUI = uiList.Find(x => x.uiType == uiType);
            
        // 리스트에 해당 UI가 존재한다면 해당 UI를 활성화 시켜주고, 맨 앞으로 레이아웃을 잡아준다(최신 UI이기 때문)
        if (findUI == null)
            return null;
        
        findUI.gameObject.SetActive(true);
        if(findUI.transform.parent != null)
            findUI.transform.SetSiblingIndex(findUI.transform.parent.childCount - 1);
            
        return findUI;
    }

    // UI 생성(비동기)
    private async UniTask<UIBase> CreateAsync(eUIType uiType)
    {
        var findUI = FindUI(uiType);
        if (findUI != null)
            return findUI;

        var path = $"{uiType}{ConstValues.Prefab}";
        var objAsset = await ResourceManager.Instance.LoadAssetAsync<GameObject>(path);
        return objAsset != null ? CreateUI(objAsset, uiType) : null;
    }
    // UI 생성(동기)
    private UIBase Create(eUIType uiType)
    {
        var findUI = FindUI(uiType);
        if (findUI != null)
            return findUI;
        
        var path = $"{uiType}{ConstValues.Prefab}";
        var objAsset = ResourceManager.Instance.LoadAsset<GameObject>(path);
        return objAsset != null ? CreateUI(objAsset, uiType) : null;
    }

    private UIBase CreateUI(GameObject prefab, eUIType type)
    {
        var obj = Instantiate(prefab);
        obj.name = prefab.name;

        var baseInfo = obj.GetComponent<UIBase>();
        if (baseInfo == null)
            baseInfo = obj.AddComponent<UIBase>();
        
        uiList.Add(baseInfo);

        ePoolType poolType = ConvertType(type);
        if (poolType != ePoolType.None)
        {
            baseInfo.transform.SetParent(dicPool[poolType], false);
        }

        baseInfo.Setup(type);
        return baseInfo;
    }
    #endregion

	#region UI 관리
    // 리스트에 해당 타입의 UI가 존재하는지 확인한다
    private bool IsExistUI(eUIType uiType)
    {
        return uiList.Find(x => x.uiType == uiType) != null;
    }

    private UIBase GetUIType(eUIType uiType)
    {
        return IsExistUI(uiType) ? uiList.Find(x => x.uiType == uiType) : null;
    }

    // 해당 프리팹의 _앞 부분을 확인 후, UI면 UI풀에, Popup이면 Popup풀에 자식 오브젝트로 저장한다
    private ePoolType ConvertType(eUIType uiType)
    {
        string poolName = uiType.ToString().Split('_')[0];
        if (Enum.TryParse(poolName, out ePoolType poolType))
            return poolType;

        return ePoolType.None;
    }

    // 이벤트 시스템 막기
    private void SetActive(bool active)
    {
        isActive = active;
        if (eventSystem != null)
            eventSystem.enabled = isActive;
    }
    #endregion
}
