using RimWorld;
using System;
using Verse;
using AbilityUser;
using System.Linq;

namespace TorannMagic
{
    class Verb_SoulBond : ClassProgression.Verb_SpiritTargetable
    {
        bool flagSD = false;
        bool flagWD = false;
        Pawn owner;
        Pawn pawn;
        Pawn oldBondPawn;
        int verVal;
        int pwrVal;

        bool validTarg;
        //Used for non-unique abilities that can be used with shieldbelt
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == CasterPawn)
            {
                return this.verbProps.targetParams.canTargetSelf;
            }
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
                }
                else
                {
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot() => ApplyHostedEffect();

        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body,Ability);
            pawn = currentTarget.Pawn;
            var comp = owner?.GetCompAbilityUserMagic();
            if (comp == null || pawn == null || pawn.Dead || !pawn.RaceProps.Humanlike || pawn == body || pawn == owner)
            {
                Messages.Message("TM_CannotSoulBondThing".Translate(body.LabelShort),MessageTypeDefOf.RejectInput);
                return false;
            }
            if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD))
            {
                Messages.Message("TM_CannotSoulBondUndead".Translate(body.LabelShort),MessageTypeDefOf.RejectInput);
                return false;
            }
            if (pawn.Faction != body.Faction)
            {
                Messages.Message("TM_CannotSoulBondUnwilling".Translate(body.LabelShort,pawn.LabelShort),MessageTypeDefOf.RejectInput);
                return false;
            }
            flagSD = ClassProgression.ClassIdentity.DemonEffect(owner,false,owner.gender == Gender.Female);
            flagWD = ClassProgression.ClassIdentity.DemonEffect(owner,true,owner.gender == Gender.Male);
            if (!CanReplace(comp,pawn,flagSD,flagWD) || !ClassProgression.DemonSoulBond.SourceAvailable(body,owner,flagWD))
            {
                Messages.Message("TM_CannotSoulBondAnother".Translate(body.LabelShort,pawn.LabelShort),MessageTypeDefOf.RejectInput);
                return false;
            }

            var bondObservation = ClassProgression.DemonTrials.BeforeBond(this,pawn);
            oldBondPawn = comp.soulBondPawn;
            if (oldBondPawn == pawn)
            {
                RemovePreviousRecipient();
                ClassProgression.DemonSoulBond.RemoveSource(body,owner,flagWD,false);
                comp.soulBondPawn = null;
                comp.spell_ShadowCall = comp.spell_ShadowStep = false;
                comp.RemovePawnAbility(TorannMagicDefOf.TM_ShadowCall);
                comp.RemovePawnAbility(TorannMagicDefOf.TM_ShadowStep);
            }
            else
            {
                pwrVal = TM_Calc.GetSkillPowerLevel(owner,TorannMagicDefOf.TM_SoulBond);
                verVal = TM_Calc.GetSkillVersatilityLevel(owner,TorannMagicDefOf.TM_SoulBond);
                if (!ApplyHediffs()) return false;
                if (oldBondPawn != null) RemovePreviousRecipient();
                comp.soulBondPawn = pawn;
                if (oldBondPawn == null)
                {
                    comp.spell_ShadowCall = comp.spell_ShadowStep = true;
                    comp.AddPawnAbility(TorannMagicDefOf.TM_ShadowCall);
                    comp.AddPawnAbility(TorannMagicDefOf.TM_ShadowStep);
                }
            }
            ClassProgression.DemonRitual.BondChanged(comp,flagWD,oldBondPawn,comp.soulBondPawn);
            ClassProgression.DemonTrials.AfterBond(bondObservation);
            return true;
        }

        internal static bool CanReplace(CompAbilityUserMagic magic, Pawn target, bool physical, bool mental)
        {
            if (magic == null || target?.health?.hediffSet == null || (!physical && !mental)) return false;
            if (magic.soulBondPawn == target) return true;
            return !target.health.hediffSet.hediffs.Any(h =>
                (physical && h.def.defName == "TM_SoulBondPhysicalHD") || (mental && h.def.defName == "TM_SoulBondMentalHD"));
        }

        private bool ApplyHediffs()
        {
            var body = CasterPawn;
            var effect = HediffMaker.MakeHediff(ClassProgression.DemonSoulBond.Receiver(flagWD),pawn);
            effect.TryGetComp<HediffComp_SoulBondHost>().BonderPawn = owner;
            effect.Severity = .5f + verVal;
            var sender = ClassProgression.DemonSoulBond.Sender(flagWD);
            bool hadSource = body.health.hediffSet.HasHediff(sender);
            bool complete = false;
            try
            {
                pawn.health.AddHediff(effect);
                if (!pawn.health.hediffSet.hediffs.Contains(effect) || !ClassProgression.DemonSoulBond.SourceAvailable(body,owner,flagWD)) return false;
                ClassProgression.DemonSoulBond.ApplySource(body,owner,flagWD,.5f+pwrVal);
                complete = body.health.hediffSet.GetFirstHediffOfDef(sender)?.TryGetComp<HediffComp_SoulBond>()?.Owner == owner;
                return complete;
            }
            finally
            {
                if (!complete)
                {
                    if (pawn.health.hediffSet.hediffs.Contains(effect)) pawn.health.RemoveHediff(effect);
                    if (!hadSource) ClassProgression.DemonSoulBond.RemoveSource(body,owner,flagWD,false);
                }
            }
        }

        private void RemovePreviousRecipient()
        {
            var previous = oldBondPawn.health?.hediffSet?.GetFirstHediffOfDef(ClassProgression.DemonSoulBond.Receiver(flagWD));
            if (previous?.TryGetComp<HediffComp_SoulBondHost>()?.BonderPawn == owner)
                oldBondPawn.health.RemoveHediff(previous);
        }
    }
}
