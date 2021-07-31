using System;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic.Conditions
{
    public class IncidentWorker_WanderingLich : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            bool tempAllow = false;
            Map map = (Map)parms.target;
            MagicMapComponent mmc = map.GetComponent<MagicMapComponent>();
            if (mmc != null && mmc.allowAllIncidents)
            {
                tempAllow = true;
            }
            if (settingsRef.wanderingLichChallenge > 0 || tempAllow)
            {
                int duration = Mathf.RoundToInt(this.def.durationDays.RandomInRange * 60000f);
                List<Faction> lichFaction = Find.FactionManager.AllFactions.ToList();
                bool factionFlag = false;
                for (int i = 0; i < lichFaction.Count; i++)
                {
                    if (lichFaction[i].def.defName == "TM_SkeletalFaction")
                    {
                        //Faction.OfPlayer.TrySetRelationKind(lichFaction[i], FactionRelationKind.Hostile, false, null, null);
                        Faction.OfPlayer.TryAffectGoodwillWith(lichFaction[i], -200, false, false, null, null);
                        factionFlag = true;
                    }
                }
                if (!factionFlag)
                {
                    return false;
                }                
                TM_Action.ForceFactionDiscoveryAndRelation(TorannMagicDefOf.TM_SkeletalFaction);
                GameCondition_WanderingLich gameCondition_WanderingLich = (GameCondition_WanderingLich)GameConditionMaker.MakeCondition(GameConditionDef.Named("WanderingLich"), duration);
                map.gameConditionManager.RegisterCondition(gameCondition_WanderingLich);
                base.SendStandardLetter(parms, gameCondition_WanderingLich.thing, "");
                //base.SendStandardLetter(new TargetInfo(gameCondition_WanderingLich.edgeLocation.ToIntVec3, map, false), null, new string[0]);
                return true;
            }
            else
            {
                return false; 
            }
        }
    }
}