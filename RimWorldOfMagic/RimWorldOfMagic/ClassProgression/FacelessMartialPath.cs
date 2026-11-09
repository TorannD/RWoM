using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal sealed class FacelessDisciplineData : AdditionalMartialData
    {
        internal TMAbilityDef Sample;
        internal List<string> Studied;
        internal List<int> Rescued;
        internal int Days, LastDay = -60000;
        internal bool Virtuoso;
        public override void ExposeData()
        {
            Scribe_Defs.Look(ref Sample, "sample");
            Scribe_Collections.Look(ref Studied, "studied", LookMode.Value);
            Scribe_Collections.Look(ref Rescued, "rescued", LookMode.Value);
            Scribe_Values.Look(ref Days, "days");
            Scribe_Values.Look(ref LastDay, "lastDay", -60000);
            Scribe_Values.Look(ref Virtuoso, "virtuoso");
        }
    }
    internal sealed class FacelessMartialPath : AdditionalMartialPath
    {
        internal const string ClassId = "rwom.faceless", Echo = "TM_FacelessEcho";
        private static readonly string[] Forms = { "TM_ReadFacelessInfiltrator", "TM_ReadFacelessUnderstudy" };
        internal override string Id => ClassId;
        internal override string Book => "BookOfFaceless";
        internal override string[] Choices => Forms;
        internal static DisciplineProgress Path(Pawn p) => ClassIdentity.TryGetState(p, out var state) ? state.Find(ClassId) : null;
        internal static DisciplineProgress ActivePath(Pawn body, out Pawn owner)
        {
            owner = body;
            var local = Path(body);
            if (local != null || body == null || !ClassProgressionFeature.Enabled ||
                !SpiritRebirthState.TryGetPossessingSpirit(body, out var soul) ||
                !SpiritRebirthState.TryGet(soul, out var spirit) || spirit.DepletionPending) return local;
            var retained = Path(soul);
            if (retained != null) owner = soul;
            return retained;
        }

        internal static FacelessDisciplineData Data(DisciplineProgress path) => (FacelessDisciplineData)(path.MartialData ??= new FacelessDisciplineData());
        internal override bool CanEnroll(CompAbilityUserMight might, string choice) => Forms.Contains(choice) && TorannMagicDefOf.TM_Mimic != null &&
            TorannMagicDefOf.TM_Disguise != null && DefDatabase<TMAbilityDef>.GetNamedSilentFail(Echo) != null;
        internal override IEnumerable<MightPower> Powers(CompAbilityUserMight might) => might.MightData.MightPowersF.Where(p => p.learned);

        internal override void Restore(CompAbilityUserMight might, DisciplineProgress path)
        {
            Grant(might, TorannMagicDefOf.TM_Mimic);
            Grant(might, TorannMagicDefOf.TM_Disguise);
            if ((path.MartialData as FacelessDisciplineData)?.Sample is TMAbilityDef sample && FacelessImitation.Kind(sample) != EchoKind.None)
                TM_ClassState.AddRuntimeAbility(might, DefDatabase<TMAbilityDef>.GetNamedSilentFail(Echo));
            if (path.Stage >= InitiationStage.Adept) Grant(might, TorannMagicDefOf.TM_Transpose);
            if (path.Stage >= InitiationStage.Master) Grant(might, TorannMagicDefOf.TM_Reversal);
            if (path.Stage == InitiationStage.Ascended && (path.MartialData as FacelessDisciplineData)?.Virtuoso != true)
                Grant(might, TorannMagicDefOf.TM_Possess);
        }
        private static void Grant(CompAbilityUserMight might, TMAbilityDef def)
        {
            if (def == null) return;
            var family = might.MightData.MightPowersF.FirstOrDefault(p => p.TMabilityDefs.Contains(def));
            if (family == null) return;
            family.learned = true;
            TM_ClassState.AddRuntimeAbility(might, family.abilityDef);
        }
        private static string Trial(DisciplineProgress path) => ClassId + "." + path.Stage;
        private static bool Ready(DisciplineProgress path) => path.CompletedTrials.Contains(Trial(path));
        private static int RescueGoal(InitiationStage stage) => stage == InitiationStage.Foundation ? 3 : stage == InitiationStage.Adept ? 4 : 6;
        internal static void Studied(DisciplineProgress path, EchoKind kind)
        {
            if (path.Stage != InitiationStage.Foundation || Ready(path)) return;
            var data = Data(path);
            data.Studied ??= new List<string>();
            if (!data.Studied.Contains(kind.ToString())) data.Studied.Add(kind.ToString());
            if (data.Studied.Count >= 3) path.RecordCompletedTrial(Trial(path));
        }
        internal static void Performed(DisciplineProgress path)
        {
            if (path.Stage != InitiationStage.Adept && path.Stage != InitiationStage.Master || Ready(path)) return;
            var data = Data(path);
            int now = Find.TickManager.TicksGame;
            if ((long)now - data.LastDay < GenDate.TicksPerDay) return;
            data.LastDay = now;
            if (++data.Days >= (path.Stage == InitiationStage.Adept ? 5 : 8)) path.RecordCompletedTrial(Trial(path));
        }
        internal static void Rescue(Pawn pawn, Pawn patient)
        {
            var path = ActivePath(pawn,out _);
            if (path == null || path.Stage >= InitiationStage.Ascended || patient == null || patient.thingIDNumber <= 0 || Ready(path)) return;
            var data = Data(path);
            data.Rescued ??= new List<int>();
            if (!data.Rescued.Contains(patient.thingIDNumber)) data.Rescued.Add(patient.thingIDNumber);
            if (data.Rescued.Count >= RescueGoal(path.Stage)) path.RecordCompletedTrial(Trial(path));
        }
        internal static bool Claim(Pawn pawn, bool virtuoso = false)
        {
            if (LegacyMartialConversion.AlternativeConflicts(pawn,virtuoso)) return false;
            var path = Path(pawn);var might = pawn.GetCompAbilityUserMight();
            if (path == null || !Ready(path) || might?.ExistingMightData == null ||
                path.Stage == InitiationStage.Master && might.MightData.MightUserLevel < 30) return false;
            if (!ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(ClassId, path.Stage, Trial(path))) return false;
            var d = Data(path);d.Days = 0;d.Rescued = null;d.Studied = null;
            if (path.Stage == InitiationStage.Ascended) d.Virtuoso = virtuoso;
            new FacelessMartialPath().Restore(might,path);
            return true;
        }
        internal override string Requirements(Pawn pawn,DisciplineProgress path)
        {
            var d = path.MartialData as FacelessDisciplineData;
            string sample = "TM_FacelessSample".Translate(d?.Sample?.LabelCap.ToString() ?? "—").ToString();
            if (path.Stage == InitiationStage.Ascended)
                return sample + "\n" + (d?.Virtuoso == true ? "TM_FacelessVirtuoso".Translate().ToString() : TorannMagicDefOf.TM_Possess.LabelCap.ToString());
            string requirement = path.Stage == InitiationStage.Foundation ? "TM_FacelessOrigins".Translate(d?.Studied?.Count ?? 0).ToString() :
                "TM_FacelessPractice".Translate(d?.Days ?? 0, path.Stage == InitiationStage.Adept ? 5 : 8).ToString();
            requirement += " " + "TM_FacelessRescue".Translate(d?.Rescued?.Count ?? 0, RescueGoal(path.Stage)).ToString();
            if(path.Stage == InitiationStage.Master) requirement += " " + "TM_DKLevel".Translate(pawn.GetCompAbilityUserMight().MightData.MightUserLevel).ToString();
            return sample + "\n" + requirement;
        }
        internal override float ControlsHeight(Pawn pawn, DisciplineProgress path) => Ready(path) ? 40 : 0;
        internal override void DrawControls(Pawn pawn, DisciplineProgress path, float width, ref float y)
        {
            if (!Ready(path)) return;
            if (Widgets.ButtonText(new Rect(0,y,width,32),"TM_DKClaim".Translate()))
            {
                if (path.Stage != InitiationStage.Master) Claim(pawn);
                else if (pawn.GetCompAbilityUserMight().MightData.MightUserLevel >= 30)
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption> {
                        new FloatMenuOption(TorannMagicDefOf.TM_Possess.LabelCap,()=>Claim(pawn)),
                        new FloatMenuOption("TM_FacelessVirtuoso".Translate(),()=>Claim(pawn,true)) }));
            }
            y += 40f;
        }
    }
}
