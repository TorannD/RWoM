using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class ClassSpellAccess
    {
        public static List<MagicPower> ClassPowers(Pawn pawn, MagicData data)
        {
            var result = new List<MagicPower>();
            if (data == null || !ClassIdentity.TryGetState(pawn, out var state)) return result;
            var comp = pawn.GetCompAbilityUserMagic();
            if (ClassIdentity.TryGetDemonBranch(pawn, out _) && ReferenceEquals(comp?.MagicData, data))
                TM_ClassState.RestoreSharedMagicProgression(comp);
            if (state.Find("rwom.chaos") != null)
                foreach (var power in data.MagicPowersCM) if (!result.Contains(power)) result.Add(power);
            foreach (var direction in state.Disciplines)
            {
                var profile = ClassIdentity.Profile(direction?.ClassId);
                if (profile?.profileId == "rwom.empath")
                {
                    if (profile.domain == DisciplineDomain.Magic && profile.magicPowerList == "magicPowerCustom")
                        foreach (var owner in EmpathOwners.Resolve(data) ?? new List<MagicPower>()) if (!result.Contains(owner)) result.Add(owner);
                    continue;
                }
                if (profile?.domain != DisciplineDomain.Magic || string.IsNullOrEmpty(profile.magicPowerList)
                    || !profile.magicPowerList.StartsWith("magicPower", System.StringComparison.Ordinal)
                    || profile.magicPowerList.Length <= "magicPower".Length) continue;
                var powers = TM_ClassGroups.ResolveMagicList(data, profile.magicPowerList);
                if (powers != null) foreach (var power in powers) if (!result.Contains(power)) result.Add(power);
            }
            // Novice spell picks and every learned power (scrolls, standalones) always
            // render, so they can be upgraded in the Grimoire before a discipline —
            // or without one — is chosen.
            if (state.NoviceSpells != null)
                foreach (string id in state.NoviceSpells)
                {
                    var noviceAbility = DefDatabase<TMAbilityDef>.GetNamedSilentFail(id);
                    var noviceOwner = noviceAbility == null ? null : data.AllMagicPowers.FirstOrDefault(p => p.TMabilityDefs != null && p.TMabilityDefs.Contains(noviceAbility));
                    if (noviceOwner != null && !result.Contains(noviceOwner)) result.Add(noviceOwner);
                }
            foreach (var owner in data.AllMagicPowers)
                if (owner != null && owner.learned && owner.abilityDef is TMAbilityDef && !result.Contains(owner)) result.Add(owner);
            return result;
        }

        // Learned spell ownership always wins, including preserved legacy investments.
        // This controls new learning, never strips abilities or rewrites skill records.
        public static bool CanLearn(Pawn pawn, MagicPower power)
        {
            if (power == null) return false;
            var canonicalComp = pawn?.GetCompAbilityUserMagic();
            if (ClassIdentity.TryGetDemonBranch(pawn, out _) && power.TMabilityDefs.Any(a => a?.defName == "TM_SoulBond" || a?.defName == "TM_ShadowBolt" || a?.defName == "TM_Dominate"))
            {
                TM_ClassState.RestoreSharedMagicProgression(canonicalComp);
                if (!ReferenceEquals(TM_ClassState.PreferredMagicOwner(canonicalComp, power.TMabilityDefs[0]), power)) return false;
            }
            if (power.learned || !ClassIdentity.TryGetState(pawn, out var state)) return true;
            if (EmpathOwners.IsFamily(power)) return EmpathOwners.CanLearn(pawn, power);
            if (EmpathOwners.IsFade(power)) return EmpathOwners.CanLearnFade(pawn, power);
            if (GolemancerBrands.IsBrand(power)) return GolemancerBrands.CanLearn(pawn, power);
            if (power.TMabilityDefs.Contains(TorannMagicDefOf.TM_DeathBolt) && !ClassIdentity.IsLich(pawn)) return false;
            bool profiled = false;
            foreach (var profile in DefDatabase<ClassDisciplineDef>.AllDefsListForReading)
                foreach (var spell in profile.spells ?? new List<DisciplineSpell>())
                {
                    if (spell?.ability == null || !power.TMabilityDefs.Contains(spell.ability)) continue;
                    profiled = true;
                    var path = state.Find(profile.profileId);
                    if (path != null && path.Domain == profile.domain
                        && path.Stage >= InitiationStage.Foundation && path.Stage <= InitiationStage.Ascended
                        && spell.stage >= InitiationStage.Foundation && spell.stage <= InitiationStage.Ascended
                        && path.Stage >= spell.stage) return true;
                }
            if (profiled) return false;
            // A missing/partial profile must not turn a class scroll into a bypass.
            // Existing learned owners returned above remain grandfathered.
            var magic = pawn.GetCompAbilityUserMagic();
            if (magic?.MagicData != null && (magic.MagicData.MagicPowersPR.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersN.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersIF.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersHoF.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersSB.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersP.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersS.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersT.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersG.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersShaman.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersGolemancer.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersBrightmage.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersC.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersE.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersBM.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersD.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersB.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersWD.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersSD.Any(p => ReferenceEquals(p, power))
                || magic.MagicData.MagicPowersA.Any(p => ReferenceEquals(p, power)))) return false;
            return true;
        }

        // A paid shared scroll is a separate access route from a class skill button.
        // This only lifts the progression stage gate; the existing item effect still
        // enforces mage/custom-class/required-trait restrictions and consumes the item.
        public static bool CanLearnFromScroll(Pawn pawn, MagicPower power, ThingDef scroll)
        {
            if (power?.TMabilityDefs == null || scroll == null
                || (!power.TMabilityDefs.OfType<TMAbilityDef>().Any(a => a.learnItem == scroll)
                    && !(scroll.defName == "SpellOf_Resurrection" && power.TMabilityDefs.Contains(TorannMagicDefOf.TM_Resurrection)))) return false;
            var data = pawn?.GetCompAbilityUserMagic()?.MagicData;
            if (EmpathOwners.IsFade(power)) return ReferenceEquals(EmpathOwners.FadeOwner(data), power) && CanLearn(pawn, power);
            if (GolemancerBrands.IsBrand(power)) return data != null && ReferenceEquals(GolemancerBrands.ScrollOwner(data, scroll), power) && CanLearn(pawn, power);
            if (data == null || !data.AllMagicPowersWithSkills.Any(owner => ReferenceEquals(owner, power))) return false;
            if (ReferenceEquals(SharedScrollOwner(data, scroll), power)) return true;
            return CanLearn(pawn, power);
        }

        internal static MagicPower SharedScrollOwner(MagicData data, ThingDef scroll)
        {
            if (data == null || scroll == null) return null;
            TMAbilityDef ability;
            IEnumerable<MagicPower> owners;
            switch (scroll.defName)
            {
                case "SpellOf_Rain": ability = TorannMagicDefOf.TM_Rainmaker; owners = data.MagicPowersHoF; break;
                case "SpellOf_Blink": ability = TorannMagicDefOf.TM_Blink; owners = data.MagicPowersA; break;
                case "SpellOf_Teleport": ability = TorannMagicDefOf.TM_Teleport; owners = data.MagicPowersA; break;
                case "SpellOf_Heal": ability = TorannMagicDefOf.TM_Heal; owners = data.MagicPowersP; break;
                case "SpellOf_SummonMinion": ability = TorannMagicDefOf.TM_SummonMinion; owners = data.MagicPowersS; break;
                default: return null;
            }
            return ability == null ? null : owners.FirstOrDefault(owner => owner.TMabilityDefs.Contains(ability) && owner.TMabilityDefs.OfType<TMAbilityDef>().Any(a => a.learnItem == scroll));
        }

        // Explicit mappings from InitializeSpell; flags are saved ownership, not
        // permission to learn another class's locked spells.
        internal static IEnumerable<TMAbilityDef> FlagAbilities(CompAbilityUserMagic comp)
        {
            if (comp.recallSpell && comp.recallSet && comp.recallPosition != default(IntVec3)
                && Find.TickManager != null && comp.recallExpiration > Find.TickManager.TicksGame)
                yield return TorannMagicDefOf.TM_Recall;
            if (comp.spell_Rain) yield return TorannMagicDefOf.TM_Rainmaker;
            if (comp.spell_Blink) yield return TorannMagicDefOf.TM_Blink;
            if (comp.spell_Teleport) yield return TorannMagicDefOf.TM_Teleport;
            if (comp.spell_Heal) yield return TorannMagicDefOf.TM_Heal;
            if (comp.spell_Heater) yield return TorannMagicDefOf.TM_Heater;
            if (comp.spell_Cooler) yield return TorannMagicDefOf.TM_Cooler;
            if (comp.spell_PowerNode) yield return TorannMagicDefOf.TM_PowerNode;
            if (comp.spell_Sunlight) yield return TorannMagicDefOf.TM_Sunlight;
            if (comp.spell_DryGround) yield return TorannMagicDefOf.TM_DryGround;
            if (comp.spell_WetGround) yield return TorannMagicDefOf.TM_WetGround;
            if (comp.spell_ChargeBattery) yield return TorannMagicDefOf.TM_ChargeBattery;
            if (comp.spell_SmokeCloud) yield return TorannMagicDefOf.TM_SmokeCloud;
            if (comp.spell_Extinguish) yield return TorannMagicDefOf.TM_Extinguish;
            if (comp.spell_EMP) yield return TorannMagicDefOf.TM_EMP;
            if (comp.spell_Blizzard) yield return TorannMagicDefOf.TM_Blizzard;
            if (comp.spell_Firestorm) yield return TorannMagicDefOf.TM_Firestorm;
            if (comp.spell_SummonMinion) yield return TorannMagicDefOf.TM_SummonMinion;
            if (comp.spell_TransferMana) yield return TorannMagicDefOf.TM_TransferMana;
            if (comp.spell_SiphonMana) yield return TorannMagicDefOf.TM_SiphonMana;
            if (comp.spell_RegrowLimb) yield return TorannMagicDefOf.TM_RegrowLimb;
            if (comp.spell_EyeOfTheStorm) yield return TorannMagicDefOf.TM_EyeOfTheStorm;
            if (comp.spell_HeatShield) yield return TorannMagicDefOf.TM_HeatShield;
            if (comp.spell_ManaShield) yield return TorannMagicDefOf.TM_ManaShield;
            if (comp.spell_Blur) yield return TorannMagicDefOf.TM_Blur;
            if (comp.spell_FoldReality) yield return TorannMagicDefOf.TM_FoldReality;
            if (comp.spell_Resurrection) yield return TorannMagicDefOf.TM_Resurrection;
            if (comp.spell_HolyWrath) yield return TorannMagicDefOf.TM_HolyWrath;
            if (comp.spell_LichForm) yield return TorannMagicDefOf.TM_LichForm;
            if (comp.spell_Flight) yield return TorannMagicDefOf.TM_Flight;
            if (comp.spell_SummonPoppi) yield return TorannMagicDefOf.TM_SummonPoppi;
            if (comp.spell_BattleHymn) yield return TorannMagicDefOf.TM_BattleHymn;
            if (comp.spell_CauterizeWound) yield return TorannMagicDefOf.TM_CauterizeWound;
            if (comp.spell_SpellMending) yield return TorannMagicDefOf.TM_SpellMending;
            if (comp.spell_FertileLands) yield return TorannMagicDefOf.TM_FertileLands;
            if (comp.spell_PsychicShock) yield return TorannMagicDefOf.TM_PsychicShock;
            if (comp.spell_Scorn) yield return TorannMagicDefOf.TM_Scorn;
            if (comp.spell_BlankMind) yield return TorannMagicDefOf.TM_BlankMind;
            if (comp.spell_ShadowStep) yield return TorannMagicDefOf.TM_ShadowStep;
            if (comp.spell_ShadowCall) yield return TorannMagicDefOf.TM_ShadowCall;
            if (comp.spell_Teach) yield return TorannMagicDefOf.TM_TeachMagic;
            if (comp.spell_Meteor) yield return TorannMagicDefOf.TM_Meteor;
            if (comp.spell_OrbitalStrike) yield return TorannMagicDefOf.TM_OrbitalStrike;
            if (comp.spell_BloodMoon) yield return TorannMagicDefOf.TM_BloodMoon;
            if (comp.spell_EnchantedAura) yield return TorannMagicDefOf.TM_EnchantedAura;
            if (comp.spell_Shapeshift) yield return TorannMagicDefOf.TM_Shapeshift;
            if (comp.spell_ShapeshiftDW) yield return TorannMagicDefOf.TM_ShapeshiftDW;
            if (comp.spell_DirtDevil) yield return TorannMagicDefOf.TM_DirtDevil;
            if (comp.spell_MechaniteReprogramming) yield return TorannMagicDefOf.TM_MechaniteReprogramming;
            if (comp.spell_ArcaneBolt) yield return TorannMagicDefOf.TM_ArcaneBolt;
            if (comp.spell_LightningTrap) yield return TorannMagicDefOf.TM_LightningTrap;
            if (comp.spell_Invisibility) yield return TorannMagicDefOf.TM_Invisibility;
            if (comp.spell_BriarPatch) yield return TorannMagicDefOf.TM_BriarPatch;
            if (comp.spell_Recall) yield return TorannMagicDefOf.TM_TimeMark;
            if (comp.spell_MageLight) yield return TorannMagicDefOf.TM_MageLight;
            if (comp.spell_SnapFreeze) yield return TorannMagicDefOf.TM_SnapFreeze;
            if (comp.spell_Ignite) yield return TorannMagicDefOf.TM_Ignite;
        }

        // Explicit saved children survive stage changes. New children require their
        // native main owner, never merely an invested skill or a copied ability.
        public static void RestoreEnchanterChildren(CompAbilityUserMagic comp)
        {
            if (comp == null || !ClassIdentity.TryGetRole(comp.Pawn, Vocation.Mage, out bool mage) || !mage) return;
            var data = comp.MagicData;
            var shape = data.MagicPowersE.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Shapeshift));
            if (comp.spell_Shapeshift && shape != null) shape.learned = true;
            bool native = ClassIdentity.IsEnchanter(comp.Pawn)
                && ClassIdentity.Profile("rwom.enchanter")?.domain == DisciplineDomain.Magic;
            var aura = data.MagicPowersStandalone.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_EnchantedAura));
            if (aura != null && (aura.learned || comp.spell_EnchantedAura
                || (native && data.MagicPowersE.Any(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_EnchantedBody)))))
            { aura.learned = true; comp.spell_EnchantedAura = true; TM_ClassState.AddRuntimeAbility(comp, aura.abilityDef as TMAbilityDef); }
            var wolf = data.MagicPowersStandalone.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_ShapeshiftDW));
            if (wolf != null && (wolf.learned || comp.spell_ShapeshiftDW
                || (native && shape?.learned == true && data.MagicPowerSkill_Shapeshift.Any(s => s.label == "TM_Shapeshift_ver" && s.level >= 3))))
            { wolf.learned = true; comp.spell_ShapeshiftDW = true; TM_ClassState.AddRuntimeAbility(comp, wolf.abilityDef as TMAbilityDef); }
        }

        // Recall owns TimeMark, while the Recall control itself belongs solely
        // to a real marker. Restoration must not create or refresh that marker.
        public static void RestoreChronomancerChildren(CompAbilityUserMagic comp)
        {
            if (comp == null || !ClassIdentity.TryGetRole(comp.Pawn, Vocation.Mage, out bool mage) || !mage) return;
            var data = comp.MagicData;
            var recall = data.MagicPowersC.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Recall));
            if (comp.spell_Recall && recall != null) recall.learned = true;
            var mark = data.MagicPowersStandalone.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_TimeMark));
            bool native = ClassIdentity.IsChronomancer(comp.Pawn)
                && ClassIdentity.Profile("rwom.chronomancer")?.domain == DisciplineDomain.Magic;
            if (mark != null && (mark.learned || comp.spell_Recall || (native && recall?.learned == true)))
            {
                mark.learned = true;
                TM_ClassState.AddRuntimeAbility(comp, mark.abilityDef);
            }
        }

        // These native children opt out of automatic initialization in XML.
        // Existing standalone ownership survives; new grants require the learned parent.
        public static void RestoreBrightmageChildren(CompAbilityUserMagic comp)
        {
            if (comp == null || !ClassIdentity.TryGetRole(comp.Pawn, Vocation.Mage, out bool mage) || !mage) return;
            var data = comp.MagicData;
            bool native = ClassIdentity.IsBrightmage(comp.Pawn)
                && ClassIdentity.Profile("rwom.brightmage")?.domain == DisciplineDomain.Magic;
            bool learned = data.MagicPowersBrightmage.Any(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightSkip));
            int power = data.MagicPowerSkill_LightSkip.FirstOrDefault(s => s.label == "TM_LightSkip_pwr")?.level ?? 0;
            var mass = data.MagicPowersStandalone.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightSkipMass));
            var global = data.MagicPowersStandalone.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightSkipGlobal));
            if (mass != null && (mass.learned || (native && learned && power >= 1)))
            { mass.learned = true; TM_ClassState.AddRuntimeAbility(comp, mass.abilityDef); }
            if (global != null && (global.learned || (native && learned && power >= 2)))
            { global.learned = true; TM_ClassState.AddRuntimeAbility(comp, global.abilityDef); }
        }

        public static bool RestoreLearned(CompAbilityUserMagic comp)
        {
            if (comp == null || !ClassIdentity.TryGetRole(comp.Pawn, Vocation.Mage, out bool mage)) return false;
            // InitializeSpell runs during PostLoadInit on every humanlike pawn. A
            // warrior has a magic component but legitimately has no MagicData.
            if (!mage) return true;
            var chaos = ChaosDiscipline.Path(comp.Pawn);
            if (chaos != null && comp.ExistingMagicData != null) ChaosDiscipline.RestoreView(comp,chaos);
            TM_ClassState.RestoreSharedMagicProgression(comp);
            var sharedCooldowns = new Dictionary<AbilityUser.AbilityDef, int>();
            if (ClassIdentity.TryGetDemonBranch(comp.Pawn, out _))
            {
                // Preserve the canonical runtime instance (and its cooldown), but
                // discard stale rank variants of the same restored native family.
                foreach (string name in new[] { "TM_SoulBond", "TM_ShadowBolt", "TM_Dominate" })
                {
                    var family = comp.MagicData.MagicPowersWD.FirstOrDefault(p => p.TMabilityDefs.Any(a => a?.defName == name));
                    var owner = family == null ? null : TM_ClassState.PreferredMagicOwner(comp, family.TMabilityDefs[0]);
                    if (owner?.learned != true) continue;
                    sharedCooldowns[owner.abilityDef] = comp.AbilityData.Powers.Where(a => a != null && owner.TMabilityDefs.Contains(a.Def))
                        .Select(a => a.CooldownTicksLeft).DefaultIfEmpty(0).Max();
                    comp.AbilityData.Powers.RemoveAll(a => a != null && owner.TMabilityDefs.Contains(a.Def) && a.Def != owner.abilityDef);
                }
                comp.AbilityData.AllPowers = null;
            }
            LichTransformation.Restore(comp);
            RestoreEnchanterChildren(comp);
            RestoreChronomancerChildren(comp);
            RestoreBrightmageChildren(comp);
            // Eye's saved scroll flag already owns the native SB power. Reconcile
            // that exact owner before runtime restoration, preserving its investment.
            // This neither enrolls a class nor unlocks an unowned ultimate.
            if (comp.spell_EyeOfTheStorm && TorannMagicDefOf.TM_EyeOfTheStorm != null)
            {
                var eye = comp.MagicData.MagicPowersSB.FirstOrDefault(p =>
                    p.TMabilityDefs.Contains(TorannMagicDefOf.TM_EyeOfTheStorm));
                if (eye != null) eye.learned = true;
            }
            // Preserve the explicit legacy Fold Reality grant on its existing native owner.
            if (comp.spell_FoldReality && TorannMagicDefOf.TM_FoldReality != null)
            {
                var fold = comp.MagicData.MagicPowersA.FirstOrDefault(p =>
                    p.TMabilityDefs.Contains(TorannMagicDefOf.TM_FoldReality));
                if (fold != null) fold.learned = true;
            }
            // An explicit saved ultimate flag owns only this existing native class power.
            if (comp.spell_HolyWrath && TorannMagicDefOf.TM_HolyWrath != null)
            {
                var owner = comp.MagicData.MagicPowersP.FirstOrDefault(p =>
                    p.TMabilityDefs.Contains(TorannMagicDefOf.TM_HolyWrath));
                if (owner != null) owner.learned = true;
            }
            // An explicit saved ultimate flag owns only this existing native class power.
            if (comp.spell_SummonPoppi && TorannMagicDefOf.TM_SummonPoppi != null)
            {
                var owner = comp.MagicData.MagicPowersS.FirstOrDefault(p =>
                    p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SummonPoppi));
                if (owner != null) owner.learned = true;
            }
            if (comp.spell_RegrowLimb && TorannMagicDefOf.TM_RegrowLimb != null)
            {
                var owner = comp.MagicData.MagicPowersD.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_RegrowLimb));
                if (owner != null) owner.learned = true;
            }
            if (comp.spell_OrbitalStrike && TorannMagicDefOf.TM_OrbitalStrike != null)
            {
                var owner = comp.MagicData.MagicPowersT.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_OrbitalStrike));
                if (owner != null) owner.learned = true;
            }
            if (comp.spell_Meteor && TorannMagicDefOf.TM_Meteor != null)
            {
                var owner = comp.MagicData.MagicPowersG.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Meteor));
                if (owner != null) owner.learned = true;
            }
            if (comp.spell_BloodMoon && TorannMagicDefOf.TM_BloodMoon != null)
            {
                var owner = comp.MagicData.MagicPowersBM.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_BloodMoon));
                if (owner != null) owner.learned = true;
            }
            if (comp.spell_BattleHymn && TorannMagicDefOf.TM_BattleHymn != null)
            {
                var owner = comp.MagicData.MagicPowersB.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_BattleHymn));
                if (owner != null) owner.learned = true;
            }
            if (comp.spell_PsychicShock)
            {
                var owner = comp.MagicData.MagicPowersWD.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_PsychicShock));
                if (owner != null) owner.learned = true;
            }
            if (comp.spell_Scorn)
            {
                var owner = comp.MagicData.MagicPowersSD.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Scorn));
                if (owner != null) owner.learned = true;
            }
            // Native TechnoWeapon grants this standalone child outside XML. Persist
            // only its existing native owner; a copied weapon cannot acquire it.
            if (ClassIdentity.IsTechnomancer(comp.Pawn)
                && ClassIdentity.Profile("rwom.technomancer")?.domain == DisciplineDomain.Magic
                && comp.MagicData.MagicPowersT.Any(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_TechnoWeapon)))
            {
                var child = comp.MagicData.MagicPowersStandalone.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_NanoStimulant));
                if (child != null) child.learned = true;
            }
            var seen = new HashSet<AbilityUser.AbilityDef>();
            foreach (var power in ClassPowers(comp.Pawn, comp.MagicData).Concat(comp.MagicData.AllMagicPowers))
            {
                if (ClassIdentity.TryGetDemonBranch(comp.Pawn, out _) && power?.TMabilityDefs?.Any(a => a?.defName == "TM_SoulBond" || a?.defName == "TM_ShadowBolt" || a?.defName == "TM_Dominate") == true
                    && !ReferenceEquals(TM_ClassState.PreferredMagicOwner(comp, power.TMabilityDefs[0]), power)) continue;
                if (power?.learned != true || !(power.abilityDef is TMAbilityDef ability) || !seen.Add(ability)) continue;
                if (ability == TorannMagicDefOf.TM_LichForm && ClassIdentity.IsLich(comp.Pawn)) continue;
                // Ranked variants commonly opt out of initial class grants. Their
                // base definition owns initialization; retain the saved cast rank.
                if ((power.TMabilityDefs.FirstOrDefault() as TMAbilityDef)?.shouldInitialize == true)
                    TM_ClassState.AddRuntimeAbility(comp, ability);
                else seen.Remove(ability);
                if (ability.childAbilities != null)
                    foreach (var child in ability.childAbilities)
                        if (child != null && child.shouldInitialize) TM_ClassState.AddRuntimeAbility(comp, child);
            }
            // Some shared scrolls persist ownership only in a spell flag. Restore
            // those explicit legacy grants without rerolling class starts or ranks.
            foreach (var ability in FlagAbilities(comp))
            {
                if (ability == null) continue;
                var owner = comp.MagicData.AllMagicPowers.FirstOrDefault(p => p.learned && p.TMabilityDefs.Contains(ability));
                var current = owner?.abilityDef as TMAbilityDef ?? ability;
                if (seen.Add(current)) TM_ClassState.AddRuntimeAbility(comp, current);
            }
            foreach (var cooldown in sharedCooldowns)
            {
                var runtime = comp.AbilityData.Powers.FirstOrDefault(a => a?.Def == cooldown.Key);
                if (runtime != null) runtime.CooldownTicksLeft = System.Math.Max(runtime.CooldownTicksLeft, cooldown.Value);
            }
            BrightmageLifecycle.Restore(comp);
            GolemancerLifecycle.Restore(comp);
            EmpathLifecycle.Restore(comp);
            ShamanLifecycle.Restore(comp);
            return true;
        }
    }
}





