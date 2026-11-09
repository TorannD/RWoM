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
    public class Verb_Stoneskin : ClassProgression.Verb_SpiritTargetable
    {
        
        int pwrVal;
        int verVal;
        CompAbilityUserMagic comp;

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
            var target = currentTarget.Pawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body,Ability);
            if (owner == null || target?.Spawned != true || target.Dead || target.Map != body?.Map) return false;
            comp = owner.GetCompAbilityUserMagic();
            if (comp?.ExistingMagicData == null) return false;
            var trial = ClassProgression.GeomancerTrials.CaptureStoneskin(Ability,target);
            if (owner == body)
            {
                pwrVal = TM_Calc.GetSkillPowerLevel(owner, Ability.Def as TMAbilityDef);
                verVal = TM_Calc.GetSkillVersatilityLevel(owner, Ability.Def as TMAbilityDef);
            }
            else
            {
                pwrVal = ClassProgression.SpiritRebirthRuntime.SavedRank(comp.ExistingMagicData,"TM_Stoneskin_pwr");
                verVal = ClassProgression.SpiritRebirthRuntime.SavedRank(comp.ExistingMagicData,"TM_Stoneskin_ver");
            }
            var def = TorannMagicDefOf.TM_StoneskinHD;
            var ward = target.health.hediffSet.GetFirstHediffOfDef(def);
            var wards = comp.StoneskinPawns;
            bool dispel = ward != null && ward.Severity >= 4 + pwrVal;
            if (!dispel && !wards.Contains(target) && wards.Count >= verVal + 2)
            {
                if (body.IsColonist) Messages.Message("TM_TooManyStoneskins".Translate(body.LabelShort,verVal + 2,
                    string.Join(" & ", wards.Select(p => p.LabelShort))), MessageTypeDefOf.RejectInput);
                return false;
            }
            if (ward != null && ward.TryGetComp<HediffComp_Stoneskin>()?.Owner == null)
                ClearLegacyOwners(target);
            if (dispel)
            {
                target.health.RemoveHediff(ward);
                wards.RemoveAll(p => p == target);
            }
            else ApplyStoneskin(target);
            ClassProgression.GeomancerTrials.CompleteStoneskin(trial);
            return true;
        }

        private static void ClearLegacyOwners(Pawn target)
        {
            // Old wards have no back-reference. The saved per-mage lists identify whoever paid their upkeep.
            foreach (var pawn in target.Map.mapPawns.AllPawnsSpawned)
            {
                pawn.GetCompAbilityUserMagic()?.stoneskinPawns?.RemoveAll(p => p == target);
                if (ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(pawn,out var soul))
                    soul.GetCompAbilityUserMagic()?.stoneskinPawns?.RemoveAll(p => p == target);
            }
        }

        protected override bool TryCastShot()
        {
            ApplyHostedEffect();
            return true;
        }

        public void ApplyStoneskin(Pawn pawn)
        {
            if (comp == null || pawn?.Spawned != true || pawn.Dead) return;
            var wards = comp.StoneskinPawns;
            if (!wards.Contains(pawn) && wards.Count >= verVal + 2) return;
            var def = TorannMagicDefOf.TM_StoneskinHD;
            var ward = pawn.health.hediffSet.GetFirstHediffOfDef(def) ?? pawn.health.AddHediff(def);
            int charges = 4 + Mathf.Clamp(pwrVal,0,3);
            ward.Severity = charges;
            ward.TryGetComp<HediffComp_Stoneskin>()?.Bind(comp.Pawn,charges);
            if (!wards.Contains(pawn)) wards.Add(pawn);
            var info = SoundInfo.InMap(new TargetInfo(pawn.Position,pawn.Map), MaintenanceType.None);
            info.pitchFactor = .7f;
            SoundDefOf.EnergyShield_Reset.PlayOneShot(info);
            FleckMaker.ThrowLightningGlow(pawn.DrawPos,pawn.Map,1.5f);
            var effecter = TorannMagicDefOf.TM_Stoneskin_Effecter.Spawn();
            try { effecter.Trigger(new TargetInfo(pawn.Position,pawn.Map),new TargetInfo(pawn.Position,pawn.Map)); }
            finally { effecter.Cleanup(); }
        }
    }
}
