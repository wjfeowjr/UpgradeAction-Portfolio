// 풀 조회 방식 A/B 벤치마크
//
// ObjectPoolService 로 바꾸면서 '재사용 대상을 찾는 방법'이 달라졌다.
// 실제로 얼마나 차이가 나는지 재현 가능한 형태로 측정한다.
//
// Instantiate 비용은 양쪽이 동일하므로 제외하고, 조회 로직만 격리해서 잰다.
//   OLD: objectList.FindAll(이름 일치) 후 그 안에서 비활성 검색  + prefabList.Find
//   NEW: Dictionary 조회 후 해당 id 인스턴스만 순회
//
// 실행: Test Runner -> EditMode -> PoolLookupBenchmark
// 결과는 Console 에 표로 출력된다. 수치는 머신마다 다르므로 '비율'을 본다.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PoolLookupBenchmark
{
    // 실제 게임 규모를 흉내낸다
    private const int PrefabKinds = 200;      // 프리팹 종류
    private const int InstancesPerKind = 15;  // 종류당 살아있는 인스턴스
    private const int SpawnCalls = 20000;     // 측정할 스폰 횟수

    private readonly List<GameObject> created = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (var go in created)
            if (go) UnityEngine.Object.DestroyImmediate(go);
        created.Clear();
    }

    private GameObject New(string name)
    {
        var go = new GameObject(name);
        created.Add(go);
        return go;
    }

    [Test]
    public void 조회_방식_비교()
    {
        // ---- 데이터 준비 (양쪽 공통) ----
        var prefabList = new List<GameObject>();
        for (int i = 0; i < PrefabKinds; i++)
            prefabList.Add(New($"Prefab_{i}"));

        // 기존 방식이 훑던 전체 인스턴스 목록
        var objectList = new List<GameObject>();
        // 새 방식이 쓰는 id 별 목록
        var instancesById = new Dictionary<string, List<GameObject>>();
        var prefabById = new Dictionary<string, GameObject>();

        foreach (var p in prefabList)
            prefabById[p.name] = p;

        for (int i = 0; i < PrefabKinds; i++)
        {
            var id = $"Prefab_{i}";
            var list = new List<GameObject>();
            for (int k = 0; k < InstancesPerKind; k++)
            {
                var go = New($"{id}(Clone)");
                go.SetActive(k != InstancesPerKind - 1); // 종류마다 마지막 하나만 비활성
                objectList.Add(go);
                list.Add(go);
            }
            instancesById[id] = list;
        }

        Debug.Log($"[Benchmark] 프리팹 {PrefabKinds}종 / 전체 인스턴스 {objectList.Count}개 / 조회 {SpawnCalls}회");

        // ---- OLD ----
        var oldResult = Measure(() =>
        {
            for (int n = 0; n < SpawnCalls; n++)
            {
                var id = $"Prefab_{n % PrefabKinds}";
                var objectName = $"{id}(Clone)";                        // 문자열 할당
                var isSearch = objectList.FindAll(x => x.name == objectName); // List 할당 + 전체 스캔
                GameObject go;
                if (isSearch.Count == 0)
                {
                    go = prefabList.Find(x => x.name == id);            // 전체 스캔
                }
                else
                {
                    go = isSearch.Find(x => !x.activeSelf);
                    if (go == null)
                        go = prefabList.Find(x => x.name == id);
                }
                if (go == null) throw new Exception("lookup failed");
            }
        });

        // ---- NEW ----
        var newResult = Measure(() =>
        {
            for (int n = 0; n < SpawnCalls; n++)
            {
                var id = $"Prefab_{n % PrefabKinds}";
                GameObject go = null;
                if (instancesById.TryGetValue(id, out var list))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var c = list[i];
                        if (c && !c.activeSelf) { go = c; break; }
                    }
                }
                if (go == null)
                    prefabById.TryGetValue(id, out go);
                if (go == null) throw new Exception("lookup failed");
            }
        });

        // ---- 출력 ----
        double timeGain = oldResult.ms > 0 ? oldResult.ms / Math.Max(newResult.ms, 0.0001) : 0;
        long allocPerCallOld = oldResult.bytes / SpawnCalls;
        long allocPerCallNew = newResult.bytes / SpawnCalls;

        Debug.Log(
            "\n[Pool 조회 방식 비교]\n" +
            $"  {"",-8}{"시간(ms)",12}{"할당(KB)",14}{"호출당 할당(B)",18}\n" +
            $"  {"OLD",-8}{oldResult.ms,12:F1}{oldResult.bytes / 1024.0,14:F1}{allocPerCallOld,18}\n" +
            $"  {"NEW",-8}{newResult.ms,12:F1}{newResult.bytes / 1024.0,14:F1}{allocPerCallNew,18}\n" +
            $"\n  시간 {timeGain:F1}배 빠름 / 할당 {oldResult.bytes - newResult.bytes:N0}B 감소\n");

        // 회귀 방지: 새 방식이 더 적게 할당해야 한다
        Assert.Less(newResult.bytes, oldResult.bytes, "새 방식의 할당이 더 커졌다면 회귀다");
    }

    private struct Result
    {
        public double ms;
        public long bytes;
    }

    private static Result Measure(Action action)
    {
        // 워밍업 (JIT, 캐시)
        action();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long before = GC.GetTotalMemory(true);
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        long after = GC.GetTotalMemory(false);

        return new Result { ms = sw.Elapsed.TotalMilliseconds, bytes = Math.Max(0, after - before) };
    }
}
