using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class SaveObject : InteractionController
{
    [SerializeField] private Transform savePointPos;
    [SerializeField] private GameObject minimapObject;

    public Transform SavePointPos => savePointPos;
    public Vector2 ColSize => GetComponent<BoxCollider2D>().size;
    public GameObject MinimapObject => minimapObject;

    public void SetSaveAction(Action action)
    {
        SetInteractionAction(action, 30002, GameManager.Instance.upKey);
    }
    
    public void SetFastTravelAction()
    {
        ActiveInteractionObject(false);
        SetActionInteractionSelect(SetAction, SetInteractionSelectCloseAction);
    }

    private void SetAction(int idx)
    {
        switch (idx)
        {
            case 0:
                SpawnFastTravel();
                break;

            case 1:
                SetInteractionSelectCloseAction();
                break;
        }
    }

    private void SpawnFastTravel()
    {
        // 세이브 포인트가 활성화된 방들을 RoomArray(idx) 순서대로 수집
        var savePointRooms = RoomManager.Instance.GetSavePointRooms();
        var targetPositions = new List<Vector3>();
        var placeNames = new List<string>();
        var roomIds = new List<string>();
        var startIndex = 0;
        for (int i = 0; i < savePointRooms.Count; i++)
        {
            targetPositions.Add(savePointRooms[i].SaveObject.transform.position);
            placeNames.Add(savePointRooms[i].Place);
            roomIds.Add(savePointRooms[i].Id);

            // 현재 상호작용 중인 세이브 포인트(this)에서 시작
            if (savePointRooms[i].SaveObject == this)
                startIndex = i;
        }

        // 선택지를 숨기고 패스트 트래블 팝업을 띄운다
        ActiveInteractionSelect(false);

        var uiBase = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_FastTravel, Vector3.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is Popup_FastTravel popupFastTravel)
        {
            var fastTravelInterface = popupFastTravel.FastTravelView.ConvertTo<IPopupFastTravelView>();
            // 닫기 연타 시 중복 호출을 막기 위한 플래그
            var isClosing = false;
            var fastTravelModel = new PopupFastTravelModel()
            {
                moveText = string.Format(GameManager.Instance.GetTalk(30110), GameManager.Instance.GetKeyCode(GameManager.Instance.upKey), GameManager.Instance.GetKeyCode(GameManager.Instance.downKey)),
                selectText = string.Format(GameManager.Instance.GetTalk(30103), GameManager.Instance.GetKeyCode(GameManager.Instance.confirmKey)),
                cancelText = string.Format(GameManager.Instance.GetTalk(30102), GameManager.Instance.GetKeyCode(GameManager.Instance.escKey)),

                targetPositions = targetPositions,
                placeNames = placeNames,
                roomIds = roomIds,
                startIndex = startIndex,
                miniMapCamera = GameManager.Instance.MiniMapCamera,
                closeAction = () =>
                {
                    if (isClosing)
                        return;
                    isClosing = true;
                    CloseFastTravelAsync(uiBase).Forget();
                },
                selectAction = (index) =>
                {
                    if (isClosing)
                        return;
                    isClosing = true;
                    FastTravelSelectAsync(uiBase, roomIds[index]).Forget();
                }
            };
            var fastTravelPresenter = new PopupFastTravelPresenter(fastTravelInterface, fastTravelModel);
            popupFastTravel.SetFastTravelPresenter(fastTravelPresenter);
            fastTravelPresenter.SetFastTravelText();
            fastTravelPresenter.Expansion(() =>
            {
                uiBase.FadeOpen(true, true, 0.1f, false).Forget();
            });
            // 시작 위치(현재 방)로 카메라 이동 및 place 표시
            fastTravelPresenter.Open();
        }
    }

    private async UniTaskVoid CloseFastTravelAsync(UIBase uiBase)
    {
        await uiBase.FadeClose(true, true, 0.1f);
        // 팝업 닫힘 트윈이 끝난 뒤에 선택지 상태와 조작을 복구한다
        SetInteractionSelectCloseAction();
    }

    private async UniTaskVoid FastTravelSelectAsync(UIBase uiBase, string roomId)
    {
        // 조작 복구는 FastTravelMove 쪽에서 처리하므로 controlStart=false
        await uiBase.ReductionClose(true, false);
        // 현재 방에서 선택한 세이브 포인트(roomId)로 패스트 트래블
        RoomManager.Instance.CurrentRoom.FastTravelMove(roomId);
    }

    public void SetSelectAction()
    {
        List<int> selectIdx = new List<int>
        {
            20011,
            20012
        };
        SpawnInteractionSelect(selectIdx);
    }

    public void SetParents(Transform targetTransform)
    {
        minimapObject.transform.SetParent(targetTransform);
    }
}
