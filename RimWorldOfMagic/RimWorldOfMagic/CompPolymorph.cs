using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using AbilityUser;

namespace TorannMagic
{
    public class CompPolymorph : ThingComp
    {
        private Effecter effecter;
        private bool initialized;
        private bool temporary = false;
        private int ticksLeft;
        private int ticksToDestroy = 1800;
        public bool validSummoning = false;

        CompAbilityUserMagic compSummoner;
        Pawn spawner;
        Pawn original = null;

        List<float> bodypartDamage = new List<float>();
        List<DamageDef> bodypartDamageType = new List<DamageDef>();
        List<Hediff_Injury> injuries = new List<Hediff_Injury>();

        public Map activeMap = null;

        public CompProperties_Polymorph Props
        {
            get
            {
                return (CompProperties_Polymorph)this.props;
            }
        }

        public Pawn ParentPawn
        {
            get
            {
                Pawn pawn = this.parent as Pawn;
                bool flag = pawn == null;
                if (flag)
                {
                    Log.Error("pawn is null");
                }
                return pawn;
            }
            set => this.parent = value;
        }

        public Pawn Original
        {
            get => this.original;
            set => original = value;
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
            set
            {
                this.ticksLeft = value;
            }
        }        

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);            
        }

        private void SpawnSetup()
        {
            this.ticksLeft = this.ticksToDestroy;
            TransmutateEffects(ParentPawn.Position);
            if (this.original != null && this.spawner == this.original && this.original.Spawned)
            {
                bool drafter = this.original.Drafted;
                this.original.DeSpawn();
                if(drafter)
                {
                    this.ParentPawn.drafter.Drafted = true;
                }
                Find.Selector.Select(this.ParentPawn, false, true);
                
            }
        }

        public void CheckPawnState()
        {
            if (Find.TickManager.TicksGame % Rand.Range(30, 60) == 0 && this.ParentPawn.kindDef == PawnKindDef.Named("TM_Dire_Wolf"))
            {
                bool castSuccess = false;                
                AutoCast.AnimalBlink.Evaluate(this.ParentPawn, 2, 6, out castSuccess);                
            }

            if (this.ParentPawn.drafter == null)
            {
                this.ParentPawn.drafter = new Pawn_DraftController(this.ParentPawn);
            }
        }        

        public override void CompTick()
        {
            if (this.original != null)
            {
                base.CompTick();
                if (Find.TickManager.TicksGame % 4 == 0)
                {
                    if (!this.initialized)
                    {
                        this.initialized = true;
                        SpawnSetup();
                    }
                    this.activeMap = this.ParentPawn.Map;
                    bool flag2 = this.temporary;
                    if (flag2 && this.initialized)
                    {
                        this.ticksLeft -= 4;
                        bool flag3 = this.ticksLeft <= 0;
                        if (flag3)
                        {
                            this.PreDestroy();
                            ParentPawn.Destroy(DestroyMode.Vanish);                            
                        }
                        CheckPawnState();
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
                    else if(this.initialized && !flag2 && this.parent.Spawned)
                    {
                        CheckPawnState();
                    }
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            using (IEnumerator<Gizmo> enumerator = base.CompGetGizmosExtra().GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    Gizmo c = enumerator.Current;
                    yield return c;
                    /*Error: Unable to find new state assignment for yield return*/
                    ;
                }
            }
            if (this.original != null && this.original.Faction != null && this.spawner != null && this.spawner.Faction == this.original.Faction)
            {
                String label = "TM_CancelPolymorph".Translate();
                String desc = "TM_CancelPolymorphDesc".Translate();
                Command_Toggle item = new Command_Toggle
                {
                    defaultLabel = label,
                    defaultDesc = desc,
                    order = 109,
                    icon = ContentFinder<Texture2D>.Get("UI/Polymorph_cancel", true),
                    isActive = (() => true),
                    toggleAction = delegate
                    {
                        this.temporary = true;
                        this.ticksLeft = 1;
                    }
                };
                yield return (Gizmo)item;
            }
            yield break;          
        }

        public void PreDestroy()
        {
            if(this.original != null)
            {
                //CopyDamage(ParentPawn); removed for polymorph balance
                SpawnOriginal(ParentPawn.Map);
                ApplyDamage(original);
                this.original = null;
            }
        }

        public override void PostDeSpawn(Map map)
        {
            bool flag = this.effecter != null;
            if (flag)
            {
                this.effecter.Cleanup();
            }
            base.PostDeSpawn(map);
            
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.temporary, "temporary", false, false);
            Scribe_Values.Look<bool>(ref this.validSummoning, "validSummoning", true, false);
            Scribe_Values.Look<int>(ref this.ticksLeft, "ticksLeft", 0, false);
            Scribe_Values.Look<int>(ref this.ticksToDestroy, "ticksToDestroy", 1800, false);
            Scribe_Values.Look<CompAbilityUserMagic>(ref this.compSummoner, "compSummoner", null, false);
            Scribe_References.Look<Pawn>(ref this.spawner, "spawner", true);
            //Scribe_References.Look<Pawn>(ref this.original, "original", true);
            Scribe_Deep.Look<Pawn>(ref this.original, true, "original", new object[0]);
        }

        public void CopyDamage(Pawn pawn)
        {
            using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    BodyPartRecord rec = enumerator.Current;
                    IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                    Func<Hediff_Injury, bool> arg_BB_1;
                    arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                    foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                    {
                        bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                        if (flag5)
                        {
                            this.injuries.Add(current);
                        }                            
                    }                    
                }
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (this.ticksLeft > 0 && (this.parent.DestroyedOrNull() || ParentPawn.Dead))
            {
                DestroyParentCorpse(this.activeMap);
                SpawnOriginal(this.activeMap);
                original.Kill(null, null);
                this.Original = null;
            }            
            base.PostDestroy(mode, previousMap);
        }

        public void DestroyParentCorpse(Map map)
        {
            List<Thing> thingList = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            for (int i = 0; i < thingList.Count; i++)
            {
                Corpse parentCorpse = thingList[i] as Corpse;
                if (parentCorpse != null)
                {
                    Pawn innerPawn = parentCorpse.InnerPawn;
                    CompPolymorph compPoly = innerPawn.GetComp<CompPolymorph>();
                    if (innerPawn != null && compPoly != null)
                    {
                        if (compPoly.Original == this.original)
                        {
                            thingList[i].Destroy(DestroyMode.Vanish);
                            break;
                        }
                    }
                }
            }
        }

        public void SpawnOriginal(Map map)
        {
            bool drafter = this.ParentPawn.Drafted;
            bool selected = Find.Selector.IsSelected(this.ParentPawn);
            if (map != null)
            {
                GenSpawn.Spawn(this.original, ParentPawn.Position, map, WipeMode.Vanish);
                TransmutateEffects(ParentPawn.Position);
            }
            else
            {
                map = this.spawner.Map;
                GenSpawn.Spawn(this.original, ParentPawn.Position, map, WipeMode.Vanish);
                TransmutateEffects(ParentPawn.Position);
            }  
            if(drafter)
            {
                this.original.drafter.Drafted = true;
            }
            if (selected)
            {
                Find.Selector.Select(this.original, false, true);
            }
        }

        public void ApplyDamage(Pawn pawn)
        {
            List<BodyPartRecord> bodyparts = pawn.health.hediffSet.GetNotMissingParts().ToList();
            //Removed for polymorph balance
            //if (injuries != null)
            //{
            //    for (int i = 0; i < this.injuries.Count; i++)
            //    {
            //        try
            //        {
            //            pawn.health.AddHediff(this.injuries[i], bodyparts.RandomElement());
            //        }
            //        catch
            //        {
            //            //unable to add injury
            //        }
            //    }
            //}
            try
            {
                if (pawn.story != null && pawn.story.traits != null && pawn.story.traits.HasTrait(TraitDefOf.Transhumanist))
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(TorannMagicDefOf.Polymorphed_Transhumanist, this.spawner);
                }
                else if(this.spawner == this.original)
                {
                    //do not give bad thoughts
                }
                else
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(TorannMagicDefOf.Polymorphed, this.spawner);
                }
            }
            catch(NullReferenceException ex)
            {

            }
        }

        public void TransmutateEffects(IntVec3 position)
        {
            Vector3 rndPos = position.ToVector3Shifted();
            FleckMaker.ThrowHeatGlow(position, this.ParentPawn.Map, 1f);
            for (int i = 0; i < 6; i++)
            {
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(-.5f, .5f);
                rndPos.y += Rand.Range(.3f, 1.3f);
                FleckMaker.ThrowSmoke(rndPos, this.ParentPawn.Map, Rand.Range(.7f, 1.1f));
                FleckMaker.ThrowLightningGlow(position.ToVector3Shifted(), this.ParentPawn.Map, 1.4f);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            //initializes after reload
            base.PostSpawnSetup(respawningAfterLoad);
        }

    }
}
