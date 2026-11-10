using AbilityUser;
using RimWorld;
using TorannMagic.ClassProgression;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    public class Verb_RangerShot : Verb_UseAbility
    {
        internal const string Order="TM_SpiritArrowStorm";
        internal static bool Family(TMAbilityDef def) => def!=null &&
            (def==TorannMagicDefOf.TM_ArrowStorm || def==TorannMagicDefOf.TM_ArrowStorm_I ||
            def==TorannMagicDefOf.TM_ArrowStorm_II || def==TorannMagicDefOf.TM_ArrowStorm_III);
        internal static Verb_RangerShot For(Pawn body,MightAbility power) =>
            new Verb_RangerShot {caster=body,Ability=power,verbProps=power.mightDef.MainVerb};

        internal static bool HasBow(Pawn body)
        {
            var weapon=body?.equipment?.Primary;
            var projectile=weapon?.GetComp<CompEquippable>()?.PrimaryVerb?.GetProjectile()?.projectile;
            if (weapon?.def.IsRangedWeapon != true || projectile == null) return false;
            return projectile.damageDef?.defName?.IndexOf("arrow",System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                weapon.def.defName.IndexOf("bow",System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                DefDatabase<WeaponCategoryList>.GetNamedSilentFail("TM_Category_Bows")?.weaponDefNames?.Contains(weapon.def.defName)==true;
        }

        internal static Job MakeOrder(Pawn body,Pawn soul,LocalTargetInfo target) =>
            JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(Order),soul,target,body.equipment?.Primary);

        internal bool CanVolley(Pawn target) => HasBow(CasterPawn) && target?.Spawned==true && !target.Dead &&
            target!=CasterPawn && target.Map==CasterPawn.Map &&
            TM_Calc.HasLoSFromTo(CasterPawn.Position,target.Position,CasterPawn,0,verbProps.range) &&
            TryFindShootLineFromTo(CasterPawn.Position,target,out _);

        internal bool FireArrow(Pawn target)
        {
            if (!CanVolley(target)) return false;
            currentTarget=target;
            return TryLaunchProjectileCheck(UseAbilityProps.defaultProjectile,target);
        }

        protected override bool TryCastShot()
        {
            if (!HasBow(CasterPawn))
            {
                if (CasterPawn?.IsColonist==true)
                    Messages.Message(CasterPawn.equipment?.Primary is Thing weapon ?
                        "MustHaveBow".Translate(CasterPawn.LabelShort,weapon.LabelShort) : "MustHaveRangedWeapon".Translate(CasterPawn.LabelShort),
                        MessageTypeDefOf.RejectInput);
                return false;
            }
            return CanVolley(currentTarget.Pawn) && base.TryCastShot();
        }
    }

    public sealed class JobDriver_RangerVolley : JobDriver
    {
        private TMAbilityDef rank;
        private IntVec3 origin;
        private int mapId, fired, nextShotAt;
        private Pawn Source => job.targetA.Pawn;
        private MightAbility Power => SpiritHostedMartial.Find(pawn,Source,Verb_RangerShot.Order);

        private bool CanFire(MightAbility power) => power!=null && (rank==null || power.Def==rank) &&
            pawn.Spawned && !pawn.Dead && !pawn.Downed && !pawn.InMentalState && !pawn.WorkTagIsDisabled(WorkTags.Violent) &&
            pawn.stances?.stunner?.Stunned!=true && pawn.equipment?.Primary!=null && pawn.equipment.Primary==job.targetC.Thing &&
            (rank==null || pawn.Position==origin && pawn.Map.uniqueID==mapId) &&
            Verb_RangerShot.For(pawn,power).CanVolley(job.targetB.Pawn);

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var power=Power;
            return CanFire(power) && SpiritHostedMartial.Ready(pawn,power,Verb_RangerShot.Order);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref rank,"rangerVolleyRank");
            Scribe_Values.Look(ref origin,"rangerVolleyOrigin",IntVec3.Invalid);
            Scribe_Values.Look(ref mapId,"rangerVolleyMap");
            Scribe_Values.Look(ref fired,"rangerVolleyFired");
            Scribe_Values.Look(ref nextShotAt,"rangerVolleyNext");
        }

        protected override System.Collections.Generic.IEnumerable<Toil> MakeNewToils()
        {
            var aim=Toils_General.Wait(1,TargetIndex.B);
            aim.initAction=()=>{
                var power=Power;
                if (!CanFire(power) || !SpiritHostedMartial.Ready(pawn,power,Verb_RangerShot.Order)) { EndJobWith(JobCondition.Incompletable); return; }
                rank=power.mightDef; origin=pawn.Position; mapId=pawn.Map.uniqueID;
                ticksLeftThisToil=Mathf.Max(1,GenTicks.SecondsToTicks(rank.MainVerb.warmupTime*Compatibility.AbilityTiming.WarmupMultiplier));
            };
            yield return aim.WithProgressBarToilDelay(TargetIndex.B);
            var volley=new Toil { defaultCompleteMode=ToilCompleteMode.Never, initAction=ShootWhenDue, tickAction=ShootWhenDue };
            yield return volley;
        }

        private void ShootWhenDue()
        {
            if (rank==null || fired>=rank.MainVerb.burstShotCount) { EndJobWith(JobCondition.Succeeded); return; }
            if (Find.TickManager.TicksGame<nextShotAt) return;
            var power=Power;
            if (!CanFire(power) || (fired==0 && !SpiritHostedMartial.Ready(pawn,power,Verb_RangerShot.Order)) ||
                !Verb_RangerShot.For(pawn,power).FireArrow(job.targetB.Pawn))
            { EndJobWith(JobCondition.Incompletable); return; }
            fired++;
            if (fired==1) power.PostAbilityAttempt();
            nextShotAt=Find.TickManager.TicksGame+Mathf.Max(1,rank.MainVerb.ticksBetweenBurstShots);
            if (fired==rank.MainVerb.burstShotCount) EndJobWith(JobCondition.Succeeded);
        }
    }
}
