using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_Polymorph : Verb_UseAbility
    {

        private int verVal;
        private int pwrVal;
        bool validTarg;
        private float arcaneDmg = 1;

        private int duration = 1800;
        int min = 20;
        int max = 100;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map))
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
            this.TargetsAoE.Clear();
            this.UpdateTargets();
            MagicPowerSkill pwr = base.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Polymorph.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Polymorph_pwr");
            MagicPowerSkill ver = base.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Polymorph.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Polymorph_ver");
            verVal = ver.level;
            pwrVal = pwr.level;
            CompAbilityUserMagic comp = base.CasterPawn.GetCompAbilityUserMagic();
            this.arcaneDmg = base.CasterPawn.GetCompAbilityUserMagic().arcaneDmg;
            this.duration += Mathf.RoundToInt(600 * verVal * this.arcaneDmg); 

            if (base.CasterPawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                MightPowerSkill mpwr = base.CasterPawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                MightPowerSkill mver = base.CasterPawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                pwrVal = mpwr.level;
                verVal = mver.level;
            }
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (settingsRef.AIHardMode && !this.CasterPawn.IsColonist)
            {
                verVal = 2;
                pwrVal = 3;
            }
            bool flag2 = this.UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && this.TargetsAoE.Count > 1;
            if (flag2)
            {
                this.TargetsAoE.RemoveRange(0, this.TargetsAoE.Count - 1);
            }
            for (int i = 0; i < this.TargetsAoE.Count; i++)
            {
                Pawn newPawn = this.TargetsAoE[i].Thing as Pawn;
                if (newPawn != this.CasterPawn)
                {
                    CompPolymorph compPoly = newPawn.GetComp<CompPolymorph>();
                    if (compPoly != null && compPoly.Original != null && compPoly.TicksLeft > 0)
                    {
                        compPoly.Temporary = true;
                        compPoly.TicksLeft = 0;
                    }
                    else
                    {
                        float enchantChance = .5f;
                        if (!TM_Calc.IsRobotPawn(newPawn))
                        {
                            enchantChance = (.5f + (.1f * pwrVal) * TM_Calc.GetSpellSuccessChance(this.CasterPawn, newPawn));
                        }
                        else
                        {
                            enchantChance = (.0f + (.2f * pwrVal) * TM_Calc.GetSpellSuccessChance(this.CasterPawn, newPawn));
                        }
                        if (Rand.Chance(enchantChance) && newPawn.GetComp<CompPolymorph>() != null)
                        { 
                            FactionDef fDef = null;
                            if (newPawn.Faction != null)
                            {
                                fDef = newPawn.Faction.def;
                            }
                            SpawnThings spawnThing = new SpawnThings();
                            spawnThing.factionDef = fDef;
                            spawnThing.spawnCount = 1;
                            spawnThing.temporary = false;

                            GetPolyMinMax(newPawn);

                            spawnThing = TM_Action.AssignRandomCreatureDef(spawnThing, this.min, this.max);
                            if (spawnThing.def == null || spawnThing.kindDef == null)
                            {
                                spawnThing.def = ThingDef.Named("Rat");
                                spawnThing.kindDef = PawnKindDef.Named("Rat");
                                Log.Message("random creature was null");
                            }

                            Pawn polymorphedPawn = TM_Action.PolymorphPawn(this.CasterPawn, newPawn, newPawn, spawnThing, newPawn.Position, true, duration, newPawn.Faction);

                            if (polymorphedPawn.Faction != this.CasterPawn.Faction && polymorphedPawn.mindState != null && Rand.Chance(Mathf.Clamp((.2f * this.pwrVal), 0f, .5f)))
                            {
                                polymorphedPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "wild beast!", true, false, null, true);
                            }

                            if (this.verVal >= 3)
                            {
                                polymorphedPawn.GetComp<CompPolymorph>().Temporary = false;
                            }

                            FleckMaker.ThrowSmoke(newPawn.DrawPos, newPawn.Map, 2);
                            FleckMaker.ThrowMicroSparks(newPawn.DrawPos, newPawn.Map);
                            FleckMaker.ThrowHeatGlow(newPawn.Position, newPawn.Map, 2);
                            
                            newPawn.DeSpawn();
                            if (polymorphedPawn.IsColonist && !base.CasterPawn.IsColonist)
                            {
                                TM_Action.SpellAffectedPlayerWarning(polymorphedPawn);
                            }
                        }
                        else
                        {
                            MoteMaker.ThrowText(newPawn.DrawPos, newPawn.Map, "TM_ResistedSpell".Translate(), -1);
                        }
                    }
                                        
                }
            }
            this.PostCastShot(flag, out flag);
            return flag;
        }   
        
        private void GetPolyMinMax(Pawn pawn)
        {
            if (this.verVal >= 3)
            {
                if (pawn.Faction != this.CasterPawn.Faction)
                {
                    this.min = 20;
                    this.max = 60;
                }
                else
                {
                    this.min = 80;
                    this.max = 200;
                }
            }
            else if (this.verVal >= 2)
            {
                if (pawn.Faction != this.CasterPawn.Faction)
                {
                    this.min = 20;
                    this.max = 60;
                }
                else
                {
                    this.min = 60;
                    this.max = 160;
                }
            }
            else if (this.verVal >= 1)
            {
                if (pawn.Faction != this.CasterPawn.Faction)
                {
                    this.min = 40;
                    this.max = 80;
                }
                else
                {
                    this.min = 50;
                    this.max = 100;
                }
            }
            else
            {
                if (pawn.Faction != this.CasterPawn.Faction)
                {
                    this.min = 60;
                    this.max = 100;
                }
                else
                {
                    this.min = 20;
                    this.max = 60;
                }
            }
        }        
    }
}
