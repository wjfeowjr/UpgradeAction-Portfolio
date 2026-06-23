using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class CandleObject
{
    public GameObject candle;
    public bool on;
}

public class Candle : MonoBehaviour
{
    [SerializeField] private CandleObject[] candleObjects;
    
    void Start()
    {
        foreach (var candleObject in candleObjects)
            candleObject.candle.SetActive(candleObject.on);
    }
}
