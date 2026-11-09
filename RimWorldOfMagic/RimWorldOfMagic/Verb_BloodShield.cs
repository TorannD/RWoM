using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using AbilityUser;
using UnityEngine;
using Verse.AI.Group;

namespace TorannMagic
{
    public class Verb_BloodShield : ClassProgression.Verb_SpiritTargetable
    {
        
        int pwrVal;
        CompAbilityUserMagic comp;
        float arcaneDmg = 1;

        bool validTarg;
        //Used for non-unique abilities that can be used with shieldbelt
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

        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body, Ability);
            var target = currentTarget.Pawn;
            if (owner == null || body?.Spawned != true || target?.Spawned != true || target.Map != body.Map || target.Dead || target == body)
                return false;
            comp = owner.GetCompAbilityUserMagic();
            if (comp?.ExistingMagicData == null) return false;
            var data = comp.MagicData;
            pwrVal = data.MagicPowerSkill_BloodShield.FirstOrDefault(x => x.label == "TM_BloodShield_pwr")?.level ?? 0;
            if (owner.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true)
                pwrVal = owner.GetCompAbilityUserMight()?.MightData.MightPowerSkill_Mimic.FirstOrDefault(x => x.label == "TM_Mimic_pwr")?.level ?? 0;
            if (ModOptions.Settings.Instance.AIHardMode && !owner.IsColonist) pwrVal = 3;
            int gift = data.MagicPowerSkill_BloodGift.FirstOrDefault(x => x.label == "TM_BloodGift_pwr")?.level ?? 0;
            arcaneDmg = comp.arcaneDmg * (1f + .1f * gift);
            ApplyBloodShield(target);
            return true;
        }

        protected override bool TryCastShot()
        {
            if (!ApplyHostedEffect() && CasterPawn != null)
                Messages.Message("TM_InvalidTarget".Translate(CasterPawn.LabelShort, TorannMagicDefOf.TM_BloodShield.label), MessageTypeDefOf.RejectInput);
            return true;
        }

        public void ApplyBloodShield(Pawn pawn)
        {
            var trial = ClassProgression.BloodMageTrials.CaptureShield(this.Ability, pawn);
            HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_BloodShieldHD"), (100f + (10f * pwrVal))*arcaneDmg);
            HediffComp_BloodShield comp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_BloodShieldHD"), false).TryGetComp<HediffComp_BloodShield>();
            comp.Link(CasterPawn, ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn, Ability));
            ClassProgression.BloodMageTrials.CompleteShield(trial);
            SoundInfo info = SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map, false), MaintenanceType.None);
            info.pitchFactor = .75f;
            SoundDefOf.EnergyShield_Reset.PlayOneShot(info);
            Effecter bloodshieldEffecter = TorannMagicDefOf.TM_BloodShieldEffecter.Spawn();
            bloodshieldEffecter.Trigger(new TargetInfo(pawn.Position, pawn.Map, false), new TargetInfo(pawn.Position, pawn.Map, false));
            bloodshieldEffecter.Cleanup();
            
        }    
    }
}
