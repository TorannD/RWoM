using AbilityUser;
using RimWorld;
using TorannMagic.ClassProgression;
using TorannMagic.Odyssey;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    public class Verb_SummonDemon : Verb_SpiritTargetable
    {
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            // The native stance keeps its remaining ticks, but this completed ability
            // verb is rebuilt on load and has no persistent verb ID.
            if (CasterPawn?.stances?.curStance is Stance_Cooldown stance && ReferenceEquals(stance.verb,this))
                stance.verb = null;
        }

        protected override bool TryCastShot()
        {
            burstShotsLeft = 0;
            if (!(Ability is MagicAbility power)) return false;
            var body = CasterPawn;
            var owner = SpiritRebirthCasting.SkillOwner(body,power);
            if (!DemonRitual.CanStart(body,owner,out var offering,out var succubus,out var warlock)) return false;
            bool accepted;
            if (owner == succubus)
            {
                var job = JobMaker.MakeJob(TorannMagicDefOf.JobDriver_SummonDemon,offering,succubus,warlock);
                accepted = body.jobs.TryTakeOrderedJob(job,JobTag.Misc);
            }
            else accepted = Projectile_SummonDemon.Begin(body,warlock,succubus,offering,power);
            if (!accepted) return false;
            if (owner == body) ManualTravelSafety.Settle(power);
            else ManualTravelSafety.SettleHosted(power,body);
            return true;
        }
    }
}
