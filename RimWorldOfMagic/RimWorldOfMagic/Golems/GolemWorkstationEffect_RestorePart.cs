using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

using Verse.Sound;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class GolemWorkstationEffect_RestorePart : GolemWorkstationEffect
    {
        public int partsToReplace = 1;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.partsToReplace, "partsToReplace");
        }

        public override void StartEffect(Building_TMGolemBase golem_building, TM_GolemUpgrade upgrade, float effectLevel = 1)
        {
            FleckMaker.ThrowDustPuff(golem_building.Position, golem_building.Map, 1f);
            for (int i = 0 ;i < 4; i++)
            {
                Vector3 rndPos = golem_building.DrawPos;
                rndPos.x += Rand.Range(-.3f, .3f);
                rndPos.z += Rand.Range(-.3f, .3f);
                TM_MoteMaker.ThrowSparkFlashMote(rndPos, golem_building.Map, Rand.Range(.6f, 1.2f));
            }
            List<Hediff> hediffs = golem_building.GolemPawn?.health?.hediffSet?.hediffs;
            if (hediffs != null)
            {
                for (int i = 0; i < partsToReplace; i++)
                {
                    foreach (Hediff hd in hediffs)
                    {
                        if (hd.Part != null && hd is Hediff_MissingPart)
                        {
                            bool partRequiresUpgrade = false;
                            foreach (TM_GolemUpgrade gu in golem_building.Upgrades)
                            {
                                if (gu.golemUpgradeDef.bodypart == hd.Part.def && gu.golemUpgradeDef.partRequiresUpgrade)
                                {
                                    partRequiresUpgrade = true;
                                }
                            }
                            if (!partRequiresUpgrade)
                            {
                                if (hd.Part != null)
                                {
                                    Messages.Message("Restored " + hd.Part.Label + " on " + golem_building.GolemPawn.Label, MessageTypeDefOf.PositiveEvent);
                                }
                                golem_building.GolemPawn.health.RemoveHediff(hd);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public override bool CanDoEffect(Building_TMGolemBase golem_building)
        {
            List<Hediff> hediffs = golem_building.GolemPawn?.health?.hediffSet?.hediffs;
            if(hediffs != null)
            {
                foreach(Hediff hd in hediffs)
                {
                    if(hd.Part != null && hd is Hediff_MissingPart)
                    {
                        bool partRequiresUpgrade = false;
                        foreach (TM_GolemUpgrade gu in golem_building.Upgrades)
                        {
                            if (gu.golemUpgradeDef.bodypart == hd.Part.def && gu.golemUpgradeDef.partRequiresUpgrade)
                            {
                                partRequiresUpgrade = true;                                
                            }                            
                        }
                        if(!partRequiresUpgrade)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
