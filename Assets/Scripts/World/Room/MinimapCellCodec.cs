// 미니맵 방문 셀의 저장 형식.
//
// 세이브 파일에는 "x_y_z;x_y_z;..." 한 줄로 들어간다.
// JsonUtility 가 Vector3Int 리스트를 다루지 못해서 문자열로 눌러 담는 방식을 쓰는데,
// 같은 인코딩/디코딩 코드가 테두리·내부·숏컷·숨겨진 구역 네 곳에 복사돼 있었다.
// 형식을 한 번만 정의해두면 네 곳이 어긋날 일이 없고, MonoBehaviour 밖이라 테스트도 붙는다.
//
// 잘못된 항목은 건너뛴다. 세이브 파일이 깨졌을 때 방 하나가 통째로 안 열리는 것보다,
// 복원 가능한 셀만 살리고 나머지를 다시 탐색하게 두는 편이 낫다.

using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class MinimapCellCodec
{
    private const char CellSeparator = ';';
    private const char AxisSeparator = '_';

    /// <summary>셀 목록을 "x_y_z;" 형식 한 줄로 만든다.</summary>
    public static string Encode(IEnumerable<Vector3Int> cells)
    {
        if (cells == null)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var c in cells)
        {
            sb.Append(c.x).Append(AxisSeparator)
              .Append(c.y).Append(AxisSeparator)
              .Append(c.z).Append(CellSeparator);
        }
        return sb.ToString();
    }

    /// <summary>
    /// "x_y_z;" 형식을 읽어 <paramref name="into"/> 뒤에 덧붙인다.
    /// 비우지 않는 이유는 기존 호출부가 전부 빈 리스트에 이어 붙이는 방식이었기 때문이다.
    /// </summary>
    public static void Decode(string text, List<Vector3Int> into)
    {
        if (into == null || string.IsNullOrEmpty(text))
            return;

        var entries = text.Split(new[] { CellSeparator }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var entry in entries)
        {
            if (TryParseCell(entry, out var cell))
                into.Add(cell);
        }
    }

    /// <summary>"x_y_z" 한 칸을 읽는다. 축이 3개가 아니거나 정수가 아니면 false.</summary>
    public static bool TryParseCell(string entry, out Vector3Int cell)
    {
        cell = default;
        if (string.IsNullOrEmpty(entry))
            return false;

        var axis = entry.Split(AxisSeparator);
        if (axis.Length != 3)
            return false;

        if (!int.TryParse(axis[0], out int x) ||
            !int.TryParse(axis[1], out int y) ||
            !int.TryParse(axis[2], out int z))
            return false;

        cell = new Vector3Int(x, y, z);
        return true;
    }
}
