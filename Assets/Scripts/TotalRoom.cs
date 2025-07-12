using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TotalRoom : MonoBehaviour
{
     [Header("디자인 타일이 미리 그려진 미니맵 Tilemap")]
    public Tilemap minimapTilemap;

    [Header("카메라 & 저장키")]
    public Camera gameCamera;
    public string saveKey = "MiniMap_VisitedCells";

    // 내부 저장용
    private HashSet<Vector3Int> allRoomCells = new HashSet<Vector3Int>();
    private Dictionary<Vector3Int, TileBase> originalTiles = new Dictionary<Vector3Int, TileBase>();
    private HashSet<Vector3Int> visitedCells = new HashSet<Vector3Int>();

    void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        // 1. 모든 그려진 타일 위치 저장 및 비활성화
        var bounds = minimapTilemap.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (minimapTilemap.HasTile(pos))
            {
                allRoomCells.Add(pos);
                originalTiles[pos] = minimapTilemap.GetTile(pos);
            }
        }
        minimapTilemap.ClearAllTiles();
    }

    void Start()
    {
        // 2. 저장된 데이터 유무에 따라 초기 복원
        if (!PlayerPrefs.HasKey(saveKey))
        {
            // 저장 데이터 없음: 모든 타일 비활성화
            minimapTilemap.ClearAllTiles();
        }
        else
        {
            // 저장 데이터 있음: 불러와서 해당 셀만 활성화
            LoadVisitedCells();
            foreach (var cell in visitedCells)
            {
                if (originalTiles.TryGetValue(cell, out var tile))
                    minimapTilemap.SetTile(cell, tile);
            }
        }
    }

    void Update()
    {
        RevealCellsInView();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SaveVisitedCells();
        }
    }

    // 3. 카메라 뷰 영역에 조금이라도 겹치면 활성화
    private void RevealCellsInView()
    {
        Vector3 camPos = gameCamera.transform.position;
        float halfH = gameCamera.orthographicSize;
        float halfW = halfH * gameCamera.aspect;
        Rect viewRect = new Rect(
            camPos.x - halfW, camPos.y - halfH,
            halfW * 2, halfH * 2
        );

        Vector2 halfCell = minimapTilemap.cellSize; // * 0.5f
        bool anyNew = false;

        foreach (var cell in allRoomCells)
        {
            if (visitedCells.Contains(cell))
                continue;

            Vector3 center = minimapTilemap.GetCellCenterWorld(cell);
            Vector2 min = new Vector2(center.x - halfCell.x, center.y - halfCell.y);
            Vector2 max = new Vector2(center.x + halfCell.x, center.y + halfCell.y);

            // 타일이 카메라 뷰와 조금이라도 겹치면 활성화
            if (max.x >= viewRect.xMin && min.x <= viewRect.xMax &&
                max.y >= viewRect.yMin && min.y <= viewRect.yMax)
            {
                visitedCells.Add(cell);
                minimapTilemap.SetTile(cell, originalTiles[cell]);
                anyNew = true;
            }
        }

        if (anyNew)
            SaveVisitedCells();
    }

    // 5. PlayerPrefs에 방문 셀 저장
    private void SaveVisitedCells()
    {
        var sb = new StringBuilder();
        foreach (var c in visitedCells)
            sb.Append(c.x).Append('_').Append(c.y).Append('_').Append(c.z).Append(';');

        PlayerPrefs.SetString(saveKey, sb.ToString());
        PlayerPrefs.Save();
        Debug.Log("미니맵 저장!");
    }

    // 2. 저장된 방문 셀 로드
    private void LoadVisitedCells()
    {
        string data = PlayerPrefs.GetString(saveKey, "");
        if (string.IsNullOrEmpty(data))
            return;

        var entries = data.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var e in entries)
        {
            var p = e.Split('_');
            if (p.Length == 3
             && int.TryParse(p[0], out int x)
             && int.TryParse(p[1], out int y)
             && int.TryParse(p[2], out int z))
            {
                visitedCells.Add(new Vector3Int(x, y, z));
            }
        }
    }

    private void OnApplicationQuit()
    {
        SaveVisitedCells();
    }
}
