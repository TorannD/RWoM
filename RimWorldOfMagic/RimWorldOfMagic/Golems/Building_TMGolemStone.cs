using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using TorannMagic.TMDefs;
using AbilityUser;
using Verse.Sound;
using HarmonyLib;

namespace TorannMagic.Golems
{
    public class Building_TMGolemStone : Building_TMGolemBase
    {
        public ThingDef madeFromChunk = null;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<ThingDef>(ref madeFromChunk, "madeFromChunk");
        }
        public override ThingDef GetGolemThingDef
        {
            get
            {
                if (this.Stuff != null)
                {
                    if (Stuff.defName == "BlocksSandstone")
                    {
                        madeFromChunk = ThingDef.Named("ChunkSandstone");
                        return TorannMagicDefOf.TM_SandstoneGolem;
                    }
                    else if (Stuff.defName == "BlocksGranite")
                    {
                        madeFromChunk = ThingDef.Named("ChunkGranite");
                        return TorannMagicDefOf.TM_GraniteGolem;
                    }
                    else if (Stuff.defName == "BlocksLimestone")
                    {
                        madeFromChunk = ThingDef.Named("ChunkLimestone");
                        return TorannMagicDefOf.TM_LimestoneGolem;
                    }
                    else if (Stuff.defName == "BlocksSlate")
                    {
                        madeFromChunk = ThingDef.Named("ChunkSlate");
                        return TorannMagicDefOf.TM_SlateGolem;
                    }
                    else if (Stuff.defName == "BlocksMarble")
                    {
                        madeFromChunk = ThingDef.Named("ChunkMarble");
                        return TorannMagicDefOf.TM_MarbleGolem;
                    }
                }
                return TorannMagicDefOf.TM_StoneGolem;
            }
        }

        public override PawnKindDef GetGolemKindDef
        {
            get
            {
                if (this.Stuff != null)
                {
                    if (Stuff.defName == "BlocksSandstone")
                    {
                        return TorannMagicDefOf.TM_SandstoneGolemK;
                    }
                    else if (Stuff.defName == "BlocksGranite")
                    {
                        return TorannMagicDefOf.TM_GraniteGolemK;
                    }
                    else if (Stuff.defName == "BlocksLimestone")
                    {
                        return TorannMagicDefOf.TM_LimestoneGolemK;
                    }
                    else if (Stuff.defName == "BlocksSlate")
                    {
                        return TorannMagicDefOf.TM_SlateGolemK;
                    }
                    else if (Stuff.defName == "BlocksMarble")
                    {
                        return TorannMagicDefOf.TM_MarbleGolemK;
                    }
                }
                return TorannMagicDefOf.TM_StoneGolemK;
            }
        }

        public override void DoGolemWorkingEffect()
        {
            if (Find.TickManager.TicksGame % 41 == 0)
            {
                Vector3 effectPos = this.DrawPos;
                effectPos.x += Rand.Range(-.3f, .3f);
                effectPos.z += .6f;
                TM_MoteMaker.ThrowGenericFleck(FleckDefOf.DustPuffThick, effectPos, this.Map, Rand.Range(.5f, .8f), .4f, Rand.Range(0, .1f), Rand.Range(.4f, .6f), Rand.Range(10, 30), Rand.Range(.1f, .2f), Rand.Range(-10, 10), Rand.Range(0, 360));
            }
        }

    }
}
