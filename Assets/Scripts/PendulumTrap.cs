using UnityEngine;

public class PendulumTrap : MonoBehaviour
{
    [SerializeField] float amplitude = 60f;     // 최대 각도(±60)
    [SerializeField] float speed = 2f;          // 흔들리는 속도
    [SerializeField] float activateOffset = 10f; // 정점에서 이만큼 내려오면 콜라이더 활성화
    [SerializeField] bool startToRight = true;  // 시작 시 오른쪽으로 휘두를지(false면 왼쪽)
    [SerializeField] float startAngle = 0f;     // 시작 각도(-amplitude ~ amplitude)
    private Attack attack;

    // 현재 스윙 방향: 1(오른쪽), -1(왼쪽), 0(초기값)
    private int swingDir;
    // 이번 스윙(정점 이후)에서 콜라이더를 다시 켰는지 여부
    private bool activated;

    // 시작 각도/방향으로부터 계산한 위상 오프셋
    private float phaseOffset;
    // 객체 시작 기준 경과 시간(시작 각도를 정확히 맞추기 위해 Time.time 대신 사용)
    private float timer;
    private bool initialized;

    void Update()
    {
        if (attack == null)
            attack = GetComponent<Attack>();

        if (!initialized)
        {
            // 시작 각도 → 위상으로 변환. startToRight에 따라 올라가는/내려가는 가지를 선택
            float ratio = amplitude != 0f ? Mathf.Clamp(startAngle / amplitude, -1f, 1f) : 0f;
            float baseOffset = Mathf.Asin(ratio); // [-π/2, π/2] : 각도가 커지는(오른쪽) 가지
            phaseOffset = startToRight ? baseOffset : Mathf.PI - baseOffset;
            initialized = true;
        }

        float phase = timer * speed + phaseOffset;

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
            if (attack)
                attack.ColliderActive(true);
            activated = true;
        }

        timer += Time.deltaTime;
    }
}
