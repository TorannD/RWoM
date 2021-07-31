using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_NightshadeToxin : HediffComp
    {

        private bool removeNow = false;

        private int eventFrequency = 60;       

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null && base.Pawn.Map != null;
            if (flag)
            {
                if (Find.TickManager.TicksGame % this.eventFrequency == 0)
                {
                    if (!Pawn.Dead)
                    {
                        if (Pawn.RaceProps != null && !Pawn.RaceProps.IsMechanoid)
                        {
                            float sev = this.parent.Severity;
                            if (sev > 1f)
                            {
                                List<BodyPartRecord> insideParts = new List<BodyPartRecord>();
                                for (int i = 0; i < this.Pawn.RaceProps.body.AllParts.Count; i++)
                                {
                                    BodyPartRecord part = this.Pawn.RaceProps.body.AllParts[i];
                                    if (part.depth == BodyPartDepth.Inside)
                                    {
                                        insideParts.AddDistinct(part);
                                    }
                                }
                                if (insideParts.Count > 0 && this.parent.Severity >= 3)
                                {
                                    TM_Action.DamageEntities(this.Pawn, insideParts.RandomElement(), this.parent.Severity / 2f, TMDamageDefOf.DamageDefOf.TM_Toxin, null);
                                }
                            }
                        }
                    }
                    else
                    {
                        this.removeNow = true;
                    }
                } 
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return this.removeNow || base.CompShouldRemove;
            }
        }        
    }
}
