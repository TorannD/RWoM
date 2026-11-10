using System;
using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class EnchanterTrials
    {
        public const string FoundationTrialId = "rwom.enchanter.foundation", MasteryTrialId = "rwom.enchanter.mastery";
        static CompAbilityUserMagic Magic(Pawn pawn) => pawn?.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
        static MagicPower Owner(Pawn pawn, bool enchant) => Magic(pawn)?.MagicData.MagicPowersE.FirstOrDefault(p => p.learned && p.TMabilityDefs.Contains(enchant ? TorannMagicDefOf.TM_EnchantWeapon : TorannMagicDefOf.TM_Transmutate));
        static Pawn CastingBody(Pawn owner)
        {
            if (!SpiritRebirthState.TryGet(owner, out var state)) return owner?.Spawned == true ? owner : null;
            return !state.DepletionPending && state.HasValidHost(owner) && state.Host.Spawned ? state.Host : null;
        }

        static DisciplineProgress Owned(Pawn pawn, bool enchant)
        {
            if (pawn == null || pawn.Dead || CastingBody(pawn) == null || pawn.Faction == null
                || pawn.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true
                || !ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.enchanter");
            return path?.Domain == DisciplineDomain.Magic && path.Stage == (enchant ? InitiationStage.Adept : InitiationStage.Foundation)
                && !path.CompletedTrials.Contains(enchant ? MasteryTrialId : FoundationTrialId) && Owner(pawn, enchant) != null ? path : null;
        }
        static bool RuntimeOwned(PawnAbility ability, Pawn caster, bool enchant) => ability != null && ability.Pawn == caster
            && ability.Def == (enchant ? TorannMagicDefOf.TM_EnchantWeapon : TorannMagicDefOf.TM_Transmutate)
            && ability.AbilityUser == Magic(caster) && Magic(caster)?.AbilityData.Powers.Any(p => ReferenceEquals(p, ability)) == true;
        public sealed class TransmutationScope
        {
            internal Pawn caster;
            internal PawnAbility ability;
            internal Thing source;
            internal MagicPower owner;
            internal DisciplineProgress path;
        }
        [ThreadStatic] static TransmutationScope transmutating;
        public static TransmutationScope BeginTransmutation(PawnAbility ability, Thing source)
        {
            var previous = transmutating;
            transmutating = null;
            var caster = ability?.Pawn;
            var path = Owned(caster, false);
            if (path != null && RuntimeOwned(ability, caster, false) && source != null && source.Spawned && source.Map == CastingBody(caster)?.Map && source.stackCount > 0)
                transmutating = new TransmutationScope { caster = caster, ability = ability, source = source, path = path, owner = Owner(caster, false) };
            return previous;
        }
        public static void EndTransmutation(TransmutationScope previous) => transmutating = previous;
        public sealed class RawConversion
        {
            internal TransmutationScope scope;
            internal ThingDef sourceDef;
            internal int sourceCount;
            internal bool paid, used;
        }
        // Only the native raw-resource branch may claim the current cast; nested work cannot reuse it.
        public static RawConversion CaptureRawConversion(Pawn caster, Thing source)
        {
            var scope = transmutating;
            transmutating = null;
            return scope != null && scope.caster == caster && scope.source == source && !source.Destroyed && source.stackCount > 0
                ? new RawConversion { scope = scope, sourceDef = source.def, sourceCount = source.stackCount } : null;
        }
        public static void RawResourceConsumed(RawConversion conversion, int amount, bool paymentDestroyed)
        {
            if (conversion == null || conversion.used || amount <= 0 || amount > conversion.sourceCount) return;
            var source = conversion.scope.source;
            conversion.paid = paymentDestroyed && (source.Destroyed || source.stackCount <= conversion.sourceCount - amount);
        }
        public static bool CompleteRawConversion(RawConversion conversion, ThingDef outputDef, int outputCount, bool placed)
        {
            if (conversion == null || conversion.used) return false;
            conversion.used = true;
            var scope = conversion.scope;
            return conversion.paid && placed && outputCount > 0 && outputDef != null && outputDef != conversion.sourceDef
                && Owned(scope.caster, false) == scope.path && Owner(scope.caster, false) == scope.owner
                && RuntimeOwned(scope.ability, scope.caster, false) && scope.path.RecordCompletedTrial(FoundationTrialId);
        }
        static bool Ally(Pawn caster, Pawn target) => target != null && target != CastingBody(caster) && !target.Dead && target.Spawned
            && target.Map == CastingBody(caster)?.Map && target.Faction == caster.Faction && !target.IsPrisoner && !target.IsSlave
            && target.RaceProps?.Humanlike == true && !TM_Calc.IsUndead(target) && target.health?.hediffSet != null
            && target.equipment?.Primary?.def.IsMeleeWeapon == true;
        static bool NativeEnchant(Hediff h) => h.def == TorannMagicDefOf.TM_WeaponEnchantment_FireHD
            || h.def == TorannMagicDefOf.TM_WeaponEnchantment_IceHD || h.def == TorannMagicDefOf.TM_WeaponEnchantment_LitHD
            || h.def == TorannMagicDefOf.TM_WeaponEnchantment_DarkHD;
        public sealed class EnchantmentSnapshot
        {
            internal Pawn caster, target;
            internal PawnAbility ability;
            internal Thing weapon;
            internal Hediff[] before;
            internal MagicPower owner;
            internal DisciplineProgress path;
            internal bool used;
        }
        public static EnchantmentSnapshot CaptureEnchantment(PawnAbility ability, Pawn target)
        {
            var caster = ability?.Pawn;
            var path = Owned(caster, true);
            return path != null && RuntimeOwned(ability, caster, true) && Ally(caster, target)
                ? new EnchantmentSnapshot { caster = caster, target = target, ability = ability, weapon = target.equipment.Primary,
                    before = target.health.hediffSet.hediffs.ToArray(), owner = Owner(caster, true), path = path } : null;
        }
        public static bool CompleteEnchantment(EnchantmentSnapshot snapshot)
        {
            if (snapshot == null || snapshot.used) return false;
            snapshot.used = true;
            if (Owned(snapshot.caster, true) != snapshot.path || Owner(snapshot.caster, true) != snapshot.owner
                || !RuntimeOwned(snapshot.ability, snapshot.caster, true) || !Ally(snapshot.caster, snapshot.target)
                || snapshot.target.equipment.Primary != snapshot.weapon || Magic(snapshot.caster).weaponEnchants?.Contains(snapshot.target) != true) return false;
            return snapshot.target.health.hediffSet.hediffs.Any(h => !snapshot.before.Contains(h) && NativeEnchant(h)
                && h.Severity > 0 && !float.IsNaN(h.Severity) && !float.IsInfinity(h.Severity)
                && h.TryGetComp<HediffComp_EnchantedWeapon>()?.enchanterPawn == snapshot.caster
                && h.TryGetComp<HediffComp_EnchantedWeapon>()?.enchantedWeapon == snapshot.weapon)
                && snapshot.path.RecordCompletedTrial(MasteryTrialId);
        }
    }
}
