using RimWorld;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Verse.Sound;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class Verb_ControlSpiritStorm : Verb_BLOS 
    {
        public Map map = null;
        protected override bool TryCastShot()
        {
            Pawn p = this.CasterPawn;
            map = this.CasterPawn.Map;
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
            List<FlyingObject_SpiritStorm> storms = GetActiveStorms();
            if(storms != null)
            {
                foreach(FlyingObject_SpiritStorm ss in storms)
                {
                    ss.PlayerTargetSet = true;
                    ss.ManualDestination = this.currentTarget.CenterVector3;
                }
            }
            this.burstShotsLeft = 0;
            return true;
        }


        private List<FlyingObject_SpiritStorm> GetActiveStorms()
        {
            if (this.map != null)
            {
                IEnumerable<FlyingObject_SpiritStorm> storm = from def in this.map.listerThings.AllThings
                                                              where (def.Spawned && def is FlyingObject_SpiritStorm)
                                                              select def as FlyingObject_SpiritStorm;
                if(storm != null && storm.Count() > 0)
                {
                    return storm.ToList(); ;
                }
            }
            return null;                                 
        }
    }
}
