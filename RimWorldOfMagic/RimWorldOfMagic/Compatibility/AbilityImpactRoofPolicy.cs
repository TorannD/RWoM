#if RW17
using System;
using System.Collections.Generic;
using AbilityUser;
using Verse;

namespace TorannMagic.Compatibility
{
    // Explicitly audited local effect carriers. flyOverhead remains a flight policy;
    // it does not mean that a ritual, cloud or terrain effect originates in the sky.
    // Unlisted projectiles keep the framework's roof interception, including orbital
    // strikes, falling storms, aerial movement and physical grenades/charges.
    internal static class AbilityImpactRoofPolicy
    {
        private static readonly HashSet<Type> IndoorEffects = new HashSet<Type>
        {
            // Local fields, rituals and summons (all spell levels share these classes).
            typeof(Projectile_RaiseUndead), typeof(Projectile_FogOfTorment), typeof(Projectile_HealingCircle),
            typeof(Projectile_Teleport), typeof(Projectile_IgniteBlood),
             typeof(Projectile_AccelerateTime), typeof(Projectile_GraveBlade),
            typeof(Projectile_Attraction), typeof(Projectile_Repulsion), typeof(Projectile_PsychicShock),
            typeof(Projectile_Encase), typeof(Projectile_LightningCloud),
            typeof(Projectile_DistortSpirit), typeof(Projectile_SummonSpiritAnimals), typeof(Projectile_PsionicBlast),
            typeof(Projectile_Explosion), typeof(Projectile_SummonPoppi),
            // Summoned minions spawn locally at the target cell; the mana is already
            // paid at cast time, so a roof interception that destroys the carrier
            // silently ate the payment and produced no minions (player report).
            typeof(Projectile_SummonMinion), typeof(Projectile_SummonExplosive), typeof(Projectile_SummonPylon),
            typeof(Projectile_SummonDemon), typeof(Projectile_SummonElemental), typeof(Projectile_SpiritPossession),
            // Local utility effects and infrastructure magic.
            typeof(Projectile_Sunlight), typeof(Projectile_DryGround), typeof(Projectile_WetGround),
            typeof(Projectile_SmokeCloud), typeof(Projectile_Extinguish), typeof(Projectile_EMP),
            typeof(Projectile_FertileLands), typeof(Projectile_Sabotage)
        };

        // Generic JecsLite classes also carry unrelated abilities, so these entries
        // require both an audited projectile def and a RWoM ability context.
        private static readonly HashSet<string> IndoorGenericDefs = new HashSet<string>(StringComparer.Ordinal)
        {
            "Buff_AMP", "Buff_RayofHope", "Buff_Shadow", "Buff_Soothe",
            "Projectile_ConsumeCorpse", "Projectile_ConsumeCorpse_Mass", "Projectile_CureDisease", "Projectile_Regenerate",
            "Projectile_RegrowLimb", "Projectile_SootheAnimal", "Projectile_DeathMark", "TM_Projectile_Priest", "TM_Paladin_Heal",
            "TM_Paladin_Shield", "TM_Projectile_PsionicBarrier", "Projectile_FoldReality", "Projectile_Blink", "Projectile_BriarPatch",
            "Projectile_DirtDevil", "Projectile_EyeOfTheStorm", "Projectile_SpiritStorm", "Projectile_PsionicStorm", "Projectile_Summon",
            "Projectile_TM_SpawnThing", "Projectile_Warlock"
        };

        internal static bool IsIndoorEffect(Projectile_AbilityBase projectile)
            => IndoorEffects.Contains(projectile.GetType())
                || (projectile.localAbilityDef is TMAbilityDef && projectile.def != null && IndoorGenericDefs.Contains(projectile.def.defName));
    }
}
#endif
