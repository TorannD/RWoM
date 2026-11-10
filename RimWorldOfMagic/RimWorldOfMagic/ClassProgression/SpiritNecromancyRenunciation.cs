using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritNecromancyRenunciation
    {
        internal static bool Required(Pawn pawn) => pawn != null &&
            (ClassIdentity.Component(pawn)?.State?.Find("rwom.necromancer") != null ||
             pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.Necromancer || t.def == TorannMagicDefOf.Lich) == true);

        // Job IDs survive save/load even though the inner pawn of a corpse is marked destroyed.
        internal static void Confirm(Job job, Pawn pawn)
        {
            if (job?.loadID >= 0 && job.targetB.Thing is Corpse corpse && corpse.InnerPawn == pawn)
                ClassIdentity.Component(pawn).NecromancyConsentJob = job.loadID;
        }
        internal static bool Allowed(Job job, Pawn pawn) => !Required(pawn) ||
            (job?.loadID >= 0 && ClassIdentity.Component(pawn)?.NecromancyConsentJob == job.loadID);

        internal static void Apply(Pawn pawn)
        {
            var comp = ClassIdentity.Component(pawn);
            var magic = pawn.GetCompAbilityUserMagic();
            var data = magic?.ExistingMagicData;
            comp.State?.RenounceNecromancy();
            var traits = pawn.story?.traits;
            if (traits != null) foreach (var trait in traits.allTraits.Where(t =>
                t.def == TorannMagicDefOf.Necromancer || t.def == TorannMagicDefOf.Lich).ToArray())
                traits.RemoveTrait(trait);
            if (data != null)
            {
                magic.spell_LichForm = false;
                if (magic.PowerModifier != 0) magic.PowerModifier = 0;
                var native = new HashSet<AbilityUser.AbilityDef> { TorannMagicDefOf.TM_RaiseUndead, TorannMagicDefOf.TM_DeathMark,
                    TorannMagicDefOf.TM_FogOfTorment, TorannMagicDefOf.TM_ConsumeCorpse, TorannMagicDefOf.TM_CorpseExplosion,
                    TorannMagicDefOf.TM_LichForm, TorannMagicDefOf.TM_DeathBolt };
                native.Remove(null);
                var families = data.MagicPowersN.Where(p => p?.TMabilityDefs?.Any(native.Contains) == true).ToList();
                foreach (var power in families)
                {
                    power.learned = false;
                    power.autocast = false;
                    power.level = 0;
                }
                foreach (var ability in magic.AbilityData.Powers.ToArray())
                    if (ability?.Def != null && (families.Any(p => p.TMabilityDefs.Contains(ability.Def)) || ability.Def == TorannMagicDefOf.TM_ConsumeCorpse_Mass))
                        magic.RemovePawnAbility(ability.Def);
                foreach (var skill in data.MagicPowerSkill_RaiseUndead.Concat(data.MagicPowerSkill_DeathMark)
                    .Concat(data.MagicPowerSkill_ConsumeCorpse).Concat(data.MagicPowerSkill_FogOfTorment)
                    .Concat(data.MagicPowerSkill_CorpseExplosion).Concat(data.MagicPowerSkill_DeathBolt).Concat(data.MagicPowerSkill_LichForm))
                    skill.level = 0;
            }
            comp.SynchronizeHealthDisplay();
            comp.NecromancyConsentJob = -1;
        }
    }
}
