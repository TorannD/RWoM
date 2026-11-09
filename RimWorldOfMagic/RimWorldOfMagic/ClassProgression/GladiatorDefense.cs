using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public enum GladiatorDefense { None, Fortitude, Cover }

    public sealed class GladiatorCover : IExposable
    {
        private Pawn guardian, owner;
        private Hediff possession;
        private bool projected;
        private int until;
        private float remaining;
        [ThreadStatic] private static bool redirecting;
        public GladiatorCover() { }
        internal GladiatorCover(Pawn guardian, int now)
        {
            this.guardian = guardian;
            until = now + 360;
            remaining = 20f;
        }
        public void ExposeData()
        {
            Scribe_References.Look(ref guardian, "guardian");
            Scribe_Values.Look(ref until, "until");
            Scribe_Values.Look(ref remaining, "remaining");
            Scribe_Values.Look(ref projected,"projected");
            Scribe_References.Look(ref owner,"coverOwner");
            Scribe_References.Look(ref possession,"coverPossession");
        }
        internal void Bind(Pawn source)
        {
            owner=source; projected=true;
            possession=guardian.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD);
        }
        internal bool Active(Pawn ally, int now) => remaining > 0 && now < until && JobDriver_GladiatorDefense.Allies(guardian, ally)
            && guardian.Position.AdjacentTo8WayOrInside(ally.Position)
            && JobDriver_GladiatorDefense.Path(projected?owner:guardian)?.Defense == GladiatorDefense.Cover
            && (!projected || possession!=null && guardian.health.hediffSet.hediffs.Contains(possession) &&
                SpiritRebirthState.TryGetPossessingSpirit(guardian,out var held) && held==owner);
        internal void Intercept(ref DamageInfo hit, out bool absorbed)
        {
            absorbed = false;
            if (redirecting || hit.Def == null || hit.Def.isExplosive || !(hit.Amount > 0)
                || hit.WeaponBodyPartGroup != null || hit.Weapon == null
                || !(hit.Weapon.IsRangedWeapon || hit.Weapon.projectile != null)) return;
            var share = Math.Min(remaining, hit.Amount);
            GladiatorMastery.Defended(projected?owner:guardian, hit.Instigator);
            remaining -= share;
            hit.SetAmount(hit.Amount - share);
            absorbed = hit.Amount <= 0;
            var redirected = new DamageInfo(hit.Def, share, hit.ArmorPenetrationInt, hit.Angle, hit.Instigator, null, hit.Weapon, hit.Category);
            redirecting = true;
            try { guardian.TakeDamage(redirected); }
            finally { redirecting = false; }
        }
    }

    public sealed class JobDriver_GladiatorDefense : JobDriver
    {
        private bool rendezvousConfirmed;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref rendezvousConfirmed,"evacuationRendezvous");
        }
        private const string HostedCover="TM_SpiritGladiatorCover", HostedDrill="TM_SpiritGladiatorRelief", HostedGraduation="TM_SpiritGladiatorEvacuation";
        private bool Hosted => job.def.defName==HostedCover || job.def.defName==HostedDrill || job.def.defName==HostedGraduation;
        private Pawn Source => Hosted ? job.targetC.Pawn : pawn;
        private bool Graduation => job.def.defName == "TM_GladiatorEvacuationDrill" || job.def.defName==HostedGraduation;
        private const string Drill = "rwom.gladiator.relief";
        private bool Training => Graduation || job.def.defName == "TM_GladiatorReliefDrill" || job.def.defName==HostedDrill;
        internal static DisciplineProgress Path(Pawn pawn) => ClassIdentity.TryGetState(pawn, out var state)
            ? state.Find(JobDriver_GladiatorInitiation.ClassId) : null;
        internal static bool Allies(Pawn pawn, Pawn ally) => pawn != null && ally != null && pawn != ally
            && pawn.Spawned && ally.Spawned && pawn.Map == ally.Map && !pawn.Dead && !pawn.Downed && !ally.Dead
            && pawn.Faction != null && pawn.Faction == ally.Faction && !ally.IsPrisoner && ClassIdentity.Component(ally) != null;

        private static bool HasChoice(Pawn pawn, bool training)
        {
            var path = Path(pawn);
            if (path == null) return false;
            if (training) return (GladiatorAscension.Qualified(pawn) && !path.CompletedTrials.Contains(GladiatorAscension.Trial)) || (path.Stage == InitiationStage.Foundation && !path.CompletedTrials.Contains(Drill))
                || (path.Stage == InitiationStage.Adept && !path.CompletedTrials.Contains(GladiatorMastery.Trial));
            return path.Defense==GladiatorDefense.Cover;
        }
        internal static bool CanOfferProjected(Pawn body,Pawn soul,bool training) =>
            soul!=null && HasChoice(soul,training) && !HasChoice(body,training) &&
            SpiritRebirthState.TryGetPossessingSpirit(body,out var held) && held==soul &&
            SpiritRebirthState.TryGet(soul,out var state) && !state.DepletionPending;

        internal static bool CanWorkFrom(Pawn body,Pawn source,bool training)
        {
            if (body?.IsColonistPlayerControlled!=true || !HasChoice(source,training) ||
                (body!=source ? !CanOfferProjected(body,source,training) : SpiritRebirthActionPolicy.Marked(body))) return false;
            return training || Path(source).CoverReadyAt<=Find.TickManager.TicksGame &&
                source.GetCompAbilityUserMight()?.Stamina is Need_Stamina sp && sp.MaxLevel>0 && sp.CurLevel>=.2f*sp.MaxLevel;
        }
        internal static void Target(Pawn pawn,bool training) => TargetFrom(pawn,pawn,training);

        internal static void TargetFrom(Pawn pawn,Pawn source,bool training)
        {
            bool Valid(Pawn ally) => Allies(pawn,ally) && pawn.Map == Find.CurrentMap && CanWorkFrom(pawn,source,training)
                && (training ? (ally.Position-pawn.Position).LengthHorizontalSquared >= 64 : ally.Position.InHorDistOf(pawn.Position,6));
            Find.Targeter.BeginTargeting(new TargetingParameters { canTargetPawns = true, canTargetItems = false, canTargetBuildings = false,
                validator = t => Valid(t.Thing as Pawn) }, t => {
                    if (!Valid(t.Pawn) || !pawn.CanReach(t.Pawn,PathEndMode.Touch,Danger.Some)) return;
                    bool graduation=training && GladiatorAscension.Qualified(source);
                    string id;
                    if (source==pawn) id=graduation?"TM_GladiatorEvacuationDrill":training?"TM_GladiatorReliefDrill":"TM_GladiatorCoverAlly";
                    else id=graduation?HostedGraduation:training?HostedDrill:HostedCover;
                    var job=JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(id),t.Pawn,pawn.Position);
                    if (source!=pawn) job.targetC=source;
                    pawn.jobs.TryTakeOrderedJob(job,JobTag.Misc);
                });
        }
        internal static bool CompletedTrial(Pawn pawn) => Path(pawn)?.CompletedTrials.Contains(Drill) == true;
        internal static bool Choose(Pawn pawn, GladiatorDefense choice)
        {
            var path = Path(pawn);
            if ((choice != GladiatorDefense.Fortitude && choice != GladiatorDefense.Cover) || path?.Stage != InitiationStage.Foundation
                || path.Defense != GladiatorDefense.None || !CompletedTrial(pawn)) return false;
            var might = pawn.GetCompAbilityUserMight();
            var owner = might?.ExistingMightData?.MightPowersG.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Fortitude));
            if (owner == null || !ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(path.ClassId, InitiationStage.Foundation, Drill)) return false;
            path.Defense = choice;
            if (choice == GladiatorDefense.Fortitude) owner.learned = true;
            return true;
        }
        internal static bool Finish(Pawn pawn, Pawn ally, IntVec3 origin, bool training) => FinishFrom(pawn,pawn,ally,origin,training);
        private static bool FinishFrom(Pawn pawn,Pawn source,Pawn ally,IntVec3 origin,bool training)
        {
            if (!Allies(pawn, ally) || !pawn.Position.AdjacentTo8WayOrInside(ally.Position) || !CanWorkFrom(pawn,source,training)) return false;
            var path=Path(source);
            if (training && path.Stage == InitiationStage.Master) return false; // Graduation requires the return leg.
            if (training) return origin.IsValid && (pawn.Position-origin).LengthHorizontalSquared >= 49
                && path.RecordCompletedTrial(path.Stage == InitiationStage.Foundation ? Drill : GladiatorMastery.Trial);
            var recipient = ClassIdentity.Component(ally);
            int now = Find.TickManager.TicksGame;
            if (recipient.Cover?.Active(ally,now) == true) return false;
            var sp=source.GetCompAbilityUserMight().Stamina;
            path.CoverReadyAt = now + 1800;
            sp.CurLevel -= sp.MaxLevel * .2f;
            recipient.Cover = new GladiatorCover(pawn, now);
            if (source!=pawn) recipient.Cover.Bind(source);
            return true;
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed) => CanWorkFrom(pawn,Source,Training) && Allies(pawn,job.targetA.Pawn) && (!Graduation || (GladiatorAscension.Qualified(Source)
                && job.targetB.Cell == pawn.Position && (job.targetA.Pawn.Position-pawn.Position).LengthHorizontalSquared >= 64));
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !Allies(pawn,job.targetA.Pawn) || !CanWorkFrom(pawn,Source,Training));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            if (Graduation)
            {
                // Rendezvous with the partner, then practise returning to the evacuation point.
                yield return Toils_General.Wait(300,TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A)
                    .FailOn(() => !pawn.Position.AdjacentTo8WayOrInside(job.targetA.Pawn.Position) || (pawn.Position-job.targetB.Cell).LengthHorizontalSquared < 49);
                yield return Toils_General.Do(() => {
                    rendezvousConfirmed = Allies(pawn,job.targetA.Pawn) && pawn.Position.AdjacentTo8WayOrInside(job.targetA.Pawn.Position)
                        && (pawn.Position-job.targetB.Cell).LengthHorizontalSquared >= 49;
                    if (!rendezvousConfirmed) EndJobWith(JobCondition.Incompletable);
                });
                yield return Toils_Goto.GotoCell(TargetIndex.B,PathEndMode.OnCell);
                yield return Toils_General.Do(() => { if (rendezvousConfirmed && pawn.Position == job.targetB.Cell && Allies(pawn,job.targetA.Pawn)) GladiatorAscension.Graduate(Source); });
                yield break;
            }
            yield return Toils_General.Wait(Training && Path(Source)?.Stage == InitiationStage.Adept ? 600 : 60, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            yield return Toils_General.Do(() => { if (!FinishFrom(pawn,Source,job.targetA.Pawn,job.targetB.Cell,Training)) EndJobWith(JobCondition.Incompletable); });
        }
    }
}
