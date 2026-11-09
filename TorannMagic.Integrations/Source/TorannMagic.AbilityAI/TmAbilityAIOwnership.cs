using AbilityAI;
using RimWorld;
using Verse;

namespace TorannMagic.Integrations.AbilityAICompat
{
    // Reported through Core CastOwnership so every RWoM automatic casting path
    // stands down for a pawn whose bridged spells Ability AI would handle:
    // colonist ResolveAutoCast, NPC ResolveAIAutoCast, the aggressive decision
    // tree (at its DoJob commit) and the classic dispatch evaluators. Manual
    // player casts never consult this probe, so ordered casts stay untouched.
    //
    // Without this mod the assembly does not load and the probe is absent:
    // classic RWoM autocast resumes untouched. With Ability AI's master
    // toggle off, OthersStandDown is false everywhere and the handover
    // releases automatically.
    //
    // AbilityAIDriver.OthersStandDown is the driver's own cheap entry point
    // (rule lookup and settings, asked about twice a second per pawn by
    // Ability AI itself). CastOwnership's contract keeps the probe read-only;
    // its failure policy (throw = busy) fails safe here.
    public static class TmAbilityAIOwnership
    {
        public const string ProviderId = "rwom.integrations.abilityai";

        public static bool IsOwned(Pawn pawn)
        {
            var abilities = pawn?.abilities?.AllAbilitiesForReading;
            if (abilities == null) return false;
            for (int i = 0; i < abilities.Count; i++)
            {
                Ability ability = abilities[i];
                if (ability?.def is TMBridgeAbilityDef && AbilityAIDriver.OthersStandDown(pawn, ability))
                    return true;
            }
            return false;
        }
    }
}
