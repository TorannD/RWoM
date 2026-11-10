using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;

namespace TorannMagic
{
    class Verb_Summon : ClassProgression.Verb_SpiritTargetable
    {

        
        public override bool CanHitTargetFrom(IntVec3 casterPos, LocalTargetInfo targ)
        {
            bool flag = base.UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetThing;
            bool result;
            if (flag)
            {
                result = base.CanHitTargetFrom(casterPos, targ);
            }
            else
            {
                if( targ.Cell.IsValid && !targ.Cell.Fogged(base.caster.Map))
                {
                    if ((casterPos - targ.Cell).LengthHorizontal > this.verbProps.range)
                    {
                        result = false;
                    }
                    else
                    {
                        ShootLine shootLine;
                        result = base.TryFindShootLineFromTo(casterPos, targ, out shootLine);
                    }
                }
                else
                {
                    result = false;
                }
                             
            }
            return result;
        }

        protected override bool TryCastShot()
        {
            Pawn body = CasterPawn;
            var map = body?.Map;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body, Ability);
            if (map == null || owner == null || !currentTarget.IsValid || !currentTarget.Cell.InBounds(map)) return false;
            var target = currentTarget.Thing ?? (Thing)currentTarget.Cell.GetFirstPawn(map) ?? currentTarget.Cell.GetFirstItem(map);
            if (target == null || !target.Spawned || target.Map != map) return false;
            burstShotsLeft = 0;
            if (target is Pawn pawn)
            {
                if (pawn == body)
                {
                    Messages.Message("TM_CantSummonSelf".Translate(), MessageTypeDefOf.NegativeEvent);
                    return false;
                }
                if (pawn.RaceProps.Humanlike && pawn.Faction != body.Faction &&
                    !Rand.Chance(TM_Calc.GetSpellSuccessChance(owner, pawn, true)))
                {
                    MoteMaker.ThrowText(pawn.DrawPos, map, "TM_ResistedSpell".Translate(), -1);
                    return true;
                }
                if (!Compatibility.TM_Teleport.Move(pawn, body.Position)) return false;
                if (pawn.IsColonist && !body.IsColonist) TM_Action.SpellAffectedPlayerWarning(pawn);
                return true;
            }
            if (target.def.category != ThingCategory.Item || target.Position == body.Position) return false;
            var trial = ClassProgression.ArcanistTrials.CaptureMove(Ability, target, ClassProgression.ArcanistTrials.Spell.Summon);
            var origin = target.Position;
            target.DeSpawn();
            bool placed = false;
            try
            {
                placed = GenPlace.TryPlaceThing(target, body.Position, map, ThingPlaceMode.Near, out var result);
                ClassProgression.ArcanistTrials.CompleteSummon(trial, result, placed);
                return placed;
            }
            finally
            {
                if (!placed && !target.Destroyed && !target.Spawned && target.holdingOwner == null)
                    GenSpawn.Spawn(target, origin, map);
            }
        }
    }
}
