using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace TorannMagic.Golems
{
    public class UnfinishedNoProductThing : UnfinishedThing
    {
        public override string LabelNoCount
        {
            get
            {
                if (Recipe == null)
                {
                    return base.LabelNoCount;
                }
                if(Recipe.products == null)
                {
                    return "TM_UnfinishedThingNoProduct".Translate(Recipe.label);
                }
                if (Recipe.products.Count < 1)
                {
                    return "TM_UnfinishedThingNoProduct".Translate(Recipe.label);
                }
                return base.LabelNoCount;
            }
        }
    }
}
