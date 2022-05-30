using RimWorld;
using Verse;

namespace TorannMagic.Enchantment
{
    [StaticConstructorOnStartup]
    class HediffComp_WinterChill : HediffComp_EnchantedItem
    {        

        public override void PostInitialize()
        {
            this.hediffActionRate = 300;
        }

        public override void HediffActionTick()
        {
            if (Pawn.health.hediffSet.HasHediff(HediffDefOf.Heatstroke))
            {
                Hediff hd = Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
                Pawn.health.RemoveHediff(hd);
            }
            if (Pawn.Map != null && Pawn.Spawned)
            {
                GenTemperature.PushHeat(Pawn.Position, Pawn.MapHeld, -13f);
            }
        }        
    }
}
