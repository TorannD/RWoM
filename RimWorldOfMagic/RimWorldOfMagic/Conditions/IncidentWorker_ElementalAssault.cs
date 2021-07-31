using System;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;


namespace TorannMagic.Conditions
{
    public class IncidentWorker_ElementalAssault : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (settingsRef.riftChallenge > 0)
            {
                string str = "";
                Map map = (Map)parms.target;
                int duration = Mathf.RoundToInt(this.def.durationDays.RandomInRange * 60000f);
                List<Faction> lichFaction = Find.FactionManager.AllFactions.ToList();
                bool factionFlag = false;
                for (int i = 0; i < lichFaction.Count; i++)
                {
                    if (lichFaction[i].def.defName == "TM_ElementalFaction")
                    {
                        Faction.OfPlayer.TryAffectGoodwillWith(lichFaction[i], -200, false, false, null, null);
                        factionFlag = true;
                    }
                }
                if (!factionFlag)
                {
                    return false;
                }
                TM_Action.ForceFactionDiscoveryAndRelation(TorannMagicDefOf.TM_ElementalFaction);
                GameCondition_ElementalAssault gameCondition_ElementalAssault = (GameCondition_ElementalAssault)GameConditionMaker.MakeCondition(GameConditionDef.Named("ElementalAssault"), duration);
                map.gameConditionManager.RegisterCondition(gameCondition_ElementalAssault);
                base.SendStandardLetter(parms, gameCondition_ElementalAssault.thing, str);
                //base.SendStandardLetter(new TargetInfo(gameCondition_ElementalAssault.centerLocation.ToIntVec3, map, false), null, new string[0]);
                return true;
            }
            else
            {
                return false; 
            }
        }
    }
}



