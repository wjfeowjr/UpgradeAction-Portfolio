using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : Singleton<Controller>
{
    [SerializeField] private Player player;
    public bool isLeftMove;
    public bool isRightMove;

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
        if (Input.GetKeyDown(GameManager.Instance.moveLeftKey))
        {
            isLeftMove = true;
            if(isRightMove)
                isRightMove = false;
        }
        
        if (Input.GetKeyDown(GameManager.Instance.moveRightKey))
        {
            if(isLeftMove)
                isLeftMove = false;
            isRightMove = true;
        }

        if (Input.GetKeyUp(GameManager.Instance.moveLeftKey))
        {
            isLeftMove = false;
            if(Input.GetKey(GameManager.Instance.moveRightKey))
                isRightMove = true;
        }

        if (Input.GetKeyUp(GameManager.Instance.moveRightKey))
        {
            isRightMove = false;
            if(Input.GetKey(GameManager.Instance.moveLeftKey))
                isLeftMove = true;
        }
    }

    private void PlayerControl()
    {
        if (Input.GetKeyDown(GameManager.Instance.attackKey))
            player.Attack();
        if (Input.GetKeyDown(GameManager.Instance.jumpKey))
            player.Jump();
        
        if (Input.GetKeyDown(GameManager.Instance.dashKey))
            player.Skill(GameManager.Instance.dashKey);
        if (Input.GetKeyDown(GameManager.Instance.skillKey2))
            player.Skill(GameManager.Instance.skillKey2);
        if (Input.GetKeyDown(GameManager.Instance.skillKey3))
            player.Skill(GameManager.Instance.skillKey3);
        if (Input.GetKeyDown(GameManager.Instance.skillKey4))
            player.Skill(GameManager.Instance.skillKey4);
        
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
