using RimWorld;
using System;
using Verse;

namespace TorannMagic
{
    public class DeathWorker_Poppi : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse)
        {
            float radius = 2f;
            CompLeaper comp = corpse.InnerPawn.GetComp<CompLeaper>();
            if (comp != null)
            {
                radius = comp.explosionRadius;
            }
            bool flag = corpse.InnerPawn.ageTracker.CurLifeStageIndex == 0;
            if (flag)
            {
                radius = radius * Rand.Range(.8f, 1f);
            }
            else
            {
                bool flag2 = corpse.InnerPawn.ageTracker.CurLifeStageIndex == 1;
                if (flag2)
                {
                    radius = radius * Rand.Range(1.2f, 1.8f); 
                }
                else
                {
                    radius = radius * Rand.Range(1.5f, 2f);
                }
            }
            GenExplosion.DoExplosion(corpse.Position, corpse.Map, radius, DamageDefOf.Burn, corpse.InnerPawn, Rand.Range(12, 16));
        }
    }
}
