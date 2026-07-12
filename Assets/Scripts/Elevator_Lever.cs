using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Elevator_Lever : Lever
{
    private bool isTouch;

    private Elevator elevator;
    private Func<bool> elevatorActon;
    private Collider2D myCollider;

    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        AnimTrigger(isTouch ? ConstValues.Right : ConstValues.Left);
    }

    public void SetState(bool touch)
    {
        isTouch = touch;
        myCollider.enabled = !isTouch;
    }

    public void AnimSwitch()
    {
        AnimTrigger(isTouch ? ConstValues.SwitchRight : ConstValues.SwitchLeft);
    }

    public void SetAction(Func<bool> action)
    {
        elevatorActon = action;
    }

    // 상호작용 프롬프트는 사용하지 않음 (공격으로 작동하도록 변경)
    public override void SpawnInteractionObject()
    {
    }

    // 플레이어 공격에 맞으면 작동
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (isTouch)   // 비활성 레버는 작동하지 않음
            return;

        var attack = col.GetComponent<Attack>();
        if (attack == null || !(attack.CastChar is Player))   // 플레이어의 공격만 인정
            return;

        // 실제로 엘리베이터가 작동했을 때만 연출 재생 (이동 중이면 무반응)
        if (elevatorActon == null || !elevatorActon.Invoke())
            return;

        SpawnEffect(ConstValues.LeverEffect, transform.position);
        SoundManager.Instance.PlaySound(ConstValues.Lever);
    }
}
