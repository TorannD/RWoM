using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;

namespace TorannMagic
{
    public class Verb_BowTraining : Verb_UseAbility
    {       
        protected override bool TryCastShot()
        {
            Map map = base.CasterPawn.Map;
            Pawn pawn = base.CasterPawn;
            CompAbilityUserMight comp = pawn.GetComp<CompAbilityUserMight>();
            MightPowerSkill pwr = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_BowTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_BowTraining_pwr");

            if (pawn != null && !pawn.Dead)
            {
                if (comp.IsMightUser)
                {
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_BowTrainingHD, -5f);
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_BowTrainingHD, (.5f) + pwr.level);
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    if (!pawn.IsColonist && settingsRef.AIHardMode)
                    {
                        HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_BowTrainingHD, 4);
                    }

                    FleckMaker.ThrowHeatGlow(pawn.Position, map, 1.5f);
                    FleckMaker.ThrowAirPuffUp(pawn.Position.ToVector3(), map);
                }
                else
                {
                    Log.Message("Pawn not detected as might user.");
                }
            }

            this.burstShotsLeft = 0;
            return false;
        }
    }
}
