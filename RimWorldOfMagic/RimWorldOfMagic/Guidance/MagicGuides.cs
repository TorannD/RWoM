using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.Guidance
{
    public static class MagicGuides
    {
        private sealed class Guide
        {
            public readonly string key;
            public readonly string[] research, devices;
            public readonly bool odyssey;
            public Guide(string key, string[] research, string[] devices, bool odyssey = false)
            { this.key = key; this.research = research; this.devices = devices; this.odyssey = odyssey; }
        }

        private static readonly Guide[] guides = {
            new Guide("magicyte", new[] {"TM_MagicyteRefining"}, new string[0]),
            new Guide("forging", new[] {"TM_ArcaneForging"}, new[] {"TableArcaneForge"}),
            new Guide("replication", new[] {"TM_ForgeReplication"}, new[] {"TableArcaneForge"}),
            new Guide("scribing", new[] {"TM_MagicScribing"}, new[] {"TableMagicPrinter"}),
            new Guide("advancedScribing", new[] {"TM_AdvancedScribing"}, new[] {"TableMagicPrinter"}),
            new Guide("masterScribing", new[] {"TM_MasterScribing"}, new[] {"TableMagicPrinter"}),
            new Guide("enchanting", new[] {"TM_Enchanting"}, new[] {"TableGemcutting"}),
            new Guide("circle", new[] {"TM_MagicCircle"}, new[] {"TableMagicCircle"}),
            new Guide("portals", new[] {"TM_Portals"}, new[] {"TM_Portal"}),
            new Guide("golems", new[] {"TM_GolemCraftRD"}, new string[0]),
            new Guide("refining", new[] {"TM_MagofuelRefining"}, new[] {"TM_MagofuelReservoir", "TM_ManaSiphon"}, true),
            new Guide("network", new[] {"TM_MagofuelRefining", "Electricity"}, new[] {"TM_ManaBeacon", "TM_MagofuelReservoir", "TM_MagofuelGenerator"}, true),
            new Guide("smallThruster", new[] {"TM_MagofuelRefining", "BasicGravtech"}, new[] {"TM_SmallMagicalThruster"}, true),
            new Guide("largeThruster", new[] {"TM_MagofuelRefining", "StandardGravtech"}, new[] {"TM_LargeMagicalThruster"}, true),
            new Guide("spaceSupport", new[] {"TM_MagofuelRefining", "OrbitalTech"}, new[] {"TM_MagicalOxygen", "TM_ThermalShieldGenerator", "TM_CombatShieldGenerator", "TM_CombatShieldBooster", "TM_StellarObservatory"}, true),
            new Guide("spaceSpells", new[] {"TM_AdvancedScribing", "OrbitalTech"}, new[] {"SpellOf_EtherShell", "SpellOf_BreachSeal"}, true),
            new Guide("gravtech", new[] {"TM_MagicalGravtech"}, new[] {"TM_MagicalGravEngine", "TM_MagicalGravExtender"}, true),
            new Guide("dome", new[] {"TM_GrandAtmosphereDomes"}, new[] {"TM_GrandAtmosphereDome"}, true)
        };

        public static IEnumerable<string> Unlocked(Func<string, bool> researched, bool odyssey, bool vge)
            => guides.Where(g => (!g.odyssey || odyssey) && g.research.All(researched)
                && (g.key != "gravtech" || !vge || researched("StandardGravtech"))).Select(g => g.key);

        private static string Description(Def def) => def == null ? null : def.LabelCap + "\n" + def.description;
        private static string ThingDescription(string name) => Description(DefDatabase<ThingDef>.GetNamedSilentFail(name));

        public static string StartBody() => string.Join("\n\n", new[] {
            ThingDescription("TM_MagicalGravEngine"), ThingDescription("TM_SmallMagicalThruster"),
            ThingDescription("TM_ManaBeacon"), "TM_GuideNetwork".Translate().ToString(),
            "TM_GuideOwnership".Translate().ToString(), "TM_GuideReread".Translate().ToString()
        }.Where(text => !string.IsNullOrEmpty(text)));

        public static string Body(string key)
        {
            var guide = guides.FirstOrDefault(g => g.key == key);
            if (guide == null) return "";
            var paragraphs = guide.research.Select(name => Description(DefDatabase<ResearchProjectDef>.GetNamedSilentFail(name)))
                .Concat(guide.devices.Select(ThingDescription)).Where(text => !string.IsNullOrEmpty(text)).ToList();
            if (key == "refining")
            {
                paragraphs.Add(Description(DefDatabase<RecipeDef>.GetNamedSilentFail("TM_RefineMagofuel")));
                paragraphs.Add("TM_GuideNetwork".Translate());
            }
            if (key == "network" || key == "smallThruster" || key == "largeThruster" || key == "gravtech")
                paragraphs.Add("TM_GuideNetwork".Translate());
            if (key == "enchanting" || key == "portals")
            {
                var concept = DefDatabase<ConceptDef>.GetNamedSilentFail(key == "portals" ? "TM_Portals" : "TM_Enchanting");
                if (concept != null) paragraphs.Add(concept.HelpTextAdjusted);
            }
            if (key == "spaceSupport") paragraphs.Add("TM_OxygenModeDesc".Translate());
            paragraphs.Add("TM_GuideReread".Translate());
            return string.Join("\n\n", paragraphs.Where(text => !string.IsNullOrEmpty(text)));
        }

        public static string DeviceBody(ThingWithComps device)
        {
            var paragraphs = new List<string> { Description(device.def) };
            if (guides.Any(g => g.odyssey && g.devices.Contains(device.def.defName)))
            {
                paragraphs.Add("TM_GuideNetwork".Translate());
                if (device.def.defName != "TM_ManaBeacon") paragraphs.Add(ThingDescription("TM_ManaBeacon"));
                if (device.def.defName != "TM_MagofuelReservoir") paragraphs.Add(ThingDescription("TM_MagofuelReservoir"));
            }
            if (device.def.defName == "TM_MagicalOxygen")
            {
                paragraphs.Add("TM_OxygenModeDesc".Translate());
                if (device.GetComp<Odyssey.CompMagicalOxygen>()?.PipeNetworkAvailable == true) paragraphs.Add("TM_OxygenOutputDesc".Translate());
            }
            if (device.def.defName == "TableGemcutting" || device.def.defName == "TM_Portal")
            {
                var concept = DefDatabase<ConceptDef>.GetNamedSilentFail(device.def.defName == "TM_Portal" ? "TM_Portals" : "TM_Enchanting");
                if (concept != null) paragraphs.Add(concept.HelpTextAdjusted);
            }
            paragraphs.Add("TM_GuideOwnership".Translate());
            return string.Join("\n\n", paragraphs.Where(text => !string.IsNullOrEmpty(text)));
        }

        public static void AttachDeviceGuides()
        {
            foreach (var name in guides.SelectMany(g => g.devices).Distinct())
            {
                var def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def?.category != ThingCategory.Building || def.comps?.Any(c => c.compClass == typeof(CompMagicGuide)) == true) continue;
                if (def.comps == null) def.comps = new List<CompProperties>();
                def.comps.Add(new CompProperties { compClass = typeof(CompMagicGuide) });
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class MagicGuideDeviceSetup
    {
        static MagicGuideDeviceSetup() => MagicGuides.AttachDeviceGuides();
    }

    // Stateless: available on built, bought and salvaged devices before research.
    public sealed class CompMagicGuide : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action {
                defaultLabel = "TM_GuideRead".Translate(),
                defaultDesc = "TM_GuideReadDesc".Translate(),
                icon = parent.def.uiIcon,
                action = () => Find.WindowStack.Add(new Dialog_MessageBox(MagicGuides.DeviceBody(parent), title: parent.def.LabelCap))
            };
        }
    }
}
