using System;
using Cysharp.Threading.Tasks;
using Steamworks;
using UnityEngine;

public class SteamWorksManager : SingletonMono<SteamWorksManager>
{
    // StoreStats는 네트워크 호출이라 매 처치마다 부르면 안 된다. 변경분을 모아 주기적으로 반영한다
    private const float StoreInterval = 10.0f;

    private bool statsDirty;
    private float storeTimer;

    // RequestCurrentStats는 SDK 1.60부터 제거됐다.
    // Steam 클라이언트가 프로세스 시작 전에 스탯을 동기화해 주므로 Init 성공 시점부터 바로 읽고 쓸 수 있다
    private bool StatsReady => SteamManager.Initialized;

    private void Update()
    {
        if (!statsDirty)
            return;

        storeTimer += Time.unscaledDeltaTime;
        if (storeTimer >= StoreInterval)
            StoreStats();
    }

    // 누적 스탯을 amount만큼 증가시킨다 (서버 반영은 StoreStats에서 묶어서)
    public void AddStat(string apiName, int amount = 1)
    {
        if (!StatsReady)
            return;

        if (!SteamUserStats.GetStat(apiName, out int current))
        {
            Debug.LogWarning($"[Steam] 스탯을 찾을 수 없음: {apiName} (파트너 사이트 정의/Publish 확인)");
            return;
        }

        SteamUserStats.SetStat(apiName, current + amount);
        statsDirty = true;
    }

    // 값을 그대로 덮어쓴다. 여러 번 호출해도 결과가 같아야 하는 스탯(DEMO_CLEARED 등)에 사용
    public void SetStat(string apiName, int value)
    {
        if (!StatsReady)
            return;

        if (SteamUserStats.GetStat(apiName, out int current) && current == value)
            return;

        SteamUserStats.SetStat(apiName, value);
        statsDirty = true;
    }

    // 모아둔 변경분을 서버에 반영
    public void StoreStats()
    {
        storeTimer = 0f;

        if (!StatsReady || !statsDirty)
            return;

        statsDirty = false;
        SteamUserStats.StoreStats();
    }

    private void OnApplicationQuit()
    {
        StoreStats();
    }

    public void SteamCheck()
    {
        Debug.Log($"[Steam] Initialized = {SteamManager.Initialized}");

        if (!SteamManager.Initialized)
            return;

        Debug.Log($"[Steam] User = {SteamFriends.GetPersonaName()}, AppID = {SteamUtils.GetAppID()}, LoggedOn = {SteamUser.BLoggedOn()}");

        StatCheck();
    }

    private static readonly string[] StatApiNames =
    {
        ConstValues.StatKilledSun,
        ConstValues.StatKilledMoon,
        ConstValues.StatKilledTree,
        ConstValues.StatKilledBigCharge,
        ConstValues.StatKilledKnife,
        ConstValues.StatKilledGolem,
        ConstValues.StatKilledBomb,
        ConstValues.StatDemoCleared,
    };

    private CallResult<GlobalStatsReceived_t> globalStatsCallResult;

    // 전체 유저 집계값을 조회한다. 파트너 사이트에서 "집계(Aggregated)"를 켠 스탯만 값이 나온다
    public void GlobalStatCheck()
    {
        if (!StatsReady)
            return;

        if (globalStatsCallResult == null)
            globalStatsCallResult = CallResult<GlobalStatsReceived_t>.Create(OnGlobalStatsReceived);

        // 0 = 히스토리 없이 누적 합계만
        globalStatsCallResult.Set(SteamUserStats.RequestGlobalStats(0));
    }

    private void OnGlobalStatsReceived(GlobalStatsReceived_t callback, bool ioFailure)
    {
        if (ioFailure || callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogWarning($"[Steam] 전역 집계 조회 실패: ioFailure={ioFailure}, result={callback.m_eResult}");
            return;
        }

        foreach (var apiName in StatApiNames)
        {
            if (SteamUserStats.GetGlobalStat(apiName, out long total))
                Debug.Log($"[Steam][전역] {apiName} = {total}");
            else
                Debug.LogWarning($"[Steam][전역] {apiName} 조회 불가 — 집계(Aggregated) 미설정으로 보임");
        }
    }

    // 등록한 스탯 8종을 int/float 양쪽으로 조회해 어디서 어긋났는지 확인한다
    public void StatCheck()
    {
        foreach (var apiName in StatApiNames)
        {
            if (SteamUserStats.GetStat(apiName, out int intValue))
            {
                Debug.Log($"[Steam] {apiName} = {intValue} (INT 정상)");
                continue;
            }

            // INT로 못 읽었는데 FLOAT로 읽히면 파트너 사이트에서 타입을 잘못 만든 것
            if (SteamUserStats.GetStat(apiName, out float floatValue))
            {
                Debug.LogError($"[Steam] {apiName} 은 FLOAT로 등록돼 있음 (값 {floatValue}). 파트너 사이트에서 Integer로 바꿔야 함");
                continue;
            }

            Debug.LogError($"[Steam] {apiName} 조회 실패 — 미등록이거나 게시(Publish) 누락, 또는 API Name 불일치");
        }
    }
    
    // 1차 유도: 보스 연출(Product6) 종료 직후. 최초 1회만 띄우고, 팝업이 닫힐 때까지 대기한다.
    public async UniTask ShowFirstWishlistPopupAsync()
    {
        if (GameManager.Instance.IsFirstWishlistShown)
            return;

        GameManager.Instance.IsFirstWishlistShown = true;

        var completion = new UniTaskCompletionSource();
        SpawnWishlistPopup(() => completion.TrySetResult());
        await completion.Task;
    }

    // 2차 유도: 데모 마지막 구역의 세이브 포인트.
    // 1차에서 "예"를 눌렀으면 띄우지 않고, 거절했을 때만 한 번 더 묻는다.
    // 팝업을 띄웠으면 true를 반환하며, closeAction은 예/아니오 어느 쪽이든 팝업이 닫힌 뒤 호출된다.
    public bool TryShowWishlistPopup(Action closeAction)
    {
        if (GameManager.Instance.IsWishlistAccepted || GameManager.Instance.IsSecondWishlistShown)
            return false;

        GameManager.Instance.IsSecondWishlistShown = true;

        SpawnWishlistPopup(closeAction);
        return true;
    }

    // 공통 팝업 생성. "예"를 누르면 수락으로 기록하고 스토어를 연다.
    private void SpawnWishlistPopup(Action closeAction)
    {
        // 띄운 시점의 표시 여부를 즉시 기록해 중복 노출을 막는다
        GameManager.Instance.SaveGame();

        GameManager.Instance.SpawnSelect(GameManager.Instance.GetTalk(41004), null, 0,
            () =>
            {
                GameManager.Instance.IsWishlistAccepted = true;
                GameManager.Instance.SaveGame();
                OpenWishlistPage();
                closeAction?.Invoke();
            },
            () => closeAction?.Invoke());
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
