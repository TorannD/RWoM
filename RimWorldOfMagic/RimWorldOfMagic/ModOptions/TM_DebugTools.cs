
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

namespace TorannMagic.ModOptions
{
    public static class TM_DebugTools
    {
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
                        Log.Warning("failed to remove magic comp");
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
                        Log.Warning("failed to remove might comp");
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
                }

                if (addMightComp)
                {
                    compMight = new CompAbilityUserMight();
                    compMight.parent = pawn;
                    pawn.AllComps.Add(compMight);
                }
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
                        hd.def == TorannMagicDefOf.TM_HediffHeavyBlow || hd.def == TorannMagicDefOf.TM_BurningFuryHD || hd.def == TorannMagicDefOf.TM_CursedHD)
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
                            Log.Error("Could not instantiate or initialize a ThingComp: " + arg);
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
                            Log.Error("Could not instantiate or initialize a ThingComp: " + arg);
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
    }
}
