using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    internal class JobDriver_PlacePoisonTrap : JobDriver
    {
        private bool paid;
        private bool NewOrder => job.def.defName==Verb_PoisonTrap.Order;
        private Pawn Owner => NewOrder ? job.targetB.Pawn : pawn;

        internal static bool OpenCell(Map map,IntVec3 cell)
        {
            if (map==null || !cell.InBounds(map) || cell.Fogged(map) || !cell.Standable(map) || cell.GetEdifice(map)!=null) return false;
            var def=ThingDef.Named("TM_PoisonTrap");
            return !cell.GetThingList(map).Any(t=>GenSpawn.SpawningWipes(def,t.def));
        }
        private MightAbility OrderedPower() => SpiritHostedMartial.Find(pawn,Owner,Verb_PoisonTrap.Order);
        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            OpenCell(pawn.Map,TargetLocA) && (!NewOrder || OrderedPower()!=null) &&
            pawn.Reserve(job.targetA,job,1,-1,null,errorOnFailed);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref paid,"rwomRangerTrapPaid");
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(()=>!OpenCell(pawn.Map,TargetLocA) || (NewOrder && OrderedPower()==null));
            // Retain the original two toil indices so old, paid walks resume at the same step.
            var go=new Toil { defaultCompleteMode=ToilCompleteMode.PatherArrival };
            go.initAction=()=>{
                if (NewOrder && !paid)
                {
                    var power=OrderedPower();
                    if (!SpiritHostedMartial.Ready(pawn,power,Verb_PoisonTrap.Order) || !Verb_PoisonTrap.Valid(pawn,power,job.targetA))
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }
                    paid=true;
                    power.PostAbilityAttempt();
                }
                pawn.pather.StartPath(TargetLocA,PathEndMode.Touch);
            };
            yield return go;
            yield return Toils_General.Do(()=>{
                if ((NewOrder && (!paid || OrderedPower()==null)) || !OpenCell(pawn.Map,TargetLocA) ||
                    !pawn.CanReachImmediate(TargetLocA,PathEndMode.Touch))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                SingleSpawnLoop(new SpawnThings {def=ThingDef.Named("TM_PoisonTrap")},Owner,TargetLocA,pawn.Map);
            });
        }

        public void SingleSpawnLoop(SpawnThings spawnables,Pawn pawn,IntVec3 position,Map map)
        {
            var def=spawnables?.def;
            var comp=pawn?.GetCompAbilityUserMight();
            if (def?.thingClass==null || !typeof(Building_PoisonTrap).IsAssignableFrom(def.thingClass) || comp==null || !OpenCell(map,position)) return;
            var trap=(Building_PoisonTrap)ThingMaker.MakeThing(def,def.MadeFromStuff ? ThingDefOf.WoodLog : null);
            int capacity=System.Math.Max(1,2+TM_Calc.GetSkillVersatilityLevel(pawn,TorannMagicDefOf.TM_PoisonTrap));
            trap.SetFaction(pawn.Faction);
            trap.SetPlanter(pawn);
            GenSpawn.Spawn(trap,position,map,Rot4.North,WipeMode.Vanish,false);
            comp.combatItems ??= new List<Thing>();
            comp.combatItems.RemoveAll(t=>t==null || t.Destroyed);
            comp.combatItems.Add(trap);
            var excess=comp.combatItems.OfType<Building_PoisonTrap>().ToList();
            if (excess.Count<=capacity) return;
            Messages.Message("TM_TooManyTraps".Translate(pawn.LabelShort,capacity),MessageTypeDefOf.NeutralEvent);
            for(int i=0;i<excess.Count-capacity;i++)
            {
                var oldest=excess[i];
                comp.combatItems.Remove(oldest);
                oldest.Destroy();
            }
        }
    }
}
