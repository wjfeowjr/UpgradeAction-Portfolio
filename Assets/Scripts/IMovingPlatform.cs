using UnityEngine;

// 플레이어가 올라타 같이 움직일 수 있는 발판 공통 인터페이스.
// MovingPlatform, Elevator 등이 구현한다.
public interface IMovingPlatform
{
    Vector2 Velocity { get; }            // 발판 현재 속도 (탑승 동기화용)
    PlatformObject PlatformObject { get; } // 발판 콜라이더/높이 정보
}
