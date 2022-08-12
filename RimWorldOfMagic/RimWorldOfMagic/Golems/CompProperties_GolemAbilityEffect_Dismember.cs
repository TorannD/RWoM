using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

using Verse.Sound;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class CompProperties_GolemAbilityEffect_Dismember : CompProperties_GolemAbilityEffect
    {
        public bool canDecapitate;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.canDecapitate, "canDecapitate");
        }

        public override void Apply(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability, float effectLevel = 1f, float effectBonus = 1f)
        {
            base.Apply(target, caster, ability);
            if (target.Thing != null && target.Thing is Pawn victim)
            {
                List<BodyPartRecord> bprList = new List<BodyPartRecord>();
                bprList.Clear();
                List<BodyPartRecord> bprLegs = victim.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside, BodyPartTagDefOf.MovingLimbCore, null).ToList();
                List<BodyPartRecord> bprArms = victim.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside, BodyPartTagDefOf.ManipulationLimbCore, null).ToList();
                bprList.AddRange(bprLegs);
                bprList.AddRange(bprArms);
                if (canDecapitate)
                {
                    bprList.Add(victim.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def == BodyPartDefOf.Head));
                }
                if (bprList.Count > 0)
                {
                    BodyPartRecord bpr = bprList.RandomElement();
                    DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Dismember, 100, TMDamageDefOf.DamageDefOf.TM_Dismember.defaultArmorPenetration, -1, caster, bpr, null, DamageInfo.SourceCategory.ThingOrUnknown, victim, true, true);
                    for (int i = 0; i < 3; i++)
                    {
                        TM_MoteMaker.ThrowBloodSquirt(victim.DrawPos, victim.Map, Rand.Range(.6f, 1f));
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        float moteSize = Rand.Range(.5f, .8f);
                        float solidTime = Rand.Range(.6f, .8f);
                        float fadeOutTime = Rand.Range(.2f, .4f);
                        float velocity = Rand.Range(1.5f, 2.5f);
                        float velocityAngle = Rand.Range(0f, 360f);
                        for (int j = 0; j < 3; j++)
                        {
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodSquirt, victim.DrawPos, victim.Map, moteSize - (.1f * j), solidTime + (.1f * j), 0f, fadeOutTime + (.05f * j), Rand.Range(-50, 50), velocity + (.5f * j), velocityAngle, Rand.Range(0, 360));
                        }
                    }
                    victim.TakeDamage(dinfo);
                    Find.CameraDriver.shaker.DoShake(.03f);
                    if (effectLevel > 1)
                    {
                        TM_Action.DoAction_HealPawn(caster, caster, (int)effectLevel, 20);
                    }
                }
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability)
        {
            if (target == null)
            {
                return false;
            }
            if (target.Thing == null)
            {
                return false;
            }
            if (!(target.Thing is Pawn))
            {
                return false;
            }
            Pawn p = target.Thing as Pawn;
            if (p.Dead)
            {
                return false;
            }
            if (p.health == null)
            {
                return false;
            }
            if (p.health.hediffSet == null)
            {
                return false;
            }
            if (base.CanApplyOn(target, caster, ability))
            {
                bool flagLoS = TM_Calc.HasLoSFromTo(caster.Position, target, caster, ability.autocasting.minRange, ability.autocasting.maxRange);
                if (ability.autocasting.requiresLoS && !flagLoS)
                {
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
