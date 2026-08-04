// 미니맵 공개 판정 테스트.
//
// "카메라에 조금이라도 걸치면 공개한다"는 판정은 원래 일곱 군데에 복사돼 있었다.
// 한 곳으로 모았으니 경계 조건을 여기서 못 박아 둔다.
// 경계에 정확히 닿은 경우를 공개로 볼지 아닐지는 눈에 보이는 차이라서 특히 중요하다.

using NUnit.Framework;
using UnityEngine;

public class RoomMinimapOverlapTests
{
    // 원점 중심 10x10 화면
    private static readonly Rect View = new Rect(-5f, -5f, 10f, 10f);
    private static readonly Vector2 Half = new Vector2(1f, 1f);

    [Test]
    public void 화면_한가운데는_공개된다()
    {
        Assert.IsTrue(RoomMinimap.Overlaps(View, Vector2.zero, Half));
    }

    [Test]
    public void 완전히_벗어나면_공개되지_않는다()
    {
        Assert.IsFalse(RoomMinimap.Overlaps(View, new Vector2(20f, 0f), Half));
        Assert.IsFalse(RoomMinimap.Overlaps(View, new Vector2(0f, -20f), Half));
    }

    [Test]
    public void 모서리만_걸쳐도_공개된다()
    {
        // 중심은 화면 밖이지만 반지름 1 이라 왼쪽 경계에 걸친다
        Assert.IsTrue(RoomMinimap.Overlaps(View, new Vector2(-5.5f, 0f), Half));
        Assert.IsTrue(RoomMinimap.Overlaps(View, new Vector2(0f, 5.5f), Half));
    }

    [Test]
    public void 경계에_정확히_닿으면_공개된다()
    {
        // 오른쪽 끝에 정확히 접함: 판정이 >= 라서 공개된다
        Assert.IsTrue(RoomMinimap.Overlaps(View, new Vector2(6f, 0f), Half));
    }

    [Test]
    public void 경계에서_한_칸_더_나가면_공개되지_않는다()
    {
        Assert.IsFalse(RoomMinimap.Overlaps(View, new Vector2(6.01f, 0f), Half));
    }

    [Test]
    public void 대각선으로만_벗어나면_공개되지_않는다()
    {
        // x 는 걸치지만 y 가 완전히 벗어난 경우 — 축마다 따로 봐야 한다
        Assert.IsFalse(RoomMinimap.Overlaps(View, new Vector2(0f, 10f), Half));
    }

    [Test]
    public void 반지름이_0_이면_점으로_판정한다()
    {
        Assert.IsTrue(RoomMinimap.Overlaps(View, Vector2.zero, Vector2.zero));
        Assert.IsFalse(RoomMinimap.Overlaps(View, new Vector2(5.1f, 0f), Vector2.zero));
    }
}
