using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using AbilityUser;
using System.Linq;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Verb_TigerStrike : Verb_UseAbility
    {

        public int verVal = 0;
        public int pwrVal = 0;

        protected override bool TryCastShot()
        {
            bool continueAttack = true;
            if (this.CasterPawn.equipment.Primary == null)
            {
                Thing target = this.currentTarget.Thing;
                if (target != null && this.burstShotsLeft > 0)
                {
                    int dmgNum = this.GetAttackDmg(this.CasterPawn);
                    BodyPartRecord hitPart = null;
                    DamageDef dmgType = TMDamageDefOf.DamageDefOf.TM_TigerStrike;
                    if(verVal > 0 && Rand.Chance(.1f))
                    {
                        TM_Action.DamageEntities(target, null, 4, DamageDefOf.Stun, this.CasterPawn);
                    }
                    if(verVal > 1 && Rand.Chance(.4f) && target is Pawn)
                    {
                        if(TM_Calc.IsMagicUser(target as Pawn))
                        {
                            CompAbilityUserMagic compMagic = target.TryGetComp<CompAbilityUserMagic>();
                            float manaDrain = Mathf.Clamp(compMagic.Mana.CurLevel, 0, .25f);
                            this.CasterPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD).Severity += (manaDrain * 100);
                            compMagic.Mana.CurLevel -= manaDrain;

                        }
                        else if(TM_Calc.IsMightUser(target as Pawn))
                        {
                            CompAbilityUserMight compMight = target.TryGetComp<CompAbilityUserMight>();
                            float staminaDrain = Mathf.Clamp(compMight.Stamina.CurLevel, 0, .25f);
                            this.CasterPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD).Severity += (staminaDrain * 100);
                            compMight.Stamina.CurLevel -= staminaDrain;
                        }
                    }
                    if(verVal > 2 && Rand.Chance(.1f) && target is Pawn)
                    {
                        Pawn targetPawn = target as Pawn;
                        IEnumerable<BodyPartRecord> rangeOfParts = (targetPawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.BloodPumpingSource).Concat(
                            targetPawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.BloodFiltrationSource).Concat(
                                targetPawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.BreathingPathway).Concat(
                                    targetPawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.SightSource)))));

                        hitPart = rangeOfParts.RandomElement();
                        dmgNum = Mathf.RoundToInt(dmgNum * 1.4f);
                    }
                    //DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_TigerStrike, dmgNum, 0, (float)-1, this.CasterPawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                    TM_Action.DamageEntities(target, hitPart, dmgNum, dmgType, this.CasterPawn);
                    Vector3 strikeEndVec = this.currentTarget.CenterVector3;
                    strikeEndVec.x += Rand.Range(-.2f, .2f);
                    strikeEndVec.z += Rand.Range(-.2f, .2f);
                    Vector3 strikeStartVec = this.CasterPawn.DrawPos;
                    strikeStartVec.z += Rand.Range(-.2f, .2f);
                    strikeStartVec.x += Rand.Range(-.2f, .2f);
                    Vector3 angle = TM_Calc.GetVector(strikeStartVec, strikeEndVec);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_TigerStrike, strikeStartVec, this.CasterPawn.Map, .4f, .08f, .03f, .05f, 0, 8f, (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat());
                    if(!target.DestroyedOrNull() && target is Pawn)
                    {
                        Pawn targetPawn = target as Pawn;
                        if(targetPawn.Downed || targetPawn.Dead)
                        {
                            continueAttack = false;
                        }
                    }
                    if(this.burstShotsLeft <= (10 - (4 + verVal)))
                    {
                        this.burstShotsLeft = 0;
                        continueAttack = false;
                    }
                }
            }
            else
            {
                Messages.Message("MustBeUnarmed".Translate(
                    this.CasterPawn.LabelCap,
                    this.verbProps.label
                ), MessageTypeDefOf.RejectInput);
                this.burstShotsLeft = 0;
                return false;
            }
            return continueAttack;

        }

        public int GetAttackDmg(Pawn pawn)
        {
            CompAbilityUserMight comp = pawn.GetComp<CompAbilityUserMight>();
            //MightPowerSkill pwr = comp.MightData.MightPowerSkill_TigerStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_TigerStrike_pwr");
            //MightPowerSkill ver = comp.MightData.MightPowerSkill_TigerStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_TigerStrike_ver");
            //verVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_TigerStrike, "TM_TigerStrike", "_ver", true);
            //pwrVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_TigerStrike, "TM_TigerStrike", "_pwr", true);
            verVal = TM_Calc.GetSkillVersatilityLevel(pawn, this.Ability.Def as TMAbilityDef, false);
            pwrVal = TM_Calc.GetSkillPowerLevel(pawn, this.Ability.Def as TMAbilityDef, false);
            MightPowerSkill str = comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr");
            //this.verVal = ver.level;
            //this.pwrVal = pwr.level;
            //if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mver = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    MightPowerSkill mpwr = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    verVal = mver.level;
            //    pwrVal = mpwr.level;
            //}
            int dmgNum = 0;
            float pawnDPS = pawn.GetStatValue(StatDefOf.MeleeDPS, false);
            float skillMultiplier = (.8f + (.08f * pwrVal));
            return dmgNum = Mathf.RoundToInt(skillMultiplier * (pawnDPS) * comp.mightPwr * Rand.Range(.75f, 1.25f));
        }
    }
}


//if (this.CasterPawn.equipment.Primary != null && !this.CasterPawn.equipment.Primary.def.IsRangedWeapon)
//            {
//    int dmgNum = GetWeaponDmg(this.CasterPawn);
//    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
//    if (!this.CasterPawn.IsColonist && settingsRef.AIHardMode)
//    {
//        dmgNum += 10;
//    }

//    Vector3 strikeVec = this.origin;
//    DrawBlade(strikeVec, 0);
//    for (int i = 0; i < this.StartingTicksToImpact; i++)
//    {
//        strikeVec = this.ExactPosition;
//        Pawn victim = strikeVec.ToIntVec3().GetFirstPawn(map);
//        if (victim != null && victim.Faction != base.CasterPawn.Faction)
//        {
//            DrawStrike(strikeVec.ToIntVec3(), strikeVec, map);
//            damageEntities(victim, null, dmgNum, DamageDefOf.Cut);
//        }
//        float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(this.CasterPawn.DrawPos, this.currentTarget.CenterVector3)).ToAngleFlat();
//        TM_MoteMaker.ThrowGenericMote(ThingDef.Named("Mote_DirectionalDirt"), strikeVec, this.CasterPawn.Map, .3f + (.08f * i), .05f, .15f, .38f, 0, 5f - (.2f * i), angle, angle);
//        if (i == 2)
//        {
//            TM_MoteMaker.ThrowGenericMote(ThingDef.Named("Mote_Cleave"), strikeVec, this.CasterPawn.Map, .6f + (.05f * i), .05f, .04f + (.03f * i), .15f, -10000, 30, angle, angle);
//        }
//        //FleckMaker.ThrowTornadoDustPuff(strikeVec, map, .6f, Color.white);
//        for (int j = 0; j < 2 + (2 * verVal); j++)
//        {
//            IntVec3 searchCell = strikeVec.ToIntVec3() + GenAdj.AdjacentCells8WayRandomized()[j];
//            TM_MoteMaker.ThrowGenericMote(ThingDef.Named("Mote_DirectionalDirt"), searchCell.ToVector3Shifted(), this.CasterPawn.Map, .1f + (.04f * i), .05f, .04f, .28f, 0, 4f - (.2f * i), angle, angle);
//            //FleckMaker.ThrowTornadoDustPuff(searchCell.ToVector3(), map, .4f, Color.gray);
//            victim = searchCell.GetFirstPawn(map);
//            if (victim != null && victim.Faction != base.CasterPawn.Faction)
//            {
//                DrawStrike(searchCell, searchCell.ToVector3(), map);
//                damageEntities(victim, null, dmgNum, DamageDefOf.Cut);
//            }
//        }
//        this.ticksToImpact--;
//    }
//}
//            else
//            {
//    Messages.Message("MustHaveMeleeWeapon".Translate(
//        this.CasterPawn.LabelCap
//    ), MessageTypeDefOf.RejectInput);
//    return false;
//}

//            this.burstShotsLeft = 0;
//            this.PostCastShot(flag10, out flag10);
//            return flag10;