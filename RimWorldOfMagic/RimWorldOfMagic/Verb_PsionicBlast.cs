using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using TorannMagic.ClassProgression;

namespace TorannMagic
{
    class Verb_PsionicBlast : Verb_UseAbility  
    {

        internal const string Order="TM_SpiritPsionicBlast";
        internal static bool HasInitialEnergy(Pawn owner) =>
            owner?.health?.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PsionicHD) is not Hediff energy || energy.Severity >= 20f;

        internal static bool PayInitialEnergy(Pawn owner)
        {
            var energy=owner?.health?.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PsionicHD);
            if(energy==null)return true; // Copied abilities retain the ordinary non-Psionic path.
            if(energy.Severity<20f)return false;
            energy.Severity-=20f;
            return true;
        }
        internal static bool Family(TMAbilityDef def) => def!=null &&
            (def==TorannMagicDefOf.TM_PsionicBlast || def==TorannMagicDefOf.TM_PsionicBlast_I ||
             def==TorannMagicDefOf.TM_PsionicBlast_II || def==TorannMagicDefOf.TM_PsionicBlast_III);
        internal static Verb_PsionicBlast For(Pawn body,MightAbility power) =>
            new Verb_PsionicBlast {caster=body,Ability=power,verbProps=power.mightDef.MainVerb};

        internal static bool Valid(Pawn body,MightAbility power,LocalTargetInfo target) =>
            body?.Spawned==true && power!=null && target.IsValid && target.Cell.InBounds(body.Map) &&
            (!target.HasThing || target.Thing.Spawned && target.Thing.Map==body.Map) &&
            target.Thing!=body && !target.Cell.Fogged(body.Map) &&
            For(body,power).CanHitTargetFrom(body.Position,target);

        internal static void Choose(Pawn body,Pawn soul,MightAbility power)
        {
            var job=DefDatabase<JobDef>.GetNamed(Order);
            bool Available(LocalTargetInfo t) => SpiritHostedMartial.Find(body,soul,Order)==power &&
                SpiritHostedMartial.Ready(body,power,Order) && Valid(body,power,t);
            Find.Targeter.BeginTargeting(power.mightDef.MainVerb.targetParams,
                t=>{if(Available(t))body.jobs.TryTakeOrderedJob(JobMaker.MakeJob(job,soul,t),Verse.AI.JobTag.Misc);},
                highlightAction:t=>GenDraw.DrawRadiusRing(body.Position,power.mightDef.MainVerb.range),
                targetValidator:Available,caster:body);
        }

        internal bool Fire(LocalTargetInfo target)
        {
            currentTarget=target;
            return TryCastShot();
        }
        protected override bool TryCastShot()
        {

            Pawn pawn = this.CasterPawn;
            var source=Ability.Pawn;
            int verVal = TM_Calc.GetSkillVersatilityLevel(source, this.Ability.Def as TMAbilityDef);
            Map map = this.CasterPawn.Map;
            IntVec3 targetVariation = this.currentTarget.Cell;
            targetVariation.x += Mathf.RoundToInt(Rand.Range(-.1f, .1f) * Vector3.Distance(pawn.DrawPos, this.currentTarget.CenterVector3) + Rand.Range(-1f, 1f)) ;
            targetVariation.z += Mathf.RoundToInt(Rand.Range(-.1f, .1f) * Vector3.Distance(pawn.DrawPos, this.currentTarget.CenterVector3) + Rand.Range(-1f, 1f));
            targetVariation = ConstrainSpread(targetVariation,map.Size);
            float angle = (Quaternion.AngleAxis(90, Vector3.up) * GetVector(pawn.Position, targetVariation)).ToAngleFlat();
            Vector3 drawPos = pawn.DrawPos + (GetVector(pawn.Position, targetVariation) * .5f);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiBlastStart, drawPos, pawn.Map, Rand.Range(.4f, .6f), Rand.Range(.0f, .05f), .1f, .2f, 0, 0, 0, angle); //throw psi blast start
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiBlastEnd, drawPos, pawn.Map, Rand.Range(.4f, .8f), Rand.Range(.0f, .1f), .2f, .3f, 0, Rand.Range(1f, 1.5f), angle, angle); //throw psi blast end 
            if(source==pawn) this.TryLaunchProjectile(this.verbProps.defaultProjectile, targetVariation);
            else
            {
                var shot=(Projectile_PsionicBlast)GenSpawn.Spawn(verbProps.defaultProjectile,pawn.Position,map);
                shot.Prepare(source,pawn,false);
                shot.Launch(pawn,Ability.Def,pawn.DrawPos,targetVariation,ProjectileHitFlags.All,false,null,null,null,null);
            }
            float psiEnergy = 0;
            if (source.health.hediffSet.HasHediff(HediffDef.Named("TM_PsionicHD"), false))
            {
                psiEnergy = source.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_PsionicHD"), false).Severity;
                HealthUtility.AdjustSeverity(source, HediffDef.Named("TM_PsionicHD"), -(4 - verVal));
            }
            return Rand.Chance(((verVal*4)+psiEnergy)/100);
        }

        internal static IntVec3 ConstrainSpread(IntVec3 cell, IntVec3 mapSize) =>
            new IntVec3(Mathf.Clamp(cell.x,0,mapSize.x-1),cell.y,Mathf.Clamp(cell.z,0,mapSize.z-1));

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos) =>
            (objectPos-center).ToVector3().normalized;
    }
}

namespace TorannMagic
{
    public sealed class JobDriver_PsionicVolley : Verse.AI.JobDriver
    {
        private TMAbilityDef rank;
        private IntVec3 origin;
        private int mapId, fired, nextShotAt;
        private bool stopped;
        private MightAbility Power => SpiritHostedMartial.Find(pawn,job.targetA.Pawn,Verb_PsionicBlast.Order);
        private bool CanFire(MightAbility power) => power!=null && (rank==null || power.Def==rank) &&
            pawn.Spawned && !pawn.Dead && !pawn.Downed && !pawn.InMentalState &&
            !pawn.WorkTagIsDisabled(WorkTags.Violent) && pawn.stances?.stunner?.Stunned!=true &&
            (rank==null || pawn.Position==origin && pawn.Map.uniqueID==mapId) &&
            Verb_PsionicBlast.Valid(pawn,power,job.targetB);

        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            CanFire(Power) && SpiritHostedMartial.Ready(pawn,Power,Verb_PsionicBlast.Order);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref rank,"psionicVolleyRank");
            Scribe_Values.Look(ref origin,"psionicVolleyOrigin",IntVec3.Invalid);
            Scribe_Values.Look(ref mapId,"psionicVolleyMap");
            Scribe_Values.Look(ref fired,"psionicVolleyFired");
            Scribe_Values.Look(ref nextShotAt,"psionicVolleyNext");
            Scribe_Values.Look(ref stopped,"psionicVolleyStopped");
        }

        protected override System.Collections.Generic.IEnumerable<Verse.AI.Toil> MakeNewToils()
        {
            var aim=Verse.AI.Toils_General.Wait(1,Verse.AI.TargetIndex.B);
            aim.initAction=()=>{
                var power=Power;
                if(!CanFire(power) || !SpiritHostedMartial.Ready(pawn,power,Verb_PsionicBlast.Order))
                {EndJobWith(Verse.AI.JobCondition.Incompletable);return;}
                rank=power.mightDef;origin=pawn.Position;mapId=pawn.Map.uniqueID;
                ticksLeftThisToil=Mathf.Max(1,GenTicks.SecondsToTicks(rank.MainVerb.warmupTime*Compatibility.AbilityTiming.WarmupMultiplier));
            };
            yield return aim;
            yield return new Verse.AI.Toil {
                defaultCompleteMode=Verse.AI.ToilCompleteMode.Never,initAction=ShootWhenDue,tickAction=ShootWhenDue
            };
        }

        private void ShootWhenDue()
        {
            if(rank==null || stopped || fired>=rank.MainVerb.burstShotCount)
            {EndJobWith(Verse.AI.JobCondition.Succeeded);return;}
            if(Find.TickManager.TicksGame<nextShotAt)return;
            var power=Power;
            if(!CanFire(power) || fired==0 && !SpiritHostedMartial.Ready(pawn,power,Verb_PsionicBlast.Order))
            {EndJobWith(Verse.AI.JobCondition.Incompletable);return;}
            if(fired==0 && !Verb_PsionicBlast.PayInitialEnergy(power.Pawn))
            {EndJobWith(Verse.AI.JobCondition.Incompletable);return;}
            stopped=!Verb_PsionicBlast.For(pawn,power).Fire(job.targetB);
            fired++;
            if(fired==1)power.PostAbilityAttempt();
            nextShotAt=Find.TickManager.TicksGame+Mathf.Max(1,rank.MainVerb.ticksBetweenBurstShots);
            if(stopped || fired>=rank.MainVerb.burstShotCount)EndJobWith(Verse.AI.JobCondition.Succeeded);
        }
    }
}
