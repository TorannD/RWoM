using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using GiddyUpCore;
using GiddyUpCore.Storage;

namespace TorannMagic.ModCheck
{
    public static class GiddyUp
    {
        public static void ForceDismount(Pawn pawn)
        {
            ExtendedPawnData epd = Base.Instance.GetExtendedDataStorage().GetExtendedDataFor(pawn);
            if (epd != null && epd.mount != null)
            {
                epd.mount.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, true);
            }
        }

        public static bool IsMount(Pawn pawn)
        {
            if (pawn.CurJobDef.defName == "Mounted")
            {
                return true;
            }
            return false;
        }

        public static Pawn GetMount(Pawn rider)
        {
            Pawn mount = null;
            ExtendedPawnData epd = Base.Instance.GetExtendedDataStorage().GetExtendedDataFor(rider);
            if (epd != null && epd.mount != null && epd.mount.CurJobDef.defName == "Mounted")
            {
                mount = epd.mount;
            }
            return mount;
        }
    }
}
