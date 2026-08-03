// 오브젝트 풀 서비스
//
// GameManager 에서 분리했다. MonoBehaviour 가 아니므로 EditMode 테스트가 가능하다.
//
// 재사용 규칙은 기존과 동일하다.
//   "같은 id 의 인스턴스 중 비활성인 것을 앞에서부터 찾아 재사용하고, 없으면 새로 만든다"
//
// 바뀐 것은 '찾는 방법'이다.
//   이전: objectList(전체 인스턴스) 를 FindAll 로 훑어 이름이 일치하는 것을 새 List 로 모으고,
//         그 안에서 다시 비활성인 것을 찾았다. 프리팹도 매번 prefabList.Find 로 훑었다.
//   현재: id 별 Dictionary 로 해당 id 의 인스턴스만 순회한다. 프리팹 조회는 O(1).
//
// 스폰 1회당 사라진 할당
//   - $"{id}(Clone)" 문자열
//   - FindAll 이 만들던 List
//   - 람다 3~4개가 만들던 클로저
// 전투 중 이펙트·투사체가 초당 수십 개 생성되므로 GC 부담이 그대로 프레임 튐이 된다.

using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolService
{
    // 프리팹 이름 -> 프리팹
    private readonly Dictionary<string, GameObject> prefabById = new Dictionary<string, GameObject>();

    // 프리팹 이름 -> 그 id 로 만들어진 인스턴스들
    private readonly Dictionary<string, List<GameObject>> instancesById = new Dictionary<string, List<GameObject>>();

    // 만들어진 전체 인스턴스.
    private readonly List<GameObject> allInstances = new List<GameObject>();

    /// <summary>
    /// 인스펙터에서 채워진 프리팹 목록을 받아 조회용 사전을 만든다.
    /// 목록을 주입받으므로 테스트에서 임의의 프리팹을 넣을 수 있다.
    /// </summary>
    public ObjectPoolService(IEnumerable<GameObject> prefabs)
    {
        if (prefabs == null)
            return;

        foreach (var prefab in prefabs)
        {
            if (!prefab)
                continue;
            // 이름이 겹치면 먼저 등록된 것을 쓴다(기존 Find 동작과 동일)
            if (!prefabById.ContainsKey(prefab.name))
                prefabById.Add(prefab.name, prefab);
        }
    }

    /// <summary>만들어진 전체 인스턴스. 순회 전용이므로 읽기 전용으로 노출한다.</summary>
    public IReadOnlyList<GameObject> AllInstances => allInstances;

    public int PrefabCount => prefabById.Count;

    public GameObject GetPrefab(string id)
        => prefabById.TryGetValue(id, out var prefab) ? prefab : null;

    /// <summary>
    /// 비활성 인스턴스가 있으면 재사용하고, 없으면 새로 만든다.
    /// </summary>
    public GameObject Spawn(string id, Transform parent, Vector3 position, bool asLastSibling = false)
    {
        var go = GetRecyclable(id) ?? Create(id, parent);
        if (!go)
            return null;

        go.transform.position = position;
        go.SetActive(true);
        ResetParticles(go);

        if (asLastSibling)
            go.transform.SetAsLastSibling();

        return go;
    }

    /// <summary>
    /// 재사용하지 않고 항상 새로 만든다. (기존 SpawnToPoolInstantiate)
    /// 같은 프레임에 여러 개가 동시에 필요한 경우에 쓴다.
    /// </summary>
    public GameObject SpawnNew(string id, Transform parent, Vector3 position, bool active = true)
    {
        var go = Create(id, parent);
        if (!go)
            return null;

        go.transform.position = position;
        go.SetActive(active);
        return go;
    }

    /// <summary>
    /// 풀에 등록하지 않고 만든다. (기존 SpawnToMonster — 몬스터는 별도 리스트가 관리한다)
    /// </summary>
    public GameObject SpawnUntracked(string id, Transform parent, Vector3 position, bool active)
    {
        var prefab = GetPrefab(id);
        if (!prefab)
        {
            Debug.LogWarning($"[ObjectPool] {id} 가 프리팹 리스트에 없습니다");
            return null;
        }

        var go = Object.Instantiate(prefab, parent);
        go.transform.position = position;
        go.SetActive(active);
        return go;
    }

    public BoxCollider2D GetPrefabCollider(string id)
    {
        var prefab = GetPrefab(id);
        return prefab ? prefab.GetComponent<BoxCollider2D>() : null;
    }

    public void SetAllPrefabsActive(bool active)
    {
        foreach (var prefab in prefabById.Values)
            prefab.SetActive(active);
    }

    /// <summary>만들어진 인스턴스를 전부 비활성화한다.</summary>
    public void DeactivateAll()
    {
        foreach (var go in allInstances)
        {
            if (go)
                go.SetActive(false);
        }
    }

    /// <summary>추적 목록을 비운다. 실제 파괴는 호출부가 한다.</summary>
    public void ClearTracking()
    {
        allInstances.Clear();
        instancesById.Clear();
    }

    // ------------------------------------------------------------------

    // 같은 id 의 인스턴스 중 비활성인 첫 번째를 반환한다.
    private GameObject GetRecyclable(string id)
    {
        if (!instancesById.TryGetValue(id, out var list))
            return null;

        for (int i = 0; i < list.Count; i++)
        {
            var go = list[i];
            if (go && !go.activeSelf)
                return go;
        }
        return null;
    }

    private GameObject Create(string id, Transform parent)
    {
        var prefab = GetPrefab(id);
        if (!prefab)
        {
            Debug.LogWarning($"[ObjectPool] {id} 가 프리팹 리스트에 없습니다");
            return null;
        }

        var go = Object.Instantiate(prefab, parent);
        // Instantiate 는 이름 뒤에 "(Clone)" 을 붙인다. 이름으로 식별하지 않으므로
        // 원본 id 를 그대로 키로 쓴다.
        if (!instancesById.TryGetValue(id, out var list))
        {
            list = new List<GameObject>();
            instancesById.Add(id, list);
        }
        list.Add(go);
        allInstances.Add(go);
        return go;
    }

    // 풀링 재사용 시 파티클 상태/이전 위치 추적값을 새 위치 기준으로 리셋해 텔레포트 잔상 제거
    private static void ResetParticles(GameObject go)
    {
        var particles = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var particle in particles)
        {
            particle.Clear(true);
            particle.Simulate(0f, true, true);
            particle.Play(true);
        }
    }
}
