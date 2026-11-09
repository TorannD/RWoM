using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SniperFractureImpactTests
{
    public sealed class ImpactWorker : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            Damages.Add(dinfo.Amount);
            if (fatal) Set(((Pawn)victim).health, "healthState", PawnHealthState.Dead);
            return new DamageResult();
        }
    }
    private static bool fatal;
    private static readonly List<float> Damages = [];
    public static bool Skip() => false;

    internal static void Verify(Pawn shooter)
    {
        var foe = new Faction { def = new FactionDef(), loadID = 9971 };
        var previousRelations = (List<FactionRelation>)typeof(Faction).GetField("relations", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(shooter.Faction)!;
        Set(foe, "relations", new List<FactionRelation> { new() { other = shooter.Faction, kind = FactionRelationKind.Hostile } });
        var relations = previousRelations.ToList();
        relations.Add(new() { other = foe, kind = FactionRelationKind.Hostile });
        Set(shooter.Faction, "relations", relations);
        var victim = PawnWithComps(false, false).pawn;
        victim.def.race.body = shooter.def.race.body;
        victim.kindDef = new PawnKindDef { race = victim.def };
        victim.guest = new Pawn_GuestTracker(victim); victim.mindState = new Pawn_MindState(victim);
        Set(victim, "factionInt", foe); Set(victim, "mapIndexOrState", (sbyte)0);
        Set(victim, "positionInt", shooter.Position + IntVec3.East * 4);
        var damage = new DamageDef { defName = "FractureTestRanged", isRanged = true, harmsHealth = false, workerClass = typeof(ImpactWorker) };
        var ammo = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        ammo.defName = "FractureTestBullet"; ammo.thingClass = typeof(Bullet);
        ammo.projectile = new ProjectileProperties { damageDef = damage, speed = 55 };
        Set(ammo.projectile, "damageAmountBase", 20);
        var oldDefs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        var breach = DefDatabase<HediffDef>.GetNamedSilentFail("TM_SniperArmorBreach");
        if (breach == null) {
            breach = new HediffDef { defName = "TM_SniperArmorBreach", hediffClass = typeof(SniperArmorBreach), initialSeverity = 1f };
            DefDatabase<HediffDef>.Add(breach);
        }
        var lib = Assembly.Load("0Harmony");
        var ht = lib.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.fracture-impact." + Guid.NewGuid();
        var harmony = Activator.CreateInstance(ht, id)!;
        var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        void SkipMethod(MethodBase target)
            => ht.GetMethod("Patch")!.Invoke(harmony, [target, Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!, typeof(SniperFractureImpactTests).GetMethod(nameof(Skip))!), null, null, null]);
        var hits = typeof(SniperArmorBreach).Assembly.GetType("TorannMagic.ClassProgression.SniperMastery", true)!
            .GetMethod("Hits", BindingFlags.Static | BindingFlags.NonPublic)!;
        int Hits() => (int)hits.Invoke(null, [shooter])!;
        int before = Hits();
        SniperFractureBullet Fire(bool stray = false)
        {
            var bullet = new SniperFractureBullet { def = ammo };
            Set(bullet, "mapIndexOrState", (sbyte)0);
            Set(bullet, "positionInt", victim.Position);
            bullet.Launch(shooter, shooter.Position.ToVector3Shifted(), victim, stray ? shooter : victim, ProjectileHitFlags.IntendedTarget);
            Deliver(bullet);
            return bullet;
        }
        void Deliver(SniperFractureBullet bullet)
            => typeof(SniperFractureBullet).GetMethod("Impact", flags)!.Invoke(bullet, [victim, false]);
        try
        {
            Set(Find.TickManager, "gameStartAbsTick", 1);
            Set(Current.Game, "battleLog", new BattleLog());
            var patchType = typeof(SniperFractureBullet).Assembly.GetType("TorannMagic.ClassProgression.SniperFractureShieldPatch")!;
            var processor = ht.GetMethod("CreateClassProcessor")!.Invoke(harmony, [patchType])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            SkipMethod(typeof(Projectile).GetMethod("Impact", flags)!); // remove graphical impact and map destruction
            SkipMethod(typeof(Bullet).GetMethod("NotifyImpact", flags)!);
            SkipMethod(typeof(BattleLog).GetMethod("Add")!);
            SkipMethod(typeof(Thing).GetProperty("FlammableNow")!.GetMethod!);
            SkipMethod(typeof(RestUtility).GetMethod("Awake", [typeof(Pawn)])!);
            SkipMethod(typeof(CompShield).GetMethod("AbsorbedDamage", flags)!); // only the sound and flecks
            SkipMethod(typeof(Pawn_HealthTracker).GetMethod("PreApplyDamage")!); // quest/social notifications
            SkipMethod(typeof(Pawn).GetMethod("PostApplyDamage")!);
            var shield = new CompShield { parent = victim, props = new CompProperties_Shield { energyLossPerDamage = 0f } };

            victim.AllComps.Add(shield);
            Fire();
            Check.Equal(0, Damages.Count, "native shield consumes the shot before the damage worker");
            Check.Equal(false, victim.health.hediffSet.HasHediff(breach), "shielded impact cannot open armor");
            Check.Equal(before, Hits(), "personal shield prevents trial credit");
            victim.AllComps.Remove(shield);
            var fired = Fire();
            Check.Equal(20f, Damages.Single(), "fracture round sends unboosted primary weapon damage");
            var wound = victim.health.hediffSet.GetFirstHediffOfDef(breach);
            Check.Equal(true, wound is SniperArmorBreach, "unshielded intended impact opens armor");
            Deliver(fired);
            Check.Equal(1, Damages.Count, "replayed impact cannot inflict weapon damage twice");
            var openings = (System.Collections.IList)typeof(SniperArmorBreach).GetField("openings", flags)!.GetValue(wound)!;
            Check.Equal(1, openings.Count, "same bullet cannot create two openings");
            Check.Equal(before + 1, Hits(), "intended impact credited once across duplicate callback");

            Fire(stray: true);
            victim.guest.guestStatusInt = GuestStatus.Prisoner;
            Fire();
            victim.guest.guestStatusInt = GuestStatus.Guest;
            Set(victim.health, "healthState", PawnHealthState.Down);
            Fire();
            Set(victim.health, "healthState", PawnHealthState.Mobile);
            Set(victim, "factionInt", shooter.Faction);
            Fire();
            Set(victim, "factionInt", foe);
            Check.Equal(before + 1, Hits(), "collateral, captive, downed and friendly hits grant no credit");

            fatal = true;
            Fire();
            Check.Equal(true, victim.Dead, "fixture delivered a fatal primary hit");
            Check.Equal(before + 2, Hits(), "fatal hit counts despite there being no living pawn to weaken");
            Check.Equal(1, openings.Count, "fatal hit creates no new breach");
        }
        finally
        {
            fatal = false;
            Damages.Clear();
            ht.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
            Set(shooter.Faction, "relations", previousRelations);
            DefDatabase<HediffDef>.Clear(); foreach (var def in oldDefs) DefDatabase<HediffDef>.Add(def);
        }
    }
}
