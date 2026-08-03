// GameManager - 플레이어 관리 · 캐릭터 교체

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.U2D;


public partial class GameManager
{

    private void CreatePlayer()
    {
        players.Add(SpawnToObjectPool(ConstValues.Berserker, Vector2.zero).GetComponent<Player>());
        players.Add(SpawnToObjectPool(ConstValues.Gunner, Vector2.zero).GetComponent<Player>());
        players.Add(SpawnToObjectPool(ConstValues.Fighter, Vector2.zero).GetComponent<Player>());
        foreach (var player in players)
        {
            var playerSplit = player.name.Split('(');
            player.name = playerSplit[0];
            player.gameObject.SetActive(false);
        }
    }

    // 플레이어
    private void InitPlayer()
    {
        foreach (var player in players)
        {
            player.InitBasicStat();
            player.InitBonusStat();
            player.InitSkill();
            player.InitAnimation();
            player.SkillAttributeCheck();
            player.ApplyPassive();
        }
    }

    public void MovePlayer()
    {
        ControlStart = true;
        if (curPlayer)
        {
            curPlayer.Immortal = false;
            curPlayer.Dodge = false;
        }
    }

    public void StopPlayer()
    {
        ControlStart = false;
        if(curPlayer)
            curPlayer.Immortal = true;
    }

    public void SetPlayerHp(int hp)
    {
        foreach (var player in players)
            player.BasicStat.hp = hp;
    }

    public void AddPlayer(string player)
    {
        // 이미 추가된 캐릭터면 무시
        if (saveData.playerList.Contains(player))
            return;

        // 빈 리스트면 그대로 추가
        if (saveData.playerList.Count == 0)
        {
            saveData.playerList.Add(player);
            return;
        }

        // 첫 번째 요소를 앵커로 삼아 로테이션 상의 상대 위치 기준으로 정렬한다.
        // 예) [Gunner, Berserker]에 Fighter 추가 시 → [Gunner, Fighter, Berserker]
        int anchorIdx = Array.IndexOf(PlayerRotation, saveData.playerList[0]);
        int cycleLen = PlayerRotation.Length;

        saveData.playerList.Add(player);
        saveData.playerList.Sort((a, b) =>
        {
            int ra = (Array.IndexOf(PlayerRotation, a) - anchorIdx + cycleLen) % cycleLen;
            int rb = (Array.IndexOf(PlayerRotation, b) - anchorIdx + cycleLen) % cycleLen;
            return ra.CompareTo(rb);
        });
    }

    // 어떤 타입이든 받을 수 있는 회전 메서드
    private void RotatePlayerList()
    {
        // 1. 요소가 없거나 1개뿐이면 회전할 필요가 없음
        if (saveData.playerList.Count <= 1)
            return;

        // 2. 맨 앞의 아이템(0번 인덱스)을 임시 저장
        string firstIdx = saveData.playerList[0];

        // 3. 맨 앞의 아이템을 리스트에서 삭제 (남은 요소들이 앞으로 한 칸씩 당겨짐)
        saveData.playerList.RemoveAt(0);

        // 4. 저장해둔 아이템을 리스트의 맨 마지막에 추가
        saveData.playerList.Add(firstIdx);
    }

    public void InitPlayerStat()
    {
        foreach (var player in players)
        {
            player.InitBasicStat();
            player.InitBonusStat();
            player.ResetSkillCoolTime();
            player.ApplyPassive();
        }
    }

    public void SetPlayerAttribute()
    {
        foreach (var player in players)
            player.SkillAttributeCheck();
    }

    public void SpawnPlayer(string playerName)
    {
        ActivePlayer(playerName);
    }

    public Player GetPlayer(string playerName)
    {
        foreach (var player in players)
        {
            if (player.name == playerName)
                return player;
        }
        return null;
    }

    private void ActivePlayer(string playerName)
    {
        foreach (var player in players)
        {
            player.gameObject.SetActive(player.name == playerName);
            if (player.name == playerName)
                player.Flip(1);
        }
    }

    public void ReduceSkillPlayer()
    {
        changeSkill.playerSkill.ReducingCooldown();
        potionSkill.playerSkill.ReducingCooldown();
        foreach (var player in players)
            player.ReduceSkillCoolTime();
    }

    private void RefreshFace()
    {
        uiInterface.CharacterFaceView
            .Bind(new UICharacterFaceModel { playerList = PlayerList })
            .SetChangeFace();
    }

    public void RefreshPlayerHp()
    {
        var hpPresenter = uiInterface.HpView.Bind(new UIHpModel { player = CurPlayer });
        hpPresenter.SetHp();
        hpPresenter.SetHpText();
    }

    public void RefillPlayerHp()
    {
        foreach (var player in players)
            player.BasicStat.hp = player.BasicStat.maxHp;

        RefreshPlayerHp();
    }

    // 캐릭 변경할때, 때릴때
    public void RefreshPlayerResource()
    {
        var hpPresenter = uiInterface.HpView.Bind(new UIHpModel { player = CurPlayer });
        hpPresenter.SetResource();
        hpPresenter.SetResourceText();
    }

    public void RefreshPlayerIgnorePlatform()
    {
        foreach (var player in players)
            player.ClearIgnorePlatform();
    }

    public void CharacterChange(bool changeAttack = true)
    {
        var pastPlayer = curPlayer;
        var changePos = curPlayer.transform.position;
        var nextPlayerId = saveData.playerList[1];
        pastPlayer.AllBuffCancel();

        curPlayer = GetPlayer(nextPlayerId);
        // 교체 시 유지해야하는 데이터 받아오기
        curPlayer.ReceiveChangeData(pastPlayer);
        ActivePlayer(nextPlayerId);

        curPlayer.transform.position = changePos;
        curPlayer.transform.localScale = pastPlayer.transform.localScale;
        curPlayer.JumpAttackCount = 0;
        
        RotatePlayerList();
        RefreshFace();
        RefreshPlayerResource();
        curPlayer.ChangeApplyPassive();

        if (changeAttack)
            curPlayer.ChangeAttack();
        
        RefreshSkill();
        SetCameraTarget(curPlayer.transform);
    }

    public void SetCharacterOrder()
    {
        var pastPlayer = curPlayer;
        var changePos = curPlayer.transform.position;
        
        ActivePlayer(PlayerList[0]);
        curPlayer.transform.position = changePos;
        curPlayer.transform.localScale = pastPlayer.transform.localScale;
        
        RefreshSkill();
        SetCameraTarget(curPlayer.transform);
    }

    // 대화
    private Character GetCharacter(string characterId, Npc[] npc)
    {
        Character character = null;
        foreach (var player in players)
        {
            if (player.name == characterId)
            {
                character = player;
                break;
            }
        }
        foreach (var targetNpc in npc)
        {
            if (targetNpc.name == characterId)
            {
                character = targetNpc;
                break;
            }
        }

        if(character == null)
            Debug.Log($"{characterId}가 존재하지 않는다");
        
        return character;
    }

    public async void PlayerRespawn()
    {
        // 함정 피해를 입은 그 프레임에 즉시 완전 무적 — ignoreImmortal인 함정도 막아 2회 피격 방지
        curPlayer.TrapRespawning = true;
        curPlayer.Immortal = true;
        ControlStart = false;

        float delay1 = 0.3f;
        float delay2 = 0.1f;

        InitProductCancellation();
        if(await NormalDelay(delay1, productCancellation).SuppressCancellationThrow())
            return;
        
        curPlayer.SpawnObject(ConstValues.BangEffect, curPlayer.CenterPos.position);
        curPlayer.gameObject.SetActive(false);
        if(await NormalDelay(delay1, productCancellation).SuppressCancellationThrow())
            return;
        
        // 이동기능 추가
        curPlayer.transform.position = curPlayer.GetLastMarkerPosition();
        if(await NormalDelay(delay1, productCancellation).SuppressCancellationThrow())
            return;
        
        curPlayer.SpawnObject(ConstValues.BangEffect, curPlayer.CenterPos.position);
        curPlayer.gameObject.SetActive(true);
        
        if(await NormalDelay(delay2, productCancellation).SuppressCancellationThrow())
            return;
        
        curPlayer.Immortal = false;
        curPlayer.Dodge = false;
        curPlayer.TrapRespawning = false;
        ControlStart = true;
    }
}
