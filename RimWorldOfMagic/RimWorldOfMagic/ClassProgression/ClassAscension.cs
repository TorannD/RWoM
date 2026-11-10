using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public static class ClassAscension
    {
        public const int RitualTicks = 2500;
        public static string CandidateClassId(Pawn pawn)
        {
            return Candidates(pawn).FirstOrDefault();
        }
        internal static IEnumerable<string> Candidates(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state)
                || state.NeedsLegacyElementChoice || (state.Disciplines.Count != 1 && !state.ElementalPair)
                || state.Disciplines.Any(d => d?.Stage == InitiationStage.Ascended)) yield break;
            foreach (var path in state.Disciplines)
                if (path != null && path.Domain == DisciplineDomain.Magic && path.Stage == InitiationStage.Master
                && (path.ClassId == "rwom.chaos" || path.ClassId == "rwom.priest" || path.ClassId == "rwom.necromancer" || path.ClassId == "rwom.fire" || path.ClassId == "rwom.ice" || path.ClassId == "rwom.lightning" || path.ClassId == "rwom.arcanist" || path.ClassId == "rwom.paladin" || path.ClassId == "rwom.summoner" || path.ClassId == "rwom.druid" || path.ClassId == "rwom.bard" || path.ClassId == "rwom.warlock" || path.ClassId == "rwom.succubus" || path.ClassId == "rwom.geomancer" || path.ClassId == "rwom.technomancer" || path.ClassId == "rwom.bloodmage" || path.ClassId == "rwom.enchanter" || path.ClassId == "rwom.chronomancer" || path.ClassId == "rwom.brightmage" || path.ClassId == "rwom.shaman" || path.ClassId == "rwom.golemancer" || path.ClassId == "rwom.empath")) yield return path.ClassId;
        }
        private static string ForOffering(Pawn pawn, Thing scroll)
            => scroll?.def == null ? null : Candidates(pawn).FirstOrDefault(id => OfferingDefName(id) == scroll.def.defName);
        internal static TMAbilityDef Ultimate(string classId)
            => classId == "rwom.empath" ? TorannMagicDefOf.TM_Symbiosis : classId == "rwom.golemancer" ? TorannMagicDefOf.TM_LivingWall : classId == "rwom.shaman" ? TorannMagicDefOf.TM_GuardianSpirit : classId == "rwom.brightmage" ? TorannMagicDefOf.TM_SpiritOfLight : classId == "rwom.chronomancer" ? TorannMagicDefOf.TM_Recall : classId == "rwom.enchanter" ? TorannMagicDefOf.TM_Shapeshift : classId == "rwom.bloodmage" ? TorannMagicDefOf.TM_BloodMoon : classId == "rwom.technomancer" ? TorannMagicDefOf.TM_OrbitalStrike : classId == "rwom.geomancer" ? TorannMagicDefOf.TM_Meteor : classId == "rwom.warlock" ? TorannMagicDefOf.TM_PsychicShock : classId == "rwom.succubus" ? TorannMagicDefOf.TM_Scorn : classId == "rwom.druid" ? TorannMagicDefOf.TM_RegrowLimb : classId == "rwom.bard" ? TorannMagicDefOf.TM_BattleHymn : classId == "rwom.paladin" ? TorannMagicDefOf.TM_HolyWrath : classId == "rwom.summoner" ? TorannMagicDefOf.TM_SummonPoppi : classId == "rwom.arcanist" ? TorannMagicDefOf.TM_FoldReality : classId == "rwom.lightning" ? TorannMagicDefOf.TM_EyeOfTheStorm : classId == "rwom.ice" ? TorannMagicDefOf.TM_Blizzard : classId == "rwom.fire" ? TorannMagicDefOf.TM_Firestorm : classId == "rwom.necromancer" ? TorannMagicDefOf.TM_LichForm : TorannMagicDefOf.TM_Resurrection;
        private static string OfferingDefName(string classId)
            => classId == "rwom.chaos" || classId == "rwom.empath" ? "GemstoneOfInsight_Magic" : classId == "rwom.golemancer" ? "SpellOf_LivingWall" : classId == "rwom.shaman" ? "SpellOf_GuardianSpirit" : classId == "rwom.brightmage" ? "SpellOf_SpiritOfLight" : classId == "rwom.chronomancer" ? "SpellOf_Recall" : classId == "rwom.enchanter" ? "SpellOf_Shapeshift" : classId == "rwom.bloodmage" ? "SpellOf_BloodMoon" : classId == "rwom.technomancer" ? "SpellOf_OrbitalStrike" : classId == "rwom.geomancer" ? "SpellOf_Meteor" : classId == "rwom.warlock" ? "SpellOf_PsychicShock" : classId == "rwom.succubus" ? "SpellOf_Scorn" : classId == "rwom.druid" ? "SpellOf_RegrowLimb" : classId == "rwom.bard" ? "SpellOf_BattleHymn" : classId == "rwom.paladin" ? "SpellOf_HolyWrath" : classId == "rwom.summoner" ? "SpellOf_SummonPoppi" : classId == "rwom.arcanist" ? "SpellOf_FoldReality" : classId == "rwom.lightning" ? "SpellOf_EyeOfTheStorm" : classId == "rwom.ice" ? "SpellOf_Blizzard" : classId == "rwom.fire" ? "SpellOf_Firestorm" : classId == "rwom.necromancer" ? "SpellOf_LichForm" : "SpellOf_Resurrection";
        private static List<MagicPower> Powers(MagicData data, string classId)
            => classId == "rwom.empath" ? EmpathOwners.Resolve(data) : classId == "rwom.golemancer" ? data.MagicPowersGolemancer : classId == "rwom.shaman" ? data.MagicPowersShaman : classId == "rwom.brightmage" ? data.MagicPowersBrightmage : classId == "rwom.chronomancer" ? data.MagicPowersC : classId == "rwom.enchanter" ? data.MagicPowersE : classId == "rwom.bloodmage" ? data.MagicPowersBM : classId == "rwom.technomancer" ? data.MagicPowersT : classId == "rwom.geomancer" ? data.MagicPowersG : classId == "rwom.warlock" ? data.MagicPowersWD : classId == "rwom.succubus" ? data.MagicPowersSD : classId == "rwom.druid" ? data.MagicPowersD : classId == "rwom.bard" ? data.MagicPowersB : classId == "rwom.paladin" ? data.MagicPowersP : classId == "rwom.summoner" ? data.MagicPowersS : classId == "rwom.arcanist" ? data.MagicPowersA : classId == "rwom.lightning" ? data.MagicPowersSB : classId == "rwom.ice" ? data.MagicPowersHoF : classId == "rwom.fire" ? data.MagicPowersIF : classId == "rwom.necromancer" ? data.MagicPowersN : data.MagicPowersPR;
        private static readonly HashSet<Pawn> inFlight = new HashSet<Pawn>();
        public static bool IsCandidate(Pawn pawn)
            => pawn != null && !pawn.Dead && !pawn.Downed && pawn.Spawned && pawn.IsColonistPlayerControlled
                && ClassIdentity.TryGetState(pawn, out var state) && state.Vocation == Vocation.Mage
                && CandidateClassId(pawn) != null && !ClassInitiation.HasLegacyClass(pawn);

        public static bool CanBegin(Pawn pawn, Thing circle, Thing scroll)
        {
            if (!IsCandidate(pawn) || circle == null || circle.Destroyed || !circle.Spawned || circle.Map != pawn.Map
                || circle.def?.thingClass != typeof(Building_TMMagicCircle) || circle.Faction != pawn.Faction
                || circle.IsForbidden(pawn) || scroll == null || scroll.Destroyed || scroll.stackCount < 1
                || ForOffering(pawn, scroll) == null || !scroll.def.destroyable || scroll.IsForbidden(pawn)) return false;
            if (circle is Building_TMMagicCircle activeCircle
                && (activeCircle.IsActive || activeCircle.IsPending || activeCircle.MageList.Count > 0)) return false;
            if (scroll.Spawned ? scroll.Map != pawn.Map : scroll.ParentHolder != pawn.carryTracker) return false;
            var classId = ForOffering(pawn, scroll);
            var ultimate = Ultimate(classId);
            var profile = ClassIdentity.Profile(classId);
            if (classId == "rwom.chaos") return profile?.domain == DisciplineDomain.Magic && pawn.GetCompAbilityUserMagic()?.ExistingMagicData != null;
            if (profile?.domain != DisciplineDomain.Magic || profile.spells == null
                || profile.spells.Count(s => s?.stage == InitiationStage.Ascended && s.ability == ultimate) != 1) return false;
            var magic = pawn.GetCompAbilityUserMagic();
            return magic?.MagicData != null
                && Powers(magic.MagicData, classId)?.Count(p => p.TMabilityDefs.Contains(ultimate)) == 1;
        }

        public static Thing FindOffering(Pawn pawn, Thing circle) => FindClassOffering(pawn, circle, CandidateClassId(pawn));

        internal static Thing FindClassOffering(Pawn pawn, Thing circle, string classId)
        {
            if (!IsCandidate(pawn) || circle?.Map != pawn.Map) return null;
            var carried = pawn.carryTracker?.CarriedThing;
            if (carried?.def?.defName == OfferingDefName(classId) && CanBegin(pawn, circle, carried)) return carried;
            var def = DefDatabase<ThingDef>.GetNamedSilentFail(OfferingDefName(classId));
            if (def == null) return null;
            return pawn.Map.listerThings.ThingsOfDef(def)
                .Where(t => CanBegin(pawn, circle, t) && pawn.CanReserve(t)
                    && pawn.CanReach(t, PathEndMode.Touch, Danger.Some))
                .OrderBy(t => pawn.Position.DistanceToSquared(t.Position)).FirstOrDefault();
        }

        public static bool Complete(Pawn pawn, Thing circle, Thing scroll)
        {
            if (pawn == null || !inFlight.Add(pawn)) return false;
            try
            {
                if (!CanBegin(pawn, circle, scroll) || pawn.Position != circle.InteractionCell
                    || scroll.ParentHolder != pawn.carryTracker || scroll.Spawned) return false;
                var state = ClassIdentity.Component(pawn).State;
                var magic = pawn.GetCompAbilityUserMagic();
                var classId = ForOffering(pawn, scroll);
                var trialId = classId + ".ascension";
                var owner = classId == "rwom.chaos" ? null : Powers(magic.MagicData, classId).Single(p => p.TMabilityDefs.Contains(Ultimate(classId)));
                var payment = scroll.SplitOff(1);
                payment.Destroy(DestroyMode.Vanish);
                if (!payment.Destroyed) return false;
                state.Find(classId).RecordCompletedTrial(trialId);
                if (!state.TryAdvanceAfterTrial(classId, InitiationStage.Master, trialId))
                    throw new System.InvalidOperationException("Ascension state changed during offering payment.");
                if (owner != null) owner.learned = true;
                if (classId == "rwom.necromancer") magic.spell_LichForm = true;
                else if (classId == "rwom.fire") magic.spell_Firestorm = true;
                else if (classId == "rwom.ice") magic.spell_Blizzard = true;
                else if (classId == "rwom.lightning") magic.spell_EyeOfTheStorm = true;
                else if (classId == "rwom.arcanist") magic.spell_FoldReality = true;
                else if (classId == "rwom.paladin") magic.spell_HolyWrath = true;
                else if (classId == "rwom.summoner") magic.spell_SummonPoppi = true;
                else if (classId == "rwom.druid") magic.spell_RegrowLimb = true;
                else if (classId == "rwom.bard") magic.spell_BattleHymn = true;
                else if (classId == "rwom.warlock") magic.spell_PsychicShock = true;
                else if (classId == "rwom.succubus") magic.spell_Scorn = true;
                else if (classId == "rwom.geomancer") magic.spell_Meteor = true;
                else if (classId == "rwom.technomancer") magic.spell_OrbitalStrike = true;
                else if (classId == "rwom.bloodmage") magic.spell_BloodMoon = true;
                else if (classId == "rwom.enchanter") magic.spell_Shapeshift = true;
                else if (classId == "rwom.chronomancer") magic.spell_Recall = true;
                else if (classId != "rwom.chaos" && classId != "rwom.brightmage" && classId != "rwom.shaman" && classId != "rwom.golemancer" && classId != "rwom.empath") magic.spell_Resurrection = true;
                ClassSpellAccess.RestoreLearned(magic);
                ClassIdentity.Component(pawn).SynchronizeHealthDisplay();
                return true;
            }
            finally { inFlight.Remove(pawn); }
        }
    }
}
