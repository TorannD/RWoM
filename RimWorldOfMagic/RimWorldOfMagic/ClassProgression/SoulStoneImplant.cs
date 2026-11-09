using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class SoulStoneImplant
    {
        internal static bool EnterFromMap(Pawn original,Pawn host)
        {
            if (!SpiritRebirthFormPolicy.IsSpawnedEthereal(original) || !SpiritRebirthEnergy.CanCast(original) ||
                host == null || host == original || SpiritSummonedHost.IsSummoned(host) || !SpiritRebirthState.TryGet(original,out var state) ||
                host.health?.hediffSet == null || host.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SpiritPossessionHD)) return false;
            var map=original.Map; var origin=original.Position;
            bool reanimated=host.Dead;
            if (reanimated ? host.Corpse?.Map != map : host.Map != map) return false;
            var hd=HediffMaker.MakeHediff(TorannMagicDefOf.TM_SpiritPossessionHD,host);
            var link=hd.TryGetComp<HediffComp_SpiritPossession>();
            if (link == null || (reanimated && !ResurrectionUtility.TryResurrect(host))) return false;
            var holder=link.PrepareSoulHolder();
            bool entered=false;
            try
            {
                host.health.AddHediff(hd);
                if (!host.health.hediffSet.hediffs.Contains(hd)) return false;
                original.DeSpawn(DestroyMode.WillReplace);
                if (!holder.TryAdd(original,false) || !state.TryEnterHost(original,host)) return false;
                entered=true;
                link.ReanimatedHost=reanimated;
                link.BorrowAllegiance(original.Faction);
                original.GetCompAbilityUserMagic()?.RestoreSavedCooldowns();
                SpiritRebirthHealthForm.TryRefreshPending(original);
                return true;
            }
            finally
            {
                if (!entered)
                {
                    if (holder.Contains(original)) holder.Remove(original);
                    if (!original.Spawned && original.holdingOwner == null) GenSpawn.Spawn(original,origin,map,original.Rotation);
                    if (host.health.hediffSet.hediffs.Contains(hd)) host.health.RemoveHediff(hd);
                    if (reanimated && !host.Dead && !host.Destroyed) host.Kill(null);
                }
            }
        }
        internal static bool Valid(Pawn actor, Thing_SoulStone stone, Pawn original, Pawn host)
        {
            if (!ClassProgressionFeature.Enabled || original == null || stone == null || stone.Destroyed ||
                stone.ContainedSpirit != original || !SpiritRebirthState.TryGet(original,out var state) || !state.HasValidStone(original) ||
                actor?.IsColonistPlayerControlled != true || actor.Dead || actor.Downed || !actor.Spawned ||
                original.Faction != actor.Faction || stone.MapHeld != actor.Map || host?.Spawned != true || host.Map != actor.Map ||
                host.Dead || host == original || SpiritSummonedHost.IsSummoned(host) || host.RaceProps?.IsFlesh != true ||
                host.health?.hediffSet == null || host.IsShambler || host.IsGhoul || TM_Calc.IsUndeadNotVamp(host) ||
                SpiritRebirthEnergy.Marked(host) || TM_Calc.IsPossessedByOrIsSpirit(host) ||
                host.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SpiritPossessionHD)) return false;
            return host.Faction == actor.Faction || host.IsPrisonerOfColony || host.HostFaction == actor.Faction ||
                (host.RaceProps.Humanlike && host.Faction != null && !host.HostileTo(actor));
        }

        internal static bool Intrusion(Pawn actor,Pawn host)
            => host.IsPrisoner || host.HostFaction != null || host.Faction != actor.Faction;

        internal static IEnumerable<FloatMenuOption> Orders(Pawn actor, Thing_SoulStone stone)
        {
            var original = stone.ContainedSpirit;
            if (!ClassProgressionFeature.Enabled || original == null || !stone.Spawned || actor?.IsColonistPlayerControlled != true ||
                actor.Map != stone.Map || original.Faction != actor.Faction) yield break;
            yield return new FloatMenuOption("TM_SoulStoneChooseHost".Translate(original.LabelShortCap), () =>
                Find.Targeter.BeginTargeting(new TargetingParameters {canTargetPawns=true,canTargetAnimals=true,canTargetSelf=true,
                    canTargetItems=false,canTargetBuildings=false,canTargetLocations=false}, target => {
                    var host=target.Pawn;
                    void Order(bool confirmed)
                    {
                        if (!Valid(actor,stone,original,host) || !actor.CanReserveAndReach(stone,PathEndMode.Touch,Danger.Some) ||
                            (actor != host && !actor.CanReserveAndReach(host,PathEndMode.Touch,Danger.Some)))
                        { Messages.Message("TM_SoulStoneCannotImplant".Translate(),MessageTypeDefOf.RejectInput,false); return; }
                        var job=JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(confirmed ? "TM_SoulStoneImplantHostile" : "TM_SoulStoneImplant"),stone,host,original);
                        actor.jobs.TryTakeOrderedJob(job,JobTag.Misc);
                    }
                    if (Valid(actor,stone,original,host) && Intrusion(actor,host))
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("TM_SoulStoneHarmful".Translate(original.LabelShortCap,host.LabelShortCap),()=>Order(true)));
                    else Order(false);
                }, highlightAction: null, targetValidator: target => Valid(actor, stone, original, target.Pawn), caster: actor));
        }

        internal static bool Complete(Pawn actor,Thing_SoulStone stone,Pawn original,Pawn host,bool allowedIntrusion)
        {
            if (!Valid(actor,stone,original,host) || actor.carryTracker?.CarriedThing != stone ||
                !actor.CanReachImmediate(host,PathEndMode.Touch) || (Intrusion(actor,host) && !allowedIntrusion)) return false;
            bool harmful=Intrusion(actor,host);
            var faction=host.HomeFaction;
            var hd=HediffMaker.MakeHediff(TorannMagicDefOf.TM_SpiritPossessionHD,host);
            var link=hd.TryGetComp<HediffComp_SpiritPossession>();
            if (link == null) return false;
            link.PrepareSoulHolder();
            host.health.AddHediff(hd);
            if (!stone.TryImplant(original,link))
            {
                if (host.health.hediffSet.hediffs.Contains(hd) && link.SpiritPawn == null) host.health.RemoveHediff(hd);
                return false;
            }
            stone.Destroy();
            link.BorrowAllegiance(actor.Faction);
            if (harmful && host.needs?.mood?.thoughts?.memories is MemoryThoughtHandler memories)
                memories.TryGainMemory(DefDatabase<ThoughtDef>.GetNamed("TM_SoulStoneForcedHost"),actor);
            if (harmful && faction != null && faction != actor.Faction && !faction.HostileTo(actor.Faction))
                actor.Faction.TryAffectGoodwillWith(faction,-200,true,true,HistoryEventDefOf.UsedHarmfulAbility);
            return true;
        }
    }

    public sealed class JobDriver_SoulStoneImplant : JobDriver
    {
        private Thing_SoulStone Stone => job.targetA.Thing as Thing_SoulStone;
        private Pawn Host => job.targetB.Pawn;
        private Pawn Original => job.targetC.Pawn;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            job.count=1;
            return SoulStoneImplant.Valid(pawn,Stone,Original,Host) &&
                (pawn.carryTracker.CarriedThing == Stone || pawn.Reserve(Stone,job,1,1,null,errorOnFailed)) &&
                (pawn == Host || pawn.Reserve(Host,job,1,-1,null,errorOnFailed));
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !SoulStoneImplant.Valid(pawn,Stone,Original,Host));
            var approach=Toils_Goto.GotoThing(TargetIndex.B,PathEndMode.Touch);
            yield return Toils_Jump.JumpIf(approach,()=>pawn.carryTracker.CarriedThing == Stone);
            yield return Toils_Goto.GotoThing(TargetIndex.A,PathEndMode.Touch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A,failIfStackCountLessThanJobCount:true);
            yield return approach;
            yield return Toils_General.Do(()=> {
                if (!SoulStoneImplant.Complete(pawn,Stone,Original,Host,job.def.defName == "TM_SoulStoneImplantHostile")) EndJobWith(JobCondition.Incompletable);
            });
        }
    }
}
