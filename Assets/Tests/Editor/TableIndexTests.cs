// TableManager id 인덱스 테스트
//
// 실제 JSON 을 읽어 인덱스가 원본 List 와 동일한 결과를 주는지 확인한다.
// 자료구조를 Find -> Dictionary 로 바꿨으므로 '같은 id 로 같은 데이터가 나오는지'가 핵심이다.
//
// 실제 테이블을 쓰는 이유: 시트에 id 중복이 있으면 인덱스가 조용히 항목을 버린다.
// 그건 데이터 문제이므로 실제 파일로 검증해야 의미가 있다.

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TableIndexTests
{
    private TableManager table;
    private GameObject host;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("TableManagerForTest");
        table = host.AddComponent<TableManager>();
        table.Init();
    }

    [TearDown]
    public void TearDown()
    {
        if (host)
            Object.DestroyImmediate(host);
    }

    // 인덱스 조회 결과가 기존 Find 결과와 같은지 대조한다
    private static void AssertSameAsFind<T>(List<T> list, System.Func<T, string> idOf,
                                            System.Func<string, T> lookup, string label) where T : class
    {
        Assert.IsNotNull(list, $"{label} 테이블이 로드되지 않았다");
        Assert.Greater(list.Count, 0, $"{label} 테이블이 비어 있다");

        foreach (var item in list)
        {
            var id = idOf(item);
            if (string.IsNullOrEmpty(id))
                continue;

            var expected = list.Find(x => idOf(x) == id);   // 기존 방식
            var actual = lookup(id);                        // 새 방식

            Assert.AreSame(expected, actual, $"{label} id={id} 조회 결과가 다르다");
        }
    }

    [Test]
    public void SpawnedObject_인덱스가_기존_Find와_같은_결과를_준다()
    {
        AssertSameAsFind(table.spawnedObjectTable.SpawnedObject, x => x.id,
                         table.GetSpawnedObject, "SpawnedObject");
    }

    [Test]
    public void Attack_인덱스가_기존_Find와_같은_결과를_준다()
    {
        AssertSameAsFind(table.attackTable.Attack, x => x.id, table.GetAttack, "Attack");
    }

    [Test]
    public void Missile_인덱스가_기존_Find와_같은_결과를_준다()
    {
        AssertSameAsFind(table.missileTable.Missile, x => x.id, table.GetMissile, "Missile");
    }

    [Test]
    public void Monster_Skill_Player_Rooms_인덱스가_기존_Find와_같은_결과를_준다()
    {
        AssertSameAsFind(table.monsterTable.Monster, x => x.id, table.GetMonster, "Monster");
        AssertSameAsFind(table.skillTable.Skill, x => x.id, table.GetSkill, "Skill");
        AssertSameAsFind(table.playerTable.Player, x => x.id, table.GetPlayer, "Player");
        AssertSameAsFind(table.roomsTable.Rooms, x => x.id, table.GetRoom, "Rooms");
    }

    [Test]
    public void 없는_id는_예외_대신_null을_반환한다()
    {
        Assert.IsNull(table.GetSpawnedObject("존재하지_않는_id"));
        Assert.IsNull(table.GetAttack("존재하지_않는_id"));
        Assert.IsNull(table.GetSkill(null));
        Assert.IsNull(table.GetMonster(""));
    }

    [Test]
    public void 중복된_id가_있으면_인덱스에서_누락된다()
    {
        // 인덱스는 id 가 빈 행을 건너뛴다. 시트 끝에 빈 줄이 딸려오는 경우가 있어
        // 원본 개수가 아니라 'id 가 있는 행의 수' 와 비교해야 한다.
        // 이 값이 다르면 중복 id 가 있다는 뜻이고, Init 에서 경고가 찍힌다.

        AssertNoDuplicate(table.spawnedObjectTable.SpawnedObject, x => x.id,
                          nameof(SpawnedObjectData), "SpawnedObject");
        AssertNoDuplicate(table.attackTable.Attack, x => x.id,
                          nameof(AttackData), "Attack");
        AssertNoDuplicate(table.missileTable.Missile, x => x.id,
                          nameof(MissileData), "Missile");
    }

    private void AssertNoDuplicate<T>(List<T> list, System.Func<T, string> idOf,
                                      string typeName, string label)
    {
        int withId = 0;
        var seen = new HashSet<string>();
        var dups = new List<string>();

        foreach (var item in list)
        {
            var id = idOf(item);
            if (string.IsNullOrEmpty(id))
                continue;

            withId++;
            if (!seen.Add(id))
                dups.Add(id);
        }

        Assert.AreEqual(withId, table.IndexedCount(typeName),
            $"{label} 에 중복 id 가 있다: {string.Join(", ", dups)}");
    }

    [Test]
    public void id가_빈_행은_인덱스에서_제외된다()
    {
        // 빈 행이 있어도 조회는 정상이어야 한다
        Assert.IsNull(table.GetSpawnedObject(""));
        Assert.IsNull(table.GetSpawnedObject(null));

        var blankRows = table.spawnedObjectTable.SpawnedObject
                             .FindAll(x => string.IsNullOrEmpty(x.id)).Count;
        if (blankRows > 0)
            Debug.Log($"[Table] SpawnedObject 에 id 가 빈 행이 {blankRows}개 있습니다 (기능에는 영향 없음)");
    }
}
