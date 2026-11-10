using System.Linq;
using Verse;
using TorannMagic.ClassProgression;

namespace TorannMagic
{
    public class Verb_DispelEnchantWeapon : Verb_SpiritTargetable
    {
        protected override bool TryCastShot()
        {
            burstShotsLeft = 0;
            var owner = SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
            return owner != null && currentTarget.Pawn == CasterPawn && DispelOwned(owner.GetCompAbilityUserMagic());
        }

        internal static bool DispelOwned(CompAbilityUserMagic magic)
        {
            if (magic?.weaponEnchants == null || magic.weaponEnchants.Count == 0) return false;
            // Removing an effect unregisters its recipient. Snapshot once so stale entries cannot stall the cast.
            foreach (var target in magic.weaponEnchants.ToArray()) RemoveEffects(target, magic.Pawn);
            magic.weaponEnchants.Clear();
            magic.SyncEnchantWeaponControl();
            return true;
        }

        public static void RemoveExistingEnchantment(Pawn pawn) => RemoveEffects(pawn, null);

        private static void RemoveEffects(Pawn target, Pawn owner)
        {
            if (target?.health?.hediffSet == null) return;
            foreach (var effect in target.health.hediffSet.hediffs.ToArray())
            {
                var enchantment = effect.TryGetComp<HediffComp_EnchantedWeapon>();
                if (enchantment != null && (owner == null || enchantment.enchanterPawn == owner))
                    target.health.RemoveHediff(effect);
            }
        }
    }
}
