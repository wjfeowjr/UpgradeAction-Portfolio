using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandPos : MonoBehaviour
{
    [SerializeField] private Transform posObject;
    [SerializeField] private float randX;
    [SerializeField] private float randY;
    
    private Vector3 defaultVector;
    
    private void Awake()
    {
        defaultVector = posObject.localPosition;
    }

    private void OnEnable()
    {
        float x = Random.Range(-randX, randX);
        float y = Random.Range(-randY, randY);
        posObject.localPosition = new Vector2(defaultVector.x + x, defaultVector.y + y);
    }
}
