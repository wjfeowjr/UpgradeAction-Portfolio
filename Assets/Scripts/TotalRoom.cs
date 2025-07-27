using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class TotalRoom : MonoBehaviour
{
    [Header("디자인 타일이 미리 그려진 미니맵 Tilemap")]
    public Tilemap minimapFrameTilemap;
    public Tilemap minimapInTilemap;
    
    [Header("카메라 & 저장키")]
    public Camera gameCamera;

    // 내부 저장용
    private HashSet<Vector3Int> allRoomCells = new HashSet<Vector3Int>();
    private Dictionary<Vector3Int, TileBase> originalTiles = new Dictionary<Vector3Int, TileBase>();
    private HashSet<Vector3Int> visitedCells = new HashSet<Vector3Int>();

    [SerializeField] private Trace playerPoint;
    [SerializeField] private GameObject[] checkerArray;
    public List<Vector2> targets = new List<Vector2>();
    
    private int checkerLayerMask;

    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        // 1. 모든 그려진 타일 위치 저장 및 비활성화
        var frameBounds = minimapFrameTilemap.cellBounds;
        foreach (var pos in frameBounds.allPositionsWithin)
        {
            if (minimapFrameTilemap.HasTile(pos))
            {
                allRoomCells.Add(pos);
                originalTiles[pos] = minimapFrameTilemap.GetTile(pos);
            }
        }
        minimapFrameTilemap.ClearAllTiles();
        
        var inBounds = minimapInTilemap.cellBounds;
        foreach (var pos in inBounds.allPositionsWithin)
        {
            if (minimapInTilemap.HasTile(pos))
            {
                allRoomCells.Add(pos);
                originalTiles[pos] = minimapInTilemap.GetTile(pos);
            }
        }
        minimapInTilemap.ClearAllTiles();
        
        checkerLayerMask = 1 << LayerMask.NameToLayer(ConstValues.Minimap);
    }

    void Start()
    {
        // 2. 저장된 데이터 유무에 따라 초기 복원
        if (!PlayerPrefs.HasKey(ConstValues.MiniMapVisitedCells))
        {
            // 저장 데이터 없음: 모든 타일 비활성화
            minimapFrameTilemap.ClearAllTiles();
            minimapInTilemap.ClearAllTiles();
        }
        else
        {
            // 저장 데이터 있음: 불러와서 해당 셀만 활성화
            LoadVisitedCells();
            foreach (var cell in visitedCells)
            {
                if (originalTiles.TryGetValue(cell, out var frameTile))
                    minimapFrameTilemap.SetTile(cell, frameTile);
                
                if (originalTiles.TryGetValue(cell, out var inTile))
                    minimapInTilemap.SetTile(cell, inTile);
            }
        }

        // 체크포인트 불러오기
        LoadCheckerPos();
    }

    private void Update()
    {
        SetPlayerPoint();
        RevealCellsInView();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SaveVisitedCells();
        }
    }

    private void SetPlayerPoint()
    {
        if (GameManager.Instance.CurPlayer && playerPoint.IsTargetNull())
        {
            playerPoint.SetTarget(GameManager.Instance.CurPlayer.CenterPos);
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
        
        float extraV = minimapFrameTilemap.cellSize.y;
        viewRect.yMin += extraV * 2;
        viewRect.yMax += extraV * 3;

        Vector2 halfCell = minimapFrameTilemap.cellSize; // * 0.5f minimapTilemap.cellSize
        
        bool anyNew = false;

        foreach (var cell in allRoomCells)
        {
            if (visitedCells.Contains(cell))
                continue;

            Vector3 center = minimapFrameTilemap.GetCellCenterWorld(cell);
            Vector2 min = new Vector2(center.x - halfCell.x, center.y - halfCell.y);
            Vector2 max = new Vector2(center.x + halfCell.x, center.y + halfCell.y);

            // 타일이 카메라 뷰와 조금이라도 겹치면 활성화
            if (max.x >= viewRect.xMin && min.x <= viewRect.xMax &&
                max.y >= viewRect.yMin && min.y <= viewRect.yMax)
            {
                visitedCells.Add(cell);
                minimapFrameTilemap.SetTile(cell, originalTiles[cell]);
                minimapInTilemap.SetTile(cell, originalTiles[cell]);
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

        PlayerPrefs.SetString(ConstValues.MiniMapVisitedCells, sb.ToString());
        PlayerPrefs.Save();
        Debug.Log("미니맵 저장!");
    }

    // 2. 저장된 방문 셀 로드
    private void LoadVisitedCells()
    {
        string data = PlayerPrefs.GetString(ConstValues.MiniMapVisitedCells, "");
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
    
    public void SpawnChecker()
    {
        bool isFull = true;
        int idx = 0;
        for (var i = 0; i < checkerArray.Length; i++)
        {
            if (!checkerArray[i].activeSelf)
            {
                isFull = false;
                idx = i;
                break;
            }
        }

        var miniMapCameraPos = GameManager.Instance.MiniMapCamera.position;
        
        // 1) 카메라 위치와 정면 방향으로 3D Ray 생성
        Ray ray = new Ray(miniMapCameraPos, GameManager.Instance.MiniMapCamera.forward);

        // 2) 2D 콜라이더와의 교차 검사
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, 20, checkerLayerMask);

        // 3) 히트 여부 확인
        if (hit.collider != null)
        {
            foreach (var checker in checkerArray)
            {
                if (checker == hit.collider.gameObject)
                {
                    checker.gameObject.SetActive(false);
                    targets.Remove(checker.transform.position);
                }
            }
        }
        else
        {
            if (isFull)
                return;
            checkerArray[idx].transform.position = new Vector2(miniMapCameraPos.x, miniMapCameraPos.y);
            checkerArray[idx].SetActive(true);
            targets.Add(checkerArray[idx].transform.position);
        }
        SaveCheckerPos();
    }

    private void SaveCheckerPos()
    {
        // 몇 개를 저장했는지 기록
        PlayerPrefs.SetInt(ConstValues.MiniMapCheckers, targets.Count);

        for (int i = 0; i < targets.Count; i++)
        {
            Vector3 p = targets[i];
            PlayerPrefs.SetFloat($"Target_{i}_X", p.x);
            PlayerPrefs.SetFloat($"Target_{i}_Y", p.y);
        }
        PlayerPrefs.Save();
        Debug.Log($"[{targets.Count}]개 오브젝트 위치 저장 완료");
    }

    private void LoadCheckerPos()
    {
        int savedCount = PlayerPrefs.GetInt(ConstValues.MiniMapCheckers, 0);
        //int useCount = Mathf.Min(savedCount, targets.Count);

        for (int i = 0; i < savedCount; i++)
        {
            float x = PlayerPrefs.GetFloat($"Target_{i}_X");
            float y = PlayerPrefs.GetFloat($"Target_{i}_Y");
            targets.Add(new Vector2(x, y));
        }

        for (int i = 0; i < targets.Count; i++)
        {
            checkerArray[i].transform.position = targets[i];
            checkerArray[i].SetActive(true);
        }
        Debug.Log($"[{savedCount}]개 오브젝트 위치 불러오기 완료 (저장된: {savedCount}, 설정된: {targets.Count})");
    }

    private void OnApplicationQuit()
    {
        SaveVisitedCells();
    }
}
