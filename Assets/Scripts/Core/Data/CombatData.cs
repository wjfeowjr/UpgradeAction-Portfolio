// 전투 오브젝트 런타임 데이터

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GrenadeCopy
{
    public string id;
    public string minForce;
    public string maxForce;
    public float timer;
    public bool spinGrenade;
    public bool dirObject;
    public string hitTag;
    public string spawnObject;
}
