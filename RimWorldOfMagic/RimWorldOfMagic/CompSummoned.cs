using Verse;
using RimWorld;
using UnityEngine;

namespace TorannMagic
{
    public class CompSummoned : ThingComp
    {
        public int ticksToDestroy = 3600;

        private Effecter effecter;

        private bool initialized = false;

        private bool temporary;
        public bool sustained = false;

        private int ticksLeft;

        CompAbilityUserMagic compSummoner;
        Pawn spawner;

        public CompProperties_Summoned Props
        {
            get
            {
                return (CompProperties_Summoned)this.props;
            }
        }
        public Pawn Spawner
        {
            get => this.spawner;
            set => spawner = value;
        }

        public CompAbilityUserMagic CompSummoner
        {
            get
            {
                return spawner.GetCompAbilityUserMagic();
            }
        }

        public bool Temporary
        {
            get
            {
                return this.temporary;
            }
            set
            {
                this.temporary = value;
            }
        }

        public int TicksToDestroy
        {
            get
            {
                return this.ticksToDestroy;
            }
            set
            {
                ticksToDestroy = value;
            }
        }

        public int TicksLeft
        {
            get
            {
                return this.ticksLeft;
            }
        }

        private Thing Thing
        {
            get
            {
                Thing thing = this.parent as Thing;
                bool flag = thing == null;
                if (flag)
                {
                    Log.Error("pawn is null");
                }
                return thing;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            Pawn pawn = this.parent as Pawn;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.temporary, "temporary", false, false);
            Scribe_Values.Look<bool>(ref this.sustained, "sustained", false, false);
            Scribe_Values.Look<int>(ref this.ticksLeft, "ticksLeft", 0, false);
            Scribe_Values.Look<int>(ref this.ticksToDestroy, "ticksToDestroy", 3600, false);
            Scribe_Values.Look<CompAbilityUserMagic>(ref this.compSummoner, "compSummoner", null, false);
            Scribe_References.Look<Pawn>(ref this.spawner, "spawner", false);
        }       

        public void SpawnSetup()
        {
            this.ticksLeft = this.ticksToDestroy;
        }

        public override void CompTick()
        {
            base.CompTick();
            if(!initialized)
            {
                SpawnSetup();
                this.initialized = true;
            }
            if (Find.TickManager.TicksGame % 10 == 0)
            {
                bool flag2 = this.temporary;
                if (flag2)
                {
                    this.ticksLeft -= 10;
                    bool flag3 = this.ticksLeft <= 0;
                    if (flag3)
                    {
                        this.PreDestroy();
                        this.parent.Destroy(DestroyMode.Vanish);
                    }
                    bool spawned = this.parent.Spawned;
                    if (spawned)
                    {
                        bool flag4 = this.effecter == null;
                        if (flag4)
                        {
                            EffecterDef progressBar = EffecterDefOf.ProgressBar;
                            this.effecter = progressBar.Spawn();
                        }
                        else
                        {
                            LocalTargetInfo localTargetInfo = this.parent;
                            bool spawned2 = base.parent.Spawned;
                            if (spawned2)
                            {
                                this.effecter.EffectTick(this.parent, TargetInfo.Invalid);
                            }
                            MoteProgressBar mote = ((SubEffecter_ProgressBar)this.effecter.children[0]).mote;
                            bool flag5 = mote != null;
                            if (flag5)
                            {
                                float value = 1f - (float)(this.TicksToDestroy - this.ticksLeft) / (float)this.TicksToDestroy;
                                mote.progress = Mathf.Clamp01(value);
                                mote.offsetZ = -0.5f;
                            }
                        }
                    }
                }
                if(this.sustained)
                {
                    if (this.spawner != null && !this.spawner.Destroyed)
                    {
                        if (this.spawner.Dead)
                        {
                            if (this.parent != null && !this.parent.Destroyed)
                            {
                                Messages.Message("TM_SunlightCollapse".Translate(
                                    this.spawner.LabelShort
                                ), MessageTypeDefOf.NeutralEvent);
                                this.parent.Destroy();                                
                            }
                        }
                    }
                    else
                    {
                        //spawner shouldn't be null if it's sustained, destroy object if that's the case
                        if (this.parent != null && !this.parent.Destroyed)
                        {
                            Log.Message("A sunlight has been destroyed, spawner found to be null.");
                            this.parent.Destroy();                            
                        }
                    }
                }
            }
        }

        public void PreDestroy()
        {
            try
            {
                //bool flag = this.effecter != null;
                //if (flag)
                //{
                //    this.effecter.Cleanup();
                //}     

                FleckMaker.ThrowSmoke(this.parent.Position.ToVector3(), this.parent.Map, 1);
                FleckMaker.ThrowHeatGlow(this.parent.Position, this.parent.Map, 1);
                
                if (parent.def.defName.Contains("TM_ManaMine"))
                {
                    Messages.Message("MineDeSpawn".Translate(), MessageTypeDefOf.SilentInput);
                }
                if (parent.def.defName.Contains("DefensePylon"))
                {
                    Messages.Message("PylonDeSpawn".Translate(), MessageTypeDefOf.SilentInput);
                }
            }
            catch
            {
                Log.Message("TM_ExceptionClose".Translate(
                            this.parent.def.defName
                ));
            }
        }

        
        public override void PostDeSpawn(Map map)
        {
            bool flag = this.effecter != null && this.effecter.children != null;
            if (flag)
            {
                this.effecter.Cleanup();
            }
            base.PostDeSpawn(map);
        }

    }
}
