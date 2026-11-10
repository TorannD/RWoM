using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class SpiritMapCollapseTests
{
    private static IEnumerable<TestCase> HostedRaiseCases()
    {
        yield return new("SpiritProjectedCast.RaiseUndeadTargetArea", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            var projectile = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            projectile.projectile = new ProjectileProperties { explosionRadius = 4 };
            var check = typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.Projectile_RaiseUndead", true)!
                .GetMethod("HasHostedTarget", BindingFlags.Static | BindingFlags.NonPublic)!;
            bool Valid(IntVec3 cell) => (bool)check.Invoke(null, [c.Map, cell, projectile])!;
            var center = new IntVec3(15, 0, 15);
            Check.Equal(false, Valid(center), "carried corpse is not a map target");
            Check.Equal(false, Valid(IntVec3.Invalid), "out of bounds cell");
            // Pawn.Kill finalizes the pawn Thing after placing it inside the corpse.
            GrimoireProgressionTests.Set(c.Original, "mapIndexOrState", (sbyte)-2);
            c.Corpse.def.size = new IntVec2(1, 1);
            c.Corpse.holdingOwner.Remove(c.Corpse);
            GrimoireProgressionTests.Set(c.Corpse, "positionInt", center);
            GrimoireProgressionTests.Set(c.Corpse, "mapIndexOrState", (sbyte)0);
            Check.Equal(true, c.Map.spawnedThings.TryAdd(c.Corpse, false), "map takes the corpse");
            c.Map.thingGrid.Register(c.Corpse);
            Check.Equal(true, center.GetThingList(c.Map).Contains(c.Corpse), "native thing grid contains corpse");
            Check.Equal(true, Valid(center), "kill-finalized native pawn remains a resurrectable corpse");
            Check.Equal(true, Valid(center + IntVec3.East * 4), "corpse inside the native area");
            Check.Equal(false, Valid(center + IntVec3.East * 5), "corpse beyond native radius");
            var disease = new Hediff { def = new HediffDef { defName = "ROM_Vampirism" }, pawn = c.Original };
            c.Original.health.hediffSet.hediffs.Add(disease);
            Check.Equal(false, Valid(center), "immune corpse cannot admit paid projected cast");
            c.Original.health.hediffSet.hediffs.Remove(disease);
            Check.Equal(true, Valid(center), "eligible again");
        }));
        yield return new("SpiritProjectedCast.RaiseUndeadRankAndJob", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var fixture = new Fixture(r, c);
            var soul = c.Original;
            var magic = soul.GetCompAbilityUserMagic();
            var def = TorannMagicDefOf.TM_RaiseUndead;
            var ability = new MagicAbility(magic) { Def = def };
            var skills = magic.MagicData.MagicPowerSkill_RaiseUndead;
            skills.Single(s => s.label == "TM_RaiseUndead_pwr").level = 1;
            skills.Single(s => s.label == "TM_RaiseUndead_ver").level = 2;
            var shot = (Projectile_SpiritBolt)Activator.CreateInstance(typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.Projectile_RaiseUndead", true)!)!;
            shot.GetType().GetMethod("PrepareProjection", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(shot, [ability]);
            skills.Single(s => s.label == "TM_RaiseUndead_ver").level = 0;
            var type = typeof(Projectile_SpiritBolt);
            Check.Same(soul, type.GetProperty("SpellInstigator", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(shot), "only original owns the spell");
            Check.Equal(1, (int)type.GetField("pwrVal", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(shot)!, "soul power at launch");
            Check.Equal(2, (int)type.GetField("verVal", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(shot)!, "soul versatility stays frozen in flight");
            var servant = GrimoireProgressionTests.PawnWithComps(false, false).pawn;
            var foreign = c.Creator.GetCompAbilityUserMagic();
            var mark = new HediffDef { defName = "RaiseTestServantMark" };
            var hediff = new HediffWithComps { pawn = servant, def = mark };
            var component = (HediffComp)Activator.CreateInstance(def.GetType().Assembly.GetType("TorannMagic.HediffComp_Undead", true)!)!;
            component.parent = hediff; hediff.comps = [component];
            servant.health.hediffSet.hediffs.Add(hediff);
            var field = component.GetType().GetField("linkedPawn")!;
            field.SetValue(component, c.Creator);
            foreign.supportedUndead.Add(servant); foreign.supportedUndead.Add(servant);
            var bind = shot.GetType().GetMethod("BindServant", BindingFlags.Static | BindingFlags.NonPublic)!;
            bind.Invoke(null, [magic, c.Creator, servant, mark]);
            bind.Invoke(null, [magic, soul, servant, mark]);
            Check.Same(soul, field.GetValue(component), "servant belongs to the actual original");
            Check.Equal(0, foreign.supportedUndead.Count(t => t == servant), "prior owner stops paying for re-raised servant");
            Check.Equal(1, magic.supportedUndead.Count(t => t == servant), "repeat cannot duplicate upkeep");
            var jobs = XDocument.Load(Path.Combine(r.Repository, "v1.7/Defs/JobDefs/TM_SpiritRaiseUndead.xml")).Root!;
            Check.Equal(typeof(JobDriver_SpiritBolt).FullName, jobs.Element("JobDef")!.Element("driverClass")!.Value, "native warmup and payment job");
            foreach (string lang in new[] { "English", "Russian", "Ukrainian", "Spanish", "Portuguese", "PortugueseBrazilian", "ChineseSimplified" })
            {
                var langFile = Path.Combine(r.Repository, "v1.7/Languages", lang, "DefInjected/JobDef/TM_SpiritRaiseUndead.xml");
                Check.NotNull(XDocument.Load(langFile).Root!.Element("TM_SpiritRaiseUndead.reportString"), "report " + lang);
            }
        }));
    }
}
