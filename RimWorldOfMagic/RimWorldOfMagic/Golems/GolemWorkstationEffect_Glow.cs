using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

using Verse.Sound;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class GolemWorkstationEffect_Glow : GolemWorkstationEffect
    {
        public float glowRadius;
        public ColorInt glowColor;

        public override void StartEffect(Building_TMGolemBase golem_building, TM_GolemUpgrade upgrade, float effectLevel = 1)
        {
            golem_building.InitializeGlower(glowColor, glowRadius);
        }

        public override bool CanDoEffect(Building_TMGolemBase golem_building)
        {
            if (golem_building != null && golem_building.Map != null)
            {
                return golem_building.glower is null;
            }
            return false;
        }
    }
}
