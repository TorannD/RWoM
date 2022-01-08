using Verse;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using RimWorld;
using TorannMagic.Golems;

namespace TorannMagic.Golems
{
    public class Verb_GolemShoot : Verb_Shoot
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if ((root - targ.Cell).LengthHorizontal < this.verbProps.minRange)
            {
                return false;
            }
            return base.CanHitTargetFrom(root, targ);
        }

        protected override bool TryCastShot()
        {
            TMPawnGolem pg = this.CasterPawn as TMPawnGolem;
            if(pg != null)
            {
                pg.drawTickFlag = false;
            }
            return base.TryCastShot();
        }
    }
}
