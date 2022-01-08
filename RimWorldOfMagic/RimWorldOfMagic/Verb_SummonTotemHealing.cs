using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse.AI;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_SummonTotemHealing : Verb_UseAbility
    {
        bool validTarg;
        //Used for non-unique abilities that can be used with shieldbelt
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == this.caster)
            {
                return this.verbProps.targetParams.canTargetSelf;
            }
            if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
                }
                else
                {
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        int pwrVal = 0;
        int verVal = 0;
        int effVal = 0;
        Thing totem = null;

        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Map map = caster.Map;
            IntVec3 cell = currentTarget.Cell;
            CompAbilityUserMagic comp = caster.TryGetComp<CompAbilityUserMagic>();
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_Totems);
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_Totems);
            effVal = TM_Calc.GetSkillEfficiencyLevel(caster, TorannMagicDefOf.TM_Totems);

            IntVec3 shiftPos = TM_Calc.GetEmptyCellForNewBuilding(cell, map, 2f, true, 0, true);
            if (shiftPos != null && (shiftPos.IsValid && shiftPos.Standable(map)))
            {
                AbilityUser.SpawnThings tempPod = new SpawnThings();
                tempPod.def = TorannMagicDefOf.TM_HealingTotem;
                tempPod.spawnCount = 1;
                try
                {
                    this.totem = TM_Action.SingleSpawnLoop(caster, tempPod, shiftPos, map, 2500 + (125 * verVal), true, false, caster.Faction, false, ThingDefOf.WoodLog);
                    this.totem.SetFaction(caster.Faction);
                    Building_TMTotem_Healing totemBuilding = this.totem as Building_TMTotem_Healing;
                    if (totemBuilding != null)
                    {
                        totemBuilding.pwrVal = pwrVal;
                        totemBuilding.verVal = verVal;
                        totemBuilding.arcanePwr = comp.arcaneDmg;
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 rndPos = this.totem.DrawPos;
                        rndPos.x += Rand.Range(-.5f, .5f);
                        rndPos.z += Rand.Range(-.5f, .5f);
                        TM_MoteMaker.ThrowGenericFleck(FleckDefOf.DustPuffThick, rndPos, map, Rand.Range(.6f, 1f), .1f, .05f, .05f, 0, 0, 0, Rand.Range(0, 360));
                        FleckMaker.ThrowSmoke(rndPos, map, Rand.Range(.8f, 1.2f));
                    }
                }
                catch
                {
                    comp.Mana.CurLevel += comp.ActualManaCost(TorannMagicDefOf.TM_SummonTotemHealing);
                    Log.Message("TM_Exception".Translate(
                            caster.LabelShort,
                            "Earth Totem"
                        ));
                }
            }
            else
            {
                if (caster.IsColonist)
                {
                    Messages.Message("InvalidSummon".Translate(), MessageTypeDefOf.RejectInput);
                    comp.Mana.GainNeed(comp.ActualManaCost(TorannMagicDefOf.TM_SummonTotemHealing));
                }
            }
            return false;
        }
    }
}
