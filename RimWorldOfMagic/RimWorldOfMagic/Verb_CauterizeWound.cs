using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;
using HarmonyLib;


namespace TorannMagic
{
    public class Verb_CauterizeWound : Verb_UseAbility
    {
        bool validTarg;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt)
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = true;
                }
                else
                {
                    //out of range
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
            Pawn pawn = currentTarget.Thing as Pawn;
            CompAbilityUserMagic comp = base.CasterPawn.GetCompAbilityUserMagic();
            if (pawn?.health?.hediffSet?.GetInjuredParts() == null || comp == null) return false;

            IEnumerable<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury.CanHealNaturally() && injury.TendableNow());

            IEnumerator<Hediff_Injury> enumerator = injuries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Hediff_Injury injury = enumerator.Current;
                if (injury == null) break;  // Shouldn't be true, but since we are changing collection in iteration just be safe.

                if (Rand.Chance(.25f / comp.arcaneDmg))
                {
                    DamageInfo dinfo = new DamageInfo(
                        DamageDefOf.Burn, Mathf.RoundToInt(injury.Severity/2), 0, -1, CasterPawn, injury.Part);
                    dinfo.SetAllowDamagePropagation(false);
                    dinfo.SetInstantPermanentInjury(true);
                    injury.Heal(100);
                    pawn.TakeDamage(dinfo);
                    TM_MoteMaker.ThrowFlames(pawn.DrawPos, pawn.Map, Rand.Range(.2f, .5f));
                    enumerator.Reset();
                }
                else
                {
                    //current.Tended(1, 1);
                    injury.Tended(1f, 1f);
                    TM_MoteMaker.ThrowFlames(pawn.DrawPos, pawn.Map, Rand.Range(.1f, .4f));
                }
            }
            enumerator.Dispose();

            IEnumerable<Hediff_MissingPart> missingParts = pawn.health.hediffSet.GetHediffsTendable()
                .OfType<Hediff_MissingPart>()
                .Where(missingPart => missingPart.Bleeding);
            foreach (Hediff_MissingPart missingPart in missingParts)
            {
                missingPart.IsFresh = false;
            }
            return false;
        }
    }
}
