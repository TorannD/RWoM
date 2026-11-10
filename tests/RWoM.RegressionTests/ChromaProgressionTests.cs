using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.TMDefs;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class ChromaProgressionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        foreach (int level in new[] { 29, 30, 31, 49, 199, 200 })
        {
            int start = level;
            yield return new("ChromaProgression.NativeKure.Level" + start, f => Boundary(f, start));
        }
        yield return new("ChromaProgression.OldSaveRepeatedLoadPreservesProgress", OldSave);
        yield return new("ChromaProgression.OtherClassExplicitCap", OtherClass);
        yield return new("ChromaProgression.WithoutKureNormalMage", WithoutKure);
    }

    static TM_CustomClass ReadChroma()
    {
        string root = Path.Combine(Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!, "Mods/Kures AROM 1.7 (Unofficial)");
        var source = XDocument.Load(Path.Combine(root, "Common/Patches/RFS_CustomClasses.xml"))
            .Descendants("li").Single(x => x.Element("classTrait")?.Value == "TM_ChromaMage");
        return new TM_CustomClass {
            classTrait = new TraitDef { defName = "TM_ChromaMage" },
            isMage = bool.Parse(source.Element("isMage")!.Value),
            maxMageLevel = (int?)source.Element("maxMageLevel") ?? 200
        };
    }

    static CompAbilityUserMagic Mage(RuntimeFixture f, TM_CustomClass? custom)
    {
        LoadBaseDefinitions(f);
        var p = PawnWithComps(true, false);
        p.magic.customClass = custom;
        return p.magic;
    }

    static void Boundary(RuntimeFixture f, int start)
    {
        var magic = Mage(f, ReadChroma());
        magic.MagicData.MagicUserLevel = start;
        magic.MagicData.MagicAbilityPoints = 7;
        magic.MagicData.MagicUserXP = magic.GetXPForLevel(start) + 1;
        int xp = magic.MagicUserXP;
        magic.LevelUp(true);
        Check.Equal(start < 200 ? start + 1 : 200, magic.MagicUserLevel, "normal mage boundary applies to native Kure class");
        Check.Equal(start < 200 ? 8 : 7, magic.MagicData.MagicAbilityPoints, "one real level awards exactly one point");
        if (start < 200) Check.Equal(xp, magic.MagicUserXP, "uncapping does not reset accumulated XP");
    }

    static void OldSave(RuntimeFixture f)
    {
        var magic = Mage(f, ReadChroma());
        var old = magic.MagicData;
        old.MagicUserLevel = 30;
        old.MagicUserXP = magic.GetXPForLevel(30) + 19;
        old.MagicAbilityPoints = 4;
        old.MagicPowersA[0].learned = true;
        old.MagicPowersA[0].level = 2;
        old.MagicPowerSkill_global_spirit[0].level = 50;
        int xp = old.MagicUserXP;
        // Save the progression payload alone; the pawn belongs to the game save,
        // not this isolated Scribe envelope.
        Set(old, "magicPawn", null);
        string path = f.Save(old);
        for (int i = 0; i < 2; i++)
        {
            var loaded = f.Load(path);
            Set(magic, "magicData", loaded);
            Check.Equal(30, magic.MagicUserLevel, "loading does not retroactively award levels");
            Check.Equal(4, loaded.MagicAbilityPoints, "loading does not award points");
            Check.Equal(xp, magic.MagicUserXP, "old XP preserved");
            Check.Equal(true, loaded.MagicPowersA[0].learned, "learned spell preserved");
            Check.Equal(2, loaded.MagicPowersA[0].level, "spell rank preserved");
            Check.Equal(50, loaded.MagicPowerSkill_global_spirit[0].level, "spirit skill rank preserved");
            path = f.Save(loaded);
        }
        magic.LevelUp(true);
        Check.Equal(31, magic.MagicUserLevel, "old level30 save can earn next level");
        Check.Equal(5, magic.MagicData.MagicAbilityPoints, "old save receives exactly one earned point");
    }

    static void OtherClass(RuntimeFixture f)
    {
        var magic = Mage(f, new TM_CustomClass { classTrait = new TraitDef { defName = "OtherMage" }, isMage = true, maxMageLevel = 30 });
        magic.MagicData.MagicUserLevel = 30;
        magic.MagicData.MagicAbilityPoints = 3;
        magic.LevelUp(true);
        Check.Equal(30, magic.MagicUserLevel, "other explicit cap retained");
        Check.Equal(3, magic.MagicData.MagicAbilityPoints, "capped mage receives no point");
    }

    static void WithoutKure(RuntimeFixture f)
    {
        var magic = Mage(f, null);
        magic.MagicData.MagicUserLevel = 30;
        magic.LevelUp(true);
        Check.Equal(31, magic.MagicUserLevel, "normal mage needs no Kure definition");
    }
}
