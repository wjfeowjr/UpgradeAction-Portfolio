// 미니맵 방문 셀 저장 형식 테스트.
//
// 이 형식은 세이브 파일에 직접 들어간다.
// 형식이 바뀌면 이미 저장된 파일의 미니맵이 통째로 날아가므로,
// 왕복(저장 -> 로드)이 원본과 같은지가 가장 중요하다.

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MinimapCellCodecTests
{
    [Test]
    public void 저장하고_불러오면_원본과_같다()
    {
        var original = new List<Vector3Int>
        {
            new Vector3Int(0, 0, 0),
            new Vector3Int(3, -7, 0),
            new Vector3Int(-12, 45, 2),
        };

        var restored = new List<Vector3Int>();
        MinimapCellCodec.Decode(MinimapCellCodec.Encode(original), restored);

        CollectionAssert.AreEqual(original, restored);
    }

    [Test]
    public void 음수_좌표도_왕복한다()
    {
        // 미니맵 셀 좌표는 방 원점 기준이라 음수가 흔하다
        var original = new List<Vector3Int> { new Vector3Int(-1, -1, -1) };

        var restored = new List<Vector3Int>();
        MinimapCellCodec.Decode(MinimapCellCodec.Encode(original), restored);

        Assert.AreEqual(original[0], restored[0]);
    }

    [Test]
    public void 빈_목록은_빈_문자열이_된다()
    {
        Assert.AreEqual(string.Empty, MinimapCellCodec.Encode(new List<Vector3Int>()));
    }

    [Test]
    public void 빈_문자열을_읽으면_아무것도_추가하지_않는다()
    {
        var cells = new List<Vector3Int>();

        MinimapCellCodec.Decode(null, cells);
        MinimapCellCodec.Decode(string.Empty, cells);

        Assert.AreEqual(0, cells.Count);
    }

    [Test]
    public void 깨진_항목은_건너뛰고_나머지는_살린다()
    {
        // 세이브 파일이 손상돼도 방 하나가 통째로 안 열리는 것보다,
        // 복원 가능한 셀만 살리고 나머지를 다시 탐색하게 두는 편이 낫다
        var cells = new List<Vector3Int>();

        MinimapCellCodec.Decode("1_2_3;깨짐;4_5;9_9_9_9;7_8_9;", cells);

        CollectionAssert.AreEqual(
            new List<Vector3Int> { new Vector3Int(1, 2, 3), new Vector3Int(7, 8, 9) },
            cells);
    }

    [Test]
    public void 기존_목록_뒤에_덧붙인다()
    {
        // 호출부가 전부 "빈 리스트에 이어 붙이는" 방식이라 비우지 않는다
        var cells = new List<Vector3Int> { new Vector3Int(1, 1, 1) };

        MinimapCellCodec.Decode("2_2_2;", cells);

        Assert.AreEqual(2, cells.Count);
        Assert.AreEqual(new Vector3Int(1, 1, 1), cells[0]);
        Assert.AreEqual(new Vector3Int(2, 2, 2), cells[1]);
    }

    [Test]
    public void 축이_세_개가_아니면_실패한다()
    {
        Assert.IsFalse(MinimapCellCodec.TryParseCell("1_2", out _));
        Assert.IsFalse(MinimapCellCodec.TryParseCell("1_2_3_4", out _));
        Assert.IsFalse(MinimapCellCodec.TryParseCell("", out _));
        Assert.IsFalse(MinimapCellCodec.TryParseCell(null, out _));
    }

    [Test]
    public void 정수가_아니면_실패한다()
    {
        Assert.IsFalse(MinimapCellCodec.TryParseCell("1_x_3", out _));
        Assert.IsFalse(MinimapCellCodec.TryParseCell("1.5_2_3", out _));
    }

    [Test]
    public void 셀이_많아도_순서가_유지된다()
    {
        var original = new List<Vector3Int>();
        for (int i = 0; i < 500; i++)
            original.Add(new Vector3Int(i, -i, 0));

        var restored = new List<Vector3Int>();
        MinimapCellCodec.Decode(MinimapCellCodec.Encode(original), restored);

        CollectionAssert.AreEqual(original, restored);
    }
}
