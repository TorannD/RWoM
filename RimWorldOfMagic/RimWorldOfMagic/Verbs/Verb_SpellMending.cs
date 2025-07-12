using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_SpellMending : Verb_UseAbility
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
            bool flag = false;

            Map map = base.CasterPawn.Map;

            Pawn hitPawn = (Pawn)this.currentTarget;
            Pawn caster = base.CasterPawn;

            if (hitPawn != null & !hitPawn.Dead && !hitPawn.RaceProps.Animal)
            {
                CompAbilityUserMagic compCaster = caster.GetCompAbilityUserMagic();
                if (compCaster != null && compCaster.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_ver").level >= 8)
                {
                    HealthUtility.AdjustSeverity(hitPawn, HediffDef.Named("SpellMendingHD"), 1.95f);
                }
                else
                {
                    HealthUtility.AdjustSeverity(hitPawn, HediffDef.Named("SpellMendingHD"), .95f);
                }
                TM_MoteMaker.ThrowTwinkle(hitPawn.DrawPos, map, 1f);

                if(compCaster != null && hitPawn.health.hediffSet.HasHediff(HediffDef.Named("SpellMendingHD"), false) && compCaster.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level >= 8)
                {
                    Hediff hd = hitPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("SpellMendingHD"));
                    if(hd != null)
                    {
                        HediffComp_SpellMending hdComp = hd.TryGetComp<HediffComp_SpellMending>();
                        if(hdComp != null)
                        {
                            hdComp.mendTickTimer = 60;
                        }
                    }
                }
            }
            else
            {
                Messages.Message("TM_InvalidTarget".Translate(
                    this.CasterPawn.LabelShort,
                    this.Ability.Def.label
                ), MessageTypeDefOf.RejectInput);
            }
            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}
