using System;
using Steamworks;
using UnityEngine;

public class SteamWorksManager : SingletonMono<SteamWorksManager>
{
    public void SteamCheck()
    {
        Debug.Log($"[Steam] Initialized = {SteamManager.Initialized}");

        if (SteamManager.Initialized)
        {
            Debug.Log($"[Steam] User = {SteamFriends.GetPersonaName()}, AppID = {SteamUtils.GetAppID()}");
        }
    }
    
    // 데모 마지막 구역의 세이브 포인트에서 최초 1회만 위시리스트를 유도한다.
    // 팝업을 띄웠으면 true를 반환하며, closeAction은 예/아니오 어느 쪽을 골라도 팝업이 닫힌 뒤 호출된다.
    public bool TryShowWishlistPopup(Action closeAction)
    {
        if (GameManager.Instance.IsWishlistPopupShown)
            return false;

        // 예/아니오와 무관하게 다시 묻지 않도록 띄우는 시점에 기록한다
        GameManager.Instance.IsWishlistPopupShown = true;
        GameManager.Instance.SaveGame();

        GameManager.Instance.SpawnSelect(GameManager.Instance.GetTalk(41004), null, 0,
            () =>
            {
                OpenWishlistPage();
                closeAction?.Invoke();
            },
            () => closeAction?.Invoke());

        return true;
    }

    public void OpenWishlistPage()
    {
        if (SteamManager.Initialized && SteamUtils.IsOverlayEnabled())
        {
            SteamFriends.ActivateGameOverlayToStore(new AppId_t(ConstValues.AppId),
                EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
            return;
        }

        // 오버레이 비활성 유저 / Steam 외 실행 폴백
        Application.OpenURL($"https://store.steampowered.com/app/{ConstValues.AppId}/");
    }
}
