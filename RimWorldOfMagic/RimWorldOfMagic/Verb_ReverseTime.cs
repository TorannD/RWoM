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
    public class Verb_ReverseTime : ClassProgression.Verb_SpiritTargetable
    {

        private int verVal =0;
        private int pwrVal =0;
        private float arcaneDmg = 1f;

        private static bool Supported(Thing target) => target?.Spawned == true && (target is Pawn living && !living.Dead ||
            target is Corpse corpse && corpse.InnerPawn?.Dead == true && corpse.TryGetComp<CompRottable>() != null ||
            target.def.category == ThingCategory.Item && (Projectile_AccelerateTime.CanAgeItem(target) ||
                target.TryGetComp<CompRottable>() != null && target.GetStatValue(StatDefOf.Nutrition) > 0));

        private Thing ResolveTarget(LocalTargetInfo target)
        {
            var map = CasterPawn?.Map;
            if (map == null || !target.IsValid || !target.Cell.InBounds(map)) return null;
            if (target.HasThing) return target.Thing.Map == map && Supported(target.Thing) ? target.Thing : null;
            // A bare cell is sufficient only when it identifies one eligible target.
            Thing found = null;
            foreach (var thing in target.Cell.GetThingList(map))
            {
                if (!Supported(thing)) continue;
                if (found != null) return null;
                found = thing;
            }
            return found;
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            if (ResolveTarget(target) == null) return false;
            if (target.Thing == caster) return verbProps.targetParams.canTargetSelf;
            var map = CasterPawn.Map;
            return !target.Cell.Fogged(map) && target.Cell.Walkable(map) &&
                (root - target.Cell).LengthHorizontal < verbProps.range && TryFindShootLineFromTo(root,target,out _);
        }

        protected override bool TryCastShot()
        {
            PostCastShot(ApplyHostedEffect(),out bool fired);
            return fired;
        }

        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body,Ability);
            var target = ResolveTarget(currentTarget);
            if (target == null || owner == null) return false;
            var comp = owner.GetCompAbilityUserMagic();
            if (comp == null) return false;
            bool hosted = owner != body;
            pwrVal = hosted ? ClassProgression.SpiritRebirthRuntime.SavedRank(comp.ExistingMagicData,"TM_ReverseTime_pwr")
                : TM_Calc.GetSkillPowerLevel(owner,Ability.Def as TMAbilityDef);
            verVal = hosted ? ClassProgression.SpiritRebirthRuntime.SavedRank(comp.ExistingMagicData,"TM_ReverseTime_ver")
                : TM_Calc.GetSkillVersatilityLevel(owner,Ability.Def as TMAbilityDef);
            var location = new TargetInfo(target.Position,body.Map);
            if (target is Pawn pawn)
            {
                bool enemy = pawn.Faction == null || pawn.Faction != body.Faction;
                AgePawn(pawn,Mathf.RoundToInt((enemy ? 2500 : 15000) * (1 + .1f * verVal)),enemy);
            }
            else if (target is Corpse)
            {
                if (!AgeCorpse(target,owner)) return false;
            }
            else if (Projectile_AccelerateTime.CanAgeItem(target)) AgeThing(target);
            else AgeFood(target);
            var effect = TorannMagicDefOf.TM_TimeReverseEffecter.Spawn();
            try { effect.Trigger(location, location); }
            finally { effect.Cleanup(); }
            return true;
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
                    if (pawn.Faction == this.CasterPawn.Faction || (Rand.Chance((.5f + verVal) * TM_Calc.GetSpellSuccessChance(ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn,Ability), pawn, false))))
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
            var trial = ClassProgression.ChronomancerTrials.CaptureReverse(this.Ability, thing, false);
            thing.HitPoints = Mathf.Clamp(thing.HitPoints + Mathf.RoundToInt((200 + (100 * pwrVal)) * this.arcaneDmg), 0, thing.MaxHitPoints);
            if (thing is Apparel apparelThing)
            {
                if(apparelThing.WornByCorpse)
                {
                    Traverse.Create(root: apparelThing).Field(name: "wornByCorpseInt").SetValue(false);
                }
            }
            ClassProgression.ChronomancerTrials.CompleteReverse(trial);
            TransmutateEffects(thing.Position, 4);
        }

        private void AgeFood(Thing thing)
        {
            var trial = ClassProgression.ChronomancerTrials.CaptureReverse(this.Ability, thing, true);
            CompRottable compRot = thing.TryGetComp<CompRottable>();
            if(compRot != null)
            {
                compRot.RotProgress = compRot.RotProgress * Math.Max(0f,.4f - .125f * pwrVal);
            }
            ClassProgression.ChronomancerTrials.CompleteReverse(trial);
        }

        private bool AgeCorpse(Thing thing, Pawn owner)
        {
            var corpse = (Corpse)thing;
            var rot = corpse.TryGetComp<CompRottable>();
            if (rot == null) return false;
            if (rot.RotProgress > 5000 + 5000 * pwrVal)
            {
                float restored = Math.Max(20001,rot.RotProgress * Math.Max(0f,.4f - .125f * pwrVal));
                if (restored >= rot.RotProgress) return false;
                rot.RotProgress = restored;
                return true;
            }
            var dead = corpse.InnerPawn;
            var position = corpse.Position;
            if (verVal < 3 || dead?.Dead != true || !ResurrectionUtility.TryResurrectWithSideEffects(dead)) return false;
            TransmutateEffects(position,10);
            AgePawn(dead,Mathf.RoundToInt(15000 * (1 + .1f * verVal)),false);
            HealthUtility.AdjustSeverity(dead,TorannMagicDefOf.TM_DeathReversalHD,1f);
            Projectile_Resurrection.ApplyHealthDefects(dead,.25f,.3f);
            Projectile_Resurrection.ReduceSkillsOfPawn(dead,Rand.Range(.30f,.40f));
            HealthUtility.AdjustSeverity(CasterPawn,TorannMagicDefOf.TM_DeathReversalHD,Rand.Range(.4f,.6f));
            Projectile_Resurrection.ReduceSkillsOfPawn(owner,Rand.Range(.15f,.25f));
            return true;
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
