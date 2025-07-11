using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public enum EntranceDir
{
    Left,
    Right,
    Up,
    Down
}

public class Room : MonoBehaviour
{
    [SerializeField] protected RoomInfo roomInfo;

    [SerializeField] private Transform minCameraLimitX;
    [SerializeField] private Transform maxCameraLimitX;
    [SerializeField] private Transform minCameraLimitY;
    [SerializeField] private Transform maxCameraLimitY;
    
    [SerializeField] private Transform leftPlayerPos;
    [SerializeField] private Transform rightPlayerPos;
    [SerializeField] private Transform upPlayerPos;
    [SerializeField] private Transform downPlayerPos;

    [SerializeField] private Room leftRoom;
    [SerializeField] private Room rightRoom;
    [SerializeField] private Room upRoom;
    [SerializeField] private Room downRoom;

    [SerializeField] private RoomEntrance leftEntrance;
    [SerializeField] private RoomEntrance rightEntrance;
    [SerializeField] private RoomEntrance upEntrance;
    [SerializeField] private RoomEntrance downEntrance;
    
    [SerializeField] protected Transform[] customMovePos;
    [SerializeField] protected Transform[] monsterPos;
    [SerializeField] protected Transform[] trapPos;
    [SerializeField] protected Transform[] bossPos;
    [SerializeField] protected Transform[] strongSpeechPos;

    private CancellationTokenSource fadeCancellation;
    private CancellationTokenSource dialogCancellation;
    private CancellationTokenSource waitCancellation;
    private CancellationTokenSource dieCancellation;

    [SerializeField] protected SpriteRenderer[] bgSpriteRenderers;

    // 프로퍼티
    public RoomInfo RoomInfo => roomInfo;

    public void EntranceSetting()
    {
        if(leftEntrance)
            leftEntrance.SetAction(() => leftRoom.SetPlayerPos(EntranceDir.Right, gameObject));
        if(rightEntrance)
            rightEntrance.SetAction(() => rightRoom.SetPlayerPos(EntranceDir.Left, gameObject));
        if(upEntrance)
            upEntrance.SetAction(() => upRoom.SetPlayerPos(EntranceDir.Down, gameObject));
        if(downEntrance)
            downEntrance.SetAction(() => downRoom.SetPlayerPos(EntranceDir.Up, gameObject));
    }

    public async void FirstStart()
    {
        GameManager.Instance.ControlStart = false;
        GameManager.Instance.CurPlayer.transform.position = leftPlayerPos.position;
        SetCameraLimit();
        await RoomManager.Instance.EntranceFadeIn();
        GameManager.Instance.ControlStart = true;
    }

    private async void SetPlayerPos(EntranceDir dir, GameObject pastRoom)
    {
        GameManager.Instance.ControlStart = false;
        await RoomManager.Instance.EntranceFadeOut();
        GameManager.Instance.CurPlayer.ForceIdle();
        switch (dir)
        {
            case EntranceDir.Left:
                SetLeftPlayerPos();
                break;
            case EntranceDir.Right:
                SetRightPlayerPos();
                break;
            case EntranceDir.Up:
                SetUpPlayerPos();
                break;
            case EntranceDir.Down:
                SetDownPlayerPos();
                break;
        }
        SetCameraLimit();
        pastRoom.SetActive(false);
        gameObject.SetActive(true);
        fadeCancellation = new CancellationTokenSource();
        if (await NormalDelay(0.5f, fadeCancellation).SuppressCancellationThrow())
            return;
        
        GameManager.Instance.ControlStart = true;
        await RoomManager.Instance.EntranceFadeIn();
    }
    
    private void SetLeftPlayerPos()
    {
        GameManager.Instance.CurPlayer.transform.position = leftPlayerPos.position;
    }
    
    private void SetRightPlayerPos()
    {
        GameManager.Instance.CurPlayer.transform.position = rightPlayerPos.position;
    }
    
    private void SetUpPlayerPos()
    {
        GameManager.Instance.CurPlayer.transform.position = upPlayerPos.position;
    }
    
    private void SetDownPlayerPos()
    {
        GameManager.Instance.CurPlayer.transform.position = downPlayerPos.position;
    }

    private void SetCameraLimit()
    {
        GameManager.Instance.MainCamera.MaxXAndY = new Vector2(maxCameraLimitX.position.x, maxCameraLimitY.position.y);
        GameManager.Instance.MainCamera.MinXAndY = new Vector2(minCameraLimitX.position.x, minCameraLimitY.position.y);
    }

    public void CancelTask()
    {
        dialogCancellation?.Cancel();
        waitCancellation?.Cancel();
    }
    
    protected async UniTask WaitUntil(Func<bool> condition, CancellationTokenSource tokenSource)
    {
        await UniTask.WaitUntil(condition, cancellationToken: tokenSource.Token);
    }
    
    // 맵에 있는 모든 몹을 잡았을 경우 발생하는 액션
    protected async void MonsterClearAction(Action action)
    {
        if (await WaitUntil(() => GameManager.Instance.MonsterList.Count == 0, waitCancellation).SuppressCancellationThrow())
            return;
        action?.Invoke();
    }
    protected async void MonsterClearAction(Func<UniTask> asyncAction)
    {
        if (await WaitUntil(() => GameManager.Instance.MonsterList.Count == 0, waitCancellation).SuppressCancellationThrow())
            return;
        asyncAction?.Invoke();
    }

    protected void SpawnSpeechFrame(SpeechFrame speechFrame, Vector2 speechPos, string dialog)
    {
        speechFrame.SetPos(speechPos);
        speechFrame.Speech(dialog);
    }
    protected async UniTask NextDialog(SpeechFrame speechFrame)
    {
        speechFrame.NextObjectActive();
        // 스페이스바를 누르면 넘어간다
        if (await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.Space), cancellationToken: dialogCancellation.Token).SuppressCancellationThrow())
        {
            speechFrame.SpeechEnd();
            return;
        }
        speechFrame.SpeechEnd();
    }

    // 룸 저장
    protected void SaveRoom()
    {
        // json화
        string json = JsonUtility.ToJson(roomInfo, true);
        RoomBinding.SaveRoom(name, json);
    }
    // 룸 정보 불러오기
    protected void LoadRoom()
    {
        // json화
        string json = JsonUtility.ToJson(roomInfo, true);
        var loadJson = RoomBinding.LoadRoom(name, json);
        // json 불러오기
        var loadedEpisode = JsonUtility.FromJson<RoomInfo>(loadJson);
        roomInfo = loadedEpisode;
    }
    
    protected void SpawnEpisode(string episodeName)
    {
        var uiBase = GameManager.Instance.SpawnToUIPool(eUIType.UI_Episode, Vector3.zero).GetComponent<UIBase>();
        // 바인딩
        if (uiBase is UI_Episode episodeView)
        {
            var episodeInterface = episodeView.EpisodeView.ConvertTo<IUIEpisodeView>();
            var episodeModel = new UIEpisodeModel()
            {
                episodeName = episodeName,
            };
            var episodePresenter = new UIEpisodePresenter(episodeInterface, episodeModel);
            episodeView.SetEpisodePresenter(episodePresenter);
            episodePresenter.SetEpisode();
        }
    }
    protected void ProductEpisode()
    {
        if (roomInfo.episodeTitle != 0)
            return;
        
        var uiEpisodeObj = GameManager.Instance.GetUI(eUIType.UI_Episode);
        if (uiEpisodeObj == null)
            return;

        var uiInterface = uiEpisodeObj.GetComponent<UI_Episode>();
        uiInterface.EpisodePresenter.HandelEpisodeEnd(EpisodeEnd);
        uiInterface.EpisodePresenter.EpisodeProduct(() => { SoundManager.Instance.PlaySound(ConstValues.Upgrade); });
        GameManager.Instance.GetUI(eUIType.UI_Interface).SetActive(false);
    }
    private void EpisodeEnd()
    {
        roomInfo.episodeTitle = 1;
        SaveRoom();
    }

    protected async void GameOverCycle()
    {
        await UniTask.WaitUntil(() => GameManager.Instance.CurPlayer.IsDie);
        GameManager.Instance.ControlStart = false;
        dieCancellation = new CancellationTokenSource();
        if (await NormalDelay(1.0f, dieCancellation).SuppressCancellationThrow())
            return;

        GameManager.Instance.SpawnToPopupPool(eUIType.Popup_GameOver, Vector2.zero);
        Time.timeScale = 0;
    }

    protected void BgSpriteChange(string bgName)
    {
        foreach (var bgSpriteRenderer in bgSpriteRenderers)
        {
            bgSpriteRenderer.sprite = GameManager.Instance.GetAtlasSprite(bgName);
        }
    }
    
    protected async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }
    
    protected async UniTask YieldDelay(CancellationTokenSource tokenSource)
    {
        await UniTask.Yield(cancellationToken: tokenSource.Token);
    }

    protected void StopBGM()
    {
        BgmManager.Instance.Stop();
    }
    protected void PlayBGM(string bgmName)
    {
        BgmManager.Instance.PlayBgm(bgmName);
    }
    protected void PlaySound(string bgmName)
    {
        SoundManager.Instance.PlaySound(bgmName);
    }
    protected void CameraShake(float amount, float time)
    {
        GameManager.Instance.CameraShake(amount, time);
    }
    protected void SetTimeScale(float value)
    {
        Time.timeScale = value;
    }
}
