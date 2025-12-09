using System;
using UnityEngine;

public class PatrolTest : MonoBehaviour
{
    private float speed = 7.0f;
    
    private float pointA = 110;
    private float pointB = 120;
    private Vector2 dir;

    private Rigidbody2D myRigidbody;
    private BoxCollider2D myBoxCollider;

    private void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        myBoxCollider = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        dir = Vector2.left;
        //PatrolRay();
    }

    private void Update()
    {
        Move1();
    }

    private void FixedUpdate()
    {
        //Move2();
    }

    // 좌표값 이동(Update에서 사용)
    private void Move1()
    {
        if (dir == Vector2.left)
        {
            if (Vector2.Distance(transform.position, new Vector2(pointA, transform.position.y)) < 0.1f)
            {
                dir = Vector2.right;
            }
        }
        else if (dir == Vector2.right)
        {
            if (Vector2.Distance(transform.position, new Vector2(pointB, transform.position.y)) < 0.1f)
            {
                dir = Vector2.left;
            }
        }
        
        transform.Translate(dir * (speed * Time.deltaTime));
    }

    // 물리값 이동(FixedUpdate에서 사용)
    private void Move2()
    {
        float targetSpeedX = speed * dir.x;
        float targetSpeedY = myRigidbody.linearVelocity.y;
    
        if (dir == Vector2.left)
        {
            if (Vector2.Distance(transform.position, new Vector2(pointA, transform.position.y)) < 0.1f)
            {
                dir = Vector2.right;
                myRigidbody.linearVelocity = new Vector2(0, myRigidbody.linearVelocityY);
            }
        }
        else if (dir == Vector2.right)
        {
            if (Vector2.Distance(transform.position, new Vector2(pointB, transform.position.y)) < 0.1f)
            {
                dir = Vector2.left;
                myRigidbody.linearVelocity = new Vector2(0, myRigidbody.linearVelocityY);
            }
        }
        
        myRigidbody.linearVelocity = new Vector2(targetSpeedX, targetSpeedY);
    }
}
