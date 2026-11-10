using System.Xml.Linq;
using System.Text.RegularExpressions;
namespace RWoM.RegressionTests;
internal static class FleshUiTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("FleshUi.LocalizedSkillContracts", f => {
            var required = new[] { "Devour", "Restore", "Empower", "Harden", "Feast", "Mobility", "Strength", "Industry", "Capacity" };
            foreach (var language in Directory.GetDirectories(Path.Combine(f.Repository, "Languages")).Select(Path.GetFileName)) {
                var file = Path.Combine(f.Repository, "Languages", language, "Keyed", "TM_FleshDevelopment.xml");
                Check.Equal(true, File.Exists(file), language + " dedicated flesh localization exists");
                var xml = XDocument.Load(file);
                foreach (var skill in required) foreach (var suffix in new[] { "", "Desc" })
                    Check.Equal(true, !string.IsNullOrWhiteSpace(xml.Root!.Element("TM_Flesh_" + skill + suffix)?.Value), skill + suffix + " localized");
            }
        });
        yield return new("FleshUi.LocalizationParityAndRuntimeArt", f => {
            var en = XDocument.Load(Path.Combine(f.Repository, "Languages/English/Keyed/TM_FleshDevelopment.xml")).Root!.Elements().ToArray();
            var ru = XDocument.Load(Path.Combine(f.Repository, "Languages/Russian/Keyed/TM_FleshDevelopment.xml")).Root!.Elements().ToArray();
            Check.Equal(en.Length, en.Select(e => e.Name.LocalName).Distinct().Count(), "English keys unique");
            Check.Equal(ru.Length, ru.Select(e => e.Name.LocalName).Distinct().Count(), "Russian keys unique");
            Check.Equal(string.Join(",", en.Select(e => e.Name.LocalName).Order()), string.Join(",", ru.Select(e => e.Name.LocalName).Order()), "language key parity");
            foreach (var entry in en) {
                var counterpart = ru.Single(e => e.Name == entry.Name);
                string Placeholders(string value) => string.Join(",", Regex.Matches(value, @"\{\d+\}").Select(m => m.Value).Order());
                Check.Equal(Placeholders(entry.Value), Placeholders(counterpart.Value), entry.Name + " format placeholders");
            }
            foreach (var icon in new[] { "01-devour-flesh", "02-restore-flesh", "03-empower-flesh", "04-harden-skin", "05-feast-of-flesh", "06-mobile-flesh", "07-mighty-flesh", "08-industrious-flesh", "09-flesh-capacity", "10-flesh-grimoire", "11-stop-feast" })
                Check.Equal(true, File.Exists(Path.Combine(f.Repository, "Textures/UI/FleshDevelopment", icon + ".png")), icon + " runtime art exists");
        });
        yield return new("FleshUi.AllLanguagesHaveCompleteFleshTranslations", f => {
            var root = Path.Combine(f.Repository, "Languages");
            var english = XDocument.Load(Path.Combine(root, "English/Keyed/TM_FleshDevelopment.xml")).Root!.Elements().ToDictionary(e => e.Name.LocalName, e => e.Value);
            var health = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/FleshDevelopment/FleshHealth.xml")).Root!.Elements("HediffDef").Where(e => e.Element("defName") != null).Select(e => e.Element("defName")!.Value + ".description").Order().ToArray();
            var jobs = new[] { "TM_FleshCast.reportString", "TM_FleshDevour.reportString" };
            foreach (var directory in Directory.GetDirectories(root)) {
                var language = Path.GetFileName(directory);
                var entries = XDocument.Load(Path.Combine(directory, "Keyed/TM_FleshDevelopment.xml")).Root!.Elements().ToArray();
                Check.Equal(english.Count, entries.Length, language + " complete key count");
                Check.Equal(entries.Length, entries.Select(e => e.Name.LocalName).Distinct().Count(), language + " unique keys");
                foreach (var entry in entries) {
                    Check.Equal(true, english.ContainsKey(entry.Name.LocalName), language + " known key " + entry.Name);
                    Check.Equal(false, string.IsNullOrWhiteSpace(entry.Value), language + " nonempty " + entry.Name);
                    string Slots(string value) => string.Join(",", Regex.Matches(value, @"\{\d+\}").Select(m => m.Value).Order());
                    Check.Equal(Slots(english[entry.Name.LocalName]), Slots(entry.Value), language + " placeholders " + entry.Name);
                }
                if (language == "English") continue; // English descriptions and reports are in Defs.
                foreach (var contract in new[] { ("HediffDef", health), ("JobDef", jobs) }) {
                    var injected = XDocument.Load(Path.Combine(directory, "DefInjected", contract.Item1, "TM_FleshDevelopment.xml")).Root!.Elements().ToArray();
                    Check.Equal(string.Join(",", contract.Item2.Order()), string.Join(",", injected.Select(e => e.Name.LocalName).Order()), language + " injection targets " + contract.Item1);
                    Check.Equal(true, injected.All(e => !string.IsNullOrWhiteSpace(e.Value)), language + " injection text");
                }
            }
        });
        yield return new("FleshUi.CommandSignatureContracts", _ => {
            var actions = typeof(TorannMagic.FleshDevelopment.FleshActions);
            foreach (var contract in new[] {
                ("Upgrade", new[] { typeof(Verse.Pawn), typeof(TorannMagic.FleshDevelopment.FleshSkill) }),
                ("TryActivate", new[] { typeof(Verse.Pawn), typeof(TorannMagic.FleshDevelopment.FleshSkill) }),
                ("TryDevour", new[] { typeof(Verse.Pawn), typeof(Verse.Thing) }),
                ("StopAll", new[] { typeof(Verse.Pawn) }) })
                Check.NotNull(actions.GetMethod(contract.Item1, contract.Item2), contract.Item1 + " command parameter contract");
            var stop = typeof(TorannMagic.FleshDevelopment.FleshMultiplayer).GetMethod("StopFeast");
            Check.NotNull(stop, "single-pawn stop command exists");
            Check.Equal(1, stop!.GetParameters().Length, "stop serializes only undead to avoid mixed-map pawn arguments");
            Check.Equal(typeof(Verse.Pawn), stop.GetParameters()[0].ParameterType, "stop command pawn argument");
            Check.NotNull(typeof(TorannMagic.FleshDevelopment.JobDriver_FleshCast).GetMethod("TryStart", [typeof(Verse.Pawn), typeof(TorannMagic.FleshDevelopment.FleshSkill)]), "delayed casting sync command signature");
        });
    }
}
