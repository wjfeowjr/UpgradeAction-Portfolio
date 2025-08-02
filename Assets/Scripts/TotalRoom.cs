using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class TotalRoom : MonoBehaviour
{
    [SerializeField] private Trace playerPoint;
    [SerializeField] private GameObject[] checkerArray;
    public List<Vector2> targets = new List<Vector2>();
    
    private int checkerLayerMask;

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
        if (GameManager.Instance.CurPlayer && playerPoint.IsTargetNull())
        {
            playerPoint.SetTarget(GameManager.Instance.CurPlayer.CenterPos);
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
}
