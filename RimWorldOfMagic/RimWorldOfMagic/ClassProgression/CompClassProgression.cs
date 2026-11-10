using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace TorannMagic.ClassProgression
{
    public sealed class CompClassProgression : ThingComp
    {
        private ClassProgressionState state;
        internal bool DebugKeepClassic;
        private SpiritRebirthState spiritRebirth;
        public SpiritRebirthState SpiritRebirth => spiritRebirth;
        private SpiritRebirthRitualTransaction spiritRitual;
        internal SpiritRebirthRitualTransaction SpiritRitual => spiritRitual;
        internal bool TryBeginSpiritRitual(SpiritRebirthRitualTransaction receipt)
        {
            if (spiritRitual != null || receipt == null || !ReferenceEquals(receipt.Original, parent)) return false;
            spiritRitual = receipt;
            return true;
        }
        internal bool EnsureSpiritRitualPending(SpiritRebirthRitualTransaction receipt)
        {
            var pawn = parent as Pawn;
            if (receipt == null || !ReferenceEquals(spiritRitual, receipt) || !ReferenceEquals(receipt.Original, pawn)
                || pawn == null || pawn.Dead || pawn.Destroyed) return false;
            if (spiritRebirth == null) spiritRebirth = SpiritRebirthState.CreateRitualPending(pawn);
            return SpiritRebirthState.TryGet(pawn, out var state) && state.Form == SpiritRebirthForm.Dormant;
        }
        internal bool ClearRefundedSpiritRitual(SpiritRebirthRitualTransaction receipt)
        {
            if (!ReferenceEquals(spiritRitual, receipt) || receipt == null || !receipt.FullyRefunded) return false;
            spiritRitual = null;
            return true;
        }

        // Reserved for a future explicit corpse conversion transaction. No spawn,
        // loading, trait, or legacy migration path calls this entry point.
        internal bool BeginSpiritRebirth()
        {
            var pawn = parent as Pawn;
            if (spiritRebirth != null || pawn == null || pawn.Dead || pawn.Destroyed
                || !ReferenceEquals(ClassIdentity.Component(pawn), this)) return false;
            spiritRebirth = new SpiritRebirthState(pawn);
            return true;
        }
        private int tomeQuestAvailableAtTick;
        public int TomeQuestAvailableAtTick => tomeQuestAvailableAtTick;
        public void DelayNextTomeQuest(int now)
            => tomeQuestAvailableAtTick = System.Math.Max(tomeQuestAvailableAtTick, now + 60000);
        public ClassProgressionState State => state;
        public bool UsesClassicProgression => state == null;

        // Explicit entry point only. Existing saves, spawn and opening the grimoire
        // do not call it. Migration requires its own preservation-checked transaction.
        public bool BeginNewProgression(Vocation vocation)
        {
            if (!ClassProgressionFeature.Enabled || state != null || vocation == Vocation.None || (vocation & ~Vocation.WarriorMage) != 0) return false;
            state = new ClassProgressionState(vocation);
            return true;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!ClassProgressionFeature.Enabled) yield break;
            var pawn = parent as Pawn;
            if (pawn?.IsColonistPlayerControlled != true) yield break;
            // The existing purple-book gizmo opens both classic and new progression.
            // Keep only the separate enrollment action here.
            if (!ClassIdentity.TryGetState(pawn, out _) && ClassEnrollment.CanEnrollMage(pawn))
            {
                yield return new Command_Action {
                    defaultLabel = "TM_ProgressionEnrollMage".Translate(),
                    defaultDesc = "TM_ProgressionEnrollMageDesc".Translate(),
                    icon = Verse.TexButton.IconBook,
                    action = () => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "TM_ProgressionEnrollMageDesc".Translate(), () => {
                            if (ClassEnrollment.TryEnrollMage(pawn)) Window_TMGrimoire.OpenFor(pawn);
                        }))
                };
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent is not Pawn pawn) return;
            if (Circle != null && !Circle.Tick(pawn, Find.TickManager.TicksGame)) EndCircle();
            if (Ward != null && !Ward.Active(pawn,Find.TickManager.TicksGame)) Ward=null;
            if (Cover != null && !Cover.Active(pawn, Find.TickManager.TicksGame)) Cover = null;
            if (!ClassProgressionFeature.Enabled) return;
            if (spiritRebirth?.DepletionPending == true) SpiritRebirthEnergy.Retry(parent as Pawn);
            if (state?.Find("rwom.summoner")?.Stage == InitiationStage.Foundation && Find.TickManager.TicksGame % 30 == 0)
                SummonerTrials.Observe(parent as Pawn, Find.TickManager.TicksGame);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            if (Circle != null) EndCircle();
            Ward=null;
            Cover = null;
            if (!ClassProgressionFeature.Enabled) { base.PostDeSpawn(map, mode); return; }
            var path = state?.Find("rwom.summoner");
            if (path != null) path.SummonerTrials.LastActive = false;
            base.PostDeSpawn(map, mode);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!ClassProgressionFeature.Enabled) return;
            var summoner = state?.Find("rwom.summoner");
            if (summoner != null) summoner.SummonerTrials.LastActive = false;
            NecromancerControlTrial.PauseOwner(parent as Pawn);
            LichTransformation.RestoreHealth(parent as Pawn);
            SynchronizeHealthDisplay();
        }

        public void SynchronizeHealthDisplay()
        {
            var pawn = parent as Pawn;
            if (pawn?.health?.hediffSet == null || !ClassIdentity.TryGetState(pawn, out var current)) return;
            var markerDef = DefDatabase<HediffDef>.GetNamedSilentFail("TM_ClassDiscipline");
            if (markerDef == null) return;
            var wanted = new HashSet<string>(current.Disciplines.Where(p => p != null
                && !string.IsNullOrWhiteSpace(p.ClassId)).Select(p => p.ClassId));
            var seen = new HashSet<string>();
            foreach (var marker in pawn.health.hediffSet.hediffs.OfType<Hediff_ClassDiscipline>().ToList())
                if (!wanted.Contains(marker.profileId) || !seen.Add(marker.profileId)) pawn.health.RemoveHediff(marker);
            foreach (string id in wanted)
                if (!seen.Contains(id))
                {
                    var marker = HediffMaker.MakeHediff(markerDef, pawn) as Hediff_ClassDiscipline;
                    if (marker == null) continue;
                    marker.profileId = id;
                    pawn.health.AddHediff(marker);
                }
        }

        internal GladiatorCircle Circle;
        internal GladiatorWard Ward;
        private void EndCircle()
        {
            Circle=null;
            if (parent is not Pawn pawn) return;
            var def=DefDatabase<HediffDef>.GetNamedSilentFail("TM_LastCircleStance");
            var stance=def == null ? null : pawn.health?.hediffSet.GetFirstHediffOfDef(def);
            if (stance != null) pawn.health.RemoveHediff(stance);
        }
        internal GladiatorCover Cover;
        public override void PostPreApplyDamage(ref DamageInfo hit, out bool absorbed)
        {
            absorbed = false;
            if (parent is not Pawn bearer) return;
            if (bearer.jobs?.curDriver is JobDriver_SniperSingleOpening opening && opening.Aiming && hit.Amount > 0f &&
                hit.Def?.ExternalViolenceFor(bearer) == true)
                hit.SetAmount(hit.Amount * 1.2f);
            if (bearer.jobs?.curDriver is JobDriver_BladeSevenSteps form && form.Active && hit.Amount > 0f && hit.Def?.ExternalViolenceFor(bearer) == true)
                hit.SetAmount(hit.Amount * 1.15f);
            if (bearer.jobs?.curDriver is JobDriver_RangerBreakthrough hunt && hunt.Active)
            {
                hunt.Guard(ref hit, out absorbed);
                if (absorbed) return;
            }
            Hediff_CrucibleWard.Shield(bearer,ref hit,out absorbed);
            if(absorbed)return;
            CommanderOrderEffects.Shield(bearer,ref hit,out absorbed);
            if (absorbed) return;
            if (Ward != null && Ward.Active((Pawn)parent,Find.TickManager.TicksGame))
            {
                Ward.Absorb(ref hit);
                if (hit.Amount <= 0) { absorbed=true; return; }
            }
            if (Cover == null) return;
            if (!Cover.Active((Pawn)parent, Find.TickManager.TicksGame)) { Cover = null; return; }
            Cover.Intercept(ref hit, out absorbed);
        }

        internal int NecromancyConsentJob = -1;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref NecromancyConsentJob, "TM_necromancyConsentJob", -1);
            if (Scribe.mode != LoadSaveMode.Saving || Circle != null) Scribe_Deep.Look(ref Circle,"TM_gladiatorCircle");
            if (Scribe.mode != LoadSaveMode.Saving || Ward != null) Scribe_Deep.Look(ref Ward,"TM_gladiatorWard");
            if (Scribe.mode != LoadSaveMode.Saving || Cover != null) Scribe_Deep.Look(ref Cover, "TM_gladiatorCover");
            if (Scribe.mode == LoadSaveMode.Saving && !DebugKeepClassic && state == null && spiritRebirth == null && spiritRitual == null && tomeQuestAvailableAtTick == 0) return;
            Scribe_Values.Look(ref DebugKeepClassic, "TM_debugKeepClassic");
            Scribe_Deep.Look(ref state, "TM_classProgression");
            Scribe_Deep.Look(ref spiritRebirth, "TM_spiritRebirth");
            Scribe_Deep.Look(ref spiritRitual, "TM_spiritRitual");
            Scribe_Values.Look(ref tomeQuestAvailableAtTick, "TM_tomeQuestAvailableAtTick", 0);
        }
    }

    [StaticConstructorOnStartup]
    public static class ClassProgressionBootstrap
    {
        static ClassProgressionBootstrap()
        {
            // The save carrier stays registered while gameplay is disabled.
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading) EnsureComp(def);
            EnsureRegenOrigin(DefDatabase<HediffDef>.GetNamedSilentFail("TM_EnergyRegenHD"));
        }

        // Kure replaces this entire Def during XML patching. Its own timer, stages and limits still apply.
        internal static void EnsureRegenOrigin(HediffDef def)
        {
            if (def?.hediffClass == null || !typeof(HediffWithComps).IsAssignableFrom(def.hediffClass)) return;
            def.comps ??= new List<HediffCompProperties>();
            if (!def.comps.Any(p => p?.compClass == typeof(HediffComp_HostedStance)))
                def.comps.Add(new HediffCompProperties { compClass = typeof(HediffComp_HostedStance) });
        }

        public static void EnsureComp(ThingDef def)
        {
            // Generated corpses retain the live pawn's RaceProperties.
            if (def?.category != ThingCategory.Pawn || def.race?.Humanlike != true) return;
            if (def.comps == null) def.comps = new List<CompProperties>();
            if (!def.comps.Any(p => p?.compClass == typeof(AbilityKnowledgeComp)))
                def.comps.Add(new CompProperties { compClass = typeof(AbilityKnowledgeComp) });
            if (!def.comps.Any(p => p?.compClass == typeof(CompClassProgression)))
                def.comps.Add(new CompProperties { compClass = typeof(CompClassProgression) });
        }
    }
}

