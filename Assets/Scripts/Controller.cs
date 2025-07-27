using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Controller : Singleton<Controller>
{
    public bool isLeftMove;
    public bool isRightMove;

    private async void Start()
    {
        await UniTask.WaitUntil(() => GameManager.Instance.CurPlayer != null);
    }

    private void Update()
    {
        // if (!GameManager.Instance.ControlStart)
        // {
        //     if (isLeftMove)
        //         isLeftMove = false;
        //     if (isRightMove)
        //         isRightMove = false;
        //     return;
        // }
        
        StopControl();
        
        if(!GameManager.Instance.ControlStart)
            return;
        
        DirControl();
        PlayerControl();
        MovingControl();
    }

    private void FixedUpdate()
    {
        if(!GameManager.Instance.ControlStart)
            return;
        
        PlayerMove();
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            if (isLeftMove)
                isLeftMove = false;
            if (isRightMove)
                isRightMove = false;
            return;
        }
    }

    public void StopMove()
    {
        isLeftMove = false;
        isRightMove = false;
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
            isRightMove = true;
            if(isLeftMove)
                isLeftMove = false;
        }
    }

    private void StopControl()
    {
        if (Input.GetKeyUp(GameManager.Instance.leftMoveKey))
        {
            isLeftMove = false;
            if (Input.GetKey(GameManager.Instance.rightMoveKey))
            {
                if (GameManager.Instance.ControlStart)
                {
                    isRightMove = true;
                }
            }
            else
            {
                if(GameManager.Instance.CurPlayer.MoveState == EMoveState.Moving)
                    GameManager.Instance.CurPlayer.StopVelocity();
            }
        }

        if (Input.GetKeyUp(GameManager.Instance.rightMoveKey))
        {
            isRightMove = false;
            if (Input.GetKey(GameManager.Instance.leftMoveKey))
            {
                if (GameManager.Instance.ControlStart)
                {
                    isLeftMove = true;
                }
            }
            else
            {
                if(GameManager.Instance.CurPlayer.MoveState == EMoveState.Moving)
                    GameManager.Instance.CurPlayer.StopVelocity();
            }
        }
    }

    private void PlayerControl()
    {
        if (Input.GetKey(GameManager.Instance.attackKey))
            GameManager.Instance.CurPlayer.Attack();

        if (Input.GetKey(GameManager.Instance.downKey))
        {
            if (Input.GetKeyDown(GameManager.Instance.jumpKey) && GameManager.Instance.CurPlayer.MyRigidbody.linearVelocity.normalized.y == 0)
                GameManager.Instance.CurPlayer.DownJump();
        }
        else
        {
            if (Input.GetKeyDown(GameManager.Instance.jumpKey))
                GameManager.Instance.CurPlayer.Jump();
        }

        if (Input.GetKeyDown(GameManager.Instance.changeCharacterKey))
        {
            if(!string.IsNullOrEmpty(GameManager.Instance.SecondPlayer))
                GameManager.Instance.CurPlayer.ChangeCharacter();
        }
        
        if (Input.GetKeyDown(GameManager.Instance.dashKey))
            GameManager.Instance.CurPlayer.Skill(GameManager.Instance.dashKey);
        
        if (Input.GetKeyDown(GameManager.Instance.skillKey1))
            GameManager.Instance.CurPlayer.Skill(GameManager.Instance.skillKey1);
        if (Input.GetKeyDown(GameManager.Instance.skillKey2))
            GameManager.Instance.CurPlayer.Skill(GameManager.Instance.skillKey2);
        if (Input.GetKeyDown(GameManager.Instance.skillKey3))
            GameManager.Instance.CurPlayer.Skill(GameManager.Instance.skillKey3);
        if (Input.GetKeyDown(GameManager.Instance.skillKey4))
            GameManager.Instance.CurPlayer.Skill(GameManager.Instance.skillKey4);
        if (Input.GetKeyDown(GameManager.Instance.skillKey5))
            GameManager.Instance.CurPlayer.Skill(GameManager.Instance.skillKey5);
        if (Input.GetKeyDown(GameManager.Instance.skillKey6))
            GameManager.Instance.CurPlayer.Skill(GameManager.Instance.skillKey6);
        if (Input.GetKeyDown(GameManager.Instance.skillKey7))
            GameManager.Instance.CurPlayer.Skill(GameManager.Instance.skillKey7);
        if (Input.GetKeyDown(GameManager.Instance.skillKey8))
            GameManager.Instance.CurPlayer.Skill(GameManager.Instance.skillKey8);

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

        if(GameManager.Instance.CurPlayer && !isLeftMove && !isRightMove)
            GameManager.Instance.CurPlayer.Stop();
    }
     
    private void MovingControl()
    {
        if(isLeftMove)
            GameManager.Instance.CurPlayer.MoveSetting(Vector2.left);
        else if (isRightMove)
            GameManager.Instance.CurPlayer.MoveSetting(Vector2.right); 
    }

    private void PlayerMove()
    {
        if(isLeftMove)
            GameManager.Instance.CurPlayer.Move(Vector2.left);
        else if (isRightMove)
            GameManager.Instance.CurPlayer.Move(Vector2.right);
    }
}
