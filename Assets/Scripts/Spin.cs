using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class Spin : MonoBehaviour
{
    private bool spinSwitch;
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
        spinSwitch = true;
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
        SpinAngle();
    }

    private void SpinAngle()
    {
        if (!spinSwitch)
            return;
        
        if (transform.localScale.x > 0)
            angleZ -= spinSpeed * Time.deltaTime;
        else
            angleZ += spinSpeed * Time.deltaTime;

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, angleZ);
    }

    public void SpinSwitchOn(bool on)
    {
        spinSwitch = on;
    }
    
    public void Stop(int targetZ = -1)
    {
        SpinSwitchOn(false);
        if (targetZ != -1)
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, targetZ);
    }
    
    public void StopAndReset()
    {
        SpinSwitchOn(false);
        transform.eulerAngles = Vector3.zero;
    }

    public void SetSpinSpeed(bool plus)
    {
        var originSpeed = Mathf.Abs(spinSpeed);
        if (plus)
            spinSpeed = originSpeed;
        else
            spinSpeed = -originSpeed;
    }

    public void DeleteSpinObject(int idx)
    {
        spinObjects[idx].SetActive(false);
    }
}
