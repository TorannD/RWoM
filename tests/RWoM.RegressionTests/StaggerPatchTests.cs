using System.Collections.Generic;
using RimWorld;
using TorannMagic;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

// The RWoM stagger exemptions (demons, hollow golems, Burning Fury / Move Out /
// Enrage) are wired through two patch methods on the vanilla 1.7 StaggerHandler.
// The state reader used to carry an inverted condition that cleared the staggered
// flag for EVERY pawn without those hediffs, so no unit in the game ever staggered
// (workshop report). These cases pin the correct exemption semantics.
internal static class StaggerPatchTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("StaggerPatch.NormalPawnStaggersAndReportsStaggered", NormalPawn);
        yield return new("StaggerPatch.DemonAndGolemResistStagger", ResistantDefs);
        yield return new("StaggerPatch.FuryBlocksBothMoveOutEnrageBlockStateOnly", HediffExemptions);
    }

    private static StaggerHandler HandlerFor(Pawn pawn)
    {
        var handler = (StaggerHandler)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(StaggerHandler));
        Set(handler, "parent", pawn);
        return handler;
    }

    private static void NormalPawn(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var pawn = PawnWithComps(true, false).pawn;
        var handler = HandlerFor(pawn);
        Check.Equal(true, TorannMagicMod.StaggerFor_Patch(handler, 60), "a normal pawn receives stagger");
        bool staggered = true;
        TorannMagicMod.Get_Staggered(handler, ref staggered);
        Check.Equal(true, staggered, "a normal pawn keeps the staggered state");
        staggered = false;
        TorannMagicMod.Get_Staggered(handler, ref staggered);
        Check.Equal(false, staggered, "no stagger state is invented for a normal pawn");
    }

    private static void ResistantDefs(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        // LoadBaseDefinitions only initializes ability/trait DefOf fields; swap the
        // ThingDef/HediffDef fields the patches compare against (IceTrial pattern).
        var oldDemon = TorannMagicDefOf.TM_DemonR;
        var oldGolem = TorannMagicDefOf.TM_HollowGolem;
        TorannMagicDefOf.TM_DemonR = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        TorannMagicDefOf.TM_DemonR.defName = "StaggerPatchDemon";
        TorannMagicDefOf.TM_HollowGolem = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        TorannMagicDefOf.TM_HollowGolem.defName = "StaggerPatchGolem";
        try
        {
            foreach (var def in new[] { TorannMagicDefOf.TM_DemonR, TorannMagicDefOf.TM_HollowGolem })
            {
                var pawn = PawnWithComps(true, false).pawn;
                pawn.def = def;
                var handler = HandlerFor(pawn);
                Check.Equal(false, TorannMagicMod.StaggerFor_Patch(handler, 60), def.defName + " never receives stagger");
                bool staggered = true;
                TorannMagicMod.Get_Staggered(handler, ref staggered);
                Check.Equal(false, staggered, def.defName + " never reports staggered");
            }
        }
        finally
        {
            TorannMagicDefOf.TM_DemonR = oldDemon;
            TorannMagicDefOf.TM_HollowGolem = oldGolem;
        }
    }

    private static void HediffExemptions(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var oldFury = TorannMagicDefOf.TM_BurningFuryHD;
        var oldMoveOut = TorannMagicDefOf.TM_MoveOutHD;
        var oldEnrage = TorannMagicDefOf.TM_EnrageHD;
        TorannMagicDefOf.TM_BurningFuryHD = new HediffDef { defName = "StaggerPatchFury" };
        TorannMagicDefOf.TM_MoveOutHD = new HediffDef { defName = "StaggerPatchMoveOut" };
        TorannMagicDefOf.TM_EnrageHD = new HediffDef { defName = "StaggerPatchEnrage" };
        try
        {
            var fury = PawnWithComps(true, false).pawn;
            fury.health.hediffSet.hediffs.Add(new Hediff { def = TorannMagicDefOf.TM_BurningFuryHD, pawn = fury });
            var furyHandler = HandlerFor(fury);
            Check.Equal(false, TorannMagicMod.StaggerFor_Patch(furyHandler, 60), "burning fury pawn never receives stagger");
            bool staggered = true;
            TorannMagicMod.Get_Staggered(furyHandler, ref staggered);
            Check.Equal(false, staggered, "burning fury pawn never reports staggered");

            foreach (var hd in new[] { TorannMagicDefOf.TM_MoveOutHD, TorannMagicDefOf.TM_EnrageHD })
            {
                var pawn = PawnWithComps(true, false).pawn;
                pawn.health.hediffSet.hediffs.Add(new Hediff { def = hd, pawn = pawn });
                var handler = HandlerFor(pawn);
                // These hediffs only suppress the staggered STATE; the StaggerFor gate
                // stays open (preserving the original patch semantics per patch site).
                Check.Equal(true, TorannMagicMod.StaggerFor_Patch(handler, 60), hd.defName + " stagger application untouched");
                bool state = true;
                TorannMagicMod.Get_Staggered(handler, ref state);
                Check.Equal(false, state, hd.defName + " pawn never reports staggered");
            }
        }
        finally
        {
            TorannMagicDefOf.TM_BurningFuryHD = oldFury;
            TorannMagicDefOf.TM_MoveOutHD = oldMoveOut;
            TorannMagicDefOf.TM_EnrageHD = oldEnrage;
        }
    }
}
