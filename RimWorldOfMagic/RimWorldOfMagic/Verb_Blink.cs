using RimWorld;
using System;
using System.Linq;
using Verse;
using TorannMagic.Compatibility;

namespace TorannMagic
{
    class Verb_Blink : Verb_BLOS
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            return targ.IsValid && targ.Cell != root && TM_Teleport.CanMove(CasterPawn, targ.Cell)
                && !targ.Cell.Fogged(CasterPawn.Map) && (root - targ.Cell).LengthHorizontal < verbProps.range;
        }

        protected override bool TryCastShot()
        {
            Odyssey.ManualTravelSafety.BeginAttempt(Ability);
            burstShotsLeft = 0;
            Pawn pawn = CasterPawn;
            Pawn owner = ClassProgression.SpiritRebirthCasting.SkillOwner(pawn, Ability);
            if (owner == null) return false;
            var trialMove = ClassProgression.ArcanistTrials.CaptureMove(Ability, pawn, ClassProgression.ArcanistTrials.Spell.Blink);
            bool canPay = owner == pawn ? Odyssey.ManualTravelSafety.CanPay(Ability) :
                ClassProgression.SpiritHostedBolt.Ready(pawn, ClassProgression.SpiritHostedBolt.Find(pawn, owner, Ability.Def as TMAbilityDef));
            if (!canPay || !currentTarget.IsValid || !Odyssey.ManualTravelSafety.CanComplete(pawn, pawn.Map, currentTarget.Cell)
                || !TM_Teleport.Move(pawn, currentTarget.Cell))
            {
                Messages.Message("InvalidTargetLocation".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            if (owner == pawn) Odyssey.ManualTravelSafety.Settle(Ability);
            else Odyssey.ManualTravelSafety.SettleHosted((MagicAbility)Ability, pawn);
            ClassProgression.ArcanistTrials.CompleteBlink(trialMove);

            // Relocation never despawns the caster, so selection, draft state and
            // the executing ability job survive without being reconstructed.
            try
            {
                if (owner.IsColonist && owner.GetCompAbilityUserMagic()?.MagicData.MagicPowerSkill_Cantrips
                    .FirstOrDefault(skill => skill.label == "TM_Cantrips_ver")?.level >= 1)
                {
                    var enemies = TM_Calc.FindPawnsNearTarget(pawn, 2, currentTarget.Cell, true);
                    if (enemies != null)
                    {
                        foreach (Pawn enemy in enemies)
                            TM_Action.DamageEntities(enemy, null, 5f, DamageDefOf.Stun, pawn);
                    }
                }
            }
            catch (Exception exception)
            {
                // A secondary effect failure cannot undo an already completed move
                // or report a free, failed cast after relocation succeeded.
                TM_Log.Warning("Teleport", "Blink completed, but its secondary effect failed: " + exception);
            }
            return true;
        }
    }
}
