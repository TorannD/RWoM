using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal sealed class DeathKnightDisciplineData : AdditionalMartialData
    {
        internal int Battles, Rescues, LastBattle = -60000;
        internal List<int> Casualties;
        internal bool Oath;
        internal int OathReadyAt;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref Battles, "battles");
            Scribe_Values.Look(ref Rescues, "rescues");
            Scribe_Values.Look(ref LastBattle, "lastBattle", -60000);
            Scribe_Collections.Look(ref Casualties, "casualties", LookMode.Value);
            Scribe_Values.Look(ref Oath, "oath");
            Scribe_Values.Look(ref OathReadyAt, "oathReadyAt");
        }
    }

    internal sealed class DeathKnightMartialPath : AdditionalMartialPath
    {
        internal const string ClassId = "rwom.deathknight";
        private static readonly string[] Forms = { "TM_ReadDeathKnightDread", "TM_ReadDeathKnightSpite" };
        internal override string Id => ClassId;
        internal override string Book => "BookOfDeathKnight";
        internal override string[] Choices => Forms;
        internal static DisciplineProgress Path(Pawn p) => ClassIdentity.TryGetState(p, out var state) ? state.Find(ClassId) : null;
        internal static bool IsKnight(Pawn p) => p?.story?.traits?.HasTrait(TorannMagicDefOf.DeathKnight) == true || Path(p) != null;
        internal static Pawn PassiveOwner(Pawn body)
        {
            if (IsKnight(body)) return body;
            if (!SpiritRebirthState.TryGetPossessingSpirit(body,out var soul) ||
                !SpiritRebirthState.TryGet(soul,out var state) || state.DepletionPending) return null;
            return IsKnight(soul) ? soul : null;
        }
        internal static bool HasLifeSteal(Pawn p) => Path(p)?.Stage >= InitiationStage.Master;
        // Classic knights retain native retaliation. Only an active enemy can fuel a staged knight or feed its passive.
        internal static bool CanFuelHate(Pawn knight, Thing instigator) => Path(PassiveOwner(knight)) == null ||
            instigator is Pawn enemy && enemy != knight && !enemy.Downed && !enemy.Dead && !enemy.IsPrisoner && enemy.HostileTo(knight);
        internal static bool CanDrain(Pawn knight, Pawn victim) => HasLifeSteal(PassiveOwner(knight)) && victim != null && !victim.Downed &&
            !victim.Dead && !victim.IsPrisoner && victim.HostileTo(knight);
        private static DeathKnightDisciplineData Data(DisciplineProgress path) => (DeathKnightDisciplineData)(path.MartialData ??= new DeathKnightDisciplineData());
        internal override bool CanEnroll(CompAbilityUserMight might, string choice) => Forms.Contains(choice) && TorannMagicDefOf.TM_HateHD != null &&
            TorannMagicDefOf.TM_Shroud != null && TorannMagicDefOf.TM_Spite != null && TorannMagicDefOf.TM_WaveOfFear != null;
        internal override IEnumerable<MightPower> Powers(CompAbilityUserMight might) => might.MightData.MightPowersDK.Where(p => p.learned);

        internal override void Restore(CompAbilityUserMight might, DisciplineProgress path)
        {
            if (TM_Calc.GetHateHediff(might.Pawn) == null) might.Pawn.health.AddHediff(TorannMagicDefOf.TM_HateHD);
            Grant(might, TorannMagicDefOf.TM_Shroud, false);
            Grant(might, path.MartialAlternate ? TorannMagicDefOf.TM_Spite : TorannMagicDefOf.TM_WaveOfFear, true);
            if (path.Stage >= InitiationStage.Adept)
                Grant(might, path.MartialAlternate ? TorannMagicDefOf.TM_WaveOfFear : TorannMagicDefOf.TM_Spite, true);
            if (path.Stage >= InitiationStage.Master) Grant(might, TorannMagicDefOf.TM_LifeSteal, false);
            if (path.Stage == InitiationStage.Ascended && (path.MartialData as DeathKnightDisciplineData)?.Oath != true)
                Grant(might, TorannMagicDefOf.TM_GraveBlade, true);
        }
        private static void Grant(CompAbilityUserMight might, TMAbilityDef def, bool active)
        {
            if (def == null) return;
            var family = might.MightData.MightPowersDK.FirstOrDefault(p => p.TMabilityDefs.Contains(def));
            if (family == null) return;
            family.learned = true;
            if (active) TM_ClassState.AddRuntimeAbility(might, family.abilityDef);
        }
        private static int Battles(InitiationStage stage) => stage switch { InitiationStage.Foundation => 5, InitiationStage.Adept => 8, _ => 12 };
        private static int Rescues(InitiationStage stage) => stage switch { InitiationStage.Foundation => 3, InitiationStage.Adept => 4, _ => 6 };
        private static string Trial(DisciplineProgress path) => ClassId + "." + path.Stage;
        private static bool Ready(DisciplineProgress path) => path.CompletedTrials.Contains(Trial(path));

        // A day of shielding a nearby ally against a genuine hostile attack. A rescue offers a peaceful route.
        internal static void Battle(Pawn pawn)
        {
            var path = Path(PassiveOwner(pawn));
            if (path == null || path.Stage >= InitiationStage.Ascended || Ready(path)) return;
            var data = Data(path);
            int now = Find.TickManager.TicksGame;
            if ((long)now - data.LastBattle < GenDate.TicksPerDay) return;
            data.LastBattle = now;
            if (++data.Battles >= Battles(path.Stage)) path.RecordCompletedTrial(Trial(path));
        }
        internal static void Rescue(Pawn pawn, Pawn ally)
        {
            var path = Path(PassiveOwner(pawn));
            if (path == null || path.Stage >= InitiationStage.Ascended || (ally == null || ally.thingIDNumber <= 0) || Ready(path)) return;
            var data = Data(path);
            data.Casualties ??= new List<int>();
            if (data.Casualties.Contains(ally.thingIDNumber)) return;
            data.Casualties.Add(ally.thingIDNumber);
            if (++data.Rescues >= Rescues(path.Stage)) path.RecordCompletedTrial(Trial(path));
        }
        internal static bool Claim(Pawn pawn, bool oath = false)
        {
            if (LegacyMartialConversion.AlternativeConflicts(pawn,oath)) return false;
            var path = Path(pawn);
            var might = pawn.GetCompAbilityUserMight();
            if (path == null || !Ready(path) || might?.ExistingMightData == null ||
                (path.Stage == InitiationStage.Master && might.MightData.MightUserLevel < 30)) return false;
            if (!ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(ClassId, path.Stage, Trial(path))) return false;
            var data = Data(path);
            data.Battles = data.Rescues = 0;
            data.Casualties = null;
            if (path.Stage == InitiationStage.Ascended) data.Oath = oath;
            new DeathKnightMartialPath().Restore(might, path);
            return true;
        }
        internal override string Requirements(Pawn pawn, DisciplineProgress path)
        {
            var d = path.MartialData as DeathKnightDisciplineData;
            if (path.Stage == InitiationStage.Ascended)
                return d?.Oath == true ? "TM_DKOath".Translate().ToString() : TorannMagicDefOf.TM_GraveBlade.LabelCap.ToString();
            return "TM_DKTrial".Translate(d?.Battles ?? 0, Battles(path.Stage), d?.Rescues ?? 0, Rescues(path.Stage)).ToString() +
                (path.Stage == InitiationStage.Master ? "  " + "TM_DKLevel".Translate(pawn.GetCompAbilityUserMight().MightData.MightUserLevel).ToString() : "");
        }
        internal override float ControlsHeight(Pawn pawn, DisciplineProgress path) => Ready(path) ? 40f : 0f;
        internal override void DrawControls(Pawn pawn, DisciplineProgress path, float width, ref float y)
        {
            if (!Ready(path)) return;
            if (Widgets.ButtonText(new Rect(0, y, width, 32f), "TM_DKClaim".Translate()))
            {
                if (path.Stage == InitiationStage.Master)
                {
                    if (pawn.GetCompAbilityUserMight().MightData.MightUserLevel >= 30)
                        Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption> {
                            new FloatMenuOption(TorannMagicDefOf.TM_GraveBlade.LabelCap, () => Claim(pawn)),
                            new FloatMenuOption("TM_DKOath".Translate(), () => Claim(pawn, true)) }));
                }
                else Claim(pawn);
            }
            y += 40f;
        }
    }
}
