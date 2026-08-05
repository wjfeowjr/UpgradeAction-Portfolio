// GameManager - UI 스폰 · 갱신
using System;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;


public partial class GameManager
{

    // UI관련 코드
    public void SpawnGameInterface()
    {
        if (!uiInterface)
        {
            uiInterface = SpawnToUIPool(eUIType.UI_Interface, Vector2.zero).GetComponent<UI_Interface>();
            uiInterface.Setup(eUIType.UI_Interface);
        }

        uiInterface.ComboView.SetCombo();

        RefreshFace();
        RefreshPlayerHp();
        RefreshPlayerResource();

        uiInterface.BossHpView.HideHp();
        uiInterface.PlaceNameView.HideImmediate();
        uiInterface.ObjectInfoView.HideImmediate();

        uiInterface.BindSkill(() => new UISkillModel
        {
            changeSkill = changeSkill,
            potionSkill = potionSkill,
            settingSkillList = GetSettingSkillList()
        }).SetSkillInfo();

        RefreshPotionActive();

    }

    // delay: 경고 문구가 유지되는 시간. 기본 1.2초, 더 길거나 짧게 보여주고 싶을 때만 지정한다
    public async UniTask SpawnWarningPopup(string message, float delay = 1.2f)
    {
        if (popupWarning)
        {
            popupWarning.gameObject.SetActive(true);
        }
        else
        {
            popupWarning = SpawnToHighestPool(eUIType.Popup_Warning, Vector3.zero).GetComponent<Popup_Warning>();
        }

        var warningModel = new PopupWarningModel()
        {
            message = message,
            delay = delay,
        };
        await popupWarning.WarningView.SetMessage(warningModel);
    }

    public void GetGold(int getGold, int totalGold)
    {
        uiInterface.GoodsView.SetGoldText(new UIGoodsModel
        {
            getGold = getGold,
            totalGold = totalGold,
        });
    }

    public void RefreshGoods()
    {
        Gold = saveData.gold;

        uiInterface.GoodsView.SetGoldText(new UIGoodsModel { totalGold = Gold });
    }

    public void RefreshPlaceName()
    {
        if (RoomManager.Instance == null || RoomManager.Instance.CurrentRoom == null)
            return;

        uiInterface.PlaceNameView.SetPlaceText(new UIPlaceNameModel
        {
            placeName = RoomManager.Instance.CurrentRoom.Place,
        });
    }

    public void ProductObjectInfo(string id, string objectName, int count)
    {
        uiInterface.ObjectInfoView.SetObjectText(new UIObjectInfoModel
        {
            id = id,
            objectName = objectName,
            count = count,
        });
    }

    public void HidePlaceName()
    {
        uiInterface.PlaceNameView?.HideImmediate();
    }

    public GameObject GetUI(eUIType type)
    {
        GameObject result = null;
        foreach (var go in pool.AllInstances)
        {
            if (go.GetComponent<UIBase>() && go.GetComponent<UIBase>().GetUIType() == type)
            {
                result = go;
                break;
            }
        }
        return result;
    }

    public SpeechFrame GetSpeechFrame(string frameName)
    {
        SpeechFrame speechFrame;
        if(frameName == ConstValues.SpeechFrameTitle)
            speechFrame = SpawnToHighestPool(frameName, Vector2.zero).GetComponent<SpeechFrame>();
        else
            speechFrame = SpawnToUIObjectPool(frameName, Vector2.zero).GetComponent<SpeechFrame>();

        var objectData = TableManager.Instance.GetSpawnedObject(frameName);
        if (objectData == null)
            return speechFrame;
        
        var spawnedObject = speechFrame.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = speechFrame.AddComponent<SpawnedObject>();
            
        spawnedObject.SetupData(objectData, transform.localScale.x);
        spawnedObject.EnableSetting();

        if (spawnedObject.GetTrace())
        {
            var trace = speechFrame.GetComponent<Trace>();
            if(!trace)
                speechFrame.AddComponent<Trace>();
        }
        
        return speechFrame;
    }

    public void GetSkillProduct(string id, Action<string, string, int> customAction)
    {
        CurPlayer.SpawnObject(ConstValues.GetSkillExplosion, CurPlayer.CenterPos.position);
        var skillName = GetSkillName(id);
        customAction.Invoke(id, skillName, 1);
    }

    public void GetAttributeProduct(int count, Action<int> customAction)
    {
        customAction.Invoke(count);
    }

    public void GetPotionProduct(Action customAction)
    {
        customAction.Invoke();
    }

    public void GetRelicProduct(string relicId, Action<string> customAction)
    {
        customAction.Invoke(relicId);
    }

    public void GetGoldProduct(int count, Vector2 boxPos, Action<int> customAction)
    {
        CurPlayer.SpawnObject(ConstValues.BangEffect, boxPos);
        customAction.Invoke(count);
    }

    public string GetThousandCommaText(int data)
    {
        if (data == 0)
            return 0.ToString();
        
        return $"{data:#,###}";
    }

    public void SpawnSelect(string message, Sprite goodsSprite, int cost, Action yesAction, Action noAction, bool yes = true)
    {
        var uiBase = SpawnToPopupPool(eUIType.Popup_Select, Vector3.zero).GetComponent<UIBase>();
        
        if (uiBase is Popup_Select popupSelect)
        {
            var common = new PopupCommonActions
            {
                PlayMoveSound   = () => SoundManager.Instance.PlaySound(ConstValues.Jump1,         true),
                PlaySelectSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton2,  true),
                PlayCancelSound = () => SoundManager.Instance.PlaySound(ConstValues.NormalButton,   true),
            };
            var selectModel = new PopupSelectModel()
            {
                yes = yes,
                message = message,
                goods = goodsSprite,
                cost = cost,
                startAction = HideHighestObjects,
                yesAction = () =>
                {
                    uiBase.Close();
                    yesAction();
                },
                noAction = ()=>
                {
                    uiBase.Close();
                    noAction();
                },
                escAction = ()=>
                {
                    uiBase.Close();
                    noAction();
                },
                commonActions = common
            };
            
            uiBase.ExpansionOpen(false, false).Forget();
            popupSelect.SelectView.SetData(selectModel);
        }
    }
}
