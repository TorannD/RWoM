using AbilityUser;
using RimWorld;
using Verse;

namespace TorannMagic
{
    public class Verb_SummonGuardianSpirit : ClassProgression.Verb_SpiritTargetable
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            var map = CasterPawn?.Map;
            return map != null && !target.HasThing && target.IsValid && target.Cell.InBounds(map) &&
                !target.Cell.Fogged(map) && target.Cell.Standable(map) && target.Cell.GetFirstPawn(map) == null &&
                (root-target.Cell).LengthHorizontal < verbProps.range && TryFindShootLineFromTo(root,target,out _);
        }

        protected override bool TryCastShot()
        {
            burstShotsLeft = 0;
            var body = CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body,Ability);
            var comp = owner?.GetCompAbilityUserMagic();
            if (body?.Spawned != true || comp == null || !CanHitTargetFrom(body.Position,currentTarget)) return false;
            var previous = comp.bondedSpirit;
            if (previous != null && !previous.Destroyed && !previous.Dead && !ClassProgression.ShamanLifecycle.OwnsGuardian(comp)) return false;
            var type = comp.GuardianSpiritType;
            var kind = type == TorannMagicDefOf.TM_SpiritBearR ? "TM_SpiritBear" :
                type == TorannMagicDefOf.TM_SpiritMongooseR ? "TM_SpiritMongoose" :
                type == TorannMagicDefOf.TM_SpiritCrowR ? "TM_SpiritCrow" : null;
            var kindDef = kind == null ? null : DefDatabase<PawnKindDef>.GetNamedSilentFail(kind);
            if (kindDef == null || kindDef.race != type) return false;
            int versatility = TM_Calc.GetSkillVersatilityLevel(owner,TorannMagicDefOf.TM_GuardianSpirit);
            var config = new SpawnThings { def=type, kindDef=kindDef, spawnCount=1 };
            var guardian = TM_Action.SingleSpawnLoop(owner,config,currentTarget.Cell,body.Map,5,false,false,owner.Faction,false) as TMPawnSummoned;
            if (guardian == null) return false;
            try
            {
                var controller = guardian.TryGetComp<CompAnimalController>();
                if (!guardian.Spawned || controller == null) return false;
                controller.summonerPawn = owner;
                TM_Action.TrainAnimalFull(guardian,owner);
                var bond = guardian.health.AddHediff(TorannMagicDefOf.TM_SpiritBondHD);
                var link = bond?.TryGetComp<HediffComp_RangerBond>();
                if (link == null) return false;
                link.bonderPawn = owner;
                bond.Severity = .5f + versatility;
                if (type == TorannMagicDefOf.TM_SpiritCrowR)
                    HealthUtility.AdjustSeverity(guardian,TorannMagicDefOf.TM_BirdflightHD,.5f);
                if (comp.bondedSpirit != previous) return false;
                if (previous != null && !previous.Destroyed && !previous.Dead &&
                    !ClassProgression.ShamanLifecycle.ReleaseGuardian(comp)) return false;
                comp.bondedSpirit = guardian;
            }
            finally
            {
                if (comp.bondedSpirit != guardian && !guardian.Destroyed) guardian.Destroy();
            }
            comp.SyncGuardianControl();
            for (int i=0;i<3;i++)
            {
                var pos=guardian.Position.ToVector3Shifted();
                pos.x+=Rand.Range(-.5f,.5f);pos.z+=Rand.Range(-.5f,.5f);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Healing_Small,pos,body.Map,Rand.Range(.6f,.8f),.1f,.05f,.05f,0,0,0,Rand.Range(0,360));
                FleckMaker.ThrowSmoke(pos,body.Map,Rand.Range(.8f,1.2f));
            }
            return true;
        }
    }
}
