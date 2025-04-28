using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Spin : MonoBehaviour
{
    private float angleZ;
    private float firstSpeed;
    public float spinSpeed;

    private void Awake()
    {
        firstSpeed = spinSpeed;
    }

    private void OnEnable()
    {
        angleZ = 0;
    }

    private void OnDisable()
    {
        spinSpeed = firstSpeed;
    }

    private void Update()
    {
        //Debug.Log(Speed);
        SpinAngle();
    }

    private void SpinAngle()
    {
        if (transform.localScale.x > 0)
            angleZ -= spinSpeed * Time.timeScale;
        else
            angleZ += spinSpeed * Time.timeScale;

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, angleZ);
    }
}
