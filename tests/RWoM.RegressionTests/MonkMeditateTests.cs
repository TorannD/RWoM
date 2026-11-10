using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;
using Verse.AI;
using AbilityUser;

namespace RWoM.RegressionTests;

// Player report: the monk's Meditate autocast re-proc'd its activation every
// cooldown even mid-task with nothing to gain. The utility gate must fire only
// when meditation would actually do something (heal, purge, break-risk relief,
// or chi/rest building while idle) and never interrupt real work for a trickle.
internal static class MonkMeditateTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("MonkMeditate.HealthyIdleBuildsChi", HealthyIdle);
        yield return new("MonkMeditate.WorkingPawnNotInterruptedForTrickle", WorkingTrickle);
        yield return new("MonkMeditate.HighChiSkips", HighChi);
        yield return new("MonkMeditate.TiredBuildsRest", Tired);
        yield return new("MonkMeditate.InjuryAlwaysWorthIt", Injured);
        yield return new("MonkMeditate.NoChiHediffSkipsUntilGrant", NoChi);
        yield return new("MonkMeditate.JobGateHonorsKnowledge", JobGateHonorsKnowledge);
        yield return new("MonkMeditate.JobGateToleratesMissingChi", JobGateToleratesMissingChi);
    }

    // Ф4: the JobGiver decision core reads the knowledge store like the
    // dispatch, and the chi hediff lag (60-tick grant cadence) must skip
    // quietly instead of NRE-ing.
    static void JobGateHonorsKnowledge(RuntimeFixture _)
    {
        var pawn = Pawn(working: false);
        SetChi(pawn, 10f);
        var power = new MightPower(new List<AbilityUser.AbilityDef> { MeditateDef() }) { learned = true, autocast = true };
        var ability = new PawnAbility { Def = MeditateDef() };

        // No knowledge comp: legacy flags decide — on.
        Check.Equal(true, TM_Calc.MonkMeditateJobGate(pawn, power, ability, FindChi(pawn)), "legacy-only path stays on");

        // The store says OFF while legacy says ON.
        var knowledge = new AbilityKnowledgeComp { parent = pawn };
        pawn.AllComps.Add(knowledge);
        var entry = knowledge.GetOrCreate(TorannMagic.SpellIds.Of(power.abilityDef), 0);
        entry.learned = true;
        entry.autocast = false;
        Check.Equal(false, TM_Calc.MonkMeditateJobGate(pawn, power, ability, FindChi(pawn)), "knowledge autocast=false gates the job giver");

        // The store says ON while legacy says OFF.
        power.autocast = false;
        entry.autocast = true;
        Check.Equal(true, TM_Calc.MonkMeditateJobGate(pawn, power, ability, FindChi(pawn)), "knowledge autocast=true enables the job giver");
    }

    static void JobGateToleratesMissingChi(RuntimeFixture _)
    {
        var pawn = Pawn(working: false); // no chi hediff added
        var power = new MightPower(new List<AbilityUser.AbilityDef> { MeditateDef() }) { learned = true, autocast = true };
        var ability = new PawnAbility { Def = MeditateDef() };
        Check.Equal(false, TM_Calc.MonkMeditateJobGate(pawn, power, ability, null), "missing chi hediff skips without throwing");
        Check.Equal(false, TM_Calc.MonkMeditateJobGate(pawn, power, null, null), "missing runtime ability skips without throwing");
    }

    // The real AbilityDef ctor loads shaders (BaseContent) — bypass only that.
    static TMAbilityDef MeditateDef()
    {
        var def = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
        def.defName = "TM_Meditate";
        def.label = "TM_Meditate";
        return def;
    }

    static Hediff FindChi(Pawn pawn)
        => pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.def == ChiDef);

    static Pawn Pawn(bool working, float rest = 1f)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "MonkMeditatePawn"; def.category = ThingCategory.Pawn;
        def.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = def };
        pawn.health = new Pawn_HealthTracker(pawn);
        pawn.needs = new Pawn_NeedsTracker(pawn);
        pawn.needs.rest = new Need_Rest(pawn) { def = new NeedDef { defName = "MonkMeditateRest" } };
        pawn.needs.rest.CurLevel = rest;
        pawn.jobs = new Pawn_JobTracker(pawn);
        // Comps must be a real per-pawn list: the AllComps getter returns the
        // SHARED EmptyCompsList when the field is null, and Add would pollute it.
        // The field is declared on ThingWithComps, not Pawn.
        for (Type type = pawn.GetType(); type != null; type = type.BaseType)
        {
            var compsField = type.GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (compsField == null) continue;
            compsField.SetValue(pawn, new List<ThingComp>());
            break;
        }
        if (working)
        {
            // Any work-giver job registers as "busy with real work".
            var job = new Job { def = new JobDef { defName = "MonkMeditateFakeWork" } };
            job.workGiverDef = new WorkGiverDef { defName = "MonkMeditateFakeWg" };
            FleshStatTests.Set(pawn.jobs, "curJob", job);
        }
        return pawn;
    }

    private static readonly HediffDef ChiDef = new HediffDef { defName = "TM_ChiHD" };

    static void SetChi(Pawn pawn, float severity)
    {
        var chi = new Hediff { def = ChiDef, pawn = pawn };
        typeof(Hediff).GetField("severityInt", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(chi, severity);
        pawn.health.hediffSet.hediffs.Add(chi);
    }

    static bool Worth(Pawn pawn)
    {
        HediffDef old = TorannMagicDefOf.TM_ChiHD;
        TorannMagicDefOf.TM_ChiHD = ChiDef;
        try { return TM_Calc.MonkMeditateWorthAutocast(pawn); }
        finally { TorannMagicDefOf.TM_ChiHD = old; }
    }

    static void HealthyIdle(RuntimeFixture _)
    {
        var pawn = Pawn(working: false);
        SetChi(pawn, 10f);
        Check.Equal(true, Worth(pawn), "an idle healthy monk may build chi");
    }

    static void WorkingTrickle(RuntimeFixture _)
    {
        var pawn = Pawn(working: true);
        SetChi(pawn, 10f);
        Check.Equal(false, Worth(pawn), "a working pawn is never interrupted for a chi trickle");
    }

    static void HighChi(RuntimeFixture _)
    {
        var pawn = Pawn(working: false);
        SetChi(pawn, 90f);
        Check.Equal(false, Worth(pawn), "high chi skips the cast even when idle");
    }

    static void Tired(RuntimeFixture _)
    {
        var pawn = Pawn(working: true, rest: .2f);
        SetChi(pawn, 10f);
        Check.Equal(true, Worth(pawn), "a tired working monk still benefits from the rest trickle");
    }

    static void NoChi(RuntimeFixture _)
    {
        // Freshly created monk: the chi hediff is granted by the might comp a tick
        // later; until then the autocast must not start a job it cannot track.
        var pawn = Pawn(working: false);
        Check.Equal(false, Worth(pawn), "a monk without the chi hediff yet skips meditate");
    }

    static void Injured(RuntimeFixture _)
    {
        var pawn = Pawn(working: true);
        var injury = new Hediff_Injury
        {
            def = new HediffDef { defName = "MonkMeditateScratch", hediffClass = typeof(Hediff_Injury) },
            pawn = pawn, Severity = 5f
        };
        var part = new BodyPartRecord { def = new BodyPartDef { defName = "MonkMeditateTorso" } };
        FleshStatTests.Set(injury, "part", part);
        pawn.health.hediffSet.hediffs.Add(injury);
        Check.Equal(true, Worth(pawn), "wounds always justify the cast, even mid-task");
    }
}
