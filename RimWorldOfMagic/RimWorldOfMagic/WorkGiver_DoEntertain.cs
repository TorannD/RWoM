using System;
using Verse;
using Verse.AI;
using RimWorld;
using System.Linq;


namespace TorannMagic
{
    public class WorkGiver_DoEntertain : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.Touch;
            }
        }

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.Pawn);
            }
        }

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Pawn recipient) || recipient == pawn || !recipient.RaceProps.Humanlike || !recipient.IsColonist ||
                !recipient.Awake() || recipient.Drafted || pawn.Drafted || recipient.Downed || !recipient.CanCasuallyInteractNow())
                return false;
            var owner = ClassProgression.BardTrials.EntertainmentOwner(pawn);
            if (owner == null || owner.GetCompAbilityUserMagic().nextEntertainTick >= Find.TickManager.TicksGame ||
                (pawn.Position - recipient.Position).LengthHorizontal >= 50f || GenAI.EnemyIsNear(recipient, 40f) ||
                !pawn.CanReserve(recipient, 1, -1, null, forced)) return false;
            var memories = recipient.needs?.mood?.thoughts?.memories?.Memories;
            if (memories == null) return false;
            if (BreakRiskAlertUtility.PawnsAtRiskExtreme.Contains(recipient))
                return !memories.Any(m => m.def.defName == "TM_EntertainedTD" && m.MoodOffset() > 11f);
            return (BreakRiskAlertUtility.PawnsAtRiskMajor.Contains(recipient) ||
                BreakRiskAlertUtility.PawnsAtRiskMinor.Contains(recipient)) &&
                !memories.Any(m => m.def.defName == "TM_EntertainedTD");
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var owner = ClassProgression.BardTrials.EntertainmentOwner(pawn);
            return owner == null ? null : JobMaker.MakeJob(TorannMagicDefOf.JobDriver_Entertain, t, owner);

        }
    }
}
