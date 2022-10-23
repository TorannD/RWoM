using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic
{
    class Verb_ManaShield : Verb_UseAbility  
    {

        bool validTarg;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == this.caster)
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

        protected override bool TryCastShot()
        {
            bool result = false;
            Pawn pawn = this.CasterPawn;
            Map map = this.CasterPawn.Map;

            if (pawn != null && !pawn.Downed)
            {
                if(pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ManaShieldHD, false))
                {
                    using (IEnumerator<Hediff> enumerator = pawn.health.hediffSet.hediffs.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Hediff rec = enumerator.Current;
                            if (rec.def == TorannMagicDefOf.TM_ManaShieldHD)
                            {
                                pawn.health.RemoveHediff(rec);
                            }
                        }
                    }

                    TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                    TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                    TM_MoteMaker.ThrowSiphonMote(pawn.DrawPos, pawn.Map, 1f);
                }
                else
                {
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ManaShieldHD, 1f);
                    TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                    TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, 1);
                    TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                }
            }
            else
            {
                Log.Warning("failed to TryCastShot");
            }

            this.burstShotsLeft = 0;

            return result;
        }
    }
}
