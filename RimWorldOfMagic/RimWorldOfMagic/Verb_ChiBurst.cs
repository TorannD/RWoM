using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace TorannMagic
{
    class Verb_ChiBurst : Verb_UseAbility  
    {

        bool validTarg;
        private int pwrVal;
        internal const string HostedOrder = "TM_SpiritChiBurst";
        internal static Verb_ChiBurst For(Pawn body, MightAbility ability) => new Verb_ChiBurst
            { caster = body, Ability = ability, verbProps = ability.mightDef.MainVerb };

        internal static bool Valid(Pawn body, MightAbility ability, LocalTargetInfo target) =>
            ClassProgression.SpiritHostedMartial.Ready(body, ability, HostedOrder) &&
            target.IsValid && target.Cell.InBounds(body.Map) && For(body, ability).CanHitTargetFrom(body.Position, target);

        internal static void Choose(Pawn body, Pawn soul, MightAbility ability)
        {
            var map = body.Map;
            bool Available(LocalTargetInfo target) => body.Spawned && body.Map == map &&
                ClassProgression.SpiritHostedMartial.Find(body, soul, HostedOrder) == ability && Valid(body, ability, target);
            Find.Targeter.BeginTargeting(ability.mightDef.MainVerb.targetParams, target => {
                if (!Available(target)) return;
                var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(HostedOrder), soul, target);
                job.playerForced = true;
                body.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }, highlightAction: target => {
                GenDraw.DrawRadiusRing(body.Position, ability.mightDef.MainVerb.range);
                if (target.IsValid) GenDraw.DrawRadiusRing(target.Cell, ability.mightDef.MainVerb.TargetAoEProperties.range);
            }, targetValidator: Available, caster: body);
        }

        internal bool CastFromHost(LocalTargetInfo target)
        {
            if (!Valid(CasterPawn, Ability as MightAbility, target)) return false;
            currentTarget = target;
            return TryCastShot();
        }

        internal bool PreparePower()
        {
            var body = CasterPawn;
            var owner = Ability?.Pawn ?? body;
            if (owner != body && !(ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(body, out var held) &&
                held == owner && ClassProgression.SpiritRebirthState.TryGet(owner, out var state) && !state.DepletionPending))
                return false;
            var comp = owner?.GetCompAbilityUserMight();
            if (comp == null) return false;
            pwrVal = comp.MightData.MightPowerSkill_Chi.FirstOrDefault(x => x.label == "TM_Chi_pwr")?.level ?? 0;
            if (!body.IsColonist && ModOptions.Settings.Instance.AIHardMode) pwrVal = 3;
            return true;
        }

        internal float Penetration() => TM_Calc.GetSpellPenetrationFrom(Ability?.Pawn ?? CasterPawn, CasterPawn);

        private float SuccessChance(Pawn target, bool psychic) =>
            1f + Penetration() - TM_Calc.GetArcaneResistance(target, psychic);

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
            Pawn caster = this.CasterPawn;
            if (!PreparePower()) return false;
            Map map = this.CasterPawn.Map;

            Effecter SabotageEffect = TorannMagicDefOf.TM_ChiBurstED.Spawn();
            SabotageEffect.Trigger(new TargetInfo(this.currentTarget.Cell, caster.Map, false), new TargetInfo(this.currentTarget.Cell, caster.Map, false));
            SabotageEffect.Cleanup();

            List<Pawn> classPawns = GetMapClassPawnsAround(caster.Map, this.currentTarget.Cell, this.UseAbilityProps.TargetAoEProperties.range);
            if(classPawns != null && classPawns.Count > 0)
            {
                for(int i =0; i < classPawns.Count; i++)
                {
                    if(classPawns[i] != caster)
                    {
                        if (classPawns[i].health != null && classPawns[i].Faction != null && classPawns[i].health.hediffSet != null && classPawns[i].story != null && !classPawns[i].NonHumanlikeOrWildMan())
                        {
                            float successChance = SuccessChance(classPawns[i], false);
                            if (Rand.Chance(successChance))
                            {
                                DisruptClassPawn(classPawns[i]);
                            }
                            else
                            {
                                MoteMaker.ThrowText(classPawns[i].DrawPos, classPawns[i].Map, "TM_ResistedSpell".Translate(), -1);
                            }
                        }                        
                    }
                }
            }
         
            return true;
        }

        public List<Pawn> GetMapClassPawnsAround(Map map, IntVec3 centerPos, float radius)
        {
            List<Pawn> mapPawns = new List<Pawn>();
            mapPawns.Clear();
            List<Pawn> classPawns = new List<Pawn>();
            classPawns.Clear();
            mapPawns = map.mapPawns.AllPawnsSpawned.ToList();
            for(int i =0; i < mapPawns.Count; i++)
            {
                if(mapPawns[i] != CasterPawn && (mapPawns[i].Position - centerPos).LengthHorizontal <= radius)
                {
                    if(TM_Calc.IsMagicUser(mapPawns[i]) || TM_Calc.IsMightUser(mapPawns[i]))
                    {
                        classPawns.Add(mapPawns[i]);
                    }
                    else if(!mapPawns[i].DestroyedOrNull())
                    {
                        DisruptMentalState_NonClass(mapPawns[i]);
                    }
                }
            }
            return classPawns;
            
        }

        private void DisruptClassPawn(Pawn pawn)
        {
            Hediff classHediff = null;
            float energyBurn = 0;
            if (TM_Calc.IsMightUser(pawn))
            {
                CompAbilityUserMight mightComp = pawn.GetCompAbilityUserMight();
                classHediff = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PsionicHD);
                classHediff = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HateHD);
                classHediff = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD);
                if(mightComp != null && mightComp.Stamina != null)
                {
                    energyBurn = Mathf.Clamp(mightComp.Stamina.CurLevel, 0, (.5f * (1f + (.20f * pwrVal))));
                    TM_Action.DamageEntities(pawn, null, Mathf.RoundToInt(Rand.Range(30f, 50f) * energyBurn), TMDamageDefOf.DamageDefOf.TM_ChiBurn, this.CasterPawn);
                    mightComp.Stamina.CurLevel -= energyBurn;
                }
            }
            else if (TM_Calc.IsMagicUser(pawn))
            {
                CompAbilityUserMagic magicComp = pawn.GetCompAbilityUserMagic();
                classHediff = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_BloodHD);
                if (magicComp != null && magicComp.Mana != null)
                {
                    energyBurn = Mathf.Clamp(magicComp.Mana.CurLevel, 0, (.5f * (1f + (.20f * pwrVal))));
                    TM_Action.DamageEntities(pawn, null, Mathf.RoundToInt(Rand.Range(30f, 50f) * energyBurn), TMDamageDefOf.DamageDefOf.TM_ChiBurn, this.CasterPawn);
                    magicComp.Mana.CurLevel -= energyBurn;
                }
            }
            TM_Action.DamageEntities(pawn, null, Mathf.RoundToInt(Rand.Range(20f, 30f) * energyBurn), DamageDefOf.Stun, this.CasterPawn);
            if (classHediff != null)
            {
                energyBurn = Mathf.Clamp(classHediff.Severity, 0, (.5f * (1f + (.20f * pwrVal))) * 100);
                classHediff.Severity -= energyBurn;
            }
        }

        private void DisruptMentalState_NonClass(Pawn pawn)
        {
            if (Rand.Chance(SuccessChance(pawn, true)))
            {
                if (pawn.RaceProps.Humanlike && Rand.Chance(.08f))
                {
                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                }
                else if (pawn.RaceProps.Animal && Rand.Chance(.1f))
                {
                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
                }
                else if (Rand.Chance(.5f))
                {
                    TM_Action.DamageEntities(pawn, null, Rand.Range(4, 8), DamageDefOf.Stun, this.CasterPawn);
                }
            }
            else
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TM_ResistedSpell".Translate(), -1);
            }
        }
    }
}
