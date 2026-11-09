using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using TorannMagic.Utils;
using TorannMagic.Compatibility;

namespace TorannMagic.ClassProgression
{
    public sealed class JobDriver_GladiatorInitiation : JobDriver
    {
        internal const string ClassId = "rwom.gladiator";
        private const string Classic = "TM_ReadGladiatorSprint";
        private const string Reforged = "TM_ReadGladiatorManeuver";
        private const string Focus = "TM_ReadSniperFocus";
        private const string Patient = "TM_ReadSniperPatientAim";
        private const string BladeFocus = "TM_ReadBladeFocus";
        private const string BladeMemory = "TM_ReadBladeMemory";
        private const string RangerTraining = "TM_ReadRangerTraining";
        private const string TrailLore = "TM_ReadRangerTrailLore";

        private static string ClassFor(Thing book) => book?.def?.defName switch {
            "BookOfSniper" => SniperFoundation.Id, "BookOfBladedancer" => BladeCareer.Id,
            "BookOfRanger" => RangerCareer.Id, "BookOfGladiator" => ClassId, _ => AdditionalMartialRegistry.ForBook(book?.def?.defName)?.Id
        };
        private static readonly string[] GladiatorChoices = { Classic, Reforged };
        private static readonly string[] SniperChoices = { Focus, Patient };
        private static readonly string[] BladeChoices = { BladeFocus, BladeMemory };
        private static readonly string[] RangerChoices = { RangerTraining, TrailLore };
        private static string[] Options(string id) => id switch {
            SniperFoundation.Id => SniperChoices, BladeCareer.Id => BladeChoices, RangerCareer.Id => RangerChoices, ClassId => GladiatorChoices, _ => AdditionalMartialRegistry.ForId(id)?.Choices ?? System.Array.Empty<string>()
        };
        private Thing Book => job.targetA.Thing;
        internal static bool Eligible(Pawn pawn, Thing book)
        {
            if (!ClassProgressionFeature.Enabled || pawn?.Spawned != true || pawn.Dead || pawn.Downed ||
                !pawn.IsColonistPlayerControlled || ClassFor(book) == null || book.Destroyed ||
                book.stackCount <= 0 || !book.def.destroyable || DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationWarrior") == null) return false;
            if (AdditionalMartialRegistry.ForBook(book.def.defName)?.SupportsBook(book.def) == false) return false;
            if (!ClassIdentity.TryGetState(pawn, out var state)) return ClassEnrollment.UnenrolledWarrior(pawn);
            return state.Disciplines.Count == 0 && !ClassInitiation.HasLegacyClass(pawn)
                && ((state.Vocation & Vocation.Warrior) != 0 || pawn.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy));
        }

        internal static IEnumerable<FloatMenuOption> Choices(Pawn pawn, Thing book)
        {
            if (!Eligible(pawn, book)) yield break;
            if (ClassFor(book)==PsionicMartialPath.Career && !PsionicMartialPath.CanAwaken(pawn))
            {
                yield return new FloatMenuOption("TM_NotPsychicSensitive".Translate(pawn.LabelShort),null);
                yield break;
            }
            foreach (var name in Options(ClassFor(book)))
            {
                var def = DefDatabase<JobDef>.GetNamedSilentFail(name);
                if (def != null) yield return new FloatMenuOption(name.Translate(), () => {
                    if (Eligible(pawn, book) && book.Spawned && !book.IsForbidden(pawn)
                        && pawn.CanReserveAndReach(book, PathEndMode.Touch, Danger.Some))
                        pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(def, book), JobTag.Misc);
                });
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
            => Eligible(pawn, Book) && Book.Spawned && pawn.Reserve(Book, job, 1, 1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !Eligible(pawn, Book));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(Book.TryGetComp<CompUsable>().Props.useDuration, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            yield return Toils_General.Do(() => {
                if (!Complete(pawn, Book, job.def.defName)) EndJobWith(JobCondition.Incompletable);
            });
        }

        internal static bool Complete(Pawn pawn, Thing book, string choice)
        {
            if (!Eligible(pawn, book)) return false;
            string id = ClassFor(book);
            if (!Options(id).Contains(choice)) return false;
            if (book.Spawned ? book.Map != pawn.Map || !pawn.CanReachImmediate(book, PathEndMode.Touch)
                : pawn.inventory == null || book.holdingOwner != pawn.inventory.innerContainer) return false;
            return GrantFoundation(pawn,id,choice,book);
        }

        // The generator already selected this trait with the normal faction and
        // backstory rules. No physical book exists for a newly generated recruit.
        internal static bool TryGrantRolledFoundation(Pawn pawn, TraitDef trait)
        {
            if (trait != null && trait == TorannMagicDefOf.TM_Wayfarer)
                return ClassEnrollment.ChooseGeneratedWanderer(pawn, Vocation.Warrior);
            var comp = ClassIdentity.Component(pawn);
            if (comp == null || comp.State != null || pawn.story?.traits == null || pawn.health == null ||
                pawn.needs == null || pawn.Dead || pawn.Destroyed || ClassEnrollment.HasChosenClass(pawn) ||
                !ClassEnrollment.HasNoPriorOwnership(pawn) || DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationWarrior") == null) return false;
            var id = trait?.defName switch {
                "Gladiator" => ClassId, "TM_Sniper" => SniperFoundation.Id, "Bladedancer" => BladeCareer.Id,
                "Ranger" => RangerCareer.Id, "TM_Commander" => "rwom.commander", "DeathKnight" => "rwom.deathknight",
                "TM_Psionic" => "rwom.psionic", "TM_Monk" => "rwom.monk", "Faceless" => "rwom.faceless",
                "TM_SuperSoldier" => "rwom.supersoldier", "TM_Apothecary" => "rwom.apothecary", _ => null
            };
            var options = Options(id);
            return options.Length != 0 && GrantFoundation(pawn,id,options[0],null);
        }

        private static bool GrantFoundation(Pawn pawn, string id, string choice, Thing book)
        {
            bool sniper = id == SniperFoundation.Id;
            bool blade = id == BladeCareer.Id;
            bool ranger = id == RangerCareer.Id;
            var extra = AdditionalMartialRegistry.ForId(id);
            var def = extra == null ? DefDatabase<TMAbilityDef>.GetNamedSilentFail(ranger ? "TM_RangerTraining" : blade ? "TM_BladeFocus" : sniper ? "TM_SniperFocus" : choice == Classic ? "TM_Sprint" : "TM_GladiatorManeuver") : null;
            if ((extra == null && def == null) || (ranger && TorannMagicDefOf.TM_AnimalFriend == null) || (blade && DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_BladePassage") == null)) return false;
            TM_PawnTracker.EnsureClassComps(pawn);
            var might = pawn.GetCompAbilityUserMight();
            if (might == null || (book != null ? !Eligible(pawn,book) : !ClassEnrollment.HasNoPriorOwnership(pawn))) return false;
            if (extra != null && !extra.CanEnroll(might,choice)) return false;
            var existing = might.ExistingMightData;
            if (blade && existing != null)
            {
                // Validate previously saved owners before consuming the physical form.
                var data = might.MightData;
                if (data.MightPowersB.Count(p => p?.TMabilityDefs?.Contains(def) == true) != 1 ||
                    data.MightPowerSkill_BladeFocus.Count(s => s?.label == "TM_BladeFocus_pwr") != 1) return false;
            }
            if (existing != null)
            {
                if (ranger)
                {
                    var data = might.MightData;
                    if (data.MightPowersR.Count(p => p?.TMabilityDefs?.Contains(def) == true) != 1 ||
                        data.MightPowersR.Count(p => p?.TMabilityDefs?.Contains(TorannMagicDefOf.TM_AnimalFriend) == true) != 1 ||
                        data.MightPowerSkill_RangerTraining.Count(s => s?.label == "TM_RangerTraining_pwr") != 1) return false;
                }
                if (sniper && (existing.MightPowersS.Count(p => p?.TMabilityDefs?.Contains(def) == true) != 1 ||
                    existing.MightPowerSkill_SniperFocus.Count(s => s?.label == "TM_SniperFocus_pwr") != 1)) return false;
                if (choice == Classic && existing.MightPowersG.Count(p => p?.TMabilityDefs?.Contains(def) == true) != 1) return false;
            }
            var comp = ClassIdentity.Component(pawn);
            if (book != null)
            {
                var holder = book.holdingOwner;
                var payment = book.SplitOff(1);
                try { payment.Destroy(DestroyMode.Vanish); }
                catch { if (!payment.Destroyed) { ClassInitiation.RestoreUnpaidBook(pawn, payment, holder); return false; } }
                if (!payment.Destroyed) { ClassInitiation.RestoreUnpaidBook(pawn, payment, holder); return false; }
            }
            if (comp.State == null) comp.BeginNewProgression(Vocation.Warrior);
            if (!comp.State.ChooseMartialFoundation(id, choice == Options(id)[1]))
                throw new System.InvalidOperationException("Martial initiation changed during payment");
            if (sniper)
            {
                might.MightData.MightPowersS.First(p => p.TMabilityDefs.Contains(def)).learned = true;
                if (choice == Patient)
                {
                    var training = might.MightData.MightPowerSkill_SniperFocus.First(p => p.label == "TM_SniperFocus_pwr");
                    training.levelMax = 4;
                }
            }
            if (blade)
            {
                var data = might.MightData;
                data.MightPowersB.First(p => p.TMabilityDefs.Contains(def)).learned = true;
                if (choice == BladeMemory) data.MightPowerSkill_BladeFocus.First(s => s.label == "TM_BladeFocus_pwr").levelMax = 4;
            }
            if (ranger)
            {
                var data = might.MightData;
                data.MightPowersR.First(p => p.TMabilityDefs.Contains(def)).learned = true;
                if (choice == TrailLore) data.MightPowerSkill_RangerTraining.First(s => s.label == "TM_RangerTraining_pwr").levelMax = 4;
            }
            var chosenOwner = might.MightData.MightPowersG.FirstOrDefault(p => p.TMabilityDefs.Contains(def));
            if (chosenOwner != null) chosenOwner.learned = true;
            ClassEnrollment.ConsumePotentials(pawn);
            var traits = pawn.story.traits;
            var warrior = DefDatabase<TraitDef>.GetNamed("TM_VocationWarrior");
            foreach (var old in traits.allTraits.Where(t => t.def != warrior && t.def.defName.StartsWith("TM_Vocation")).ToList()) traits.RemoveTrait(old);
            if (!traits.HasTrait(warrior)) traits.GainTrait(new Trait(warrior));
            // The GainTrait hook usually syncs the class genes, but a pawn that
            // already carries the vocation trait fires no event on re-rolls:
            // sync explicitly so the chosen martial class consumes its latent
            // hidden-mage/warrior gene in every path.
            TM_ClassGenes.SyncClassGenes(pawn);
            TM_PawnTracker.ResolveComps(pawn);
            TM_LegacyRecovery.Repair(pawn);
            Restore(might);
            if (blade) BladeCareer.Restore(might);
            if (ranger) RangerCareer.Restore(might);
            extra?.Restore(might, comp.State.Find(id));
            comp.SynchronizeHealthDisplay();
            return true;
        }

        internal static void Restore(CompAbilityUserMight might)
        {
            if (!ClassIdentity.TryGetState(might.Pawn, out var state) || state.Find(ClassId) is not DisciplineProgress path) return;
            var grapple = might.ExistingMightData?.MightPowersG.FirstOrDefault(p => p?.learned == true &&
                TorannMagicDefOf.TM_Grapple != null && p.TMabilityDefs?.Contains(TorannMagicDefOf.TM_Grapple) == true);
            if (grapple != null) TM_ClassState.AddRuntimeAbility(might,grapple.abilityDef);
            // Chosen path powers own their grimoire rows: keep the saved
            // owner learned so Restore keeps the row visible after reloads.
            if (path.GladiatorManeuver)
            {
                var maneuverOwner = might.MightData.MightPowersG.FirstOrDefault(
                    p => p?.TMabilityDefs?.Any(d => d.defName == "TM_GladiatorManeuver") == true);
                if (maneuverOwner != null) maneuverOwner.learned = true;
            }
            if (path.Attack == GladiatorAttack.Sweep)
            {
                var sweepOwner = might.MightData.MightPowersG.FirstOrDefault(
                    p => p?.TMabilityDefs?.Any(d => d.defName == "TM_GladiatorSweep") == true);
                if (sweepOwner != null) sweepOwner.learned = true;
            }
            var ability = path.GladiatorManeuver ? DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_GladiatorManeuver")
                : might.MightData.MightPowersG.FirstOrDefault(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Sprint))?.abilityDef;
            if (ability != null) TM_ClassState.AddRuntimeAbility(might, ability);
            if (path.Ultimate == GladiatorUltimate.Whirlwind)
                TM_ClassState.AddRuntimeAbility(might,TorannMagicDefOf.TM_Whirlwind);
            if (path.Attack == GladiatorAttack.Sweep)
            {
                var sweep = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_GladiatorSweep");
                if (sweep != null) TM_ClassState.AddRuntimeAbility(might, sweep);
            }
            if (path.GladiatorManeuver && might.skill_Sprint)
            {
                var sprint = might.MightData.MightPowersG.FirstOrDefault(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Sprint));
                if (sprint != null) TM_ClassState.AddRuntimeAbility(might, sprint.abilityDef);
            }
        }
    }

    public sealed class Verb_GladiatorManeuver : AbilityUser.Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            var pawn = CasterPawn;
            if (pawn?.Spawned != true || pawn.Dead || pawn.Downed || ClassIdentity.Component(pawn)?.Circle != null || currentTarget.Thing != pawn ||
                !ClassIdentity.TryGetState(pawn, out var state) || state.Find(JobDriver_GladiatorInitiation.ClassId)?.GladiatorManeuver != true) return false;
            var def = DefDatabase<HediffDef>.GetNamed("TM_GladiatorManeuverHD");
            var active = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (active?.TryGetComp<HediffComp_SpiritCastOrigin>()?.Projected == true)
                pawn.health.RemoveHediff(active);
            HealthUtility.AdjustSeverity(pawn, def, 1f);
            return true;
        }
    }
}
