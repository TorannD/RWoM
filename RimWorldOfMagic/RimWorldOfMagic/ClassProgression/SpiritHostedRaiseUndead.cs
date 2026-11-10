using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TorannMagic
{
    partial class Projectile_RaiseUndead
    {
        private bool projectedLich;
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_RaiseUndead;
        protected override string SkillStem => "TM_RaiseUndead";

        private void ReadRaiseRanks(MagicData data)
        {
            var skills = Skills(data);
            pwrVal = skills.FirstOrDefault(s => s.label == "TM_RaiseUndead_pwr")?.level ?? 0;
            verVal = skills.FirstOrDefault(s => s.label == "TM_RaiseUndead_ver")?.level ?? 0;
        }

        internal override void PrepareProjection(MagicAbility owner)
        {
            base.PrepareProjection(owner);
            // This spell's native calculation also uses Raise Undead ranks for Faceless.
            ReadRaiseRanks(owner.MagicUser.MagicData);
            projectedLich = owner.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_LichHD);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (projected) Scribe_Values.Look(ref projectedLich, "TM_raiseLich");
        }

        private static bool CanRaiseCorpse(Corpse corpse)
        {
            var pawn = corpse.InnerPawn;
            if (corpse is UnnaturalCorpse || pawn == null || !pawn.Dead || pawn.Discarded || pawn is TMPawnSummoned || pawn is Golems.TMPawnGolem)
                return false;
            string name = pawn.def.defName;
            if (name is "SL_Runner" or "SL_Peon" or "SL_Archer" or "SL_Hero") return true;
            return !name.Contains("ROM_") && !pawn.IsEntity && pawn.RaceProps.IsFlesh &&
                !pawn.health.hediffSet.hediffs.Any(h => h.def.defName is "ROM_Vampirism" or "ROM_GhoulHediff");
        }

        private static void BindServant(CompAbilityUserMagic owner, Pawn previousOwner, Pawn servant, HediffDef mark)
        {
            var link = servant.health.hediffSet.GetFirstHediffOfDef(mark).TryGetComp<HediffComp_Undead>();
            link.linkedPawn = owner.Pawn;
            if (previousOwner != null && previousOwner != owner.Pawn)
                previousOwner.GetCompAbilityUserMagic()?.supportedUndead?.RemoveAll(t => t == servant);
            owner.supportedUndead.RemoveAll(t => t == servant);
            owner.supportedUndead.Add(servant);
        }

        internal static bool HasHostedTarget(Map map, IntVec3 center, ThingDef projectile)
        {
            if (map == null || projectile?.projectile == null || !center.InBounds(map)) return false;
            foreach (var cell in GenRadial.RadialCellsAround(center, projectile.projectile.explosionRadius, true))
            {
                if (!cell.InBounds(map)) continue;
                foreach (var thing in cell.GetThingList(map))
                    if ((thing is Corpse corpse && CanRaiseCorpse(corpse)) ||
                        (thing is Pawn pawn && !pawn.Dead && TM_Calc.IsUndead(pawn))) return true;
            }
            return false;
        }
    }
}
