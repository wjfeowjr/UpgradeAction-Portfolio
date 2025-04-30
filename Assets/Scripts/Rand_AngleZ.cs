using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Rand_AngleZ : MonoBehaviour
{
    public GameObject angleObject;
    public int rand1;
    public int rand2;

    private void OnEnable()
    {
        int angleZ = Random.Range(rand1, rand2);
        angleObject.transform.eulerAngles = new Vector3(0, 0, angleZ);
    }
}
