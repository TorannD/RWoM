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
    public class Verb_Stoneskin : Verb_UseAbility  
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

        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            comp = caster.GetCompAbilityUserMagic();
            //MagicPowerSkill pwr = comp.MagicData.MagicPowerSkill_Stoneskin.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Stoneskin_pwr");
            //MagicPowerSkill ver = comp.MagicData.MagicPowerSkill_Stoneskin.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Stoneskin_ver");
            //pwrVal = pwr.level;
            //verVal = ver.level;
            //ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            //if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    pwrVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr").level;
            //    verVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver").level;
            //}
            //if (settingsRef.AIHardMode && !caster.IsColonist)
            //{
            //    pwrVal = 3;
            //    verVal = 3;
            //}
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);

            if (pawn != null && pawn.health != null && pawn.health.hediffSet != null)
            {
                IEnumerable<Pawn> enumerable = from geomancer in caster.Map.mapPawns.AllPawnsSpawned
                                                   where (geomancer.RaceProps.Humanlike && geomancer.story.traits.HasTrait(TorannMagicDefOf.Geomancer))
                                                   select geomancer;
                List<Pawn> geomancers = enumerable.ToList();
                for (int i = 0; i < geomancers.Count(); i++)
                {
                    CompAbilityUserMagic compGeo = geomancers[i].GetCompAbilityUserMagic();
                    if(compGeo != null && compGeo.stoneskinPawns.Contains(pawn))
                    {
                        compGeo.stoneskinPawns.Remove(pawn);
                    }
                }
                if(pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_StoneskinHD"), false))
                {
                    Hediff hediff = new Hediff();
                    hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_StoneskinHD"));
                    if (hediff.Severity < 4 + pwrVal)
                    {
                        ApplyStoneskin(pawn);
                    }
                    else
                    {
                        RemoveHediffs(pawn);
                        comp.stoneskinPawns.Remove(pawn);
                        SoundInfo info = SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map, false), MaintenanceType.None);
                        info.pitchFactor = .7f;
                        SoundDefOf.EnergyShield_Broken.PlayOneShot(info);
                        FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1.5f);
                    }
                }
                else
                {
                    ApplyStoneskin(pawn);
                }
            }
            
            return true;
        }

        public void ApplyStoneskin(Pawn pawn)
        {
            if (comp != null && !pawn.DestroyedOrNull() && !pawn.Dead && pawn.Map != null)
            {
                if (comp.StoneskinPawns.Count() < verVal + 2)
                {
                    ApplyHediffs(pawn);
                    if (!comp.StoneskinPawns.Contains(pawn))
                    {
                        comp.stoneskinPawns.Add(pawn);
                    }
                    SoundInfo info = SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map, false), MaintenanceType.None);
                    info.pitchFactor = .7f;
                    SoundDefOf.EnergyShield_Reset.PlayOneShot(info);
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1.5f);
                    Effecter stoneskinEffecter = TorannMagicDefOf.TM_Stoneskin_Effecter.Spawn();
                    stoneskinEffecter.def.offsetTowardsTarget = FloatRange.Zero;
                    stoneskinEffecter.Trigger(new TargetInfo(pawn.Position, pawn.Map, false), new TargetInfo(pawn.Position, pawn.Map, false));
                    stoneskinEffecter.Cleanup();
                }
                else
                {
                    string stoneskinPawns = "";
                    int count = comp.StoneskinPawns.Count();
                    for (int i = 0; i < count; i++)
                    {
                        if (i + 1 == count) //last name
                        {
                            stoneskinPawns += comp.StoneskinPawns[i].LabelShort;
                        }
                        else
                        {
                            stoneskinPawns += comp.StoneskinPawns[i].LabelShort + " & ";
                        }
                    }
                    if (comp.Pawn.IsColonist)
                    {
                        Messages.Message("TM_TooManyStoneskins".Translate(
                                        caster.LabelShort,
                                        verVal + 2,
                                        stoneskinPawns
                            ), MessageTypeDefOf.RejectInput);
                    }
                }
            }
        }

        private void ApplyHediffs(Pawn target)
        {
            HealthUtility.AdjustSeverity(target, HediffDef.Named("TM_StoneskinHD"), -10);
            if (pwrVal == 3)
            {
                HealthUtility.AdjustSeverity(target, HediffDef.Named("TM_StoneskinHD"), 7);
            }
            else if (pwrVal == 2)
            {
                HealthUtility.AdjustSeverity(target, HediffDef.Named("TM_StoneskinHD"), 6);
            }
            else if(pwrVal == 1)
            {
                HealthUtility.AdjustSeverity(target, HediffDef.Named("TM_StoneskinHD"), 5);
            }
            else
            {
                HealthUtility.AdjustSeverity(target, HediffDef.Named("TM_StoneskinHD"), 4);                
            }            
        }

        private void RemoveHediffs(Pawn target)
        {
            Hediff hediff = new Hediff();
            hediff = target.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_StoneskinHD"));
            target.health.RemoveHediff(hediff);
        }
    }
}
