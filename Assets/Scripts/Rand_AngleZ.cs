using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rand_AngleZ : MonoBehaviour
{
    public GameObject AngleObject;
    public int Rand1, Rand2;

    private void OnEnable()
    {
        int angleZ = Random.Range(Rand1, Rand2);
        AngleObject.transform.eulerAngles = new Vector3(0, 0, angleZ);
    }
}
