using System.Collections.Generic;
using System.Linq;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class TotalRoom : MonoBehaviour
{
    [SerializeField] private Trace playerPoint;
    [SerializeField] private PlaceName[] placeNames;
    [SerializeField] private Room[] roomArray;
    [SerializeField] private GameObject[] checkerArray;
    
    private Player targetPlayer;

    private int checkerLayerMask;

    public Room[] RoomArray => roomArray;
    
    private void Awake()
    {
        checkerLayerMask = 1 << LayerMask.NameToLayer(ConstValues.Minimap);
    }

    void Start()
    {
        // 체크포인트 불러오기
        LoadCheckerPos();
    }

    private void Update()
    {
        SetPlayerPoint();
    }

    private void SetPlayerPoint()
    {
        if (targetPlayer)
        {
            if (GameManager.Instance.CurPlayer != targetPlayer)
            {
                playerPoint.SetTarget(GameManager.Instance.CurPlayer.CenterPos);
                targetPlayer = GameManager.Instance.CurPlayer;
            }
        }
        else
        {
            if (GameManager.Instance.CurPlayer && playerPoint.IsTargetNull())
            {
                playerPoint.SetTarget(GameManager.Instance.CurPlayer.CenterPos);
                targetPlayer = GameManager.Instance.CurPlayer;
            }
        }
    }

    public void SetPlaceName()
    {
        foreach (var placeName in placeNames)
            placeName.SetText();
    }

    public void ActivePlaceName()
    {
        bool newPlace = false;
        List<string> placeList = new List<string>();
        foreach (var room in roomArray)
        {
            var place = room.VisitedPlace();
            if (!string.IsNullOrWhiteSpace(place) && !placeList.Contains(place))
            {
                placeList.Add(place);
            }
        }

        foreach (var placeName in placeNames)
        {
            bool isVisited = false;
            foreach (var place in placeList)
            {
                if (placeName.Place != place)
                    continue;
                
                isVisited = true;
                break;
            }
            placeName.gameObject.SetActive(isVisited);
        }
    }

    public Room TargetRoom(string roomId)
    {
        Room targetRoom = null;
        foreach (var room in roomArray)
        {
            if (room.Id == roomId)
            {
                targetRoom = room;
                break;
            }
        }
        return targetRoom;
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
                    GameManager.Instance.MiniMapCheckers.Remove(checker.transform.position);
                }
            }
        }
        else
        {
            if (isFull)
                return;
            checkerArray[idx].transform.position = new Vector2(miniMapCameraPos.x, miniMapCameraPos.y);
            checkerArray[idx].SetActive(true);
            GameManager.Instance.MiniMapCheckers.Add(checkerArray[idx].transform.position);
        }
        SaveCheckerPos();
    }

    private void SaveCheckerPos()
    {
        // for (int i = 0; i < GameManager.Instance.MiniMapCheckers.Count; i++)
        // {
        //     Vector3 p = GameManager.Instance.MiniMapCheckers[i];
        //     PlayerPrefs.SetFloat($"Target_{i}_X", p.x);
        //     PlayerPrefs.SetFloat($"Target_{i}_Y", p.y);
        // }
        
        //GameManager.Instance.SaveGame();
        Debug.Log($"[{GameManager.Instance.MiniMapCheckers.Count}]개 오브젝트 위치 저장 완료");
    }

    private void LoadCheckerPos()
    {
        for (int i = 0; i < GameManager.Instance.MiniMapCheckers.Count; i++)
        {
            checkerArray[i].transform.position = GameManager.Instance.MiniMapCheckers[i];
            checkerArray[i].SetActive(true);
        }
        Debug.Log($"[{GameManager.Instance.MiniMapCheckers.Count}]개 오브젝트 위치 불러오기 완료)");
    }

    // 인스펙터 우클릭 메뉴에 “Cache Prefabs” 항목 추가
    [ContextMenu("Cache Rooms")]
    private void CachePrefabs()
    {
#if UNITY_EDITOR
        roomArray = GetComponentsInChildren<Room>();
        Debug.Log($"룸 캐싱 완료");
        foreach (var room in roomArray)
            room.CacheObjects();
        
        EditorUtility.SetDirty(this);          // 이 스크립트 붙은 객체도 더티
        EditorSceneManager.MarkSceneDirty(gameObject.scene); // 씬 더티 표시
#else
        Debug.LogWarning("CachePrefabs는 에디터 모드에서만 동작합니다.");
#endif
    }
    
    // 인스펙터 우클릭 메뉴에 “Cache Prefabs” 항목 추가
    [ContextMenu("ObjectNameChange")]
    private void ObjectNameChange()
    {
#if UNITY_EDITOR
        roomArray = GetComponentsInChildren<Room>();
        Debug.Log($"오브젝트 이름 변경 완료");
        foreach (var room in roomArray)
            room.ObjectNameChange();
        
        EditorUtility.SetDirty(this);          // 이 스크립트 붙은 객체도 더티
        EditorSceneManager.MarkSceneDirty(gameObject.scene); // 씬 더티 표시
#else
        Debug.LogWarning("CachePrefabs는 에디터 모드에서만 동작합니다.");
#endif
    }
}
