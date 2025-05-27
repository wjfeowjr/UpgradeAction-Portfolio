using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class Spin : MonoBehaviour
{
    private float angleZ;
    private float firstSpeed;
    [SerializeField] private float spinSpeed;
    [SerializeField] private GameObject[] spinObjects;

    private void Awake()
    {
        firstSpeed = spinSpeed;
    }

    private async void OnEnable()
    {
        angleZ = 0;

        await UniTask.Yield();
        foreach (var spinObject in spinObjects)
            spinObject.SetActive(true);
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
            angleZ -= spinSpeed * Time.deltaTime;
        else
            angleZ += spinSpeed * Time.deltaTime;

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, angleZ);
    }

    public void DeleteSpinObject(int idx)
    {
        spinObjects[idx].SetActive(false);
    }
}
