using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    public class Area_TMSprite : Area
    {
        public override string Label => "earth sprites";
        public override Color Color => new Color(0.4f, 0.2f, 0f);
        public override int ListPriority => 3000;

        public Area_TMSprite()
        {
        }

        public Area_TMSprite(AreaManager areaManager)
            : base(areaManager)
        {
        }

        public override bool AssignableAsAllowed()
        {
            return true;
        }

        public override string GetUniqueLoadID()
        {
            return "Area_" + ID + "_TMSprite";
        }

    }
}
