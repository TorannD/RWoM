using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using AbilityUserAI;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using TorannMagic.Golems;
using TorannMagic.TMDefs;
using TorannMagic.Utils;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TorannMagic
{
    public partial class TorannMagicMod
    {
        [HarmonyPatch(typeof(JobGiver_Mate), "TryGiveJob", null)]
        public class JobGiver_Mate_Patch
        {
            public static void Postfix(Pawn pawn, ref Job __result)
            {
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD"), false) || pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")))
                {
                    __result = null;
                }
            }
        }

        [HarmonyPatch(typeof(MapParent), "CheckRemoveMapNow", null)]
        public class CheckRemoveMapNow_Patch
        {
            public static bool Prefix()
            {
                return !ModOptions.Constants.GetPawnInFlight();
            }
        }

        [HarmonyPatch(typeof(CompMilkable), "CompInspectStringExtra", null)]
        public class CompMilkable_Patch
        {
            public static void Postfix(CompMilkable __instance, ref string __result)
            {
                if (__instance.parent.def.defName == "Poppi")
                {
                    __result = "Poppi_fuelGrowth".Translate() + ": " + __instance.Fullness.ToStringPercent();
                }
            }
        }

        [HarmonyPatch(typeof(JobGiver_Kidnap), "TryGiveJob", null)]
        public class JobGiver_Kidnap_Patch
        {
            public static void Postfix(Pawn pawn, ref Job __result)
            {
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")))
                {
                    __result = null;
                }
            }
        }

        [HarmonyPatch(typeof(ITab_Pawn_Gear), "DrawThingRow", null)]
        public class ITab_Pawn_Gear_Patch
        {
            public static Rect GetRowRect(Rect inRect, int row)
            {
                float y = 20f * (float)row;
                Rect result = new Rect(inRect.x, y, inRect.width, 18f);
                return result;
            }

            public static void Postfix(ref float y, float width, Thing thing)
            {
                bool valid = !thing.DestroyedOrNull() && thing.TryGetQuality(out QualityCategory qc);
                if (valid)
                {
                    if (((thing.def.thingClass != null && thing.def.thingClass.ToString() == "RimWorld.Apparel") || thing.TryGetComp<CompEquippable>() != null) && thing.TryGetComp<Enchantment.CompEnchantedItem>() != null)
                    {
                        if (thing.TryGetComp<Enchantment.CompEnchantedItem>().HasEnchantment)
                        {
                            Text.Font = GameFont.Tiny;
                            string str1 = "-- Enchanted (";
                            string str2 = "Enchanted \n\n";

                            Enchantment.CompEnchantedItem enchantedItem = thing.TryGetComp<Enchantment.CompEnchantedItem>();
                            if (enchantedItem.maxMP != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.maxMPTier);
                                str1 += "M";
                                str2 += enchantedItem.MaxMPLabel + "\n";
                            }
                            if (enchantedItem.mpCost != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.mpCostTier);
                                str1 += "C";
                                str2 += enchantedItem.MPCostLabel + "\n";
                            }
                            if (enchantedItem.mpRegenRate != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.mpRegenRateTier);
                                str1 += "R";
                                str2 += enchantedItem.MPRegenRateLabel + "\n";
                            }
                            if (enchantedItem.coolDown != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.coolDownTier);
                                str1 += "D";
                                str2 += enchantedItem.CoolDownLabel + "\n";
                            }
                            if (enchantedItem.xpGain != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.xpGainTier);
                                str1 += "G";
                                str2 += enchantedItem.XPGainLabel + "\n";
                            }
                            if (enchantedItem.arcaneRes != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.arcaneResTier);
                                str1 += "X";
                                str2 += enchantedItem.ArcaneResLabel + "\n";
                            }
                            if (enchantedItem.arcaneDmg != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.arcaneDmgTier);
                                str1 += "Z";
                                str2 += enchantedItem.ArcaneDmgLabel + "\n";
                            }
                            if (enchantedItem.arcaneSpectre != false)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.skillTier);
                                str1 += "*S";
                                str2 += enchantedItem.ArcaneSpectreLabel + "\n";
                            }
                            if (enchantedItem.phantomShift != false)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.skillTier);
                                str1 += "*P";
                                str2 += enchantedItem.PhantomShiftLabel + "\n";
                            }
                            str1 += ")";
                            y -= 6f;
                            Rect rect = new Rect(48f, y, width - 36f, 28f);
                            Widgets.Label(rect, str1);

                            TooltipHandler.TipRegion(rect, () => string.Concat(new string[]
                            {
                            str2,
                            }), 398512);

                            y += 28f;
                            GUI.color = Color.white;
                            Text.Font = GameFont.Small;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ITab_Pawn_Gear), "TryDrawOverallArmor", null)]
        public class ITab_Pawn_GearFillTab_Patch
        {
            //public static FieldInfo pawn = typeof(ITab_Pawn_Gear).GetField("SelPawnForGear", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

            public static void Postfix(ITab_Pawn_Gear __instance, ref float curY, float width, StatDef stat, string label)
            {
                if (stat.defName == "ArmorRating_Heat")
                {
                    //Traverse traverse = Traverse.Create(__instance);
                    Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
                    if (!pawn.DestroyedOrNull() && !pawn.Dead)
                    {
                        stat = StatDef.Named("ArmorRating_Alignment");
                        label = "TM_ArmorHarmony".Translate();
                        float num = 0f;
                        float num2 = Mathf.Clamp01(pawn.GetStatValue(stat, true) / 2f);
                        List<BodyPartRecord> allParts = pawn.RaceProps.body.AllParts;
                        List<Apparel> list = (pawn.apparel == null) ? null : pawn.apparel.WornApparel;
                        for (int i = 0; i < allParts.Count; i++)
                        {
                            float num3 = 1f - num2;
                            if (list != null)
                            {
                                for (int j = 0; j < list.Count; j++)
                                {
                                    if (list[j].def.apparel.CoversBodyPart(allParts[i]))
                                    {
                                        float num4 = Mathf.Clamp01(list[j].GetStatValue(stat, true) / 2f);
                                        num3 *= 1f - num4;
                                    }
                                }
                            }
                            num += allParts[i].coverageAbs * (1f - num3);
                        }
                        num = Mathf.Clamp(num * 2f, 0f, 2f);
                        Rect rect = new Rect(0f, curY, width, 100f);
                        Widgets.Label(rect, label.Truncate(120f, null));
                        rect.xMin += 120f;
                        Widgets.Label(rect, num.ToStringPercent());
                        curY += 22f;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(NegativeInteractionUtility), "NegativeInteractionChanceFactor", null)]
        public class NegativeInteractionChanceFactor_Patch
        {
            public static void Postfix(Pawn initiator, Pawn recipient, ref float __result)
            {
                CompAbilityUserMagic comp = initiator.GetCompAbilityUserMagic();
                if (initiator.story?.traits != null)
                {
                    if ((initiator.story.traits.HasTrait(TorannMagicDefOf.TM_Bard) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_Entertain, comp, null)))
                    {
                        MagicPowerSkill ver = comp.MagicData.MagicPowerSkill_Entertain.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Entertain_ver");
                        __result = __result / (1 + ver.level);

                    }
                    if (initiator.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper))
                    {
                        __result *= 1.2f;
                    }
                }
                if (initiator.mindState != null && initiator.mindState.mentalStateHandler != null && initiator.Inspired && initiator.InspirationDef.defName == "Outgoing")
                {
                    __result = __result * .5f;
                }
            }
        }

        [HarmonyPatch(typeof(InspirationHandler), "TryStartInspiration", null)]
        public class InspirationHandler_Patch
        {
            public static bool Prefix(InspirationHandler __instance, ref bool __result)
            {
                if (__instance.pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")) || __instance.pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD")))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        private static readonly SimpleCache<string, TraitIconMap.TraitIconValue> ColonistBarColonistDrawerCache = new Utils.SimpleCache<string, TraitIconMap.TraitIconValue>(5);

        [HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawIcons", null)]
        public class ColonistBarColonistDrawer_Patch
        {
            public static void Postfix(ColonistBarColonistDrawer __instance, ref Rect rect, Pawn colonist)
            {
                if (colonist.Dead) return;

                
                var traitIconValue = ColonistBarColonistDrawerCache.GetOrCreate(
                    colonist.ThingID,
                    () =>
                    {
                        if (colonist.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD))
                        {
                            return new TraitIconMap.TraitIconValue(TM_RenderQueue.necroMarkMat, TM_MatPool.Icon_Undead, "TM_Icon_Undead");                        
                        }
                        // Early exit condition
                        if (!ModOptions.Settings.Instance.showClassIconOnColonistBar || colonist.story == null)
                        {
                            return null;
                        }
                        //Custom Classes loaded at startup                        

                        for (int i = 0; i < colonist.story.traits.allTraits.Count; i++)
                        {
                            TraitDef trait = colonist.story.traits.allTraits[i].def;
                            if (TraitIconMap.ContainsKey(trait))
                            {
                                return TraitIconMap.Get(trait);                                
                            }
                        }
                        return null;
                    }, 5);

                // Skip rendering if there's nothing to render
                if (traitIconValue == null) return;

                // Otherwise render away!
                float num = 20f * Find.ColonistBar.Scale * ModOptions.Settings.Instance.classIconSize;
                Vector2 vector = new Vector2(rect.x + 1f, rect.yMin + 1f);
                rect = new Rect(vector.x, vector.y, num, num);
                GUI.DrawTexture(rect, traitIconValue.IconTexture);
                TooltipHandler.TipRegion(rect, traitIconValue.IconType.Translate());
                vector.x += num;
            }
        }

        [HarmonyPatch(typeof(Pawn_InteractionsTracker), "InteractionsTrackerTick", null)]
        public class InteractionsTrackerTick_Patch
        {
            public static void Postfix(Pawn_InteractionsTracker __instance, Pawn ___pawn, ref bool ___wantsRandomInteract, int ___lastInteractionTime)
            {
                if (Find.TickManager.TicksGame % 1200 == 0)
                {
                    if (___pawn.IsColonist && !___pawn.Downed && !___pawn.Dead && ___pawn.RaceProps.Humanlike)
                    {
                        CompAbilityUserMagic comp = ___pawn.GetCompAbilityUserMagic();

                        if (comp != null && comp.IsMagicUser && (comp.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_Entertain, comp, null)))
                        {
                            MagicPowerSkill pwr = comp.MagicData.MagicPowerSkill_Entertain.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Entertain_pwr");
                            if ((Find.TickManager.TicksGame - ___lastInteractionTime) > (3000 - (450 * pwr.level)))
                            {
                                ___wantsRandomInteract = true;
                            }
                        }
                        if (___pawn.Inspired && ___pawn.InspirationDef.defName == "ID_Outgoing")
                        {
                            if ((Find.TickManager.TicksGame - ___lastInteractionTime) > (1800))
                            {
                                ___wantsRandomInteract = true;
                            }
                        }
                        if (___pawn.health != null && ___pawn.health.hediffSet != null && ___pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TaskMasterHD))
                        {
                            if ((Find.TickManager.TicksGame - ___lastInteractionTime) < 30000)
                            {
                                ___wantsRandomInteract = false;
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState", null)]
        public class MentalStateHandler_Patch
        {
            //public static FieldInfo pawn = typeof(MentalStateHandler).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

            public static bool Prefix(MentalStateHandler __instance, MentalStateDef stateDef, Pawn otherPawn, Pawn ___pawn, ref bool __result)
            {
                if (___pawn.RaceProps.Humanlike && (TM_Calc.IsUndeadNotVamp(___pawn)))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPriority(100)]
        [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed", null)]
        public static class Pawn_NeedsTracker_Patch
        {
            //public static FieldInfo pawn = typeof(Pawn_NeedsTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            public static bool Prefix(Pawn_NeedsTracker __instance, NeedDef nd, Pawn ___pawn, ref bool __result)
            {
                //Traverse traverse = Traverse.Create(__instance);
                Pawn pawn = ___pawn;
                if (pawn != null)
                {
                    if (pawn.def == TorannMagicDefOf.TM_SpiritTD)
                    {
                        if (nd == TorannMagicDefOf.TM_SpiritND || nd == TorannMagicDefOf.TM_Mana)
                        {
                            return true;
                        }
                        return false;
                    }
                    if (nd.defName == "ROMV_Blood" && (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")) || pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")) || pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD"))))
                    {
                        bool hasVampHediff = pawn.health.hediffSet.HasHediff(HediffDef.Named("ROM_Vampirism"));
                        if (hasVampHediff)
                        {
                            return true;
                        }
                        __result = false;
                        return false;
                    }
                    if ((nd.defName == "TM_Mana" || nd.defName == "TM_Stamina") && pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")))
                    {
                        __result = false;
                        return false;
                    }
                    if (nd == TorannMagicDefOf.TM_Travel)// && pawn.story != null && pawn.story.traits != null)
                    {
                        __result = false;
                        return false;
                    }
                    if (TM_GolemUtility.GolemPawns.Contains(pawn.def))
                    {
                        foreach (NeedDef n in TM_GolemUtility.GetGolemDefFromThing(___pawn).needs)
                        {
                            if (n != null && n == nd)
                            {
                                __result = true;
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(PawnGenerator), "GenerateTraits", null)]
        public static class PawnGenerator_Patch
        {
            private static void Postfix(Pawn pawn)
            {
                if (pawn.IsShambler) goto TraitEnd;
                if (pawn.IsGhoul) goto TraitEnd;

                List<TraitDef> allTraits = DefDatabase<TraitDef>.AllDefsListForReading;
                List<Trait> pawnTraits = pawn.story.traits.allTraits;
                
                bool flag = pawnTraits != null;
                bool anyFightersEnabled = false;
                bool anyMagesEnabled = false;
                int baseCount = 6;
                int mageCount = 18;
                int fighterCount = 11;
                int supportingFighterCount = 2;
                int supportingMageCount = 5;
                float fighterFactor = 1f;
                float mageFactor = 1f;
                if (pawn.Faction != null)
                {
                    if (ModOptions.Settings.Instance.FactionFighterSettings.ContainsKey(pawn.Faction.def.defName))
                    {
                        fighterFactor = ModOptions.Settings.Instance.FactionFighterSettings[pawn.Faction.def.defName];
                    }
                    if (ModOptions.Settings.Instance.FactionMageSettings.ContainsKey(pawn.Faction.def.defName))
                    {
                        mageFactor = ModOptions.Settings.Instance.FactionMageSettings[pawn.Faction.def.defName];
                    }
                }
                if (TM_ClassUtility.CustomFighterClasses == null)
                {
                    TM_ClassUtility.LoadCustomClasses();
                }
                if (TM_ClassUtility.CustomMageClasses == null)
                {
                    TM_ClassUtility.LoadCustomClasses();
                }

                List<TM_CustomClass> customFighters = TM_ClassUtility.CustomFighterClasses;
                List<TM_CustomClass> customMages = TM_ClassUtility.CustomMageClasses;              

                mageCount += customMages.Count;
                fighterCount += customFighters.Count;
                if (customFighters.Count > 0 || ModOptions.Settings.Instance.Gladiator || ModOptions.Settings.Instance.Bladedancer || ModOptions.Settings.Instance.Ranger || ModOptions.Settings.Instance.Sniper || ModOptions.Settings.Instance.Faceless || ModOptions.Settings.Instance.DeathKnight || ModOptions.Settings.Instance.Psionic || ModOptions.Settings.Instance.Monk || ModOptions.Settings.Instance.Wayfarer || ModOptions.Settings.Instance.Commander || ModOptions.Settings.Instance.SuperSoldier)
                {
                    anyFightersEnabled = true;
                }
                if (customMages.Count > 0 || ModOptions.Settings.Instance.Arcanist || ModOptions.Settings.Instance.FireMage || ModOptions.Settings.Instance.IceMage || ModOptions.Settings.Instance.LitMage || ModOptions.Settings.Instance.Druid || ModOptions.Settings.Instance.Paladin || ModOptions.Settings.Instance.Summoner || ModOptions.Settings.Instance.Priest || ModOptions.Settings.Instance.Necromancer || ModOptions.Settings.Instance.Bard || ModOptions.Settings.Instance.Demonkin || ModOptions.Settings.Instance.Geomancer || ModOptions.Settings.Instance.Technomancer || ModOptions.Settings.Instance.BloodMage || ModOptions.Settings.Instance.Enchanter || ModOptions.Settings.Instance.Chronomancer || ModOptions.Settings.Instance.Wanderer || ModOptions.Settings.Instance.ChaosMage)
                {
                    anyMagesEnabled = true;
                }
                if (flag)
                {
                    float baseMageChance = mageFactor * ModOptions.Settings.Instance.baseMageChance * baseCount;
                    float baseFighterChance = fighterFactor * ModOptions.Settings.Instance.baseFighterChance * baseCount;
                    float advMageChance = mageCount * ModOptions.Settings.Instance.advMageChance * mageFactor;
                    float advFighterChance = fighterCount * ModOptions.Settings.Instance.advFighterChance * fighterFactor;

                    if (false) //ModCheck.Validate.AlienHumanoidRaces.IsInitialized())
                    {
                        if (Rand.Chance(((baseFighterChance) + (baseMageChance) + (advFighterChance) + (advMageChance)) / (allTraits.Count)))
                        {
                            if (pawnTraits.Count > 0)
                            {
                                pawnTraits.Remove(pawnTraits[pawnTraits.Count - 1]);
                            }
                            float rnd = Rand.Range(0, baseMageChance + baseFighterChance + advMageChance + advFighterChance);
                            if (rnd < (baseMageChance) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Gifted, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Gifted) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Gifted)))
                            {
                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Gifted, 0, false));
                            }
                            else if (rnd >= baseMageChance && rnd < (baseMageChance + baseFighterChance) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.PhysicalProdigy, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.PhysicalProdigy) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.PhysicalProdigy)))
                            {
                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.PhysicalProdigy, 0, false));
                            }
                            else if (rnd >= (baseMageChance + baseFighterChance) && rnd < (baseMageChance + baseFighterChance + advFighterChance))
                            {
                                if (anyFightersEnabled)
                                {
                                    int rndF = Rand.RangeInclusive(1, fighterCount);
                                    switch (rndF)
                                    {
                                        case 1:
                                            //Gladiator:;
                                            if (ModOptions.Settings.Instance.Gladiator && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Gladiator, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Gladiator) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Gladiator)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Gladiator, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Sniper;
                                            //}
                                            break;
                                        case 2:
                                            //Sniper:;
                                            if (ModOptions.Settings.Instance.Sniper && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Sniper, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Sniper) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Sniper)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Sniper, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Bladedancer;
                                            //}
                                            break;
                                        case 3:
                                            Bladedancer:;
                                            if (ModOptions.Settings.Instance.Bladedancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Bladedancer, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Bladedancer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Bladedancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Bladedancer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Ranger;
                                            //}
                                            break;
                                        case 4:
                                            Ranger:;
                                            if (ModOptions.Settings.Instance.Ranger && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Ranger, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Ranger) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Ranger)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Ranger, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Faceless;
                                            //}
                                            break;
                                        case 5:
                                            Faceless:;
                                            if (ModOptions.Settings.Instance.Faceless && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Faceless, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Faceless) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Faceless)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Faceless, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Psionic;
                                            //}
                                            break;
                                        case 6:
                                            Psionic:;
                                            if (ModOptions.Settings.Instance.Psionic && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Psionic, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Psionic) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Psionic)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Psionic, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto DeathKnight;
                                            //}
                                            break;
                                        case 7:
                                            DeathKnight:;
                                            if (ModOptions.Settings.Instance.DeathKnight && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.DeathKnight, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.DeathKnight) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.DeathKnight)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.DeathKnight, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Monk;
                                            //}
                                            break;
                                        case 8:
                                            Monk:;
                                            if (ModOptions.Settings.Instance.Monk && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Monk, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Monk) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Monk)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Monk, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Wayfarer;
                                            //}
                                            break;
                                        case 9:
                                            Wayfarer:;
                                            if (ModOptions.Settings.Instance.Wayfarer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Wayfarer, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Wayfarer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Wayfarer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Wayfarer, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Commander;
                                            //}
                                            break;
                                        case 10:
                                            Commander:;
                                            if (ModOptions.Settings.Instance.Commander && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Commander, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Commander) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Commander)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Commander, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto SuperSoldier;
                                            //}
                                            break;
                                        case 11:
                                            SuperSoldier:;
                                            if (ModOptions.Settings.Instance.SuperSoldier && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_SuperSoldier, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_SuperSoldier) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_SuperSoldier)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_SuperSoldier, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Gladiator;
                                            //}
                                            break;
                                        case int val when rndF > 11:
                                            TMDefs.TM_CustomClass cFighter = TM_ClassUtility.GetRandomCustomFighter();
                                            if (!pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(cFighter.classTrait, cFighter.traitDegree)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, cFighter.classTrait) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(cFighter.classTrait)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(cFighter.classTrait, cFighter.traitDegree, false));
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    goto TraitEnd;
                                }
                            }
                            else
                            {
                                if (anyMagesEnabled)
                                {
                                    int rndM = Rand.RangeInclusive(1, (mageCount + 1));
                                    switch (rndM)
                                    {
                                        case 1:
                                            FireMage:;
                                            if (ModOptions.Settings.Instance.FireMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.InnerFire, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.InnerFire) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.InnerFire)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.InnerFire, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto IceMage;
                                            //}
                                            break;
                                        case 2:
                                            IceMage:;
                                            if (ModOptions.Settings.Instance.IceMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.HeartOfFrost, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.HeartOfFrost) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.HeartOfFrost)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.HeartOfFrost, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto LitMage;
                                            //}
                                            break;
                                        case 3:
                                            LitMage:;
                                            if (ModOptions.Settings.Instance.LitMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.StormBorn, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.StormBorn) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.StormBorn)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.StormBorn, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Arcanist;
                                            //}
                                            break;
                                        case 4:
                                            Arcanist:;
                                            if (ModOptions.Settings.Instance.Arcanist && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Arcanist, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Arcanist) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Arcanist)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Arcanist, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Druid;
                                            //}
                                            break;
                                        case 5:
                                            Druid:;
                                            if (ModOptions.Settings.Instance.Druid && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Druid, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Druid) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Druid)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Druid, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Paladin;
                                            //}
                                            break;
                                        case 6:
                                            Paladin:;
                                            if (ModOptions.Settings.Instance.Paladin && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Paladin, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Paladin) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Paladin)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Paladin, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Summoner;
                                            //}
                                            break;
                                        case 7:
                                            Summoner:;
                                            if (ModOptions.Settings.Instance.Summoner && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Summoner, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Summoner) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Summoner)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Summoner, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Necromancer;
                                            //}
                                            break;
                                        case 8:
                                            Necromancer:;
                                            if (ModOptions.Settings.Instance.Necromancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Necromancer, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Necromancer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Necromancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Necromancer, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Priest;
                                            //}
                                            break;
                                        case 9:
                                            Priest:;
                                            if (ModOptions.Settings.Instance.Priest && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Priest, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Priest) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Priest)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Priest, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Demonkin;
                                            //}
                                            break;
                                        case 10:
                                            Demonkin:;
                                            if (ModOptions.Settings.Instance.Demonkin && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Warlock, 4)) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Succubus, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Succubus) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Warlock) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Succubus)))
                                            {
                                                if (pawn.gender != Gender.Female)
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 4, false));
                                                }
                                                else
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 4, false));
                                                }
                                            }
                                            //else
                                            //{
                                            //    goto Bard;
                                            //}
                                            break;
                                        case 11:
                                            if (ModOptions.Settings.Instance.Demonkin && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Warlock, 4)) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Succubus, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Succubus) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Warlock) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Warlock)))
                                            {
                                                if (pawn.gender != Gender.Male)
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 4, false));
                                                }
                                                else
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 4, false));
                                                }
                                            }
                                            //else
                                            //{
                                            //    goto Bard;
                                            //}
                                            break;
                                        case 12:
                                            Bard:;
                                            if (ModOptions.Settings.Instance.Bard && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Bard, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Bard) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Bard)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Bard, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Geomancer;
                                            //}
                                            break;
                                        case 13:
                                            Geomancer:;
                                            if (ModOptions.Settings.Instance.Geomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Geomancer, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Geomancer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Geomancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Geomancer, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Technomancer;
                                            //}
                                            break;
                                        case 14:
                                            Technomancer:;
                                            if (ModOptions.Settings.Instance.Technomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Technomancer, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Technomancer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Technomancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Technomancer, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto BloodMage;
                                            //}
                                            break;
                                        case 15:
                                            BloodMage:;
                                            if (ModOptions.Settings.Instance.Technomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.BloodMage, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.BloodMage) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.BloodMage)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.BloodMage, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Enchanter;
                                            //}
                                            break;
                                        case 16:
                                            Enchanter:;
                                            if (ModOptions.Settings.Instance.Technomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Enchanter, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Enchanter) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Enchanter)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Enchanter, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Chronomancer;
                                            //}
                                            break;
                                        case 17:
                                            Chronomancer:;
                                            if (ModOptions.Settings.Instance.Chronomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Chronomancer, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Chronomancer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Chronomancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Chronomancer, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Wanderer;
                                            //}
                                            break;
                                        case 18:
                                            Wanderer:;
                                            if (ModOptions.Settings.Instance.Wanderer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Wanderer, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Wanderer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Wanderer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Wanderer, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto ChaosMage;
                                            //}
                                            break;
                                        case 19:
                                            ChaosMage:;
                                            if (ModOptions.Settings.Instance.ChaosMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.ChaosMage, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.ChaosMage) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.ChaosMage)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.ChaosMage, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto FireMage;
                                            //}
                                            break;
                                        case int val when rndM > 19:
                                            TMDefs.TM_CustomClass cMage = customMages.RandomElement();
                                            if (!pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(cMage.classTrait, cMage.traitDegree)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, cMage.classTrait) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(cMage.classTrait)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(cMage.classTrait, cMage.traitDegree, false));
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    goto TraitEnd;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Rand.Chance((baseMageChance + baseFighterChance + advMageChance + advFighterChance) / (allTraits.Count)))
                        {

                            if (pawnTraits.Count > 2)
                            {
                                pawnTraits.Remove(pawnTraits[pawnTraits.Count - 1]);
                            }
                            float rnd = Rand.Range(0, baseMageChance + baseFighterChance + advFighterChance + advMageChance);
                            if (rnd < (baseMageChance) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Gifted, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Gifted)))
                            {
                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Gifted, 0, false));
                            }
                            else if (rnd >= baseMageChance && rnd < (baseMageChance + baseFighterChance) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.PhysicalProdigy, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.PhysicalProdigy)))
                            {
                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.PhysicalProdigy, 0, false));
                            }
                            else if (rnd >= (baseMageChance + baseFighterChance) && rnd < (baseMageChance + baseFighterChance + advFighterChance))
                            {
                                if (anyFightersEnabled && pawn.ageTracker?.AgeBiologicalYears >= 6)
                                {
                                    int rndF = Rand.RangeInclusive(1, fighterCount);
                                    switch (rndF)
                                    {
                                        case 1:
                                            //Gladiator:;
                                            if (ModOptions.Settings.Instance.Gladiator && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Gladiator, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Gladiator)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Gladiator, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Sniper;
                                            //}
                                            break;
                                        case 2:
                                            //Sniper:;
                                            if (ModOptions.Settings.Instance.Sniper && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Sniper, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Sniper)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Sniper, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Bladedancer;
                                            //}
                                            break;
                                        case 3:
                                            Bladedancer:;
                                            if (ModOptions.Settings.Instance.Bladedancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Bladedancer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Bladedancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Bladedancer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Ranger;
                                            //}
                                            break;
                                        case 4:
                                            Ranger:;
                                            if (ModOptions.Settings.Instance.Ranger && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Ranger, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Ranger)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Ranger, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Faceless;
                                            //}
                                            break;
                                        case 5:
                                            Faceless:;
                                            if (ModOptions.Settings.Instance.Faceless && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Faceless, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Faceless)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Faceless, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Psionic;
                                            //}
                                            break;
                                        case 6:
                                            Psionic:;
                                            if (ModOptions.Settings.Instance.Psionic && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Psionic, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Psionic)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Psionic, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto DeathKnight;
                                            //}
                                            break;
                                        case 7:
                                            DeathKnight:;
                                            if (ModOptions.Settings.Instance.DeathKnight && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.DeathKnight, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.DeathKnight)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.DeathKnight, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Monk;
                                            //}
                                            break;
                                        case 8:
                                            Monk:;
                                            if (ModOptions.Settings.Instance.Monk && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Monk, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Monk)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Monk, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Wayfarer;
                                            //}
                                            break;
                                        case 9:
                                            Wayfarer:;
                                            if (ModOptions.Settings.Instance.Wayfarer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Wayfarer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Wayfarer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Wayfarer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Commander;
                                            //}
                                            break;
                                        case 10:
                                            Commander:;
                                            if (ModOptions.Settings.Instance.Commander && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Commander, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Commander)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Commander, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto SuperSoldier;
                                            //}
                                            break;
                                        case 11:
                                            SuperSoldier:;
                                            if (ModOptions.Settings.Instance.SuperSoldier && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_SuperSoldier, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_SuperSoldier)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_SuperSoldier, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Gladiator;
                                            //}
                                            break;
                                        case int val when rndF > 11:
                                            TMDefs.TM_CustomClass cFighter = customFighters.RandomElement();
                                            if (!pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(cFighter.classTrait, cFighter.traitDegree)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(cFighter.classTrait)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(cFighter.classTrait, cFighter.traitDegree, false));
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    goto TraitEnd;
                                }
                            }
                            else
                            {
                                if (anyMagesEnabled && pawn.ageTracker?.AgeBiologicalYears >= 6)
                                {
                                    int rndM = Rand.RangeInclusive(1, (mageCount + 1));
                                    switch (rndM)
                                    {
                                        case 1:
                                            FireMage:;
                                            if (ModOptions.Settings.Instance.FireMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.InnerFire, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.InnerFire)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.InnerFire, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto IceMage;
                                            //}
                                            break;
                                        case 2:
                                            IceMage:;
                                            if (ModOptions.Settings.Instance.IceMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.HeartOfFrost, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.HeartOfFrost)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.HeartOfFrost, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto LitMage;
                                            //}
                                            break;
                                        case 3:
                                            LitMage:;
                                            if (ModOptions.Settings.Instance.LitMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.StormBorn, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.StormBorn)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.StormBorn, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Arcanist;
                                            //}
                                            break;
                                        case 4:
                                            Arcanist:;
                                            if (ModOptions.Settings.Instance.Arcanist && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Arcanist, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Arcanist)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Arcanist, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Druid;
                                            //}
                                            break;
                                        case 5:
                                            Druid:;
                                            if (ModOptions.Settings.Instance.Druid && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Druid, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Druid)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Druid, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Paladin;
                                            //}
                                            break;
                                        case 6:
                                            Paladin:;
                                            if (ModOptions.Settings.Instance.Paladin && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Paladin, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Paladin)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Paladin, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Summoner;
                                            //}
                                            break;
                                        case 7:
                                            Summoner:;
                                            if (ModOptions.Settings.Instance.Summoner && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Summoner, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Summoner)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Summoner, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Necromancer;
                                            //}
                                            break;
                                        case 8:
                                            Necromancer:;
                                            if (ModOptions.Settings.Instance.Necromancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Necromancer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Necromancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Necromancer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Priest;
                                            //}
                                            break;
                                        case 9:
                                            Priest:;
                                            if (ModOptions.Settings.Instance.Priest && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Priest, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Priest)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Priest, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Demonkin;
                                            //}
                                            break;
                                        case 10:
                                            Demonkin:;
                                            if (ModOptions.Settings.Instance.Demonkin && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Warlock, 0)) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Succubus, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Succubus)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Warlock)))
                                            {
                                                if (pawn.gender != Gender.Female)
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 0, false));
                                                }
                                                else
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 0, false));
                                                }
                                            }
                                            //else
                                            //{
                                            //    goto Bard;
                                            //}
                                            break;
                                        case 11:
                                            if (ModOptions.Settings.Instance.Demonkin && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Warlock, 0)) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Succubus, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Succubus)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Warlock)))
                                            {
                                                if (pawn.gender != Gender.Male)
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 0, false));
                                                }
                                                else
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 0, false));
                                                }
                                            }
                                            //else
                                            //{
                                            //    goto Bard;
                                            //}
                                            break;
                                        case 12:
                                            Bard:;
                                            if (ModOptions.Settings.Instance.Bard && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Bard, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Bard)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Bard, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Geomancer;
                                            //}
                                            break;
                                        case 13:
                                            Geomancer:;
                                            if (ModOptions.Settings.Instance.Geomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Geomancer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Geomancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Geomancer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Technomancer;
                                            //}
                                            break;
                                        case 14:
                                            Technomancer:;
                                            if (ModOptions.Settings.Instance.Technomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Technomancer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Technomancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Technomancer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto BloodMage;
                                            //}
                                            break;
                                        case 15:
                                            BloodMage:;
                                            if (ModOptions.Settings.Instance.Technomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.BloodMage, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.BloodMage)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.BloodMage, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Enchanter;
                                            //}
                                            break;
                                        case 16:
                                            Enchanter:;
                                            if (ModOptions.Settings.Instance.Technomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Enchanter, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Enchanter)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Enchanter, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Chronomancer;
                                            //}
                                            break;
                                        case 17:
                                            Chronomancer:;
                                            if (ModOptions.Settings.Instance.Chronomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Chronomancer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Chronomancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Chronomancer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Wanderer;
                                            //}
                                            break;
                                        case 18:
                                            Wanderer:;
                                            if (ModOptions.Settings.Instance.Wanderer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Wanderer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Wanderer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Wanderer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto ChaosMage;
                                            //}
                                            break;
                                        case 19:
                                            ChaosMage:;
                                            if (ModOptions.Settings.Instance.ChaosMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.ChaosMage, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.ChaosMage)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.ChaosMage, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto FireMage;
                                            //}
                                            break;
                                        case int val when rndM > 19:
                                            TMDefs.TM_CustomClass cMage = customMages.RandomElement();
                                            if (!pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(cMage.classTrait, cMage.traitDegree)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(cMage.classTrait)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(cMage.classTrait, cMage.traitDegree, false));
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    goto TraitEnd;
                                }
                            }
                        }
                    }

                    if (Rand.Chance(ModOptions.Settings.Instance.supportTraitChance))
                    {
                        if (TM_Calc.IsMagicUser(pawn) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted))
                        {
                            int rndS = Rand.RangeInclusive(1, supportingMageCount);
                            switch (rndS)
                            {
                                case 1:
                                    if (ModOptions.Settings.Instance.ArcaneConduit && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_ArcaneConduitTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_ArcaneConduitTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_ArcaneConduitTD, 0, false));
                                    }
                                    break;
                                case 2:
                                    if (ModOptions.Settings.Instance.ManaWell && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_ManaWellTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_ManaWellTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_ManaWellTD, 0, false));
                                    }
                                    break;
                                case 3:
                                    if(ModOptions.Settings.Instance.FaeBlood && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_FaeBloodTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_FaeBloodTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_FaeBloodTD, 0, false));
                                    }
                                    break;
                                case 4:
                                    if (ModOptions.Settings.Instance.Enlightened && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_EnlightenedTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_EnlightenedTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_EnlightenedTD, 0, false));
                                    }
                                    break;
                                case 5:
                                    if (ModOptions.Settings.Instance.Cursed && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_CursedTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_CursedTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_CursedTD, 0, false));
                                    }
                                    break;
                            }
                        }
                        else if (TM_Calc.IsMightUser(pawn) || pawn.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy))
                        {
                            int rndS = Rand.RangeInclusive(1, supportingFighterCount);
                            switch (rndS)
                            {
                                case 1:
                                    if (ModOptions.Settings.Instance.Boundless && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_BoundlessTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_BoundlessTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_BoundlessTD, 0, false));
                                    }
                                    break;
                                case 2:
                                    if (ModOptions.Settings.Instance.GiantsBlood && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_GiantsBloodTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_GiantsBloodTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_GiantsBloodTD, 0, false));
                                    }
                                    break;
                            }
                        }
                    }
                }
                TraitEnd:;
            }
        }

        [HarmonyPatch(typeof(IncidentParmsUtility), "GetDefaultPawnGroupMakerParms", null)]
        public static class GetDefaultPawnGroupMakerParms_Patch
        {
            public static void Postfix(ref PawnGroupMakerParms __result)
            {
                if (__result.faction != null && __result.faction.def.defName == "Seers")
                {
                    __result.points *= 1.65f;
                }

            }
        }

        [HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob", null)]
        public static class JobGiver_GetFood_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result)
            {
                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD) || pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD))
                {
                    __result = null;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(JobGiver_EatRandom), "TryGiveJob", null)]
        public static class JobGiver_EatRandom_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result)
            {
                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD) || pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD))
                {
                    __result = null;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(JobGiver_Haul), "TryGiveJob", null)]
        public static class JobGiver_MinionHaul_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result)
            {
                if (pawn != null && (pawn.def == TorannMagicDefOf.TM_MinionR || pawn.def == TorannMagicDefOf.TM_GreaterMinionR))
                {
                    if (pawn.jobs != null && pawn.CurJob != null && (pawn.CurJob.def == JobDefOf.HaulToCell || pawn.CurJob.def == JobDefOf.HaulToContainer || pawn.CurJob.def == JobDefOf.HaulToTransporter))
                    {
                        __result = null;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(AbilityUser.AbilityDef), "GetJob", null)]
        public static class AbilityDef_Patch
        {
            private static bool Prefix(AbilityUser.AbilityDef __instance, AbilityTargetCategory cat, LocalTargetInfo target, ref Job __result)
            {
                if (__instance.abilityClass.FullName == "TorannMagic.MagicAbility" || __instance.abilityClass.FullName == "TorannMagic.MightAbility" || __instance.defName.Contains("TM_Artifact"))
                {
                    Job result;
                    switch (cat)
                    {
                        case AbilityTargetCategory.TargetSelf:
                            result = new Job(TorannMagicDefOf.TMCastAbilitySelf, target);
                            __result = result;
                            return false;
                        case AbilityTargetCategory.TargetThing:
                            result = new Job(TorannMagicDefOf.TMCastAbilityVerb, target);
                            __result = result;
                            return false;
                        case AbilityTargetCategory.TargetAoE:
                            result = new Job(TorannMagicDefOf.TMCastAbilityVerb, target);
                            __result = result;
                            return false;
                    }
                    result = new Job(TorannMagicDefOf.TMCastAbilityVerb, target);
                    __result = result;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(JobDriver_Mine), "ResetTicksToPickHit", null)]
        public static class JobDriver_Mine_Patch
        {
            private static void Postfix(JobDriver_Mine __instance)
            {
                
                if (Rand.Chance(ModOptions.Settings.Instance.magicyteChance))
                {
                    Thing thing = null;
                    thing = ThingMaker.MakeThing(TorannMagicDefOf.RawMagicyte);
                    thing.stackCount = Rand.Range(9, 25);
                    if (thing != null)
                    {
                        GenPlace.TryPlaceThing(thing, __instance.pawn.Position, __instance.pawn.Map, ThingPlaceMode.Near, null);
                        if(!__instance.pawn.Faction.IsPlayer)
                        {
                            thing.SetForbidden(true, false);
                        }
                    }
                }
            }
        }

        [HarmonyPriority(100)] //Go last
        public static void AddHumanLikeOrders_RestrictEquipmentPatch(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            if (pawn.equipment != null)
            {
                if(pawn.def == TorannMagicDefOf.TM_SpiritTD)
                {
                    List<FloatMenuOption> remop = new List<FloatMenuOption>();
                    remop.Clear();
                    foreach(FloatMenuOption op in opts)
                    {
                        if (op.Label.StartsWith("Pick"))
                        {
                            remop.Add(op);
                        }
                    }
                    foreach(FloatMenuOption op in remop)
                    {
                        opts.Remove(op);
                    }
                }
                ThingWithComps equipment = null;
                List<Thing> thingList = c.GetThingList(pawn.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i].def == TorannMagicDefOf.TM_Artifact_BracersOfThePacifist)
                    {
                        equipment = (ThingWithComps)thingList[i];
                        break;
                    }
                }
                if (equipment != null)
                {
                    string labelShort = equipment.LabelShort;
                    FloatMenuOption nve_option;
                    if (!(pawn.story.traits.HasTrait(TorannMagicDefOf.Priest) || pawn.WorkTagIsDisabled(WorkTags.Violent)))
                    {
                        for (int j = 0; j < opts.Count; j++)
                        {                            
                            if (opts[j].Label.Contains("wear"))
                            {
                                opts.Remove(opts[j]);
                            }
                        }
                        nve_option = new FloatMenuOption("TM_ViolentCannotEquip".Translate(pawn.LabelShort, labelShort), null);
                        opts.Add(nve_option);
                    }
                }
            }
            foreach (FloatMenuOption op in opts)
            {
                if (op.Label.StartsWith("TM_Use"))
                {
                    op.Label = "TM_Use".Translate(op.revalidateClickTarget.Label);
                }
                else if(op.Label.StartsWith("TM_Learn"))
                {
                    op.Label = "TM_Learn".Translate(op.revalidateClickTarget.Label);
                }
                else if (op.Label.StartsWith("TM_Read"))
                {
                    op.Label = "TM_Read".Translate(op.revalidateClickTarget.Label);
                }
                else if (op.Label.StartsWith("TM_Inject"))
                {
                    op.Label = "TM_Inject".Translate(op.revalidateClickTarget.Label);
                }
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders", null)]
        public static class FloatMenuMakerMap_Patch
        {
            public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
            {
                if (pawn == null)
                {
                    return;
                }
                IntVec3 c = IntVec3.FromVector3(clickPos);
                Enchantment.CompEnchant comp = pawn.TryGetComp<Enchantment.CompEnchant>();
                CompAbilityUserMagic pawnComp = pawn.GetCompAbilityUserMagic();
                if (comp != null && pawnComp != null && pawnComp.IsMagicUser && pawn.story != null && pawn.story.traits != null && !pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    if (comp.enchantingContainer == null)
                    {
                        Log.Warning($"Enchanting container is null for {pawn}, initializing.");
                        comp.enchantingContainer = new ThingOwner<Thing>();
                        //comp.enchantingContainer = new ThingOwner<Thing>(comp);
                    }
                    bool emptyGround = true;
                    foreach (Thing current in c.GetThingList(pawn.Map))
                    {
                        if (current != null && current.def.EverHaulable)
                        {
                            emptyGround = false;
                        }
                    }
                    if (emptyGround && !pawn.Drafted) //c.GetThingList(pawn.Map).Count == 0 &&
                    {
                        if (comp.enchantingContainer?.Count > 0)
                        {
                            if (!pawn.CanReach(c, PathEndMode.ClosestTouch, Danger.Deadly, false, false, TraverseMode.ByPawn))
                            {
                                opts.Add(new FloatMenuOption("TM_CannotDrop".Translate(
                                    comp.enchantingContainer[0].Label
                                ) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else
                            {
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TM_DropGem".Translate(
                                comp.enchantingContainer.ContentsString
                                ), delegate
                                {
                                    Job job = new Job(TorannMagicDefOf.JobDriver_RemoveEnchantingGem, c);
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, c, "ReservedBy"));
                            }
                        }

                    }
                    foreach (Thing current in c.GetThingList(pawn.Map))
                    {
                        Thing t = current;
                        if (t != null && t.def.EverHaulable && t.def.defName.ToString().Contains("TM_EStone_"))
                        {
                            if (!pawn.CanReach(t, PathEndMode.ClosestTouch, Danger.Deadly, false, false, TraverseMode.ByPawn))
                            {
                                opts.Add(new FloatMenuOption("CannotPickUp".Translate(
                                t.Label
                                ) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, t, 1))
                            {
                                opts.Add(new FloatMenuOption("CannotPickUp".Translate(
                                t.Label
                                ) + " (" + "TooHeavy".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else// if (item.stackCount == 1)
                            {
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TM_PickupGem".Translate(
                                t.Label
                                ), delegate
                                {
                                    t.SetForbidden(false, false);
                                    Job job = new Job(TorannMagicDefOf.JobDriver_AddEnchantingGem, t);
                                    job.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, t, "ReservedBy"));
                            }
                        }
                        else if ((current.def.IsApparel || current.def.IsWeapon || current.def.IsRangedWeapon) && comp.enchantingContainer?.Count > 0)
                        {
                            if (!pawn.CanReach(t, PathEndMode.ClosestTouch, Danger.Deadly, false, false, TraverseMode.ByPawn))
                            {
                                opts.Add(new FloatMenuOption("TM_CannotReach".Translate(
                                t.Label
                                ) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else if (pawnComp.Mana.CurLevel < .5f)
                            {
                                opts.Add(new FloatMenuOption("TM_NeedManaForEnchant".Translate(
                                pawnComp.Mana.CurLevel.ToString("0.000")
                                ), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else// if (item.stackCount == 1)
                            {
                                if (current.stackCount == 1)
                                {
                                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TM_EnchantItem".Translate(
                                        t.Label
                                    ), delegate
                                    {
                                        t.SetForbidden(true, false);
                                        Job job = new Job(TorannMagicDefOf.JobDriver_EnchantItem, t);
                                        job.count = 1;
                                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                    }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, t, "ReservedBy"));
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddJobGiverWorkOrders", null)]
        public static class FloatMenuMakerMap_MagicJobGiver_Patch
        {
            public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts, bool drafted)
            {
                RimWorld.JobGiver_Work jobGiver_Work = pawn.thinker.TryGetMainTreeThinkNode<RimWorld.JobGiver_Work>();
                if (jobGiver_Work != null)
                {
                    foreach (Thing item in pawn.Map.thingGrid.ThingsAt(clickPos.ToIntVec3()))
                    {
                        if (item is Building && (item.def == TorannMagicDefOf.TableArcaneForge))
                        {
                            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                            if (comp != null && comp.Mana != null && comp.Mana.CurLevel < .5f)
                            {
                                string text = null;
                                Action action = null;
                                text = "TM_InsufficientManaForJob".Translate((comp.Mana.CurLevel * 100).ToString("0.##"));
                                FloatMenuOption menuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, action), pawn, item);
                                if (!opts.Any((FloatMenuOption op) => op.Label == menuOption.Label))
                                {
                                    menuOption.Disabled = true;
                                    opts.Add(menuOption);
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GenGrid), "Standable", null)]
        public class Standable_Patch
        {
            public static bool Prefix(ref IntVec3 c, ref Map map, ref bool __result)
            {
                if (map != null && c != default(IntVec3))
                {
                    return true;
                }
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(AttackTargetFinder), "CanSee", null)]
        public class AttackTargetFinder_CanSee_Patch
        {
            public static bool Prefix(Thing target, ref bool __result)
            {
                if (target is Pawn)
                {
                    Pawn targetPawn = target as Pawn;
                    if (targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_I, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_II, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_III, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_InvisibilityHD, false))
                    {
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(AttackTargetFinder), "CanReach", null)]
        public class AttackTargetFinder_CanReach_Patch
        {
            public static bool Prefix(Thing target, ref bool __result)
            {
                if (target is Pawn)
                {
                    Pawn targetPawn = target as Pawn;
                    if (targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_I, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_II, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_III, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_InvisibilityHD, false))
                    {
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(QualityUtility), "GenerateQualityCreatedByPawn", null)]
        [HarmonyPatch(new Type[]
        {
            typeof(Pawn),
            typeof(SkillDef)
        })]
        public static class ArcaneForge_Quality_Patch
        {
            public static void Postfix(Pawn pawn, SkillDef relevantSkill, ref QualityCategory __result)
            {
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                if (comp != null && comp.IsMagicUser && pawn.story.traits != null && !pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) && comp.ArcaneForging)
                {
                    List<IntVec3> cellList = GenRadial.RadialCellsAround(pawn.Position, 2, true).ToList();
                    bool forgeNearby = false;
                    for (int i = 0; i < cellList.Count; i++)
                    {
                        List<Thing> thingList = cellList[i].GetThingList(pawn.Map);
                        if (thingList != null && thingList.Count > 0)
                        {
                            for (int j = 0; j < thingList.Count; j++)
                            {
                                if (thingList[j] != null && thingList[j] is Building)
                                {
                                    Building bldg = thingList[j] as Building;
                                    if (bldg.def == TorannMagicDefOf.TableArcaneForge)
                                    {
                                        forgeNearby = true;
                                        break;
                                    }
                                }
                            }
                            if (forgeNearby)
                            {
                                break;
                            }
                        }
                    }
                    if (forgeNearby)
                    {
                        int mageLevel = Rand.Range(0, Mathf.RoundToInt(comp.MagicUserLevel / 15));
                        __result = (QualityCategory)Mathf.Min((int)__result + mageLevel, 6);
                        SoundInfo info = SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map, false), MaintenanceType.None);
                        info.pitchFactor = .6f;
                        info.volumeFactor = 1.6f;
                        TorannMagicDefOf.TM_Gong.PlayOneShot(info);
                        cellList.Clear();
                        cellList = GenRadial.RadialCellsAround(pawn.Position, (int)__result, false).ToList<IntVec3>();
                        for (int i = 0; i < cellList.Count; i++)
                        {
                            IntVec3 curCell = cellList[i];
                            Vector3 angle = TM_Calc.GetVector(pawn.Position, curCell);
                            TM_MoteMaker.ThrowArcaneWaveMote(curCell.ToVector3(), pawn.Map, .4f * (curCell - pawn.Position).LengthHorizontal, .1f, .05f, .1f, 0, 3, (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat());
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(DamageWorker), "ExplosionStart", null)]
        public static class ExplosionNoShaker_Patch
        {
            public static bool Prefix(DamageWorker __instance, Explosion explosion)
            {
                if (explosion.damType == TMDamageDefOf.DamageDefOf.TM_BlazingPower ||
                    explosion.damType == TMDamageDefOf.DamageDefOf.TM_BloodBurn ||
                    explosion.damType == TMDamageDefOf.DamageDefOf.TM_HailDD)
                {
                    float radMod = 6f;
                    if (explosion.damType == TMDamageDefOf.DamageDefOf.TM_HailDD)
                    {
                        radMod = 1f;
                    }

                    FleckMaker.Static(explosion.Position, explosion.Map, FleckDefOf.ExplosionFlash,
                        explosion.radius * radMod);
                    if (explosion.damType.explosionSnowMeltAmount < 0)
                    {
                        Projectile_Snowball.AddSnowRadial(explosion.Position, explosion.Map, explosion.radius,
                            explosion.radius);
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        if (explosion.damType == TMDamageDefOf.DamageDefOf.TM_BloodBurn)
                        {
                            if (i < 1)
                            {
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodMist,
                                    explosion.Position.ToVector3Shifted() +
                                    Gen.RandomHorizontalVector(explosion.radius * 0.7f), explosion.Map,
                                    Rand.Range(1f, 1.5f), .2f, 0.6f, 2f, Rand.Range(-30, 30),
                                    Rand.Range(.5f, .7f), Rand.Range(30f, 40f), Rand.Range(0, 360));
                            }
                        }
                        else
                        {
                            FleckMaker.ThrowSmoke(
                                explosion.Position.ToVector3Shifted() +
                                Gen.RandomHorizontalVector(explosion.radius * 0.7f), explosion.Map,
                                explosion.radius * 0.6f);
                        }
                    }

                    if (__instance.def.explosionInteriorMote != null)
                    {
                        int num = Mathf.RoundToInt(3.14159274f * explosion.radius * explosion.radius / 6f);
                        for (int j = 0; j < num; j++)
                        {
                            MoteMaker.ThrowExplosionInteriorMote(
                                explosion.Position.ToVector3Shifted() +
                                Gen.RandomHorizontalVector(explosion.radius * 0.7f), explosion.Map,
                                __instance.def.explosionInteriorMote);
                        }
                    }

                    return false;
                }

                return true;
            }
        }
        
        [HarmonyPatch(typeof(DamageWorker_AddInjury), "Apply", null)]
        public static class DamageWorker_ApplyEnchantmentAction_Patch
        {
            public static void Postfix(DamageWorker_AddInjury __instance, DamageInfo dinfo, Thing thing,
                DamageWorker.DamageResult __result)
            {
                if (dinfo.Instigator != null && dinfo.Instigator is Pawn && dinfo.Amount != 0 &&
                    dinfo.Weapon != null &&
                    dinfo.Weapon.HasComp(typeof(TorannMagic.Enchantment.CompEnchantedItem)))
                {
                    Pawn instigator = dinfo.Instigator as Pawn;
                    if (instigator.equipment != null && instigator.equipment.Primary != null)
                    {
                        ThingWithComps eq = instigator.equipment.Primary;
                        TorannMagic.Enchantment.CompEnchantedItem enchantment =
                            eq.TryGetComp<TorannMagic.Enchantment.CompEnchantedItem>();
                        if (enchantment != null && enchantment.enchantmentAction != null)
                        {
                            if (enchantment.enchantmentAction.type ==
                                Enchantment.EnchantmentActionType.ApplyHediff &&
                                enchantment.enchantmentAction.hediffDef != null)
                            {
                                if (Rand.Chance(enchantment.enchantmentAction.hediffChance))
                                {
                                    if (enchantment.enchantmentAction.onSelf)
                                    {
                                        List<Pawn> plist = TM_Calc.FindPawnsNearTarget(instigator,
                                            Mathf.RoundToInt(enchantment.enchantmentAction.splashRadius),
                                            instigator.Position, enchantment.enchantmentAction.friendlyFire);
                                        if (plist != null && plist.Count > 0)
                                        {
                                            for (int i = 0; i < plist.Count; i++)
                                            {
                                                HealthUtility.AdjustSeverity(plist[i],
                                                    enchantment.enchantmentAction.hediffDef,
                                                    enchantment.enchantmentAction.hediffSeverity);
                                                if (enchantment.enchantmentAction.hediffDurationTicks != 0)
                                                {
                                                    HediffComp_Disappears hdc = plist[i].health.hediffSet
                                                        .GetFirstHediffOfDef(enchantment.enchantmentAction
                                                            .hediffDef).TryGetComp<HediffComp_Disappears>();
                                                    hdc.ticksToDisappear = enchantment.enchantmentAction
                                                        .hediffDurationTicks;
                                                }
                                            }
                                        }

                                        HealthUtility.AdjustSeverity(instigator,
                                            enchantment.enchantmentAction.hediffDef,
                                            enchantment.enchantmentAction.hediffSeverity);
                                        if (enchantment.enchantmentAction.hediffDurationTicks != 0)
                                        {
                                            HediffComp_Disappears hdc = instigator.health.hediffSet
                                                .GetFirstHediffOfDef(enchantment.enchantmentAction.hediffDef)
                                                .TryGetComp<HediffComp_Disappears>();
                                            hdc.ticksToDisappear = enchantment.enchantmentAction
                                                .hediffDurationTicks;
                                        }
                                    }
                                    else if (thing is Pawn)
                                    {
                                        Pawn p = thing as Pawn;
                                        if (enchantment.enchantmentAction.splashRadius > 0)
                                        {
                                            List<Pawn> plist = TM_Calc.FindPawnsNearTarget(p,
                                                Mathf.RoundToInt(enchantment.enchantmentAction.splashRadius),
                                                p.Position, enchantment.enchantmentAction.friendlyFire);
                                            if (plist != null && plist.Count > 0)
                                            {
                                                for (int i = 0; i < plist.Count; i++)
                                                {
                                                    HealthUtility.AdjustSeverity(plist[i],
                                                        enchantment.enchantmentAction.hediffDef,
                                                        enchantment.enchantmentAction.hediffSeverity);
                                                    if (enchantment.enchantmentAction.hediffDurationTicks !=
                                                        0)
                                                    {
                                                        HediffComp_Disappears hdc = plist[i].health.hediffSet
                                                            .GetFirstHediffOfDef(enchantment.enchantmentAction
                                                                .hediffDef)
                                                            .TryGetComp<HediffComp_Disappears>();
                                                        hdc.ticksToDisappear = enchantment.enchantmentAction
                                                            .hediffDurationTicks;
                                                    }
                                                }
                                            }
                                        }

                                        HealthUtility.AdjustSeverity(p,
                                            enchantment.enchantmentAction.hediffDef,
                                            enchantment.enchantmentAction.hediffSeverity);
                                        if (enchantment.enchantmentAction.hediffDurationTicks != 0)
                                        {
                                            HediffComp_Disappears hdc = instigator.health.hediffSet
                                                .GetFirstHediffOfDef(enchantment.enchantmentAction.hediffDef)
                                                .TryGetComp<HediffComp_Disappears>();
                                            hdc.ticksToDisappear = enchantment.enchantmentAction
                                                .hediffDurationTicks;
                                        }
                                    }
                                }
                            }

                            if (enchantment.enchantmentAction.type ==
                                Enchantment.EnchantmentActionType.ApplyDamage &&
                                enchantment.enchantmentAction.damageDef != null &&
                                dinfo.Def != enchantment.enchantmentAction.damageDef)
                            {
                                if (Rand.Chance(enchantment.enchantmentAction.damageChance))
                                {
                                    DamageInfo dinfo2 = new DamageInfo(
                                        enchantment.enchantmentAction.damageDef,
                                        Rand.Range(
                                            enchantment.enchantmentAction.damageAmount -
                                            enchantment.enchantmentAction.damageVariation,
                                            enchantment.enchantmentAction.damageAmount +
                                            enchantment.enchantmentAction.damageVariation),
                                        enchantment.enchantmentAction.armorPenetration, -1f, instigator, null,
                                        dinfo.Weapon, DamageInfo.SourceCategory.ThingOrUnknown);

                                    if (enchantment.enchantmentAction.onSelf)
                                    {
                                        List<Pawn> plist = TM_Calc.FindPawnsNearTarget(instigator,
                                            Mathf.RoundToInt(enchantment.enchantmentAction.splashRadius),
                                            instigator.Position, enchantment.enchantmentAction.friendlyFire);
                                        if (plist != null && plist.Count > 0)
                                        {
                                            for (int i = 0; i < plist.Count; i++)
                                            {
                                                plist[i].TakeDamage(dinfo2);
                                            }
                                        }

                                        instigator.TakeDamage(dinfo2);
                                    }
                                    else if (thing is Pawn)
                                    {
                                        Pawn p = thing as Pawn;
                                        List<Pawn> plist = TM_Calc.FindPawnsNearTarget(p,
                                            Mathf.RoundToInt(enchantment.enchantmentAction.splashRadius),
                                            p.Position, enchantment.enchantmentAction.friendlyFire);
                                        if (plist != null && plist.Count > 0)
                                        {
                                            for (int i = 0; i < plist.Count; i++)
                                            {
                                                plist[i].TakeDamage(dinfo2);
                                            }
                                        }

                                        p.TakeDamage(dinfo2);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(AbilityAIDef), "CanPawnUseThisAbility", null)]
        public static class CanPawnUseThisAbility_Patch
        {
            private static bool Prefix(AbilityAIDef __instance, Pawn caster, LocalTargetInfo target,
                ref bool __result)
            {
                if (__instance.appliedHediffs.Count > 0 &&
                    __instance.appliedHediffs.Any((HediffDef hediffDef) =>
                        caster.health.hediffSet.HasHediff(hediffDef, false)))
                {
                    __result = false;
                }
                else
                {
                    if (!__instance.Worker.CanPawnUseThisAbility(__instance, caster, target))
                    {
                        __result = false;
                    }
                    else
                    {
                        if (!__instance.needEnemyTarget)
                        {
                            __result = true;
                        }
                        else
                        {
                            if (!__instance.usedOnCaster && target.IsValid)
                            {
                                float num = Math.Abs(caster.Position.DistanceTo(target.Cell));
                                if (num < __instance.minRange || num > __instance.maxRange)
                                {
                                    __result = false;
                                    return false;
                                }

                                if (__instance.needSeeingTarget &&
                                    !AbilityUserAI.AbilityUtility.LineOfSightLocalTarget(caster, target, true,
                                        null))
                                {
                                    __result = false;
                                    return false;
                                }
                            }

                            //Log.Message("caster " + caster.LabelShort + " attempting to case " + __instance.ability.defName + " on target " + target.Thing.LabelShort);
                            if (__instance.ability.defName == "TM_ArrowStorm" &&
                                !caster.equipment.Primary.def.weaponTags.Contains("Neolithic"))
                            {
                                __result = false;
                                return false;
                            }

                            if (__instance.ability.defName == "TM_DisablingShot" ||
                                __instance.ability.defName == "TM_Headshot" &&
                                caster.equipment.Primary.def.weaponTags.Contains("Neolithic"))
                            {
                                __result = false;
                                return false;
                            }

                            if (target.IsValid && !target.Thing.Destroyed && target.Thing.Map == caster.Map &&
                                target.Thing.Spawned)
                            {
                                Pawn targetPawn = target.Thing as Pawn;
                                if (targetPawn != null && targetPawn.Dead)
                                {
                                    __result = false;
                                    return false;
                                }
                                else
                                {
                                    __result = true;
                                }
                            }
                            else
                            {
                                __result = false;
                            }
                        }
                    }
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor", null), HarmonyPriority(100)]
        public static class FloatMenuMakerMap_ROMV_Undead_Patch
        {
            public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> __result)
            {
                IntVec3 c = IntVec3.FromVector3(clickPos);
                Pawn target = c.GetFirstPawn(pawn.Map);
                if (target != null)
                {
                    if ((target.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")) ||
                         target.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")) ||
                         target.health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD"))))
                    {
                        for (int i = 0; i < __result.Count(); i++)
                        {
                            string name = target.LabelShort;
                            if (__result[i].Label.Contains("Feed on") || __result[i].Label.Contains("Sip") ||
                                __result[i].Label.Contains("Embrace") ||
                                __result[i].Label.Contains("Give vampirism") ||
                                __result[i].Label.Contains("Create Ghoul") ||
                                __result[i].Label.Contains("Give vitae") || __result[i].Label ==
                                "Embrace " + name + " (Give vampirism)")
                            {
                                __result.Remove(__result[i]);
                            }
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(AbilityWorker), "CanPawnUseThisAbility", null)]
        public static class AbilityWorker_CanPawnUseThisAbility_Patch
        {
            public static bool Prefix(AbilityAIDef abilityDef, Pawn pawn, LocalTargetInfo target,
                ref bool __result)
            {
                if (!TM_Calc.HasMightOrMagicTrait(pawn)) return true;
                if (!ModOptions.Settings.Instance.AICasting)
                {
                    __result = false;
                    return false;
                }

                if (pawn.IsPrisoner || pawn.Downed)
                {
                    __result = false;
                    return false;
                }

                bool hasThing = target != null && target.HasThing;
                if (hasThing)
                {
                    if (abilityDef.needSeeingTarget && !TM_Calc.HasLoSFromTo(pawn.Position, target, pawn,
                            abilityDef.minRange, abilityDef.maxRange))
                    {
                        __result = false;
                        return false;
                    }

                    Pawn pawn2 = target.Thing as Pawn;
                    if (pawn2 != null)
                    {
                        if (abilityDef.ability == TorannMagicDefOf.TM_Possess && pawn2.RaceProps.Animal)
                        {
                            __result = false;
                            return false;
                        }

                        bool flag = !abilityDef.canTargetAlly;
                        if (flag)
                        {
                            __result = !pawn2.Downed;
                            return false;
                        }
                    }

                    Building bldg2 = target.Thing as Building;
                    if (bldg2 != null)
                    {
                        if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Empath) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Apothecary) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Shaman) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Commander) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.BloodMage) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.Priest) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.Geomancer) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.Druid) ||
                            pawn.story.traits.HasTrait(TorannMagicDefOf.Summoner))
                        {
                            __result = false;
                            return false;
                        }

                        __result = !bldg2.Destroyed;
                        return false;
                    }

                    Corpse corpse2 = target.Thing as Corpse;
                    if (corpse2 != null)
                    {
                        __result = true; //!corpse2.IsNotFresh();
                        return false;
                    }
                }

                __result = true;
                return false;
            }
        }
        
        [HarmonyPatch(typeof(FertilityGrid), "CalculateFertilityAt", null)]
        public static class FertilityGrid_Patch
        {
            private static void Postfix(Map ___map, IntVec3 loc, ref float __result)
            {
                if (ModOptions.Constants.GetGrowthCells().Count > 0)
                {
                    List<IntVec3> growthCells = ModOptions.Constants.GetGrowthCells();
                    for (int i = 0; i < growthCells.Count; i++)
                    {
                        if (loc != growthCells[i]) continue;

                        __result *= 2f;
                        if (Rand.Chance(.6f) && (ModOptions.Constants.GetLastGrowthMoteTick() + 5) <
                            Find.TickManager.TicksGame)
                        {
                            TM_MoteMaker.ThrowTwinkle(growthCells[i].ToVector3Shifted(), ___map,
                                Rand.Range(.3f, .7f), Rand.Range(100, 300), Rand.Range(.5f, 1.5f),
                                Rand.Range(.1f, .5f), .05f, Rand.Range(.8f, 1.8f));
                            ModOptions.Constants.SetLastGrowthMoteTick(Find.TickManager.TicksGame);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(AbilityWorker), "TargetAbilityFor", null)]
        public static class AbilityWorker_TargetAbilityFor_Patch
        {
            public static bool Prefix(AbilityAIDef abilityDef, Pawn pawn, ref LocalTargetInfo __result)
            {
                if (!TM_Calc.HasMightOrMagicTrait(pawn)) return true;


                if (abilityDef.usedOnCaster)
                {
                    __result = pawn;
                }
                else
                {
                    bool canTargetAlly = abilityDef.canTargetAlly;
                    if (canTargetAlly)
                    {
                        __result = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                            ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell,
                            TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false),
                            abilityDef.maxRange,
                            (Thing thing) => AbilityUserAI.AbilityUtility.AreAllies(pawn, thing), null, 0, -1,
                            false, RegionType.Set_Passable, false);
                    }
                    else
                    {
                        Pawn pawn2 = pawn.mindState.enemyTarget as Pawn;
                        Building bldg = pawn.mindState.enemyTarget as Building;
                        Corpse corpse = pawn.mindState.enemyTarget as Corpse;
                        if (pawn.mindState.enemyTarget != null && pawn2 != null)
                        {
                            if (!pawn2.Dead)
                            {
                                __result = pawn.mindState.enemyTarget;
                                return false;
                            }
                        }
                        else if (pawn.mindState.enemyTarget != null && bldg != null)
                        {
                            if (!bldg.Destroyed)
                            {
                                __result = pawn.mindState.enemyTarget;
                                return false;
                            }
                        }
                        else if (pawn.mindState.enemyTarget != null && corpse != null)
                        {
                            if (!corpse.IsNotFresh())
                            {
                                __result = pawn.mindState.enemyTarget;
                                return false;
                            }
                        }
                        else
                        {
                            if (pawn.mindState.enemyTarget != null && !(pawn.mindState.enemyTarget is Corpse))
                            {
                                __result = pawn.mindState.enemyTarget;
                                return false;
                            }
                        }

                        __result = null;
                    }
                }

                return false;
            }
        }
        
        [HarmonyPatch(typeof(Verb), "TryFindShootLineFromTo", null)]
        public static class TryFindShootLineFromTo_Base_Patch
        {
            public static bool Prefix(Verb __instance, IntVec3 root, LocalTargetInfo targ,
                out ShootLine resultingLine, ref bool __result)
            {
                if (__instance.verbProps.IsMeleeAttack)
                {
                    resultingLine = new ShootLine(root, targ.Cell);
                    __result = ReachabilityImmediate.CanReachImmediate(root, targ, __instance.caster.Map,
                        PathEndMode.Touch, null);
                    return false;
                }

                if (__instance.verbProps.range == 0 && __instance.CasterPawn != null &&
                    !__instance.CasterPawn.IsColonist) // allows ai to autocast on themselves
                {
                    resultingLine = default(ShootLine);
                    __result = true;
                    return false;
                }

                if (__instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Blink" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Summon" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_PhaseStrike" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_BLOS" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_LightSkip" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_SootheAnimal" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Effect_EyeOfTheStorm" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Effect_Flight" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Regenerate" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_SpellMending" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_CauterizeWound" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Transpose" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Disguise" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_SoulBond" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_SummonDemon" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_EarthSprites" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Overdrive" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_BlankMind" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_MechaniteReprogramming" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_ShadowWalk" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_SoL_CreateLight" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Enrage" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Hex" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Discord" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_AdvancedHeal" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_ControlSpiritStorm" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Scorn" ||
                    !__instance.verbProps.requireLineOfSight)
                {
                    //Ignores line of sight
                    //                    
                    if (__instance.CasterPawn != null && __instance.CasterPawn.RaceProps.Humanlike)
                    {
                        Pawn pawn = __instance.CasterPawn;
                        CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
                        if (comp != null && (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) ||
                                             TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_Transpose,
                                                 null, comp)))
                        {
                            MightPowerSkill ver =
                                comp.MightData.MightPowerSkill_Transpose.FirstOrDefault((MightPowerSkill x) =>
                                    x.label == "TM_Transpose_ver");
                            if (ver.level < 3)
                            {
                                __result = true;
                                resultingLine = default(ShootLine);
                                return true;
                            }
                        }
                    }

                    resultingLine = new ShootLine(root, targ.Cell);
                    __result = true;
                    return false;
                }

                resultingLine = default(ShootLine);
                __result = true;
                return true;
            }
        }

        [HarmonyPatch(typeof(CastPositionFinder), "TryFindCastPosition", null)]
        public static class TryFindCastPosition_Base_Patch
        {
            private static bool Prefix(CastPositionRequest newReq, out IntVec3 dest) //, ref IntVec3 __result)
            {
                CastPositionRequest req = newReq;
                IntVec3 casterLoc = req.caster.Position;
                Verb verb = req.verb;
                dest = IntVec3.Invalid;
                bool isTMAbility = verb.verbProps.verbClass.ToString().Contains("TorannMagic") ||
                                   verb.verbProps.verbClass.ToString().Contains("AbilityUser");


                if (verb.CanHitTargetFrom(casterLoc, req.target) &&
                    (req.caster.Position - req.target.Position).LengthHorizontal < verb.verbProps.range &&
                    isTMAbility)
                {
                    //If in range and in los, cast immediately
                    dest = casterLoc;
                    //__result = dest;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Pawn_SkillTracker), "Learn", null)]
        public static class Pawn_SkillTracker_Base_Patch
        {
            private static bool Prefix(Pawn_SkillTracker __instance, Pawn ___pawn)
            {
                if (___pawn != null)
                {
                    if (___pawn.story.traits.HasTrait(TorannMagicDefOf.Undead))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(SkillRecord), "Learn", null)]
        public static class SkillRecord_Patch
        {
            private static bool Prefix(SkillRecord __instance, Pawn ___pawn)
            {
                if (___pawn != null)
                {
                    if (___pawn.story.traits.HasTrait(TorannMagicDefOf.Undead))
                    {
                        return false;
                    }

                    if (___pawn is TMPawnGolem)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}