using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.Sound;
using TorannMagic.TMDefs;
using UnityEngine;

namespace TorannMagic.Golems
{
    public class CompProperties_GolemAbilityEffect : IExposable
    {
        public int goodwillImpact;
        public ClamorDef clamorType;
        public int clamorRadius;
        public float screenShakeIntensity;
        public SoundDef sound;
        public float effectLevelModifier = 0f;
        public EffecterDef effecter;
        public ThingDef mote;
        public float moteSize;
        public bool randomMoteRotation = false;
        public EffecterTargets effectTarget;

        public virtual void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.goodwillImpact, "goodwillImpact");
            Scribe_Values.Look<int>(ref this.clamorRadius, "clamorRadius");
            Scribe_Defs.Look<ClamorDef>(ref this.clamorType, "clamortype");
            Scribe_Values.Look<int>(ref this.goodwillImpact, "goodwillImpact");
            Scribe_Defs.Look<SoundDef>(ref this.sound, "sound");
            Scribe_Defs.Look<EffecterDef>(ref this.effecter, "effecter");
            Scribe_Defs.Look<ThingDef>(ref this.mote, "mote");
            Scribe_Values.Look<float>(ref this.moteSize, "moteSize");
            Scribe_Values.Look<EffecterTargets>(ref this.effectTarget, "effectTarget", EffecterTargets.OnTarget);
        }

        public float currentLevel = 1f;

        public virtual float LevelModifier => (1f + (effectLevelModifier * currentLevel));

        public virtual void Apply(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability, float effectLevel = 1f, float effectBonus = 1f)
        {
            currentLevel = effectLevel;
            if (screenShakeIntensity > 0f)
            {
                Find.CameraDriver.shaker.DoShake(screenShakeIntensity);
            }
            Pawn targetPawn = target.Pawn;
            if (targetPawn != null && caster != null && !targetPawn.IsSlaveOfColony)
            {
                Faction homeFaction = targetPawn.HomeFaction;
                if (goodwillImpact != 0 && caster.Faction == Faction.OfPlayer && homeFaction != null && !homeFaction.HostileTo(caster.Faction))
                {
                    Faction.OfPlayer.TryAffectGoodwillWith(homeFaction, goodwillImpact, true, true, HistoryEventDefOf.UsedHarmfulAbility);
                }
            }
            if (clamorType != null)
            {
                GenClamor.DoClamor(caster, target.Cell, (float)clamorRadius, clamorType);
            }
            SoundDef soundDef = sound;
            soundDef?.PlayOneShot(new TargetInfo(target.Cell, caster.Map));
            if (mote != null && mote.mote != null)
            {
                if (effectTarget == EffecterTargets.OnCaster)
                {
                    TM_MoteMaker.ThrowGenericMote(mote, caster.DrawPos, caster.Map, moteSize, mote.mote.solidTime, mote.mote.fadeInTime, mote.mote.fadeOutTime, 0, 0, 0, randomMoteRotation ? Rand.Range(0, 360) : 0f);
                }
                else if (effectTarget == EffecterTargets.OnTarget)
                {
                    TM_MoteMaker.ThrowGenericMote(mote, target.CenterVector3, caster.Map, moteSize, mote.mote.solidTime, mote.mote.fadeInTime, mote.mote.fadeOutTime, 0, 0, 0, randomMoteRotation ? Rand.Range(0, 360) : 0f);
                }
                else
                {
                    Vector3 v = TM_Calc.GetVectorBetween(target.CenterVector3, caster.DrawPos);
                    float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(caster.DrawPos, target.CenterVector3)).ToAngleFlat();
                    TM_MoteMaker.ThrowGenericMote(mote, v, caster.Map, moteSize, mote.mote.solidTime, mote.mote.fadeInTime, mote.mote.fadeOutTime, 0, 0, 0, angle);
                }
            }
            if (effecter != null)
            {
                Effecter effect = effecter.Spawn();
                if (effectTarget == EffecterTargets.OnCaster)
                {
                    effect.Trigger(new TargetInfo(caster.Position, caster.Map, false), new TargetInfo(caster.Position, caster.Map, false)); 
                }
                else if (effectTarget == EffecterTargets.OnTarget)
                {
                    effect.Trigger(new TargetInfo(target.Cell, caster.Map, false), new TargetInfo(target.Cell, caster.Map, false));
                }
                else
                {
                    Vector3 v = TM_Calc.GetVectorBetween(target.CenterVector3, caster.DrawPos);
                    effect.Trigger(new TargetInfo(v.ToIntVec3(), caster.Map, false), new TargetInfo(v.ToIntVec3(), caster.Map, false));
                }

                effect.Cleanup();
            }
        }

        public virtual bool CanApplyOn(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability)
        {
            return target != null && !caster.DestroyedOrNull() && !caster.Dead && !caster.Downed;
        }
    }

    public enum EffecterTargets
    {
        OnCaster,
        OnTarget,
        Between
    }
}
