using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;

namespace TorannMagic
{
    public class Verb_Hex : ClassProgression.Verb_SpiritTargetable
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            var map = CasterPawn?.Map;
            return map != null && target.IsValid && target.Cell.InBounds(map) && !target.Cell.Fogged(map) &&
                (root-target.Cell).LengthHorizontal < verbProps.range;
        }

        private IEnumerable<Pawn> Candidates(Pawn body)
        {
            if (UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE)
            {
                if (currentTarget.Pawn != null) yield return currentTarget.Pawn;
                yield break;
            }
            var area = UseAbilityProps.TargetAoEProperties;
            if (area == null) yield break;
            var center = area.startsFromCaster ? body.Position : currentTarget.Cell;
            var eligible = GenRadial.RadialDistinctThingsAround(center,body.Map,area.range,true).OfType<Pawn>()
                .Where(p => !p.Dead && p.RaceProps.IsFlesh && !TM_Calc.IsUndead(p) &&
                    (area.friendlyFire || p.HostileTo(body)) && UseAbilityProps.targetParams.CanTarget(new TargetInfo(p)));
            foreach (var pawn in eligible.InRandomOrder().Take(area.maxTargets)) yield return pawn;
        }

        protected override bool TryCastShot()
        {
            burstShotsLeft = 0;
            var body = CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body,Ability);
            var magic = owner?.GetCompAbilityUserMagic();
            if (body?.Spawned != true || magic == null) return false;
            int strength = TM_Calc.GetSkillPowerLevel(owner,TorannMagicDefOf.TM_Hex);
            bool attempted = false;
            foreach (var target in Candidates(body).ToArray())
            {
                if (!target.RaceProps.IsFlesh || TM_Calc.IsUndead(target)) continue;
                attempted = true;
                if (!Rand.Chance(.4f + .1f * strength * TM_Calc.GetSpellSuccessChance(owner,target,true)))
                {
                    MoteMaker.ThrowText(target.DrawPos,target.Map,"TM_ResistedSpell".Translate(),-1);
                    continue;
                }
                var trial = ClassProgression.ShamanTrials.CaptureHex(Ability,target);
                HealthUtility.AdjustSeverity(target,TorannMagicDefOf.TM_HexHD,1f);
                var targets = magic.HexedPawns;
                if (!targets.Contains(target))
                {
                    magic.RegisterHexTarget(target);
                    if (targets.Count == 1) ClassProgression.ShamanLifecycle.GrantControls(magic);
                }
                ClassProgression.ShamanTrials.CompleteHex(trial);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Hex,target.DrawPos,target.Map,.6f,.1f,.2f,.2f,0,0,0,0);
            }
            return attempted;
        }
    }
}
