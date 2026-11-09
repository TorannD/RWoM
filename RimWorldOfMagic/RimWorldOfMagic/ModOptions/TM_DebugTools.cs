
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using RimWorld.SketchGen;
using UnityEngine;
using Verse;
using Verse.AI;
using HarmonyLib;
using LudeonTK;

namespace TorannMagic.ModOptions
{
    public static class TM_DebugTools
    {
        [DebugAction("RWoM", "T: Elemental Rift - Earth", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ElementalRiftEarth() => SpawnElementalRift(0);
        [DebugAction("RWoM", "T: Elemental Rift - Fire", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ElementalRiftFire() => SpawnElementalRift(1);
        [DebugAction("RWoM", "T: Elemental Rift - Water", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ElementalRiftWater() => SpawnElementalRift(2);
        [DebugAction("RWoM", "T: Elemental Rift - Air", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ElementalRiftAir() => SpawnElementalRift(3);

        private static void SpawnElementalRift(int element)
        {
            Map map = Find.CurrentMap;
            IntVec3 cell = UI.MouseCell();
            if (map == null || map.Size.x <= 16 || map.Size.z <= 16 || !cell.InBounds(map)
                || cell.DistanceToEdge(map) < 8 || !cell.Standable(map) || cell.Roofed(map)
                || cell.Fogged(map) || cell.GetThingList(map).Any(t => t is Building || t is Pawn || t.def.category == ThingCategory.Item))
            {
                Messages.Message("Choose a clear, unroofed cell at least 8 cells from the map edge.", MessageTypeDefOf.RejectInput);
                return;
            }
            if (Settings.Instance.riftChallenge <= 0)
            {
                Messages.Message("Enable elemental rifts in RWoM settings first.", MessageTypeDefOf.RejectInput);
                return;
            }
            GameConditionDef conditionDef = GameConditionDef.Named("ElementalAssault");
            if (map.gameConditionManager.ConditionIsActive(conditionDef))
            {
                Messages.Message("An elemental assault is already active on this map.", MessageTypeDefOf.RejectInput);
                return;
            }
            Faction faction = Find.FactionManager.FirstFactionOfDef(TorannMagicDefOf.TM_ElementalFaction);
            if (faction == null)
            {
                Messages.Message("This world has no elemental faction.", MessageTypeDefOf.RejectInput);
                return;
            }
            Faction.OfPlayer.TryAffectGoodwillWith(faction, -200, false, false, null, null);
            TM_Action.ForceFactionDiscoveryAndRelation(TorannMagicDefOf.TM_ElementalFaction);
            IncidentDef incident = DefDatabase<IncidentDef>.GetNamed("ElementalAssault");
            var condition = (Conditions.GameCondition_ElementalAssault)GameConditionMaker.MakeCondition(conditionDef,
                Mathf.RoundToInt(incident.durationDays.RandomInRange * 60000f));
            condition.ConfigureDebug(cell, element);
            map.gameConditionManager.RegisterCondition(condition);
            Messages.Message("Elemental rift created: " + new[] { "Earth", "Fire", "Water", "Air" }[element]
                + ". Unpause to begin the assault.", condition.thing, MessageTypeDefOf.ThreatBig);
        }

        [DebugAction("RWoM", "Spawn Scrolls", actionType =DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnScrolls()
        {
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where (def.defName.StartsWith("SpellOf_") || def.defName.StartsWith("SkillOf_"))
                                               select def;
            foreach(ThingDef d in enumerable)
            {
                DebugThingPlaceHelper.DebugSpawn(d, UI.MouseCell(), 1, false, null);
            }
        }

        [DebugAction("RWoM", "T: Spawn Spell Book...", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnSpellBook()
        {
            SpawnLearnItemMenu(d => d.defName.StartsWith("SpellOf_"));
        }

        [DebugAction("RWoM", "T: Spawn Skill Book...", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnSkillBook()
        {
            SpawnLearnItemMenu(d => d.defName.StartsWith("SkillOf_"));
        }

        [DebugAction("RWoM", "T: Spawn Torn Book...", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnTornBook()
        {
            SpawnLearnItemMenu(d => d.defName.StartsWith("Torn_BookOf"));
        }

        // One learn item of the picked kind at the cursor cell, with a picker
        // styled like the Give Class menu (sorted by in-game label).
        private static void SpawnLearnItemMenu(Func<ThingDef, bool> filter)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs
                .Where(filter)
                .OrderBy(d => d.LabelCap.ToString()))
            {
                ThingDef captured = def;
                options.Add(new FloatMenuOption(captured.LabelCap, delegate
                {
                    Map map = Find.CurrentMap;
                    IntVec3 cell = UI.MouseCell();
                    if (map == null || !cell.InBounds(map))
                    {
                        Messages.Message("No valid cell under the cursor.", MessageTypeDefOf.RejectInput);
                        return;
                    }
                    Thing thing = ThingMaker.MakeThing(captured);
                    if (GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near))
                    {
                        Messages.Message("Spawned " + captured.LabelCap + ".", MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message("Could not place " + captured.LabelCap + " here.", MessageTypeDefOf.RejectInput);
                    }
                }));
            }
            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        [DebugAction("RWoM", "T: Spawn Mage Duel (10 v 10)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnMageDuel10()
        {
            SpawnMageDuel(10, false);
        }

        [DebugAction("RWoM", "T: Spawn Mage Duel (50 v 50, random classes)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnMageDuel50()
        {
            SpawnMageDuel(50, true);
        }

        private static void SpawnMageDuel(int perSide, bool randomClasses)
        {
            Map map = Find.CurrentMap;
            IntVec3 root = UI.MouseCell();
            if (!root.IsValid || map == null)
            {
                root = map.Center;
            }

            // two distinct non-player factions that are mutual enemies
            var nonPlayer = Find.FactionManager.AllFactions
                .Where(f => !f.IsPlayer && !f.defeated && !f.temporary && !f.HostileTo(Faction.OfPlayer))
                .ToList();
            Faction sideA = null;
            Faction sideB = null;
            foreach (Faction a in nonPlayer)
            {
                foreach (Faction b in nonPlayer)
                {
                    if (a != b && a.HostileTo(b))
                    {
                        sideA = a;
                        sideB = b;
                        break;
                    }
                }
                if (sideB != null)
                {
                    break;
                }
            }
            if (sideA == null || sideB == null)
            {
                Messages.Message("No two mutually hostile factions found on this map/world.", MessageTypeDefOf.RejectInput);
                return;
            }

            // mage kinds: the RWoM Seer family (they come with casting comp via backstory)
            List<PawnKindDef> kinds = DefDatabase<PawnKindDef>.AllDefs
                .Where(k => k.defName.StartsWith("Seer"))
                .ToList();
            if (kinds.Count == 0)
            {
                Messages.Message("No Seer mage pawnkinds found.", MessageTypeDefOf.RejectInput);
                return;
            }

            int spread = perSide <= 10 ? 14 : 26;
            IntVec3 posA = root + new IntVec3(-spread, 0, -spread / 2);
            IntVec3 posB = root + new IntVec3(spread, 0, spread / 2);
            List<Pawn> listA = SpawnMageSide(map, posA, sideA, kinds, perSide, randomClasses);
            List<Pawn> listB = SpawnMageSide(map, posB, sideB, kinds, perSide, randomClasses);
            if (listA.Count == 0 || listB.Count == 0)
            {
                foreach (Pawn p in listA.Concat(listB))
                {
                    p.Destroy(DestroyMode.Vanish);
                }
                Messages.Message("Spawn failed (no valid cells).", MessageTypeDefOf.RejectInput);
                return;
            }

            // both sides immediately assault each other
            // damageFraction: max — the vanilla transition would switch the whole
            // lord to ExitMapAndDefendSelf once the targets took 100% cumulative
            // damage, sending everyone off the map mid-fight; with it maxed the
            // assault lasts until a side is downed
            Verse.AI.Group.LordMaker.MakeNewLord(sideA, new LordJob_AssaultThings(sideA, listB.Cast<Verse.Thing>().ToList(), float.MaxValue), map);
            Verse.AI.Group.LordMaker.MakeNewLord(sideB, new LordJob_AssaultThings(sideB, listA.Cast<Verse.Thing>().ToList(), float.MaxValue), map);

            string classNote = randomClasses ? " with random classes" : "";
            Messages.Message($"Mage duel{classNote}: {sideA.Name} ({listA.Count}) vs {sideB.Name} ({listB.Count}).",
                MessageTypeDefOf.ThreatBig);
        }

        private static List<Pawn> SpawnMageSide(Map map, IntVec3 root, Faction faction, List<PawnKindDef> kinds, int perSide, bool randomClasses)
        {
            List<Pawn> list = new List<Pawn>();
            for (int i = 0; i < perSide; i++)
            {
                PawnKindDef kind = kinds[i % kinds.Count];
                IntVec3 cell = CellFinder.RandomClosewalkCellNear(root, map, perSide <= 10 ? 5 : 12);
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    kind,
                    faction,
                    PawnGenerationContext.NonPlayer,
                    map.Tile,
                    forceGenerateNewPawn: true));
                GenSpawn.Spawn(pawn, cell, map);
                // vanilla raid pawns get this in PawnGroupKindWorker_Normal: without
                // it any damage below the kind's flee threshold starts PanicFlee and
                // the duelist runs for the map edge mid-battle
                pawn.mindState.canFleeIndividual = false;
                if (pawn.needs?.mood != null)
                {
                    pawn.needs.mood.CurLevelPercentage = 1f;
                }
                if (randomClasses)
                {
                    ApplyRandomClass(pawn);
                }
                // start the duel with full resources — otherwise the autocast mana
                // threshold keeps them from casting while it regenerates
                Need_Mana mana = pawn.needs?.TryGetNeed<Need_Mana>();
                if (mana != null)
                {
                    mana.CurLevel = mana.MaxLevel;
                }
                Need_Stamina stamina = pawn.needs?.TryGetNeed<Need_Stamina>();
                if (stamina != null)
                {
                    stamina.CurLevel = stamina.MaxLevel;
                }
                list.Add(pawn);
            }
            return list;
        }

        // Gives a generated pawn a random class through the same full-reset path as
        // the Give Class action, then rolls random power levels so the AI leveled-
        // variant swap casts scaled spells right away.
        private static void ApplyRandomClass(Pawn pawn)
        {
            try
            {
                List<TraitDef> pool = TM_Data.MagicTraits.Concat(TM_Data.MightTraits).ToList();
                List<TorannMagic.TMDefs.TM_CustomClass> customs = TM_ClassUtility.CustomClasses;
                if (pawn.IsColonist && customs != null && customs.Count > 0 && Rand.Chance(0.25f))
                {
                    int index = Rand.RangeInclusive(0, customs.Count - 1);
                    TorannMagic.TMDefs.TM_CustomClass cc = customs[index];
                    if (cc != null && cc.classTrait != null)
                    {
                        ApplyClassTrait(pawn, cc.classTrait, cc.traitDegree, index, cc, cc.isMage, logGrant: false);
                        RollRandomPowerLevels(pawn);
                        return;
                    }
                }
                if (pool.Count == 0)
                {
                    return;
                }
                TraitDef picked = pool.RandomElement();
                ApplyClassTrait(pawn, picked, 0, -1, null, TM_Data.MagicTraits.Contains(picked), logGrant: false);
                RollRandomPowerLevels(pawn);
            }
            catch (Exception ex)
            {
                Compatibility.TM_Log.Warning("Classes", "Debug: random class roll failed for " + pawn.LabelShortCap + ": " + ex.Message);
            }
        }

        private static void RollRandomPowerLevels(Pawn pawn)
        {
            try
            {
                CompAbilityUserMagic cm = pawn.GetCompAbilityUserMagic();
                if (cm != null && cm.MagicData != null)
                {
                    foreach (MagicPower mp in cm.MagicData.AllMagicPowersWithSkills)
                    {
                        TMAbilityDef ability = mp.abilityDef as TMAbilityDef;
                        if (mp.learned && mp.maxLevel > 0 && mp.TMabilityDefs != null && mp.TMabilityDefs.Count > 0)
                        {
                            int cap = Math.Min(2, Math.Min(mp.maxLevel, mp.TMabilityDefs.Count - 1));
                            mp.level = Rand.RangeInclusive(0, cap);
                        }
                    }
                }
                CompAbilityUserMight cmm = pawn.GetCompAbilityUserMight();
                if (cmm != null && cmm.MightData != null)
                {
                    foreach (MightPower mp in cmm.MightData.AllMightPowersWithSkills)
                    {
                        TMAbilityDef ability = mp.abilityDef as TMAbilityDef;
                        if (mp.learned && mp.maxLevel > 0 && mp.TMabilityDefs != null && mp.TMabilityDefs.Count > 0)
                        {
                            int cap = Math.Min(2, Math.Min(mp.maxLevel, mp.TMabilityDefs.Count - 1));
                            mp.level = Rand.RangeInclusive(0, cap);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Compatibility.TM_Log.Warning("Classes", "Debug: random power level roll failed: " + ex.Message);
            }
        }

        [DebugAction("RWoM", "T: Give mage foundation (new progression)...", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void GiveMageFoundation(Pawn pawn)
        {
            if (!ClassProgression.ClassProgressionFeature.Enabled || pawn?.story?.traits == null) return;
            var options = ClassProgression.ClassInitiation.FoundationProfiles.OrderBy(p => p.label)
                .Select(p => new FloatMenuOption(p.LabelCap, () => {
                    if (!ClassProgression.ClassInitiation.TryGrantStartingFoundation(pawn, p.legacyTrait))
                        Messages.Message("Requires a fresh pawn with no existing class or progression.", MessageTypeDefOf.RejectInput, false);
                })).ToList();
            if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
        }

        [DebugAction("RWoM", "T: Give Class...", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void GiveClass(Pawn pawn)
        {
            if (pawn == null || pawn.story == null || pawn.story.traits == null)
            {
                return;
            }
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            foreach (TraitDef td in TM_Data.MagicTraits)
            {
                TraitDef captured = td;
                options.Add(new FloatMenuOption("magic: " + ClassTraitLabel(td), delegate
                {
                    ApplyClassTrait(pawn, captured, 0, customIndex: -1, customClass: null, isMage: true);
                }));
            }
            foreach (TraitDef td in TM_Data.MightTraits)
            {
                TraitDef captured = td;
                options.Add(new FloatMenuOption("might: " + ClassTraitLabel(td), delegate
                {
                    ApplyClassTrait(pawn, captured, 0, customIndex: -1, customClass: null, isMage: false);
                }));
            }

            List<TorannMagic.TMDefs.TM_CustomClass> customClasses = TM_ClassUtility.CustomClasses;
            if (customClasses != null)
            {
                for (int i = 0; i < customClasses.Count; i++)
                {
                    TorannMagic.TMDefs.TM_CustomClass cc = customClasses[i];
                    if (cc == null || cc.classTrait == null)
                    {
                        continue;
                    }
                    int index = i;
                    string kind = cc.isMage ? "custom mage" : "custom fighter";
                    options.Add(new FloatMenuOption(kind + ": " + ClassTraitLabel(cc.classTrait), delegate
                    {
                        ApplyClassTrait(pawn, cc.classTrait, cc.traitDegree, index, cc, cc.isMage, logGrant: false);
                    }));
                }
            }

            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static string ClassTraitLabel(TraitDef td)
        {
            if (td != null && td.degreeDatas != null && td.degreeDatas.Count > 0)
            {
                return td.degreeDatas[0].label;
            }
            return td != null ? td.defName : "???";
        }

        /// <summary>
        /// Grants a class the way a class book does, with a full reset first so an
        /// existing class does not leave stale abilities behind: clear class trait
        /// and class hediffs, drop the comp's power/XP data, gain the new trait,
        /// store the customIndex/customClass pair for custom classes, then force the
        /// comp to re-run its own assignment (the same ResolveFactionChange path the
        /// game uses when a pawn joins the colony with a class).
        /// </summary>
        internal static bool ApplyClassTrait(Pawn pawn, TraitDef classTrait, int traitDegree, int customIndex, TorannMagic.TMDefs.TM_CustomClass customClass, bool isMage, bool logGrant = true)
        {
            try
            {
                TM_DebugTools.RemoveClassTrait(pawn);
                if (pawn.health != null && pawn.health.hediffSet != null)
                {
                    TM_DebugTools.RemoveClassHediffs(pawn);
                }

                CompAbilityUserMagic compMagic = pawn.GetCompAbilityUserMagic();
                CompAbilityUserMight compMight = pawn.GetCompAbilityUserMight();

                // some class traits have no degree-0 entry (e.g. TM_WindMage starts
                // at degree 1) — normalize or GainTrait spams "found no data at degree".
                // NOTE: DataAtDegree() has a built-in first-defined fallback and never
                // returns null, so scan degreeDatas directly.
                if (!classTrait.degreeDatas.Any(dd => dd.degree == traitDegree) && classTrait.degreeDatas.Count > 0)
                {
                    traitDegree = classTrait.degreeDatas[0].degree;
                }
                bool spiritClass = classTrait == TorannMagicDefOf.TM_Possessor;
                bool possessedClass = classTrait == TorannMagicDefOf.TM_Possessed;
                // GainTrait immediately invokes AddAdvancedClass, which requires
                // possession to exist already or removes this trait reentrantly.
                if (possessedClass)
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_SpiritPossessionHD, .5f);
                if (spiritClass && pawn.def != TorannMagicDefOf.TM_SpiritTD)
                {
                    // the class-gain patch reserves the spirit trait for actual
                    // spirit pawns (TM_SpiritTD); a debug grant overrides that
                    pawn.story.traits.allTraits.Add(new Trait(classTrait, traitDegree, false));
                }
                else
                {
                    pawn.story.traits.GainTrait(new Trait(classTrait, traitDegree, false));
                }
                if (spiritClass)
                {
                    // the same class hediff and need refresh AssignSpiritEffecters
                    // gives a freshly generated spirit pawn
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_SpiritPossessorHD, .5f);
                    pawn.needs.AddOrRemoveNeedsAsAppropriate();
                }

                // mirror the class-book mechanic: taking a class consumes the
                // Magically Gifted / Physical Prodigy stepping-stone traits
                // (the book and ritual paths remove them the same way)
                for (int i = pawn.story.traits.allTraits.Count - 1; i >= 0; i--)
                {
                    Trait t = pawn.story.traits.allTraits[i];
                    if (t != null && (t.def == TorannMagicDefOf.TM_Gifted || t.def == TorannMagicDefOf.PhysicalProdigy))
                    {
                        pawn.story.traits.allTraits.RemoveAt(i);
                    }
                }

                // A direct dev trait insertion does not run the normal GainTrait hooks.
                // Resolve identity before discarding and rebuilding either data owner.
                Utils.TM_PawnTracker.ResolveComps(pawn);
                if (isMage && compMagic?.IsMagicUser != true) return false;
                if (compMagic != null && compMagic.IsMagicUser)
                {
                    if (customClass != null)
                    {
                        compMagic.customIndex = customIndex;
                        compMagic.customClass = customClass;
                    }
                    if (compMagic.MagicData != null)
                    {
                        compMagic.RemovePowers(true);
                        Traverse.Create(compMagic).Field("magicData").SetValue(null);
                    }
                    Traverse.Create(compMagic).Field("colonistPowerCheck").SetValue(false);
                    compMagic.magicPowersInitializedForColonist = false;
                    compMagic.ResolveFactionChange();
                }
                if (compMight != null && compMight.IsMightUser)
                {
                    if (customClass != null)
                    {
                        compMight.customIndex = customIndex;
                        compMight.customClass = customClass;
                    }
                    if (compMight.MightData != null)
                    {
                        compMight.RemovePowers(true);
                        Traverse.Create(compMight).Field("mightData").SetValue(null);
                    }
                    compMight.AssignAbilities();
                }

                // make mana/stamina (class hediff + need) appear immediately: the comp
                // only resolves them on its own init tick, which already ran for pawns
                // that existed before the class was granted
                if (compMagic != null && compMagic.IsMagicUser)
                {
                    compMagic.ResolveMana();
                }
                if (compMight != null && compMight.IsMightUser)
                {
                    compMight.ResolveStamina();
                }
                if (classTrait == TorannMagicDefOf.Lich && !ClassProgression.ClassIdentity.TryGetState(pawn,out _) &&
                    !ClassProgression.LichTransformation.GiveClassicDebugForm(compMagic)) return false;

                // the spirit class bypasses GainTrait, and the active gene state
                // must follow debug class changes just like gameplay ones
                TM_ClassGenes.SyncClassGenes(pawn);

                if (logGrant)
                {
                    Compatibility.TM_Log.Message("Classes", $"Debug: granted class '{ClassTraitLabel(classTrait)}' to {pawn.LabelShortCap}.");
                }
                return true;
            }
            catch (Exception ex)
            {
                Compatibility.TM_Log.Error("Classes", "Debug GiveClass failed: " + ex);
                return false;
            }
        }

        [DebugAction("RWoM", "Repair Missing Class Data", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void RepairMissingClassData(Pawn pawn)
        {
            Compatibility.TM_LegacyRecovery.RepairWithReport(pawn);
        }

        [DebugAction("RWoM", "Remove Class", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void RemoveClass(Pawn pawn)
        {
            //Pawn pawn = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).Where((Thing t) => t is Pawn).Cast<Pawn>().FirstOrDefault();
            if (pawn != null)
            {
                bool addMagicComp = false;
                bool addMightComp = false;
                CompAbilityUserMagic compMagic = pawn.GetCompAbilityUserMagic();
                if(compMagic != null && compMagic.IsMagicUser)
                {
                    RemoveMagicComp(compMagic);
                    try
                    {
                        pawn.AllComps.Remove(compMagic);
                        addMagicComp = true;
                    }
                    catch(NullReferenceException ex)
                    {
                        Compatibility.TM_Log.Warning("Classes", "failed to remove magic comp");
                    }
                }
                CompAbilityUserMight compMight = pawn.GetCompAbilityUserMight();
                if(compMight != null && compMight.IsMightUser)
                {
                    RemoveMightComp(compMight);
                    try
                    {
                        pawn.AllComps.Remove(compMight);
                        addMightComp = true;
                    }
                    catch(NullReferenceException ex)
                    {
                        Compatibility.TM_Log.Warning("Classes", "failed to remove might comp");
                    }
                }
                if (pawn.story != null && pawn.story.traits != null)
                {
                    RemoveClassTrait(pawn);
                }
                if (pawn.health != null && pawn.health.hediffSet != null)
                {
                    RemoveClassHediffs(pawn);                                        
                }

                if (addMagicComp)
                {
                    compMagic = new CompAbilityUserMagic();
                    compMagic.parent = pawn;
                    pawn.AllComps.Add(compMagic);
                    TM_Calc.InvalidateCompCache(pawn);
                }

                if (addMightComp)
                {
                    compMight = new CompAbilityUserMight();
                    compMight.parent = pawn;
                    pawn.AllComps.Add(compMight);
                    TM_Calc.InvalidateCompCache(pawn);
                }
                TM_ClassGenes.SyncClassGenes(pawn);
                //if (addMagicComp || addMightComp)
                //{
                //    InitializeTMComps(pawn, addMagicComp, addMightComp);
                //}
            }
        }

        public static void RemoveClassHediffs(Pawn pawn)
        {
            List<Hediff> rhd = new List<Hediff>();
            foreach (Hediff hd in pawn.health.hediffSet.hediffs)
            {
                if (hd != null)
                {
                    if (hd.def == TorannMagicDefOf.TM_MagicUserHD || hd.def == TorannMagicDefOf.TM_MightUserHD || hd.def == TorannMagicDefOf.TM_LightCapacitanceHD || hd.def == TorannMagicDefOf.TM_ChiHD ||
                        hd.def == TorannMagicDefOf.TM_PsionicHD || hd.def == TorannMagicDefOf.TM_HateHD || hd.def == TorannMagicDefOf.TM_BloodHD || hd.def == TorannMagicDefOf.TM_BowTrainingHD ||
                        hd.def == TorannMagicDefOf.TM_BladeArtHD || hd.def == TorannMagicDefOf.TM_RayOfHope_AuraHD || hd.def == TorannMagicDefOf.TM_InnerFire_AuraHD || hd.def == TorannMagicDefOf.TM_Shadow_AuraHD ||
                        hd.def == TorannMagicDefOf.TM_SoothingBreeze_AuraHD || hd.def == TorannMagicDefOf.TM_CommanderAuraHD || hd.def == TorannMagicDefOf.TM_EnchantedAuraHD || hd.def == TorannMagicDefOf.TM_ProvisionerAuraHD ||
                        hd.def == TorannMagicDefOf.TM_TaskMasterAuraHD || hd.def == TorannMagicDefOf.TM_SDSoulBondPhysicalHD || hd.def == TorannMagicDefOf.TM_WDSoulBondMentalHD || hd.def == TorannMagicDefOf.TM_EnchantedBodyHD ||
                        hd.def == TorannMagicDefOf.TM_TechnoBitHD || hd.def == TorannMagicDefOf.TM_MindOverBodyHD || hd.def == TorannMagicDefOf.TM_HediffFortitude || hd.def == TorannMagicDefOf.TM_MageLightHD ||
                        hd.def == TorannMagicDefOf.TM_PredictionHD || hd.def == TorannMagicDefOf.TM_Artifact_BloodBoostHD || hd.def == TorannMagicDefOf.TM_Artifact_HateBoostHD || hd.def == TorannMagicDefOf.TM_Artifact_PsionicBoostHD ||
                        hd.def == TorannMagicDefOf.TM_LichHD || hd.def == TorannMagicDefOf.TM_ShapeshiftHD || hd.def == TorannMagicDefOf.TM_NightshadeHD ||  hd.def == TorannMagicDefOf.TM_HediffSprint || hd.def == TorannMagicDefOf.TM_SunderArmorHD ||
                        hd.def == TorannMagicDefOf.TM_ShadowSlayerCloakHD || hd.def == TorannMagicDefOf.TM_ManaShieldHD || hd.def == TorannMagicDefOf.TM_BlurHD ||  hd.def == TorannMagicDefOf.TM_InvisibilityHD ||
                        hd.def == TorannMagicDefOf.TM_HediffFightersFocus || hd.def == TorannMagicDefOf.TM_HediffThickSkin || hd.def == TorannMagicDefOf.TM_HediffStrongBack || hd.def == TorannMagicDefOf.TM_HediffGearRepair ||
                        hd.def == TorannMagicDefOf.TM_HediffHeavyBlow || hd.def == TorannMagicDefOf.TM_BurningFuryHD || hd.def == TorannMagicDefOf.TM_CursedHD ||
                        hd.def == TorannMagicDefOf.TM_SpiritPossessorHD || hd.def == TorannMagicDefOf.TM_SpiritPossessionHD)
                    {
                        rhd.Add(hd);
                    }
                    if(TM_ClassUtility.CustomClassHediffs().Contains(hd.def))
                    {
                        rhd.Add(hd);
                    }
                }
            }
            foreach (Hediff hd in rhd)
            {
                pawn.health.RemoveHediff(hd);
            }
        }

        public static void RemoveClassTrait(Pawn pawn)
        {
            for (int i = 0; i < TM_Data.AllClassTraits.Count; i++)
            {
                for (int j = 0; j < pawn.story.traits.allTraits.Count; j++)
                {
                    if (pawn.story.traits.allTraits[j].def == TM_Data.AllClassTraits[i])
                    {
                        pawn.story.traits.allTraits.Remove(pawn.story.traits.allTraits[j]);
                        break;
                    }
                }
            }
            // custom classes (spirit classes included) are not part of the builtin
            // trait lists, but a new grant must not keep the previous custom trait
            List<TorannMagic.TMDefs.TM_CustomClass> customClasses = TM_ClassUtility.CustomClasses;
            if (customClasses != null)
            {
                for (int i = 0; i < customClasses.Count; i++)
                {
                    TraitDef customTrait = customClasses[i] != null ? customClasses[i].classTrait : null;
                    for (int j = pawn.story.traits.allTraits.Count - 1; j >= 0; j--)
                    {
                        if (customTrait != null && pawn.story.traits.allTraits[j].def == customTrait)
                        {
                            pawn.story.traits.allTraits.RemoveAt(j);
                        }
                    }
                }
            }
            if(pawn.story.traits.HasTrait(TorannMagicDefOf.TM_CursedTD))
            {
                pawn.story.traits.RemoveTrait(pawn.story.traits.GetTrait(TorannMagicDefOf.TM_CursedTD));
            }
        }

        public static void RemoveMightComp(CompAbilityUserMight compMight)
        {
            if (compMight.bondedPet != null)
            {
                compMight.bondedPet.SetFaction(null, null);
                compMight.bondedPet.Destroy(DestroyMode.Vanish);
            }
            foreach (Thing t in compMight.combatItems)
            {
                if(t != null)
                t.Destroy(DestroyMode.Vanish);
            }
            compMight.shouldDraw = false;
        }

        public static void RemoveMagicComp(CompAbilityUserMagic compMagic)
        {
            if (!compMagic.bondedSpirit.DestroyedOrNull())
            {
                compMagic.bondedSpirit.Destroy(DestroyMode.Vanish);
            }
            compMagic.earthSprites = default(IntVec3);
            compMagic.earthSpriteType = 0;
            if (compMagic.enchanterStones != null)
            {
                foreach (Thing t in compMagic.enchanterStones)
                {
                    if(t != null)
                        t.Destroy(DestroyMode.Vanish);
                }
            }
            if(compMagic.fertileLands != null)
                compMagic.fertileLands.Clear();
            if (!compMagic.SoL.DestroyedOrNull())
            {
                compMagic.SoL.Destroy(DestroyMode.Vanish);
            }
            if (compMagic.StoneskinPawns != null)
            {
                foreach (Pawn p in compMagic.StoneskinPawns)
                {
                    if (p != null && p.health != null && p.health.hediffSet != null)
                    {
                        Hediff hd = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_StoneskinHD);
                        if (hd != null)
                        {
                            p.health.RemoveHediff(hd);
                        }
                    }
                }
            }
            foreach (Thing t in compMagic.summonedCoolers)
            {
                if (t != null)
                    t.Destroy(DestroyMode.Vanish);
            }
            foreach (Thing t in compMagic.summonedHeaters)
            {
                if (t != null)
                    t.Destroy(DestroyMode.Vanish);
            }
            foreach (Thing t in compMagic.summonedLights)
            {
                if (t != null)
                    t.Destroy(DestroyMode.Vanish);
            }
            foreach (Thing t in compMagic.summonedPowerNodes)
            {
                if (t != null)
                    t.Destroy(DestroyMode.Vanish);
            }
            foreach (Thing t in compMagic.summonedSentinels)
            {
                if (t != null)
                    t.Destroy(DestroyMode.Vanish);
            }
            foreach (Thing t in compMagic.supportedUndead)
            {
                if (t != null)
                    t.Kill(null, null);
            }
            foreach (Pawn p in compMagic.weaponEnchants)
            {
                if (p != null)
                {
                    if (p.health != null && p.health.hediffSet != null)
                    {
                        foreach (Hediff hd in p.health.hediffSet.hediffs)
                        {
                            if (hd.def == TorannMagicDefOf.TM_WeaponEnchantment_DarkHD || hd.def == TorannMagicDefOf.TM_WeaponEnchantment_FireHD || hd.def == TorannMagicDefOf.TM_WeaponEnchantment_IceHD || hd.def == TorannMagicDefOf.TM_WeaponEnchantment_LitHD)
                            {
                                p.health.RemoveHediff(hd);
                            }
                        }
                    }
                }
            }
            compMagic.shouldDraw = false;            
        }

        public static void InitializeTMComps(Pawn p, bool initMagic = false, bool initMight = false)
        {
            if(p.def.comps.Any())
            {
                List<ThingComp> comps = Traverse.Create(root: p).Field(name: "comps").GetValue<List<ThingComp>>();
                for(int i = 0; i < comps.Count; i++)
                {
                    ThingComp thingComp = null;
                    if (initMagic && comps[i].ToString().Contains("CompAbilityUserMagic"))
                    {
                        try
                        {
                            //thingComp = (ThingComp)Activator.CreateInstance(comps[i]);
                            thingComp.parent = p;
                            comps.Add(thingComp);
                            thingComp.Initialize(p.def.comps[i]);
                        }
                        catch (Exception arg)
                        {
                            Compatibility.TM_Log.Error("Classes", "Could not instantiate or initialize a ThingComp: " + arg);
                            comps.Remove(thingComp);
                        }
                    }
                    if (initMight && p.def.comps[i].compClass.ToString().Contains("CompAbilityUserMight"))
                    {
                        try
                        {
                            thingComp = (ThingComp)Activator.CreateInstance(p.def.comps[i].compClass);
                            thingComp.parent = p;
                            comps.Add(thingComp);
                            thingComp.Initialize(p.def.comps[i]);
                        }
                        catch (Exception arg)
                        {
                            Compatibility.TM_Log.Error("Classes", "Could not instantiate or initialize a ThingComp: " + arg);
                            comps.Remove(thingComp);
                        }
                    }
                }
                Traverse.Create(root: p).Field(name: "comps").SetValue(comps);
            }
        }

        [DebugAction("RWoM", "Spawn Spirit", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnSpirit()
        {            
            Pawn spiritPawn = TM_Action.GenerateSpiritPawn(UI.MouseCell(), Faction.OfPlayer);
            GenSpawn.Spawn(spiritPawn, UI.MouseCell(), Find.CurrentMap);  
        }

        [DebugAction("RWoM", "T: -20% Spirit", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ReduceSpirit(Pawn pawn)
        {
            //Pawn pawn = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).Where((Thing t) => t is Pawn).Cast<Pawn>().FirstOrDefault();
            if (pawn != null && pawn.needs != null)
            {
                Need_Spirit ns = pawn.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND) as Need_Spirit;
                if(ns != null)
                {
                    ns.GainNeed(-20f);
                }
            }
        }

        [DebugAction("RWoM", "T: +20% Spirit", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void GainSpirit(Pawn pawn)
        {
            //Pawn pawn = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).Where((Thing t) => t is Pawn).Cast<Pawn>().FirstOrDefault();
            if (pawn != null && pawn.needs != null)
            {
                Need_Spirit ns = pawn.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND) as Need_Spirit;
                if (ns != null)
                {
                    ns.GainNeed(20f);
                }
            }
        }

        [DebugAction("RWoM", "Give Energy", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void GiveEnergy(Pawn pawn)
        {
            //Pawn pawn = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).Where((Thing t) => t is Pawn).Cast<Pawn>().FirstOrDefault();
            if (pawn != null)
            {               
                CompAbilityUserMagic compMagic = pawn.GetCompAbilityUserMagic();
                if (compMagic != null && compMagic.IsMagicUser && compMagic.Mana != null)
                {                    
                    try
                    {
                        compMagic.Mana.CurLevel += 1f;
                    }
                    catch (NullReferenceException ex)
                    {
                        Compatibility.TM_Log.Warning("Classes", "failed to give mana");
                    }
                }
                CompAbilityUserMight compMight = pawn.GetCompAbilityUserMight();
                if (compMight != null && compMight.IsMightUser && compMight.Stamina != null)
                {
                    try
                    {
                        compMight.Stamina.CurLevel += 1f;
                    }
                    catch (NullReferenceException ex)
                    {
                        Compatibility.TM_Log.Warning("Classes", "failed to give stamina");
                    }
                }                
            }
        }
    }
}
