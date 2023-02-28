using RimWorld;
using System;
using System.Collections.Generic;
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

        public CompProperties_Polymorph Props => (CompProperties_Polymorph)props;

        public Pawn ParentPawn
        {
            get
            {
                Pawn pawn = parent as Pawn;
                if (pawn == null)
                {
                    Log.Error("pawn is null");
                }
                return pawn;
            }
            set => parent = value;
        }

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

        private void SpawnSetup()
        {
            ticksLeft = ticksToDestroy;
            TransmutateEffects(ParentPawn.Position);
            if (original != null && spawner == original && original.Spawned)
            {
                bool drafter = original.Drafted;
                original.DeSpawn();
                if(drafter)
                {
                    ParentPawn.drafter.Drafted = true;
                }
                Find.Selector.Select(ParentPawn, false);
            }
        }

        private void CheckPawnState()
        {
            if (Find.TickManager.TicksGame % Rand.Range(30, 60) == 0 && ParentPawn.kindDef == PawnKindDef.Named("TM_Dire_Wolf"))
            {
                AutoCast.AnimalBlink.Evaluate(ParentPawn, 2, 6, out _);
            }

            if (ParentPawn.drafter == null)
            {
                ParentPawn.drafter = new Pawn_DraftController(ParentPawn);
            }
        }        

        public override void CompTick()
        {
            if (original == null) return;
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
            else if(initialized && !temporary && parent.Spawned)
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
            if (original == null) return;

            //CopyDamage(ParentPawn); removed for polymorph balance
            SpawnOriginal(ParentPawn.Map);
            ApplyDamage(original);
            original = null;
        }

        public override void PostDeSpawn(Map map)
        {
            effecter?.Cleanup();
            base.PostDeSpawn(map);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref initialized, "initialized");
            Scribe_Values.Look<bool>(ref temporary, "temporary");
            Scribe_Values.Look<bool>(ref validSummoning, "validSummoning", true);
            Scribe_Values.Look<int>(ref ticksLeft, "ticksLeft");
            Scribe_Values.Look<int>(ref ticksToDestroy, "ticksToDestroy", 1800);
            Scribe_Values.Look<CompAbilityUserMagic>(ref compSummoner, "compSummoner");
            Scribe_References.Look<Pawn>(ref spawner, "spawner", true);
            Scribe_Deep.Look<Pawn>(ref original, true, "original");
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (ticksLeft > 0 && (parent.DestroyedOrNull() || ParentPawn.Dead))
            {
                DestroyParentCorpse(activeMap);
                SpawnOriginal(activeMap);
                original.Kill(null);
                Original = null;
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
            bool drafter = ParentPawn.Drafted;
            bool selected = Find.Selector.IsSelected(ParentPawn);
            if (map != null)
            {
                GenSpawn.Spawn(original, ParentPawn.Position, map);
                TransmutateEffects(ParentPawn.Position);
            }
            else
            {
                map = spawner.Map;
                GenSpawn.Spawn(original, ParentPawn.Position, map);
                TransmutateEffects(ParentPawn.Position);
            }  
            if(drafter)
            {
                original.drafter.Drafted = true;
            }
            if (selected)
            {
                Find.Selector.Select(original, false);
            }
        }

        private void ApplyDamage(Pawn pawn)
        {
            // Removed keeping previous injuries for polymorph balance
            try
            {
                if (pawn.story?.traits != null && pawn.story.traits.HasTrait(TraitDefOf.Transhumanist))
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(TorannMagicDefOf.Polymorphed_Transhumanist, spawner);
                }
                else if(spawner == original)
                {
                    //do not give bad thoughts
                }
                else
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(TorannMagicDefOf.Polymorphed, spawner);
                }
            }
            catch(NullReferenceException)
            {

            }
        }

        private void TransmutateEffects(IntVec3 position)
        {
            Vector3 rndPos = position.ToVector3Shifted();
            FleckMaker.ThrowHeatGlow(position, ParentPawn.Map, 1f);
            for (int i = 0; i < 6; i++)
            {
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(-.5f, .5f);
                rndPos.y += Rand.Range(.3f, 1.3f);
                FleckMaker.ThrowSmoke(rndPos, ParentPawn.Map, Rand.Range(.7f, 1.1f));
                FleckMaker.ThrowLightningGlow(position.ToVector3Shifted(), ParentPawn.Map, 1.4f);
            }
        }
    }
}
