// ObjectPoolService 단위 테스트
//
// 풀링은 "재사용 시 이전 상태를 물려받는다"는 위험이 있어 눈으로 검증하기 어렵다.
// (실제로 부활 후 미사일이 이전 콜백을 물고 다니던 버그가 있었다)
// 서비스로 분리하면서 재사용 규칙을 테스트로 고정한다.

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ObjectPoolServiceTests
{
    private readonly List<GameObject> created = new List<GameObject>();

    private GameObject MakePrefab(string name)
    {
        var go = new GameObject(name);
        created.Add(go);
        return go;
    }

    [TearDown]
    public void TearDown()
    {
        // 테스트가 만든 오브젝트를 정리한다 (씬에 남으면 다음 테스트에 영향을 준다)
        foreach (var go in created)
        {
            if (go)
                Object.DestroyImmediate(go);
        }
        created.Clear();
    }

    private ObjectPoolService Make(params string[] prefabNames)
    {
        var prefabs = new List<GameObject>();
        foreach (var n in prefabNames)
            prefabs.Add(MakePrefab(n));
        return new ObjectPoolService(prefabs);
    }

    private void Track(GameObject go)
    {
        if (go)
            created.Add(go);
    }

    [Test]
    public void 프리팹을_이름으로_찾는다()
    {
        var sut = Make("Bat", "Missile");

        Assert.AreEqual(2, sut.PrefabCount);
        Assert.IsNotNull(sut.GetPrefab("Bat"));
        Assert.IsNull(sut.GetPrefab("없는프리팹"));
    }

    [Test]
    public void 없는_id로_스폰하면_null을_반환하고_터지지_않는다()
    {
        var sut = Make("Bat");

        var go = sut.Spawn("없는프리팹", null, Vector3.zero);

        Assert.IsNull(go);
    }

    [Test]
    public void 처음_스폰하면_새_인스턴스를_만든다()
    {
        var sut = Make("Bat");

        var go = sut.Spawn("Bat", null, new Vector3(1, 2, 0));
        Track(go);

        Assert.IsNotNull(go);
        Assert.IsTrue(go.activeSelf);
        Assert.AreEqual(new Vector3(1, 2, 0), go.transform.position);
        Assert.AreEqual(1, sut.AllInstances.Count);
    }

    [Test]
    public void 활성_상태인_인스턴스는_재사용하지_않고_새로_만든다()
    {
        var sut = Make("Bat");

        var first = sut.Spawn("Bat", null, Vector3.zero);
        Track(first);
        var second = sut.Spawn("Bat", null, Vector3.one);
        Track(second);

        Assert.AreNotSame(first, second);
        Assert.AreEqual(2, sut.AllInstances.Count);
    }

    [Test]
    public void 비활성_인스턴스가_있으면_재사용한다()
    {
        var sut = Make("Bat");

        var first = sut.Spawn("Bat", null, Vector3.zero);
        Track(first);
        first.SetActive(false);

        var second = sut.Spawn("Bat", null, new Vector3(5, 5, 0));

        Assert.AreSame(first, second, "비활성 인스턴스를 재사용해야 한다");
        Assert.AreEqual(1, sut.AllInstances.Count, "재사용했으므로 인스턴스가 늘면 안 된다");
        Assert.IsTrue(second.activeSelf, "재사용 시 다시 활성화되어야 한다");
        Assert.AreEqual(new Vector3(5, 5, 0), second.transform.position, "새 위치로 옮겨져야 한다");
    }

    [Test]
    public void 비활성이_여러_개면_먼저_만든_것부터_재사용한다()
    {
        var sut = Make("Bat");

        var a = sut.Spawn("Bat", null, Vector3.zero);
        Track(a);
        var b = sut.Spawn("Bat", null, Vector3.zero);
        Track(b);
        a.SetActive(false);
        b.SetActive(false);

        var reused = sut.Spawn("Bat", null, Vector3.zero);

        Assert.AreSame(a, reused, "생성 순서대로 재사용해야 한다(기존 FindAll 동작과 동일)");
    }

    [Test]
    public void 다른_id의_비활성_인스턴스는_재사용하지_않는다()
    {
        var sut = Make("Bat", "Missile");

        var bat = sut.Spawn("Bat", null, Vector3.zero);
        Track(bat);
        bat.SetActive(false);

        var missile = sut.Spawn("Missile", null, Vector3.zero);
        Track(missile);

        Assert.AreNotSame(bat, missile, "id 가 다르면 재사용하면 안 된다");
        Assert.AreEqual(2, sut.AllInstances.Count);
    }

    [Test]
    public void SpawnNew_는_비활성_인스턴스가_있어도_항상_새로_만든다()
    {
        var sut = Make("Bat");

        var first = sut.Spawn("Bat", null, Vector3.zero);
        Track(first);
        first.SetActive(false);

        var second = sut.SpawnNew("Bat", null, Vector3.zero);
        Track(second);

        Assert.AreNotSame(first, second);
        Assert.AreEqual(2, sut.AllInstances.Count);
    }

    [Test]
    public void SpawnUntracked_는_추적_목록에_넣지_않는다()
    {
        var sut = Make("Bat");

        var go = sut.SpawnUntracked("Bat", null, Vector3.zero, true);
        Track(go);

        Assert.IsNotNull(go);
        Assert.AreEqual(0, sut.AllInstances.Count, "몬스터는 별도 리스트가 관리하므로 풀에 넣지 않는다");
    }

    [Test]
    public void DeactivateAll_은_만든_인스턴스를_전부_끈다()
    {
        var sut = Make("Bat", "Missile");

        var a = sut.Spawn("Bat", null, Vector3.zero);
        Track(a);
        var b = sut.Spawn("Missile", null, Vector3.zero);
        Track(b);

        sut.DeactivateAll();

        Assert.IsFalse(a.activeSelf);
        Assert.IsFalse(b.activeSelf);
    }

    [Test]
    public void ClearTracking_후에는_이전_인스턴스를_재사용하지_않는다()
    {
        var sut = Make("Bat");

        var first = sut.Spawn("Bat", null, Vector3.zero);
        Track(first);
        first.SetActive(false);

        sut.ClearTracking();
        Assert.AreEqual(0, sut.AllInstances.Count);

        var second = sut.Spawn("Bat", null, Vector3.zero);
        Track(second);

        Assert.AreNotSame(first, second, "추적을 비웠으면 새로 만들어야 한다");
    }

    [Test]
    public void 이름이_겹치는_프리팹은_먼저_등록된_것을_쓴다()
    {
        var first = MakePrefab("Bat");
        var second = MakePrefab("Bat");
        var sut = new ObjectPoolService(new List<GameObject> { first, second });

        Assert.AreEqual(1, sut.PrefabCount);
        Assert.AreSame(first, sut.GetPrefab("Bat"));
    }

    [Test]
    public void 프리팹_목록이_null이어도_생성된다()
    {
        var sut = new ObjectPoolService(null);

        Assert.AreEqual(0, sut.PrefabCount);
        Assert.IsNull(sut.Spawn("Bat", null, Vector3.zero));
    }
}
