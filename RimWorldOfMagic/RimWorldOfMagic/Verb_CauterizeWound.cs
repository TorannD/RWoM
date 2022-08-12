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
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            bool flag = pawn != null && pawn.health != null && pawn.health.hediffSet != null && pawn.health.hediffSet.GetInjuredParts() != null;
            if (flag && comp != null)
            {
                Enumerate:
                using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        BodyPartRecord rec = enumerator.Current;

                        IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                        Func<Hediff_Injury, bool> arg_BB_1;

                        arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                        foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                        {
                            bool flag5 = current.CanHealNaturally() && !current.IsPermanent() && current.TendableNow();
                            if (flag5)
                            {
                                if (Rand.Chance(.25f / comp.arcaneDmg))
                                {
                                    DamageInfo dinfo;
                                    dinfo = new DamageInfo(DamageDefOf.Burn, Mathf.RoundToInt(current.Severity/2), 0, (float)-1, this.CasterPawn, rec, null, DamageInfo.SourceCategory.ThingOrUnknown);
                                    dinfo.SetAllowDamagePropagation(false);
                                    dinfo.SetInstantPermanentInjury(true);                                  
                                    current.Heal(100);                                    
                                    pawn.TakeDamage(dinfo);
                                    TM_MoteMaker.ThrowFlames(pawn.DrawPos, pawn.Map, Rand.Range(.2f, .5f));
                                    goto Enumerate;
                                }
                                else
                                {
                                    //current.Tended(1, 1);
                                    current.Tended(1f, 1f);
                                    TM_MoteMaker.ThrowFlames(pawn.DrawPos, pawn.Map, Rand.Range(.1f, .4f));
                                }                                
                            }                           
                        }                        
                    }
                }
                using (IEnumerator<Hediff> enumerator1 = pawn.health.hediffSet.GetHediffsTendable().GetEnumerator())
                {
                    while (enumerator1.MoveNext())
                    {
                        Hediff rec1 = enumerator1.Current;
                        if (rec1.TendableNow() && rec1.Bleeding && rec1 is Hediff_MissingPart)
                        {
                            Traverse.Create(root: rec1).Field(name: "isFreshInt").SetValue(false);
                        }                        
                    }
                }
            }
            return false;
        }
    }
}
