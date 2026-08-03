using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RandAngle : MonoBehaviour
{
    [SerializeField] private GameObject angleObject;
    [SerializeField] private Vector2 randAngleX;
    [SerializeField] private Vector2 randAngleY;
    [SerializeField] private Vector2 randAngleZ;

    private void OnEnable()
    {
        int angleX = (int)angleObject.transform.eulerAngles.x;
        int angleY = (int)angleObject.transform.eulerAngles.y;
        int angleZ = (int)angleObject.transform.eulerAngles.z;
        
        if(randAngleX != Vector2.zero)
            angleX = Random.Range((int)randAngleX.x, (int)randAngleX.y);
        
        if(randAngleY != Vector2.zero)
            angleY = Random.Range((int)randAngleY.x, (int)randAngleY.y);
        
        if(randAngleZ != Vector2.zero)
            angleZ = Random.Range((int)randAngleZ.x, (int)randAngleZ.y);
        
        angleObject.transform.eulerAngles = new Vector3(angleX, angleY, angleZ);
    }
}
