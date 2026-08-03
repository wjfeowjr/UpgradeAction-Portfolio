using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class PortalObject : InteractionController
{
    [SerializeField] private Transform playerPos;
    [SerializeField] private GameObject minimapObject;
    [SerializeField] private AudioSource myAudioSource;
    [SerializeField] private string targetRoom;

    private float volume;

    public Transform PlayerPos => playerPos;
    public Vector2 ColSize => GetComponent<BoxCollider2D>().size;
    public GameObject MinimapObject => minimapObject;

    private void Awake()
    {
        volume = myAudioSource.volume;
    }

    private void OnEnable()
    {
        myAudioSource.volume = volume;
    }

    public void SetPortalAction(Action action)
    {
        SetInteractionAction(action, 30018, GameManager.Instance.upKey);
    }

    public void SetParents(Transform targetTransform)
    {
        minimapObject.transform.SetParent(targetTransform);
    }

    public void SoundActive(bool active)
    {
        if (active)
            myAudioSource.volume = volume;
        else
            myAudioSource.volume = 0;
    }

    // 발견한 포탈 목록을 패스트 트래블 UI로 띄우고, Enter 선택 시 MovePortal 실행
    public void SpawnFastTravel()
    {
        // 포탈이 발견된 방들을 RoomArray(idx) 순서대로 수집
        var portalRooms = RoomManager.Instance.GetPortalRooms();
        var targetPositions = new List<Vector3>();
        var placeNames = new List<string>();
        var roomIds = new List<string>();
        var startIndex = 0;
        for (int i = 0; i < portalRooms.Count; i++)
        {
            targetPositions.Add(portalRooms[i].PortalObject.transform.position);
            placeNames.Add(portalRooms[i].Place);
            roomIds.Add(portalRooms[i].Id);

            // 현재 포탈(this)이 있는 방에서 시작
            if (portalRooms[i].PortalObject == this)
                startIndex = i;
        }

        var uiBase = GameManager.Instance.SpawnToPopupPool(eUIType.Popup_FastTravel, Vector3.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is Popup_FastTravel popupFastTravel)
        {
            // 닫기/선택 연타 시 중복 호출을 막기 위한 플래그
            var isClosing = false;
            var fastTravelModel = new PopupFastTravelModel()
            {
                moveText = string.Format(GameManager.Instance.GetTalk(30110), GameManager.Instance.GetKeyCode(GameManager.Instance.upKey), GameManager.Instance.GetKeyCode(GameManager.Instance.downKey)),
                selectText = string.Format(GameManager.Instance.GetTalk(30103), GameManager.Instance.GetKeyCode(GameManager.Instance.enterKey)),
                cancelText = string.Format(GameManager.Instance.GetTalk(30102), GameManager.Instance.GetKeyCode(GameManager.Instance.escKey)),

                targetPositions = targetPositions,
                placeNames = placeNames,
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
            var fastTravelPresenter = popupFastTravel.FastTravelView.Bind(fastTravelModel);
            popupFastTravel.SetFastTravelPresenter(fastTravelPresenter);
            fastTravelPresenter.SetFastTravelText();
            fastTravelPresenter.Fade(() =>
            {
                uiBase.FadeOpen(true, true, 0.1f, false).Forget();
            });
            // 시작 위치(현재 방)로 카메라 이동 및 목록/선택 상태 초기화
            fastTravelPresenter.Open();
        }
    }

    private async UniTaskVoid CloseFastTravelAsync(UIBase uiBase)
    {
        await uiBase.FadeClose(true, true, 0.1f);
        SetInteractionSelectCloseAction();
    }

    private async UniTaskVoid FastTravelSelectAsync(UIBase uiBase, string roomId)
    {
        // 조작 복구는 MovePortal 쪽에서 처리하므로 controlStart=false
        await uiBase.FadeClose(true, false, 0.1f);
        // 현재 방에서 선택한 포탈(roomId)로 이동
        RoomManager.Instance.CurrentRoom.MovePortal(roomId);
    }
}
