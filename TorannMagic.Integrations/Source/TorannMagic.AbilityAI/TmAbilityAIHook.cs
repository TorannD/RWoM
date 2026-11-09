using AbilityAI;
using RimWorld;
using Verse;

namespace TorannMagic.Integrations.AbilityAICompat
{
    // Bridges are vanilla tracker abilities, so Ability AI asks this hook
    // (not RegisterVote) for every ready bridged ability with a rule.
    // Claim keeps NPC casts working even when the player has turned off
    // "Also use abilities no mod claims" — RWoM owns these spells and hands
    // them over wholesale. For the player's own pawns only a Veto would
    // count, and we have none: their autocast choice is theirs.
    internal sealed class TmAbilityAIHook : IAbilityAIHook
    {
        internal static readonly TmAbilityAIHook Instance = new TmAbilityAIHook();

        public AbilityAIVote Vote(Pawn pawn, Ability ability)
        {
            return ability?.def is TMBridgeAbilityDef ? AbilityAIVote.Claim : AbilityAIVote.NoOpinion;
        }

        public void Notify_AICast(Pawn pawn, Ability ability, LocalTargetInfo target, LocalTargetInfo dest)
        {
            // The cast flows through the bridge's own Activate into the
            // JecsLite owner; payment, cooldown and ownership are already
            // owned there. Nothing extra to record.
        }
    }
}
