using RimWorld;
using Verse;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using TorannMagic.Golems;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_BurningFury : HediffComp
    {

        private bool initializing = true;
        private int nextAction = 1;
        private int nextSlowAction = 1;
        private bool removeNow = false;
        CompAbilityUserMight comp = null;
        private float intensity = 1f;
        private float drain = 1f;

        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }


        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned && base.Pawn.Map != null)
            {
                FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 3f);
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (initializing)
                {
                    initializing = false;
                    this.Initialize();
                }
            }
            if (!this.Pawn.DestroyedOrNull() && this.Pawn.Spawned && !this.Pawn.Downed)
            {
                if(comp == null && TM_Calc.IsMightUser(this.Pawn))
                {
                    comp = this.Pawn.GetCompAbilityUserMight();
                    int pwrVal = comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_pwr").level;
                    if (pwrVal >= 4)
                    {
                        this.intensity = 1.5f;
                        if(pwrVal >= 14)
                        {
                            this.intensity = 2f;
                        }
                    }
                    int verVal = comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_ver").level;
                    if (pwrVal >= 14)
                    {
                        this.drain = .65f;
                    }
                }
                if (Find.TickManager.TicksGame % 30 == 0)
                {                    
                    if (comp != null && comp.Stamina != null)
                    {
                        comp.Stamina.CurLevel -= (.02f * this.drain);
                        if (comp.Stamina.CurLevel <= .001f)
                        {
                            this.removeNow = true;
                        }
                    }
                    else if(Pawn is TMPawnGolem || Pawn is TMHollowGolem)
                    {
                        severityAdjustment -= .01f;
                        if(this.parent.Severity <= .01f)
                        {
                            this.removeNow = true;
                        }
                    }
                    else
                    {
                        this.removeNow = true;
                    }
                }
                if (!removeNow && Find.TickManager.TicksGame >= this.nextAction)
                {
                    this.nextAction = Find.TickManager.TicksGame + Mathf.RoundToInt(Rand.Range(50f/this.intensity, 80f/this.intensity));
                    TickAction();
                }
                if (!removeNow && Find.TickManager.TicksGame % nextSlowAction == 0)
                {
                    this.nextSlowAction = Rand.Range(200, 500);
                    SlowTickAction();
                }
            }
            else
            {
                this.removeNow = true;
            }
        }

        public void TickAction()
        {
            Pawn victim = TM_Calc.FindNearbyEnemy(this.Pawn, 2);
            if (victim != null)
            {
                TM_Action.DamageEntities(victim, null, Rand.Range(4, 6), DamageDefOf.Burn, this.Pawn);
                TM_MoteMaker.ThrowFlames(victim.DrawPos, victim.Map, Rand.Range(.1f, .4f));
            }

            if(Rand.Chance(.2f))
            {
                TM_Action.DamageEntities(this.Pawn, null, Rand.Range(3, 5), 5f, DamageDefOf.Burn, this.Pawn);
                TM_MoteMaker.ThrowFlames(this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.1f, .2f));
            }

        }

        public void SlowTickAction()
        {
            Hediff_Injury injuryToTend = Pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .FirstOrDefault(injury => injury.CanHealNaturally() && injury.TendableNow());
            if (injuryToTend == default) return;

            if (Rand.Chance(.15f))
            {
                DamageInfo dinfo = new DamageInfo(
                    DamageDefOf.Burn,
                    amount: Mathf.RoundToInt(injuryToTend.Severity / 2),
                    instigator: Pawn,
                    hitPart: injuryToTend.Part
                );
                dinfo.SetAllowDamagePropagation(false);
                dinfo.SetInstantPermanentInjury(true);
                injuryToTend.Heal(100);
                TM_MoteMaker.ThrowFlames(Pawn.DrawPos, Pawn.Map, Rand.Range(.1f, .2f));
                Pawn.TakeDamage(dinfo);
            }
            else
            {
                injuryToTend.Tended(1, 1);
                TM_MoteMaker.ThrowFlames(Pawn.DrawPos, Pawn.Map, Rand.Range(.1f, .2f));
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return this.removeNow || !this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BurningFuryHD, false) || base.CompShouldRemove;
            }
        }
    }
}
