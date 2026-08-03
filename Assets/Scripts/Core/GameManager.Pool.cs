// GameManager - 오브젝트 풀 · 스프라이트 아틀라스
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;

public partial class GameManager
{
    private void SetPrefabActive(bool active)
    {
        pool.SetAllPrefabsActive(active);
        Debug.Log($"{pool.PrefabCount}개의 프리팹 {(active ? "활성화" : "비활성화")}완료");
    }

    // PrefabCacher(에디터 툴)가 인스펙터 목록을 직접 채운다
    public List<GameObject> GetPrefabList()
    {
        return prefabList;
    }

    private void InitAtlas(SpriteAtlas spriteAtlas)
    {
        // Atlas 안에 들어있는 스프라이트 개수만큼 배열 생성
        cloneSprites = new Sprite[spriteAtlas.spriteCount];
        // GetSprites 호출 시 배열에 모두 채워진다
        spriteAtlas.GetSprites(cloneSprites);
        foreach (var sprite in cloneSprites)
        {
            var keyName = sprite.name.Split(ConstValues.AtlasClone)[0];
            atlasDic.Add(keyName, sprite);
        }
    }

    public Sprite GetAtlasSprite(string id)
    {
        return atlasDic[id];
    }

    public void DisActiveObjectList()
    {
        pool.DeactivateAll();
    }

    // 일반 오브젝트
    public GameObject SpawnToObjectPool(string id, Transform objTransform)
        => pool.Spawn(id, objectPool, objTransform.position);

    public GameObject SpawnToObjectPool(string id, Vector3 objVector)
        => pool.Spawn(id, objectPool, objVector);

    // 일반 UI오브젝트
    public GameObject SpawnToUIObjectPool(string id, Transform objTransform)
        => pool.Spawn(id, uiObjectPool, objTransform.position);

    public GameObject SpawnToUIObjectPool(string id, Vector2 objVector)
        => pool.Spawn(id, uiObjectPool, objVector);

    public GameObject SpawnToUIObjectPoolInstantiate(string id, Transform objTransform)
        => pool.SpawnNew(id, uiObjectPool, objTransform.position);

    public GameObject SpawnToUIObjectPoolInstantiate(string id, Vector2 objVector)
        => pool.SpawnNew(id, uiObjectPool, objVector);

    // UI화면
    public GameObject SpawnToUIPool(string id, Vector2 objVector)
        => pool.Spawn(id, uiPool, objVector);

    public GameObject SpawnToUIPool(eUIType type, Transform objTransform)
        => pool.Spawn(type.ToString(), uiPool, objTransform.position);

    public GameObject SpawnToUIPool(eUIType type, Vector2 objVector)
        => pool.Spawn(type.ToString(), uiPool, objVector);

    // UI팝업화면
    public GameObject SpawnToPopupPool(eUIType type, Transform objTransform)
        => pool.Spawn(type.ToString(), popupPool, objTransform.position);

    public GameObject SpawnToPopupPool(eUIType type, Vector2 objVector)
        => pool.Spawn(type.ToString(), popupPool, objVector, true);

    // 최상위 UI오브젝트
    public GameObject SpawnToHighestPool(string id, Transform objTransform)
        => pool.Spawn(id, highestPool, objTransform.position);

    public GameObject SpawnToHighestPool(string id, Vector2 objVector)
        => pool.Spawn(id, highestPool, objVector);

    public GameObject SpawnToHighestPool(eUIType type, Vector2 objVector)
        => pool.Spawn(type.ToString(), highestPool, objVector);

    public GameObject SpawnToRaw(string id, Vector2 objVector)
        => pool.Spawn(id, null, objVector);

    public BoxCollider2D ObjectCollider(string id)
        => pool.GetPrefabCollider(id);

    public GameObject SpawnToMonster(string id, Transform pool_, Vector3 objVector, bool isActive)
        => pool.SpawnUntracked(id, pool_, objVector, isActive);

    public GameObject SpawnToPoolInstantiate(string id, Transform pool_, Transform objTransform)
        => pool.SpawnNew(id, pool_, objTransform.position);

    public GameObject SpawnToPoolInstantiate(string id, Transform pool_, Vector3 objVector)
        => pool.SpawnNew(id, pool_, objVector);

    private void FirstCashing()
    {
        if (!fadeSystem)
        {
            fadeSystem = SpawnToHighestPool(ConstValues.FadeUI, Vector3.zero).GetComponent<FadeSystem>();
            fadeSystem.transform.localPosition = Vector3.zero;
        }
        fadeSystem.gameObject.SetActive(false);
    }

    public async UniTask Fading(float start, float end, float duration, bool delete, Color color, bool ignoreTime = true)
    {
        fadeSystem.ColorInput(color);
        fadeSystem.gameObject.SetActive(true);
        fadeSystem.SetParameter(start, end, duration, delete);
        if (await fadeSystem.Fade(ignoreTime).SuppressCancellationThrow())
            return;
    }

    public void FadeObjectActiveImmediately(bool active)
    {
        if (FadeSystem == null)
            return;

        fadeSystem.ColorInput(ConstValues.BlackColor);
        fadeSystem.gameObject.SetActive(active);
    }

    public void PoolDisActive()
    {
        inGame = false;

        players.Clear();
        monsterList.Clear();
        pool.ClearTracking();

        foreach (Transform child in objectPool)
            Destroy(child.gameObject);

        foreach (Transform child in uiObjectPool)
            Destroy(child.gameObject);

        foreach (Transform child in uiPool)
            Destroy(child.gameObject);

        foreach (Transform child in popupPool)
            Destroy(child.gameObject);
    }

    public void SpawnHighestObject(string id, Vector2 pos, int zAngle = 0)
    {
        var obj = SpawnToHighestPool(id, pos);
        var objectData = TableManager.Instance.GetSpawnedObject(id);

        var spawnedObject = obj.GetComponent<SpawnedObject>();
        if (!spawnedObject)
            spawnedObject = obj.AddComponent<SpawnedObject>();

        spawnedObject.SetupData(objectData, transform.localScale.x);
        spawnedObject.EnableSetting(true);
        if (zAngle != 0)
        {
            var finalAngle = zAngle;
            if (transform.localScale.x < 0)
                finalAngle = -zAngle;

            var objectAngle = spawnedObject.transform.eulerAngles;
            spawnedObject.transform.eulerAngles = new Vector3(objectAngle.x, objectAngle.y, objectAngle.z + finalAngle);
        }
    }

    public void HideHighestObjects()
    {
        for (int i = 0; i < highestPool.childCount; i++)
            highestPool.GetChild(i).gameObject.SetActive(false);
    }
}
