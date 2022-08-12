using System;
using Verse.AI;
using Verse;
using RimWorld;
using System.Collections.Generic;
using Verse.Sound;

namespace TorannMagic.Enchantment
{
    public class JobDriver_EnchantItem : JobDriver
    {
        Thing thing;
        IntVec3 thingLoc;
        Pawn actor;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null);
            throw new NotImplementedException();
        }

        public static void ErrorCheck(Pawn pawn, Thing haulThing)
        {
            if (!haulThing.Spawned)
            {
                Log.Message(string.Concat(new object[]
                {
                    pawn,
                    " tried to start carry ",
                    haulThing,
                    " which isn't spawned."
                }));
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
            }
            if (haulThing.stackCount == 0)
            {
                Log.Message(string.Concat(new object[]
                {
                    pawn,
                    " tried to start carry ",
                    haulThing,
                    " which had stackcount 0."
                }));
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
            }
            if (pawn.jobs.curJob.count <= 0)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Invalid count: ",
                    pawn.jobs.curJob.count,
                    ", setting to 1. Job was ",
                    pawn.jobs.curJob
                }));
                pawn.jobs.curJob.count = 1;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);


            Toil gotoThing = new Toil();
            gotoThing.initAction = delegate
            {
                this.pawn.pather.StartPath(this.TargetThingA, PathEndMode.Touch);
            };
            gotoThing.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            gotoThing.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return gotoThing;
            Toil enchanting = new Toil();//actions performed to enchant an item
            enchanting.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            enchanting.FailOnDestroyedOrNull(TargetIndex.A);
            enchanting.initAction = delegate
            {
                actor = enchanting.actor;
                thing = TargetThingA;
                thingLoc = thing.Position;
                if (!(thing.def.IsMeleeWeapon || thing.def.IsRangedWeapon || thing.def.IsApparel))
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                    Log.Message("Failed to initialize enchanting - invalid item type.");
                }
                else if(thing.def.defName.Contains("TM_Artifact"))
                {
                    Messages.Message("TM_CannotEnchantArtifact".Translate(
                        actor.LabelShort,
                        thing.LabelShort
                    ), MessageTypeDefOf.RejectInput);
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable, true);                    
                }
            };
            enchanting.tickAction = delegate
            {
                if(thing.Position != thingLoc || thing.Destroyed)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                    Log.Message("Failed to complete enchanting - item being enchanted not at enchanting location or destroyed");
                }
                if (Find.TickManager.TicksGame % 5 == 0)
                {
                    TM_MoteMaker.ThrowEnchantingMote(TargetLocA.ToVector3Shifted(), actor.Map, .6f);
                }
            };
            enchanting.WithProgressBar(TargetIndex.A, delegate
            {
                if (thing == null)
                {
                    return 1f;
                }
                return 1f - (float)enchanting.actor.jobs.curDriver.ticksLeftThisToil / 240;

            }, false, 0f);
            enchanting.defaultCompleteMode = ToilCompleteMode.Delay;
            enchanting.defaultDuration = 240;
            enchanting.AddFinishAction(delegate
            {
                CompEnchantedItem enchantment = thing.TryGetComp<CompEnchantedItem>();            
                CompEnchant enchantingItem = actor.TryGetComp<CompEnchant>();
                CompAbilityUserMagic pawnComp = actor.GetCompAbilityUserMagic();
                if (enchantment != null && enchantingItem != null && enchanting.actor.jobs.curDriver.ticksLeftThisToil < 1)
                {
                    if (EnchantItem(enchantingItem.enchantingContainer[0], enchantment))
                    {
                        enchantingItem.enchantingContainer[0].SplitOff(1).Destroy(DestroyMode.Vanish);
                        pawnComp.Mana.CurLevel -= .5f;
                        int num = Rand.Range(130, 180);
                        pawnComp.MagicUserXP += num;
                        MoteMaker.ThrowText(actor.DrawPos, actor.Map, "XP +" + num, -1f);
                        MoteMaker.ThrowText(TargetLocA.ToVector3Shifted(), actor.Map, "TM_Enchanted".Translate(), -1);
                        SoundStarter.PlayOneShotOnCamera(TorannMagicDefOf.ItemEnchanted, null);
                    }
                    else
                    {
                        Messages.Message("TM_NothingEnchanted".Translate(actor.LabelShort, thing.LabelShort, enchantingItem.enchantingContainer[0].LabelShort), MessageTypeDefOf.RejectInput);
                    }
                    
                    //DestroyEnchantingStone(enchantingItem.innerContainer[0]);
                }
                else
                {
                    Log.Message("Detected null enchanting comp.");
                }
            });
            yield return enchanting;
        }

        private bool EnchantItem(Thing gemstone, CompEnchantedItem enchantment)
        {
            enchantment.HasEnchantment = true;
            int enchantmentsApplied = 0;
            switch (gemstone.def.defName)
            {
                case "TM_EStone_wonder_minor":
                    if (enchantment.maxMP < .05f)
                    {
                        enchantment.maxMP = .05f;
                        enchantment.maxMPTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    if (enchantment.mpRegenRate < .05f)
                    {
                        enchantment.mpRegenRate = .05f;
                        enchantment.mpRegenRateTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    if (enchantment.mpCost > -.03f)
                    {
                        enchantment.mpCost = -.03f;
                        enchantment.mpCostTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    if (enchantment.coolDown > -.03f)
                    {
                        enchantment.coolDown = -.03f;
                        enchantment.coolDownTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    if (enchantment.xpGain < .05f)
                    {
                        enchantment.xpGain = .05f;
                        enchantment.xpGainTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    if (enchantment.arcaneRes < .10f)
                    {
                        enchantment.arcaneRes = .10f;
                        enchantment.arcaneResTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    if (enchantment.arcaneDmg < .04f)
                    {
                        enchantment.arcaneDmg = .04f;
                        enchantment.arcaneDmgTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_wonder":
                    if (enchantment.maxMP < .1f)
                    {
                        enchantment.maxMP = .1f;
                        enchantment.maxMPTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    if (enchantment.mpRegenRate < .1f)
                    {
                        enchantment.mpRegenRate = .1f;
                        enchantment.mpRegenRateTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    if (enchantment.mpCost > -.05f)
                    {
                        enchantment.mpCost = -.05f;
                        enchantment.mpCostTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    if (enchantment.coolDown > -.05f)
                    {
                        enchantment.coolDown = -.05f;
                        enchantment.coolDownTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    if (enchantment.xpGain < .10f)
                    {
                        enchantment.xpGain = .10f;
                        enchantment.xpGainTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    if (enchantment.arcaneRes < .20f)
                    {
                        enchantment.arcaneRes = .20f;
                        enchantment.arcaneResTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    if (enchantment.arcaneDmg < .08f)
                    {
                        enchantment.arcaneDmg = .08f;
                        enchantment.arcaneDmgTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_wonder_major":
                    if (enchantment.maxMP < .15f)
                    {
                        enchantment.maxMP = .15f;
                        enchantment.maxMPTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    if (enchantment.mpRegenRate < .15f)
                    {
                        enchantment.mpRegenRate = .15f;
                        enchantment.mpRegenRateTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    if (enchantment.mpCost > -.07f)
                    {
                        enchantment.mpCost = -.07f;
                        enchantment.mpCostTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    if (enchantment.coolDown > -.07f)
                    {
                        enchantment.coolDown = -.07f;
                        enchantment.coolDownTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    if (enchantment.xpGain < .15f)
                    {
                        enchantment.xpGain = .15f;
                        enchantment.xpGainTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    if (enchantment.arcaneRes < .30f)
                    {
                        enchantment.arcaneRes = .30f;
                        enchantment.arcaneResTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    if (enchantment.arcaneDmg < .12f)
                    {
                        enchantment.arcaneDmg = .12f;
                        enchantment.arcaneDmgTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_maxMP_minor":
                    if (enchantment.maxMP < .05f)
                    {
                        enchantment.maxMP = .05f;
                        enchantment.maxMPTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_maxMP":
                    if (enchantment.maxMP < .1f)
                    {
                        enchantment.maxMP = .1f;
                        enchantment.maxMPTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_maxMP_major":
                    if (enchantment.maxMP < .15f)
                    {
                        enchantment.maxMP = .15f;
                        enchantment.maxMPTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_mpRegenRate_minor":
                    if (enchantment.mpRegenRate < .05f)
                    {
                        enchantment.mpRegenRate = .05f;
                        enchantment.mpRegenRateTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_mpRegenRate":
                    if (enchantment.mpRegenRate < .1f)
                    {
                        enchantment.mpRegenRate = .1f;
                        enchantment.mpRegenRateTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_mpRegenRate_major":
                    if (enchantment.mpRegenRate < .15f)
                    {
                        enchantment.mpRegenRate = .15f;
                        enchantment.mpRegenRateTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_mpCost_minor":
                    if (enchantment.mpCost > -.03f)
                    {
                        enchantment.mpCost = -.03f;
                        enchantment.mpCostTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_mpCost":
                    if (enchantment.mpCost > -.05f)
                    {
                        enchantment.mpCost = -.05f;
                        enchantment.mpCostTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_mpCost_major":
                    if (enchantment.mpCost > -.07f)
                    {
                        enchantment.mpCost = -.07f;
                        enchantment.mpCostTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_coolDown_minor":
                    if (enchantment.coolDown > -.03f)
                    {
                        enchantment.coolDown = -.03f;
                        enchantment.coolDownTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_coolDown":
                    if (enchantment.coolDown > -.05f)
                    {
                        enchantment.coolDown = -.05f;
                        enchantment.coolDownTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_coolDown_major":
                    if (enchantment.coolDown > -.07f)
                    {
                        enchantment.coolDown = -.07f;
                        enchantment.coolDownTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_xpGain_minor":
                    if (enchantment.xpGain < .05f)
                    {
                        enchantment.xpGain = .05f;
                        enchantment.xpGainTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_xpGain":
                    if (enchantment.xpGain < .10f)
                    {
                        enchantment.xpGain = .10f;
                        enchantment.xpGainTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_xpGain_major":
                    if (enchantment.xpGain < .15f)
                    {
                        enchantment.xpGain = .15f;
                        enchantment.xpGainTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_arcaneRes_minor":
                    if (enchantment.arcaneRes < .10f)
                    {
                        enchantment.arcaneRes = .10f;
                        enchantment.arcaneResTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_arcaneRes":
                    if (enchantment.arcaneRes < .20f)
                    {
                        enchantment.arcaneRes = .20f;
                        enchantment.arcaneResTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_arcaneRes_major":
                    if (enchantment.arcaneRes < .30f)
                    {
                        enchantment.arcaneRes = .30f;
                        enchantment.arcaneResTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_arcaneDmg_minor":
                    if (enchantment.arcaneDmg < .04f)
                    {
                        enchantment.arcaneDmg = .04f;
                        enchantment.arcaneDmgTier = EnchantmentTier.Minor;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_arcaneDmg":
                    if (enchantment.arcaneDmg < .08f)
                    {
                        enchantment.arcaneDmg = .08f;
                        enchantment.arcaneDmgTier = EnchantmentTier.Standard;
                        enchantmentsApplied++;
                    }
                    break;
                case "TM_EStone_arcaneDmg_major":
                    if (enchantment.arcaneDmg < .12f)
                    {
                        enchantment.arcaneDmg = .12f;
                        enchantment.arcaneDmgTier = EnchantmentTier.Major;
                        enchantmentsApplied++;
                    }
                    break;
                case "null":
                    Log.Message("null");
                    break;
            }
            return enchantmentsApplied > 0;
        }

        private void DestroyEnchantingStone(Thing enchantingStone)
        {

        }
    }
}
