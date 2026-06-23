using UnityEngine;

public class PendulumTrap : MonoBehaviour
{
    [SerializeField] float amplitude = 60f;     // 최대 각도(±60)
    [SerializeField] float speed = 2f;          // 흔들리는 속도
    [SerializeField] float activateOffset = 10f; // 정점에서 이만큼 내려오면 콜라이더 활성화
    private Attack attack;

    // 현재 스윙 방향: 1(오른쪽), -1(왼쪽), 0(초기값)
    private int swingDir;
    // 이번 스윙(정점 이후)에서 콜라이더를 다시 켰는지 여부
    private bool activated;

    void Update()
    {
        if(attack == null)
            attack = GetComponent<Attack>();
        
        float phase = Time.time * speed;

        // Sin 기반 좌우 왕복 회전
        float z = amplitude * Mathf.Sin(phase);
        transform.localRotation = Quaternion.Euler(0, 0, z);

        // 각속도 부호(cos)로 현재 휘두르는 방향 판정
        // cos >= 0 : 각도가 커지는 중(오른쪽으로 휘두름), cos < 0 : 왼쪽으로 휘두름
        int newDir = Mathf.Cos(phase) >= 0f ? 1 : -1;

        // 정점(최대 각도)에 도달해 방향이 바뀌는 순간: 콜라이더를 끄고 재무장
        if (newDir != swingDir)
        {
            swingDir = newDir;
            if (attack)
            {
                attack.ColliderActive(false); // false → 내부에서 TargetColReset() 호출(누적 타겟 초기화)
                attack.DirChange(newDir);     // 휘두르는 방향에 맞춰 넉백 방향 변경
            }
            activated = false;            // 아직 이번 스윙에서 재활성화 안 함
        }

        // 정점에서 activateOffset(기본 10도) 이상 내려오면 콜라이더 활성화 (스윙당 1회)
        // 예) amplitude 60 → 50도에서 활성화, amplitude 60의 반대편(-60) → -50도에서 활성화
        if (!activated && Mathf.Abs(z) <= amplitude - activateOffset)
        {
            if(attack)
                attack.ColliderActive(true);
            activated = true;
        }
    }
}
