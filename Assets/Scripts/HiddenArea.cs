using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

// 미니맵에서 숨겨지는 구역.
// 두 가지 원칙을 모두 만족해야 미니맵에 공개된다.
// 1. 플레이어가 구역에 실제로 들어와 발견(isDiscovered)해야 한다.
// 2. 발견 이후에는 기존 미니맵과 동일하게, 카메라 시야에 들어온 타일부터 점진적으로 공개된다.
public class HiddenArea : MonoBehaviour
{
    [Header("구역 진입 판정")]
    [SerializeField] private BoxCollider2D areaCollider;
    [SerializeField] private bool ignoreHeight;   // 세로 범위 무시: 어떤 높이로 지나가든 x축만 걸치면 발견

    [Header("이 구역 전용 미니맵 타일맵(테두리/내부)")]
    [SerializeField] private Tilemap[] hiddenTileMaps;

    private bool isDiscovered;
    private List<Dictionary<Vector3Int, TileBase>> originalTilesList = new List<Dictionary<Vector3Int, TileBase>>();
    private List<List<Vector3Int>> visitedCellsList = new List<List<Vector3Int>>();

    public bool IsDiscovered => isDiscovered;

    // Room.Awake에서 호출: 원본 타일을 캐싱한 뒤 전부 숨긴다
    public void CacheTiles()
    {
        if (areaCollider)
            areaCollider.isTrigger = true;

        foreach (var tilemap in hiddenTileMaps)
        {
            var originalTiles = new Dictionary<Vector3Int, TileBase>();
            var bounds = tilemap.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (tilemap.HasTile(pos))
                    originalTiles[pos] = tilemap.GetTile(pos);
            }
            tilemap.ClearAllTiles();

            originalTilesList.Add(originalTiles);
            visitedCellsList.Add(new List<Vector3Int>());
        }
    }

    // 플레이어가 구역을 처음 지나가는 순간 발견 처리하고 true를 반환.
    // 콜라이더 안에 있을 때뿐 아니라, 빠르게 지나쳐 넘어가기만 해도 판정된다
    public bool CheckDiscover(Vector2 prevPos, Vector2 curPos)
    {
        if (isDiscovered || !areaCollider)
            return false;

        if (!ColliderCrossUtil.CrossedOrInside(areaCollider, prevPos, curPos, ignoreHeight))
            return false;

        return Discover();
    }

    // 발견 처리. 투명벽 등 외부 트리거에서도 호출한다. 최초 1회만 true를 반환
    // playSound가 false면 사운드는 호출한 쪽(투명벽 등)이 재생한다
    public bool Discover(bool playSound = true)
    {
        if (isDiscovered)
            return false;

        isDiscovered = true;

        // 최초 발견 1회에 한해 사운드 재생. 사운드 리소스가 준비되면 SoundManager 호출로 교체
        if (playSound)
            SoundManager.Instance.PlaySound(ConstValues.RewardPage);

        return true;
    }

    // 발견된 구역만, 카메라 시야에 들어온 타일부터 점진 공개. 새로 공개된 타일이 있으면 true
    public bool RevealCellsInView(Rect viewRect, Vector2 halfCell)
    {
        if (!isDiscovered)
            return false;

        bool revealedNew = false;
        for (int i = 0; i < hiddenTileMaps.Length; i++)
        {
            var targetMap = hiddenTileMaps[i];
            foreach (var pair in originalTilesList[i])
            {
                if (visitedCellsList[i].Contains(pair.Key))
                    continue;

                Vector3 center = targetMap.GetCellCenterWorld(pair.Key);
                Vector2 min = new Vector2(center.x - halfCell.x, center.y - halfCell.y);
                Vector2 max = new Vector2(center.x + halfCell.x, center.y + halfCell.y);

                // 타일이 카메라 뷰와 조금이라도 겹치면 활성화
                if (max.x >= viewRect.xMin && min.x <= viewRect.xMax &&
                    max.y >= viewRect.yMin && min.y <= viewRect.yMax)
                {
                    visitedCellsList[i].Add(pair.Key);
                    targetMap.SetTile(pair.Key, pair.Value);
                    revealedNew = true;
                }
            }
        }
        return revealedNew;
    }

    // 세이브 로드 시 발견 여부 복원
    public void SetDiscovered(bool discovered)
    {
        isDiscovered = discovered;
    }

    // 저장: 타일맵별 셀 목록을 '|'로 구분한 한 줄 문자열로 직렬화
    public string SaveVisitedCells()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < visitedCellsList.Count; i++)
        {
            if (i > 0)
                sb.Append('|');
            foreach (var c in visitedCellsList[i])
                sb.Append(c.x).Append('_').Append(c.y).Append('_').Append(c.z).Append(';');
        }
        return sb.ToString();
    }

    // 로드: 저장 문자열을 복원하고 공개됐던 타일을 즉시 설치
    public void LoadVisitedCells(string data)
    {
        if (string.IsNullOrEmpty(data))
            return;

        var tilemapEntries = data.Split('|');
        for (int i = 0; i < tilemapEntries.Length; i++)
        {
            // 인덱스 안전망
            if (i >= visitedCellsList.Count)
                break;

            var entries = tilemapEntries[i].Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var e in entries)
            {
                var p = e.Split('_');
                if (p.Length == 3 && int.TryParse(p[0], out int x) && int.TryParse(p[1], out int y) && int.TryParse(p[2], out int z))
                {
                    var cell = new Vector3Int(x, y, z);
                    visitedCellsList[i].Add(cell);
                    if (originalTilesList[i].TryGetValue(cell, out var tile))
                        hiddenTileMaps[i].SetTile(cell, tile);
                }
            }
        }
    }
}
