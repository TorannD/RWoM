using System;
using System.Collections.Generic;
using Verse;

namespace TorannMagic.ClassProgression
{
    [Flags]
    public enum Vocation { None = 0, Mage = 1, Warrior = 2, WarriorMage = Mage | Warrior }
    public enum DisciplineDomain { Magic = 1, Might = 2, WarriorMage = 3 }
    public enum InitiationStage { Foundation = 1, Adept = 2, Master = 3, Ascended = 4 }

    // Owned by the pawn, independent of class traits and the magic/might skill owners.
    // Missing state means classic progression, including existing addon classes.
    public sealed class ClassProgressionState : IExposable
    {
        public const int CurrentVersion = 1;
        private int version = CurrentVersion;
        private Vocation vocation;
        private List<string> noviceSpells = new List<string>();
        public IReadOnlyList<string> NoviceSpells => noviceSpells.AsReadOnly();
        private List<DisciplineProgress> disciplines = new List<DisciplineProgress>();
        private Dictionary<string, string> spellMasteries;
        private bool legacyElementChoice;
        internal bool NeedsLegacyElementChoice => legacyElementChoice && ElementalPair && !disciplines.Exists(p=>p.Stage==InitiationStage.Ascended);
        public string Mastery(string spellId) => spellId != null && spellMasteries != null && spellMasteries.TryGetValue(spellId, out var choice) ? choice : null;
        internal void SetMastery(string spellId, string choice)
        {
            spellMasteries ??= new Dictionary<string, string>();
            spellMasteries[spellId] = choice;
            revision++;
        }

        private int revision;
        public int Revision => revision;
        public int Version => version;
        public Vocation Vocation => vocation;
        public IReadOnlyList<DisciplineProgress> Disciplines => disciplines.AsReadOnly();

        public ClassProgressionState() { } // Scribe only: no implicit enrollment.
        public ClassProgressionState(Vocation vocation)
        {
            if (vocation == Vocation.None || (vocation & ~Vocation.WarriorMage) != 0)
                throw new ArgumentOutOfRangeException(nameof(vocation));
            this.vocation = vocation;
        }

        internal void RenounceNecromancy()
        {
            if (disciplines.RemoveAll(path => path?.ClassId == "rwom.necromancer") > 0) revision++;
        }

        public DisciplineProgress Find(string classId)
            => disciplines.Find(p => p != null && string.Equals(p.ClassId, classId, StringComparison.Ordinal));

        // Storage operation for a future verified initiation transaction. It neither
        // consumes books nor grants abilities, traits, XP, points or hediffs.
        internal static bool Elemental(string id) => id == "rwom.fire" || id == "rwom.ice";

        internal bool ElementalPair => disciplines.Count == 2 && Find("rwom.fire")?.Domain == DisciplineDomain.Magic
            && Find("rwom.ice")?.Domain == DisciplineDomain.Magic;

        internal bool CanChooseFoundation(string id) => disciplines.Count == 0
            || (disciplines.Count == 1 && disciplines[0]?.Domain == DisciplineDomain.Magic
                && Elemental(disciplines[0].ClassId) && Elemental(id) && disciplines[0].ClassId != id);

        internal bool ChooseMartialFoundation(string classId, bool reforged)
        {
            if (classId != JobDriver_GladiatorInitiation.ClassId && classId != SniperFoundation.Id && classId != BladeCareer.Id && classId != RangerCareer.Id && AdditionalMartialRegistry.ForId(classId) == null) return false;
            if (version != CurrentVersion || vocation == Vocation.None || disciplines.Count != 0) return false;
            vocation = Vocation.Warrior;
            disciplines.Add(new DisciplineProgress(classId, DisciplineDomain.Might) {
                MartialAlternate = reforged && AdditionalMartialRegistry.ForId(classId) != null,
                GladiatorManeuver = classId == JobDriver_GladiatorInitiation.ClassId && reforged,
                SniperPatientAim = classId == SniperFoundation.Id && reforged,
                BladeMemory = classId == BladeCareer.Id && reforged,
                RangerTrailLore = classId == RangerCareer.Id && reforged
            });
            revision++;
            return true;
        }

        internal bool ChooseWanderer(Vocation route)
        {
            if (version != CurrentVersion || vocation == Vocation.None || disciplines.Count != 0) return false;
            if (route == Vocation.None || (route & ~Vocation.WarriorMage) != 0) return false;
            vocation = route;
            disciplines.Add(new DisciplineProgress("rwom.wanderer", (DisciplineDomain)route));
            revision++;
            return true;
        }

        public bool TryAddFoundation(string classId, DisciplineDomain domain)
        {
            if (version != CurrentVersion || string.IsNullOrWhiteSpace(classId)
                || classId != classId.Trim() || Find(classId) != null) return false;
            Vocation required = domain == DisciplineDomain.Magic ? Vocation.Mage
                : domain == DisciplineDomain.Might ? Vocation.Warrior : Vocation.None;
            if (required == Vocation.None || (vocation & required) == 0) return false;
            disciplines.Add(new DisciplineProgress(classId, domain));
            revision++;
            return true;
        }

        public bool TryAdvanceAfterTrial(string classId, InitiationStage expected, string trialId)
        {
            if (version != CurrentVersion || string.IsNullOrWhiteSpace(trialId)) return false;
            var path = Find(classId);
            if (expected == InitiationStage.Master && ElementalPair && (NeedsLegacyElementChoice || disciplines.Exists(d => d.Stage == InitiationStage.Ascended))) return false;
            if (path == null || !path.TryAdvanceAfterTrial(expected, trialId)) return false;
            revision++;
            return true;
        }

        public bool TryChooseNoviceSpell(string abilityId)
        {
            if (version != CurrentVersion || (vocation & Vocation.Mage) == 0 || noviceSpells.Count >= 2
                || string.IsNullOrWhiteSpace(abilityId) || abilityId != abilityId.Trim() || noviceSpells.Contains(abilityId)) return false;
            noviceSpells.Add(abilityId);
            revision++;
            return true;
        }

        internal bool RecognizeImportedAscensions(List<DisciplineProgress> paid)
        {
            if (paid.Count == 0 || disciplines.Exists(p => p.Stage == InitiationStage.Ascended)) return false;
            if (ElementalPair && paid.Count == 2) legacyElementChoice = true;
            else paid[0].ImportLegacyStage(paid[0].ImportedTrait, InitiationStage.Ascended);
            revision++;
            return true;
        }

        internal bool ChooseImportedElement(string id)
        {
            if (!ClassProgressionFeature.Enabled || version != CurrentVersion || !NeedsLegacyElementChoice || !Elemental(id)) return false;
            var path=Find(id);
            if(path?.ImportedTrait==null || !path.AuditedLegacyUltimate) return false;
            legacyElementChoice=false;
            path.ImportLegacyStage(path.ImportedTrait, InitiationStage.Ascended);
            revision++;
            return true;
        }

        internal bool ActivateLichForm()
        {
            if (version != CurrentVersion || Find("rwom.necromancer")?.ActivateLichForm() != true) return false;
            revision++;
            return true;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref legacyElementChoice,"legacyElementChoice");
            Scribe_Collections.Look(ref spellMasteries, "spellMasteries", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref noviceSpells, "noviceSpells", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && noviceSpells == null) noviceSpells = new List<string>();
            Scribe_Values.Look(ref version, "version", CurrentVersion);
            Scribe_Values.Look(ref vocation, "vocation", Vocation.None);
            Scribe_Collections.Look(ref disciplines, "disciplines", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && disciplines == null)
                disciplines = new List<DisciplineProgress>();
            // Unknown profile IDs survive without a Def cross-reference or data loss.
            // Loading never initiates a class, completes a ritual or issues a reward.
        }
    }

    public sealed class DisciplineProgress : IExposable
    {
        private string classId;
        private DisciplineDomain domain;
        private InitiationStage stage = InitiationStage.Foundation;
        internal string ImportedTrait;
        internal bool AuditedLegacyUltimate;
        internal InitiationStage ImportedMartialStage;
        internal string RetainedMartialUltimate;
        internal void ImportLegacyStage(string trait, InitiationStage earned)
        {
            ImportedTrait = trait;
            stage = earned;
        }
        private List<string> completedTrials = new List<string>();
        private ServantControlProgress servantControl;
        private bool lichFormActive;
        private bool golemancerBrandsGranted;
        public bool GolemancerBrandsGranted => golemancerBrandsGranted;
        internal void MarkGolemancerBrandsGranted() { if (classId == "rwom.golemancer" && domain == DisciplineDomain.Magic && stage >= InitiationStage.Adept) golemancerBrandsGranted = true; }
        private float fireboltDamage;
        private SummonerTrialProgress summonerTrials;
        public SummonerTrialProgress SummonerTrials => summonerTrials ?? (summonerTrials = new SummonerTrialProgress());
        private ArcanistTrialProgress arcanistTrials;
        public ArcanistTrialProgress ArcanistTrials => arcanistTrials ?? (arcanistTrials = new ArcanistTrialProgress());
        private LightningTrialProgress lightningTrials;
        public LightningTrialProgress LightningTrials => lightningTrials ?? (lightningTrials = new LightningTrialProgress());
        private ShamanTrialProgress shamanTrials;
        public ShamanTrialProgress ShamanTrials => shamanTrials ?? (shamanTrials = new ShamanTrialProgress());
        private BrightmageTrialProgress brightmageTrials;
        public BrightmageTrialProgress BrightmageTrials => brightmageTrials ?? (brightmageTrials = new BrightmageTrialProgress());
        private BloodMageTrialProgress bloodMageTrials;
        public BloodMageTrialProgress BloodMageTrials => bloodMageTrials ?? (bloodMageTrials = new BloodMageTrialProgress());
        private IceTrialProgress iceTrials;
        public IceTrialProgress IceTrials => iceTrials ?? (iceTrials = new IceTrialProgress());
        private List<FireclawCastWitness> fireclawWitnesses = new List<FireclawCastWitness>();
        public IReadOnlyList<FireclawCastWitness> FireclawWitnesses => fireclawWitnesses.AsReadOnly();
        internal void RecordFireclawVictim(int castId, int victimId)
        {
            if (classId != "rwom.fire" || domain != DisciplineDomain.Magic || stage != InitiationStage.Adept
                || castId <= 0 || victimId <= 0 || completedTrials.Contains(FireclawTrial.TrialId)) return;
            var witness = fireclawWitnesses.Find(w => w.castId == castId);
            if (witness != null)
            {
                if (witness.firstVictimId == victimId) return;
                RecordCompletedTrial(FireclawTrial.TrialId);
                fireclawWitnesses.Clear();
                return;
            }
            if (fireclawWitnesses.Count >= FireclawTrial.WitnessLimit) fireclawWitnesses.RemoveAt(0);
            fireclawWitnesses.Add(new FireclawCastWitness { castId = castId, firstVictimId = victimId });
        }
        public float FireboltDamage => fireboltDamage;
        internal void RecordFireboltDamage(float amount)
        {
            if (classId != "rwom.fire" || stage != InitiationStage.Foundation || domain != DisciplineDomain.Magic
                || float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0) return;
            fireboltDamage = System.Math.Min(100f, fireboltDamage + amount);
            if (fireboltDamage >= 100f) RecordCompletedTrial(FireboltDamageTrial.TrialId);
        }
        public bool LichFormActive => lichFormActive;
        internal bool ActivateLichForm()
        {
            if (lichFormActive || classId != "rwom.necromancer" || domain != DisciplineDomain.Magic
                || stage != InitiationStage.Ascended) return false;
            lichFormActive = true;
            return true;
        }
        public ServantControlProgress ServantControl => servantControl;
        public ServantControlProgress EnsureServantControl() => servantControl ?? (servantControl = new ServantControlProgress());
        internal GladiatorUltimate Ultimate;
        internal int CircleReadyAt;
        internal GladiatorAttack Attack;
        internal GladiatorDefense Defense;
        internal int CoverReadyAt;
        internal SniperShotChoice ShotChoice;
        internal int AimedUpgrades;
        internal int BreachUpgrades;
        internal int FractureHits;
        internal int ResearchWorkTicks;
        internal SniperUltimate SniperUltimate;
        internal List<int> CoveredRescues;
        internal int EvacuationDrills;
        internal int NextDrillTick;
        internal int OpeningReadyAt;
        private SniperSurvey survey;
        internal SniperSurvey Survey => survey;
        internal SniperSurvey EnsureSniperSurvey() => survey ??= new SniperSurvey();
        internal AdditionalMartialData MartialData;
        internal bool MartialAlternate;
        internal bool RangerTrailLore;
        internal List<int> RangerTrapVictims;
        internal Dictionary<string,int> RangerClearanceTimes;
        internal int RangerClearances;
        internal RangerUltimate RangerUltimate;
        internal int RangerHuntReadyAt;
        internal int RangerRetreats;
        internal int RangerCares;
        internal int RangerNextCareShift;
        internal Dictionary<int,int> RangerRetreatTimes;
        internal int RangerPetId;
        internal int RangerCareDays;
        internal int RangerCareReadyAt;
        internal bool BladeMemory;
        internal int BladeLastTargetId;
        internal int BladeMemoryReadyAt;
        internal int BladeChallengerId;
        internal int BladeReplyBody;
        internal int BladeReplyUntil;
        internal int BladeReplies;
        internal int BladeRescues;
        internal Dictionary<int,int> BladeRescueTimes;
        internal int BladePassageUpgrades;
        internal int BladePassages;
        internal BladeUltimate BladeUltimate;
        internal int BladeRoutes;
        internal int BladeDeliveries;
        internal int BladeNextSupplyAt;
        internal int BladeRouteOpponent;
        internal int BladeRouteUntil;
        internal int BladeRouteMap;
        internal int BladeRouteBody;
        internal IntVec3 BladeRouteOrigin;
        internal int BladeFormReadyAt;
        internal int BladeExhaustedUntil;
        internal List<int> BladeWeapons;
        internal bool SniperPatientAim;
        internal bool GladiatorManeuver;
        private WandererLessons lessons;
        internal WandererLessons Lessons => lessons;
        internal WandererLessons EnsureLessons() => lessons ??= new WandererLessons();
        private ChaosDisciplineState chaos;
        public ChaosDisciplineState Chaos => chaos;
        internal ChaosDisciplineState EnsureChaos() => chaos ??= new ChaosDisciplineState();
        public string ClassId => classId;
        public DisciplineDomain Domain => domain;
        public InitiationStage Stage => stage;
        public IReadOnlyList<string> CompletedTrials => completedTrials.AsReadOnly();
        public DisciplineProgress() { }
        internal DisciplineProgress(string classId, DisciplineDomain domain)
        { this.classId = classId; this.domain = domain; }

        // Completion is local to one direction: an Ice trial cannot finish Fire.
        // Repeated notifications are idempotent; rewards belong to ritual transactions.
        public bool RecordCompletedTrial(string trialId)
        {
            if (string.IsNullOrWhiteSpace(trialId) || trialId != trialId.Trim()
                || completedTrials.Contains(trialId)) return false;
            completedTrials.Add(trialId);
            return true;
        }

        internal bool TryAdvanceAfterTrial(InitiationStage expected, string trialId)
        {
            if (stage != expected || expected < InitiationStage.Foundation || expected >= InitiationStage.Ascended
                || !completedTrials.Contains(trialId)) return false;
            stage = (InitiationStage)((int)stage + 1);
            return true;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Ultimate,"gladiatorUltimate");
            Scribe_Values.Look(ref CircleReadyAt,"circleReadyAt");
            Scribe_Values.Look(ref Attack, "gladiatorAttack");
            Scribe_Values.Look(ref Defense, "gladiatorDefense");
            Scribe_Values.Look(ref CoverReadyAt, "gladiatorCoverReadyAt");
            Scribe_Values.Look(ref ShotChoice,"sniperShotChoice");
            Scribe_Values.Look(ref AimedUpgrades, "sniperAimedUpgrades");
            Scribe_Values.Look(ref BreachUpgrades, "sniperBreachUpgrades");
            Scribe_Values.Look(ref FractureHits, "sniperFractureHits");
            Scribe_Values.Look(ref ResearchWorkTicks, "sniperResearchWorkTicks");
            Scribe_Values.Look(ref SniperUltimate, "sniperUltimate");
            Scribe_Collections.Look(ref CoveredRescues, "sniperCoveredRescues", LookMode.Value);
            Scribe_Values.Look(ref EvacuationDrills, "sniperEvacuationDrills");
            Scribe_Values.Look(ref NextDrillTick, "sniperNextDrillTick");
            Scribe_Values.Look(ref OpeningReadyAt, "sniperOpeningReadyAt");
            Scribe_Deep.Look(ref survey,"sniperSurvey");
            Scribe_Deep.Look(ref MartialData, "additionalMartialData");
            Scribe_Values.Look(ref MartialAlternate, "additionalMartialAlternate");
            Scribe_Values.Look(ref RangerTrailLore, "rangerTrailLore");
            Scribe_Collections.Look(ref RangerTrapVictims, "rangerTrapVictims", LookMode.Value);
            Scribe_Collections.Look(ref RangerClearanceTimes, "rangerClearanceTimes", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref RangerClearances, "rangerClearances");
            Scribe_Values.Look(ref RangerUltimate, "rangerUltimate");
            Scribe_Values.Look(ref RangerHuntReadyAt, "rangerHuntReadyAt");
            Scribe_Values.Look(ref RangerRetreats, "rangerRetreats");
            Scribe_Values.Look(ref RangerCares, "rangerCares");
            Scribe_Values.Look(ref RangerNextCareShift, "rangerNextCareShift");
            Scribe_Collections.Look(ref RangerRetreatTimes, "rangerRetreatTimes", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref RangerPetId, "rangerPetId");
            Scribe_Values.Look(ref RangerCareDays, "rangerCareDays");
            Scribe_Values.Look(ref RangerCareReadyAt, "rangerCareReadyAt");
            Scribe_Values.Look(ref BladeMemory, "bladeMemory");
            Scribe_Values.Look(ref BladeLastTargetId, "bladeLastTargetId");
            Scribe_Values.Look(ref BladeMemoryReadyAt, "bladeMemoryReadyAt");
            Scribe_Values.Look(ref BladeChallengerId, "bladeChallengerId");
            Scribe_Values.Look(ref BladeReplyBody,"bladeReplyBody");
            Scribe_Values.Look(ref BladeReplyUntil, "bladeReplyUntil");
            Scribe_Values.Look(ref BladeReplies, "bladeReplies");
            Scribe_Values.Look(ref BladeRescues, "bladeRescues");
            Scribe_Collections.Look(ref BladeRescueTimes, "bladeRescueTimes", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref BladePassageUpgrades, "bladePassageUpgrades");
            Scribe_Values.Look(ref BladeUltimate, "bladeUltimate");
            Scribe_Values.Look(ref BladeRoutes, "bladeRoutes");
            Scribe_Values.Look(ref BladeDeliveries, "bladeDeliveries");
            Scribe_Values.Look(ref BladeNextSupplyAt, "bladeNextSupplyAt");
            Scribe_Values.Look(ref BladeRouteOpponent, "bladeRouteOpponent");
            Scribe_Values.Look(ref BladeRouteUntil, "bladeRouteUntil");
            Scribe_Values.Look(ref BladeRouteMap, "bladeRouteMap");
            Scribe_Values.Look(ref BladeRouteBody,"bladeRouteBody");
            Scribe_Values.Look(ref BladeRouteOrigin, "bladeRouteOrigin");
            Scribe_Values.Look(ref BladeFormReadyAt, "bladeFormReadyAt");
            Scribe_Values.Look(ref BladeExhaustedUntil, "bladeExhaustedUntil");
            Scribe_Values.Look(ref BladePassages, "bladePassages");
            Scribe_Collections.Look(ref BladeWeapons, "bladeWeapons", LookMode.Value);
            Scribe_Values.Look(ref SniperPatientAim,"sniperPatientAim");
            Scribe_Values.Look(ref GladiatorManeuver, "gladiatorManeuver");
            Scribe_Deep.Look(ref lessons, "wandererLessons");
            Scribe_Deep.Look(ref chaos,"chaos");
            Scribe_Values.Look(ref classId, "classId");
            Scribe_Values.Look(ref domain, "domain");
            Scribe_Values.Look(ref stage, "stage", InitiationStage.Foundation);
            Scribe_Values.Look(ref ImportedTrait, "importedTrait");
            Scribe_Values.Look(ref AuditedLegacyUltimate, "auditedLegacyUltimate");
            Scribe_Values.Look(ref ImportedMartialStage, "importedMartialStage");
            Scribe_Values.Look(ref RetainedMartialUltimate, "retainedMartialUltimate");
            Scribe_Collections.Look(ref completedTrials, "completedTrials", LookMode.Value);
            Scribe_Deep.Look(ref servantControl, "servantControl");
            Scribe_Values.Look(ref lichFormActive, "lichFormActive", false);
            Scribe_Values.Look(ref golemancerBrandsGranted, "golemancerBrandsGranted", false);
            Scribe_Values.Look(ref fireboltDamage, "fireboltDamage", 0f);
            Scribe_Deep.Look(ref iceTrials, "iceTrials");
            Scribe_Deep.Look(ref lightningTrials, "lightningTrials");
            Scribe_Deep.Look(ref bloodMageTrials, "bloodMageTrials");
            Scribe_Deep.Look(ref brightmageTrials, "brightmageTrials");
            Scribe_Deep.Look(ref shamanTrials, "shamanTrials");
            Scribe_Deep.Look(ref arcanistTrials, "arcanistTrials");
            Scribe_Deep.Look(ref summonerTrials, "summonerTrials");
            Scribe_Collections.Look(ref fireclawWitnesses, "fireclawWitnesses", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && (float.IsNaN(fireboltDamage) || float.IsInfinity(fireboltDamage) || fireboltDamage < 0)) fireboltDamage = 0;
            if (Scribe.mode == LoadSaveMode.PostLoadInit) fireboltDamage = System.Math.Min(100f, fireboltDamage);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && completedTrials == null)
                completedTrials = new List<string>();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (fireclawWitnesses == null) fireclawWitnesses = new List<FireclawCastWitness>();
                var seen = new HashSet<int>();
                fireclawWitnesses.RemoveAll(w => w == null || w.castId <= 0 || w.firstVictimId <= 0 || !seen.Add(w.castId));
                if (fireclawWitnesses.Count > FireclawTrial.WitnessLimit)
                    fireclawWitnesses.RemoveRange(0, fireclawWitnesses.Count - FireclawTrial.WitnessLimit);
                if (completedTrials.Contains(FireclawTrial.TrialId)) fireclawWitnesses.Clear();
            }
        }
    }
}
