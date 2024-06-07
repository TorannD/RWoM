using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class CompPolymorph : ThingComp
    {

        private CompAbilityUserMagic compSummoner;
        private Pawn spawner;
        private Pawn original;

        private Effecter effecter;
        private bool initialized;
        private bool temporary;
        private int ticksLeft;
        private int ticksToDestroy = 1800;
        private bool validSummoning;
        private Map activeMap;

        //currently unused
        //List<float> bodypartDamage = new List<float>();
        //List<DamageDef> bodypartDamageType = new List<DamageDef>();
        //List<Hediff_Injury> injuries = new List<Hediff_Injury>();

        public CompProperties_Polymorph Props => (CompProperties_Polymorph)props;

        public Pawn ParentPawn
        {
            get
            {
                Pawn pawn = this.parent as Pawn;
                if (pawn == null)
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

        private void CheckPawnState()
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
            if (this.original == null) return;
            base.CompTick();
            if (Find.TickManager.TicksGame % 4 != 0) return;
            if (!initialized)
            {
                initialized = true;
                SpawnSetup();
            }
            activeMap = ParentPawn.Map;
            if (temporary && initialized)
            {
                ticksLeft -= 4;
                if (ticksLeft <= 0)
                {
                    PreDestroy();
                    ParentPawn.Destroy();
                }
                CheckPawnState();
                if (parent.Spawned)
                {
                    if (effecter == null)
                    {
                        EffecterDef progressBar = EffecterDefOf.ProgressBar;
                        effecter = progressBar.Spawn();
                    }
                    else
                    {
                        effecter.EffectTick(parent, TargetInfo.Invalid);
                        MoteProgressBar mote = ((SubEffecter_ProgressBar)effecter.children[0]).mote;
                        if (mote != null)
                        {
                            float value = 1f - (float)(TicksToDestroy - ticksLeft) / TicksToDestroy;
                            mote.progress = Mathf.Clamp01(value);
                            mote.offsetZ = -0.5f;
                        }                        
                    }
                }
            }
            else if (initialized && !temporary && parent.Spawned)
            {
                CheckPawnState();
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;

            if (original?.Faction == null || spawner == null || spawner.Faction != original.Faction) yield break;

            String label = "TM_CancelPolymorph".Translate();
            String desc = "TM_CancelPolymorphDesc".Translate();
            Command_Toggle item = new Command_Toggle
            {
                defaultLabel = label,
                defaultDesc = desc,
                Order = 109,
                icon = ContentFinder<Texture2D>.Get("UI/Polymorph_cancel"),
                isActive = () => true,
                toggleAction = delegate
                {
                    temporary = true;
                    ticksLeft = 1;
                }
            };
            yield return item;            
        }

        private void PreDestroy()
        {
            if (this.original == null) return;

            //CopyDamage(ParentPawn); removed for polymorph balance
            SpawnOriginal(ParentPawn.Map);
            ApplyDamage(original);
            this.original = null;            
        }

        public override void PostDeSpawn(Map map)
        {
            this.effecter?.Cleanup();            
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
            Scribe_Deep.Look<Pawn>(ref this.original, true, "original");
        }

        //Currently unused, retention of injuries was removed to increase the combat benefits of friendly polymorphing
        //public void CopyDamage(Pawn pawn)
        //{
        //    IEnumerable<Hediff_Injury> injuriesToAdd = pawn.health.hediffSet.hediffs
        //        .OfType<Hediff_Injury>()
        //        .Where(injury => injury.CanHealNaturally());
        //    this.injuries.AddRange(injuriesToAdd);
        //}

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

        private void DestroyParentCorpse(Map map)
        {
            List<Thing> thingList = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            for (int i = 0; i < thingList.Count; i++)
            {
                Corpse parentCorpse = thingList[i] as Corpse;
                if (parentCorpse == null) continue;

                Pawn innerPawn = parentCorpse.InnerPawn;
                CompPolymorph compPoly = innerPawn.GetComp<CompPolymorph>();
                if (compPoly == null || compPoly.Original != original) continue;
                thingList[i].Destroy();
                break;
            }
        }

        private void SpawnOriginal(Map map)
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
            //List<BodyPartRecord> bodyparts = pawn.health.hediffSet.GetNotMissingParts().ToList();
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
                if (pawn.story?.traits != null && pawn.story.traits.HasTrait(TraitDefOf.Transhumanist))
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
            catch(NullReferenceException)
            {

            }
        }

        private void TransmutateEffects(IntVec3 position)
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
    }
}
