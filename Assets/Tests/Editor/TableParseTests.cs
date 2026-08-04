// TableParse 단위 테스트
//
// 가장 중요한 검증은 '소수점이 쉼표인 지역에서도 같은 결과가 나오는가' 다.
// 기존 float.Parse 는 현재 문화권을 따라가서, 독일·프랑스 로케일 PC 에서
// "1.5" 가 파싱에 실패하거나 15 로 읽혔다.

using System.Globalization;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

public class TableParseTests
{
    private CultureInfo original;

    [SetUp]
    public void SetUp()
    {
        original = Thread.CurrentThread.CurrentCulture;
    }

    [TearDown]
    public void TearDown()
    {
        Thread.CurrentThread.CurrentCulture = original;
    }

    [Test]
    public void 소수점이_쉼표인_지역에서도_같은_값을_읽는다()
    {
        // 독일어권: 소수 구분자가 ',' 이고 '.' 는 천 단위 구분자다.
        // 이 설정에서 float.Parse("1.5") 는 15 가 되거나 예외가 난다.
        Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

        Assert.AreEqual(1.5f, TableParse.Float("1.5"), 0.0001f);
        Assert.AreEqual(0.15f, TableParse.Float("0.15"), 0.0001f);
        Assert.AreEqual(new Vector2(7.5f, 6f), TableParse.Vector2("7.5;6"));
    }

    [Test]
    public void 한국어_영어_지역에서도_동일하다()
    {
        foreach (var name in new[] { "ko-KR", "en-US", "fr-FR", "ru-RU" })
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(name);
            Assert.AreEqual(1.5f, TableParse.Float("1.5"), 0.0001f, $"{name} 에서 값이 다르다");
        }
    }

    [Test]
    public void 잘못된_값은_예외_대신_기본값을_준다()
    {
        // 기존 구현은 FormatException 으로 게임이 멈췄다
        Assert.AreEqual(0f, TableParse.Float("숫자아님"));
        Assert.AreEqual(3f, TableParse.Float("오타", 3f));
        Assert.AreEqual(0, TableParse.Int("x"));
        Assert.AreEqual(-1, TableParse.Int("x", -1));
    }

    [Test]
    public void 빈_값과_null_은_기본값이다()
    {
        Assert.AreEqual(0f, TableParse.Float(null));
        Assert.AreEqual(0f, TableParse.Float(""));
        Assert.AreEqual(0f, TableParse.Float("   "));
        Assert.AreEqual(5f, TableParse.Float(null, 5f));
    }

    [Test]
    public void 앞뒤_공백은_무시한다()
    {
        // 시트에서 복사할 때 공백이 섞여 들어오는 경우가 많다
        Assert.AreEqual(1.5f, TableParse.Float("  1.5  "), 0.0001f);
        Assert.AreEqual(10, TableParse.Int(" 10 "));
        Assert.AreEqual(new Vector2(3f, 10f), TableParse.Vector2("3; 10"));
    }

    [Test]
    public void 소수로_적힌_정수도_읽는다()
    {
        // 시트에서 1 을 1.0 으로 적어두는 경우가 있다
        Assert.AreEqual(1, TableParse.Int("1.0"));
        Assert.AreEqual(2, TableParse.Int("1.6"));   // 반올림
    }

    [Test]
    public void Bool_은_true_false_와_1_0_을_모두_받는다()
    {
        Assert.IsTrue(TableParse.Bool("true"));
        Assert.IsTrue(TableParse.Bool("True"));
        Assert.IsTrue(TableParse.Bool("1"));
        Assert.IsFalse(TableParse.Bool("false"));
        Assert.IsFalse(TableParse.Bool("0"));
        Assert.IsFalse(TableParse.Bool("아무말"));
    }

    [Test]
    public void 없는_enum_값은_예외_대신_기본값이다()
    {
        // Enum.Parse 는 없는 값이면 ArgumentException 을 던진다
        Assert.AreEqual(EBodyType.SuperArmor, TableParse.Enum("SuperArmor", EBodyType.Normal));
        Assert.AreEqual(EBodyType.Normal, TableParse.Enum("없는타입", EBodyType.Normal));
        Assert.AreEqual(EBodyType.Normal, TableParse.Enum<EBodyType>(null));
    }

    [Test]
    public void enum_은_대소문자를_가리지_않는다()
    {
        // 시트 표기가 흔들려도 읽히게 한다
        Assert.AreEqual(EBodyType.SuperArmor, TableParse.Enum("superarmor", EBodyType.Normal));
    }

    [Test]
    public void 구분자가_모자라면_기본값이다()
    {
        Assert.AreEqual(UnityEngine.Vector2.zero, TableParse.Vector2("5"));
        Assert.AreEqual(UnityEngine.Vector2.zero, TableParse.Vector2(""));
    }
}
