using System;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform[] points;
    [SerializeField] private float speed = 2f;

    private Rigidbody2D myRigidbody;
    private int targetIndex;
    private Vector2 prevPos;
    private Vector2 platformVelocity;
    private List<Vector2> movePos = new List<Vector2>();
    public Vector2 Velocity => platformVelocity;  // 외부 제공용
    
    private void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myRigidbody.bodyType = RigidbodyType2D.Kinematic;
        myRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        prevPos = transform.position;
        
        foreach (var point in points)
            movePos.Add(point.position);
    }

    private void FixedUpdate()
    {
        if (movePos.Count == 0)
            return;

        Vector2 cur = myRigidbody.position;
        Vector2 target = movePos[targetIndex];
        Vector2 next = Vector2.MoveTowards(cur, target, speed * Time.fixedDeltaTime);
        myRigidbody.MovePosition(next);

        // 2) 이번 프레임 플랫폼이 실제로 이동한 delta
        Vector2 delta = next - prevPos;
        platformVelocity = delta / Time.fixedDeltaTime;
        prevPos = next;

        // 4) 목적지 도착하면 다음 포인트
        if ((next - target).sqrMagnitude < 0.0001f)
            targetIndex = (targetIndex + 1) % movePos.Count;
    }
}
