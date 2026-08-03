// 룸 진행 상태 데이터 (연출 · 숏컷 · 미니맵 · 오브젝트)

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
// 연출 이벤트
public class RoomProduct
{
    public int idx;
    public int count;
    public bool isFinish;
}

[Serializable]
// 이벤트를 처리해야하는 Npc
public class EventNpc
{
    public string id;
    public bool isActive;
}

[Serializable]
// 상황에 따라 껐다 켰다 하는 오브젝트
public class EventCustomObject
{
    public string id;
    public bool isActive;
}

[Serializable]
// 숏컷
public class ShortCut
{
    public string id;
    public string type;
    public bool isOpened;
}

[Serializable]
// 방의 오브젝트
public class RoomObjectClass
{
    public string id;
    public int count;
    public bool alreadyGet;
}

[Serializable]
// 엘리베이터
public class ElevatorData
{
    public string id;
    public int idx;
    public int startIdx;   // 게임오버 리셋 시 돌아갈 최초 시작 인덱스
}

[Serializable]
// 잠긴 문
public class LockDoorData
{
    public string id;
    public bool isOpen;
}

// 미니맵에 표시되는(발견 시 공개되는) 오브젝트 종류. 새 오브젝트가 생기면 값만 추가한다.
public enum EMinimapObjectType
{
    SavePoint,          // 세이브 포인트
    Portal,             // 포탈
    Merchant,           // 상인
    AttributePoint,     // 특성 포인트
    Potion,             // 포션
}

[Serializable]
public class RoomInfo
{
    public string roomId;

    public string visitedFrameCells;                                 // 방문한 구역 테두리
    public string visitedInCells;                                    // 방문한 구역 내부
    public List<string> visitedShortcutCells = new List<string>();   // 방문한 숏컷

    public List<bool> hiddenAreaDiscovered = new List<bool>();       // 숨겨진 구역 발견 여부
    public List<string> visitedHiddenCells = new List<string>();     // 발견 후 카메라 시야로 공개된 숨겨진 구역 셀

    // 발견(공개)된 미니맵 오브젝트 종류 집합. 종류가 늘어도 이 리스트 하나로 관리한다.
    public List<EMinimapObjectType> revealedMinimapObjects = new List<EMinimapObjectType>();

    // 해당 미니맵 오브젝트가 발견되었는지 여부
    public bool IsRevealed(EMinimapObjectType type) => revealedMinimapObjects.Contains(type);

    // 미니맵 오브젝트를 발견 처리(중복 추가 방지)
    public void Reveal(EMinimapObjectType type)
    {
        if (!revealedMinimapObjects.Contains(type))
            revealedMinimapObjects.Add(type);
    }

    public List<RoomProduct> roomProduct = new List<RoomProduct>();
    public List<EventNpc> eventNpc = new List<EventNpc>();
    public List<EventCustomObject> customObject = new List<EventCustomObject>();
    public List<ShortCut> shortCut = new List<ShortCut>();
    
    public List<RoomObjectClass> skillAndPassive = new List<RoomObjectClass>();
    public List<RoomObjectClass> treasureBox = new List<RoomObjectClass>();
    public List<RoomObjectClass> item = new List<RoomObjectClass>();
    public List<RoomObjectClass> attributePoint = new List<RoomObjectClass>();
    public List<RoomObjectClass> relic = new List<RoomObjectClass>();
    public List<RoomObjectClass> potion = new List<RoomObjectClass>();
    
    public List<ElevatorData> elevators = new List<ElevatorData>();
    public List<LockDoorData> lockDoors = new List<LockDoorData>();
}
