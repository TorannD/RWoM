using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_EnchanterStone : Verb_UseAbility
    {

        private int verVal;
        private int effVal;

        bool validTarg;
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = true;
                }
                else
                {
                    //out of range
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
            MagicPowerSkill eff = base.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EnchanterStone.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EnchanterStone_eff");
            MagicPowerSkill ver = base.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EnchanterStone.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EnchanterStone_ver");
            effVal = eff.level;
            verVal = ver.level;

            List<Thing> thingList = this.currentTarget.Cell.GetThingList(this.CasterPawn.Map);
            Thing newThing = null;
            if (thingList != null && thingList.Count > 0)
            {
                for(int i =0; i < thingList.Count;i++)
                {
                    Thing thing = thingList[i];
                    if (thing != null && thing.def.EverHaulable)
                    {
                        if(thing.def == ThingDefOf.Silver)
                        {
                            if(thing.stackCount >= 500)
                            {
                                if (verVal >= 1)
                                {
                                    thing.SplitOff(500).Destroy();
                                    newThing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_Silver, null);
                                }
                                else
                                {
                                    Messages.Message("TM_CannotCreateStone".Translate(
                                        this.CasterPawn.LabelShort,
                                        thing.def.label
                                    ), MessageTypeDefOf.RejectInput);
                                }
                                break;
                            }
                            else
                            {
                                Messages.Message("InsufficientMaterialForArtifact".Translate(
                                    thing.def.label
                                ), MessageTypeDefOf.RejectInput);
                            }
                        }
                        else if (thing.def == ThingDefOf.Gold)
                        {
                            if (thing.stackCount >= 50)
                            {
                                if (verVal >= 3)
                                {
                                    thing.SplitOff(50).Destroy();
                                    newThing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_Gold, null);
                                }
                                else
                                {
                                    Messages.Message("TM_CannotCreateStone".Translate(
                                        this.CasterPawn.LabelShort,
                                        thing.def.label
                                    ), MessageTypeDefOf.RejectInput);
                                }
                                break;
                            }
                            else
                            {
                                Messages.Message("InsufficientMaterialForArtifact".Translate(
                                    thing.def.label
                                ), MessageTypeDefOf.RejectInput);
                            }
                        }
                        else if (thing.def == ThingDef.Named("Jade"))
                        {
                            if (thing.stackCount >= 50)
                            {
                                if (verVal >= 2)
                                {
                                    thing.SplitOff(50).Destroy();
                                    newThing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_Jade, null);
                                }
                                else
                                {
                                    Messages.Message("TM_CannotCreateStone".Translate(
                                        this.CasterPawn.LabelShort,
                                        thing.def.label
                                    ), MessageTypeDefOf.RejectInput);
                                }
                                break;
                            }
                            else
                            {
                                Messages.Message("InsufficientMaterialForArtifact".Translate(
                                    thing.def.label
                                ), MessageTypeDefOf.RejectInput);
                            }
                        }
                        else if (thing.def == TorannMagicDefOf.RawMagicyte)
                        {
                            if (thing.stackCount >= 100)
                            {
                                if (verVal >= 3)
                                {
                                    thing.SplitOff(100).Destroy();
                                    newThing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_Magicyte, null);
                                }
                                else
                                {
                                    Messages.Message("TM_CannotCreateStone".Translate(
                                        this.CasterPawn.LabelShort,
                                        thing.def.label
                                    ), MessageTypeDefOf.RejectInput);
                                }
                                break;
                            }
                            else
                            {
                                Messages.Message("InsufficientMaterialForArtifact".Translate(
                                    thing.def.label
                                ), MessageTypeDefOf.RejectInput);
                            }
                        }
                        else if (thing.def == ThingDefOf.Steel)
                        {
                            if (thing.stackCount >= 75)
                            {
                                if (verVal >= 1)
                                {
                                    thing.SplitOff(75).Destroy();
                                    newThing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_Steel, null);
                                }
                                else
                                {
                                    Messages.Message("TM_CannotCreateStone".Translate(
                                        this.CasterPawn.LabelShort,
                                        thing.def.label
                                    ), MessageTypeDefOf.RejectInput);
                                }
                                break;
                            }
                            else
                            {
                                Messages.Message("InsufficientMaterialForArtifact".Translate(
                                    thing.def.label
                                ), MessageTypeDefOf.RejectInput);
                            }
                        }
                        else if (thing.def == ThingDefOf.MedicineHerbal)
                        {
                            if (thing.stackCount >= 10)
                            {
                                if (verVal >= 2)
                                {
                                    thing.SplitOff(10).Destroy();
                                    newThing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_Medicine, null);
                                }
                                else
                                {
                                    Messages.Message("TM_CannotCreateStone".Translate(
                                        this.CasterPawn.LabelShort,
                                        thing.def.label
                                    ), MessageTypeDefOf.RejectInput);
                                }
                                break;
                            }
                            else
                            {
                                Messages.Message("InsufficientMaterialForArtifact".Translate(
                                    thing.def.label
                                ), MessageTypeDefOf.RejectInput);
                            }
                        }
                        else if (thing.def == ThingDefOf.WoodLog)
                        {
                            if (thing.stackCount >= 75)
                            {
                                if (verVal >= 2)
                                {
                                    thing.SplitOff(75).Destroy();
                                    newThing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_Wood, null);
                                }
                                else
                                {
                                    Messages.Message("TM_CannotCreateStone".Translate(
                                        this.CasterPawn.LabelShort,
                                        thing.def.label
                                    ), MessageTypeDefOf.RejectInput);
                                }
                                break;
                            }
                            else
                            {
                                Messages.Message("InsufficientMaterialForArtifact".Translate(
                                    thing.def.label
                                ), MessageTypeDefOf.RejectInput);
                            }
                        }
                        else if (thing.def.defName == "LotR_Iron")
                        {
                            if (thing.stackCount >= 75)
                            {
                                if (verVal >= 1)
                                {
                                    thing.SplitOff(75).Destroy();
                                    newThing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_Iron, null);
                                }
                                else
                                {
                                    Messages.Message("TM_CannotCreateStone".Translate(
                                        this.CasterPawn.LabelShort,
                                        thing.def.label
                                    ), MessageTypeDefOf.RejectInput);
                                }
                                break;
                            }
                            else
                            {
                                Messages.Message("InsufficientMaterialForArtifact".Translate(
                                    thing.def.label
                                ), MessageTypeDefOf.RejectInput);
                            }
                        }
                        else if(thing.def.defName == "ChunkSlate")
                        {
                            thing.Destroy();
                            newThing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_Slate, null);
                            break;
                        }
                        else if (thing.def.defName == "ChunkMarble")
                        {
                            thing.Destroy();
                            newThing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_Marble, null);
                            break;
                        }
                        else if (thing.def.defName == "ChunkSandstone")
                        {
                            thing.Destroy();
                            newThing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_Sandstone, null);
                            break;
                        }
                        else if (thing.def.defName == "ChunkGranite")
                        {
                            thing.Destroy();
                            newThing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_Granite, null);
                            break;
                        }
                        else if (thing.def.defName == "ChunkLimestone")
                        {
                            thing.Destroy();
                            newThing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_Limestone, null);
                            break;
                        }
                    }
                }               
            }
            else
            {
                Messages.Message("TM_InvalidTarget".Translate(
                    this.CasterPawn.LabelShort,
                    this.verbProps.label
                ), MessageTypeDefOf.RejectInput);
            }

            if(newThing != null)
            {
                if(comp.enchanterStones == null)
                {
                    comp.enchanterStones = new List<Thing>();
                    comp.enchanterStones.Clear();
                }
                GenPlace.TryPlaceThing(newThing, this.currentTarget.Cell, this.CasterPawn.Map, ThingPlaceMode.Near);
                comp.enchanterStones.Add(newThing);

                TM_Action.TransmutateEffects(this.currentTarget.Cell, this.CasterPawn);
            }

            this.burstShotsLeft = 0;
            return false;
        }
    }
}
