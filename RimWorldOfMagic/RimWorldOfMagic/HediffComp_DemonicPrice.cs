using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_DemonicPrice : HediffComp
    {
        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }


        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn.DestroyedOrNull();
            if (!flag)
            {
                if (base.Pawn.Spawned && !base.Pawn.Dead)
                {
                    if (Find.TickManager.TicksGame % 4 == 0)
                    {
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ArcaneFlame, this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.2f, .3f), .1f, .05f, .2f, 0, Rand.Range(1.5f, 2f), Rand.Range(-60, 60), 0);
                    }    
                    if(Find.TickManager.TicksGame % 60 == 0)
                    {
                        DamageEntities(this.Pawn, Rand.Range(4, 8), DamageDefOf.Flame);
                    }
                }
            }
        }

        public void DamageEntities(Thing e, float d, DamageDef type)
        {
            int amt = Mathf.RoundToInt(Rand.Range(.75f, 1.25f) * d);
            DamageInfo dinfo = new DamageInfo(type, amt, 0, (float)-1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
            }
        }
    }
}
