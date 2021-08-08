using RimWorld;
using System;
using Verse;
using AbilityUser;

namespace TorannMagic
{
    public class Spirit_DeathWorker : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse)
        {
            
            if (corpse.Map != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    FleckMaker.ThrowSmoke(corpse.DrawPos, corpse.Map, Rand.Range(.5f, 1.1f));
                }
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ghost, corpse.DrawPos, corpse.Map, 1.3f, .25f, .1f, .45f, 0, Rand.Range(1f, 2f), 0, 0);
            }
            Pawn innerPawn = corpse.InnerPawn;
            innerPawn.SetFaction(Find.FactionManager.FirstFactionOfDef(TorannMagicDefOf.TM_SkeletalFaction), null);
            corpse.Destroy();           
        }
    }
}

