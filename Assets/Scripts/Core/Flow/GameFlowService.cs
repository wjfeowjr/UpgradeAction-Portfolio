// 시간 정지 · 입력 잠금 관리
//
// 문제
//   팝업이 열릴 때 Time.timeScale = 0, 닫힐 때 = 1 을 직접 넣고 있었다.
//   팝업 하나만 뜰 때는 맞지만, 팝업 위에 팝업이 뜨면 어긋난다.
//
//     ① 특성 팝업 열림        timeScale = 0
//     ② 구매 확인 팝업 열림    timeScale = 0
//     ③ 확인 팝업 닫힘        timeScale = 1   <- 특성 팝업이 아직 열려 있는데 게임이 돌아간다
//
//   실제로 특성 팝업에서 구매를 누르면 확인 팝업이 뜨므로 도달 가능한 경로였다.
//   입력 잠금(ControlStart)도 같은 구조였다.
//
// 해결
//   "누가 멈춰달라고 했는지"를 집합으로 들고 있는다.
//   요청자가 하나라도 남아 있으면 멈춘 상태를 유지하고,
//   마지막 요청자가 풀렸을 때만 원래대로 돌린다.
//
//   중첩 깊이가 아니라 '요청자 신원'으로 세는 이유는,
//   같은 팝업이 실수로 두 번 풀어도 카운트가 음수로 내려가지 않게 하기 위해서다.
//
// 슬로우모션(저스트 카운터 0.05, 아레나 0.2)은 정지와 별개로 다룬다.
// 정지 중에는 0 이 우선하고, 정지가 풀리면 슬로우모션 값으로 돌아간다.

using System.Collections.Generic;
using UnityEngine;

public class GameFlowService
{
    // 시간을 멈춰달라고 요청한 대상들
    private readonly HashSet<object> timeHolders = new HashSet<object>();
    // 입력을 잠가달라고 요청한 대상들
    private readonly HashSet<object> inputHolders = new HashSet<object>();

    // 정지가 아닐 때의 기본 배속. 슬로우모션 연출이 이 값을 바꾼다.
    private float baseTimeScale = 1f;

    public bool IsTimeStopped => timeHolders.Count > 0;
    public bool IsInputLocked => inputHolders.Count > 0;

    public int TimeHolderCount => timeHolders.Count;
    public int InputHolderCount => inputHolders.Count;

    /// <summary>정지가 아닐 때 적용될 배속. 슬로우모션 연출용.</summary>
    public float BaseTimeScale
    {
        get => baseTimeScale;
        set
        {
            baseTimeScale = Mathf.Max(0.01f, value);
            Apply();
        }
    }

    /// <summary>owner 가 시간 정지를 요청한다. 이미 요청했다면 무시된다.</summary>
    public void StopTime(object owner)
    {
        if (owner == null || !timeHolders.Add(owner))
            return;
        Apply();
    }

    /// <summary>owner 의 정지 요청을 푼다. 다른 요청자가 남아 있으면 멈춘 상태가 유지된다.</summary>
    public void ResumeTime(object owner)
    {
        if (owner == null || !timeHolders.Remove(owner))
            return;
        Apply();
    }

    public void LockInput(object owner)
    {
        if (owner == null)
            return;
        inputHolders.Add(owner);
    }

    public void UnlockInput(object owner)
    {
        if (owner == null)
            return;
        inputHolders.Remove(owner);
    }

    /// <summary>
    /// 씬 전환이나 게임오버처럼 상태를 통째로 되돌려야 할 때 쓴다.
    /// 요청자가 파괴되어 해제를 못 부른 경우에도 여기서 정리된다.
    /// </summary>
    public void ClearAll()
    {
        timeHolders.Clear();
        inputHolders.Clear();
        baseTimeScale = 1f;
        Apply();
    }

    /// <summary>파괴된 오브젝트가 남긴 요청을 정리한다.</summary>
    public void RemoveDestroyed()
    {
        timeHolders.RemoveWhere(IsDestroyed);
        inputHolders.RemoveWhere(IsDestroyed);
        Apply();
    }

    private static bool IsDestroyed(object owner)
    {
        // Unity 오브젝트는 파괴되면 == null 이 true 가 된다
        return owner is Object unityObject && unityObject == null;
    }

    private void Apply()
    {
        Time.timeScale = timeHolders.Count > 0 ? 0f : baseTimeScale;
    }
}
