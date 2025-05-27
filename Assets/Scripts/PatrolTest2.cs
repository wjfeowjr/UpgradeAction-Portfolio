using System;
using UnityEngine;

public class PatrolTest2 : MonoBehaviour
{
    private float pointA = -5;
    private float pointB = 5;
    private Vector2 dir;
    private float speed = 7.0f;

    private void Start()
    {
        dir = Vector2.left;
    }

    private void Update()
    {
        Move();
    }

    private void Move()
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
}
