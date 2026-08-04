// 방 하나의 미니맵 공개 상태.
//
// 분리한 이유
//   Room 이 3,800줄까지 커진 원인은 "방에 관련된 모든 것"이 한 클래스에 모여 있었기 때문이다.
//   그중 미니맵은 전용 필드 9개(방문 셀, 원본 타일 캐시, 미니맵 타일맵)를 쓰는데
//   그 필드를 Room 의 다른 코드가 건드리는 곳은 Awake 의 초기화뿐이었다.
//   즉 자리만 Room 안이었을 뿐, 원래부터 독립된 기능이었다.
//
// MonoBehaviour 가 아니다. 방문 셀 계산은 격자 수학이라 씬 없이도 검증할 수 있고,
// 저장 형식은 MinimapCellCodec 으로 빼서 테스트를 붙였다.
//
// 미니맵 마커(세이브 포인트·포탈·상인·획득물)는 Room 에 남겼다.
// 그것들은 미니맵 전용 데이터가 아니라 방이 소유한 오브젝트이고,
// 공개 조건도 "이미 먹었는가" 같은 방 상태에 걸려 있어서 여기로 가져오면 결합이 늘어난다.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomMinimap
{
    private readonly Tilemap frameTilemap;
    private readonly Tilemap inTilemap;
    private readonly Tilemap[] shortcutTilemaps;
    private readonly HiddenArea[] hiddenAreas;

    // 테두리
    private readonly List<Vector3Int> allFrameCells = new List<Vector3Int>();
    private readonly Dictionary<Vector3Int, TileBase> originalFrameTiles = new Dictionary<Vector3Int, TileBase>();
    private readonly List<Vector3Int> visitedFrameCells = new List<Vector3Int>();

    // 내부
    private readonly List<Vector3Int> allInCells = new List<Vector3Int>();
    private readonly Dictionary<Vector3Int, TileBase> originalInTiles = new Dictionary<Vector3Int, TileBase>();
    private readonly List<Vector3Int> visitedInCells = new List<Vector3Int>();

    // 숏컷
    private readonly List<Vector3Int> allShortcutCells = new List<Vector3Int>();
    private readonly List<Dictionary<Vector3Int, TileBase>> originalShortcutTiles = new List<Dictionary<Vector3Int, TileBase>>();
    private readonly List<List<Vector3Int>> visitedShortcutCells = new List<List<Vector3Int>>();

    private RoomInfo roomInfo;

    public RoomMinimap(Tilemap frameTilemap, Tilemap inTilemap, Tilemap[] shortcutTilemaps, HiddenArea[] hiddenAreas)
    {
        this.frameTilemap = frameTilemap;
        this.inTilemap = inTilemap;
        this.shortcutTilemaps = shortcutTilemaps ?? new Tilemap[0];
        this.hiddenAreas = hiddenAreas;
    }

    /// <summary>세이브 데이터가 붙기 전까지는 복원/저장을 할 수 없다.</summary>
    public void Bind(RoomInfo info)
    {
        roomInfo = info;
    }

    // 원본 타일을 캐싱한 뒤 전부 지운다.
    // 미니맵은 "그려둔 것을 지웠다가 방문한 만큼 다시 칠하는" 방식이라 원본을 들고 있어야 한다.
    public void CacheTiles()
    {
        CacheInto(frameTilemap, allFrameCells, originalFrameTiles);
        CacheInto(inTilemap, allInCells, originalInTiles);

        for (int i = 0; i < shortcutTilemaps.Length; i++)
        {
            visitedShortcutCells.Add(new List<Vector3Int>());
            originalShortcutTiles.Add(new Dictionary<Vector3Int, TileBase>());

            var targetMap = shortcutTilemaps[i];
            foreach (var pos in targetMap.cellBounds.allPositionsWithin)
            {
                if (!targetMap.HasTile(pos))
                    continue;

                originalShortcutTiles[i][pos] = targetMap.GetTile(pos);

                // 숏컷 타일맵끼리 좌표가 겹칠 수 있어 전체 목록에는 한 번만 넣는다
                if (!allShortcutCells.Contains(pos))
                    allShortcutCells.Add(pos);
            }
            targetMap.ClearAllTiles();
        }

        if (hiddenAreas != null)
        {
            foreach (var hiddenArea in hiddenAreas)
                hiddenArea.CacheTiles();
        }
    }

    private static void CacheInto(Tilemap tilemap, List<Vector3Int> allCells, Dictionary<Vector3Int, TileBase> originals)
    {
        if (!tilemap)
            return;

        foreach (var pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(pos))
                continue;

            allCells.Add(pos);
            originals[pos] = tilemap.GetTile(pos);
        }
        tilemap.ClearAllTiles();
    }

    // 세이브 데이터에서 이미 방문한 셀만 다시 칠한다.
    // 저장된 것이 없으면 전부 비운 상태로 시작한다.
    public void Restore()
    {
        if (roomInfo == null)
            return;

        if (string.IsNullOrEmpty(roomInfo.visitedFrameCells))
        {
            frameTilemap.ClearAllTiles();
        }
        else
        {
            MinimapCellCodec.Decode(roomInfo.visitedFrameCells, visitedFrameCells);
            PaintCells(frameTilemap, visitedFrameCells, originalFrameTiles);
        }

        if (string.IsNullOrEmpty(roomInfo.visitedInCells))
        {
            inTilemap.ClearAllTiles();
        }
        else
        {
            MinimapCellCodec.Decode(roomInfo.visitedInCells, visitedInCells);
            PaintCells(inTilemap, visitedInCells, originalInTiles);
        }

        if (roomInfo.visitedShortcutCells.Count > 0)
        {
            LoadVisitedShortcutCells();
            for (int i = 0; i < visitedShortcutCells.Count; i++)
            {
                if (i >= shortcutTilemaps.Length)
                    break;

                PaintCells(shortcutTilemaps[i], visitedShortcutCells[i], originalShortcutTiles[i]);
            }
        }

        // 숨겨진 구역: 발견 여부와 공개됐던 셀을 함께 복원한다
        if (hiddenAreas != null)
        {
            for (int i = 0; i < hiddenAreas.Length; i++)
            {
                if (i < roomInfo.hiddenAreaDiscovered.Count)
                    hiddenAreas[i].SetDiscovered(roomInfo.hiddenAreaDiscovered[i]);
                if (i < roomInfo.visitedHiddenCells.Count)
                    hiddenAreas[i].LoadVisitedCells(roomInfo.visitedHiddenCells[i]);
            }
        }
    }

    private static void PaintCells(Tilemap tilemap, List<Vector3Int> cells, Dictionary<Vector3Int, TileBase> originals)
    {
        foreach (var cell in cells)
        {
            if (originals.TryGetValue(cell, out var tile))
                tilemap.SetTile(cell, tile);
        }
    }

    /// <summary>
    /// 카메라 뷰에 걸친 테두리/내부 셀을 공개하고, 뒤 단계에서 쓸 판정 사각형을 돌려준다.
    ///
    /// 사각형을 위로 넓히는 보정은 원래 코드 그대로다. 미니맵 타일이 방보다 위쪽에 그려져 있어
    /// 카메라 사각형을 그대로 쓰면 화면에 보이는 구역이 미니맵에 늦게 칠해진다.
    /// 테두리 보정과 내부 보정이 누적되고, 그 누적된 값을 마커·숏컷·숨겨진 구역이 함께 쓴다.
    /// </summary>
    public Rect RevealFrameAndInCells(Rect cameraRect)
    {
        var viewRect = cameraRect;

        float extraFrame = frameTilemap.cellSize.y * 0.5f;
        viewRect.yMin += extraFrame;
        viewRect.yMax += extraFrame * 3;

        Vector2 frameCellSize = frameTilemap.cellSize;
        if (RevealInto(frameTilemap, allFrameCells, visitedFrameCells, originalFrameTiles, viewRect, frameCellSize))
            SaveVisitedFrameCells();

        float extraIn = inTilemap.cellSize.y * 0.5f;
        viewRect.yMin += extraIn;
        viewRect.yMax += extraIn * 3;

        Vector2 inCellSize = inTilemap.cellSize;
        if (RevealInto(inTilemap, allInCells, visitedInCells, originalInTiles, viewRect, inCellSize))
            SaveVisitedInCells();

        return viewRect;
    }

    private static bool RevealInto(Tilemap tilemap, List<Vector3Int> allCells, List<Vector3Int> visited,
                                   Dictionary<Vector3Int, TileBase> originals, Rect viewRect, Vector2 halfCell)
    {
        bool revealed = false;
        foreach (var cell in allCells)
        {
            if (visited.Contains(cell))
                continue;

            if (!Overlaps(viewRect, tilemap.GetCellCenterWorld(cell), halfCell))
                continue;

            visited.Add(cell);
            tilemap.SetTile(cell, originals[cell]);
            revealed = true;
        }
        return revealed;
    }

    /// <summary>숏컷과 숨겨진 구역을 공개한다. 판정 사각형은 RevealFrameAndInCells 가 돌려준 것을 쓴다.</summary>
    public void RevealShortcutsAndHidden(Rect viewRect)
    {
        // 원래 코드가 숏컷 판정에 테두리 셀 크기를 쓰고 있었다. 숏컷 타일맵의 셀 크기가 아니다.
        Vector2 frameCellSize = frameTilemap.cellSize;

        bool shortcutNew = false;
        foreach (var cell in allShortcutCells)
        {
            for (int i = 0; i < shortcutTilemaps.Length; i++)
            {
                if (visitedShortcutCells[i].Contains(cell))
                    continue;

                // 이 좌표가 원래 i번째 타일맵에 있던 타일인지 확인한다
                if (!originalShortcutTiles[i].TryGetValue(cell, out var originalTile))
                    continue;

                var targetMap = shortcutTilemaps[i];
                if (!Overlaps(viewRect, targetMap.GetCellCenterWorld(cell), frameCellSize))
                    continue;

                visitedShortcutCells[i].Add(cell);
                targetMap.SetTile(cell, originalTile);
                shortcutNew = true;
            }
        }
        if (shortcutNew)
            SaveVisitedShortcutCells();

        if (hiddenAreas == null)
            return;

        bool hiddenNew = false;
        foreach (var hiddenArea in hiddenAreas)
            hiddenNew |= hiddenArea.RevealCellsInView(viewRect, frameCellSize);

        if (hiddenNew)
            SaveHiddenAreaData();
    }

    /// <summary>
    /// 중심과 반지름으로 만든 사각형이 판정 사각형에 조금이라도 걸치는가.
    /// 같은 판정이 미니맵 셀·세이브 포인트·포탈·상인·획득물까지 일곱 군데에 복사돼 있었다.
    /// </summary>
    public static bool Overlaps(Rect viewRect, Vector2 center, Vector2 half)
    {
        return center.x + half.x >= viewRect.xMin && center.x - half.x <= viewRect.xMax &&
               center.y + half.y >= viewRect.yMin && center.y - half.y <= viewRect.yMax;
    }

    /// <summary>종료 시점처럼 공개 상태를 통째로 적어야 할 때 쓴다.</summary>
    public void SaveAll()
    {
        if (roomInfo == null)
            return;

        SaveVisitedFrameCells();
        SaveVisitedInCells();
        SaveVisitedShortcutCells();
        SaveHiddenAreaData();
    }

    /// <summary>숨겨진 구역의 발견 여부와 공개된 셀을 세이브 데이터에 적는다.</summary>
    public void SaveHiddenAreaData()
    {
        if (roomInfo == null || hiddenAreas == null)
            return;

        roomInfo.hiddenAreaDiscovered.Clear();
        roomInfo.visitedHiddenCells.Clear();
        foreach (var hiddenArea in hiddenAreas)
        {
            roomInfo.hiddenAreaDiscovered.Add(hiddenArea.IsDiscovered);
            roomInfo.visitedHiddenCells.Add(hiddenArea.SaveVisitedCells());
        }
    }

    private void SaveVisitedFrameCells()
    {
        roomInfo.visitedFrameCells = MinimapCellCodec.Encode(visitedFrameCells);
    }

    private void SaveVisitedInCells()
    {
        roomInfo.visitedInCells = MinimapCellCodec.Encode(visitedInCells);
    }

    private void SaveVisitedShortcutCells()
    {
        roomInfo.visitedShortcutCells.Clear();
        foreach (var cells in visitedShortcutCells)
            roomInfo.visitedShortcutCells.Add(MinimapCellCodec.Encode(cells));
    }

    private void LoadVisitedShortcutCells()
    {
        for (int i = 0; i < roomInfo.visitedShortcutCells.Count; i++)
        {
            // 세이브가 저장된 뒤에 방 프리팹의 숏컷 개수가 줄어들 수 있다
            if (i >= visitedShortcutCells.Count)
                break;

            MinimapCellCodec.Decode(roomInfo.visitedShortcutCells[i], visitedShortcutCells[i]);
        }
    }
}
