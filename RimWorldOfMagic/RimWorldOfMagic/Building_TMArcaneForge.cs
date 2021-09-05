using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Verse.Sound;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Building_TMArcaneForge : Building_WorkTable
    {
        private Thing targetThing = null;
        private LocalTargetInfo infoTarget = null;

        List<RecipeDef> replicatedRecipes = new List<RecipeDef>();

        //Saved forge recipe variables
        private bool hasSavedRecipe = false;
        private ThingDef copiedThingDef = null;
        private ThingDef copiedStuffDef = null;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.hasSavedRecipe, "hasSavedRecipe", false, false);
            Scribe_Defs.Look<ThingDef>(ref this.copiedThingDef, "copiedThingDef");
            Scribe_Defs.Look<ThingDef>(ref this.copiedStuffDef, "copiedStuffDef");
            bool flag = Scribe.mode == LoadSaveMode.PostLoadInit;
            if (flag && this.hasSavedRecipe)
            {
                RestoreForgeRecipeAfterLoad();
            }
        }


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            replicatedRecipes = new List<RecipeDef>();
            replicatedRecipes.Clear();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            
            var gizmoList = base.GetGizmos().ToList();
            if (ResearchProjectDef.Named("TM_ForgeReplication").IsFinished)
            {
                bool canScan = true;
                for(int i =0; i < this.BillStack.Count; i++)
                {
                    if(this.BillStack[i].recipe.defName == "ArcaneForge_Replication")
                    {
                        canScan = false;
                    }
                }
                if (canScan)
                {
                    TargetingParameters newParameters = new TargetingParameters();
                    newParameters.canTargetItems = true;
                    newParameters.canTargetBuildings = true;
                    newParameters.canTargetLocations = true;


                    String label = "TM_Replicate".Translate();
                    String desc = "TM_ReplicateDesc".Translate();

                    Command_LocalTargetInfo item = new Command_LocalTargetInfo
                    {
                        defaultLabel = label,
                        defaultDesc = desc,
                        order = 67,
                        icon = ContentFinder<Texture2D>.Get("UI/replicate", true),
                        targetingParams = newParameters
                    };
                    item.action = delegate (LocalTargetInfo thing)
                    {
                        this.infoTarget = thing;
                        IntVec3 localCell = thing.Cell;
                        this.targetThing = thing.Cell.GetFirstItem(this.Map);
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Scan, localCell.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather), this.Map, 1.2f, .8f, 0f, .5f, -400, 0, 0, Rand.Range(0, 360));
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Scan, localCell.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather), this.Map, 1.2f, .8f, 0f, .5f, 400, 0, 0, Rand.Range(0, 360));
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Scan, this.DrawPos, this.Map, 1.2f, .8f, 0f, .5f, 400, 0, 0, Rand.Range(0, 360));
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Scan, this.DrawPos, this.Map, 1.2f, .8f, 0f, .5f, -400, 0, 0, Rand.Range(0, 360));
                        SoundInfo info = SoundInfo.InMap(new TargetInfo(thing.Cell, this.Map, false), MaintenanceType.None);
                        info.pitchFactor = 1.3f;
                        info.volumeFactor = 1.3f;
                        SoundDefOf.TurretAcquireTarget.PlayOneShot(info);
                        if (targetThing != null && targetThing.def.EverHaulable)
                        {
                            ClearReplication();
                            Replicate();
                        }
                        else
                        {
                            Messages.Message("TM_FoundNoReplicateTarget".Translate(), MessageTypeDefOf.CautionInput);
                        }
                    };
                    gizmoList.Add(item);
                }
                else
                {
                    String label = "TM_ReplicateDisabled".Translate();
                    String desc = "TM_ReplicateDisabledDesc".Translate();

                    Command_Action item2 = new Command_Action
                    {
                        defaultLabel = label,
                        defaultDesc = desc,
                        order = 68,
                        icon = ContentFinder<Texture2D>.Get("UI/replicateDisabled", true),
                        action = delegate
                        {
                            ClearReplication();
                        }
                    };
                    gizmoList.Add(item2);
                }
            }

            return gizmoList;            
        }

        private void ClearReplication()
        {
            for (int i = 0; i < this.BillStack.Count; i++)
            {
                if (this.BillStack[i].recipe.defName == "ArcaneForge_Replication")
                {
                    List<Pawn> mapPawns = this.Map.mapPawns.AllPawnsSpawned;
                    for(int j =0; j < mapPawns.Count; j++)
                    {
                        if(mapPawns[j].IsColonist && mapPawns[j].RaceProps.Humanlike && mapPawns[j].CurJob != null && mapPawns[j].CurJob.bill != null)
                        {
                            if (mapPawns[j].CurJob.bill.recipe.defName == this.BillStack[i].recipe.defName)
                            {
                                mapPawns[j].jobs.EndCurrentJob(Verse.AI.JobCondition.Incompletable, false);
                            }
                        }
                    }
                    
                    this.BillStack.Bills.Remove(this.BillStack[i]);
                }
            }
            
            
        }

        public void Replicate(ThingDef repThingDef = null, ThingDef repStuffDef = null)
        {
            ThingDef replicatedThingDef = null;
            ThingDef replicatedStuffDef = null;
            if (repThingDef == null)
            {
                CheckForUnfinishedThing();
                replicatedThingDef = this.targetThing.def;
                replicatedStuffDef = targetThing.Stuff;
            }
            else
            {
                replicatedThingDef = repThingDef;
                replicatedStuffDef = repStuffDef;
            }
            RecipeDef forgeRecipe = TorannMagicDefOf.ArcaneForge_Replication;
            RecipeDef replicant = null;
            List<RecipeDef> potentialRecipes = new List<RecipeDef>();
            potentialRecipes.Clear();

            IEnumerable<RecipeDef> enumerable = from def in DefDatabase<RecipeDef>.AllDefs
                                                where (!(def is MagicRecipeDef) && def.defName.Contains(replicatedThingDef.defName) && !def.defName.Contains("Administer") && !def.label.Contains("Replicate") && !def.label.Contains("Install") && !def.label.Contains("install"))
                                                select def;

            foreach (RecipeDef current in enumerable)
            {
                potentialRecipes.Add(current);
            }

            RecipeDef gemstoneRecipe = null;
            gemstoneRecipe = CheckForGemstone(replicatedThingDef);

            if(gemstoneRecipe != null)
            {
                potentialRecipes.Add(gemstoneRecipe);
            }

            if(potentialRecipes != null && replicatedThingDef != null && potentialRecipes.Count > 0)
            {
                replicant = potentialRecipes.RandomElement();

                IngredientCount ic = new IngredientCount();
                if (replicant != null && replicant.ingredients != null)
                {
                    for (int i = 0; i < replicant.ingredients.Count; i++)
                    {
                        if (!replicant.ingredients[i].IsFixedIngredient && !replicatedThingDef.MadeFromStuff)
                        {
                            Messages.Message("TM_ReplicatedUnrecognizedIngredient".Translate(replicatedThingDef.LabelCap), MessageTypeDefOf.RejectInput);
                            goto EndReplicate;
                        }
                    }

                    forgeRecipe.ingredients.Clear();

                    if (replicant.ingredients.Count > 0)
                    {
                        for (int i = 0; i < replicant.ingredients.Count; i++)
                        {
                            if (replicant.ingredients[i].filter != null && replicant.ingredients[i].filter.ToString() != "ingredients" && replicant.ingredients[i].IsFixedIngredient) //added fixed ingredients
                            {
                                forgeRecipe.ingredients.Add(replicant.ingredients[i]);
                            }
                        }
                    }
                }

                if (replicatedStuffDef != null && replicatedThingDef != null && replicatedThingDef.MadeFromStuff)
                {
                    ic.filter.SetAllow(replicatedStuffDef, true);
                    ic.SetBaseCount(replicatedThingDef.costStuffCount);
                    forgeRecipe.ingredients.Add(ic);
                }

                forgeRecipe.workAmount = replicant.workAmount;
                forgeRecipe.description = replicant.description;
                forgeRecipe.label = "Replicate " + replicant.label;
                forgeRecipe.unfinishedThingDef = replicant.unfinishedThingDef;
                forgeRecipe.products = replicant.products;
                this.copiedThingDef = replicatedThingDef;
                this.copiedStuffDef = replicatedStuffDef;
                this.hasSavedRecipe = true;                

                EndReplicate:;

                if (forgeRecipe.ingredients.Count == 0)
                {
                    forgeRecipe.ingredients.Clear();

                    replicant = TorannMagicDefOf.ArcaneForge_Replication_Restore;
                    if (replicant.ingredients.Count > 0)
                    {
                        for (int i = 0; i < replicant.ingredients.Count; i++)
                        {
                            if (replicant.ingredients[i].filter.ToString() != "ingredients")
                            {
                                forgeRecipe.ingredients.Add(replicant.ingredients[i]);
                            }
                        }

                        forgeRecipe.workAmount = replicant.workAmount;
                        forgeRecipe.description = replicant.description;
                        forgeRecipe.label = replicant.label;
                        forgeRecipe.unfinishedThingDef = replicant.unfinishedThingDef;
                        forgeRecipe.products = replicant.products;
                    }
                    this.hasSavedRecipe = false;
                }
            }
            else
            {
                Messages.Message("TM_FoundNoReplicateRecipe".Translate(this.targetThing.def.defName), MessageTypeDefOf.CautionInput);
            }
      
        }

        private void CheckForUnfinishedThing()
        {
            Thing unfinishedThing = this.Position.GetFirstItem(this.Map);
            if(unfinishedThing != null && unfinishedThing.def.isUnfinishedThing)
            {
                unfinishedThing.Destroy(DestroyMode.Cancel);
            }
        }

        private RecipeDef CheckForGemstone(ThingDef td)
        {
            RecipeDef returnedRecipe = null;
            String gemString = "Cut";
            String gemType = "";
            String gemQual = "";
            if (td.defName.Contains("wonder"))
            {
                gemType = "wonder";
            }
            if (td.defName.Contains("maxMP"))
            {
                gemType = "MPGem";
            }
            if (td.defName.Contains("mpRegenRate"))
            {
                gemType = "MPRegenRateGem";
            }
            if (td.defName.Contains("mpCost"))
            {
                gemType = "MPCostGem";
            }
            if (td.defName.Contains("coolDown"))
            {
                gemType = "CoolDownGem";
            }
            if (td.defName.Contains("xpGain"))
            {
                gemType = "XPGainGem";
            }
            if (td.defName.Contains("arcaneRes"))
            {
                gemType = "ArcaneResGem";
            }
            if (td.defName.Contains("arcaneDmg"))
            {
                gemType = "ArcaneDmgGem";
            }
            if(td.defName.Contains("_major"))
            {
                gemQual = "Major";
            }
            if (td.defName.Contains("_minor"))
            {
                gemQual = "Minor";
            }

            gemString += gemQual;
            gemString += gemType;


            IEnumerable<RecipeDef> enumerable = from def in DefDatabase<RecipeDef>.AllDefs
                                                where (def.defName == gemString)
                                                select def;

            foreach (RecipeDef current in enumerable)
            {
                returnedRecipe = current;
            }

            return returnedRecipe;
        }

        private void RestoreForgeRecipeAfterLoad()
        {
            RecipeDef forgeRecipe = TorannMagicDefOf.ArcaneForge_Replication;            
            if(this.hasSavedRecipe)
            {
                Replicate(this.copiedThingDef, this.copiedStuffDef);
            }
            else
            {
                ClearReplication();
            }
        }

    }
}
