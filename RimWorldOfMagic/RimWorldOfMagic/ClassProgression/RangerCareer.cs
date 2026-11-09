using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class RangerCareer
    {
        internal const string Id = "rwom.ranger";
        internal const int RequiredDays = 12;
        private const string Trial = "rwom.ranger.companion";
        internal static DisciplineProgress Path(Pawn pawn) =>
            ClassIdentity.TryGetState(pawn, out var state) ? state.Find(Id) : null;

        internal static DisciplineProgress ActivePath(Pawn body,out Pawn owner)
        {
            owner=body;
            var path=Path(body);
            if (path!=null || body==null || !SpiritRebirthState.TryGetPossessingSpirit(body,out var soul) ||
                !SpiritRebirthState.TryGet(soul,out var state) || state.DepletionPending) return path;
            path=Path(soul);
            if (path!=null) owner=soul;
            return path;
        }

        internal static Pawn Companion(Pawn ranger) => ranger.GetCompAbilityUserMight()?.bondedPet;
        internal static bool Ready(Pawn ranger)
        {
            var path = Path(ranger);
            return path?.Stage == InitiationStage.Foundation && path.RangerCareDays >= RequiredDays;
        }

        internal static bool Eligible(Pawn ranger, Pawn pet)
        {
            var path = Path(ranger);
            var body=CareBody(ranger);
            return path?.Stage == InitiationStage.Foundation && path.RangerCareDays < RequiredDays &&
                body?.Spawned==true && !body.Dead && !body.Downed && pet?.Spawned == true && !pet.Dead &&
                pet.Map == body.Map && pet.Faction == body.Faction && pet.RaceProps.Animal &&
                Companion(ranger) == pet && pet.health.hediffSet.HasHediff(TorannMagicDefOf.TM_RangerBondHD) &&
                Find.TickManager.TicksGame >= path.RangerCareReadyAt;
        }

        internal static Pawn CareBody(Pawn ranger)
        {
            if (ranger==null || ranger.Dead) return null;
            if (ranger.Spawned) return SpiritRebirthActionPolicy.Marked(ranger)?null:ranger;
            if (!SpiritRebirthState.TryGet(ranger,out var state) || state.DepletionPending) return null;
            var host=state.Host;
            return host?.IsColonistPlayerControlled==true && Path(host)==null &&
                SpiritRebirthState.TryGetPossessingSpirit(host,out var held) && held==ranger ? host : null;
        }

        internal static void OrderCare(Pawn ranger,Pawn pet)
        {
            var body=CareBody(ranger);
            if (body==null || !Eligible(ranger,pet) || pet.IsForbidden(body) ||
                !body.CanReserveAndReach(pet,PathEndMode.Touch,Danger.Some)) return;
            var job=JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_RangerCompanionCare"),pet);
            if(body!=ranger) { job.targetB=ranger; job.targetC=body.Position; }
            body.jobs.TryTakeOrderedJob(job,JobTag.Misc);
        }
        internal static void BondChanged(Pawn ranger)
        {
            var path = Path(ranger);
            if (path?.Stage != InitiationStage.Foundation || path.RangerCareDays >= RequiredDays) return;
            path.RangerPetId = 0;
            path.RangerCareDays = 0;
        }

        internal static bool FinishCare(Pawn ranger, Pawn pet)
        {
            if (!Eligible(ranger, pet) || !CareBody(ranger).CanReachImmediate(pet, PathEndMode.Touch)) return false;
            var path = Path(ranger);
            if (path.RangerPetId != pet.thingIDNumber)
            {
                path.RangerCareDays = 0;
                path.RangerPetId = pet.thingIDNumber;
            }
            path.RangerCareReadyAt = Find.TickManager.TicksGame + GenDate.TicksPerDay;
            if (++path.RangerCareDays == RequiredDays) path.RecordCompletedTrial(Trial);
            return true;
        }

        internal static int Days(Pawn ranger)
        {
            var path = Path(ranger);
            if (path == null) return 0;
            return path.RangerCareDays >= RequiredDays || Companion(ranger)?.thingIDNumber == path.RangerPetId ? path.RangerCareDays : 0;
        }

        private static MightPower Owner(CompAbilityUserMight might, TMAbilityDef ability) =>
            ability == null ? null : might?.ExistingMightData?.MightPowersR.FirstOrDefault(p => p.TMabilityDefs.Contains(ability));

        internal static bool Claim(Pawn ranger)
        {
            if (!Ready(ranger)) return false;
            var might = ranger.GetCompAbilityUserMight();
            if (Owner(might,TorannMagicDefOf.TM_PoisonTrap)?.abilityDef == null ||
                !ClassIdentity.Component(ranger).State.TryAdvanceAfterTrial(Id, InitiationStage.Foundation, Trial)) return false;
            Restore(might);
            return true;
        }

        internal static void Restore(CompAbilityUserMight might)
        {
            var path = Path(might.Pawn);
            if (path == null) return;
            Grant(might, TorannMagicDefOf.TM_AnimalFriend);
            RangerMastery.Restore(might);
            RangerAscension.Restore(might, path);
            if (path.Stage >= InitiationStage.Adept) Grant(might, TorannMagicDefOf.TM_PoisonTrap);
        }

        private static void Grant(CompAbilityUserMight might, TMAbilityDef ability)
        {
            var owner = Owner(might, ability);
            if (owner?.abilityDef == null) return;
            owner.learned = true;
            TM_ClassState.AddRuntimeAbility(might, owner.abilityDef);
        }
    }

    public sealed class JobDriver_RangerCompanionCare : JobDriver
    {
        private Pawn Pet => job.targetA.Pawn;
        private Pawn Ranger => job.targetC.IsValid ? job.targetB.Pawn : pawn;
        private bool Valid => RangerCareer.CareBody(Ranger)==pawn && RangerCareer.Eligible(Ranger,Pet);
        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            Valid && !Pet.IsForbidden(pawn) && pawn.Reserve(Pet,job,1,-1,null,errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !Valid);
            yield return Toils_Goto.GotoThing(TargetIndex.A,PathEndMode.Touch);
            yield return Toils_General.WaitWith(TargetIndex.A,600,useProgressBar:true,maintainPosture:true);
            yield return Toils_General.Do(() =>
            {
                if (!Valid || !RangerCareer.FinishCare(Ranger,Pet)) EndJobWith(JobCondition.Incompletable);
            });
        }
    }
}
