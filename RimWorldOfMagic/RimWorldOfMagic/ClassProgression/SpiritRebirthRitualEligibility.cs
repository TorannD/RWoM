using System;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public enum SpiritRebirthRitualRejection
    {
        None, CreatorUnavailable, CreatorNotAscendedNecromancer, CreatorProfileInvalid,
        CreatorPowerOwnerInvalid, CircleUnavailable, CircleWrongOwner, CircleBusy,
        CorpseUnavailable, CorpseIdentityChanged, CorpseWrongCustody,
        CorpseNotDeadColonist, CorpseUndead, CorpseUnnatural, CorpseAlreadyReborn
    }

    // Structural preflight only. The ordered job must separately prove reachability,
    // reservations and exact payment, then revalidate these same saved identities.
    public static class SpiritRebirthRitualEligibility
    {
        public const int RitualTicks = 10000;
        public const int MagicyteCost = 300;
        public const int SilverCost = 50;

        public static SpiritRebirthRitualRejection CreatorRejection(Pawn creator)
        {
            if (creator == null || creator.Destroyed || creator.Dead || creator.Downed || !creator.Spawned
                || !creator.IsColonistPlayerControlled || ClassIdentity.Component(creator)?.SpiritRebirth != null)
                return SpiritRebirthRitualRejection.CreatorUnavailable;
            if (!ClassIdentity.TryGetState(creator, out var state) || (state.Vocation & Vocation.Mage) == 0
                || ClassInitiation.HasLegacyClass(creator)) return SpiritRebirthRitualRejection.CreatorNotAscendedNecromancer;
            var path = state.Find("rwom.necromancer");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage != InitiationStage.Ascended)
                return SpiritRebirthRitualRejection.CreatorNotAscendedNecromancer;
            var ultimate = TorannMagicDefOf.TM_LichForm;
            var profile = ClassIdentity.Profile("rwom.necromancer");
            if (ultimate == null || profile?.domain != DisciplineDomain.Magic || profile.spells == null
                || profile.spells.Count(s => s?.stage == InitiationStage.Ascended && s.ability == ultimate) != 1)
                return SpiritRebirthRitualRejection.CreatorProfileInvalid;
            var owners = creator.GetCompAbilityUserMagic()?.ExistingMagicData?.magicPowerN;
            if (owners == null || owners.Count(p => p?.TMabilityDefs?.Contains(ultimate) == true) != 1)
                return SpiritRebirthRitualRejection.CreatorPowerOwnerInvalid;
            return SpiritRebirthRitualRejection.None;
        }

        public static SpiritRebirthRitualRejection Rejection(Pawn creator, Thing circle, Corpse corpse, Pawn expectedOriginal)
        {
            var rejection = CreatorRejection(creator);
            if (rejection != SpiritRebirthRitualRejection.None) return rejection;
            if (!(circle is Building_TMMagicCircle largeCircle) || circle.Destroyed || !circle.Spawned
                || circle.def?.thingClass != typeof(Building_TMMagicCircle) || circle.Map != creator.Map)
                return SpiritRebirthRitualRejection.CircleUnavailable;
            if (circle.Faction != creator.Faction || circle.IsForbidden(creator))
                return SpiritRebirthRitualRejection.CircleWrongOwner;
            if (largeCircle.IsActive || largeCircle.IsPending || largeCircle.MageList.Count != 0)
                return SpiritRebirthRitualRejection.CircleBusy;
            if (corpse == null || corpse.Destroyed || corpse.IsForbidden(creator))
                return SpiritRebirthRitualRejection.CorpseUnavailable;
            if (corpse is UnnaturalCorpse) return SpiritRebirthRitualRejection.CorpseUnnatural;
            var original = corpse.InnerPawn;
            if (original == null || expectedOriginal == null || !ReferenceEquals(original, expectedOriginal))
                return SpiritRebirthRitualRejection.CorpseIdentityChanged;
            var holder = corpse.GetDirectlyHeldThings();
            if (holder?.Contains(original) != true || !ReferenceEquals(holder.Owner, corpse)
                || !ReferenceEquals(original.holdingOwner, holder) || original.Spawned
                || (corpse.Spawned ? corpse.Map != creator.Map
                    : creator.carryTracker == null || !ReferenceEquals(corpse.ParentHolder, creator.carryTracker)
                        || !ReferenceEquals(creator.carryTracker.CarriedThing, corpse)))
                return SpiritRebirthRitualRejection.CorpseWrongCustody;
            // Pawn.Kill finalizes the held pawn as Destroyed. Discarded specifically
            // means the identity can no longer be recovered from this exact corpse.
            if (original.Discarded || !original.Dead
                || original.RaceProps?.Humanlike != true || !original.RaceProps.IsFlesh
                || original.Faction != creator.Faction || !original.IsColonist)
                return SpiritRebirthRitualRejection.CorpseNotDeadColonist;
            if (TM_Calc.IsUndead(original, SpiritNecromancyRenunciation.Required(original))) return SpiritRebirthRitualRejection.CorpseUndead;
            // Any existing marker, including unsupported versions, blocks repeat conversion.
            if (ClassIdentity.Component(original)?.SpiritRebirth != null)
                return SpiritRebirthRitualRejection.CorpseAlreadyReborn;
            return SpiritRebirthRitualRejection.None;
        }
    }
}
