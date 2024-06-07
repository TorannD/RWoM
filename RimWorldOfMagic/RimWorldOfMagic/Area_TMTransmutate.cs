using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    public class Area_TMTransmutate : Area
    {
        public override string Label => "transmutate";
        public override Color Color => new Color(0.4f, 0.55f, 1f);
        public override int ListPriority => 3000;

        public Area_TMTransmutate()
        {
        }

        public Area_TMTransmutate(AreaManager areaManager)
            : base(areaManager)
        {
        }

        public override bool AssignableAsAllowed()
        {
            return true;
        }

        public override string GetUniqueLoadID()
        {
            return "Area_" + ID + "_TMTransmutate";
        }

    }
}
