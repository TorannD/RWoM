using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class TMPawnSummoned : Pawn
    {
        private Effecter effecter;
        private bool initialized;
        private bool temporary;
        private int ticksLeft;
        private int ticksToDestroy = 1800;
        public bool validSummoning;

        CompAbilityUserMagic compSummoner;
        Pawn spawner;
        Pawn original;

        List<Hediff_Injury> injuries = new List<Hediff_Injury>();

        public Pawn Original
        {
            get => original;
            set => original = value;
        }

        public Pawn Spawner
        {
            get => spawner;
            set => spawner = value;
        }

        public CompAbilityUserMagic CompSummoner => spawner.GetCompAbilityUserMagic();

        public bool Temporary
        {
            get => temporary;
            set => temporary = value;
        }

        public int TicksToDestroy
        {
            get => ticksToDestroy;
            set => ticksToDestroy = value;
        }

        public int TicksLeft
        {
            get => ticksLeft;
            set => ticksLeft = value;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            ticksLeft = ticksToDestroy;
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public void PostSummonSetup()
        {
            if (!validSummoning)
            {
                Destroy();
            }
        }

        public void CheckPawnState()
        {
            if (def.race.body.defName == "Minion")
            if (def.race.body.defName == "Minion")
            {
                try
                {
                    if (Downed && !Destroyed && Faction == Faction.OfPlayer)
                    {
                        Messages.Message("MinionFled".Translate(), MessageTypeDefOf.NeutralEvent);
                        FleckMaker.ThrowSmoke(Position.ToVector3(), Map, 1);
                        FleckMaker.ThrowHeatGlow(Position, Map, 1);
                        CompSummoner?.summonedMinions.Remove(this);
                        Destroy();
                    }
                }
                catch
                {
                    Log.Message("TM_ExceptionTick".Translate(
                        def.defName
                    ));
                    Destroy();
                }
            }
        }

        public override void Tick()
        {
            if (!Spawned || Map == null) return;

            base.Tick();
            if (Find.TickManager.TicksGame % 10 != 0) return;

            if (!initialized)
            {
                initialized = true;
                PostSummonSetup();
            }
            if (temporary)
            {
                ticksLeft -= 10;
                if (ticksLeft <= 0)
                {
                    PreDestroy();
                    if (!Destroyed)
                    {
                        Destroy();
                        return;
                    }
                }
                CheckPawnState();

                if (effecter == null)
                {
                    EffecterDef progressBar = EffecterDefOf.ProgressBar;
                    effecter = progressBar.Spawn();
                }
                else
                {
                    effecter.EffectTick(this, TargetInfo.Invalid);
                    MoteProgressBar mote = ((SubEffecter_ProgressBar)effecter.children[0]).mote;
                    if (mote != null)
                    {
                        mote.progress = Mathf.Clamp01(1f - (float)(TicksToDestroy - ticksLeft) / TicksToDestroy);
                        mote.offsetZ = -0.5f;
                    }
                }
                if (Find.TickManager.TicksGame % 120 == 0)
                {
                    CheckAndTrain();
                }
            }
        }

        public void PreDestroy()
        {
            if (carryTracker?.CarriedThing != null)
            {
                carryTracker.TryDropCarriedThing(Position, ThingPlaceMode.Near, out _);
            }
            if (def.defName == "TM_MinionR" || def.defName == "TM_GreaterMinionR")
            {
                try
                {
                    if (Map != null)
                    {
                        FleckMaker.ThrowSmoke(Position.ToVector3(), Map, 3);
                    }
                    else
                    {
                        holdingOwner.Remove(this);
                    }

                    CompSummoner?.summonedMinions.Remove(this);
                }
                catch
                {
                    Log.Message("TM_ExceptionClose".Translate(def.defName));
                }
            }
            if(def == TorannMagicDefOf.TM_SpiritWolfR || def == TorannMagicDefOf.TM_SpiritBearR
               || def == TorannMagicDefOf.TM_SpiritCrowR || def == TorannMagicDefOf.TM_SpiritMongooseR)
            {
                try
                {
                    if(Map != null)
                    {
                        FleckMaker.ThrowSmoke(DrawPos, Map, Rand.Range(1f, 3f));
                        FleckMaker.ThrowSmoke(DrawPos, Map, Rand.Range(1f, 2f));
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ghost, DrawPos, Map, 1f, .25f, 0f, .25f, 0, Rand.Range(1f, 2f), 0, 0);
                    }
                    else if(holdingOwner != null && holdingOwner.Contains(this))
                    {
                        holdingOwner.Remove(this);
                    }                
                }
                catch
                {
                    Log.Message("TM_ExceptionClose".Translate(def.defName));
                }
            }
            if(original != null)
            {
                //Log.Message("pre destroy");
                CopyDamage(this);
                SpawnOriginal();
                ApplyDamage(original);
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            effecter?.Cleanup();
            base.DeSpawn(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref temporary, "temporary");
            Scribe_Values.Look<bool>(ref validSummoning, "validSummoning", true);
            Scribe_Values.Look<int>(ref ticksLeft, "ticksLeft");
            Scribe_Values.Look<int>(ref ticksToDestroy, "ticksToDestroy", 1800);
            Scribe_Values.Look<CompAbilityUserMagic>(ref compSummoner, "compSummoner");
            Scribe_References.Look<Pawn>(ref spawner, "spawner");
            Scribe_References.Look<Pawn>(ref original, "original");
        }

        public void CopyDamage(Pawn pawn)
        {
            IEnumerable<Hediff_Injury> injuriesToCopy = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury.CanHealNaturally());
            injuries.AddRange(injuriesToCopy);
        }

        public void SpawnOriginal()
        {
            GenSpawn.Spawn(original, Position, Map);
        }

        public void ApplyDamage(Pawn pawn)
        {
            List<BodyPartRecord> bodyParts = pawn.health.hediffSet.GetNotMissingParts().ToList();
            for(int i =0; i < injuries.Count; i++)
            {
                pawn.health.AddHediff(injuries[i], bodyParts.RandomElement());
            }
        }

        public void CheckAndTrain()
        {
            if (training != null && training.CanBeTrained(TrainableDefOf.Tameness))
            {
                while (!training.HasLearned(TrainableDefOf.Tameness))
                {
                    training.Train(TrainableDefOf.Tameness, null);
                }
            }
        }
    }
}
