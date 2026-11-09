using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SpellMastery
    {
        // The job choice numbers are serialized. Append new choices without renumbering.
        internal sealed class Choice
        {
            internal readonly int Number;
            internal readonly string Id, Spell, Scroll;
            internal int Magicyte => Scroll == null ? 200 : 100;
            internal Choice(int number, string id, string spell, string scroll)
            { Number = number; Id = id; Spell = spell; Scroll = scroll; }
            internal TMAbilityDef Ability => Spell == "rwom.tm_fireball" ? TorannMagicDefOf.TM_Fireball : TorannMagicDefOf.TM_Heal;
        }

        internal static readonly Choice[] Choices =
        {
            new(1, "focus", "rwom.tm_fireball", null), new(2, "reach", "rwom.tm_fireball", null),
            new(3, "depth", "rwom.tm_heal", "SpellOf_Heal"), new(4, "breadth", "rwom.tm_heal", "SpellOf_Heal")
        };

        internal static bool Eligible(Pawn pawn, Choice choice, out ClassProgressionState state)
        {
            state = null;
            if (choice?.Ability == null || !ClassIdentity.TryGetState(pawn, out state)
                || !state.Disciplines.Any(d => d?.Domain == DisciplineDomain.Magic
                    && (d.Stage == InitiationStage.Master || d.Stage == InitiationStage.Ascended))
                || ClassInitiation.HasLegacyClass(pawn)) return false;
            var comp = pawn.GetCompAbilityUserMagic();
            var data = comp?.ExistingMagicData;
            if (data == null || state.Find("rwom.chaos")?.Chaos?.Contains(choice.Spell) == true) return false;
            if (choice.Ability == TorannMagicDefOf.TM_Heal)
                return comp.spell_Heal || (ClassIdentity.IsPaladin(pawn) && data.MagicPowersP.Any(p => p.learned && p.TMabilityDefs.Contains(choice.Ability)));
            return ClassIdentity.IsFireMage(pawn) && data.MagicPowersIF.Any(p => p.learned && p.TMabilityDefs.Contains(choice.Ability));
        }

        internal static string Active(Pawn pawn, PawnAbility owner)
        {
            if (pawn == null || owner?.Pawn != pawn || !(owner.AbilityUser is CompAbilityUserMagic magic)
                || !ClassIdentity.TryGetState(pawn, out var state)) return null;
            var choice = Choices.FirstOrDefault(c => c.Ability == owner.Def);
            var selected = state.Mastery(choice?.Spell);
            return selected != null && magic.AbilityData.Powers.Any(a => ReferenceEquals(a, owner))
                && Eligible(pawn, choice, out _) ? selected : null;
        }
    }
}
