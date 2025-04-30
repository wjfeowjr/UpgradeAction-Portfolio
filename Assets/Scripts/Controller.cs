using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Controller : Singleton<Controller>
{
    private Player player;
    public bool isLeftMove;
    public bool isRightMove;

    private async void Start()
    {
        await UniTask.WaitUntil(() => GameManager.Instance.CurPlayer != null);
        player = GameManager.Instance.CurPlayer;
    }

    private void Update()
    {
        DirControl();
        PlayerControl();
    }

    void FixedUpdate()
    {
        MovingControl();
    }

    // 방향 컨트롤(좌,우 동시입력 방지)
    private void DirControl()
    {
        if (Input.GetKeyDown(GameManager.Instance.leftMoveKey))
        {
            isLeftMove = true;
            if(isRightMove)
                isRightMove = false;
        }
        
        if (Input.GetKeyDown(GameManager.Instance.rightMoveKey))
        {
            if(isLeftMove)
                isLeftMove = false;
            isRightMove = true;
        }

        if (Input.GetKeyUp(GameManager.Instance.leftMoveKey))
        {
            isLeftMove = false;
            if(Input.GetKey(GameManager.Instance.rightMoveKey))
                isRightMove = true;
        }

        if (Input.GetKeyUp(GameManager.Instance.rightMoveKey))
        {
            isRightMove = false;
            if(Input.GetKey(GameManager.Instance.leftMoveKey))
                isLeftMove = true;
        }
    }

    private void PlayerControl()
    {
        if (Input.GetKey(GameManager.Instance.attackKey))
            player.Attack();
        if (Input.GetKeyDown(GameManager.Instance.jumpKey))
            player.Jump();
        
        if (Input.GetKeyDown(GameManager.Instance.dashKey))
            player.Skill(GameManager.Instance.dashKey);
        
        if (Input.GetKeyDown(GameManager.Instance.skillKey1))
            player.Skill(GameManager.Instance.skillKey1);
        if (Input.GetKeyDown(GameManager.Instance.skillKey2))
            player.Skill(GameManager.Instance.skillKey2);
        if (Input.GetKeyDown(GameManager.Instance.skillKey3))
            player.Skill(GameManager.Instance.skillKey3);
        if (Input.GetKeyDown(GameManager.Instance.skillKey4))
            player.Skill(GameManager.Instance.skillKey4);
        if (Input.GetKeyDown(GameManager.Instance.skillKey5))
            player.Skill(GameManager.Instance.skillKey5);
        if (Input.GetKeyDown(GameManager.Instance.skillKey6))
            player.Skill(GameManager.Instance.skillKey6);
        if (Input.GetKeyDown(GameManager.Instance.skillKey7))
            player.Skill(GameManager.Instance.skillKey7);
        if (Input.GetKeyDown(GameManager.Instance.skillKey8))
            player.Skill(GameManager.Instance.skillKey8);
        
        // if (Input.GetKeyDown(KeyCode.Q))
        //     player.Grabbed(new Vector2(0, -2.0f));
        //
        // if (Input.GetKeyDown(KeyCode.W))
        //     player.Airborne(6, 12);
        //
        // if (Input.GetKeyDown(KeyCode.E))
        //     player.Stun(3.0f);
        //
        // if (Input.GetKeyDown(KeyCode.R))
        // {
        //     player.Damaged(0.5f);
        //     player.KnockBack(1.0f);
        // }

        if(!isLeftMove && !isRightMove)
            player.Stop();
    }
     
    private void MovingControl()
    {
        if(isLeftMove)
            player.Move(Vector2.left);
        else if (isRightMove)
            player.Move(Vector2.right); 
    }
}
