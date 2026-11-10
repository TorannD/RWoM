using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;
namespace TorannMagic.ClassProgression
{
    public static class ShamanTrials
    {
        public const string FoundationTrialId = "rwom.shaman.foundation", MasteryTrialId = "rwom.shaman.mastery";
        static CompAbilityUserMagic Magic(Pawn pawn) => pawn?.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
        static MagicPower Owner(Pawn pawn, bool hex) => Magic(pawn)?.MagicData.MagicPowersShaman.FirstOrDefault(p => p.learned && p.TMabilityDefs.Contains(hex ? TorannMagicDefOf.TM_Hex : TorannMagicDefOf.TM_Totems));
        static Pawn CastingBody(Pawn owner)
        {
            if (!SpiritRebirthState.TryGet(owner,out var state)) return owner?.Spawned == true ? owner : null;
            return !state.DepletionPending && state.HasValidHost(owner) && state.Host.Spawned ? state.Host : null;
        }
        static DisciplineProgress Owned(Pawn pawn, bool hex)
        {
            if (pawn == null || pawn.Dead || CastingBody(pawn) == null || pawn.Faction == null
                || pawn.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true
                || !ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.shaman");
            return path?.Domain == DisciplineDomain.Magic && path.Stage == (hex ? InitiationStage.Adept : InitiationStage.Foundation)
                && !path.CompletedTrials.Contains(hex ? MasteryTrialId : FoundationTrialId) && Owner(pawn, hex) != null ? path : null;
        }
        static bool RuntimeOwned(PawnAbility ability, Pawn caster, bool hex) => ability != null && ability.Pawn == caster
            && (hex ? ability.Def == Owner(caster, true)?.abilityDef : ability.Def == TorannMagicDefOf.TM_SummonTotemHealing)
            && ability.AbilityUser == Magic(caster) && Magic(caster)?.AbilityData.Powers.Any(p => ReferenceEquals(p, ability)) == true;
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Living(Pawn caster, Pawn target) => target != null && !target.Dead && target.Spawned && target.Map == CastingBody(caster)?.Map
            && !target.IsPrisoner && !target.IsSlave && !TM_Calc.IsUndead(target) && target.health?.hediffSet != null;
        static bool Ally(Pawn caster, Pawn target) => Living(caster, target) && target.Faction == caster.Faction;
        static bool Enemy(Pawn caster, Pawn target) => Living(caster, target) && target != caster && target.RaceProps?.IsFlesh == true && target.HostileTo(caster)
            && (target.Faction == null ? target.InAggroMentalState : target.Faction != caster.Faction && target.Faction.HostileTo(caster.Faction));
        public sealed class SpawnSnapshot
        {
            internal Pawn caster;
            internal PawnAbility ability;
            internal MagicPower owner;
            internal DisciplineProgress path;
            internal bool used;
        }
        public static SpawnSnapshot CaptureSpawn(PawnAbility ability)
        {
            var caster = ability?.Pawn; var path = Owned(caster, false);
            return path != null && RuntimeOwned(ability, caster, false)
                ? new SpawnSnapshot { caster = caster, ability = ability, owner = Owner(caster, false), path = path } : null;
        }
        public static bool CompleteSpawn(SpawnSnapshot snapshot, Building_TMTotem_Healing totem)
        {
            if (snapshot == null || snapshot.used) return false;
            snapshot.used = true;
            if (totem == null || !totem.Spawned || totem.def != TorannMagicDefOf.TM_HealingTotem || totem.Map != CastingBody(snapshot.caster)?.Map || totem.Faction != snapshot.caster.Faction
                || Owned(snapshot.caster, false) != snapshot.path || Owner(snapshot.caster, false) != snapshot.owner || !RuntimeOwned(snapshot.ability, snapshot.caster, false)) return false;
            totem.trialCaster = snapshot.caster; totem.trialAbilityId = "TM_SummonTotemHealing"; totem.trialOwner = snapshot.owner;
            return true;
        }
        static bool Source(Building_TMTotem_Healing totem)
        {
            var caster = totem?.trialCaster;
            return caster != null && totem.Spawned && totem.Map == CastingBody(caster)?.Map && totem.Faction == caster.Faction
                && totem.def == TorannMagicDefOf.TM_HealingTotem && totem.trialAbilityId == "TM_SummonTotemHealing"
                && (totem.trialOwner == null || totem.trialOwner == Owner(caster, false))
                && Magic(caster)?.AbilityData.Powers.Any(a => RuntimeOwned(a, caster, false)) == true;
        }
        public sealed class HealSnapshot
        {
            internal Building_TMTotem_Healing totem;
            internal Pawn caster, target;
            internal MagicPower owner;
            internal DisciplineProgress path;
            internal Dictionary<Hediff_Injury, float> before;
            internal bool used;
        }
        public static HealSnapshot CaptureHealing(Building_TMTotem_Healing totem, Pawn target)
        {
            var caster = totem?.trialCaster; var path = Owned(caster, false);
            if (path == null || !Source(totem) || !Ally(caster, target)) return null;
            var injuries = target.health.hediffSet.hediffs.OfType<Hediff_Injury>().Where(h => Finite(h.Severity) && h.Severity > 0).ToDictionary(h => h, h => h.Severity);
            return injuries.Count > 0 ? new HealSnapshot { totem = totem, caster = caster, target = target, owner = Owner(caster, false), path = path, before = injuries } : null;
        }
        public static void CompleteHealing(HealSnapshot snapshot)
        {
            if (snapshot == null || snapshot.used) return;
            snapshot.used = true;
            if (Owned(snapshot.caster, false) != snapshot.path || Owner(snapshot.caster, false) != snapshot.owner || snapshot.totem.trialCaster != snapshot.caster
                || !Source(snapshot.totem) || !Ally(snapshot.caster, snapshot.target)) return;
            float amount = 0;
            foreach (var injury in snapshot.before)
            {
                float after = snapshot.target.health.hediffSet.hediffs.Contains(injury.Key) ? injury.Key.Severity : 0;
                if (Finite(after) && after >= 0 && after < injury.Value) amount += injury.Value - after;
            }
            if (!Finite(amount) || amount <= 0) return;
            var progress = snapshot.path.ShamanTrials;
            progress.Healing = Math.Min(8, progress.Healing + amount);
            if (progress.Healing >= 8) snapshot.path.RecordCompletedTrial(FoundationTrialId);
        }
        public sealed class HexSnapshot
        {
            internal Pawn caster, target;
            internal PawnAbility ability;
            internal MagicPower owner;
            internal DisciplineProgress path;
            internal bool used;
        }
        public static HexSnapshot CaptureHex(PawnAbility ability, Pawn target)
        {
            var caster = ability?.Pawn; var path = Owned(caster, true);
            return path != null && RuntimeOwned(ability, caster, true) && Enemy(caster, target)
                && !target.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HexHD) && !Magic(caster).HexedPawns.Contains(target)
                ? new HexSnapshot { caster = caster, target = target, ability = ability, owner = Owner(caster, true), path = path } : null;
        }
        public static bool CompleteHex(HexSnapshot snapshot)
        {
            if (snapshot == null || snapshot.used) return false;
            snapshot.used = true;
            if (Owned(snapshot.caster, true) != snapshot.path || Owner(snapshot.caster, true) != snapshot.owner
                || !RuntimeOwned(snapshot.ability, snapshot.caster, true) || !Enemy(snapshot.caster, snapshot.target) || !Magic(snapshot.caster).HexedPawns.Contains(snapshot.target)) return false;
            var hd = snapshot.target.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HexHD);
            return hd != null && hd.pawn == snapshot.target && Finite(hd.Severity) && hd.Severity > 0 && snapshot.path.RecordCompletedTrial(MasteryTrialId);
        }
    }
}
