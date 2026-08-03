using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;


public partial class GameManager
{

    private void SetPrefabActive(bool active)
    {
        foreach (var prefab in prefabList)
            prefab.SetActive(active);

        string text = active ? "활성화" : "비활성화";
        Debug.Log($"{prefabList}개의 프리팹 {text}완료");
    }

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
        foreach (var obj in objectList)
            obj.SetActive(false);
    }

    // 일반 오브젝트
    public GameObject SpawnToObjectPool(string id, Transform objTransform)
    {
        return SpawnToPool(id, objectPool, objTransform);
    }

    public GameObject SpawnToObjectPool(string id, Vector3 objVector)
    {
        return SpawnToPool(id, objectPool, objVector);
    }

    // 일반 UI오브젝트
    public GameObject SpawnToUIObjectPool(string id, Transform objTransform)
    {
        return SpawnToPool(id, uiObjectPool, objTransform);
    }

    public GameObject SpawnToUIObjectPool(string id, Vector2 objVector)
    {
        return SpawnToPool(id, uiObjectPool, objVector);
    }

    public GameObject SpawnToUIObjectPoolInstantiate(string id, Transform objTransform)
    {
        return SpawnToPoolInstantiate(id, uiObjectPool, objTransform);
    }

    public GameObject SpawnToUIObjectPoolInstantiate(string id, Vector2 objVector)
    {
        return SpawnToPoolInstantiate(id, uiObjectPool, objVector);
    }

    // UI화면
    public GameObject SpawnToUIPool(string id, Vector2 objVector)
    {
        return SpawnToPool(id, uiPool, objVector);
    }

    public GameObject SpawnToUIPool(eUIType type, Transform objTransform)
    {
        var go = SpawnToPool(type.ToString(), uiPool, objTransform);
        return go;
    }

    public GameObject SpawnToUIPool(eUIType type, Vector2 objVector)
    {
        var go = SpawnToPool(type.ToString(), uiPool, objVector);
        return go;
    }

    // UI팝업화면
    public GameObject SpawnToPopupPool(eUIType type, Transform objTransform)
    {
        var go = SpawnToPool(type.ToString(), popupPool, objTransform);
        return go;
    }

    public GameObject SpawnToPopupPool(eUIType type, Vector2 objVector)
    {
        var go = SpawnToPool(type.ToString(), popupPool, objVector, true);
        return go;
    }

    // 최상위 UI오브젝트
    public GameObject SpawnToHighestPool(string id, Transform objTransform)
    {
        return SpawnToPool(id, highestPool, objTransform);
    }

    public GameObject SpawnToHighestPool(string id, Vector2 objVector)
    {
        return SpawnToPool(id, highestPool, objVector);
    }

    public GameObject SpawnToHighestPool(eUIType type, Vector2 objVector)
    {
        var go = SpawnToPool(type.ToString(), highestPool, objVector);
        return go;
    }

    public GameObject SpawnToRaw(string id, Vector2 objVector)
    {
        return SpawnToPool(id, null, objVector);
    }

    public BoxCollider2D ObjectCollider(string id)
    {
        var go = prefabList.Find(x => x.name == id).gameObject;
        if (go != null)
        {
            var targetCollider = go.GetComponent<BoxCollider2D>();
            return targetCollider;
        }
        return null;
    }

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
        if(await fadeSystem.Fade(ignoreTime).SuppressCancellationThrow())
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
        objectList.Clear();
        monsterList.Clear();
        
        foreach (Transform child in objectPool)
            Destroy(child.gameObject);

        foreach (Transform child in uiObjectPool)
            Destroy(child.gameObject);

        foreach (Transform child in uiPool)
            Destroy(child.gameObject);
        
        foreach (Transform child in popupPool)
            Destroy(child.gameObject);

    }

    private GameObject SpawnToPool(string id, Transform pool, Transform objTransform)
    {
        var objectName = $"{id}(Clone)";
        var isSearch = objectList.FindAll(x => x.name == objectName);
        
        GameObject go;
        if (isSearch.Count == 0)
        {
            if(prefabList.Find(x => x.name == id) == null)
                Debug.LogWarning($"{id}가 프리팹 리스트에 없다");
            go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
            objectList.Add(go);
        }
        else
        {
            var recycleObj = isSearch.Find(x => !x.activeSelf);
            if (recycleObj == null)
            {
                go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
                objectList.Add(go);
            }
            else
            {
                go = recycleObj;
            }
        }
        
        go.transform.position = objTransform.position;
        go.SetActive(true);
        ResetParticles(go);
        return go;
    }

    private GameObject SpawnToPool(string id, Transform pool, Vector3 objVector, bool isHightest = false)
    { 
        var objectName = $"{id}(Clone)";
        var isSearch = objectList.FindAll(x => x.name == objectName);
        
        GameObject go;
        if (isSearch.Count == 0)
        {
            if(prefabList.Find(x => x.name == id) == null)
                Debug.LogWarning($"{id}가 프리팹 리스트에 없다");
            go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
            objectList.Add(go);
        }
        else
        {
            var recycleObj = isSearch.Find(x => !x.activeSelf);
            if (recycleObj == null)
            {
                go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
                objectList.Add(go);
            }
            else
            {
                go = recycleObj;
            }
        }

        go.transform.position = objVector;
        go.SetActive(true);
        ResetParticles(go);

        if(isHightest)
            go.transform.SetAsLastSibling();

        return go;
    }

    // 풀링 재사용 시 파티클 상태/이전 위치 추적값을 새 위치 기준으로 리셋해 텔레포트 잔상 제거
    private void ResetParticles(GameObject go)
    {
        var particles = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var particle in particles)
        {
            particle.Clear(true);
            particle.Simulate(0f, true, true);
            particle.Play(true);
        }
    }

    public GameObject SpawnToMonster(string id, Transform pool, Vector3 objVector, bool isActive)
    {
        GameObject go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
        
        go.transform.position = objVector;
        go.SetActive(isActive);
        return go;
    }

    public GameObject SpawnToPoolInstantiate(string id, Transform pool, Transform objTransform)
    {
        GameObject go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
        objectList.Add(go);
        
        go.transform.position = objTransform.transform.position;
        go.SetActive(true);
        return go;
    }

    public GameObject SpawnToPoolInstantiate(string id, Transform pool, Vector3 objVector)
    {
        GameObject go = Instantiate(prefabList.Find(x => x.name == id).gameObject, pool);
        objectList.Add(go);
        
        go.transform.position = objVector;
        go.SetActive(true);
        return go;
    }

    public void SpawnHighestObject(string id, Vector2 pos, int zAngle = 0)
    {
        var obj = SpawnToHighestPool(id, pos);
        var objectData = TableManager.Instance.spawnedObjectTable.SpawnedObject.Find(x => x.id == id);

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
