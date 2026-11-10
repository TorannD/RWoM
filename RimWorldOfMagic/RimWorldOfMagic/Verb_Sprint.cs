using System;
using AbilityUser;
using RimWorld;
using Verse;

namespace TorannMagic
{
    public class Verb_Sprint : Verb_UseAbility
    {
        internal static int Rank(TMAbilityDef def)
        {
            if (def == null) return -1;
            if (def == TorannMagicDefOf.TM_Sprint) return 0;
            if (def == TorannMagicDefOf.TM_Sprint_I) return 1;
            if (def == TorannMagicDefOf.TM_Sprint_II) return 2;
            return def == TorannMagicDefOf.TM_Sprint_III ? 3 : -1;
        }

        internal static void Burst(Pawn pawn, int rank, Func<Pawn,HediffDef,float,Hediff> add)
        {
            if (rank <= 0) return;
            add(pawn, TorannMagicDefOf.TM_EvasionHD, rank == 1 ? .25f : .1f);
            var adrenaline = add(pawn, TorannMagicDefOf.TM_AdrenalineHD, rank == 1 ? .2f : rank == 2 ? .3f : .55f);
            if (rank < 3 && adrenaline?.TryGetComp<HediffComp_SetDuration>() is HediffComp_SetDuration timed)
                timed.duration = rank == 1 ? 5 : 8;
            if (rank < 2) return;
            var allies = TM_Calc.FindAllPawnsAround(pawn.Map, pawn.Position, rank == 2 ? 5f : 10f, pawn.Faction, true);
            if (allies != null)
                foreach (var ally in allies) add(ally, TorannMagicDefOf.TM_EvasionHD, rank == 2 ? .25f : .3f);
        }

        internal static void Refund(Pawn pawn, CompAbilityUserMight owner, int rank, Func<Pawn,HediffDef,float,Hediff> add)
        {
            if (rank <= 0 || owner?.Stamina == null) return;
            float bonus = rank == 1 ? .2f : rank == 2 ? .3f : .4f;
            owner.Stamina.CurLevel += bonus;
            var regen = add(pawn, TorannMagicDefOf.TM_EnergyRegenHD, 1f + bonus);
            if (regen?.TryGetComp<HediffComp_SetDuration>() is HediffComp_SetDuration timed)
                timed.duration = rank == 1 ? 5 : rank == 2 ? 8 : 10;
        }

        private static Hediff Native(Pawn target,HediffDef def,float amount)
        {
            HealthUtility.AdjustSeverity(target,def,amount);
            return target.health.hediffSet.GetFirstHediffOfDef(def);
        }

        protected override bool TryCastShot()
        {
            if (currentTarget.Thing is not Pawn pawn || pawn.Dead) return true;
            var def = Ability.Def as TMAbilityDef;
            var comp = pawn.GetCompAbilityUserMight();
            var buff = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HediffSprint);
            if (buff?.TryGetComp<ClassProgression.HediffComp_HostedStance>()?.Projected == true)
            {
                pawn.health.RemoveHediff(buff);
                buff = null;
            }
            if (buff != null)
            {
                pawn.health.RemoveHediff(buff);
                if (comp?.MightData != null) Refund(pawn, comp, Rank(def), Native);
            }
            else
            {
                Native(pawn, TorannMagicDefOf.TM_HediffSprint, .5f + TM_Calc.GetSkillPowerLevel(CasterPawn, def));
                FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, 1f);
                if (comp?.MightData != null) Burst(pawn, Rank(def), Native);
            }
            return true;
        }
    }
}
