using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class FollowCamera : MonoBehaviour
{
    private Camera myCamera;
    private float shakeAmount;
    private float shakeTime;
    private Vector2 shakeVector;
    private Vector3 initialPosition;
    
    [SerializeField] private float xMargin;      // 카메라가 따라 가기 전에 플레이어가 이동할 수있는 x 축의 거리.
    [SerializeField] private float yMargin;      // 카메라가 따라 가기 전에 플레이어가 이동할 수있는 y 축의 거리.
    [SerializeField] private float xSmooth;      // 카메라가 x 축에서 목표 이동을 따라 잡는 것이 얼마나 부드럽게 수행되는지.
    [SerializeField] private float ySmooth;      // 카메라가 y 축에서 목표 이동을 따라 잡는 것이 얼마나 부드럽게 수행되는지.
    
    [SerializeField] private Vector2 maxXAndY;   // 카메라가 가질 수있는 최대 x 및 y 좌표입니다.
    [SerializeField] private Vector2 minXAndY;   // 카메라가 가질 수있는 최소 x 및 y 좌표입니다.

    [SerializeField] private float targetX;
    [SerializeField] private float targetY;
    
    public Vector2 MaxXAndY
    {
        get => maxXAndY;
        set => maxXAndY = value;
    }
    public Vector2 MinXAndY
    {
        get => minXAndY;
        set => minXAndY = value;
    }

    public Camera MyCamera => myCamera;
    
    private void Awake()
    {
        myCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        // 카메라 제약이 있을때만 플레이어를 따라다닌다(X축)
        TrackPlayer();
    }

    bool CheckXMargin()
    {
        // x 축의 카메라와 플레이어 사이의 거리가 x 여백보다 큰 경우 true를 반환합니다.
        return Mathf.Abs(transform.position.x - GameManager.Instance.CurPlayer.transform.position.x) > xMargin;
    }

    bool CheckYMargin()
    {
        // y 축의 카메라와 플레이어 사이의 거리가 x 여백보다 큰 경우 true를 반환합니다.
        return Mathf.Abs(transform.position.y - GameManager.Instance.CurPlayer.transform.position.y) > yMargin;
    }

    public void Shake(float amount, float time)
    {
        shakeAmount = amount;
        shakeTime = time;
    }

    private void TrackPlayer()
    {
        if(!GameManager.Instance.CurPlayer)
            return;
        
        if (shakeTime > 0)
        {
            if (Time.timeScale > 0)
            {
                shakeVector = new Vector2(Random.insideUnitSphere.x * shakeAmount, Random.insideUnitSphere.y * shakeAmount);
                shakeTime -= Time.deltaTime;
            }
            else
            {
                shakeVector = Vector2.zero;
            }
        }
        else
        {
            shakeVector = Vector2.zero;
            shakeTime = 0.0f;
        }

        // 기본적으로 카메라의 목표 x 및 y 좌표는 현재 x 및 y 좌표입니다.
        targetX = transform.position.x;
        targetY = transform.position.y;

        // 플레이어가 x 마진을 넘어서 움직 였다면
        if (CheckXMargin())
            // 대상 x 좌표는 카메라의 현재 x 위치와 플레이어의 현재 x 위치 사이의 Lerp 여야합니다.
            targetX = Mathf.Lerp(transform.position.x, GameManager.Instance.CurPlayer.transform.position.x, xSmooth * Time.deltaTime);

        // 플레이어가 y 마진을 넘어서 움직 였다면
        if (CheckYMargin())
            // 대상 y 좌표는 카메라의 현재 y 위치와 플레이어의 현재 y 위치 사이의 Lerp 여야합니다.
            targetY = Mathf.Lerp(transform.position.y, GameManager.Instance.CurPlayer.transform.position.y, ySmooth * Time.deltaTime);

        // 목표 x 및 y 좌표는 최소값보다 크거나 작아야합니다.
        targetX = Mathf.Clamp(targetX, minXAndY.x, maxXAndY.x);
        targetY = Mathf.Clamp(targetY, minXAndY.y, maxXAndY.y);
        
        Vector3 newPos = new Vector3(targetX + shakeVector.x, targetY + shakeVector.y, transform.position.z);
        newPos.x = Mathf.Round(newPos.x * 100f) / 100f;
        newPos.y = Mathf.Round(newPos.y * 100f) / 100f;
        transform.position = newPos;

        // 동일한 z 구성 요소를 사용하여 카메라의 위치를 ​​목표 위치로 설정하십시오.
        //transform.position = new Vector3(targetX + shakeVector.x, targetY + shakeVector.y, transform.position.z);
    }
}
