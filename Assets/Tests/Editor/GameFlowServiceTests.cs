// GameFlowService 단위 테스트
//
// 핵심은 '팝업 위에 팝업' 상황이다.
// 이전 구현은 닫히는 쪽이 무조건 timeScale = 1 을 넣어서,
// 아래 팝업이 아직 열려 있는데도 게임이 다시 돌아갔다.

using NUnit.Framework;
using UnityEngine;

public class GameFlowServiceTests
{
    private GameFlowService flow;
    private float originalScale;

    [SetUp]
    public void SetUp()
    {
        originalScale = Time.timeScale;
        flow = new GameFlowService();
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = originalScale;
    }

    private static readonly object PopupA = "PopupA";
    private static readonly object PopupB = "PopupB";

    [Test]
    public void 정지_요청이_있으면_시간이_멈춘다()
    {
        flow.StopTime(PopupA);

        Assert.IsTrue(flow.IsTimeStopped);
        Assert.AreEqual(0f, Time.timeScale);
    }

    [Test]
    public void 마지막_요청자가_풀려야_시간이_돌아온다()
    {
        // 팝업 위에 팝업이 뜬 상황
        flow.StopTime(PopupA);
        flow.StopTime(PopupB);

        // 위쪽 팝업만 닫힘
        flow.ResumeTime(PopupB);

        Assert.IsTrue(flow.IsTimeStopped, "아래 팝업이 남아 있으면 계속 멈춰 있어야 한다");
        Assert.AreEqual(0f, Time.timeScale);

        // 아래 팝업까지 닫힘
        flow.ResumeTime(PopupA);

        Assert.IsFalse(flow.IsTimeStopped);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void 같은_요청자가_두_번_풀어도_어긋나지_않는다()
    {
        flow.StopTime(PopupA);
        flow.StopTime(PopupB);

        flow.ResumeTime(PopupB);
        flow.ResumeTime(PopupB);   // 실수로 두 번 호출

        Assert.IsTrue(flow.IsTimeStopped, "중복 해제가 다른 요청자의 정지를 풀면 안 된다");
        Assert.AreEqual(1, flow.TimeHolderCount);
    }

    [Test]
    public void 같은_요청자가_두_번_요청해도_한_번으로_센다()
    {
        flow.StopTime(PopupA);
        flow.StopTime(PopupA);

        Assert.AreEqual(1, flow.TimeHolderCount);

        flow.ResumeTime(PopupA);
        Assert.IsFalse(flow.IsTimeStopped);
    }

    [Test]
    public void 요청하지_않은_대상이_풀어도_영향이_없다()
    {
        flow.StopTime(PopupA);
        flow.ResumeTime(PopupB);   // 요청한 적 없음

        Assert.IsTrue(flow.IsTimeStopped);
        Assert.AreEqual(1, flow.TimeHolderCount);
    }

    [Test]
    public void 입력_잠금도_같은_규칙을_따른다()
    {
        flow.LockInput(PopupA);
        flow.LockInput(PopupB);

        flow.UnlockInput(PopupB);
        Assert.IsTrue(flow.IsInputLocked, "아래 팝업이 남아 있으면 계속 잠겨 있어야 한다");

        flow.UnlockInput(PopupA);
        Assert.IsFalse(flow.IsInputLocked);
    }

    [Test]
    public void 슬로우모션은_정지가_풀린_뒤에_적용된다()
    {
        flow.BaseTimeScale = 0.05f;          // 저스트 카운터 연출
        Assert.AreEqual(0.05f, Time.timeScale, 0.0001f);

        flow.StopTime(PopupA);               // 그 위에 팝업이 뜨면
        Assert.AreEqual(0f, Time.timeScale, "정지가 슬로우모션보다 우선한다");

        flow.ResumeTime(PopupA);             // 팝업이 닫히면
        Assert.AreEqual(0.05f, Time.timeScale, 0.0001f, "슬로우모션 값으로 돌아와야 한다");

        flow.BaseTimeScale = 1f;
        Assert.AreEqual(1f, Time.timeScale, 0.0001f);
    }

    [Test]
    public void ClearAll_은_모든_요청을_비우고_원래대로_돌린다()
    {
        flow.StopTime(PopupA);
        flow.StopTime(PopupB);
        flow.LockInput(PopupA);
        flow.BaseTimeScale = 0.2f;

        flow.ClearAll();

        Assert.IsFalse(flow.IsTimeStopped);
        Assert.IsFalse(flow.IsInputLocked);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void 파괴된_요청자는_정리된다()
    {
        // 팝업이 해제를 못 부르고 파괴되면 영원히 멈춰 있게 된다
        var go = new GameObject("DestroyedPopup");
        flow.StopTime(go);
        Assert.IsTrue(flow.IsTimeStopped);

        Object.DestroyImmediate(go);
        flow.RemoveDestroyed();

        Assert.IsFalse(flow.IsTimeStopped, "파괴된 요청자는 정리되어야 한다");
        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void 배속은_0_이하로_내려가지_않는다()
    {
        flow.BaseTimeScale = 0f;
        Assert.Greater(Time.timeScale, 0f, "0 을 넣으면 정지와 구분되지 않는다");

        flow.BaseTimeScale = -1f;
        Assert.Greater(Time.timeScale, 0f);
    }
}
