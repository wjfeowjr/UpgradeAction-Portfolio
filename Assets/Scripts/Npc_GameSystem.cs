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

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 착지
        if ((other.gameObject.CompareTag(ConstValues.Ground) || other.gameObject.CompareTag(ConstValues.Wall)) && landingState == ELandingState.Air)
        {
            SpawnObject(ConstValues.NpcGameSystemDie, transform.position);
            gameObject.SetActive(false);
        }
    }
}