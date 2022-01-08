using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;


namespace TorannMagic
{
    public class Verb_ReverseTime : Verb_UseAbility
    {

        private int verVal =0;
        private int pwrVal =0;
        private float arcaneDmg = 1f;

        bool validTarg;
        //can be used with shieldbelt
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == this.caster)
            {
                return this.verbProps.targetParams.canTargetSelf;
            }
            if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
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

        private void Initialize()
        {
            Pawn pawn = this.CasterPawn;
            CompAbilityUserMagic comp = pawn.GetComp<CompAbilityUserMagic>();
            //MagicPowerSkill pwr = pawn.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_ReverseTime.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ReverseTime_pwr");
            //MagicPowerSkill ver = pawn.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_ReverseTime.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ReverseTime_ver");
            //ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            //pwrVal = pwr.level;
            //verVal = ver.level;
            //arcaneDmg = comp.arcaneDmg;
            //if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mpwr = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    MightPowerSkill mver = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    pwrVal = mpwr.level;
            //    verVal = mver.level;
            //}
            //if (settingsRef.AIHardMode && !pawn.IsColonist)
            //{
            //    pwrVal = 3;
            //    verVal = 3;
            //}
            pwrVal = TM_Calc.GetSkillPowerLevel(pawn, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(pawn, this.Ability.Def as TMAbilityDef);
        }

        protected override bool TryCastShot()
        {
            bool flag = false;
            Initialize();

            bool flagPawn = false;
            bool flagStuffItem = false;
            bool flagNoStuffItem = false;
            bool flagNutrition = false;
            bool flagCorpse = false;

            Pawn targetPawn = this.currentTarget.Thing as Pawn;
            if (targetPawn == null)
            {
                this.currentTarget.Cell.GetFirstPawn(this.CasterPawn.Map);
            }

            if (targetPawn != null)
            {
                if (targetPawn.Faction != null && targetPawn.Faction == this.CasterPawn.Faction)
                {
                    AgePawn(targetPawn, Mathf.RoundToInt((6 * 2500) * (1 + (.1f * verVal))), false);
                }
                else
                {
                    AgePawn(targetPawn, Mathf.RoundToInt((2500) * (1 + (.1f * verVal))), true);
                }
                flagPawn = true;
            }

            if (!flagPawn)
            {
                List<Thing> thingList = this.currentTarget.Cell.GetThingList(caster.Map);
                Thing ageThing = null;


                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i] != null && !(thingList[i] is Pawn) && !(thingList[i] is Building))
                    {
                        //if (thingList[i].def.thingCategories != null && thingList[i].def.thingCategories.Count > 0 && (thingList[i].def.thingCategories.Contains(ThingCategoryDefOf.ResourcesRaw) || thingList[i].def.thingCategories.Contains(ThingCategoryDefOf.StoneBlocks) || thingList[i].def.defName == "RawMagicyte"))                    
                        if (thingList[i].def.MadeFromStuff)
                        {
                            //Log.Message("stuff item");
                            flagStuffItem = true;
                            ageThing = thingList[i];
                            break;
                        }
                        if (!thingList[i].def.MadeFromStuff && thingList[i].TryGetComp<CompQuality>() != null)
                        {
                            //Log.Message("non stuff item");
                            flagNoStuffItem = true;
                            ageThing = thingList[i];
                            break;
                        }
                        if ((thingList[i].def.statBases != null && thingList[i].GetStatValue(StatDefOf.Nutrition) > 0) && !(thingList[i] is Corpse))
                        {
                            //Log.Message("food item");
                            flagNutrition = true;
                            ageThing = thingList[i];
                            break;
                        }
                        if (thingList[i] is Corpse)
                        {
                            //Log.Message("corpse");
                            flagCorpse = true;
                            ageThing = thingList[i];
                            break;
                        }
                    }
                }

                if (ageThing != null)
                {
                    if (flagNoStuffItem || flagStuffItem)
                    {
                        AgeThing(ageThing);
                    }
                    else if (flagNutrition)
                    {
                        AgeFood(ageThing);
                    }
                    else if (flagCorpse)
                    {
                        AgeCorpse(ageThing);
                    }
                }
            }
            Effecter ReverseEffect = TorannMagicDefOf.TM_TimeReverseEffecter.Spawn();
            ReverseEffect.Trigger(new TargetInfo(this.currentTarget.Cell, this.CasterPawn.Map, false), new TargetInfo(this.currentTarget.Cell, this.CasterPawn.Map, false));
            ReverseEffect.Cleanup();

            this.PostCastShot(flag, out flag);
            return flag;
        }

        private void AgePawn(Pawn pawn, int duration, bool isBad)
        {
            duration = Mathf.RoundToInt(duration * this.arcaneDmg);
            if (!pawn.DestroyedOrNull() && !pawn.Dead && pawn.health != null && pawn.health.hediffSet != null && pawn.Map != null)
            {
                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_AccelerateTimeHD))
                {
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_AccelerateTimeHD);
                    pawn.health.RemoveHediff(hediff);
                }
                else
                {
                    if (pawn.Faction == this.CasterPawn.Faction || (Rand.Chance((.5f + verVal) * TM_Calc.GetSpellSuccessChance(this.CasterPawn, pawn, false))))
                    {
                        if(isBad)
                        {
                            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ReverseTimeBadHD, .5f + pwrVal);
                            HediffComp_ReverseTime hediffComp = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ReverseTimeBadHD, false).TryGetComp<HediffComp_ReverseTime>();
                            if (hediffComp != null)
                            {
                                hediffComp.durationTicks = (duration);
                                hediffComp.isBad = isBad;
                                if (pawn.IsColonist && !base.CasterPawn.IsColonist)
                                {
                                    TM_Action.SpellAffectedPlayerWarning(pawn);
                                }
                            }
                        }
                        else
                        {
                            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ReverseTimeHD, .5f + (.1f * pwrVal));
                            HediffComp_ReverseTime hediffComp = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ReverseTimeHD, false).TryGetComp<HediffComp_ReverseTime>();
                            if (hediffComp != null)
                            {
                                hediffComp.durationTicks = (duration);
                                hediffComp.isBad = isBad;
                            }
                        }                        
                        
                        TimeEffects(pawn, 3);
                    }
                    else
                    {
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TM_ResistedSpell".Translate(), -1);
                    }
                }
            }
        }

        private void AgeThing(Thing thing)
        {

            thing.HitPoints = Mathf.Clamp(thing.HitPoints + Mathf.RoundToInt((200 + (100 * pwrVal)) * this.arcaneDmg), 0, thing.MaxHitPoints);
            if (thing is Apparel)
            {
                Apparel apparelThing = thing as Apparel;
                if(apparelThing.WornByCorpse)
                {
                    apparelThing.Notify_PawnResurrected();
                    Traverse.Create(root: apparelThing).Field(name: "wornByCorpseInt").SetValue(false);
                }
            }
            TransmutateEffects(thing.Position, 4);
        }

        private void AgeFood(Thing thing)
        {
            CompRottable compRot = thing.TryGetComp<CompRottable>();
            if(compRot != null)
            {
                compRot.RotProgress = compRot.RotProgress * (.4f - (.125f * pwrVal));
            }
        }

        private void AgeCorpse(Thing thing)
        {
            CompRottable compRot = thing.TryGetComp<CompRottable>();
            if (compRot != null)
            {
                if(compRot.RotProgress <= (5000 + (5000*pwrVal)))
                {
                    Corpse corpse = thing as Corpse;
                    if (corpse != null && verVal >= 3)
                    {
                        TransmutateEffects(corpse.Position, 10);
                        Pawn innerPawn = corpse.InnerPawn;
                        ResurrectionUtility.ResurrectWithSideEffects(innerPawn);
                        AgePawn(innerPawn, Mathf.RoundToInt((6*2500) * (1 + (.1f * verVal))), false);
                        HealthUtility.AdjustSeverity(innerPawn, TorannMagicDefOf.TM_DeathReversalHD, 1f);
                        Projectile_Resurrection.ApplyHealthDefects(innerPawn, .25f, .3f);
                        Projectile_Resurrection.ReduceSkillsOfPawn(innerPawn, Rand.Range(.30f, .40f));
                        HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_DeathReversalHD, Rand.Range(.4f, .6f));
                        //Projectile_Resurrection.ApplyHealthDefects(this.CasterPawn, .15f, .2f);
                        Projectile_Resurrection.ReduceSkillsOfPawn(this.CasterPawn, Rand.Range(.15f, .25f));
                    }
                }
                else
                {
                    compRot.RotProgress = compRot.RotProgress * (.4f - (.125f * pwrVal));
                    if(compRot.RotProgress <= 20000)
                    {
                        compRot.RotProgress = 20001;
                    }
                }
            }
        }

        public void TransmutateEffects(IntVec3 position, int intensity)
        {
            Vector3 rndPos = position.ToVector3Shifted();
            FleckMaker.ThrowHeatGlow(position, this.CasterPawn.Map, 1f);
            for (int i = 0; i < intensity; i++)
            {
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(-.5f, .5f);
                rndPos.y += Rand.Range(.3f, 1.3f);
                FleckMaker.ThrowSmoke(rndPos, this.CasterPawn.Map, Rand.Range(.7f, 1.1f));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Shadow, position.ToVector3(), this.CasterPawn.Map, Rand.Range(.8f, 1.2f), .1f, .1f, .4f, Rand.RangeInclusive((int)-4, (int)4) * 100, Rand.Range(0, 1), Rand.Range(0, 360), Rand.Range(0, 360));
            }
        }

        public void TimeEffects(Pawn pawn, int intensity)
        {
            for (int i = 0; i < intensity; i++)
            {
                Effecter AccelEffect = TorannMagicDefOf.TM_TimeReverseEffecter.Spawn();
                AccelEffect.Trigger(new TargetInfo(pawn), new TargetInfo(pawn));
                AccelEffect.Cleanup();
            }
        }

    }
}
