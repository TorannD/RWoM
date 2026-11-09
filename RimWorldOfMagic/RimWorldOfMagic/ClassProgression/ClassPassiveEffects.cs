using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class ClassPassiveEffects
    {
        private static TraitDef Priest(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state)
                || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.priest");
            var profile = ClassIdentity.Profile("rwom.priest");
            if (path == null || path.Domain != DisciplineDomain.Magic
                || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait?.defName != "Priest") return null;
            // Even a suppressed legacy trait must not acquire a second owner here.
            if (pawn.story?.traits?.allTraits.Any(t => t.def == profile.legacyTrait) == true) return null;
            return profile.legacyTrait;
        }

        public static float StatOffset(Pawn pawn, StatDef stat)
            => SpiritRebirthBodyEffects.SuppressFormerBodyEffects(pawn) ? 0f
                : RangerFoundation.Offset(pawn,stat) + BladeFoundation.Offset(pawn,stat) + SniperFoundation.Offset(pawn,stat) + Offset(Demon(pawn), stat) + Offset(Priest(pawn), stat) + Offset(Necromancer(pawn), stat) + Offset(Lich(pawn), stat) + Offset(Fire(pawn), stat) + Offset(Ice(pawn), stat) + Offset(Lightning(pawn), stat) + Offset(Arcanist(pawn), stat) + Offset(Paladin(pawn), stat) + Offset(Summoner(pawn), stat) + Offset(Druid(pawn), stat) + Offset(Technomancer(pawn), stat) + Offset(Geomancer(pawn), stat) + Offset(BloodMage(pawn), stat) + Offset(Enchanter(pawn), stat) + Offset(Chronomancer(pawn), stat) + Offset(Brightmage(pawn), stat) + Offset(Golemancer(pawn), stat) + Offset(Empath(pawn), stat) + Offset(Shaman(pawn), stat) + (Bard(pawn)?.DataAtDegree(BardDegree(pawn))?.statOffsets?.Where(x => x.stat == stat).Sum(x => x.value) ?? 0f);

        private static TraitDef Fire(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.fire");
            var profile = ClassIdentity.Profile("rwom.fire");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.InnerFire
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.InnerFire) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Ice(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.ice");
            var profile = ClassIdentity.Profile("rwom.ice");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.HeartOfFrost
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.HeartOfFrost) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Lightning(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.lightning");
            var profile = ClassIdentity.Profile("rwom.lightning");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.StormBorn
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.StormBorn) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Arcanist(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.arcanist");
            var profile = ClassIdentity.Profile("rwom.arcanist");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.Arcanist
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.Arcanist) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Paladin(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.paladin");
            var profile = ClassIdentity.Profile("rwom.paladin");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.Paladin
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.Paladin) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Summoner(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.summoner");
            var profile = ClassIdentity.Profile("rwom.summoner");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.Summoner
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.Summoner) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Druid(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.druid");
            var profile = ClassIdentity.Profile("rwom.druid");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.Druid
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.Druid) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Technomancer(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.technomancer");
            var profile = ClassIdentity.Profile("rwom.technomancer");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.Technomancer
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.Technomancer) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Geomancer(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.geomancer");
            var profile = ClassIdentity.Profile("rwom.geomancer");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.Geomancer
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.Geomancer) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef BloodMage(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.bloodmage");
            var profile = ClassIdentity.Profile("rwom.bloodmage");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.BloodMage
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.BloodMage) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Enchanter(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.enchanter");
            var profile = ClassIdentity.Profile("rwom.enchanter");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.Enchanter
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.Enchanter) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Chronomancer(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.chronomancer");
            var profile = ClassIdentity.Profile("rwom.chronomancer");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.Chronomancer
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.Chronomancer) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Brightmage(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.brightmage");
            var profile = ClassIdentity.Profile("rwom.brightmage");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.TM_Brightmage
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.TM_Brightmage) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Golemancer(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.golemancer");
            var profile = ClassIdentity.Profile("rwom.golemancer");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.TM_Golemancer
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.TM_Golemancer) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Empath(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.empath");
            var profile = ClassIdentity.Profile("rwom.empath");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.TM_Empath
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.TM_Empath) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Shaman(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.shaman");
            var profile = ClassIdentity.Profile("rwom.shaman");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.TM_Shaman
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.TM_Shaman) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Bard(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.bard");
            var profile = ClassIdentity.Profile("rwom.bard");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage < InitiationStage.Foundation || path.Stage > InitiationStage.Ascended
                || profile?.domain != DisciplineDomain.Magic || profile.legacyTrait != TorannMagicDefOf.TM_Bard
                || pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.TM_Bard) == true) return null;
            return profile.legacyTrait;
        }

        private static TraitDef Demon(Pawn pawn)
        {
            if (!ClassIdentity.TryGetDemonBranch(pawn, out bool warlock)) return null;
            var trait = warlock ? TorannMagicDefOf.Warlock : TorannMagicDefOf.Succubus;
            var profile = ClassIdentity.Profile(warlock ? "rwom.warlock" : "rwom.succubus");
            return profile?.legacyTrait == trait && pawn.story?.traits?.allTraits.Any(t => t.def == trait) != true ? trait : null;
        }

        public static int BardDegree(Pawn pawn)
            => System.Math.Max(0, System.Math.Min(3, pawn?.GetCompAbilityUserMagic()?.MagicData?.MagicPowerSkill_BardTraining?.FirstOrDefault(x => x.label == "TM_BardTraining_pwr")?.level ?? 0));

        private static float Offset(TraitDef trait, StatDef stat)
            => trait?.DataAtDegree(0)?.statOffsets?.Where(s => s.stat == stat).Sum(s => s.value) ?? 0f;

        private static TraitDef Necromancer(Pawn pawn)
        {
            if (!ClassIdentity.TryGetState(pawn, out _) || !ClassIdentity.IsNecromancer(pawn)
                || ClassIdentity.IsLich(pawn)) return null;
            var profile = ClassIdentity.Profile("rwom.necromancer");
            if (profile?.domain != DisciplineDomain.Magic || profile.legacyTrait?.defName != "Necromancer") return null;
            // Existing Necromancer or transformed Lich traits remain the native stat owner.
            if (pawn.story?.traits?.allTraits.Any(t => t.def == profile.legacyTrait
                || t.def == TorannMagicDefOf.Lich) == true) return null;
            return profile.legacyTrait;
        }

        public static WorkTags DisabledWork(Pawn pawn)
            => SpiritRebirthBodyEffects.SuppressFormerBodyEffects(pawn) ? WorkTags.None
                : (Priest(pawn)?.disabledWorkTags ?? WorkTags.None) | (Lich(pawn)?.disabledWorkTags ?? WorkTags.None) | (Bard(pawn)?.disabledWorkTags ?? WorkTags.None);

        public static TraitDef Lich(Pawn pawn)
        {
            if (SpiritRebirthBodyEffects.SuppressFormerBodyEffects(pawn)) return null;
            if (!ClassIdentity.TryGetState(pawn, out _) || !ClassIdentity.IsLich(pawn)) return null;
            // Suppressed native traits still own their effects; never revive or double them.
            if (pawn.story?.traits?.allTraits.Any(t => t.def == TorannMagicDefOf.Lich) == true) return null;
            return TorannMagicDefOf.Lich;
        }

        public static float StatFactor(Pawn pawn, StatDef stat)
            => SpiritRebirthBodyEffects.SuppressFormerBodyEffects(pawn) ? 1f
                : (stat == StatDefOf.MoveSpeed ? RangerSafeTrails.Speed(pawn) : 1f) * (stat == StatDefOf.MoveSpeed && JobDriver_BladeSevenSteps.Exhausted(pawn) ? .8f
                : stat == StatDefOf.MoveSpeed && pawn?.jobs?.curDriver is JobDriver_RangerBreakthrough hunt && hunt.Active ? .8f
                : RangerFoundation.Factor(pawn,stat) * (Lich(pawn)?.DataAtDegree(0)?.statFactors?.FirstOrDefault(s => s.stat == stat)?.value ?? 1f));
    }

    [HarmonyPatch(typeof(Pawn_StoryTracker), nameof(Pawn_StoryTracker.DisabledWorkTagsBackstoryAndTraits), MethodType.Getter)]
    public static class ClassPassiveWorkPatch
    {
        public static void Postfix(Pawn ___pawn, ref WorkTags __result)
            => __result |= ClassPassiveEffects.DisabledWork(___pawn);
    }

    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized))]
    public static class ClassPassiveStatPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var offset = AccessTools.Method(typeof(Trait), nameof(Trait.OffsetOfStat));
            var health = AccessTools.Field(typeof(Pawn), nameof(Pawn.health));
            var set = AccessTools.Field(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.hediffSet));
            var hediffs = AccessTools.Field(typeof(HediffSet), nameof(HediffSet.hediffs));
            var calls = Enumerable.Range(0, System.Math.Max(0, code.Count - 2)).Where(i => code[i].Calls(offset)
                && code[i + 1].opcode == OpCodes.Add && code[i + 2].IsStloc()).ToList();
            var anchors = Enumerable.Range(0, System.Math.Max(0, code.Count - 2)).Where(i => code[i].LoadsField(health)
                && code[i + 1].LoadsField(set) && code[i + 2].LoadsField(hediffs)).ToList();
            if (calls.Count != 1 || anchors.Count != 1 || anchors[0] <= calls[0])
            {
                Log.Warning("[RWoM] Priest progression stat offsets disabled: native trait-offset stage was not uniquely recognized.");
                return code;
            }
            var store = code[calls[0] + 2];
            CodeInstruction load;
            if (store.opcode == OpCodes.Stloc_0) load = new CodeInstruction(OpCodes.Ldloc_0);
            else if (store.opcode == OpCodes.Stloc_1) load = new CodeInstruction(OpCodes.Ldloc_1);
            else if (store.opcode == OpCodes.Stloc_2) load = new CodeInstruction(OpCodes.Ldloc_2);
            else if (store.opcode == OpCodes.Stloc_3) load = new CodeInstruction(OpCodes.Ldloc_3);
            else load = new CodeInstruction(store.opcode == OpCodes.Stloc_S ? OpCodes.Ldloc_S : OpCodes.Ldloc, store.operand);
            // Pawn is already on the stack. Preserve it for the native health read,
            // and add to the same accumulator before ANY trait/hediff multipliers.
            var injected = new List<CodeInstruction> {
                new CodeInstruction(OpCodes.Dup), new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StatWorker), "stat")),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ClassPassiveEffects), nameof(ClassPassiveEffects.StatOffset))),
                load, new CodeInstruction(OpCodes.Add), new CodeInstruction(store.opcode, store.operand)
            };
            injected[0].labels.AddRange(code[anchors[0]].labels);
            code[anchors[0]].labels.Clear();
            injected[0].blocks.AddRange(code[anchors[0]].blocks);
            code[anchors[0]].blocks.Clear();
            code.InsertRange(anchors[0], injected);
            return code;
        }
    }
}
