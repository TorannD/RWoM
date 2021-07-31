using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    public class IncidentWorker_QuestArcaneCamp : IncidentWorker
    {
        //protected override bool CanFireNowSub(IncidentParms target)
        //{
        //    Faction faction;
        //    Faction faction2;
        //    int num;
        //    return base.CanFireNowSub(target) && this.TryFindFactions(out faction, out faction2) && TileFinder.TryFindNewSiteTile(out num, 8, 30, false, true, -1);
        //}

        //protected override bool TryExecuteWorker(IncidentParms parms)
        //{
        //    Faction faction;
        //    Faction faction2;
        //    if (!this.TryFindFactions(out faction, out faction2))
        //    {
        //        return false;
        //    }
        //    int tile;
        //    if (!TileFinder.TryFindNewSiteTile(out tile, 4, 25, false, true, -1))
        //    {
        //        return false;
        //    }
        //    Site site = SiteMaker.MakeSite(SitePartDefOf.Outpost, tile, faction2, true);
        //    SitePart turrets = new SitePart();
        //    turrets.def = SitePartDefOf.Turrets;
        //    site.parts.Add(turrets);
        //    SitePart arcaneBanditSquad = new SitePart();
        //    arcaneBanditSquad.def = TorannMagicDefOf.ArcaneBanditSquad;
        //    site.parts.Add(arcaneBanditSquad);
        //    site.Tile = tile;
        //    List<Thing> list = this.GenerateRewards(faction, parms);
        //    site.GetComponent<DefeatAllEnemiesQuestComp>().StartQuest(faction, 12, list);
        //    Find.WorldObjects.Add(site);
        //    string itemList = "";
        //    for(int i = 0; i< list.Count(); i++)
        //    {
        //        itemList += list[i].LabelShort + "\n";
        //    }
        //    base.SendStandardLetter(parms, site, faction, new NamedArgument()
        //    {
        //        faction.leader.LabelShort,
        //        faction.def.leaderTitle,
        //        faction.Name,
        //        itemList
        //    });
        //    return true;
        //}

        //private List<Thing> GenerateRewards(Faction alliedFaction, IncidentParms parms)
        //{
        //    int totalMarketValue = (int)Mathf.Clamp(StorytellerUtility.DefaultThreatPointsNow(parms.target) * 10, 1000, 3000);
        //    List<Thing> list = new List<Thing>();
        //    ItemCollectionGenerator_Internal_Arcane itc_ia = new ItemCollectionGenerator_Internal_Arcane();            
        //    return itc_ia.Generate(totalMarketValue, list);
        //    //return ItemCollectionGeneratorDefOf.BanditCampQuestRewards.Worker.Generate(parms);
        //}

        //private bool AnyQuestExistsFrom(Faction faction)
        //{
        //    List<Site> sites = Find.WorldObjects.Sites;
        //    for (int i = 0; i < sites.Count; i++)
        //    {
        //        DefeatAllEnemiesQuestComp component = sites[i].GetComponent<DefeatAllEnemiesQuestComp>();
        //        if (component != null && component.Active && component.requestingFaction == faction)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        //private bool TryFindFactions(out Faction alliedFaction, out Faction enemyFaction)
        //{
        //    if ((from x in Find.FactionManager.AllFactions
        //         where !x.def.hidden && !x.defeated && !x.IsPlayer && !x.HostileTo(Faction.OfPlayer) && this.CommonHumanlikeEnemyFactionExists(Faction.OfPlayer, x) && !this.AnyQuestExistsFrom(x)
        //         select x).TryRandomElement(out alliedFaction))
        //    {
        //        enemyFaction = this.CommonHumanlikeEnemyFaction(Faction.OfPlayer, alliedFaction);
        //        return true;
        //    }
        //    alliedFaction = null;
        //    enemyFaction = null;
        //    return false;
        //}

        //private bool CommonHumanlikeEnemyFactionExists(Faction f1, Faction f2)
        //{
        //    return this.CommonHumanlikeEnemyFaction(f1, f2) != null;
        //}

        //private Faction CommonHumanlikeEnemyFaction(Faction f1, Faction f2)
        //{
        //    Faction result;
        //    if ((from x in Find.FactionManager.AllFactions
        //         where x != f1 && x != f2 && !x.def.hidden && x.def.humanlikeFaction && !x.defeated && x.HostileTo(f1) && x.HostileTo(f2)
        //         select x).TryRandomElement(out result))
        //    {
        //        return result;
        //    }
        //    return null;
        //}
    }
}
