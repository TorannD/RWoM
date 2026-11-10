using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic.Compatibility;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TorannMagic.ClassProgression
{
    public sealed class RangerHuntAbility : MightAbility
    {
        internal const string JobName = "TM_RangerBreakthroughCast";
        public RangerHuntAbility() { }
        public RangerHuntAbility(CompAbilityUser owner) : base(owner) { }
        public RangerHuntAbility(AbilityData data) : base(data) { }
        public RangerHuntAbility(Pawn pawn, AbilityUser.AbilityDef def) : base(pawn, def) { }

        public override Job GetJob(AbilityContext context, LocalTargetInfo target)
        {
            var job = MakeJobFor(Pawn,target);
            job.playerForced = context == AbilityContext.Player;
            return job;
        }

        internal Job MakeJobFor(Pawn body, LocalTargetInfo target)
        {
            var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(JobName), target,
                body.equipment?.Primary, RangerCareer.Companion(Pawn));
            if (body != Pawn) job.AddQueuedTarget(TargetIndex.B, Pawn);
            return job;
        }

        internal Verb_RangerBreakthrough VerbFor(Pawn body) => body == Pawn ? (Verb_RangerBreakthrough)Verb :
            new Verb_RangerBreakthrough { caster=body, Ability=this, verbProps=mightDef.MainVerb };

        public override bool CanCastPowerCheck(AbilityContext context, out string reason)
        {
            if (!base.CanCastPowerCheck(context, out reason)) return false;
            if (JobDriver_RangerBreakthrough.Ready(Pawn)) return true;
            reason = "TM_RangerHuntUnavailable".Translate();
            return false;
        }
    }

    public sealed class Verb_RangerBreakthrough : Verb_SniperBallisticShot
    {
        protected override bool OwnsTechnique(DisciplineProgress path) => RangerAscension.HasHunt(Ability?.Pawn ?? CasterPawn);
        protected override string RoundName => "TM_RangerHuntArrow";
        public override float EffectiveRange => Mathf.Min(30f, base.EffectiveRange);
        internal static bool Bow(Pawn ranger)
        {
            var weapon = ranger?.equipment?.Primary;
            var verb = weapon?.TryGetComp<CompEquippable>()?.PrimaryVerb as Verb_Shoot;
            var round = verb?.Projectile;
            if (verb?.GetType() != typeof(Verb_Shoot) || round?.thingClass != typeof(Bullet) || round.projectile?.explosionRadius != 0f)
                return false;
            // A stored secondary weapon or a bow-like name alone cannot turn a rifle into a bow.
            var damage = round.projectile.damageDef?.defName;
            return damage == "Arrow" || damage == "ArrowHighVelocity" ||
                DefDatabase<WeaponCategoryList>.GetNamedSilentFail("TM_Category_Bows")?.weaponDefNames?.Contains(weapon.def.defName) == true;
        }
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target) =>
            target.IsValid && root.DistanceToSquared(target.Cell) <= 900 && Bow(CasterPawn) && base.CanHitTargetFrom(root, target);
        protected override bool TryCastShot() => false;
        internal bool Fire(Pawn target)
        {
            if (!FireWeapon(target)) return false;
            CasterPawn.equipment.Primary.GetComp<CompEquippable>().PrimaryVerb.verbProps.soundCast?.PlayOneShot(SoundInfo.InMap(CasterPawn));
            return true;
        }
    }

    public sealed class RangerHuntArrow : SniperWeaponBullet
    {
        public override int DamageAmount => Mathf.Max(1, Mathf.RoundToInt(base.DamageAmount * .7f));
    }

    public sealed class JobDriver_RangerBreakthrough : JobDriver
    {
        private const int Arrows = 6;
        private bool begun, paid;
        private int fired, guards, mapId;
        private List<Pawn> marks;
        private RangerHuntAbility owner;
        private bool Hosted => job.targetQueueB?.Count > 0;
        private Pawn Source => Hosted ? job.targetQueueB[0].Pawn : pawn;
        private Verb_RangerBreakthrough firingVerb;
        private RangerHuntAbility Owner => owner ??= Source?.GetCompAbilityUserMight()?.AbilityData?.Powers.OfType<RangerHuntAbility>().FirstOrDefault();
        private Verb_RangerBreakthrough Verb => firingVerb ??= Owner?.VerbFor(pawn);
        internal bool Active => begun && paid && fired < Arrows;

        private static bool HasPair(Pawn ranger, Pawn source, Pawn pet)
        {
            return ranger?.Spawned == true && !ranger.Dead && !ranger.Downed && pet?.Spawned == true && !pet.Dead && !pet.Downed &&
                ranger.Map == pet.Map && pet.Faction == ranger.Faction && ranger.Position.InHorDistOf(pet.Position,12f) &&
                source != null && RangerCareer.Companion(source) == pet && pet.health.hediffSet.HasHediff(TorannMagicDefOf.TM_RangerBondHD);
        }
        internal static bool Ready(Pawn ranger, Pawn source = null)
        {
            source ??= ranger;
            if (ranger?.Spawned != true || !RangerAscension.HasHunt(source) || ranger.WorkTagIsDisabled(WorkTags.Violent) ||
                !HasPair(ranger, source, RangerCareer.Companion(source)) || !Verb_RangerBreakthrough.Bow(ranger)) return false;
            var stamina = source.GetCompAbilityUserMight()?.Stamina;
            return stamina?.MaxLevel > 0f && stamina.CurLevel >= .60f * stamina.MaxLevel &&
                Find.TickManager.TicksGame >= RangerCareer.Path(source).RangerHuntReadyAt;
        }
        private bool Together() => Source != null && (!Hosted || (Owner != null &&
            SpiritHostedMartial.Find(pawn,Source,RangerHuntAbility.JobName)==Owner)) &&
            HasPair(pawn,Source,job.targetC.Pawn) && RangerAscension.HasHunt(Source) &&
            pawn.equipment?.Primary == job.targetB.Thing && Verb_RangerBreakthrough.Bow(pawn) &&
            pawn.stances?.stunner?.Stunned != true && (!begun || pawn.Map.uniqueID == mapId);
        public override bool TryMakePreToilReservations(bool errorOnFailed) => Ready(pawn,Source) && Together() && Verb?.CanHitTargetFrom(pawn.Position,job.targetA) == true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref begun,"rangerHuntBegun");
            Scribe_Values.Look(ref paid,"rangerHuntPaid");
            Scribe_Values.Look(ref fired,"rangerHuntFired");
            Scribe_Values.Look(ref guards,"rangerHuntGuards");
            Scribe_Values.Look(ref mapId,"rangerHuntMap");
            Scribe_Collections.Look(ref marks,"rangerHuntMarks",LookMode.Reference);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(_ => Interrupt());
            this.FailOn(() => !Together() || (!paid && !Ready(pawn,Source)));
            yield return Toils_General.Do(Begin);
            yield return Toils_General.Wait(Mathf.Max(1,Mathf.RoundToInt(300f * AbilityTiming.WarmupMultiplier)),TargetIndex.A)
                .WithProgressBarToilDelay(TargetIndex.A);
            for (int i=0;i<Arrows;i++)
            {
                if (i != 0) yield return Toils_General.Wait(240);
                int index=i;
                yield return Toils_General.Do(() => Shoot(index));
            }
        }

        private void Begin()
        {
            if (begun) return;
            var first=job.targetA.Pawn;
            if (first == null || Verb?.CanHitTargetFrom(pawn.Position,first) != true)
            { EndJobWith(JobCondition.Incompletable); return; }
            mapId=pawn.Map.uniqueID;
            marks = new List<Pawn> {first};
            marks.AddRange(GenRadial.RadialDistinctThingsAround(first.Position,pawn.Map,8f,true).OfType<Pawn>()
                .Where(p => p != first && Verb.CanHitTargetFrom(pawn.Position,p))
                .OrderBy(p => first.Position.DistanceToSquared(p.Position)).ThenBy(p => p.thingIDNumber).Take(2));
            begun=true;
        }
        private void Shoot(int index)
        {
            if (fired > index) return;
            if (fired != index || !Together() || (!paid && !Ready(pawn,Source)) || marks.NullOrEmpty())
            { EndJobWith(JobCondition.Incompletable); return; }
            for (int i=0;i<marks.Count;i++)
            {
                var target=marks[(index+i)%marks.Count];
                if (target == null || !Verb.Fire(target)) continue;
                fired++;
                if (!paid) Pay(.60f,120000);
                return;
            }
            EndJobWith(JobCondition.Incompletable);
        }

        internal void Guard(ref DamageInfo hit, out bool absorbed)
        {
            absorbed=false;
            if (!Active || guards >= 2 || hit.Amount <= 0f || hit.Def?.isExplosive != false || hit.Weapon?.IsWeapon != true ||
                hit.Instigator?.HostileTo(pawn) != true || !Together()) return;
            guards++;
            hit.SetAmount(Mathf.Max(0f, hit.Amount-25f));
            absorbed=hit.Amount <= 0f;
        }

        private void Interrupt()
        {
            if (begun && !paid) Pay(.06f,6000);
            begun=false;
        }
        private void Pay(float fraction,int ticks)
        {
            var path=Source == null ? null : RangerCareer.Path(Source);
            if (paid || path == null) return;
            paid=true;
            var stamina=Source.GetCompAbilityUserMight()?.Stamina;
            if (stamina != null) stamina.CurLevel=Mathf.Max(0f, stamina.CurLevel-fraction*stamina.MaxLevel);
            int lockout=AbilityTiming.Cooldown(ticks);
            path.RangerHuntReadyAt=Find.TickManager.TicksGame+lockout;
            if (Owner != null) Owner.CooldownTicksLeft=lockout;
        }
    }
}
