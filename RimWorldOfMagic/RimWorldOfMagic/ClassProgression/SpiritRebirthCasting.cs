using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class SpiritRebirthCasting
    {
        // The physical verb caster can differ from its ability owner only for
        // the exact original still held by this host. Never redirect the host's own spell.
        internal static Pawn SkillOwner(Pawn body, PawnAbility ability)
        {
            var owner = ability?.Pawn;
            if (owner == null || ReferenceEquals(owner, body)) return body;
            return SpiritRebirthState.TryGetPossessingSpirit(body, out var soul) && ReferenceEquals(soul, owner)
                ? owner : null;
        }

        public static bool Allows(Pawn pawn, AbilityDef ability)
        {
            // A trait, race, or native possession alone never opts a legacy pawn in.
            if (ClassIdentity.Component(pawn)?.SpiritRebirth == null) return true;
            if (!SpiritRebirthEnergy.CanCast(pawn)) return false;
            // The reborn spirit keeps its former class: abilities it had
            // already learned stay castable alongside the spirit family
            // (classed-spirit report). Unlearned powers still need the
            // grimoire, and possession hosts carry no marker, so their own
            // abilities were never gated here.
            return IsSpiritAbility(ability) || OwnsLearnedAbility(pawn, ability);
        }

        // Read-only over the retained MagicData. The aggregate power getters
        // materialize owners and must never run inside this admission gate.
        private static bool OwnsLearnedAbility(Pawn pawn, AbilityDef ability)
        {
            var data = pawn.GetCompAbilityUserMagic()?.ExistingMagicData;
            if (data == null) return false;
            return KnowsLearned(data.magicPowerCustom, ability)
                || KnowsLearned(data.magicPowerCustomStandalone, ability)
                || KnowsLearned(data.MagicPowers, ability)
                || KnowsLearned(data.MagicPowersStandalone, ability)
                || KnowsLearned(data.MagicPowersW, ability)
                || KnowsLearned(data.MagicPowersIF, ability)
                || KnowsLearned(data.MagicPowersHoF, ability)
                || KnowsLearned(data.MagicPowersSB, ability);
        }

        private static bool KnowsLearned(List<MagicPower> powers, AbilityDef ability)
        {
            if (powers == null) return false;
            for (int i = 0; i < powers.Count; i++)
            {
                var power = powers[i];
                if (power?.learned != true || power.TMabilityDefs == null) continue;
                for (int j = 0; j < power.TMabilityDefs.Count; j++)
                    if (ReferenceEquals(power.TMabilityDefs[j], ability)) return true;
            }
            return false;
        }

        public static bool IsSpiritAbility(AbilityDef ability)
        {
            return ability != null && NativeFamily().Contains(ability);
        }

        internal static HashSet<AbilityDef> NativeFamily()
        {
            var result = new HashSet<AbilityDef>();
            if (TorannMagicDefOf.TM_Possessor == null) return result;
            var classes = DefDatabase<TM_CustomClassDef>.GetNamedSilentFail("TM_CustomClasses")?.customClasses;
            var matches = classes?.Where(c => c != null && c.classTrait == TorannMagicDefOf.TM_Possessor).ToList();
            if (matches?.Count != 1 || !matches[0].isMage || matches[0].classMageAbilities == null) return result;
            // Native rank variants are explicit class entries. Follow only their
            // actual child definitions, with cycle protection; names grant nothing.
            var pending = new Stack<TMAbilityDef>(matches[0].classMageAbilities.Where(a => a != null));
            var seen = new HashSet<TMAbilityDef>();
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (!seen.Add(current)) continue;
                result.Add(current);
                foreach (var child in current.childAbilities ?? new List<TMAbilityDef>())
                    if (child != null) pending.Push(child);
            }
            return result;
        }
    }
}
