using RimWorld;
using System;
using RimWorld.Planet;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;

namespace TorannMagic
{
    class Verb_ShadowCall : Verb_UseAbility  
    {
        bool arg_41_0;
        bool arg_42_0;

        protected override bool TryCastShot()
        {
            bool result = false;
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
            Pawn soulPawn = comp.soulBondPawn;

            if(soulPawn != null && !soulPawn.Dead && !soulPawn.Destroyed)
            {
                if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                {
                    ModCheck.GiddyUp.ForceDismount(soulPawn);
                }
                bool drafted = soulPawn.Drafted;
                bool soulPawnSpawned = soulPawn.Spawned;
                Map map = soulPawn.Map;
                if(map == null)
                {
                    Hediff bondHediff = null;
                    bondHediff = soulPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_SoulBondPhysicalHD"), false);
                    if (bondHediff != null)
                    {
                        HediffComp_SoulBondHost compS = bondHediff.TryGetComp<HediffComp_SoulBondHost>();
                        if (compS != null && compS.polyHost != null && !compS.polyHost.DestroyedOrNull() && !compS.polyHost.Dead)
                        {
                            soulPawnSpawned = true;
                            soulPawn = compS.polyHost;
                        }
                    }
                    bondHediff = null;

                    bondHediff = soulPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_SoulBondMentalHD"), false);
                    if (bondHediff != null)
                    {
                        HediffComp_SoulBondHost compS = bondHediff.TryGetComp<HediffComp_SoulBondHost>();
                        if (compS != null && compS.polyHost != null && !compS.polyHost.DestroyedOrNull() && !compS.polyHost.Dead)
                        {
                            soulPawnSpawned = true;
                            soulPawn = compS.polyHost;
                        }
                    }
                    if (soulPawn.ParentHolder != null && soulPawn.ParentHolder is Caravan van)
                    {
                        van.RemovePawn(soulPawn);
                        GenPlace.TryPlaceThing(soulPawn, this.CasterPawn.Position, this.CasterPawn.Map, ThingPlaceMode.Near);
                        if(van.PawnsListForReading != null && van.PawnsListForReading.Count <= 0)
                        {
                            CaravanEnterMapUtility.Enter(van, this.CasterPawn.Map, CaravanEnterMode.Center, CaravanDropInventoryMode.DropInstantly, false);
                        }
                        
                        //Messages.Message("" + p.LabelShort + " has shadow stepped to a caravan with " + soulPawn.LabelShort, MessageTypeDefOf.NeutralEvent);
                        goto fin;
                    }
                }
                IntVec3 casterCell = this.CasterPawn.Position;
                IntVec3 targetCell = soulPawn.Position;
                if (soulPawnSpawned)
                {
                    try
                    {
                        soulPawn.DeSpawn();
                        GenSpawn.Spawn(soulPawn, casterCell, this.CasterPawn.Map);
                        if (drafted)
                        {
                            soulPawn.drafter.Drafted = true;
                        }
                    }
                    catch
                    {
                        Log.Message("Exception occured when trying to summon soul bound pawn - recovered pawn at original position");
                        GenSpawn.Spawn(soulPawn, targetCell, map);

                    }
                }
                else
                {
                    Messages.Message("TM_BondedPawnNotSpawned".Translate(
                        soulPawn.LabelShort), MessageTypeDefOf.RejectInput);
                }
                //this.Ability.PostAbilityAttempt();
                result = true;
            }
            else
            {
                Log.Warning("No soul bond found to shadow call.");
            }
            fin:;
            this.burstShotsLeft = 0;
            //this.ability.TicksUntilCasting = (int)base.UseAbilityProps.SecondsToRecharge * 60;
            return result;
        }
    }
}
