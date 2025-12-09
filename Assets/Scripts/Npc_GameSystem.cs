using System;
using UnityEngine;

public class Npc_GameSystem : Npc
{
    [SerializeField] private Spin spinObject;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        if (spinObject.enabled)
        {
            spinObject.enabled = false;
            spinObject.transform.eulerAngles = Vector3.zero;
        }
    }
    
    protected override void Update()
    {
        base.Update();
        if (normalState == ENormalState.Airborne)
        {
            if (!spinObject.enabled)
                spinObject.enabled = true;
        }
        else
        {
            if (spinObject.enabled)
            {
                spinObject.enabled = false;
                spinObject.transform.eulerAngles = Vector3.zero;
            }
        }
    }

    protected override void OnTriggerEnter2D(Collider2D col)
    {
        base.OnTriggerEnter2D(col);
        // 착지
        if (col.gameObject.CompareTag(ConstValues.Ground) && landingState == ELandingState.Air)
        {
            SpawnObject(ConstValues.NpcGameSystemDie, transform.position);
            gameObject.SetActive(false);
        }
    }
}