using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using RimWorld;
using System.Collections.Generic;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Projectile_ThunderStrike : Projectile_AbilityBase
    {

        Vector3 origin;
        Vector3 destination;
        Vector3 direction;
        Vector3 directionOffsetRight;
        Vector3 directionOffsetLeft;

        int iteration = 0;
        int maxIteration = 4;
        float directionMagnitudeOffset = 1.5f;
        private bool initialized = false;

        private int verVal = 0;
        private int pwrVal = 0;
        private float arcaneDmg = 1f;

        int nextEventTick = 0;
        int nextRightEventTick = 0;
        int nextLeftEventTick = 0;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Vector3>(ref this.origin, "origin", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.destination, "destination", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.direction, "direction", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.directionOffsetRight, "directionOffsetRight", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.directionOffsetLeft, "directionOffsetLeft", default(Vector3), false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.iteration, "iteration", 0, false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
        }

        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);

            if(!this.initialized)
            {
                Initialize(base.Position, this.launcher as Pawn);
            }
            Vector3 directionOffset = default(Vector3);

            if(initialized && this.nextLeftEventTick < Find.TickManager.TicksGame && this.nextLeftEventTick != 0)
            {
                directionOffset = this.directionOffsetLeft * (this.directionMagnitudeOffset * this.iteration);
                DoThunderStrike(directionOffset);
                this.nextLeftEventTick = 0;
            }
            if(initialized && this.nextRightEventTick < Find.TickManager.TicksGame && this.nextRightEventTick != 0)
            {
                directionOffset = this.directionOffsetRight * (this.directionMagnitudeOffset * this.iteration);
                DoThunderStrike(directionOffset);
                this.nextRightEventTick = 0;
            }
            if (this.initialized && this.nextEventTick < Find.TickManager.TicksGame)
            {               
                
                if(iteration == 1 && verVal > 0)
                {
                    this.nextRightEventTick = Find.TickManager.TicksGame + Rand.Range(2, 6);
                    this.nextLeftEventTick = Find.TickManager.TicksGame + Rand.Range(2, 6);                    
                }
                if (iteration == 3 && verVal > 1)
                {
                    this.nextRightEventTick = Find.TickManager.TicksGame + Rand.Range(2, 6);
                    this.nextLeftEventTick = Find.TickManager.TicksGame + Rand.Range(2, 6);
                }
                if (iteration == 5 && verVal > 2)
                {
                    this.nextRightEventTick = Find.TickManager.TicksGame + Rand.Range(2, 6);
                    this.nextLeftEventTick = Find.TickManager.TicksGame + Rand.Range(2, 6);
                }
                this.iteration++;
                directionOffset = this.direction * (this.directionMagnitudeOffset * this.iteration);
                DoThunderStrike(directionOffset);

                this.nextEventTick = Find.TickManager.TicksGame + Rand.Range(2,5);                
            }                       

        }

        private void DoThunderStrike(Vector3 directionOffset)
        {
            IntVec3 currentPos = default(IntVec3);
            if (directionOffset != default(Vector3))
            {
                currentPos = (this.origin + directionOffset).ToIntVec3();
                if (currentPos != default(IntVec3) && currentPos.IsValid && currentPos.InBounds(base.Map) && currentPos.Walkable(base.Map) && currentPos.DistanceToEdge(base.Map) > 3)
                {
                    CellRect cellRect = CellRect.CenteredOn(currentPos, 1);
                    //cellRect.ClipInsideMap(base.Map);
                    IntVec3 rndCell = cellRect.RandomCell;
                    if (rndCell != null && rndCell != default(IntVec3) && rndCell.IsValid && rndCell.InBounds(base.Map) && rndCell.Walkable(base.Map) && rndCell.DistanceToEdge(base.Map) > 3)
                    {
                        Map.weatherManager.eventHandler.AddEvent(new TM_WeatherEvent_MeshFlash(base.Map, rndCell, TM_MatPool.chiLightning, TMDamageDefOf.DamageDefOf.TM_ChiBurn, this.launcher, Mathf.RoundToInt(Rand.Range(8, 14) * (1 +(.12f * pwrVal)) * this.arcaneDmg), Rand.Range(1.5f, 2f)));
                    }
                }
            }
        }

        private void Initialize(IntVec3 target, Pawn pawn)
        {
            if (target != null && pawn != null)
            {
                verVal = TM_Calc.GetSkillVersatilityLevel(pawn, TorannMagicDefOf.TM_ThunderStrike, false);
                pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_ThunderStrike, false);
                //verVal = TM_Calc.GetMightSkillLevel(pawn, pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_ThunderStrike, "TM_ThunderStrike", "_ver", true);
                //pwrVal = TM_Calc.GetMightSkillLevel(pawn, pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_ThunderStrike, "TM_ThunderStrike", "_pwr", true);
                //this.verVal = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_ThunderStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ThunderStrike_ver").level;
                //this.pwrVal = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_ThunderStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ThunderStrike_pwr").level;
                //if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                //{
                //    MightPowerSkill mver = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                //    MightPowerSkill mpwr = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                //    verVal = mver.level;
                //    pwrVal = mpwr.level;
                //}
                this.arcaneDmg = pawn.GetComp<CompAbilityUserMight>().mightPwr;
                this.origin = pawn.Position.ToVector3Shifted();
                this.destination = target.ToVector3Shifted();
                this.direction = TM_Calc.GetVector(this.origin, this.destination);
                this.directionOffsetRight = Quaternion.AngleAxis(30, Vector3.up) * direction;
                this.directionOffsetLeft = Quaternion.AngleAxis(-30, Vector3.up) * direction;
                //Log.Message("origin: " + this.origin + " destination: " + this.destination + " direction: " + this.direction + " directionRight: " + this.directionOffsetRight);
                this.maxIteration = this.maxIteration + verVal;
                initialized = true;
                HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_HediffInvulnerable, .05f);
            }
            else
            {
                Log.Warning("Failed to initialize " + this.def.defName);
                this.iteration = this.maxIteration;
            }            
        }        

        public override void Tick()
        {
            base.Tick();
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (this.iteration >= this.maxIteration)
            {
                Pawn pawn = this.launcher as Pawn;
                if(!pawn.DestroyedOrNull() && !pawn.Dead && pawn.Spawned)
                {
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HediffInvulnerable, false);
                    if (hediff != null)
                    {
                        pawn.health.RemoveHediff(hediff);
                    }
                }
                base.Destroy(mode);
            }
        }
    }
}
