using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class GameManager
{
    // 전체화면 상태를 토글하고 저장 (Alt+Enter)
    // SetResolution으로 창을 다시 만들어야 창모드 복귀 시 크기 조절 핸들이 정상 복원된다
    private void ToggleFullScreen()
    {
        fullScreen = fullScreen == 1 ? 0 : 1;
        SettingIntBinding.SaveGameSetting(ConstValues.FullScreen, fullScreen);

        Vector2Int resolution = PopupVideoView.ClampToDisplay(resolutionX, resolutionY);
        Screen.SetResolution(resolution.x, resolution.y, fullScreen == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    public void GoScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void InitCamera(FollowCamera targetCamera)
    {
        mainCamera = targetCamera;
        uiObjectCanvas.worldCamera = targetCamera.GetComponent<Camera>();
    }

    public void CameraShake(float amountX, float amountY, float time)
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("[CameraShake] mainCamera == null. InitCamera가 아직 안 됐거나, RoomManager.mainCameraFollow가 비어있습니다.");
            return;
        }
        mainCamera.Shake(amountX, amountY, time);
    }

    public Monster ActiveAndHideMonster(string id, Vector3 monsterVector, bool isExplosion = true, EMonsterType monsterType = EMonsterType.Normal)
    {
        var monster = SpawnToPoolInstantiate(id, objectPool, monsterVector).GetComponent<Monster>();
        monster.IsExplosion = isExplosion;
        monster.MonsterType = monsterType;
        monster.gameObject.SetActive(false);
        monsterList.Add(monster);
        return monster;
    }

    public Monster ActiveAndHideMonster(string id, Transform monsterTransform, Vector3 monsterVector, bool isActive, bool isExplosion = true, EMonsterType monsterType = EMonsterType.Normal)
    {
        var monster = SpawnToMonster(id, monsterTransform, monsterVector, isActive).GetComponent<Monster>();
        monster.IsExplosion = isExplosion;
        monster.MonsterType = monsterType;
        monster.gameObject.SetActive(false);
        monsterList.Add(monster);
        return monster;
    }

    public void SetMonster(Monster monster, EMonsterType monsterType, bool isExplosion)
    {
        monster.MonsterType = monsterType;
        monster.IsExplosion = isExplosion;
        monsterList.Add(monster);
    }

    public void RemoveMonster(Monster monster)
    {
        monsterList.Remove(monster);
    }

    public void ClearMonsterList()
    {
        foreach (var monster in monsterList)
            monster.gameObject.SetActive(false);
        
        monsterList.Clear();
    }

    public void InputDataTrap(string trapId, Collider2D trapObject)
    {
        string originId = trapId.Split(' ')[0];
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == originId);
        if (objectData != null)
        {
            var spawnedObject = trapObject.GetComponent<SpawnedObject>();
            if (!spawnedObject)
                spawnedObject = trapObject.AddComponent<SpawnedObject>();
            
            spawnedObject.SetupData(objectData, transform.localScale.x);
            spawnedObject.EnableSetting();
        }

        var attackData = TableManager.Instance.attackTable.Attack.Find(x => x.id == originId);
        if (attackData != null)
        {
            var attack = trapObject.GetComponent<Attack>();
            if (!attack)
            {
                attack = trapObject.AddComponent<Attack>();
                attack.SetupData(attackData);
            }

            attack.EnableSetting();
        }
    }

    public void SetCameraTarget(Transform targetTransform)
    {
        mainCamera.SetTarget(targetTransform);
    }

    public void RoomMoveSetting()
    {
        foreach (var list in pool.AllInstances)
        {
            if(list && list.activeSelf && (list.GetComponent<Missile>() || list.GetComponent<Grenade>()))
                list.SetActive(false);
        }
    }

    public void InitProductCancellation()
    {
        productCancellation = new CancellationTokenSource();
    }

    public async UniTask NormalDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), cancellationToken: tokenSource.Token);
    }

    public async UniTask IgnoreTimeScaleDelay(float second, CancellationTokenSource tokenSource)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second), delayType: DelayType.Realtime, cancellationToken: tokenSource.Token);
    }

    public async UniTask YieldDelay(CancellationTokenSource tokenSource)
    {
        await UniTask.Yield(cancellationToken: tokenSource.Token);
    }

    // 대기 딜레이
    public async UniTask WaitUntilDelay(Func<bool> condition, CancellationTokenSource tokenSource)
    {
        await UniTask.WaitUntil(condition, cancellationToken: tokenSource.Token);
    }
}
