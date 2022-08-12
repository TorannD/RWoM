using System;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;


namespace TorannMagic
{
    public class WorkGiver_ChargeArcaneObject : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.InteractionCell;
            }
        }

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            
            return (from p in pawn.Map.listerBuildings.allBuildingsColonist
                    where p.def == TorannMagicDefOf.TM_Portal || p.def == TorannMagicDefOf.TM_ArcaneCapacitor || p.def == TorannMagicDefOf.TM_DimensionalManaPocket
                    select p).Cast<Thing>();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_TMPortal portal = t as Building_TMPortal;
            Building_TMArcaneCapacitor arcaneCapacitor = t as Building_TMArcaneCapacitor;
            Building_TM_DMP dmp = t as Building_TM_DMP;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if (comp != null && portal != null && comp.IsMagicUser && comp.Mana.CurLevelPercentage >= .7f && portal.IsPaired && portal.ArcaneEnergyCur < .7f)
            {
                if (pawn != null && pawn.RaceProps.Humanlike && pawn.workSettings.WorkIsActive(TorannMagicDefOf.TM_Magic) && pawn.health.capacities.GetLevel(TorannMagicDefOf.MagicManipulation) > 0 && pawn.IsColonist && pawn.Awake() && !pawn.Drafted && !pawn.Downed && pawn.CanReserveAndReach(portal, PathEndMode.InteractionCell, Danger.Some))
                {
                    return true;
                }
            }
            if (comp != null && arcaneCapacitor != null && comp.IsMagicUser && comp.Mana.CurLevelPercentage >= .9f && arcaneCapacitor.ArcaneEnergyCur < arcaneCapacitor.TargetArcaneEnergyPct && arcaneCapacitor.CapacitorIsOn)
            {
                if (pawn != null && pawn.RaceProps.Humanlike && pawn.workSettings.WorkIsActive(TorannMagicDefOf.TM_Magic) && pawn.health.capacities.GetLevel(TorannMagicDefOf.MagicManipulation) > 0 && pawn.IsColonist && pawn.Awake() && !pawn.Drafted && !pawn.Downed && pawn.CanReserveAndReach(arcaneCapacitor, PathEndMode.InteractionCell, Danger.Some))
                {
                    return true;
                }
            }
            if (comp != null && dmp != null && comp.IsMagicUser && comp.Mana.CurLevelPercentage >= .9f && dmp.ArcaneEnergyCur < dmp.TargetArcaneEnergyPct && dmp.IsOn)
            {
                if (pawn != null && pawn.RaceProps.Humanlike && pawn.workSettings.WorkIsActive(TorannMagicDefOf.TM_Magic) && pawn.health.capacities.GetLevel(TorannMagicDefOf.MagicManipulation) > 0 && pawn.IsColonist && pawn.Awake() && !pawn.Drafted && !pawn.Downed && pawn.CanReserveAndReach(dmp, PathEndMode.InteractionCell, Danger.Some))
                {
                    return true;
                }
            }
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return new Job(TorannMagicDefOf.ChargePortal, t);        
        }
    }
}
