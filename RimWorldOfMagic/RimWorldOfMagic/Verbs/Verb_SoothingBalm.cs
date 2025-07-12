using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using HarmonyLib;
using UnityEngine;
using TorannMagic.Utils;


namespace TorannMagic
{
    public class Verb_SoothingBalm : Verb_UseAbility
    {
        bool validTarg;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt)
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ != null && targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
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
            Map map = this.CasterPawn.Map;

            Pawn pawn = this.currentTarget.Thing as Pawn;
            Pawn caster = this.CasterPawn;
            CompAbilityUserMight comp = caster.GetCompAbilityUserMight();

            int verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_SoothingBalm, false);
            int pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_SoothingBalm, false);

            try
            {
                if (pawn == null || pawn.Dead || pawn.Destroyed || TM_Calc.IsUndead(pawn)) return false;
                if (pawn.Spawned && map != null && pawn.health?.hediffSet != null)
                {
                    int injuriesToHeal = 2 + Mathf.RoundToInt(.3f * verVal);
                    
                    int injuriesPerBodyPart = !CasterPawn.IsColonist && ModOptions.Settings.Instance.AIHardMode ? 5 : 1 + Mathf.RoundToInt(.2f * verVal);

                    IEnumerable<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs
                        .OfType<Hediff_Injury>()
                        .Where(injury => injury.CanHealNaturally() && injury.TendableNow())
                        .DistinctBy(injury => injury.Part, injuriesPerBodyPart)
                        .Take(injuriesToHeal);

                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_SoothingBalmHD, .3f - .03f * verVal);

                    float healAmount = pawn.IsColonist ? 4.0f + pwrVal : 10 + pwrVal * 3f;
                    foreach (Hediff_Injury injury in injuries)
                    {
                        injury.Heal(healAmount);

                        HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_SoothingBalmHD, .04f);
                        Vector3 pos = pawn.DrawPos;
                        pos.x += Rand.Range(-.3f, .3f);
                        pos.z += Rand.Range(-.3f, .3f);
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Healing_Small, pos, map, Rand.Range(.6f, 1f), .3f, .2f, .5f, 0, 0f, 0f, Rand.Range(0, 360));

                        if (!injury.TendableNow()) continue;

                        float tendQuality = Rand.Range(.5f, .7f) + pwrVal * .1f;
                        injury.Tended(tendQuality, 1f);
                        pawn.records.Increment(RecordDefOf.TimesTendedTo);
                        caster.records.Increment(RecordDefOf.TimesTendedOther);
                    }
                }
                else
                {
                    Messages.Message("TM_InvalidTarget".Translate(caster.LabelShort, Ability.Def.label), MessageTypeDefOf.NeutralEvent);
                }
            }
            catch (NullReferenceException ex)
            {

            }
            
            return false;
        }
    }
}
