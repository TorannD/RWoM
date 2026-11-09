using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    // Burst orders use a separate persistent driver; never execute them as instant effects.
    internal static class SuperSoldierCast
    {
        internal const string Whip = "TM_SpiritCastPistolWhip", Grenade = "TM_SpiritCastMk203GL",
            Charge = "TM_SpiritCastBreachingCharge", Dressing = "TM_SpiritCastSuperSoldierFieldDressing";
        internal const string Buckshot = "TM_SpiritCastBuckshot", Suppressing = "TM_SpiritCastSuppressingFire";
        internal const string Mortar = "TM_SpiritCast60mmMortar";
        internal static bool IsBurst(string job) => job == Buckshot || job == Suppressing;
        internal static bool Handles(string job) => job == Whip || job == Grenade || job == Charge || job == Dressing || job == Mortar || IsBurst(job);
        internal static TMAbilityDef Definition(string job) => job == Whip ? TorannMagicDefOf.TM_PistolWhip :
            job == Grenade ? TorannMagicDefOf.TM_Mk203GL : job == Charge ? TorannMagicDefOf.TM_BreachingCharge :
            job == Dressing ? DefDatabase<TMAbilityDef>.GetNamedSilentFail(SuperSoldierMartialPath.Dressing) :
            job == Buckshot ? TorannMagicDefOf.TM_Buckshot : job == Suppressing ? TorannMagicDefOf.TM_SuppressingFire :
            job == Mortar ? TorannMagicDefOf.TM_60mmMortar : null;

        private sealed class ProjectileVerb : Verb_SpiritTargetable
        {
            // This job pays after successful launch. The native failure/refund path assumes prepayment.
            protected override bool TryCastShot() => TryLaunchProjectileCheck(verbProps.defaultProjectile, currentTarget);
        }

        private static Verb_SpiritTargetable For(Pawn body, MightAbility power, string job)
        {
            Verb_SpiritTargetable verb = job == Whip ? new Verb_PistolWhip() :
                job == Dressing ? new Verb_SuperSoldierFieldDressing() : job == Buckshot ? new Verb_Buckshot() :
                job == Suppressing ? new Verb_SuppressingFire() : new ProjectileVerb();
            verb.caster = body; verb.Ability = power; verb.verbProps = power.mightDef.MainVerb;
            return verb;
        }

        internal static bool Funded(Pawn body, MightAbility power, string job)
        {
            if (SuperSoldierMartialPath.Owner(body) != power.Pawn) return false;
            if (job == Dressing) return Verb_SuperSoldierFieldDressing.CanDress(body);
            if (job == Mortar) return !body.WorkTagIsDisabled(WorkTags.Violent);
            if (body.WorkTagIsDisabled(WorkTags.Violent) || body.equipment?.Primary == null ||
                !SuperSoldierWeaponProfile.ReadyWeapon(body)) return false;
            if ((job == Grenade || IsBurst(job)) && body.apparel != null)
                foreach (var apparel in body.apparel.WornApparel)
                    if (!apparel.AllowVerbCast(For(body, power, job))) return false;
            return true;
        }

        internal static bool Valid(Pawn body, MightAbility power, string job, LocalTargetInfo target)
        {
            if (power == null || body?.Spawned != true || !target.IsValid ||
                target.HasThing && (!target.Thing.Spawned || target.Thing.Map != body.Map)) return false;
            if (job == Dressing) return target.Thing == body && Verb_SuperSoldierFieldDressing.CanDress(body);
            if (job == Mortar) return target.Thing == body;
            if (job == Whip && target.Pawn == null) return false;
            return For(body, power, job).CanHitTargetFrom(body.Position, target);
        }

        internal static bool Execute(Pawn body, Pawn soul, string job, LocalTargetInfo target)
        {
            if (IsBurst(job)) return false;
            var power = SpiritHostedMartial.Find(body, soul, job);
            if (!SpiritHostedMartial.Ready(body, power, job) || !Valid(body, power, job, target)) return false;
            if (job == Mortar) return Verb_60mmMortar.DeployFromHost(body, power);
            if (!For(body, power, job).CastFromHost(target)) return false;
            power.PostAbilityAttempt();
            return true;
        }

        internal static void Choose(Pawn body, Pawn soul, MightAbility power, string name)
        {
            var job = DefDatabase<JobDef>.GetNamed(name);
            bool Allowed(LocalTargetInfo target) => SpiritHostedMartial.Ready(body, SpiritHostedMartial.Find(body, soul, name), name) && Valid(body, power, name, target);
            void Order(LocalTargetInfo target)
            {
                if (body.CurJobDef != job && Allowed(target)) body.jobs.TryTakeOrderedJob(
                    JobMaker.MakeJob(job, soul, target, IsBurst(name) ? body.equipment.Primary : LocalTargetInfo.Invalid), JobTag.Misc);
            }
            if (name == Dressing || name == Mortar) { Order(body); return; }
            Find.Targeter.BeginTargeting(power.mightDef.MainVerb.targetParams, Order,
                highlightAction: target => GenDraw.DrawRadiusRing(body.Position, power.mightDef.MainVerb.range),
                targetValidator: Allowed, caster: body);
        }
    }
}
