using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    public class Area_TMRegrowth : Area
    {
        public override string Label => "regrowth seed";
        public override Color Color => new Color(0f, 0.8f, 0f);
        public override int ListPriority => 3000;

        public Area_TMRegrowth()
        {
        }

        public Area_TMRegrowth(AreaManager areaManager)
            : base(areaManager)
        {
        }

        public override bool AssignableAsAllowed()
        {
            return true;
        }

        public override string GetUniqueLoadID()
        {
            return "Area_" + ID + "_TMRegrowth";
        }

    }
}
