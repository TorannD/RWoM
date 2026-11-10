using System.Xml.Linq;
using TorannMagic.ClassProgression;

namespace RWoM.RegressionTests;

internal static class MartialCareerDefinitionTests
{
    internal static void Verify(RuntimeFixture fixture)
    {
        string mod = Path.Combine(fixture.Repository, "v1.7");
        string abilities = Path.Combine(mod, "Defs", "AbilityDefs");
        foreach (var name in new[] { "BladePassage", "BladeSevenSteps", "RangerSafeTrail", "RangerBreakthrough" })
        {
            var file = "Abilities_" + name + ".xml";
            var original = XDocument.Load(Path.Combine(abilities, file)).Root!.Elements("TorannMagic.TMAbilityDef").Single();
            var bridge = XDocument.Load(Path.Combine(abilities, "Bridge", "Bridge_"+file)).Root!.Elements("AbilityDef").Single();
            var verb = original.Element("MainVerb")!;
            var binder = original.Element("modExtensions")!.Elements().Single(x=>x.Element("spellId")!=null);
            Check.Equal("TM_"+name, (string?)original.Element("defName"), "canonical ability name");
            Check.Equal("Might", (string?)binder.Element("domain"), "martial Core domain");
            Check.Equal((string?)binder.Element("spellId"), (string?)bridge.Element("spellId"), "same Core identity");
            Check.Equal((string?)verb.Element("verbClass"), (string?)bridge.Element("verbProperties")!.Element("verbClass"), "same runtime verb");
            Check.NotNull(typeof(SniperSurvey).Assembly.GetType((string)verb.Element("verbClass")!), "verified verb in built assembly");
            if (original.Element("abilityClass") is XElement owner)
                Check.NotNull(typeof(SniperSurvey).Assembly.GetType(owner.Value), "verified job owner in built assembly");
        }
        foreach (var name in new[] {"Bladedancer","Ranger"})
        {
            foreach (var def in XDocument.Load(Path.Combine(abilities,"Abilities_TMagic_"+name+".xml")).Root!.Elements("TorannMagic.TMAbilityDef"))
                if ((string?)def.Attribute("ParentName") == "BaseMightAbility")
                    Check.Equal("Might", (string?)def.Descendants("domain").Single(),"legacy martial discipline metadata");
        }
        string langs = Path.Combine(mod,"Languages");
        string english = Path.Combine(langs,"English");
        var files = Directory.EnumerateFiles(english,"*.xml",SearchOption.AllDirectories).Where(f =>
        {
            var name=Path.GetFileNameWithoutExtension(f);
            return name.StartsWith("TM_Blade",StringComparison.Ordinal) || name.StartsWith("TM_Ranger",StringComparison.Ordinal);
        }).ToArray();
        foreach (string lang in Directory.GetDirectories(langs))
            foreach (string file in files)
            {
                string relative = Path.GetRelativePath(english,file);
                string localized = Path.Combine(lang,relative);
                Check.Equal(true,File.Exists(localized),"translated "+localized);
                var langKeys = XDocument.Load(localized).Root!.Elements().ToDictionary(e=>e.Name.LocalName,e=>e.Value);
                foreach (var expected in XDocument.Load(file).Root!.Elements())
                {
                    Check.Equal(true,langKeys.TryGetValue(expected.Name.LocalName,out var value) && !string.IsNullOrWhiteSpace(value),localized+" "+expected.Name);
                    string Slots(string str) => string.Join(",",System.Text.RegularExpressions.Regex.Matches(str,@"\{\d+\}").Select(m=>m.Value).Order());
                    Check.Equal(Slots(expected.Value),Slots(value!),"format arguments "+localized);
                }
            }
    }
}
