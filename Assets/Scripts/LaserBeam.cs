using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class LaserBeam : MonoBehaviour
{
    private LineRenderer myLineRenderer;
    private BoxCollider2D myBoxCollider;

    [Header("Prefabs")]
    [SerializeField] private GameObject beamLineRendererPrefab; //Put a prefab with a line renderer onto here.
    [SerializeField] private GameObject beamStartPrefab; //This is a prefab that is put at the start of the beam.
    [SerializeField] private GameObject beamEndPrefab; //Prefab put at end of beam.
    
    [Header("Beam Options")]
    [SerializeField] private Vector3 endVector;

    //public bool alwaysOn = true; //Enable this to spawn the beam when script is loaded.
    //public bool beamCollides = true; //Beam stops at colliders
    [SerializeField] private float beamLength = 0;            // 레이져 길이
    [SerializeField] private float firstBeamLength = 0;
    [SerializeField] private float beamEndOffset = 0f;        // 레이캐스트 히트 포인트에서 엔드 이펙트가 위치하는 거리
    [SerializeField] private float textureScrollSpeed = 0f;   // 텍스처가 빔을 따라 스크롤하는 속도는 음수 또는 양수일 수 있습니다.
    [SerializeField] private float textureLengthScale = 0f;   // 수직을 기준으로 텍스처의 수평 길이로 설정합니다.
                                            // 예: 텍스처의 높이가 200픽셀이고 길이가 600픽셀인 경우 이 값을 3으로 설정합니다.
    [SerializeField] private bool expansion;
    [SerializeField] private float laserScale;
    [SerializeField] private float limitScale;

    [SerializeField] private float prefabScale;
    [SerializeField] private float limitPrefabScale;

    [SerializeField] private float second;
    private int layerMask;

    private void Awake()
    {
        myLineRenderer = beamLineRendererPrefab.GetComponent<LineRenderer>();
        myBoxCollider = GetComponent<BoxCollider2D>();
        layerMask = 1 << LayerMask.NameToLayer(ConstValues.Ground) | 1 << LayerMask.NameToLayer(ConstValues.Platform) | 1 << LayerMask.NameToLayer(ConstValues.Wall);
    }

    private void OnEnable()
    {
        expansion = true;
        laserScale = 0;
        prefabScale = 0;
        beamStartPrefab.transform.localScale = Vector3.zero;
        beamEndPrefab.transform.localScale = Vector3.zero;

        SpawnBeam();
        ScaleControl();
        LaserShot();
    }
    
    private void Start()
    {
        firstBeamLength = beamLength;
        BeamShot(firstBeamLength);
    }

    private void FixedUpdate()
    {
        LaserShot();
    }

    private void BeamShot(float targetBeamLength)
    {
        Quaternion quaternion = Quaternion.Euler(0, 0, transform.eulerAngles.z);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, quaternion * Vector2.up, targetBeamLength, layerMask);
        Debug.DrawRay(transform.position, quaternion * Vector2.up * targetBeamLength, ConstValues.RedColor, 0.025f);
        if (hit.collider != null)
            targetBeamLength = Vector2.Distance(transform.position, hit.point);
        
        // 콜라이더 체크
        if (hit.collider != null &&
            (transform.eulerAngles.z is >= 0 and < 180 && hit.point.x < transform.position.x || // 왼쪽
             transform.eulerAngles.z is >= 180 and < 360 && hit.point.x > transform.position.x)) // 오른쪽
        {
            endVector = new Vector3(hit.point.x, hit.point.y, 0) - (quaternion * Vector3.up * beamEndOffset);
            if (myBoxCollider)
                myBoxCollider.size = new Vector2(myBoxCollider.size.x, Vector2.Distance(transform.position, endVector));
        }
        else
        {
            endVector = transform.position + (quaternion * Vector3.up * targetBeamLength);
            if (myBoxCollider)
                myBoxCollider.size = new Vector2(myBoxCollider.size.x, targetBeamLength);
        }
    }

    private async void ScaleControl()
    {
        while(true)
        {
            if (expansion)
            {
                if (laserScale < limitScale)
                    laserScale += 0.2f * Time.timeScale;
                else
                    laserScale = limitScale;

                if (prefabScale < limitPrefabScale)
                {
                    prefabScale += 0.2f * Time.timeScale;
                    beamStartPrefab.transform.localScale = new Vector3(prefabScale, prefabScale, prefabScale);
                    beamEndPrefab.transform.localScale = new Vector3(prefabScale, prefabScale, prefabScale);
                }
                else
                {
                    prefabScale = limitPrefabScale;
                }
            }
            else
            {
                laserScale -= 0.1f * Time.timeScale;
                if (laserScale <= 0)
                {
                    laserScale = 0;
                    gameObject.SetActive(false);
                }

                prefabScale -= 0.05f * Time.timeScale;
                beamStartPrefab.transform.localScale = new Vector3(prefabScale, prefabScale, prefabScale);
                beamEndPrefab.transform.localScale = new Vector3(prefabScale, prefabScale, prefabScale);
                if (prefabScale <= 0)
                {
                    prefabScale = 0;
                    gameObject.SetActive(false);
                }
            }

            if (myLineRenderer)
            {
                myLineRenderer.startWidth = laserScale;
                myLineRenderer.endWidth = laserScale;
            }
            await UniTask.Yield();
        }        
    }

    public void LaserShot()
    {
        if (beamLineRendererPrefab) //Updates the beam
        {
            if (myLineRenderer)
                myLineRenderer.SetPosition(0, transform.position);

            BeamShot(beamLength);

            // 박스 콜라이더 위치
            if (myBoxCollider)
                myBoxCollider.offset = new Vector2(0, myBoxCollider.size.y * 0.5f);

            if (myLineRenderer)
                myLineRenderer.SetPosition(1, endVector);

            if (beamStartPrefab)
            {
                beamStartPrefab.transform.position = transform.position;
            }
            if (beamEndPrefab)
            {
                beamEndPrefab.transform.position = endVector;
            }

            float distance = Vector2.Distance(transform.position, endVector);
            if (myLineRenderer)
            {
                myLineRenderer.material.mainTextureScale = new Vector2(distance / textureLengthScale, 1); // 이것은 텍스처의 크기를 설정하여 늘어나 보이지 않게 합니다.
                myLineRenderer.material.mainTextureOffset -= new Vector2(Time.deltaTime * textureScrollSpeed, 0); // 0으로 설정되지 않은 경우 빔을 따라 텍스처를 스크롤합니다.
            }
        }
    }

    public void SpawnBeam() // 이 함수는 linerenderer가 있는 프리팹을 생성합니다.
    {
        if (beamLineRendererPrefab)
        {
            beamLineRendererPrefab.transform.position = transform.position;
            beamLineRendererPrefab.transform.parent = transform;
            beamLineRendererPrefab.transform.rotation = transform.rotation;

            myLineRenderer.useWorldSpace = true;
#if UNITY_5_5_OR_NEWER
            myLineRenderer.positionCount = 2;
#else
			line.SetVertexCount(2); 
#endif
        }
        else
            print("Add a hecking prefab with a line renderer to the PixelArsenalBeamStatic script on " + gameObject.name + "! Heck!");
    }
}