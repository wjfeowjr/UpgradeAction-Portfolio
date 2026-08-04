// 테이블 데이터 파싱 유틸
//
// JSON 테이블의 수치는 "1.5;3.0" 같은 문자열로 들어온다.
// 이걸 파싱할 때 두 가지 문제가 있었다.
//
// 1) 로케일 의존
//    float.Parse("1.5") 는 현재 문화권 설정을 따른다.
//    소수점이 쉼표인 지역(독일·프랑스·러시아·스페인 등)에서는
//    파싱에 실패하거나 15 로 해석된다.
//    이 게임은 8개 언어를 지원하므로 실제로 터질 수 있는 경로다.
//    -> CultureInfo.InvariantCulture 를 고정한다.

using System.Globalization;
using UnityEngine;

public static class TableParse
{
    private const NumberStyles FloatStyle = NumberStyles.Float | NumberStyles.AllowThousands;
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <summary>테이블 문자열을 float 로 읽는다. 실패하면 경고 후 기본값.</summary>
    public static float Float(string text, float fallback = 0f, string context = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        if (float.TryParse(text.Trim(), FloatStyle, Culture, out var value))
            return value;

        Warn(text, "float", context, fallback);
        return fallback;
    }

    /// <summary>테이블 문자열을 int 로 읽는다. 실패하면 경고 후 기본값.</summary>
    public static int Int(string text, int fallback = 0, string context = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        if (int.TryParse(text.Trim(), NumberStyles.Integer, Culture, out var value))
            return value;

        // "1.0" 처럼 소수로 적힌 정수도 받아준다
        if (float.TryParse(text.Trim(), FloatStyle, Culture, out var f))
            return Mathf.RoundToInt(f);

        Warn(text, "int", context, fallback);
        return fallback;
    }

    /// <summary>테이블 문자열을 bool 로 읽는다. "1"/"0" 표기도 받는다.</summary>
    public static bool Bool(string text, bool fallback = false, string context = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        var t = text.Trim();
        if (bool.TryParse(t, out var value))
            return value;
        if (t == "1") return true;
        if (t == "0") return false;

        Warn(text, "bool", context, fallback);
        return fallback;
    }

    /// <summary>
    /// 테이블 문자열을 enum 으로 읽는다.
    /// Enum.Parse 는 없는 값이면 예외를 던지므로 TryParse 를 쓴다.
    /// </summary>
    public static T Enum<T>(string text, T fallback = default, string context = null) where T : struct
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        if (System.Enum.TryParse<T>(text.Trim(), true, out var value))
            return value;

        Warn(text, typeof(T).Name, context, fallback);
        return fallback;
    }

    /// <summary>"1.5;3.0" 처럼 구분자로 이어진 값을 Vector2 로 읽는다.</summary>
    public static Vector2 Vector2(string text, char separator = ';', string context = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return UnityEngine.Vector2.zero;

        var parts = text.Split(separator);
        if (parts.Length < 2)
        {
            Warn(text, "Vector2", context, UnityEngine.Vector2.zero);
            return UnityEngine.Vector2.zero;
        }

        return new Vector2(Float(parts[0], 0f, context), Float(parts[1], 0f, context));
    }

    private static void Warn(string text, string type, string context, object fallback)
    {
        var where = string.IsNullOrEmpty(context) ? "" : $"[{context}] ";
        Debug.LogWarning($"[Table] {where}\"{text}\" 를 {type} 로 읽지 못했습니다. {fallback} 으로 대체합니다");
    }
}
