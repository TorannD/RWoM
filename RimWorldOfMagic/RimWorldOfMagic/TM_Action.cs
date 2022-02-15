using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using RimWorld;
using System;
using AbilityUser;
using HarmonyLib;
using TorannMagic.Enchantment;
using TorannMagic.TMDefs;

namespace TorannMagic
{
    public static class TM_Action
    {
        public static class TM_Toils
        {
            public static void GotoAndWait(Pawn pawn, LocalTargetInfo target, int durationTicks)
            {
                if (pawn != null && target != null)
                {
                    if (pawn.drafter != null && !pawn.Drafted && pawn.GetPosture() == PawnPosture.Standing && pawn.jobs != null && pawn.CurJob != null && !pawn.CurJob.playerForced)
                    {
                        Job job = new Job(TorannMagicDefOf.JobDriver_TM_GotoAndWait, target, durationTicks, false);
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                }
            }

            public static void SendPawnTo(Pawn pawn, LocalTargetInfo target)
            {
                if (pawn != null && target != null)
                {
                    if (pawn.drafter != null && !pawn.Drafted && pawn.GetPosture() == PawnPosture.Standing && pawn.jobs != null && pawn.CurJob != null && !pawn.CurJob.playerForced)
                    {
                        Job job = new Job(JobDefOf.Goto, target);
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                }
            }

            public static void PawnWait(Pawn pawn, int duration)
            {
                duration *= 60; //seconds to ticks
                if (pawn != null)
                {
                    if (pawn.drafter != null && !pawn.Drafted && pawn.GetPosture() == PawnPosture.Standing && pawn.jobs != null && pawn.CurJob != null && !pawn.CurJob.playerForced)
                    {
                        Job job = new Job(JobDefOf.Wait);
                        job.expiryInterval = duration;
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                }
            }
        }

        public static Toil With_TM_Effects(this Toil toil, ThingDef moteDef, int frequency, float scale, float solidTime, float fadeIn, float fadeOut, int rotationRate, float velocity, float velocityAngle, float lookAngle)
        {
            toil.AddPreTickAction(delegate
            {
                if (toil.actor.Faction == Faction.OfPlayer && toil.actor.Map != null && Find.TickManager.TicksGame % frequency == 0)
                {
                    TM_MoteMaker.ThrowGenericMote(moteDef, toil.actor.DrawPos, toil.actor.Map, Rand.Range(.2f * scale, 1.1f * scale), Rand.Range(.75f * solidTime, 1.25f* solidTime), Rand.Range(.8f *fadeIn, 1.2f*fadeIn), Rand.Range(.8f * fadeOut, 1.2f * fadeOut), Mathf.RoundToInt(Rand.Range(.8f *rotationRate, 1.2f* rotationRate)), velocity, velocityAngle, Rand.Range(0,360));
                }
            });
            toil.AddFinishAction(delegate
            {
                
            });
            return toil;
        }            


        public static void DoMeleeReversal(DamageInfo dinfo, Pawn reflectingPawn)
        {
            Thing instigator = dinfo.Instigator;

            if (instigator is Pawn)
            {
                Pawn meleePawn = instigator as Pawn;
                if ((dinfo.Weapon != null && dinfo.Weapon.IsMeleeWeapon) || dinfo.WeaponBodyPartGroup != null)
                {
                    DamageInfo dinfo2 = new DamageInfo(dinfo.Def, dinfo.Amount, dinfo.ArmorPenetrationInt, dinfo.Angle, reflectingPawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown, meleePawn);
                    meleePawn.TakeDamage(dinfo2);
                }
            }
        }

        public static void DoReversal(DamageInfo dinfo, Pawn reflectingPawn)
        {
            Thing instigator = dinfo.Instigator;

            if (instigator is Pawn)
            {
                Pawn shooterPawn = instigator as Pawn;
                if (dinfo.Weapon != null && !dinfo.Weapon.IsMeleeWeapon && dinfo.WeaponBodyPartGroup == null)
                {
                    TM_CopyAndLaunchProjectile.CopyAndLaunchThing(shooterPawn.equipment.PrimaryEq.PrimaryVerb.verbProps.defaultProjectile, reflectingPawn, instigator, shooterPawn, ProjectileHitFlags.All, null);
                }
            }
            if (instigator is Building)
            {
                Building turret = instigator as Building;
                ThingDef projectile = null;

                if (turret.def.building.turretGunDef != null)
                {
                    ThingDef turretGun = turret.def.building.turretGunDef;
                    for (int i = 0; i < turretGun.Verbs.Count; i++)
                    {
                        if (turretGun.Verbs[i].defaultProjectile != null)
                        {
                            projectile = turretGun.Verbs[i].defaultProjectile;
                        }
                    }
                }

                if (projectile != null)
                {
                    TM_CopyAndLaunchProjectile.CopyAndLaunchThing(projectile, reflectingPawn, instigator, turret, ProjectileHitFlags.All, null);
                }
            }

            //GiveReversalJob(dinfo);            
        }        

        public static void DoReversalRandomTarget(DamageInfo dinfo, Pawn reflectingPawn, float minRange, float maxRange)
        {
            Thing instigator = dinfo.Instigator;

            if (instigator is Pawn)
            {
                Pawn shooterPawn = instigator as Pawn;
                if (dinfo.Weapon != null && !dinfo.Weapon.IsMeleeWeapon && dinfo.WeaponBodyPartGroup == null)
                {
                    Pawn randomTarget = null;
                    randomTarget = TM_Calc.FindNearbyEnemy(reflectingPawn, (int)maxRange);
                    if (randomTarget != null)
                    {
                        TM_CopyAndLaunchProjectile.CopyAndLaunchThing(shooterPawn.equipment.PrimaryEq.PrimaryVerb.verbProps.defaultProjectile, reflectingPawn, randomTarget, randomTarget, ProjectileHitFlags.All, null);
                    }
                }
            }
            if (instigator is Building)
            {
                Building turret = instigator as Building;
                ThingDef projectile = null;

                if (turret.def.building.turretGunDef != null)
                {
                    ThingDef turretGun = turret.def.building.turretGunDef;
                    for (int i = 0; i < turretGun.Verbs.Count; i++)
                    {
                        if (turretGun.Verbs[i].defaultProjectile != null)
                        {
                            projectile = turretGun.Verbs[i].defaultProjectile;
                        }
                    }
                }

                if (projectile != null)
                {
                    Thing target = null;
                    if ((turret.Position - reflectingPawn.Position).LengthHorizontal <= maxRange)
                    {
                        target = turret;
                    }
                    else
                    {
                        target = TM_Calc.FindNearbyEnemy(reflectingPawn, (int)maxRange);
                    }
                    TM_CopyAndLaunchProjectile.CopyAndLaunchThing(projectile, reflectingPawn, target, target, ProjectileHitFlags.All, null);
                }
            }
        }

        public static void TryCopyIdeo(Pawn sourcePawn, Pawn targetPawn)
        {
            if (ModsConfig.IdeologyActive && sourcePawn?.ideo != null && targetPawn?.ideo != null)
            {
                if (targetPawn.Ideo != sourcePawn.Ideo)
                {
                    targetPawn.ideo.SetIdeo(sourcePawn.Ideo);
                }
                if(targetPawn.ideo.Certainty != sourcePawn.ideo.Certainty)
                {
                    Traverse.Create(root: targetPawn.ideo).Field(name: "certainty").SetValue(sourcePawn.ideo.Certainty);
                }
            }
        }

        public static void DoAction_TechnoWeaponCopy(Pawn caster, Thing thing, ThingDef td = null, QualityCategory _qc = QualityCategory.Normal)
        {
            CompAbilityUserMagic comp = caster.TryGetComp<CompAbilityUserMagic>();
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            bool destroyThingAtEnd = false;
            if (thing != null && comp != null)
            {
                comp.technoWeaponThingDef = thing.def;
                CompQuality cq = thing.TryGetComp<CompQuality>();
                if (cq != null)
                {
                    comp.technoWeaponQC = cq.Quality;
                }
            }

            if (thing == null && td != null)
            {
                destroyThingAtEnd = true;
                thing = ThingMaker.MakeThing(td);
                CompQuality cq = thing.TryGetComp<CompQuality>();
                if (cq != null)
                {
                    cq.SetQuality(_qc, ArtGenerationContext.Colony);
                }
            }


            if (thing != null && thing.def != null && thing.def.IsRangedWeapon && (thing.def.techLevel >= TechLevel.Industrial || settingsRef.unrestrictedWeaponCopy) && (thing.def.Verbs.FirstOrDefault().verbClass.ToString() == "Verse.Verb_Shoot" || settingsRef.unrestrictedWeaponCopy))
            {
                int verVal = comp.MagicData.MagicPowerSkill_TechnoWeapon.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoWeapon_ver").level;
                int pwrVal = comp.MagicData.MagicPowerSkill_TechnoWeapon.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoWeapon_pwr").level;
                ThingDef newThingDef = new ThingDef();
                ThingDef newProjectileDef = new ThingDef();
                if (comp.technoWeaponDefNum != -1)
                {
                    newThingDef = ThingDef.Named("TM_TechnoWeapon_Base" + comp.technoWeaponDefNum.ToString());
                }
                else
                {
                    int highNum = 0;
                    List<Pawn> mapPawns = caster.Map.mapPawns.AllPawns.ToList();
                    for (int i = 0; i < mapPawns.Count; i++)
                    {
                        if (!mapPawns[i].DestroyedOrNull() && mapPawns[i].RaceProps.Humanlike)
                        {
                            if (comp.IsMagicUser)
                            {
                                if (comp.technoWeaponDefNum > highNum)
                                {
                                    highNum = comp.technoWeaponDefNum;
                                }
                            }
                        }
                    }
                    if (ModOptions.Constants.GetTechnoWeaponCount() > highNum)
                    {
                        highNum = ModOptions.Constants.GetTechnoWeaponCount();
                    }
                    highNum++;
                    newThingDef = ThingDef.Named("TM_TechnoWeapon_Base" + highNum.ToString());
                    comp.technoWeaponDefNum = highNum;
                    ModOptions.Constants.SetTechnoWeaponCount(highNum);
                }
                comp.technoWeaponThing = thing;
                comp.technoWeaponThingDef = thing.def;
                newThingDef.label = thing.def.label + " (modified)";
                newThingDef.description = thing.def.description + "\n\nThis weapon has been modified by a Technomancer.";
                newThingDef.graphicData.texPath = thing.def.graphicData.texPath;
                newThingDef.soundInteract = thing.def.soundInteract;

                newThingDef.SetStatBaseValue(StatDefOf.RangedWeapon_DamageMultiplier, 1f + (.02f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.RangedWeapon_Cooldown, thing.GetStatValue(StatDefOf.RangedWeapon_Cooldown) * (1 - .02f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyTouch, thing.GetStatValue(StatDefOf.AccuracyTouch) * (1 + .01f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyShort, thing.GetStatValue(StatDefOf.AccuracyShort) * (1 + .01f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyMedium, thing.GetStatValue(StatDefOf.AccuracyMedium) * (1 + .01f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyLong, thing.GetStatValue(StatDefOf.AccuracyLong) * (1 + .01f * pwrVal));

                newThingDef.Verbs.FirstOrDefault().defaultProjectile = thing.def.Verbs.FirstOrDefault().defaultProjectile;
                newThingDef.Verbs.FirstOrDefault().range = thing.def.Verbs.FirstOrDefault().range * (1f + .02f * pwrVal);
                newThingDef.Verbs.FirstOrDefault().warmupTime = thing.def.Verbs.FirstOrDefault().warmupTime * (1f - .02f * pwrVal);
                newThingDef.Verbs.FirstOrDefault().burstShotCount = Mathf.RoundToInt(thing.def.Verbs.FirstOrDefault().burstShotCount * (1f + .02f * pwrVal));
                newThingDef.Verbs.FirstOrDefault().ticksBetweenBurstShots = Mathf.RoundToInt(thing.def.Verbs.FirstOrDefault().ticksBetweenBurstShots * (1f - .02f * pwrVal));
                newThingDef.Verbs.FirstOrDefault().soundCast = thing.def.Verbs.FirstOrDefault().soundCast;
                Thing technoWeapon = ThingMaker.MakeThing(newThingDef, null);

                try
                {
                    CompQuality twcq = technoWeapon.TryGetComp<CompQuality>();
                    QualityCategory qc = thing.TryGetComp<CompQuality>().Quality;
                    twcq.SetQuality(qc, ArtGenerationContext.Colony);
                }
                catch (NullReferenceException ex)
                {
                    //ignore
                }

                GenPlace.TryPlaceThing(technoWeapon, caster.Position, caster.Map, ThingPlaceMode.Direct, null, null);
                Job job = new Job(JobDefOf.Equip, technoWeapon);
                caster.jobs.TryTakeOrderedJob(job, JobTag.ChangingApparel);
            }
            else
            {
                Log.Message("cannot copy target thing or unable to restore techno weapon");
            }
            if (destroyThingAtEnd)
            {
                thing.Destroy(DestroyMode.Vanish);
            }
        }

        public static void DoAction_PistolSpecCopy(Pawn caster, ThingWithComps thing)
        {
            CompAbilityUserMight comp = caster.TryGetComp<CompAbilityUserMight>();
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();

            if (thing != null && thing.def != null && thing.def.IsRangedWeapon)
            {
                int pwrVal = comp.MightData.MightPowerSkill_PistolSpec.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PistolSpec_pwr").level;
                ThingDef newThingDef = new ThingDef();
                ThingDef newProjectileDef = new ThingDef();
                if (comp.specWpnRegNum != -1)
                {
                    newThingDef = ThingDef.Named("TM_PistolSpec_Base" + comp.specWpnRegNum.ToString());
                }
                else
                {
                    int highNum = 0;
                    if (ModOptions.Constants.GetPistolSpecCount() > highNum)
                    {
                        highNum = ModOptions.Constants.GetPistolSpecCount();
                    }
                    highNum++;
                    newThingDef = ThingDef.Named("TM_PistolSpec_Base" + highNum.ToString());
                    comp.specWpnRegNum = highNum;
                    ModOptions.Constants.SetPistolSpecCount(highNum);
                }
                newThingDef.label = thing.def.label + "++";
                newThingDef.description = thing.def.description + "\n\nThis weapon has improved stats in the hands of a specialist.";
                newThingDef.graphic = thing.def.graphic;
                newThingDef.graphicData = thing.def.graphicData;
                newThingDef.graphicData.texPath = thing.def.graphicData.texPath;
                newThingDef.equipmentType = EquipmentType.Primary;
                newThingDef.uiIcon = thing.def.uiIcon;
                newThingDef.soundInteract = thing.def.soundInteract;
                newThingDef.equippedAngleOffset = thing.def.equippedAngleOffset;

                newThingDef.SetStatBaseValue(StatDefOf.RangedWeapon_DamageMultiplier, thing.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier) * (1f + (.03f * pwrVal)));
                newThingDef.SetStatBaseValue(StatDefOf.RangedWeapon_Cooldown, thing.GetStatValue(StatDefOf.RangedWeapon_Cooldown) * (1 - .025f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyTouch, thing.GetStatValue(StatDefOf.AccuracyTouch));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyShort, thing.GetStatValue(StatDefOf.AccuracyShort) * (1 + .015f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyMedium, thing.GetStatValue(StatDefOf.AccuracyMedium) * (1 + .005f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyLong, thing.GetStatValue(StatDefOf.AccuracyLong));

                newThingDef.Verbs.FirstOrDefault().defaultProjectile = thing.def.Verbs.FirstOrDefault().defaultProjectile;
                newThingDef.Verbs.FirstOrDefault().range = thing.def.Verbs.FirstOrDefault().range * (1f + .01f * pwrVal);
                newThingDef.Verbs.FirstOrDefault().warmupTime = thing.def.Verbs.FirstOrDefault().warmupTime * (1f - .025f * pwrVal);
                newThingDef.Verbs.FirstOrDefault().burstShotCount = Mathf.RoundToInt(thing.def.Verbs.FirstOrDefault().burstShotCount * (1f + .02f * pwrVal));
                newThingDef.Verbs.FirstOrDefault().ticksBetweenBurstShots = Mathf.RoundToInt(thing.def.Verbs.FirstOrDefault().ticksBetweenBurstShots * (1f - .03f * pwrVal));
                newThingDef.Verbs.FirstOrDefault().soundCast = thing.def.Verbs.FirstOrDefault().soundCast;
                ThingWithComps specWeapon = (ThingWithComps)ThingMaker.MakeThing(newThingDef, null);

                try
                {
                    CompQuality twcq = specWeapon.TryGetComp<CompQuality>();
                    QualityCategory qc = thing.TryGetComp<CompQuality>().Quality;
                    twcq.SetQuality(qc, ArtGenerationContext.Colony);
                }
                catch (NullReferenceException ex)
                {
                    //ignore
                }

                try
                {
                    for (int i = 0; i < thing.AllComps.Count; i++)
                    {

                        if (specWeapon.AllComps[i] is CompEquippable)
                        {
                            //CompEquippable ce = thing.AllComps[i] as CompEquippable;
                            //ce.PrimaryVerb.loadID = String.Concat(ce.PrimaryVerb.loadID, "1");

                        }
                        else
                        {
                            specWeapon.AllComps.AddDistinct(thing.AllComps[i]);
                        }
                    }
                }
                catch (NullReferenceException ex)
                {
                    //ignore
                }
                specWeapon.HitPoints = thing.HitPoints;
                //if (caster.equipment.Primary != null)
                //{
                //    Log.Message("destroying " + caster.equipment.Primary + " in pistol copy");
                //    caster.equipment.Primary.Destroy(DestroyMode.Vanish);
                //}
                caster.equipment.AddEquipment(specWeapon);
                //GenPlace.TryPlaceThing(technoWeapon, caster.Position, caster.Map, ThingPlaceMode.Direct, null, null);
                //Job job = new Job(JobDefOf.Equip, technoWeapon);
                //caster.jobs.TryTakeOrderedJob(job, JobTag.ChangingApparel);

            }
            else
            {
                Log.Message("cannot copy target thing or unable to restore techno weapon");
            }
        }

        public static void DoAction_RifleSpecCopy(Pawn caster, ThingWithComps thing)
        {
            CompAbilityUserMight comp = caster.TryGetComp<CompAbilityUserMight>();
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();

            if (thing != null && thing.def != null && thing.def.IsRangedWeapon)
            {
                int pwrVal = comp.MightData.MightPowerSkill_RifleSpec.FirstOrDefault((MightPowerSkill x) => x.label == "TM_RifleSpec_pwr").level;
                ThingDef newThingDef = new ThingDef();
                ThingDef newProjectileDef = new ThingDef();
                if (comp.specWpnRegNum != -1)
                {
                    newThingDef = ThingDef.Named("TM_RifleSpec_Base" + comp.specWpnRegNum.ToString());
                }
                else
                {
                    int highNum = 0;
                    if (ModOptions.Constants.GetRifleSpecCount() > highNum)
                    {
                        highNum = ModOptions.Constants.GetRifleSpecCount();
                    }
                    highNum++;
                    newThingDef = ThingDef.Named("TM_RifleSpec_Base" + highNum.ToString());
                    comp.specWpnRegNum = highNum;
                    ModOptions.Constants.SetRifleSpecCount(highNum);
                }
                newThingDef.label = thing.def.label + "++";
                newThingDef.description = thing.def.description + "\n\nThis weapon has improved stats in the hands of a specialist.";
                newThingDef.graphic = thing.def.graphic;
                newThingDef.graphicData = thing.def.graphicData;
                newThingDef.graphicData.texPath = thing.def.graphicData.texPath;
                newThingDef.equipmentType = EquipmentType.Primary;
                newThingDef.uiIcon = thing.def.uiIcon;
                newThingDef.soundInteract = thing.def.soundInteract;
                newThingDef.equippedAngleOffset = thing.def.equippedAngleOffset;

                newThingDef.SetStatBaseValue(StatDefOf.RangedWeapon_DamageMultiplier, thing.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier) * (1f + (.02f * pwrVal)));
                newThingDef.SetStatBaseValue(StatDefOf.RangedWeapon_Cooldown, thing.GetStatValue(StatDefOf.RangedWeapon_Cooldown) * (1 - .01f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyTouch, thing.GetStatValue(StatDefOf.AccuracyTouch));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyShort, thing.GetStatValue(StatDefOf.AccuracyShort) * (1 + .01f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyMedium, thing.GetStatValue(StatDefOf.AccuracyMedium) * (1 + .01f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyLong, thing.GetStatValue(StatDefOf.AccuracyLong) * (1 + .01f * pwrVal));

                newThingDef.Verbs.FirstOrDefault().defaultProjectile = thing.def.Verbs.FirstOrDefault().defaultProjectile;
                newThingDef.Verbs.FirstOrDefault().range = thing.def.Verbs.FirstOrDefault().range * (1f + .02f * pwrVal);
                newThingDef.Verbs.FirstOrDefault().warmupTime = thing.def.Verbs.FirstOrDefault().warmupTime * (1f - .01f * pwrVal);
                newThingDef.Verbs.FirstOrDefault().burstShotCount = Mathf.RoundToInt(thing.def.Verbs.FirstOrDefault().burstShotCount * (1f + .03f * pwrVal));
                newThingDef.Verbs.FirstOrDefault().ticksBetweenBurstShots = Mathf.RoundToInt(thing.def.Verbs.FirstOrDefault().ticksBetweenBurstShots * (1f - .02f * pwrVal));
                newThingDef.Verbs.FirstOrDefault().soundCast = thing.def.Verbs.FirstOrDefault().soundCast;
                ThingWithComps specWeapon = (ThingWithComps)ThingMaker.MakeThing(newThingDef, null);

                try
                {
                    CompQuality twcq = specWeapon.TryGetComp<CompQuality>();
                    QualityCategory qc = thing.TryGetComp<CompQuality>().Quality;
                    twcq.SetQuality(qc, ArtGenerationContext.Colony);
                }
                catch (NullReferenceException ex)
                {
                    //ignore
                }

                try
                {
                    for (int i = 0; i < thing.AllComps.Count; i++)
                    {

                        if (specWeapon.AllComps[i] is CompEquippable)
                        {
                            //CompEquippable ce = thing.AllComps[i] as CompEquippable;
                            //ce.PrimaryVerb.loadID = String.Concat(ce.PrimaryVerb.loadID, "1");

                        }
                        else
                        {
                            specWeapon.AllComps.AddDistinct(thing.AllComps[i]);
                        }
                    }
                }
                catch (NullReferenceException ex)
                {
                    //ignore
                }
                specWeapon.HitPoints = thing.HitPoints;
                //if (caster.equipment.Primary != null)
                //{
                //    caster.equipment.Primary.Destroy(DestroyMode.Vanish);
                //}
                caster.equipment.AddEquipment(specWeapon);
                //GenPlace.TryPlaceThing(technoWeapon, caster.Position, caster.Map, ThingPlaceMode.Direct, null, null);
                //Job job = new Job(JobDefOf.Equip, technoWeapon);
                //caster.jobs.TryTakeOrderedJob(job, JobTag.ChangingApparel);

            }
            else
            {
                Log.Message("cannot copy target thing or unable to restore techno weapon");
            }
        }

        //public static void DoAction_ShotgunSpecCopy(Pawn caster, ThingDef thingDef, int wpnQuality, ThingDef stuff = null)
        public static void DoAction_ShotgunSpecCopy(Pawn caster, ThingWithComps thing)
        {
            CompAbilityUserMight comp = caster.TryGetComp<CompAbilityUserMight>();
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();

            if (thing != null && thing.def != null && thing.def.IsRangedWeapon)
            {
                int pwrVal = comp.MightData.MightPowerSkill_ShotgunSpec.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ShotgunSpec_pwr").level;

                //ThingWithComps thing = (ThingWithComps)ThingMaker.MakeThing(thingDef, stuff);
                //CompQuality cq = thing.TryGetComp<CompQuality>();
                //if(cq != null)
                //{
                //    cq.SetQuality((QualityCategory)wpnQuality, ArtGenerationContext.Colony);
                //}

                ThingDef newThingDef = new ThingDef();
                ThingDef newProjectileDef = new ThingDef();
                if (comp.specWpnRegNum != -1)
                {
                    newThingDef = ThingDef.Named("TM_ShotgunSpec_Base" + comp.specWpnRegNum.ToString());
                }
                else
                {
                    int highNum = 0;
                    if (ModOptions.Constants.GetShotgunSpecCount() > highNum)
                    {
                        highNum = ModOptions.Constants.GetShotgunSpecCount();
                    }
                    highNum++;
                    newThingDef = ThingDef.Named("TM_ShotgunSpec_Base" + highNum.ToString());
                    comp.specWpnRegNum = highNum;
                    ModOptions.Constants.SetShotgunSpecCount(highNum);
                }
                newThingDef.label = thing.def.label + "++";
                newThingDef.description = thing.def.description + "\n\nThis weapon has improved stats in the hands of a specialist.";
                newThingDef.equipmentType = EquipmentType.Primary;
                newThingDef.graphic = thing.def.graphic;
                newThingDef.graphicData = thing.def.graphicData;
                newThingDef.graphicData.texPath = thing.def.graphicData.texPath;
                newThingDef.uiIcon = thing.def.uiIcon;
                newThingDef.soundInteract = thing.def.soundInteract;
                newThingDef.equippedAngleOffset = thing.def.equippedAngleOffset;

                newThingDef.SetStatBaseValue(StatDefOf.RangedWeapon_DamageMultiplier, thing.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier) * (1f + (.02f * pwrVal)));
                newThingDef.SetStatBaseValue(StatDefOf.RangedWeapon_Cooldown, thing.GetStatValue(StatDefOf.RangedWeapon_Cooldown) * (1 - .03f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyTouch, thing.GetStatValue(StatDefOf.AccuracyTouch) * (1 + .01f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyShort, thing.GetStatValue(StatDefOf.AccuracyShort) * (1 + .015f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyMedium, thing.GetStatValue(StatDefOf.AccuracyMedium) * (1 + .005f * pwrVal));
                newThingDef.SetStatBaseValue(StatDefOf.AccuracyLong, thing.GetStatValue(StatDefOf.AccuracyLong));

                newThingDef.Verbs.FirstOrDefault().defaultProjectile = thing.def.Verbs.FirstOrDefault().defaultProjectile;
                newThingDef.Verbs.FirstOrDefault().range = thing.def.Verbs.FirstOrDefault().range * (1f + .01f * pwrVal);
                newThingDef.Verbs.FirstOrDefault().warmupTime = thing.def.Verbs.FirstOrDefault().warmupTime * (1f - .03f * pwrVal);
                newThingDef.Verbs.FirstOrDefault().burstShotCount = Mathf.RoundToInt(thing.def.Verbs.FirstOrDefault().burstShotCount);
                newThingDef.Verbs.FirstOrDefault().ticksBetweenBurstShots = Mathf.RoundToInt(thing.def.Verbs.FirstOrDefault().ticksBetweenBurstShots * (1f - .02f * pwrVal));
                newThingDef.Verbs.FirstOrDefault().soundCast = thing.def.Verbs.FirstOrDefault().soundCast;
                ThingWithComps specWeapon = (ThingWithComps)ThingMaker.MakeThing(newThingDef, null);

                try
                {
                    CompQuality twcq = specWeapon.TryGetComp<CompQuality>();
                    QualityCategory qc = thing.TryGetComp<CompQuality>().Quality;
                    twcq.SetQuality(qc, ArtGenerationContext.Colony);
                }
                catch (NullReferenceException ex)
                {
                    //ignore
                }

                try
                {
                    for (int i = 0; i < thing.AllComps.Count; i++)
                    {

                        if (specWeapon.AllComps[i] is CompEquippable)
                        {
                            //CompEquippable ce = thing.AllComps[i] as CompEquippable;
                            //ce.PrimaryVerb.loadID = String.Concat(ce.PrimaryVerb.loadID, "1");

                        }
                        else
                        {
                            specWeapon.AllComps.AddDistinct(thing.AllComps[i]);
                        }
                    }
                }
                catch (NullReferenceException ex)
                {
                    //ignore
                }
                specWeapon.HitPoints = thing.HitPoints;
                //if(caster.equipment.Primary != null)
                //{
                //    caster.equipment.Primary.Destroy(DestroyMode.Vanish);
                //}
                caster.equipment.AddEquipment(specWeapon);
                //GenPlace.TryPlaceThing(technoWeapon, caster.Position, caster.Map, ThingPlaceMode.Direct, null, null);
                //Job job = new Job(JobDefOf.Equip, technoWeapon);
                //caster.jobs.TryTakeOrderedJob(job, JobTag.ChangingApparel);

            }
            else
            {
                Log.Message("cannot copy target thing or unable to restore techno weapon");
            }
        }

        public static void DamageEntities_AoE(DamageDef type, float amount, float armorPen, Pawn caster, Pawn target, Map map, float radius, bool friendlyFire = false, BodyPartRecord hitPart = null, ThingDef weapon = null, bool centerFalloff = false)
        {
            if(caster != null && target != null)
            {
                float amt = Rand.Range(.75f, 1.25f) * amount;
                if (radius > 0)
                {
                    List<Pawn> targetList = TM_Calc.FindAllPawnsAround(map, target.Position, radius);
                    if (targetList != null && targetList.Count > 0)
                    {
                        foreach (Pawn p in targetList)
                        {
                            if (centerFalloff)
                            {
                                amt = (.5f * amt) + (.5f * ((p.Position - target.Position).LengthHorizontal / radius));
                            }
                            if (friendlyFire || p.Faction != caster.Faction)
                            {                                
                                TM_Action.DamageEntities(p, hitPart, amt, armorPen, type, caster);
                            }
                        }
                    }
                }
                else
                {
                    TM_Action.DamageEntities(target, hitPart, amt, armorPen, type, caster);
                }
            }
        }

        public static void DoAction_ApplySplashDamage(DamageInfo dinfo, Pawn caster, Pawn target, Map map, int ver = 0)
        {
            bool multiplePawns = false;
            bool flag = !dinfo.InstantPermanentInjury;
            CompAbilityUserMight comp = caster.GetComp<CompAbilityUserMight>();
            MightPowerSkill eff = comp.MightData.MightPowerSkill_DragonStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_DragonStrike_eff");
            MightPowerSkill globalSkill = comp.MightData.MightPowerSkill_global_seff.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_seff_pwr");
            float actualStaminaCost = .1f * (1 - (.1f * eff.level) * (1 - (.03f * globalSkill.level)));
            if (flag && comp != null && comp.Stamina.CurLevel >= actualStaminaCost)
            {
                bool flag2 = dinfo.Instigator != null;
                if (flag2)
                {
                    bool flag3 = caster != null && caster.PositionHeld != default(IntVec3) && !caster.Downed;
                    if (flag3)
                    {
                        System.Random random = new System.Random();
                        int rnd = GenMath.RoundRandom(random.Next(0, 100));
                        if (rnd < (ver * 15))
                        {
                            target.TakeDamage(dinfo);
                            FleckMaker.ThrowMicroSparks(target.Position.ToVector3(), map);
                        }
                        target.TakeDamage(dinfo);
                        FleckMaker.ThrowMicroSparks(target.Position.ToVector3(), map);
                        for (int i = 0; i < 8; i++)
                        {
                            IntVec3 intVec = target.PositionHeld + GenAdj.AdjacentCells[i];
                            Pawn cleaveVictim = new Pawn();
                            cleaveVictim = intVec.GetFirstPawn(map);
                            if (cleaveVictim != null && cleaveVictim.Faction != caster.Faction && cleaveVictim.HostileTo(caster.Faction))
                            {
                                cleaveVictim.TakeDamage(dinfo);
                                FleckMaker.ThrowMicroSparks(cleaveVictim.Position.ToVector3(), map);
                                multiplePawns = true;
                                rnd = GenMath.RoundRandom(random.Next(0, 100));
                                if (rnd < (ver * 15))
                                {
                                    cleaveVictim.TakeDamage(dinfo);
                                    FleckMaker.ThrowMicroSparks(cleaveVictim.Position.ToVector3(), map);
                                }
                            }
                        }
                    }
                }
            }
            if (multiplePawns)
            {
                Vector3 angle = TM_Calc.GetVector(caster.DrawPos, target.DrawPos);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_DragonStrike, target.DrawPos, caster.Map, 1.1f, .1f, .01f, .1f, 650, 0, 0, (Quaternion.AngleAxis(-25, Vector3.up) * angle).ToAngleFlat());
                if (comp != null)
                {
                    comp.Stamina.CurLevel -= actualStaminaCost;
                    comp.MightUserXP += (int)(.1f * 180);
                }
            }
        }

        public static void DoAction_SabotagePawn(Pawn targetPawn, Pawn caster, float rnd, int pwrVal, float arcaneDmg, Thing instigator)
        {
            if (Rand.Chance(TM_Calc.GetSpellSuccessChance(caster, targetPawn, true)))
            {
                if (rnd <= .33f)
                {
                    TM_Action.DamageEntities(targetPawn, null, (Rand.Range(8, 15) + pwrVal) * arcaneDmg, TMDamageDefOf.DamageDefOf.TM_ElectricalBurn, instigator);
                }
                else if (rnd <= .66f)
                {
                    if (targetPawn.mindState != null)
                    {
                        targetPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "logic circuits sabotaged", true, false, null, true);
                    }
                    else
                    {
                        targetPawn.TryStartAttack(TM_Calc.FindNearbyPawn(targetPawn, 10));
                    }
                }
                else if (rnd <= 1f)
                {
                    int rndCount = Rand.Range(2, 5);
                    for (int j = 0; j < rndCount; j++)
                    {
                        TM_Action.DamageEntities(targetPawn, null, (Rand.Range(3, 5) + pwrVal) * arcaneDmg, TMDamageDefOf.DamageDefOf.TM_ElectricalBurn, instigator);
                    }
                }
            }
            else
            {
                MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "TM_ResistedSpell".Translate(), -1);
            }
        }

        public static void DoAction_HealPawn(Pawn caster, Pawn pawn, int bodypartCount, float amountToHeal)
        {
            int num = bodypartCount;
            using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    BodyPartRecord rec = enumerator.Current;
                    bool flag2 = num > 0;

                    if (flag2)
                    {
                        int num2 = bodypartCount;
                        IEnumerable<Hediff_Injury> injury_hediff = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                        Func<Hediff_Injury, bool> partInjured;

                        partInjured = ((Hediff_Injury injury) => injury.Part == rec);
                        bool healedBleeding = false;

                        foreach (Hediff_Injury current in injury_hediff.Where(partInjured))
                        {
                            bool flag4 = num2 > 0;
                            if (flag4)
                            {
                                bool flag5 = current.BleedRate > 0;
                                if (flag5)
                                {
                                    current.Heal(amountToHeal);
                                    num--;
                                    num2--;
                                    healedBleeding = true;
                                }
                            }
                        }

                        if (!healedBleeding)
                        {
                            foreach (Hediff_Injury current in injury_hediff.Where(partInjured))
                            {
                                bool flag4 = num2 > 0;
                                if (flag4)
                                {
                                    bool flag5 = !current.IsPermanent();
                                    if (flag5)
                                    {
                                        current.Heal(amountToHeal);
                                        num--;
                                        num2--;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void HealAllSickness(Pawn p)
        {
            if (p != null && p.health != null && p.health.hediffSet != null)
            {
                List<Hediff> removeHediffs = new List<Hediff>();
                removeHediffs.Clear();
                foreach (Hediff h in p.health.hediffSet.hediffs)
                {
                    if(h.def.makesSickThought)
                    {
                        removeHediffs.Add(h);
                    }
                }
                foreach(Hediff r in removeHediffs)
                {
                    p.health.RemoveHediff(r);
                }
            }
        }

        public static Thing SingleSpawnLoop(Pawn caster, SpawnThings spawnables, IntVec3 position, Map map, int duration, bool temporary, bool hostile = false, Faction spawnableFaction = null, bool hasFaction = true, ThingDef makeFromStuff = null)
        {
            bool flag = spawnables.def != null;
            Thing thing = null;
            if (flag)
            {
                Faction faction = spawnableFaction;
                if (hasFaction)
                {
                    faction = TM_Action.ResolveFaction(caster, spawnables, spawnableFaction, hostile);
                }
                bool flag2 = spawnables.def.race != null;
                if (flag2)
                {
                    bool flag3 = spawnables.kindDef == null;
                    if (flag3)
                    {
                        Log.Error("Missing kinddef");
                    }
                    else
                    {
                        thing = TM_Action.SpawnPawn(caster, spawnables, faction, position, duration, map);
                    }
                }
                else
                {
                    ThingDef def = spawnables.def;
                    ThingDef stuff = null;
                    bool madeFromStuff = def.MadeFromStuff;
                    if (madeFromStuff)
                    {
                        if (makeFromStuff != null)
                        {
                            stuff = makeFromStuff;
                        }
                        else
                        {
                            stuff = ThingDefOf.Steel;
                        }
                    }
                    thing = ThingMaker.MakeThing(def, stuff);
                    if (thing != null)
                    {
                        if (thing.def.defName != "Portfuel" && spawnables.kindDef != null)
                        {
                            thing.SetFaction(faction, null);
                        }
                        else if(faction != null)
                        {
                            thing.SetFaction(faction, null);
                        }
                        CompSummoned bldgComp = thing.TryGetComp<CompSummoned>();
                        if (bldgComp != null)
                        {
                            bldgComp.TicksToDestroy = duration;
                            bldgComp.Temporary = temporary;
                        }
                        GenSpawn.Spawn(thing, position, map, Rot4.North, WipeMode.Vanish, false);
                    }
                }
            }
            return thing;
        }

        public static Faction ResolveFaction(Pawn caster, SpawnThings spawnables, Faction spawnAbleFaction, bool hostile = false)
        {
            FactionDef val = FactionDefOf.PlayerColony;
            Faction obj = null;

            if (!hostile)
            {
                return spawnAbleFaction;
            }

            obj = ((caster != null) ? caster.Faction : null);
            Faction val2 = obj;
            if (hostile)
            {
                if (obj != null && !val2.IsPlayer)
                {
                    return val2;
                }
                if (spawnables.factionDef != null)
                {
                    val = spawnables.factionDef;
                }
                if (spawnables.kindDef != null && spawnables.kindDef.defaultFactionType != null)
                {
                    val = spawnables.kindDef.defaultFactionType;
                }
                if (val != null)
                {
                    return FactionUtility.DefaultFactionFrom(val);
                }
                else
                {
                    return Find.FactionManager.RandomEnemyFaction(true, true, true);
                }
            }
            if (caster != null && caster.Faction != null)
            {
                return caster.Faction;
            }
            return Find.FactionManager.AllFactionsVisible.RandomElement();

        }

        public static TMPawnSummoned SpawnPawn(Pawn caster, SpawnThings spawnables, Faction faction, IntVec3 position, int duration, Map map)
        {
           TMPawnSummoned newPawn = (TMPawnSummoned)PawnGenerator.GeneratePawn(spawnables.kindDef, faction);
            newPawn.validSummoning = true;
            if (caster != null)
            {
                newPawn.Spawner = caster;
            }
            newPawn.Temporary = spawnables.temporary;
            newPawn.TicksToDestroy = duration;
            //Faction val = default(Faction);
            //int num;
            //if (newPawn.Faction != Faction.OfPlayerSilentFail)
            //{
            //    Faction obj = null;

            //    obj = ((caster != null) ? caster.Faction : null);

            //    val = obj;
            //    num = ((obj != null) ? 1 : 0);
            //}
            //else
            //{
            //    num = 0;
            //}
            //if (num != 0)
            //{
            //    newPawn.SetFaction(val, null);
            //}
            GenSpawn.Spawn(newPawn, position, map, 0);

            if (newPawn.Faction != null && newPawn.Faction != Faction.OfPlayer)
            {
                Lord lord = null;
                if (newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction).Any((Pawn p) => p != newPawn))
                {
                    Predicate<Thing> validator = (Thing p) => p != newPawn && ((Pawn)p).GetLord() != null;
                    Pawn p2 = (Pawn)GenClosest.ClosestThing_Global(newPawn.Position, newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction), 99999f, validator, null);
                    lord = p2.GetLord();
                }
                bool flag4 = lord == null;
                if (flag4)
                {
                    LordJob_DefendPoint lordJob = new LordJob_DefendPoint(newPawn.Position);
                    lord = LordMaker.MakeNewLord(faction, lordJob, newPawn.Map, null);
                }
                else
                {
                    try
                    {
                        newPawn.mindState.duty = new PawnDuty(DutyDefOf.Defend);
                    }
                    catch
                    {
                        Log.Message("error attempting to assign a duty to summoned object");
                    }
                }
                lord.AddPawn(newPawn);
            }
            return newPawn;
        }

        public static Pawn PolymorphPawn(Pawn caster, Pawn original, Pawn polymorphFactionPawn, SpawnThings spawnables, IntVec3 position, bool temporary, int duration, Faction fac = null)
        {
            Pawn polymorphPawn = null;
            bool flag = spawnables.def != null;
            if (flag)
            {
                Faction faction = TM_Action.ResolveFaction(polymorphFactionPawn, spawnables, fac);
                bool flag2 = spawnables.def.race != null;
                if (flag2)
                {
                    bool flag3 = spawnables.kindDef == null;
                    if (flag3)
                    {
                        Log.Error("Missing kinddef");
                    }
                    else
                    {
                        try
                        {
                            if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                            {
                                ModCheck.GiddyUp.ForceDismount(original);
                            }
                        }
                        catch
                        {

                        }

                        Pawn newPawn = new Pawn();

                        newPawn = (Pawn)PawnGenerator.GeneratePawn(spawnables.kindDef, faction);
                        newPawn.AllComps.Add(new CompPolymorph());
                        CompPolymorph compPoly = newPawn.GetComp<CompPolymorph>();
                        //CompProperties_Polymorph props = new CompProperties_Polymorph();
                        //compPoly.Initialize(props);

                        if (compPoly != null)
                        {
                            compPoly.ParentPawn = newPawn;
                            compPoly.Spawner = caster;
                            compPoly.Temporary = temporary;
                            compPoly.TicksToDestroy = duration;
                            compPoly.Original = original;
                        }
                        else
                        {
                            Log.Message("CompPolymorph was null.");
                        }

                        try
                        {
                            GenSpawn.Spawn(newPawn, position, original.Map);
                            polymorphPawn = newPawn;

                            polymorphPawn.drafter = new Pawn_DraftController(polymorphPawn);
                            polymorphPawn.equipment = new Pawn_EquipmentTracker(polymorphPawn);
                            polymorphPawn.story = new Pawn_StoryTracker(polymorphPawn);
                            if (original.workSettings != null)
                            {
                                polymorphPawn.workSettings = new Pawn_WorkSettings(polymorphPawn);
                                DefMap<WorkTypeDef, int> priorities = Traverse.Create(root: original.workSettings).Field(name: "priorities").GetValue<DefMap<WorkTypeDef, int>>();
                                priorities = new DefMap<WorkTypeDef, int>();
                                priorities.SetAll(0);
                                Traverse.Create(root: polymorphPawn.workSettings).Field(name: "priorities").SetValue(priorities);
                            }

                            //polymorphPawn.apparel = new Pawn_ApparelTracker(polymorphPawn);
                            //polymorphPawn.mindState = new Pawn_MindState(polymorphPawn);
                            //polymorphPawn.thinker = new Pawn_Thinker(polymorphPawn);
                            //polymorphPawn.jobs = new Pawn_JobTracker(polymorphPawn);
                            //polymorphPawn.records = new Pawn_RecordsTracker(polymorphPawn);
                            //polymorphPawn.skills = new Pawn_SkillTracker(polymorphPawn);
                            //PawnComponentsUtility.AddAndRemoveDynamicComponents(polymorphPawn, true);

                            polymorphPawn.Name = original.Name;
                            polymorphPawn.gender = original.gender;

                            if (original.health.hediffSet.HasHediff(HediffDef.Named("TM_SoulBondPhysicalHD")) || original.health.hediffSet.HasHediff(HediffDef.Named("TM_SoulBondMentalHD")))
                            {
                                TM_Action.TransferSoulBond(original, polymorphPawn);
                            }
                        }
                        catch (NullReferenceException ex)
                        {
                            Log.Message("TM_Exception".Translate(
                                caster.LabelShort,
                                ex.ToString()
                                ));
                            polymorphPawn = null;
                        }
                        if (polymorphPawn != null && newPawn.Faction != null && newPawn.Faction != Faction.OfPlayer)
                        {
                            Lord lord = null;
                            if (newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction).Any((Pawn p) => p != newPawn))
                            {
                                Predicate<Thing> validator = (Thing p) => p != newPawn && ((Pawn)p).GetLord() != null;
                                Pawn p2 = (Pawn)GenClosest.ClosestThing_Global(newPawn.Position, newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction), 99999f, validator, null);
                                lord = p2.GetLord();
                            }
                            bool flag4 = lord == null;
                            if (flag4)
                            {
                                LordJob_DefendPoint lordJob = new LordJob_DefendPoint(newPawn.Position);
                                lord = LordMaker.MakeNewLord(faction, lordJob, original.Map, null);
                            }
                            try
                            {
                                lord.AddPawn(newPawn);
                            }
                            catch (NullReferenceException ex)
                            {
                                if (lord != null)
                                {
                                    LordJob_AssaultColony lordJob = new LordJob_AssaultColony(faction, false, false, false, false);
                                    lord = LordMaker.MakeNewLord(faction, lordJob, original.Map, null);
                                }
                            }

                        }
                    }
                }
                else
                {
                    Log.Message("Missing race");
                }
            }
            return polymorphPawn;
        }

        public static void TransferSoulBond(Pawn bondedPawn, Pawn polymorphedPawn)
        {
            Hediff bondHediff = null;
            bondHediff = bondedPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_SoulBondPhysicalHD"), false);
            if (bondHediff != null)
            {
                HediffComp_SoulBondHost comp = bondHediff.TryGetComp<HediffComp_SoulBondHost>();
                if (comp != null)
                {
                    comp.polyHost = polymorphedPawn;
                }
            }
            bondHediff = null;

            bondHediff = bondedPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_SoulBondMentalHD"), false);
            if (bondHediff != null)
            {
                HediffComp_SoulBondHost comp = bondHediff.TryGetComp<HediffComp_SoulBondHost>();
                if (comp != null)
                {
                    comp.polyHost = polymorphedPawn;
                }
            }
        }

        public static SpawnThings AssignRandomCreatureDef(SpawnThings spawnthing, int combatPowerMin, int combatPowerMax)
        {
            IEnumerable<PawnKindDef> enumerable = from def in DefDatabase<PawnKindDef>.AllDefs
                                                  where (def.combatPower >= combatPowerMin && def.combatPower <= combatPowerMax && def.race != null && def.race.race != null && def.race.race.thinkTreeMain.ToString() == "Animal")
                                                  select def;

            foreach (PawnKindDef current in enumerable)
            {
                //Log.Message("random creature includes " + current.defName + " race of " + current.race.defName);
            }
            PawnKindDef assignDef = enumerable.RandomElement();
            spawnthing.kindDef = assignDef;
            spawnthing.def = assignDef.race;
            return spawnthing;
        }

        public static void RemoveBodypart(Pawn p, BodyPartRecord part)
        {
            if(p != null && p.health != null && p.health.hediffSet != null)
            {
                HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(TMDamageDefOf.DamageDefOf.TM_PartRemoval, p, part);
                Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, p);
                hediff_Injury.Part = part;
                hediff_Injury.Severity = part.def.GetMaxHealth(p);
                p.health.AddHediff(hediff_Injury);
            }
        }

        public static void DamageEntities(Thing victim, BodyPartRecord hitPart, float amt, DamageDef type, Thing instigator)
        {
            DamageInfo dinfo;
            amt = Rand.Range(amt * .75f, amt * 1.25f);
            dinfo = new DamageInfo(type, amt, 0, (float)-1, instigator, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
            dinfo.SetAllowDamagePropagation(false);
            victim.TakeDamage(dinfo);
        }

        public static void DamageEntities(Thing victim, BodyPartRecord hitPart, float amt, float armorPenetration, DamageDef type, Thing instigator)
        {
            DamageInfo dinfo;
            amt = Rand.Range(amt * .75f, amt * 1.25f);
            dinfo = new DamageInfo(type, amt, armorPenetration, (float)-1, instigator, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
            dinfo.SetAllowDamagePropagation(false);
            victim.TakeDamage(dinfo);
        }

        public static void DamageUndead(Pawn undead, float amt, Thing instigator)
        {
            DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Holy, Rand.Range(amt * 1f, amt * 1.4f), 1, -1, instigator);
            for (int i = 0; i < 4; i++)
            {
                if (undead != null && !undead.Destroyed && !undead.Dead)
                {
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Holy, undead.DrawPos, undead.Map, Rand.Range(.5f, .8f), .1f, (.1f * i), .5f - (.1f * i), Rand.Range(-400, 400), Rand.Range(.5f, 1f), Rand.Range(0, 360), Rand.Range(0, 360));
                }
            }
            SoundInfo info = SoundInfo.InMap(new TargetInfo(undead.Position, undead.Map, false), MaintenanceType.None);
            TorannMagicDefOf.TM_FireWooshSD.PlayOneShot(info);
            TM_Action.DoEffecter(TorannMagicDefOf.TM_HolyImplosion, undead.Position, undead.Map);
            undead.TakeDamage(dinfo);
        }

        public static void TendWithoutNotice(Hediff rec, float quality, float maxQuality)
        {
            HediffComp_TendDuration hdc_td = rec.TryGetComp<HediffComp_TendDuration>();
            if (hdc_td != null)
            {
                hdc_td.tendQuality = Mathf.Clamp(quality + Rand.Range(-0.25f, 0.25f), 0f, maxQuality);
                float ttq = Traverse.Create(root: hdc_td).Field(name: "totalTendQuality").GetValue<float>();
                Traverse.Create(root: hdc_td).Field(name: "totalTendQuality").SetValue(ttq + hdc_td.tendQuality);
                if (hdc_td.TProps.TendIsPermanent)
                {
                    hdc_td.tendTicksLeft = 1;
                }
                else
                {
                    hdc_td.tendTicksLeft = Mathf.Max(0, hdc_td.tendTicksLeft) + hdc_td.TProps.TendTicksFull;
                }
                rec.pawn.health.Notify_HediffChanged(hdc_td.parent);
            }
        }

        public static void DoEffecter(EffecterDef effecterDef, IntVec3 position, Map map)
        {
            Effecter effecter = effecterDef.Spawn();
            effecter.Trigger(new TargetInfo(position, map, false), new TargetInfo(position, map, false));
            effecter.Cleanup();
        }

        /// <summary>
        /// Actions used to perform Chronomancer ability "Recall"
        /// Loaded in static to allow immediate execution when downed or dead
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="comp"></param>
        /// <param name="deathTrigger"></param>
        public static void DoRecall(Pawn pawn, CompAbilityUserMagic comp, bool deathTrigger)
        {
            try
            {
                if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                {
                    ModCheck.GiddyUp.ForceDismount(pawn);
                }
            }
            catch
            {

            }
            FleckMaker.ThrowSmoke(pawn.DrawPos, pawn.Map, 1.4f);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_AlterFate, pawn.DrawPos, pawn.Map, 1.6f, .2f, .1f, .8f, -500, 0, 0, Rand.Range(0, 360));
            Effecter RecallFromEffect = TorannMagicDefOf.TM_RecallFromED.Spawn();
            RecallFromEffect.Trigger(new TargetInfo(pawn), new TargetInfo(pawn));
            RecallFromEffect.Cleanup();
            RecallHediffs(pawn, comp);
            RecallNeeds(pawn, comp);
            RecallPosition(pawn, comp);
            ResetPowers(pawn, comp);
            comp.recallSet = false;
            comp.recallSpell = false;
            comp.RemovePawnAbility(TorannMagicDefOf.TM_Recall);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_AlterFate, pawn.DrawPos, pawn.Map, 1.6f, .2f, .1f, .8f, 500, 0, 0, Rand.Range(0, 360));
            Effecter RecallToEffect = TorannMagicDefOf.TM_RecallToED.Spawn();
            RecallToEffect.Trigger(new TargetInfo(pawn), new TargetInfo(pawn));
            RecallToEffect.Cleanup();
            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_HediffInvulnerable, .02f);
            if (pawn.Dead)
            {
                ResurrectionUtility.Resurrect(pawn);
                deathTrigger = true;
            }
            if (deathTrigger && Rand.Chance(.5f))
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Wander_Psychotic);
            }
        }

        private static void RecallHediffs(Pawn pawn, CompAbilityUserMagic comp)
        {
            if (comp.recallInjuriesList != null && comp.recallHediffList != null)
            {
                for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
                {
                    if (!pawn.health.hediffSet.hediffs[i].IsPermanent() && pawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_MagicUserHD && !pawn.health.hediffSet.hediffs[i].def.defName.Contains("TM_HediffEnchantment") &&
                        !pawn.health.hediffSet.hediffs[i].def.defName.Contains("TM_Artifact") && pawn.health.hediffSet.hediffs[i].def.defName != "PsychicAmplifier" && pawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_MightUserHD &&
                        pawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_BloodHD && pawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_ChiHD && pawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_PsionicHD)
                    {
                        if (!(pawn.health.hediffSet.hediffs[i] is Hediff_MissingPart) && !(pawn.health.hediffSet.hediffs[i] is Hediff_AddedPart))
                        {
                            //Log.Message("removing " + pawn.health.hediffSet.hediffs[i].Label + " at severity " + pawn.health.hediffSet.hediffs[i].Severity);
                            Hediff hediff = pawn.health.hediffSet.hediffs[i];
                            pawn.health.RemoveHediff(hediff);
                            i--;
                        }
                    }
                }
                for (int i = 0; i < comp.recallHediffList.Count; i++)
                {
                    //Log.Message("adding " + comp.recallHediffList[i].Label + " at severity " + comp.recallHediffList[i].Severity);
                    Hediff hdwc = comp.recallHediffList[i];
                    pawn.health.AddHediff(hdwc.def, hdwc.Part, null, null);
                    //Log.Message("adding hediff of " + hdwc.def.defName + " + with severity " + comp.recallHediffDefSeverityList[i]);
                    Hediff hd = pawn.health.hediffSet.GetFirstHediffOfDef(hdwc.def);
                    hd.Severity = comp.recallHediffDefSeverityList[i];
                    //Log.Message("verifying hd severity is " + hd.Severity);
                    if(comp.recallHediffDefTicksRemainingList[i] > 0)
                    {
                        HediffComp_Disappears hdc_d = hd.TryGetComp<HediffComp_Disappears>();
                        if(hdc_d != null)
                        {
                            //Log.Message("hediff has disappears comp, loading disappear tick of " + comp.recallHediffDefTicksRemainingList[i]);
                            hdc_d.ticksToDisappear = comp.recallHediffDefTicksRemainingList[i];
                        }
                    }
                    //HediffDef hdDef = comp.recallHediffList[i].def;
                    //foreach (HediffComp hdc in hdwc.comps)
                    //{
                    //    if (hdwc.comps)
                    //}
                }
                for (int i = 0; i < comp.recallInjuriesList.Count; i++)
                {
                    //Log.Message("adding injury " + comp.recallInjuriesList[i].Label + " at severity " + comp.recallInjuriesList[i].Severity);
                    pawn.health.AddHediff(comp.recallInjuriesList[i]);
                }
                comp.recallHediffList.Clear();
                comp.recallInjuriesList.Clear();
            }

        }

        private static void RecallNeeds(Pawn pawn, CompAbilityUserMagic comp)
        {
            for (int i = 0; i < pawn.needs.AllNeeds.Count; i++)
            {
                bool hasNeed = false;
                for (int j = 0; j < comp.recallNeedValues.Count; j++)
                {
                    if (comp.recallNeedDefnames[j] == pawn.needs.AllNeeds[i].def.defName)
                    {
                        //Log.Message("setting " + pawn.needs.AllNeeds[i].def.defName + " from " + pawn.needs.AllNeeds[i].CurLevel + " to " + comp.recallNeedValues[j]);
                        pawn.needs.AllNeeds[i].CurLevel = comp.recallNeedValues[j];
                        hasNeed = true;
                    }
                }
                if (!hasNeed)
                {
                    //Log.Message("removing need " + pawn.needs.AllNeeds[i].def.defName);
                    pawn.needs.AllNeeds.Remove(pawn.needs.AllNeeds[i]);
                }
            }
            pawn.needs.AddOrRemoveNeedsAsAppropriate();
            comp.recallNeedDefnames.Clear();
            comp.recallNeedValues.Clear();
        }

        private static void RecallPosition(Pawn pawn, CompAbilityUserMagic comp)
        {
            bool draftFlag = pawn.Drafted;
            bool selectFlag = Find.Selector.IsSelected(pawn);
            pawn.DeSpawn();
            GenPlace.TryPlaceThing(pawn, comp.recallPosition, comp.recallMap, ThingPlaceMode.Near);
            pawn.drafter.Drafted = draftFlag;
            if (selectFlag && ModOptions.Settings.Instance.cameraSnap)
            {
                CameraJumper.TryJumpAndSelect(pawn);
            }
            comp.recallPosition = default(IntVec3);
        }

        private static void ResetPowers(Pawn pawn, CompAbilityUserMagic comp)
        {
            foreach (PawnAbility current in comp.AbilityData.Powers)
            {
                if (current.Def != TorannMagicDefOf.TM_Recall && current.Def != TorannMagicDefOf.TM_TimeMark)
                {
                    current.CooldownTicksLeft = 0;
                }
                else if (current.Def == TorannMagicDefOf.TM_TimeMark)
                {
                    current.CooldownTicksLeft = Mathf.RoundToInt(current.MaxCastingTicks * comp.coolDown);
                }
            }
        }

        public static void ConsumeManaXP(Pawn p, float mp, float xpMultiplier = 1f, bool applyArcaneWeakness = true)
        {
            if (p != null)
            {
                CompAbilityUserMagic comp = p.GetComp<CompAbilityUserMagic>();
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (comp != null && comp.Mana != null)
                {
                    int xpNum = (int)((mp * 300) * comp.xpGain * settingsRef.xpMultiplier * xpMultiplier);
                    comp.MagicUserXP += xpNum;
                    MoteMaker.ThrowText(p.DrawPos, p.MapHeld, "XP +" + xpNum, -1f);
                    mp *= comp.mpCost;
                    if (applyArcaneWeakness)
                    {
                        comp.Mana.UseMagicPower(mp);
                    }
                    else
                    {
                        comp.Mana.CurLevel = Mathf.Clamp(comp.Mana.CurLevel - mp, 0, comp.Mana.MaxLevel);
                    }
                }
            }
        }

        public static void PawnActionDelay(Pawn pawn, int duration, LocalTargetInfo target, Verb verb)
        {
            //pawn.stances.SetStance(new Stance_Busy(duration, target, verb));
            pawn.stances.SetStance(new Stance_Cooldown(duration, target, verb));
        }

        public static void ForceFactionDiscoveryAndRelation(FactionDef fDef)
        {
            foreach (FactionDef allDefs in DefDatabase<FactionDef>.AllDefs)
            {
                if (allDefs == fDef)
                {
                    List<Faction> allFactions = Find.FactionManager.AllFactions.ToList();
                    bool flagList = false;
                    bool flagRelation = false;
                    for (int i = 0; i < allFactions.Count; i++)
                    {
                        if (allFactions[i].def.defName == fDef.defName)
                        {
                            flagList = true;
                            if (allFactions[i].RelationWith(Faction.OfPlayer, true) != null)
                            {
                                flagRelation = true;
                            }
                        }
                    }
                    if (!flagList)
                    {
                        Faction f = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(fDef));
                        Find.FactionManager.Add(f);
                        //foreach (Map map in Find.Maps)
                        //{
                        //    map.pawnDestinationReservationManager.RegisterFaction(f);
                        //}
                    }
                    if (!flagRelation)
                    {
                        Faction f = Find.FactionManager.FirstFactionOfDef(fDef);
                        if (fDef.CanEverBeNonHostile)
                        {
                            f.TryAffectGoodwillWith(Faction.OfPlayerSilentFail, -200, false, false, null, null);
                        }
                        else
                        {
                            f.TryAffectGoodwillWith(Faction.OfPlayerSilentFail, 0, false, false, null, null);
                        }
                    }
                }
            }
        }

        public static void PromoteWanderer(Pawn pawn)
        {
            RemoveTrait(pawn, TorannMagicDefOf.TM_Gifted);
            pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Wanderer, 0, false));
            pawn.needs.AddOrRemoveNeedsAsAppropriate();
        }

        public static void PromoteWayfarer(Pawn pawn)
        {
            RemoveTrait(pawn, TorannMagicDefOf.PhysicalProdigy);
            pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Wayfarer, 0, false));
            pawn.needs.AddOrRemoveNeedsAsAppropriate();
        }

        public static void RemoveTrait(Pawn pawn, TraitDef trait)
        {
            List<Trait> allTraits = pawn.story.traits.allTraits;

            for (int i = 0; i < allTraits.Count; i++)
            {
                if (allTraits[i].def == trait)
                {
                    allTraits.Remove(allTraits[i]);
                    break;
                }
            }
        }

        public static void TransmutateEffects(IntVec3 position, Pawn p)
        {
            Vector3 rndPos = position.ToVector3Shifted();
            FleckMaker.ThrowHeatGlow(position, p.Map, 1f);
            for (int i = 0; i < 6; i++)
            {
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(-.5f, .5f);
                rndPos.y += Rand.Range(.3f, 1.3f);
                FleckMaker.ThrowSmoke(rndPos, p.Map, Rand.Range(.7f, 1.1f));
                FleckMaker.ThrowLightningGlow(position.ToVector3Shifted(), p.Map, 1.4f);
            }
        }

        public static void SpellAffectedPlayerWarning(Pawn p)
        {
            Vector3 rndPos = p.DrawPos;
            rndPos.z += .5f;
            for (int i = 0; i < 3; i++)
            {
                float angle = Rand.Range(-30, 30);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ExclamationRed, rndPos, p.Map, .4f, .6f, .05f, .25f, Rand.Range(-20, 20), 1f, angle, angle);
                FleckMaker.ThrowLightningGlow(p.DrawPos, p.Map, .8f);
            }
            SoundInfo info = SoundInfo.InMap(new TargetInfo(p.Position, p.Map, false), MaintenanceType.None);
            info.pitchFactor = .6f;
            info.volumeFactor = 2f;
            SoundDefOf.TinyBell.PlayOneShot(info);
        }

        public static bool DoWildSurge(Pawn p, CompAbilityUserMagic comp, MagicAbility ability, TMAbilityDef abilityDef, LocalTargetInfo target, bool canDoBad = true)
        {
            bool completeJob = false;

            float rnd = Rand.Range(0f, 1f);
            float pwrVal = (comp.MagicData.MagicPowerSkill_ChaosTradition.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ChaosTradition_pwr").level * .1f);
            float good = .2f + pwrVal;
            float bad = (.5f - pwrVal) + good;
            string surgeText = "";
            if (rnd < good)
            {
                int rndGood = Rand.RangeInclusive(0, 2);
                switch (rndGood)
                {
                    case 0:
                        List<Pawn> nearPawns = TM_Calc.FindPawnsNearTarget(p, 4, p.Position, false);
                        if (nearPawns != null && nearPawns.Count > 0)
                        {
                            for (int i = 0; i < nearPawns.Count; i++)
                            {
                                TM_Action.DoAction_HealPawn(p, nearPawns[i], 1, 10);
                            }
                        }
                        TM_MoteMaker.MakePowerBeamMote(p.Position, p.Map, 4 * 6f, 1.2f, 2f, .3f, 1f);
                        surgeText = "Healing Wave";
                        completeJob = true;
                        break;
                    case 1:
                        comp.Mana.CurLevel += comp.ActualManaCost(abilityDef);
                        TM_MoteMaker.ThrowRegenMote(p.DrawPos, p.Map, 1.2f);
                        surgeText = "Zero Cost";
                        completeJob = true;
                        break;
                    case 2:
                        ability.CooldownTicksLeft = 0;
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PowerWave, p.DrawPos, p.Map, .8f, .2f, .1f, .1f, 0, 1f, 0, Rand.Chance(.5f) ? 0 : 180);
                        surgeText = "Ability Reset";
                        completeJob = true;
                        break;
                }
            }
            else if ((rnd < bad) && canDoBad)
            {
                int rndBad = Rand.RangeInclusive(0, 3);
                switch (rndBad)
                {
                    case 0:
                        GenExplosion.DoExplosion(p.Position, p.Map, 5f, DamageDefOf.Bomb, p, Rand.Range(8, 12), 1f, null, null, null, null, null, 0, 1, false, null, 0, 1, 0, true);
                        surgeText = "Explosion";
                        break;
                    case 1:
                        List<Pawn> allPawns = p.Map.mapPawns.AllPawnsSpawned;
                        for (int i = 0; i < allPawns.Count; i++)
                        {
                            if (TM_Calc.IsMagicUser(allPawns[i]))
                            {
                                CompAbilityUserMagic apComp = allPawns[i].TryGetComp<CompAbilityUserMagic>();
                                if (apComp != null)
                                {
                                    for (int j = 0; j < apComp.AbilityData.AllPowers.Count; j++)
                                    {
                                        MagicAbility ma = apComp.AbilityData.AllPowers[j] as MagicAbility;
                                        if (ma != null)
                                        {
                                            ma.CooldownTicksLeft = (int)(ma.Def.MainVerb.SecondsToRecharge * 60);
                                        }
                                    }
                                }
                            }
                        }
                        surgeText = "Delay Magic";
                        break;
                    case 2:
                        HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_MuteHD, Rand.Range(5, 8));
                        TM_Action.TransmutateEffects(p.Position, p);
                        surgeText = "Muted";
                        break;
                    case 3:
                        List<IntVec3> cellList = new List<IntVec3>();
                        cellList.Clear();
                        cellList = GenRadial.RadialCellsAround(p.Position, 6, true).ToList();
                        for (int i = 0; i < cellList.Count; i++)
                        {
                            if (cellList[i].IsValid && cellList[i].InBounds(p.Map))
                            {
                                List<Thing> thingList = cellList[i].GetThingList(p.Map);
                                if (thingList != null && thingList.Count > 0)
                                {
                                    for (int j = 0; j < thingList.Count; j++)
                                    {
                                        Pawn pawn = thingList[j] as Pawn;
                                        if (pawn != null && pawn != p)
                                        {
                                            RemoveFireAt(thingList[j].Position, pawn.Map);
                                            if (Rand.Chance(TM_Calc.GetSpellSuccessChance(p, pawn, false)))
                                            {
                                                IntVec3 targetCell = pawn.Position;
                                                targetCell.z++;
                                                DoTimeDelayLaunch(targetCell, p, pawn, 1, Mathf.RoundToInt(Rand.Range(1400, 1800)));
                                            }
                                            else
                                            {
                                                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TM_ResistedSpell".Translate(), -1);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        surgeText = "Time Field";
                        break;
                }
            }
            else
            {
                int rndNeutral = Rand.RangeInclusive(0, 3);
                switch (rndNeutral)
                {
                    case 0:
                        ModOptions.Constants.SetPawnInFlight(true);
                        bool draftFlag = p.Drafted;
                        IntVec3 initPos = p.Position;
                        Map map = p.Map;
                        IntVec3 moveTo = TM_Calc.TryFindSafeCell(p, p.Position, 16, 3, 10);
                        p.DeSpawn();
                        if (moveTo != default(IntVec3))
                        {
                            GenSpawn.Spawn(p, moveTo, map);
                        }
                        else
                        {
                            GenSpawn.Spawn(p, initPos, map);
                        }
                        p.drafter.Drafted = draftFlag;
                        if (p.IsColonist)
                        {
                            p.drafter.Drafted = true;
                            if (ModOptions.Settings.Instance.cameraSnap)
                            {
                                CameraJumper.TryJumpAndSelect(p);
                            }
                        }
                        surgeText = "Teleportation";
                        ModOptions.Constants.SetPawnInFlight(false);
                        break;
                    case 1:
                        if (target != null && target != p && target.Cell != null && target.Cell.IsValid && target.Cell != default(IntVec3))
                        {
                            float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(p.Position, target.Cell)).ToAngleFlat();
                            for (int i = 0; i < 6; i++)
                            {
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Flowers, p.DrawPos, p.Map, Rand.Range(.3f, 1f), Rand.Range(1f, 1.5f), .1f, Rand.Range(.2f, .5f), Rand.Range(-100, 100), Rand.Range(1, 3), angle, Rand.Range(0, 360));
                                FleckMaker.ThrowLightningGlow(p.DrawPos, p.Map, Rand.Range(.5f, 1f));
                                TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, p.DrawPos, p.Map, Rand.Range(.5f, 1f), 3f, .1f, Rand.Range(.5f, 1f), Rand.Range(-20, 20), Rand.Range(.5f, .8f), angle, Rand.Range(0, 360));
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                float angle = Rand.Range(0, 360);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Flowers, p.DrawPos, p.Map, Rand.Range(.3f, 1f), Rand.Range(1f, 1.5f), .1f, Rand.Range(.2f, .5f), Rand.Range(-100, 100), Rand.Range(1, 3), angle, Rand.Range(0, 360));
                                FleckMaker.ThrowLightningGlow(p.DrawPos, p.Map, Rand.Range(.5f, 1f));
                                TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, p.DrawPos, p.Map, Rand.Range(.5f, 1f), 3f, .1f, Rand.Range(.5f, 1f), Rand.Range(-20, 20), Rand.Range(.5f, .8f), angle, Rand.Range(0, 360));
                            }
                        }
                        surgeText = "Flowers";
                        break;
                    case 2:
                        if (p.Map != null)
                        {
                            List<Pawn> allPawns = p.Map.mapPawns.AllPawnsSpawned;
                            if (allPawns != null && allPawns.Count > 0)
                            {
                                for (int i = 0; i < allPawns.Count; i++)
                                {
                                    if (allPawns[i].needs != null && allPawns[i].needs.food != null)
                                    {
                                        HealthUtility.AdjustSeverity(allPawns[i], HediffDefOf.FoodPoisoning, Rand.Range(.3f, .7f));
                                    }
                                }
                            }
                        }
                        surgeText = "Mass Poison";
                        break;
                    case 3:
                        if (p.Map != null)
                        {
                            List<Pawn> allPawns = p.Map.mapPawns.AllPawnsSpawned;
                            if (allPawns != null && allPawns.Count > 0)
                            {
                                for (int i = 0; i < allPawns.Count; i++)
                                {
                                    if (allPawns[i].needs != null && allPawns[i].needs.rest != null)
                                    {
                                        Need need = allPawns[i].needs.TryGetNeed(NeedDefOf.Rest);
                                        if (need != null)
                                        {
                                            need.CurLevel = 0;
                                        }
                                    }
                                }
                            }
                        }
                        surgeText = "Mass Exhaustion";
                        break;
                }
            }
            MoteMaker.ThrowText(p.DrawPos, p.Map, "TM_WildSurge".Translate(surgeText), -1);

            return completeJob;
        }

        public static void DoTimeDelayLaunch(IntVec3 targetCell, Pawn caster, Pawn pawn, int force, int duration)
        {
            bool flag = targetCell != null && targetCell != default(IntVec3);
            if (flag)
            {
                if (pawn != null && pawn.Position.IsValid && pawn.Spawned && pawn.Map != null && !pawn.Downed && !pawn.Dead)
                {
                    if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                    {
                        ModCheck.GiddyUp.ForceDismount(pawn);
                    }
                    FlyingObject_TimeDelay flyingObject = (FlyingObject_TimeDelay)GenSpawn.Spawn(ThingDef.Named("FlyingObject_TimeDelay"), pawn.Position, pawn.Map);
                    flyingObject.speed = .01f;
                    flyingObject.duration = duration;
                    flyingObject.Launch(caster, targetCell, pawn);
                }
            }
        }

        private static void RemoveFireAt(IntVec3 position, Map map)
        {
            List<Thing> thingList = position.GetThingList(map);
            if (thingList != null && thingList.Count > 0)
            {
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i].def == ThingDefOf.Fire)
                    {
                        //Log.Message("removing fire at " + position);
                        FleckMaker.ThrowHeatGlow(position, map, .6f);
                        thingList[i].Destroy(DestroyMode.Vanish);
                        i--;
                    }
                }
            }
        }

        public static void DisplayShieldHit(Pawn shieldedPawn, DamageInfo dinfo)
        {
            if (shieldedPawn != null && shieldedPawn.Map != null && shieldedPawn.Spawned)
            {
                DisplayShield(shieldedPawn, dinfo.Amount, dinfo.Angle);
            }
        }

        public static void DisplayShield(Pawn shieldedPawn, float amount, float angle = 0f)
        {            
            Vector3 impactAngleVect;
            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(shieldedPawn.Position, shieldedPawn.Map, false));
            impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(angle);
            Vector3 loc = shieldedPawn.TrueCenter() + impactAngleVect.RotatedBy(180f) * 0.5f;
            float num = Mathf.Min(10f, 2f + amount / 10f);
            FleckMaker.Static(loc, shieldedPawn.Map, FleckDefOf.ExplosionFlash, num);
            int num2 = (int)num;
            for (int i = 0; i < num2; i++)
            {
                FleckMaker.ThrowDustPuff(loc, shieldedPawn.Map, Rand.Range(0.8f, 1.2f));
                DrawShieldHit(shieldedPawn, amount, impactAngleVect);
            }
        }

        private static void DrawShieldHit(Pawn shieldedPawn, float magnitude, Vector3 impactAngleVect)
        {
            bool flag = !shieldedPawn.Dead && !shieldedPawn.Downed && shieldedPawn.Graphic != null && shieldedPawn.Graphic.drawSize != null;
            if (flag)
            {
                float num = Mathf.Lerp(1.2f, 1.55f, magnitude);
                Vector3 vector = shieldedPawn.Drawer.DrawPos;
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);

                float angle = (float)Rand.Range(0, 360);
                Vector3 s = new Vector3(1.7f * shieldedPawn.Graphic.drawSize.magnitude, 1f, 1.7f * shieldedPawn.Graphic.drawSize.magnitude);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                if (shieldedPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffShield) || shieldedPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HTLShieldHD) || shieldedPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MagicShieldHD))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.whiteShieldMat, 0);
                }
                else if (shieldedPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DemonScornHD) || shieldedPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DemonScornHD_I) || shieldedPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DemonScornHD_II) || shieldedPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DemonScornHD_III))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.demonShieldMat, 0);
                }
                else if(shieldedPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SymbiosisHD) || shieldedPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_OutOfBodyHD))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.redShieldMat, 0);
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.manaShieldMat, 0);
                }
            }
        }

        public static void DoTransmutate(Pawn caster, Thing transmutateThing, bool flagNoStuffItem, bool flagRawResource, bool flagStuffItem, bool flagNutrition, bool flagCorpse)
        {
            CompAbilityUserMagic comp = caster.GetComp<CompAbilityUserMagic>();
            int pwrVal = comp.MagicData.MagicPowerSkill_Transmutate.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Transmutate_pwr").level;
            int verVal = comp.MagicData.MagicPowerSkill_Transmutate.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Transmutate_ver").level;

            //Log.Message("Current target thing is " + transmutateThing.LabelShort + " with a stack count of " + transmutateThing.stackCount + " and market value of " + transmutateThing.MarketValue + " base market value of " + transmutateThing.def.BaseMarketValue + " total value of stack " + transmutateThing.def.BaseMarketValue * transmutateThing.stackCount);
            if (flagNoStuffItem)
            {
                CompQuality compQual = transmutateThing.TryGetComp<CompQuality>();
                float wornRatio = ((float)transmutateThing.HitPoints / (float)transmutateThing.MaxHitPoints);
                Thing thing = transmutateThing;

                if (compQual != null && caster.Inspired && caster.InspirationDef == InspirationDefOf.Inspired_Creativity && compQual.Quality != QualityCategory.Legendary)
                {
                    thing.TryGetComp<CompQuality>().SetQuality(compQual.Quality + 1, ArtGenerationContext.Colony);
                    caster.mindState.inspirationHandler.EndInspiration(caster.Inspiration);
                    if (compQual.Quality == QualityCategory.Legendary && thing.HitPoints == thing.MaxHitPoints)
                    {
                        thing.SetForbidden(true, false);
                    }
                }
                thing.HitPoints = Mathf.RoundToInt((wornRatio * thing.MaxHitPoints) - ((.2f - (.1f * pwrVal)) * thing.MaxHitPoints));
                if ((wornRatio != 1 && thing.HitPoints > thing.MaxHitPoints))
                {
                    thing.HitPoints = thing.MaxHitPoints;
                    thing.SetForbidden(true, false);
                }

                Apparel aThing = thing as Apparel;
                if (aThing != null && aThing.WornByCorpse)
                {
                    aThing.Notify_PawnResurrected();
                    Traverse.Create(root: aThing).Field(name: "wornByCorpseInt").SetValue(false);
                }

                TM_Action.TransmutateEffects(transmutateThing.Position, caster);

            }
            else if (flagRawResource)
            {
                //if (transmutateThing.def.defName != "RawMagicyte")
                //{
                //    for (int i = 0; i < transmutateThing.def.stuffProps.categories.Count; i++)
                //    {
                //        Log.Message("categories include " + transmutateThing.def.stuffProps.categories[i].defName);
                //    }
                //}

                int transStackCount = 0;
                if (transmutateThing.stackCount > 250)
                {
                    transStackCount = 250;
                }
                else
                {
                    transStackCount = transmutateThing.stackCount;
                }
                int transStackValue = Mathf.RoundToInt(transStackCount * transmutateThing.def.BaseMarketValue);
                float newMatCount = 0;
                IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs.InRandomOrder()
                                                   where (def.BaseMarketValue >= 1f && def.BaseMarketValue <= 200 && def != transmutateThing.def && ((def.stuffProps != null && def.stuffProps.categories != null && def.stuffProps.categories.Count > 0) || def.defName == "RawMagicyte") || def.IsWithinCategory(ThingCategoryDefOf.ResourcesRaw) || def.IsWithinCategory(ThingCategoryDefOf.Leathers))
                                                   select def;
                ThingDef newThingDef = null;
                foreach (ThingDef current in enumerable)
                {
                    if (current != null && current.defName != null)
                    {
                        float newThingValue = current.BaseMarketValue < 1f ? 1f : current.BaseMarketValue;
                        newMatCount = transStackValue / newThingValue;
                        newThingDef = current;
                        break;
                        //Log.Message("transumtation resource " + current.defName + " base value " + current.BaseMarketValue + " value count converts to " + newMatCount);
                    }
                }
                if (!transmutateThing.DestroyedOrNull() && transStackCount <= 0)
                {
                    transmutateThing.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    transmutateThing.SplitOff(transStackCount).Destroy(DestroyMode.Vanish);
                }
                Thing thing = null;
                if (newThingDef != null)
                {
                    thing = ThingMaker.MakeThing(newThingDef);
                    thing.stackCount = Mathf.RoundToInt((.7f + (.05f * pwrVal)) * newMatCount);
                    if (newMatCount < 1)
                    {
                        newMatCount = 1;
                    }
                }

                if (thing != null)
                {
                    GenPlace.TryPlaceThing(thing, transmutateThing.Position, caster.Map, ThingPlaceMode.Near, null);
                    TM_Action.TransmutateEffects(transmutateThing.Position, caster);
                }
            }
            else if (flagStuffItem)
            {
                float transValue = 1f;
                if (transmutateThing.Stuff != null)
                {
                    transValue = transmutateThing.Stuff.BaseMarketValue;
                }
                //Log.Message("" + transmutateThing.LabelShort + " is made from " + transmutateThing.Stuff.label + " with a market value of " + transValue);
                IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                                   where (def.stuffProps != null && def.stuffProps.categories != null && 
                                                   def.stuffProps.categories.Contains(transmutateThing.Stuff.stuffProps.categories.RandomElement()) &&
                                                   def.BaseMarketValue <= (3f * transValue * (1f + (.1f * pwrVal))) && def.BaseMarketValue >= ((.1f + (.1f * pwrVal)) * transValue))
                                                   select def;

                //foreach (ThingDef current in enumerable)
                //{
                //    if (current != null && current.defName != null)
                //    {
                //        Log.Message("transumtation resource " + current.defName + " base value " + current.BaseMarketValue);
                //    }
                //}

                CompQuality compQual = transmutateThing.TryGetComp<CompQuality>();
                float wornRatio = ((float)transmutateThing.HitPoints / (float)transmutateThing.MaxHitPoints);
                Thing thing = new Thing();
                ThingDef newThingDef = enumerable.RandomElement();
                thing = ThingMaker.MakeThing(transmutateThing.def, newThingDef);
                if (compQual != null)
                {
                    if (caster.Inspired && caster.InspirationDef == InspirationDefOf.Inspired_Creativity && compQual.Quality != QualityCategory.Legendary)
                    {
                        thing.TryGetComp<CompQuality>().SetQuality(compQual.Quality + 1, ArtGenerationContext.Colony);
                        caster.mindState.inspirationHandler.EndInspiration(caster.Inspiration);
                        if (compQual.Quality == QualityCategory.Legendary && thing.HitPoints == thing.MaxHitPoints)
                        {
                            thing.SetForbidden(true, false);
                        }
                        thing.SetStuffDirect(transmutateThing.Stuff);
                    }
                    else
                    {
                        thing.TryGetComp<CompQuality>().SetQuality(compQual.Quality, ArtGenerationContext.Colony);
                    }
                }
                thing.HitPoints = Mathf.RoundToInt((wornRatio * thing.MaxHitPoints) - ((.2f - (.1f * pwrVal)) * thing.MaxHitPoints));
                if (thing.HitPoints > thing.MaxHitPoints)
                {
                    thing.HitPoints = thing.MaxHitPoints;
                    thing.SetForbidden(true, false);
                }
                transmutateThing.Destroy(DestroyMode.Vanish);
                if (thing != null)
                {
                    GenPlace.TryPlaceThing(thing, transmutateThing.Position, caster.Map, ThingPlaceMode.Near, null);
                    if (thing.HitPoints <= 0)
                    {
                        thing.Destroy(DestroyMode.Vanish);
                        Messages.Message("TM_TransmutationLostCohesion".Translate(thing.def.label), MessageTypeDefOf.NeutralEvent);
                    }
                    TM_Action.TransmutateEffects(transmutateThing.Position, caster);
                }
            }
            else if (flagNutrition)
            {
                int transStackCount = 0;
                if (transmutateThing.stackCount > 500)
                {
                    transStackCount = 500;
                }
                else
                {
                    transStackCount = transmutateThing.stackCount;
                }
                float transNutritionTotal = transmutateThing.GetStatValue(StatDefOf.Nutrition) * transStackCount;
                float newMatCount = 0;
                ThingDef newThingDef = null;
                //Log.Message("" + transmutateThing.LabelShort + " has a nutrition value of " + transmutateThing.GetStatValue(StatDefOf.Nutrition) + " and stack count of " + transmutateThing.stackCount + " for a total nutrition value of " + transNutritionTotal);
                IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                                   where (def.defName == "Pemmican" || def.defName == "MealNutrientPaste")
                                                   select def;

                newThingDef = enumerable.RandomElement();
                if (newThingDef != null)
                {
                    transmutateThing.SplitOff(transStackCount).Destroy(DestroyMode.Vanish);
                    Thing thing = null;
                    newMatCount = transNutritionTotal / newThingDef.GetStatValueAbstract(StatDefOf.Nutrition);
                    thing = ThingMaker.MakeThing(newThingDef);
                    thing.stackCount = Mathf.RoundToInt((.7f + (.05f * pwrVal)) * newMatCount);
                    if (thing.stackCount < 1)
                    {
                        thing.stackCount = 1;
                    }

                    if (thing != null)
                    {
                        GenPlace.TryPlaceThing(thing, transmutateThing.Position, caster.Map, ThingPlaceMode.Near, null);
                        TM_Action.TransmutateEffects(transmutateThing.Position, caster);
                    }
                }
                else
                {
                    Log.Message("No known edible foods to transmutate to - pemmican and nutrient paste removed?");
                }
            }
            else if (flagCorpse)
            {
                Corpse transCorpse = transmutateThing as Corpse;
                ThingDef newThingDef = null;
                float corpseNutritionValue = 0;
                if (transCorpse != null)
                {
                    if (transCorpse.ButcherProducts(caster, 1f) != null && transCorpse.ButcherProducts(caster, 1f).Count() > 0)
                    {
                        List<Thing> butcherProducts = transCorpse.ButcherProducts(caster, 1f).ToList();
                        for (int j = 0; j < butcherProducts.Count; j++)
                        {
                            if (butcherProducts[j].GetStatValue(StatDefOf.Nutrition) > 0)
                            {
                                corpseNutritionValue = (butcherProducts[j].GetStatValue(StatDefOf.Nutrition) * butcherProducts[j].stackCount);
                            }
                        }

                        if (corpseNutritionValue > 0)
                        {
                            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                                               where (def.defName == "MealNutrientPaste")
                                                               select def;

                            if (enumerable != null && enumerable.Count() > 0)
                            {
                                newThingDef = enumerable.RandomElement();
                                if (newThingDef != null)
                                {
                                    transCorpse.Destroy(DestroyMode.Vanish);
                                    Thing thing = null;
                                    int newMatCount = Mathf.RoundToInt(corpseNutritionValue / newThingDef.GetStatValueAbstract(StatDefOf.Nutrition));
                                    thing = ThingMaker.MakeThing(newThingDef);
                                    thing.stackCount = Mathf.RoundToInt((.7f + (.05f * pwrVal)) * newMatCount);

                                    if (thing != null)
                                    {
                                        GenPlace.TryPlaceThing(thing, transmutateThing.Position, caster.Map, ThingPlaceMode.Near, null);
                                        TM_Action.TransmutateEffects(transmutateThing.Position, caster);
                                    }
                                }
                            }
                            else
                            {
                                Log.Message("No known edible foods to transmutate to - nutrient paste removed?");
                            }
                        }
                        else
                        {
                            transCorpse.Destroy(DestroyMode.Vanish);
                            for (int j = 0; j < butcherProducts.Count; j++)
                            {
                                Thing thing = null;
                                thing = ThingMaker.MakeThing(butcherProducts[j].def);
                                thing.stackCount = butcherProducts[j].stackCount;
                                if (thing != null)
                                {
                                    GenPlace.TryPlaceThing(thing, transmutateThing.Position, caster.Map, ThingPlaceMode.Near, null);
                                }

                            }
                            TM_Action.TransmutateEffects(transmutateThing.Position, caster);
                        }
                    }
                }
            }
            else
            {
                Messages.Message("TM_UnableToTransmutate".Translate(
                    caster.LabelShort,
                    transmutateThing.LabelShort
                ), MessageTypeDefOf.RejectInput);
            }
        }

        public static void CreateMagicDeathEffect(Pawn pawn, IntVec3 pos, bool canCauseDeath = true, bool friendlyFire = false)
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            List<IntVec3> targets = new List<IntVec3>();
            List<Pawn> pawns = new List<Pawn>();
            int rnd = Rand.RangeInclusive(0, 6);
            if(friendlyFire)
            {
                rnd = Rand.RangeInclusive(0, 4);
            }
            switch (rnd)
            {
                case 0: //Death explosion
                    IntVec3 curCell;
                    Pawn victim = new Pawn();

                    float radius = 3f;
                    if (settingsRef.AIHardMode)
                    {
                        radius *= 1.5f;
                    }

                    if (pawn.Map == null || !pawn.Position.IsValid)
                    {
                        Log.Warning("Tried to do explosion in a null map.");
                        return;
                    }

                    Faction faction = pawn.Faction;
                    Pawn p = new Pawn();
                    p = pawn;
                    Map map = p.Map;
                    GenExplosion.DoExplosion(p.Position, p.Map, 0f, DamageDefOf.Burn, p as Thing, 0, 0, SoundDefOf.Thunder_OnMap, null, null, null, null, 0f, 0, false, null, 0f, 0, 0.0f, false);
                    Effecter deathEffect = TorannMagicDefOf.TM_DeathExplosion.Spawn();
                    deathEffect.Trigger(new TargetInfo(p.Position, p.Map, false), new TargetInfo(p.Position, p.Map, false));
                    deathEffect.Cleanup();
                    targets = GenRadial.RadialCellsAround(p.Position, radius, true).ToList();
                    for (int i = 0; i < targets.Count; i++)
                    {
                        curCell = targets.ToArray<IntVec3>()[i];
                        if (curCell.InBounds(map) && curCell.IsValid)
                        {
                            victim = curCell.GetFirstPawn(map);
                        }
                        if (victim != null)
                        {
                            if (friendlyFire || (victim.Faction != faction || victim == pawn))
                            {
                                TM_Action.DamageEntities(victim, null, Rand.Range(12, 20), 1f, TMDamageDefOf.DamageDefOf.TM_Arcane, pawn);
                            }
                        }
                        victim = null;
                    }
                    break;
                case 1: //Berserk pulse
                    Effecter berserkEffect = TorannMagicDefOf.TM_IgniteED.Spawn();
                    berserkEffect.Trigger(new TargetInfo(pawn.Position, pawn.Map, false), new TargetInfo(pawn.Position, pawn.Map, false));
                    berserkEffect.Cleanup();
                    pawns = TM_Calc.FindAllPawnsAround(pawn.Map, pawn.Position, 4f, pawn.Faction, false);
                    if (pawns != null && pawns.Count > 0)
                    {
                        for (int i = 0; i < pawns.Count; i++)
                        {
                            if (pawns[i] != null && pawns[i].mindState != null && pawns[i].mindState.mentalStateHandler != null)
                            {
                                if (pawns[i].Faction != pawn.Faction || friendlyFire)
                                {
                                    if (Rand.Chance(TM_Calc.GetSpellSuccessChance(pawn, pawns[i], true)))
                                    {
                                        if (pawns[i].mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk))
                                        {
                                            pawns[i].mindState.mentalStateHandler.CurState.forceRecoverAfterTicks = 480;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                case 2: //Summon 4x firestorm skyfallers
                    Pawn targetF = TM_Calc.FindNearbyEnemy(pos, pawn.Map, pawn.Faction, 60, 10);
                    if(friendlyFire)
                    {
                        targetF = pawn;
                    }
                    if (targetF != null)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            IntVec3 cell = targetF.Position;
                            cell.x += Rand.Range(-2, 2);
                            cell.z += Rand.Range(-2, 2);
                            if (Rand.Chance(.6f))
                            {
                                SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Firestorm_Tiny, cell, pawn.Map);
                            }
                            else if (Rand.Chance(.4f))
                            {
                                SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Firestorm_Small, cell, pawn.Map);
                            }
                            else
                            {
                                SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Firestorm_Large, cell, pawn.Map);
                            }
                        }
                    }
                    break;
                case 3: //summon 4x blizzard skyfallers
                    Pawn targetI = TM_Calc.FindNearbyEnemy(pos, pawn.Map, pawn.Faction, 70, 10);
                    if(friendlyFire)
                    {
                        targetI = pawn;
                    }
                    if (targetI != null)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            IntVec3 cell = targetI.Position;
                            cell.x += Rand.Range(-2, 2);
                            cell.z += Rand.Range(-2, 2);
                            if (Rand.Chance(.6f))
                            {
                                SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Blizzard_Tiny, cell, pawn.Map);
                            }
                            else if (Rand.Chance(.4f))
                            {
                                SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Blizzard_Small, cell, pawn.Map);
                            }
                            else
                            {
                                SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Blizzard_Large, cell, pawn.Map);
                            }
                        }
                    }
                    break;
                case 4: //stun pulse
                    GenExplosion.DoExplosion(pos, pawn.Map, 6, DamageDefOf.Stun, pawn, 0, 0);
                    pawns = TM_Calc.FindAllPawnsAround(pawn.Map, pos, 4f, pawn.Faction, friendlyFire);
                    if (pawns != null && pawns.Count > 0)
                    {
                        for (int i = 0; i < pawns.Count; i++)
                        {
                            DamageEntities(pawns[i], null, Rand.Range(4, 8), 0, DamageDefOf.Stun, pawn);
                        }
                    }
                    break;
                case 5: //mana mine trap
                    AbilityUser.SpawnThings tempPod = new SpawnThings();
                    tempPod.def = ThingDef.Named("TM_ManaMine_III");
                    tempPod.spawnCount = 1;                    
                    Projectile_SummonExplosive.SingleSpawnLoop(tempPod, pos, pawn.Map, pawn, 15000);
                    break;
                case 6:  //Healing wave
                    Effecter healEffect = TorannMagicDefOf.TM_ChiBurstED.Spawn();
                    healEffect.Trigger(new TargetInfo(pawn.Position, pawn.Map, false), new TargetInfo(pawn.Position, pawn.Map, false));
                    healEffect.Cleanup();
                    pawns = TM_Calc.FindAllPawnsAround(pawn.Map, pos, 4f, pawn.Faction, true);
                    if (pawns != null && pawns.Count > 0)
                    {
                        for (int i = 0; i < pawns.Count; i++)
                        {
                            if (pawns[i] != null)
                            {
                                if (pawns[i].Faction == pawn.Faction)
                                {
                                    TM_Action.DoAction_HealPawn(pawn, pawns[i], 3, 25);
                                }
                            }
                        }
                    }
                    break;
            }
            if (canCauseDeath && settingsRef.deathRetaliationIsLethal && rnd < 6)
            {
                KillPawnByMindBurn(pawn);
            }
        }

        public static void KillPawnByMindBurn(Pawn pawn)
        {
            if (pawn != null && !pawn.Dead)
            {
                for (int i = 0; i < 4; i++)
                {
                    TM_MoteMaker.ThrowBloodSquirt(pawn.DrawPos, pawn.Map, Rand.Range(.3f, .6f));
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ArcaneFlame, pawn.DrawPos, pawn.Map, Rand.Range(.2f, .3f), .1f, .05f, .2f, 0, Rand.Range(1.5f, 2f), Rand.Range(-60, 60), 0);
                }
                DamageInfo dinfo2;
                BodyPartRecord vitalPart = null;
                int amt = 30;
                IEnumerable<BodyPartRecord> partSearch = pawn.def.race.body.AllParts;
                vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.ConsciousnessSource));
                dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Arcane, amt, 2, 0, pawn as Thing, vitalPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
                dinfo2.SetAllowDamagePropagation(false);
                pawn.TakeDamage(dinfo2);
            }
        }

        public static void CreateMightDeathEffect(Pawn pawn, IntVec3 pos)
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            List<IntVec3> targets = new List<IntVec3>();
            List<Pawn> pawns = new List<Pawn>();
            int rnd = Rand.RangeInclusive(0, 4);
            switch (rnd)
            {
                case 0: //Spike trap
                    Thing trap = ThingMaker.MakeThing(ThingDefOf.TrapSpike, ThingDefOf.WoodLog);
                    trap.SetFactionDirect(pawn.Faction);
                    GenPlace.TryPlaceThing(trap, pawn.Position, pawn.Map, ThingPlaceMode.Direct);
                    break;
                case 1: //poison trap
                    Thing pTrap = ThingMaker.MakeThing(ThingDef.Named("TM_PoisonTrap"));
                    pTrap.SetFactionDirect(pawn.Faction);
                    GenPlace.TryPlaceThing(pTrap, pawn.Position, pawn.Map, ThingPlaceMode.Direct);
                    break;
                case 2: //burst into flames
                    GenExplosion.DoExplosion(pawn.Position, pawn.Map, 2f, DamageDefOf.Flame, pawn, 10, 0, DamageDefOf.Flame.soundExplosion);
                    break;
                case 3: //wave of fear
                    SoundInfo info = SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map, false), MaintenanceType.None);
                    TorannMagicDefOf.TM_GaspingAir.PlayOneShot(info);
                    Effecter FearWave = TorannMagicDefOf.TM_FearWave.Spawn();
                    FearWave.Trigger(new TargetInfo(pawn.Position, pawn.Map, false), new TargetInfo(pawn.Position, pawn.Map, false));
                    FearWave.Cleanup();
                    List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
                    if (mapPawns != null && mapPawns.Count > 0)
                    {
                        for (int i = 0; i < mapPawns.Count; i++)
                        {
                            Pawn victim = mapPawns[i];
                            if (!victim.DestroyedOrNull() && !victim.Dead && victim.Map != null && !victim.Downed && victim.mindState != null && !victim.InMentalState)
                            {
                                if (victim.Faction != null && victim.Faction != pawn.Faction && (victim.Position - pawn.Position).LengthHorizontal < 6)
                                {
                                    if (Rand.Chance(TM_Calc.GetSpellSuccessChance(pawn, victim, true)))
                                    {
                                        LocalTargetInfo t = new LocalTargetInfo(victim.Position + (6 * TM_Calc.GetVector(pawn.DrawPos, victim.DrawPos)).ToIntVec3());
                                        Job job = new Job(JobDefOf.FleeAndCower, t);
                                        victim.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                        HealthUtility.AdjustSeverity(victim, HediffDef.Named("TM_WaveOfFearHD"), .5f);
                                    }
                                    else
                                    {
                                        MoteMaker.ThrowText(victim.DrawPos, victim.Map, "TM_ResistedSpell".Translate(), -1);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case 4: //Ingest drugs
                    Thing luci = ThingMaker.MakeThing(ThingDef.Named("Luciferium"));
                    if (luci != null)
                    {
                        luci.stackCount = 1;
                        GenSpawn.Spawn(luci, pawn.Position, pawn.Map, WipeMode.Vanish);
                        luci.Ingested(pawn, 0f);
                    }
                    Thing go = ThingMaker.MakeThing(ThingDef.Named("GoJuice"));
                    if (go != null)
                    {
                        go.stackCount = 1;
                        GenSpawn.Spawn(go, pawn.Position, pawn.Map, WipeMode.Vanish);
                        go.Ingested(pawn, 0f);
                    }
                    Thing yayo = ThingMaker.MakeThing(ThingDef.Named("Yayo"));
                    if (yayo != null)
                    {
                        yayo.stackCount = 1;
                        GenSpawn.Spawn(yayo, pawn.Position, pawn.Map, WipeMode.Vanish);
                        yayo.Ingested(pawn, 0f);
                    }
                    TM_Action.DoAction_HealPawn(pawn, pawn, 2, 10f);
                    break;
            }
            if (settingsRef.deathRetaliationIsLethal && rnd < 4)
            {
                KillPawnBySepeku(pawn);
            }
        }

        public static void KillPawnBySepeku(Pawn pawn)
        {
            if (pawn != null && !pawn.Dead)
            {
                for (int i = 0; i < 4; i++)
                {
                    TM_MoteMaker.ThrowBloodSquirt(pawn.DrawPos, pawn.Map, Rand.Range(.5f, .7f));
                }
                DamageInfo dinfo2;
                BodyPartRecord vitalPart = null;
                int amt = 30;
                IEnumerable<BodyPartRecord> partSearch = pawn.def.race.body.AllParts;
                vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.BloodPumpingSource));
                dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Arcane, amt, 2, 0, pawn as Thing, vitalPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
                dinfo2.SetAllowDamagePropagation(false);
                pawn.TakeDamage(dinfo2);
            }
        }

        public static IntVec3 FindNearestWalkableCell(Pawn pawn, IntVec3 c)
        {
            if (c.Walkable(pawn.Map))
            {
                return c;
            }
            else
            {
                return TM_Calc.FindWalkableCellNextTo(c, pawn.Map).Cell;
            }
        }

        public static void TrainAnimalFull(Pawn animal, Pawn trainer)
        {
            if (animal.training.CanBeTrained(TrainableDefOf.Tameness))
            {
                while (!animal.training.HasLearned(TrainableDefOf.Tameness))
                {
                    animal.training.Train(TrainableDefOf.Tameness, trainer);
                }
            }

            if (animal.training.CanBeTrained(TrainableDefOf.Obedience))
            {
                while (!animal.training.HasLearned(TrainableDefOf.Obedience))
                {
                    animal.training.Train(TrainableDefOf.Obedience, trainer);
                }
            }

            if (animal.training.CanBeTrained(TrainableDefOf.Release))
            {
                while (!animal.training.HasLearned(TrainableDefOf.Release))
                {
                    animal.training.Train(TrainableDefOf.Release, trainer);
                }
            }

            if (animal.training.CanBeTrained(TorannMagicDefOf.Haul))
            {
                while (!animal.training.HasLearned(TorannMagicDefOf.Haul))
                {
                    animal.training.Train(TorannMagicDefOf.Haul, trainer);
                }
            }

            if (animal.training.CanBeTrained(TorannMagicDefOf.Rescue))
            {
                while (!animal.training.HasLearned(TorannMagicDefOf.Rescue))
                {
                    animal.training.Train(TorannMagicDefOf.Rescue, trainer);
                }
            }
        }

        public static void UpdateAnimalTraining(Pawn p)
        {
            if (p.RaceProps.Animal)
            {
                if (p.training.CanAssignToTrain(TrainableDefOf.Tameness).Accepted)
                {
                    if (p.training.CanBeTrained(TrainableDefOf.Tameness))
                    {
                        p.training.Train(TrainableDefOf.Tameness, null);
                    }
                }

                if (p.training.CanAssignToTrain(TrainableDefOf.Obedience).Accepted)
                {
                    if (p.training.CanBeTrained(TrainableDefOf.Obedience))
                    {
                        p.training.Train(TrainableDefOf.Obedience, null);
                    }
                }

                if (p.training.CanAssignToTrain(TrainableDefOf.Release).Accepted)
                {
                    if (p.training.CanBeTrained(TrainableDefOf.Release))
                    {
                        p.training.Train(TrainableDefOf.Release, null);
                    }
                }

                if (p.training.CanAssignToTrain(TorannMagicDefOf.Haul).Accepted)
                {
                    if (p.training.CanBeTrained(TorannMagicDefOf.Haul))
                    {
                        p.training.Train(TorannMagicDefOf.Haul, null);
                    }
                }

                if (p.training.CanAssignToTrain(TorannMagicDefOf.Rescue).Accepted)
                {
                    if (p.training.CanBeTrained(TorannMagicDefOf.Rescue))
                    {
                        p.training.Train(TorannMagicDefOf.Rescue, null);
                    }
                }
            }
        }

        public static void InvulnerableAoEFor(IntVec3 center, Map map, float radius, int durationTicks, Faction forFaction = null)
        {
            if(map != null && center != default(IntVec3))
            {
                List<Pawn> pawnList = map.mapPawns.AllPawnsSpawned;
                if(pawnList != null)
                {
                    foreach(Pawn p in pawnList)
                    {
                        if((forFaction == null || (p.Faction == forFaction)) && (p.Position - center).LengthHorizontal <= radius)
                        {
                            if(p.health != null && p.health.hediffSet != null)
                            {
                                HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_HediffTimedInvulnerable, 1f);
                                Hediff hd = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HediffTimedInvulnerable);
                                HediffComp_Disappears hdc = hd.TryGetComp<HediffComp_Disappears>();
                                if(hdc != null)
                                {
                                    hdc.ticksToDisappear += durationTicks;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void SearchAndTaunt(Pawn caster, float radius, int maxTargets, float tauntChance)
        {
            List<Pawn> mapPawns = caster.Map.mapPawns.AllPawnsSpawned;
            List<Pawn> tauntTargets = new List<Pawn>();
            tauntTargets.Clear();
            if (mapPawns != null && mapPawns.Count > 0)
            {
                for (int i = 0; i < mapPawns.Count; i++)
                {
                    Pawn victim = mapPawns[i];
                    if (!victim.DestroyedOrNull() && !victim.Dead && victim.Map != null && !victim.Downed && victim.mindState != null && !victim.InMentalState && victim.jobs != null)
                    {
                        if (caster.Faction.HostileTo(victim.Faction) && (victim.Position - caster.Position).LengthHorizontal < radius)
                        {
                            tauntTargets.Add(victim);
                        }
                    }
                    if (tauntTargets.Count >= maxTargets)
                    {
                        break;
                    }
                }
                for (int i = 0; i < tauntTargets.Count; i++)
                {
                    if (Rand.Chance(tauntChance))
                    {

                        //Log.Message("taunting " + threatPawns[i].LabelShort + " doing job " + threatPawns[i].CurJobDef.defName + " with follow radius of " + threatPawns[i].CurJob.followRadius);
                        if (tauntTargets[i].CurJobDef == JobDefOf.Follow || tauntTargets[i].CurJobDef == JobDefOf.FollowClose)
                        {
                            Job job = new Job(JobDefOf.AttackMelee, caster);
                            tauntTargets[i].jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        }
                        HealthUtility.AdjustSeverity(tauntTargets[i], TorannMagicDefOf.TM_TauntHD, 1);
                        Hediff hd = tauntTargets[i].health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_TauntHD);
                        HediffComp_Disappears comp_d = hd.TryGetComp<HediffComp_Disappears>();
                        if (comp_d != null)
                        {
                            comp_d.ticksToDisappear = 600;
                        }
                        HediffComp_Taunt comp_t = hd.TryGetComp<HediffComp_Taunt>();
                        if (comp_t != null)
                        {
                            comp_t.tauntTarget = caster;
                        }
                        MoteMaker.ThrowText(tauntTargets[i].DrawPos, tauntTargets[i].Map, "Taunted!", -1);
                    }
                    else
                    {
                        MoteMaker.ThrowText(tauntTargets[i].DrawPos, tauntTargets[i].Map, "TM_ResistedSpell".Translate(), -1);
                    }
                }
            }
        }

        public static GizmoResult DrawAutoCastForGizmo(Command_PawnAbility com, Rect rect, bool shrink, GizmoResult oldResult)
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();            
            if (settingsRef.autocastEnabled && com.pawnAbility.Def.defName.StartsWith("TM_"))
            {
                CompAbilityUserMagic comp = com.pawnAbility.Pawn.GetComp<CompAbilityUserMagic>();
                CompAbilityUserMight mightComp = com.pawnAbility.Pawn.GetComp<CompAbilityUserMight>();
                MagicPower magicPower = null;
                MightPower mightPower = null;
                TMAbilityDef tmAbilityDef = com.pawnAbility.Def as TMAbilityDef;
                Text.Font = GameFont.Tiny;
                bool flag = false;
                //Rect rect = new Rect(topLeft.x, topLeft.y, com.GetWidth(maxWidth), 75f);
                //if (shrink)
                //{
                //    Traverse.Create(root: com).Field(name: "order").SetValue(200);
                //    topLeft = new Vector2(ModOptions.Constants.GetIconVector().x, ModOptions.Constants.GetIconVector().y);
                //    rect = new Rect(topLeft.x + (ModOptions.Constants.GetGizmoCount(com.pawnAbility.Pawn, com.pawnAbility) * 38f), topLeft.y, 36f, 36f);
                //}
                if (Mouse.IsOver(rect))
                {
                    flag = true;
                    GUI.color = GenUI.MouseoverColor;
                }

                MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Command);
                Material material = com.disabled ? TexUI.GrayscaleGUI : null;
                //GUI.DrawTexture(rect, Command.BGTex);
                GenUI.DrawTextureWithMaterial(rect, shrink ? Command.BGTexShrunk : Command.BGTex, material);

                Texture2D texture2D = com.icon;
                if (texture2D == null)
                {
                    texture2D = BaseContent.BadTex;
                }
                GUI.color = com.IconDrawColor;
                rect.position += new Vector2(com.iconOffset.x * rect.size.x, com.iconOffset.y * rect.size.y);
                Widgets.DrawTextureFitted(rect, texture2D, com.iconDrawScale * 0.85f, com.iconProportions, com.iconTexCoords, com.iconAngle, material);
                GUI.color = Color.white;
                bool flag2 = false;
                KeyCode keyCode = (com.hotKey != null) ? com.hotKey.MainKey : KeyCode.None;
                if (keyCode != 0 && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
                {
                    Rect rect2 = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 18f);
                    Widgets.Label(rect2, keyCode.ToStringReadable());
                    GizmoGridDrawer.drawnHotKeys.Add(keyCode);
                    if (com.hotKey.KeyDownEvent)
                    {
                        flag2 = true;
                        Event.current.Use();
                    }
                }
                if (Widgets.ButtonInvisible(rect))
                {
                    flag2 = true;
                }
                if (!shrink)
                {
                    string topRightLabel = com.TopRightLabel;
                    if (!topRightLabel.NullOrEmpty())
                    {
                        Vector2 vector2 = Text.CalcSize(topRightLabel);
                        Rect position;
                        Rect rect3 = position = new Rect(rect.xMax - vector2.x - 2f, rect.y + 3f, vector2.x, vector2.y);
                        position.x -= 2f;
                        position.width += 3f;
                        Text.Anchor = TextAnchor.UpperRight;
                        GUI.DrawTexture(position, TexUI.GrayTextBG);
                        Widgets.Label(rect3, topRightLabel);
                        Text.Anchor = TextAnchor.UpperLeft;
                    }
                    string labelCap = com.LabelCap;
                    if (!labelCap.NullOrEmpty())
                    {
                        float num = Text.CalcHeight(labelCap, rect.width);
                        Rect rect2 = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
                        GUI.DrawTexture(rect2, TexUI.GrayTextBG);
                        Text.Anchor = TextAnchor.UpperCenter;
                        Widgets.Label(rect2, labelCap);
                        Text.Anchor = TextAnchor.UpperLeft;
                    }
                    GUI.color = Color.white;
                }
                if (Mouse.IsOver(rect))
                {
                    TipSignal tipSignal = com.Desc;
                    if (com.disabled && !com.disabledReason.NullOrEmpty())
                    {
                        tipSignal.text = tipSignal.text + "\n" + StringsToTranslate.AU_DISABLED + ": " + com.disabledReason;
                    }
                    TooltipHandler.TipRegion(rect, tipSignal);
                }
                if (com.pawnAbility.CooldownTicksLeft != -1 && com.pawnAbility.CooldownTicksLeft < com.pawnAbility.MaxCastingTicks)
                {
                    float fillPercent = (float)com.curTicks / (float)com.pawnAbility.MaxCastingTicks;
                    Widgets.FillableBar(rect, fillPercent, AbilityButtons.FullTex, AbilityButtons.EmptyTex, doBorder: false);
                }
                if (!com.HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null || !Find.WindowStack.FloatMenu.windowRect.Overlaps(rect)))
                {
                    UIHighlighter.HighlightOpportunity(rect, com.HighlightTag);
                }
                if (comp != null && comp.MagicData != null && tmAbilityDef != null)
                {
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Blink || com.pawnAbility.Def == TorannMagicDefOf.TM_Blink_I || com.pawnAbility.Def == TorannMagicDefOf.TM_Blink_II || com.pawnAbility.Def == TorannMagicDefOf.TM_Blink_III)
                    {
                        magicPower = comp.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect)) //__result.State == GizmoState.Mouseover)
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Summon || com.pawnAbility.Def == TorannMagicDefOf.TM_Summon_I || com.pawnAbility.Def == TorannMagicDefOf.TM_Summon_II || com.pawnAbility.Def == TorannMagicDefOf.TM_Summon_III)
                    {
                        magicPower = comp.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Firebolt)
                    {
                        magicPower = comp.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Icebolt)
                    {
                        magicPower = comp.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_LightningBolt)
                    {
                        magicPower = comp.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_FrostRay || com.pawnAbility.Def == TorannMagicDefOf.TM_FrostRay_I || com.pawnAbility.Def == TorannMagicDefOf.TM_FrostRay_II || com.pawnAbility.Def == TorannMagicDefOf.TM_FrostRay_III)
                    {
                        magicPower = comp.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_MagicMissile || com.pawnAbility.Def == TorannMagicDefOf.TM_MagicMissile_I || com.pawnAbility.Def == TorannMagicDefOf.TM_MagicMissile_II || com.pawnAbility.Def == TorannMagicDefOf.TM_MagicMissile_III)
                    {
                        magicPower = comp.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Entertain)
                    {
                        magicPower = comp.MagicData.MagicPowersB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Entertain);
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_EnchantedAura)
                    {
                        magicPower = comp.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantedAura);
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_EnchantedBody)
                    {
                        magicPower = comp.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantedBody);
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Shadow || com.pawnAbility.Def == TorannMagicDefOf.TM_Shadow_I || com.pawnAbility.Def == TorannMagicDefOf.TM_Shadow_II || com.pawnAbility.Def == TorannMagicDefOf.TM_Shadow_III)
                    {
                        magicPower = comp.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_RayofHope || com.pawnAbility.Def == TorannMagicDefOf.TM_RayofHope_I || com.pawnAbility.Def == TorannMagicDefOf.TM_RayofHope_II || com.pawnAbility.Def == TorannMagicDefOf.TM_RayofHope_III)
                    {
                        magicPower = comp.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_P_RayofHope || com.pawnAbility.Def == TorannMagicDefOf.TM_P_RayofHope_I || com.pawnAbility.Def == TorannMagicDefOf.TM_P_RayofHope_II || com.pawnAbility.Def == TorannMagicDefOf.TM_P_RayofHope_III)
                    {
                        magicPower = comp.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Soothe || com.pawnAbility.Def == TorannMagicDefOf.TM_Soothe_I || com.pawnAbility.Def == TorannMagicDefOf.TM_Soothe_II || com.pawnAbility.Def == TorannMagicDefOf.TM_Soothe_III)
                    {
                        magicPower = comp.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);
                    }                    
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Prediction)
                    {
                        magicPower = comp.MagicData.MagicPowersC.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Prediction);
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Poison)
                    {
                        magicPower = comp.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Poison);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Regenerate)
                    {
                        magicPower = comp.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Regenerate);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_CureDisease)
                    {
                        magicPower = comp.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_CureDisease);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Heal)
                    {
                        magicPower = comp.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Heal);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Shield || com.pawnAbility.Def == TorannMagicDefOf.TM_Shield_I | com.pawnAbility.Def == TorannMagicDefOf.TM_Shield_II || com.pawnAbility.Def == TorannMagicDefOf.TM_Shield_III)
                    {
                        magicPower = comp.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_AdvancedHeal)
                    {
                        magicPower = comp.MagicData.MagicPowersPR.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_AdvancedHeal);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_TransferMana)
                    {
                        magicPower = comp.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TransferMana);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_SiphonMana)
                    {
                        magicPower = comp.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SiphonMana);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_CauterizeWound)
                    {
                        magicPower = comp.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_CauterizeWound);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_SpellMending)
                    {
                        magicPower = comp.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SpellMending);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_TeachMagic)
                    {
                        magicPower = comp.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TeachMagic);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_ShadowBolt || com.pawnAbility.Def == TorannMagicDefOf.TM_ShadowBolt_I || com.pawnAbility.Def == TorannMagicDefOf.TM_ShadowBolt_II || com.pawnAbility.Def == TorannMagicDefOf.TM_ShadowBolt_III)
                    {
                        if (comp.Pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                        {
                            magicPower = comp.MagicData.MagicPowersWD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);
                        }
                        else
                        {
                            magicPower = comp.MagicData.MagicPowersSD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);
                        }

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Purify)
                    {
                        magicPower = comp.MagicData.MagicPowersPR.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Purify);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_SummonMinion)
                    {
                        magicPower = comp.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonMinion);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_MechaniteReprogramming)
                    {
                        magicPower = comp.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_MechaniteReprogramming);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_DirtDevil)
                    {
                        magicPower = comp.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_DirtDevil);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_ArcaneBolt)
                    {
                        magicPower = comp.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ArcaneBolt);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_TimeMark)
                    {
                        magicPower = comp.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TimeMark);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Transmutate)
                    {
                        magicPower = comp.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Transmutate);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_RegrowLimb)
                    {
                        magicPower = comp.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RegrowLimb);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            magicPower.AutoCast = !magicPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_SuppressiveAura)
                    {
                        magicPower = comp.MagicData.MagicPowersCustom.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmAbilityDef);
                    }
                    if (comp.MagicData.MagicPowersCustomAll != null)
                    {
                        foreach (MagicPower mp in comp.MagicData.MagicPowersCustomAll)
                        {
                            if (mp.autocasting != null && mp.autocasting.type != TMDefs.AutocastType.Null && (mp.autocasting.drafted || mp.autocasting.undrafted))
                            {
                                if (mp.TMabilityDefs.Contains(com.pawnAbility.Def))
                                {
                                    magicPower = mp;
                                    if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                                    {
                                        magicPower.AutoCast = !magicPower.AutoCast;
                                        return new GizmoResult(GizmoState.Mouseover, null);
                                        
                                    }
                                }
                            }
                        }
                    }
                }
                if (mightComp != null && mightComp.MightData != null && com.pawnAbility.Def != null)
                {
                    //might abilities
                    if (mightComp.MightData.MightPowersCustomAll != null)
                    {
                        foreach (MightPower mp in mightComp.MightData.MightPowersCustomAll)
                        {
                            if (mp.autocasting != null && mp.autocasting.type != TMDefs.AutocastType.Null && (mp.autocasting.drafted || mp.autocasting.undrafted))
                            {
                                if (mp.TMabilityDefs.Contains(com.pawnAbility.Def))
                                {
                                    mightPower = mp;
                                    if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                                    {
                                        mightPower.AutoCast = !mightPower.AutoCast;
                                        return new GizmoResult(GizmoState.Mouseover, null);
                                        
                                    }
                                }
                            }
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Grapple || com.pawnAbility.Def == TorannMagicDefOf.TM_Grapple_I || com.pawnAbility.Def == TorannMagicDefOf.TM_Grapple_II || com.pawnAbility.Def == TorannMagicDefOf.TM_Grapple_III)
                    {
                        mightPower = mightComp.MightData.MightPowersG.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == tmAbilityDef);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_BladeSpin)
                    {
                        mightPower = mightComp.MightData.MightPowersB.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_BladeSpin);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }                    
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_PhaseStrike || com.pawnAbility.Def == TorannMagicDefOf.TM_PhaseStrike_I || com.pawnAbility.Def == TorannMagicDefOf.TM_PhaseStrike_II || com.pawnAbility.Def == TorannMagicDefOf.TM_PhaseStrike_III)
                    {
                        mightPower = mightComp.MightData.MightPowersB.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == tmAbilityDef);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_ArrowStorm || com.pawnAbility.Def == TorannMagicDefOf.TM_ArrowStorm_I || com.pawnAbility.Def == TorannMagicDefOf.TM_ArrowStorm_II || com.pawnAbility.Def == TorannMagicDefOf.TM_ArrowStorm_III)
                    {
                        mightPower = mightComp.MightData.MightPowersR.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == tmAbilityDef);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_DisablingShot || com.pawnAbility.Def == TorannMagicDefOf.TM_DisablingShot_I || com.pawnAbility.Def == TorannMagicDefOf.TM_DisablingShot_II || com.pawnAbility.Def == TorannMagicDefOf.TM_DisablingShot_III)
                    {
                        mightPower = mightComp.MightData.MightPowersS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == tmAbilityDef);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Spite || com.pawnAbility.Def == TorannMagicDefOf.TM_Spite_I || com.pawnAbility.Def == TorannMagicDefOf.TM_Spite_II || com.pawnAbility.Def == TorannMagicDefOf.TM_Spite_III)
                    {
                        mightPower = mightComp.MightData.MightPowersDK.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == tmAbilityDef);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Headshot)
                    {
                        mightPower = mightComp.MightData.MightPowersS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Headshot);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_AntiArmor)
                    {
                        mightPower = mightComp.MightData.MightPowersS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_AntiArmor);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_ThrowingKnife)
                    {
                        mightPower = mightComp.MightData.MightPowersStandalone.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_ThrowingKnife);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_PommelStrike)
                    {
                        mightPower = mightComp.MightData.MightPowersStandalone.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_PommelStrike);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_TempestStrike)
                    {
                        mightPower = mightComp.MightData.MightPowersStandalone.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_TempestStrike);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_TigerStrike)
                    {
                        mightPower = mightComp.MightData.MightPowersM.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_TigerStrike);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_ThunderStrike)
                    {
                        mightPower = mightComp.MightData.MightPowersM.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_ThunderStrike);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_ProvisionerAura)
                    {
                        mightPower = mightComp.MightData.MightPowersC.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_ProvisionerAura);
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_TaskMasterAura)
                    {
                        mightPower = mightComp.MightData.MightPowersC.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_TaskMasterAura);
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_CommanderAura)
                    {
                        mightPower = mightComp.MightData.MightPowersC.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_CommanderAura);
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_PistolWhip)
                    {
                        mightPower = mightComp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_PistolWhip);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_SuppressingFire)
                    {
                        mightPower = mightComp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_SuppressingFire);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Buckshot)
                    {
                        mightPower = mightComp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Buckshot);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_FirstAid)
                    {
                        mightPower = mightComp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_FirstAid);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Meditate)
                    {
                        mightPower = mightComp.MightData.MightPowersM.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Meditate);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_Nightshade)
                    {
                        mightPower = mightComp.MightData.MightPowersShadow.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Nightshade);
                    }
                    if (com.pawnAbility.Def == TorannMagicDefOf.TM_TeachMight)
                    {
                        mightPower = mightComp.MightData.MightPowersStandalone.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_TeachMight);

                        if (Input.GetMouseButtonDown(1) && Mouse.IsOver(rect))
                        {
                            mightPower.AutoCast = !mightPower.AutoCast;
                            return new GizmoResult(GizmoState.Mouseover, null);
                            
                        }
                    }
                }
                if (magicPower != null && comp != null && comp.Mana != null)
                {
                    //Rect rect = new Rect(topLeft.x, topLeft.y, com.GetWidth(maxWidth), 75f);
                    Rect position = new Rect(rect.x + rect.width - 24f, rect.y, 24f, 24f);
                    if (shrink)
                    {
                        position = new Rect(rect.x + rect.width - 14f, rect.y, 14f, 14f);
                    }
                    Texture2D image = (!magicPower.AutoCast) ? Widgets.CheckboxOffTex : Widgets.CheckboxOnTex;
                    GUI.DrawTexture(position, image);
                }
                if (mightPower != null && mightComp != null && mightComp.Stamina != null)
                {
                    //Rect rect = new Rect(topLeft.x, topLeft.y, com.GetWidth(maxWidth), 75f);
                    Rect position = new Rect(rect.x + rect.width - 24f, rect.y, 24f, 24f);
                    if (shrink)
                    {
                        position = new Rect(rect.x + rect.width - 14f, rect.y, 14f, 14f);
                    }
                    Texture2D image = (!mightPower.AutoCast) ? Widgets.CheckboxOffTex : Widgets.CheckboxOnTex;
                    GUI.DrawTexture(position, image);
                }
                if (flag2 && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonUp(1))
                {
                    if (com.disabled)
                    {
                        if (!com.disabledReason.NullOrEmpty())
                        {
                            Messages.Message(com.disabledReason, MessageTypeDefOf.RejectInput);
                        }
                        return new GizmoResult(GizmoState.Mouseover, null);
                        
                    }
                    if (!TutorSystem.AllowAction(com.TutorTagSelect))
                    {
                        return new GizmoResult(GizmoState.Mouseover, null);
                        
                    }
                    return new GizmoResult(GizmoState.Interacted, Event.current);
                    
                }

                if (flag)
                {
                    return new GizmoResult(GizmoState.Mouseover, null);
                    
                }
                return new GizmoResult(GizmoState.Clear, null);
                
            }
            return oldResult;
        }

        public static void ClearSustainedMagicHediffs(CompAbilityUserMagic comp)
        {
            if (comp != null)
            {
                Pawn p = comp.Pawn;
                if (p != null && p.health != null && p.health.hediffSet != null)
                {
                    List<Hediff> recList = new List<Hediff>();
                    recList.Clear();
                    List<Hediff> hds = p.health.hediffSet.GetHediffs<Hediff>().ToList();
                    if (hds != null && hds.Count > 0)
                    {
                        for (int i = 0; i < hds.Count; i++)
                        {
                            if (hds[i].def == TorannMagicDefOf.TM_RayOfHope_AuraHD || hds[i].def == TorannMagicDefOf.TM_SoothingBreeze_AuraHD || hds[i].def == TorannMagicDefOf.TM_Shadow_AuraHD || hds[i].def == TorannMagicDefOf.TM_InnerFire_AuraHD ||
                                hds[i].def == TorannMagicDefOf.TM_TechnoBitHD || hds[i].def == TorannMagicDefOf.TM_EnchantedAuraHD || hds[i].def == TorannMagicDefOf.TM_EnchantedBodyHD ||
                                hds[i].def == TorannMagicDefOf.TM_PredictionHD || hds[i].def == TorannMagicDefOf.TM_SDSoulBondPhysicalHD || hds[i].def == TorannMagicDefOf.TM_WDSoulBondMentalHD)
                            {
                                recList.Add(hds[i]);
                            }
                        }
                        for (int i = 0; i < recList.Count; i++)
                        {
                            p.health.RemoveHediff(recList[i]);
                        }
                    }
                }
            }
        }
    }
}
