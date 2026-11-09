using System.Collections.Generic;
using RimWorld;
using TorannMagic.ClassProgression;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    internal class JobDriver_PsionicBarrier : JobDriver
    {
        private const int Duration=900;
        private int age=-1;
        private readonly List<IntVec3> ring=new();
        private readonly List<Projectile> crossing=new();
        private float radius;
        private bool Hosted => job.count==1 || job.count==2;
        private Pawn Source => Hosted ? job.targetB.Pawn : pawn;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref age,"psionicBarrierAge",-1);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)=>true;

        private bool OwnsChannel(Pawn source)
        {
            if (!Hosted) return source==pawn;
            string order=job.count==2 ? Verb_PsionicBarrier.FieldOrder : Verb_PsionicBarrier.SelfOrder;
            return source!=null && source!=pawn && SpiritHostedMartial.Find(pawn,source,order)!=null;
        }

        internal float Radius => radius>0f ? radius : CalculateRadius();
        private float CalculateRadius()=>2.5f+.75f*TM_Calc.GetSkillVersatilityLevel(Source,TorannMagicDefOf.TM_PsionicBarrier);
        private void BuildRing()
        {
            radius=CalculateRadius();
            int outer=GenRadial.NumCellsInRadius(radius);
            for (int i=GenRadial.NumCellsInRadius(radius-2f);i<outer;i++)
            {
                var cell=TargetLocA+GenRadial.RadialPattern[i];
                if (cell.InBounds(Map)) ring.Add(cell);
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(()=>!pawn.Spawned || pawn.Downed);
            var barrier=new Toil {defaultCompleteMode=ToilCompleteMode.Delay,defaultDuration=Duration};
            barrier.initAction=()=> { age=0; ticksLeftThisToil=10; };
            barrier.tickAction=TickBarrier;
            barrier.WithProgressBar(TargetIndex.A,()=>1f-ticksLeftThisToil/100f);
            barrier.AddFinishAction(()=>{
                var physical=pawn.GetCompAbilityUserMight();
                if (physical!=null) physical.shouldDrawPsionicShield=false;
                if (Source==pawn && pawn.story?.traits?.HasTrait(TorannMagicDefOf.Faceless)==true &&
                    physical?.mimicAbility!=null) physical.RemovePawnAbility(physical.mimicAbility);
            });
            yield return barrier;
        }

        private void TickBarrier()
        {
            var source=Source;
            var comp=source?.GetCompAbilityUserMight();
            if (comp==null || !OwnsChannel(source)) { EndJobWith(JobCondition.Incompletable);return; }
            if (age<0) age=Mathf.Max(0,Find.TickManager.TicksGame-startTick);
            if (ring.Count==0) BuildRing();
            var physical=pawn.GetCompAbilityUserMight();
            if (physical!=null) physical.shouldDrawPsionicShield=true;
            var energy=PsionicMartialPath.Energy(source);
            var ranks=comp.MightData.MightPowerSkill_PsionicBarrier;
            int power=ranks.Find(s=>s.label=="TM_PsionicBarrier_pwr")?.level ?? 0;
            int eff=ranks.Find(s=>s.label=="TM_PsionicBarrier_eff")?.level ?? 0;
            Repel(source,comp,energy!=null && !Rand.Chance(.15f*power)?energy:null,eff);
            age++;
            if (energy!=null)
            {
                energy.Severity-=pawn.IsColonist ? .004f : .04f;
                ticksLeftThisToil=(int)energy.Severity;
                if (energy.Severity<1) EndJobWith(JobCondition.Succeeded);
            }
            else
            {
                if (comp.Stamina==null) {EndJobWith(JobCondition.Incompletable);return;}
                comp.Stamina.CurLevel-=.00035f;
                ticksLeftThisToil=Mathf.RoundToInt((Duration-age)*100f/Duration);
                if (age>Duration || comp.Stamina.CurLevel<.01f) EndJobWith(JobCondition.Succeeded);
            }
        }

        private void Repel(Pawn source,CompAbilityUserMight comp,Hediff energy,int efficiency)
        {
            crossing.Clear();
            foreach (var cell in ring)
                foreach (var thing in cell.GetThingList(Map))
                    if (thing is Projectile shot && shot.Launcher!=null && shot.Launcher.Position.DistanceTo(TargetLocA)>radius+.5f)
                        crossing.Add(shot);
            foreach (var shot in crossing)
            {
                if (!shot.Spawned || shot.Map!=Map) continue;
                var cell=shot.Position;
                var def=shot.def;
                var properties=def.projectile;
                float damage=properties.GetDamageAmount(1,null);
                bool hostile=PsionicService.Hostile(pawn,shot.Launcher);
                var pos=cell.ToVector3Shifted();
                pos.x+=Rand.Range(-.3f,.3f);pos.z+=Rand.Range(-.3f,.3f);
                TM_MoteMaker.ThrowGenericFleck(FleckDefOf.LightningGlow,pos,Map,damage/8f,.2f,.1f,.3f,0,0,0,Rand.Range(0,360));
                if (energy!=null) energy.Severity-=PsionicMartialPath.ProjectileCost(source,damage/(12+efficiency));
                else if (comp.Stamina!=null) comp.Stamina.CurLevel-=damage/600f;
                if (properties.explosionRadius>0 && def!=TorannMagicDefOf.Projectile_FogOfTorment)
                    GenExplosion.DoExplosion(cell,Map,properties.explosionRadius,properties.damageDef,pawn,(int)damage,
                        properties.GetArmorPenetration(null,null),properties.soundExplode,null,def,null,
                        properties.postExplosionSpawnThingDef,properties.postExplosionSpawnChance,properties.postExplosionSpawnThingCount,
                        null,0f,1,properties.applyDamageToExplosionCellsNeighbors,properties.preExplosionSpawnThingDef,
                        properties.preExplosionSpawnChance,properties.preExplosionSpawnThingCount,properties.explosionChanceToStartFire,properties.explosionDamageFalloff);
                if(shot.Spawned)shot.Destroy(DestroyMode.Vanish);
                if(hostile && shot.Destroyed)PsionicMartialPath.Service(pawn,hostileDamage:damage);
            }
        }
    }
}
