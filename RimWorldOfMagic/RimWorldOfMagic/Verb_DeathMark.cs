using RimWorld;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class Verb_DeathMark : ClassProgression.Verb_SpiritTargetable
    {

        private int verVal;
        private int pwrVal;
        private float arcaneDmg = 1f;

        bool validTarg;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    //out of range
                    validTarg = true;
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
            bool result = false;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
            if (owner == null) return false;
            var comp = owner.GetCompAbilityUserMagic();
            if (comp?.MagicData == null) return false;
            if (owner != CasterPawn)
            {
                var skills = comp.MagicData.MagicPowerSkill_DeathMark;
                verVal = ClassProgression.Projectile_SpiritBolt.SpellRank(owner, skills, "TM_DeathMark", "_ver");
                pwrVal = ClassProgression.Projectile_SpiritBolt.SpellRank(owner, skills, "TM_DeathMark", "_pwr");
            }
            else
            {
                verVal = TM_Calc.GetSkillVersatilityLevel(owner, Ability.Def as TMAbilityDef);
                pwrVal = TM_Calc.GetSkillPowerLevel(owner, Ability.Def as TMAbilityDef);
            }
            if (this.currentTarget != null && base.CasterPawn != null)
            {
                
                Map map = this.CasterPawn.Map;
                this.TargetsAoE.Clear();
                //this.UpdateTargets();
                this.FindTargets();
                
                for(int i =0; i < this.TargetsAoE.Count; i++)
                {
                    if (this.TargetsAoE[i].Thing is Pawn)
                    {
                        Pawn victim = this.TargetsAoE[i].Thing as Pawn;
                        if(!victim.RaceProps.IsMechanoid)
                        {
                            if (Rand.Chance(TM_Calc.GetSpellSuccessChance(owner, victim, true)))
                            {
                                float markSeverity = Rand.Range(1f + pwrVal, 4 + 2 * pwrVal) * this.arcaneDmg;
                                HealthUtility.AdjustSeverity(victim, HediffDef.Named("TM_DeathMarkCurse"), markSeverity);
                                ClassProgression.NecromancerDeathMarkTrial.Applied(this.CasterPawn, victim, this.Ability, markSeverity);
                                TM_MoteMaker.ThrowSiphonMote(victim.DrawPos, victim.Map, 1.4f);
                                if (comp.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD"), false))
                                {
                                    comp.PowerModifier += 1;
                                }

                                if (Rand.Chance(verVal * .2f))
                                {
                                    if (Rand.Chance(verVal * .1f)) //terror
                                    {
                                        HealthUtility.AdjustSeverity(victim, HediffDef.Named("TM_Terror"), Rand.Range(3f * verVal, 5f * verVal) * this.arcaneDmg);
                                        TM_MoteMaker.ThrowDiseaseMote(victim.DrawPos, victim.Map, 1f, .5f, .2f, .4f);
                                        MoteMaker.ThrowText(victim.DrawPos, victim.Map, "Terror", -1);
                                    }
                                    if (Rand.Chance(verVal * .1f)) //berserk
                                    {
                                        if (victim.mindState != null && victim.RaceProps != null && victim.RaceProps.Humanlike)
                                        {
                                            victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "cursed", true, false, false, null);
                                            FleckMaker.ThrowMicroSparks(victim.DrawPos, victim.Map);
                                            MoteMaker.ThrowText(victim.DrawPos, victim.Map, "Berserk", -1);
                                        }

                                    }
                                }
                                if (victim.IsColonist && !base.CasterPawn.IsColonist)
                                {
                                    TM_Action.SpellAffectedPlayerWarning(victim);
                                }
                            }
                            else
                            {
                                MoteMaker.ThrowText(victim.DrawPos, victim.Map, "TM_ResistedSpell".Translate(), -1);
                            }
                        }
                    }
                }

                result = true;
            }

            this.burstShotsLeft = 0;
            //this.ability.TicksUntilCasting = (int)base.UseAbilityProps.SecondsToRecharge * 60;
            return result;
        }


        private void FindTargets()
        {
            bool flag = this.UseAbilityProps.AbilityTargetCategory == AbilityTargetCategory.TargetAoE;
            if (flag)
            {
                bool flag2 = this.UseAbilityProps.TargetAoEProperties == null;
                if (flag2)
                {
                    Log.Error("Tried to Cast AoE-Ability without defining a target class");
                }
                List<Thing> list = new List<Thing>();
                IntVec3 aoeStartPosition = this.caster.PositionHeld;
                bool flag3 = !this.UseAbilityProps.TargetAoEProperties.startsFromCaster;
                if (flag3)
                {
                    aoeStartPosition = this.currentTarget.Cell;
                }
                bool flag4 = !this.UseAbilityProps.TargetAoEProperties.friendlyFire;
                if (flag4)
                {
                    list = (from x in this.caster.Map.listerThings.AllThings
                            where x.Position.InHorDistOf(aoeStartPosition, (float)this.UseAbilityProps.TargetAoEProperties.range) && this.UseAbilityProps.TargetAoEProperties.targetClass.IsAssignableFrom(x.GetType()) && x.Faction != Faction.OfPlayer
                            select x).ToList<Thing>();
                }
                else
                {
                    bool flag5 = this.UseAbilityProps.TargetAoEProperties.targetClass == typeof(Plant) || this.UseAbilityProps.TargetAoEProperties.targetClass == typeof(Building);
                    if (flag5)
                    {
                        list = (from x in this.caster.Map.listerThings.AllThings
                                where x.Position.InHorDistOf(aoeStartPosition, (float)this.UseAbilityProps.TargetAoEProperties.range) && this.UseAbilityProps.TargetAoEProperties.targetClass.IsAssignableFrom(x.GetType())
                                select x).ToList<Thing>();
                        foreach (Thing current in list)
                        {
                            LocalTargetInfo item = new LocalTargetInfo(current);
                            this.TargetsAoE.Add(item);
                        }
                        return;
                    }
                    list.Clear();
                    list = (from x in this.caster.Map.listerThings.AllThings
                            where x.Position.InHorDistOf(aoeStartPosition, (float)this.UseAbilityProps.TargetAoEProperties.range) && this.UseAbilityProps.TargetAoEProperties.targetClass.IsAssignableFrom(x.GetType()) && (x.HostileTo(Faction.OfPlayer) || this.UseAbilityProps.TargetAoEProperties.friendlyFire)
                            select x).ToList<Thing>();
                }
                int maxTargets = this.UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties.maxTargets;
                List<Thing> list2 = new List<Thing>(list.InRandomOrder(null));
                int num = 0;
                while (num < maxTargets && num < list2.Count<Thing>())
                {
                    TargetInfo targ = new TargetInfo(list2[num]);
                    bool flag6 = this.UseAbilityProps.targetParams.CanTarget(targ);
                    if (flag6)
                    {
                        this.TargetsAoE.Add(new LocalTargetInfo(list2[num]));
                    }
                    num++;
                }
            }
            else
            {
                this.TargetsAoE.Clear();
                this.TargetsAoE.Add(this.currentTarget);
            }
        }
    }
}
