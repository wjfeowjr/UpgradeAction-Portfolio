// LocalizationService 단위 테스트
//
// MonoBehaviour 도 싱글턴도 아니므로 Unity 런타임 없이 EditMode 에서 바로 돌아간다.
// GameManager 안에 있을 때는 불가능했던 검증이다.

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class LocalizationServiceTests
{
    private static TalkDataList MakeTalkTable()
    {
        return new TalkDataList
        {
            Talk = new List<TalkData>
            {
                new TalkData { idx = 10000, kr = "망할 모험", en = "Damn Adventure", ja = "クソったれな冒険",
                               cn = "该死的冒险", tw = "該死的冒險", es = "Una Maldita Aventura",
                               ru = "Чёртово приключение", pt = "Uma Maldita Aventura" },
                new TalkData { idx = 50000, kr = "광전사", en = "Berserker" },
                new TalkData { idx = 50001, kr = "거너",   en = "Gunner" },
                new TalkData { idx = 50002, kr = "파이터", en = "Fighter" },
                new TalkData { idx = 130000, kr = "숲",    en = "Forest" },
            }
        };
    }

    private static ItemDataList MakeItemTable()
    {
        return new ItemDataList
        {
            Item = new List<ItemData>
            {
                new ItemData { id = "Potion", name = 10000, explain = 50000 },
            }
        };
    }

    private static LocalizationService Make()
        => new LocalizationService(MakeTalkTable(), MakeItemTable());

    [Test]
    public void 언어에_따라_같은_idx가_다른_문자열을_반환한다()
    {
        var sut = Make();

        Assert.AreEqual("망할 모험", sut.GetTalk(10000, ConstValues.Korean));
        Assert.AreEqual("Damn Adventure", sut.GetTalk(10000, ConstValues.English));
        Assert.AreEqual("クソったれな冒険", sut.GetTalk(10000, ConstValues.Japanese));
        Assert.AreEqual("該死的冒險", sut.GetTalk(10000, ConstValues.ChineseTraditional));
    }

    [Test]
    public void 없는_idx는_예외_대신_null을_반환한다()
    {
        var sut = Make();

        // 기존 구현은 Find 가 null 을 반환해 NullReferenceException 이 났다
        Assert.IsNull(sut.GetTalk(999999, ConstValues.Korean));
    }

    [Test]
    public void 지원하지_않는_언어는_null을_반환한다()
    {
        var sut = Make();
        Assert.IsNull(sut.GetTalk(10000, "Klingon"));
    }

    [Test]
    public void 직업_id로_직업명을_찾는다()
    {
        var sut = Make();

        Assert.AreEqual("광전사", sut.GetCharacterTalk(ConstValues.Berserker, ConstValues.Korean));
        Assert.AreEqual("Gunner", sut.GetCharacterTalk(ConstValues.Gunner, ConstValues.English));
        Assert.IsNull(sut.GetCharacterTalk("없는직업", ConstValues.Korean));
    }

    [Test]
    public void 아이템_이름과_설명은_각각_다른_idx를_참조한다()
    {
        var sut = Make();

        Assert.AreEqual("망할 모험", sut.GetItemTalk("Potion", ConstValues.Korean));   // name  = 10000
        Assert.AreEqual("광전사", sut.GetItemExplain("Potion", ConstValues.Korean));   // explain = 50000
    }

    [Test]
    public void 없는_아이템은_null을_반환한다()
    {
        var sut = Make();
        Assert.IsNull(sut.GetItemTalk("없는아이템", ConstValues.Korean));
    }

    [Test]
    public void 지역명을_언어에_맞게_반환하고_미지정_지역은_Non이다()
    {
        var sut = Make();

        Assert.AreEqual("숲", sut.GetPlaceName(ePlace.Forest, ConstValues.Korean));
        Assert.AreEqual("Forest", sut.GetPlaceName(ePlace.Forest, ConstValues.English));
    }

    [Test]
    public void 키_표시는_언어와_무관하며_특수키는_기호로_바꾼다()
    {
        Assert.AreEqual("←", LocalizationService.GetKeyCodeText(KeyCode.LeftArrow));
        Assert.AreEqual("Esc", LocalizationService.GetKeyCodeText(KeyCode.Escape));
        Assert.AreEqual("Enter", LocalizationService.GetKeyCodeText(KeyCode.Return));

        // 매핑에 없는 키는 이름 그대로
        Assert.AreEqual("A", LocalizationService.GetKeyCodeText(KeyCode.A));
    }

    [Test]
    public void 중복된_idx가_있으면_먼저_나온_것을_쓴다()
    {
        var table = new TalkDataList
        {
            Talk = new List<TalkData>
            {
                new TalkData { idx = 1, kr = "첫번째" },
                new TalkData { idx = 1, kr = "두번째" },
            }
        };
        var sut = new LocalizationService(table, MakeItemTable());

        Assert.AreEqual("첫번째", sut.GetTalk(1, ConstValues.Korean));
        Assert.AreEqual(1, sut.TalkCount);
    }

    [Test]
    public void 빈_테이블로도_생성되고_터지지_않는다()
    {
        var sut = new LocalizationService(null, null);

        Assert.AreEqual(0, sut.TalkCount);
        Assert.IsNull(sut.GetTalk(10000, ConstValues.Korean));
    }
}
