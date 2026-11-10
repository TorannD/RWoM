using AbilityUser;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    public class Verb_Lullaby : Verb_SpiritTargetable
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            var map = CasterPawn?.Map;
            return map != null && target.IsValid && (!target.HasThing || target.Thing.Map == map)
                && target.Cell.InBounds(map) && !target.Cell.Fogged(map)
                && (root - target.Cell).LengthHorizontal < verbProps.range;
        }

        protected override bool ApplyHostedEffect()
        {
            var owner = Ability?.Pawn ?? CasterPawn;
            var def = Ability.Def as TMAbilityDef;
            int versatility = TM_Calc.GetSkillVersatilityLevel(owner, def);
            int power = TM_Calc.GetSkillPowerLevel(owner, def);
            TargetsAoE.Clear();
            UpdateTargets();
            if (UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && TargetsAoE.Count > 1)
                TargetsAoE.RemoveRange(0, TargetsAoE.Count - 1);
            bool applied = false;
            foreach (var target in TargetsAoE)
            {
                if (!(target.Thing is Pawn victim) || victim.Dead || !victim.Spawned ||
                    victim.Map != CasterPawn.Map || !victim.RaceProps.IsFlesh) continue;
                if (Rand.Chance(.4f + .1f * power * TM_Calc.GetSpellSuccessChance(owner, victim, true)))
                {
                    if (victim.InMentalState) victim.mindState.mentalStateHandler.Reset();
                    if (victim.needs?.rest != null && victim.jobs != null)
                    {
                        victim.needs.rest.CurLevel = 0f;
                        var sleep = JobMaker.MakeJob(JobDefOf.LayDown, victim.Position);
                        sleep.forceSleep = true;
                        victim.jobs.StartJob(sleep, JobCondition.InterruptForced);
                    }
                    for (int i = 0; i < 3; i++)
                        TM_MoteMaker.ThrowNoteMote(victim.DrawPos, victim.Map, Rand.Range(.3f, .8f));
                }
                else MoteMaker.ThrowText(victim.DrawPos, victim.Map, "TM_ResistedSpell".Translate(), -1);
                HealthUtility.AdjustSeverity(victim, HediffDef.Named("TM_LullabyHD"), .95f + versatility);
                applied = true;
            }
            return applied;
        }

        protected override bool TryCastShot()
        {
            ApplyHostedEffect();
            PostCastShot(false, out bool fired);
            return fired;
        }
    }
}
