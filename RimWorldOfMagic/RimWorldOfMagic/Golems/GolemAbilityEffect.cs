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
    public class GolemAbilityEffect
    {       

        public virtual void DoEffect(Pawn launcher, LocalTargetInfo target)
        {
            
        }

        public virtual bool CanDoEffect(LocalTargetInfo target)
        {
            if(target.Thing.DestroyedOrNull())
            {
                return false;
            }
            if(!target.Thing.Spawned)
            {
                return false;
            }
            if(target.Thing is Pawn)
            {
                if(target.Pawn.Dead)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
