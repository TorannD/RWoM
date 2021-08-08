using RimWorld;
using System;
using Verse;
using System.Collections.Generic;

namespace TorannMagic
{
    public class DeathWorker_Spirit : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse)
        {            
            for (int i = 0; i < 3; i++)
            {
                FleckMaker.ThrowSmoke(corpse.DrawPos, corpse.Map, Rand.Range(.5f, 1.1f));
            }
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ghost, corpse.DrawPos, corpse.Map, 1f, .25f, 0f, .25f, 0, Rand.Range(2f, 3f), 0, 0);
            Pawn innerPawn = corpse.InnerPawn;
            innerPawn.SetFaction(Faction.OfAncients, null);
            corpse.Destroy();            
        }
    }
}
