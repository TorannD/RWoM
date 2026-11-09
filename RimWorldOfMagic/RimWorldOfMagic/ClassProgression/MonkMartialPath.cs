using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal sealed class MonkDisciplineData : AdditionalMartialData
    {
        internal int Interrupts, Days, Rescues, ReliefDays, SealUpgrades, MeridianReadyAt;
        internal int CultivatedAt = -60000, TreatedAt = -60000, NextDay, NextRelief;
        internal List<int> Wounds;
        internal Dictionary<int,int> Casualties;
        internal bool Meridian;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref Interrupts,"interrupts");
            Scribe_Values.Look(ref Days,"days");
            Scribe_Values.Look(ref Rescues,"rescues");
            Scribe_Values.Look(ref ReliefDays,"reliefDays");
            Scribe_Values.Look(ref CultivatedAt,"cultivatedAt",-60000);
            Scribe_Values.Look(ref TreatedAt,"treatedAt",-60000);
            Scribe_Values.Look(ref NextDay,"nextDay");
            Scribe_Values.Look(ref NextRelief,"nextRelief");
            Scribe_Values.Look(ref SealUpgrades,"sealUpgrades");
            Scribe_Values.Look(ref Meridian,"meridian");
            Scribe_Values.Look(ref MeridianReadyAt,"meridianReadyAt");
            Scribe_Collections.Look(ref Wounds,"wounds",LookMode.Value);
            Scribe_Collections.Look(ref Casualties,"casualties",LookMode.Value,LookMode.Value);
        }
    }

    internal sealed class MonkMartialPath : AdditionalMartialPath
    {
        internal const string ClassId="rwom.monk", SealName="TM_MonkPalmSeal", MeridianName="TM_MonkSilentMeridian";
        private static readonly string[] Forms={"TM_ReadMonkRestorative","TM_ReadMonkCultivation"};
        internal override string Id => ClassId;
        internal override string Book => "BookOfMonk";
        internal override string[] Choices => Forms;
        internal static DisciplineProgress Path(Pawn pawn) => ClassIdentity.TryGetState(pawn,out var state) ? state.Find(ClassId) : null;
        internal static bool IsMonk(Pawn pawn) => Path(pawn) != null || pawn?.story?.traits?.HasTrait(TorannMagicDefOf.TM_Monk) == true;
        internal static Pawn Owner(Pawn actor)
        {
            if (IsMonk(actor)) return actor;
            if (!SpiritRebirthState.TryGetPossessingSpirit(actor, out var soul) ||
                !SpiritRebirthState.TryGet(soul, out var spirit) || spirit.DepletionPending) return null;
            return IsMonk(soul) ? soul : null;
        }
        internal static MonkDisciplineData Data(DisciplineProgress path) => path?.MartialData as MonkDisciplineData;
        internal static MonkDisciplineData Writable(DisciplineProgress path) => (MonkDisciplineData)(path.MartialData ??= new MonkDisciplineData());
        internal static Hediff Chi(Pawn pawn) => TorannMagicDefOf.TM_ChiHD == null ? null : pawn?.health?.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD);
        internal static float Capacity(Hediff chi) => chi?.TryGetComp<HediffComp_Chi>()?.maxSev ?? 100f;

        internal override bool CanEnroll(CompAbilityUserMight might,string choice) =>
            might?.Pawn.health != null && Forms.Contains(choice) && TorannMagicDefOf.TM_ChiHD != null &&
            TorannMagicDefOf.TM_Chi != null && TorannMagicDefOf.TM_Meditate != null && DefDatabase<TMAbilityDef>.GetNamedSilentFail(SealName)!=null;
        internal override IEnumerable<MightPower> Powers(CompAbilityUserMight might) => might.MightData.MightPowersM.Where(p=>p.learned);

        internal override void Restore(CompAbilityUserMight might, DisciplineProgress path)
        {
            if (Chi(might.Pawn)==null && TorannMagicDefOf.TM_ChiHD != null)
                might.Pawn.health.AddHediff(TorannMagicDefOf.TM_ChiHD);
            RefreshTraining(might.Pawn);
            Grant(might,TorannMagicDefOf.TM_Chi,false);
            Grant(might,TorannMagicDefOf.TM_Meditate,true);
            TM_ClassState.AddRuntimeAbility(might,DefDatabase<TMAbilityDef>.GetNamedSilentFail(SealName));
            if (path.Stage>=InitiationStage.Adept) Grant(might,TorannMagicDefOf.TM_TigerStrike,true);
            if (path.Stage>=InitiationStage.Master) Grant(might,TorannMagicDefOf.TM_DragonStrike,true);
            if (path.Stage==InitiationStage.Ascended)
                TM_ClassState.AddRuntimeAbility(might,Data(path)?.Meridian==true ? DefDatabase<TMAbilityDef>.GetNamedSilentFail(MeridianName) : TorannMagicDefOf.TM_ChiBurst);
        }
        internal static void RefreshTraining(Pawn pawn)
        {
            if (Path(pawn)==null || pawn.health==null) return;
            var might=pawn.GetCompAbilityUserMight();
            if (might?.ExistingMightData==null) return;
            if (TorannMagicDefOf.TM_MindOverBodyHD != null)
            {
                var training = might.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MindOverBodyHD);
                if (training == null) training = might.Pawn.health.AddHediff(TorannMagicDefOf.TM_MindOverBodyHD);
                int rank = might.MightData.MightPowerSkill_MindOverBody.FirstOrDefault(x => x.label == "TM_MindOverBody_pwr")?.level ?? 0;
                training.Severity = Mathf.Clamp(rank, 0, 3) + .5f;
            }
        }
        private static void Grant(CompAbilityUserMight might,TMAbilityDef def,bool active)
        {
            if (def==null) return;
            var power=might.MightData.MightPowersM.FirstOrDefault(p=>p.TMabilityDefs.Contains(def));
            if (power == null) return;
            power.learned=true;
            if (active) TM_ClassState.AddRuntimeAbility(might,power.abilityDef);
        }
        private static string Trial(InitiationStage stage)=>ClassId+"."+stage;
        internal static bool Ready(DisciplineProgress path) => path!=null && path.Stage<InitiationStage.Ascended && path.CompletedTrials.Contains(Trial(path.Stage));
        internal static bool Claim(Pawn pawn,bool meridian=false)
        {
            if (LegacyMartialConversion.AlternativeConflicts(pawn,meridian)) return false;
            var path=Path(pawn);
            if (!Ready(path)) return false;
            var might=pawn.GetCompAbilityUserMight();
            if (might?.ExistingMightData==null) return false;
            if (path.Stage==InitiationStage.Master && (might.MightData.MightUserLevel<30 ||
                (meridian ? DefDatabase<TMAbilityDef>.GetNamedSilentFail(MeridianName) : TorannMagicDefOf.TM_ChiBurst)==null)) return false;
            if (!ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(ClassId,path.Stage,Trial(path.Stage))) return false;
            if (path.Stage==InitiationStage.Ascended) Writable(path).Meridian=meridian;
            new MonkMartialPath().Restore(might,path);
            return true;
        }

        internal static void Interrupted(Pawn pawn)
        {
            var path=Path(Owner(pawn));
            if (path?.Stage!=InitiationStage.Foundation || Ready(path)) return;
            if (++Writable(path).Interrupts>=8) path.RecordCompletedTrial(Trial(path.Stage));
        }
        internal static void Treatment(Pawn pawn,List<Hediff_Injury> wounds)
        {
            var path=Path(Owner(pawn));
            if (path==null || path.Stage>InitiationStage.Adept || Ready(path)) return;
            var d=Writable(path);
            if (path.Stage==InitiationStage.Foundation)
            {
                d.Wounds ??= new List<int>();
                foreach (var wound in wounds)
                    if (wound.loadID>0 && !wound.TendableNow() && !d.Wounds.Contains(wound.loadID))
                    {
                        d.Wounds.Add(wound.loadID);
                        if(d.Wounds.Count==20) {path.RecordCompletedTrial(Trial(path.Stage)); break;}
                    }
            }
            else if (wounds.Any(w=>!w.TendableNow())) MedicalWork(path);
        }

        internal static void MedicalWork(DisciplineProgress path)
        {
            var d=Writable(path);
            d.TreatedAt=Find.TickManager.TicksGame;
            CountDay(path,d);
        }
        internal static void Cultivated(Pawn pawn)
        {
            var path=Path(pawn);
            if (path?.Stage!=InitiationStage.Adept || Ready(path)) return;
            var d=Writable(path);
            d.CultivatedAt=Find.TickManager.TicksGame;
            CountDay(path,d);
        }
        private static void CountDay(DisciplineProgress path, MonkDisciplineData d)
        {
            int now=Find.TickManager.TicksGame;
            if (now<d.NextDay || now-(long)d.TreatedAt>=GenDate.TicksPerDay || now-(long)d.CultivatedAt>=GenDate.TicksPerDay) return;
            d.NextDay=now+GenDate.TicksPerDay;
            d.TreatedAt=d.CultivatedAt=-GenDate.TicksPerDay;
            if (++d.Days==10) path.RecordCompletedTrial(Trial(path.Stage));
        }
        internal static void Rescue(Pawn pawn,Pawn patient)
        {
            var path=Path(Owner(pawn));
            if (pawn.equipment?.Primary!=null || patient.thingIDNumber<=0 || Ready(path)) return;
            if(path?.Stage==InitiationStage.Adept) { MedicalWork(path); return; }
            if (path?.Stage!=InitiationStage.Master) return;
            var d=Writable(path); int now=Find.TickManager.TicksGame;
            d.Casualties ??= new Dictionary<int,int>();
            if (d.Casualties.TryGetValue(patient.thingIDNumber,out var last) && (long)now-last<GenDate.TicksPerDay) return;
            d.Casualties[patient.thingIDNumber]=now;
            if (++d.Rescues==8) path.RecordCompletedTrial(Trial(path.Stage));
        }
        internal static void Relief(Pawn pawn)
        {
            var path=Path(Owner(pawn));
            if (path?.Stage!=InitiationStage.Master || pawn.equipment?.Primary!=null || Ready(path)) return;
            var d=Writable(path); int now=Find.TickManager.TicksGame;
            if (now<d.NextRelief) return;
            d.NextRelief=now+GenDate.TicksPerDay;
            if (++d.ReliefDays==10) path.RecordCompletedTrial(Trial(path.Stage));
        }
        internal override string Requirements(Pawn pawn,DisciplineProgress path)
        {
            var d=Data(path);
            return path.Stage switch {
                InitiationStage.Foundation => "TM_MonkFoundationTrial".Translate(d?.Interrupts??0,d?.Wounds?.Count??0),
                InitiationStage.Adept => "TM_MonkAdeptTrial".Translate(d?.Days??0),
                InitiationStage.Master => "TM_MonkMasterTrial".Translate(d?.Rescues??0,d?.ReliefDays??0,pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightUserLevel??0),
                _ => (d?.Meridian==true ? DefDatabase<TMAbilityDef>.GetNamedSilentFail(MeridianName) : TorannMagicDefOf.TM_ChiBurst)?.LabelCap.ToString() ?? ""
            };
        }
        internal override float ControlsHeight(Pawn p,DisciplineProgress path) => 40f+(Ready(path)?40f:0);
        internal override void DrawControls(Pawn pawn,DisciplineProgress path,float width,ref float y)
        {
            int rank=Rank(pawn);
            string label="TM_MonkSealRank".Translate(rank);
            if (rank<3 && pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightAbilityPoints>=1)
            {
                if (Widgets.ButtonText(new Rect(0,y,width,32f),label+"  +1")) Upgrade(pawn,rank);
            }
            else Widgets.Label(new Rect(0,y,width,32f),label);
            y+=40;
            if (!Ready(path)) return;
            if (path.Stage!=InitiationStage.Master || pawn.GetCompAbilityUserMight().MightData.MightUserLevel>=30)
            {
                if (Widgets.ButtonText(new Rect(0,y,width,32f),"TM_MonkAdvance".Translate()))
                {
                    if (path.Stage==InitiationStage.Master)
                        Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>{
                            new FloatMenuOption(TorannMagicDefOf.TM_ChiBurst.LabelCap,()=>Claim(pawn)),
                            new FloatMenuOption(DefDatabase<TMAbilityDef>.GetNamed(MeridianName).LabelCap,()=>Claim(pawn,true))}));
                    else Claim(pawn);
                }
            }
            y+=40;
        }
        internal static int Rank(Pawn pawn) => 1 + Mathf.Clamp(Data(Path(pawn))?.SealUpgrades??0,0,2);
        internal static bool Upgrade(Pawn pawn,int from)
        {
            var path=Path(pawn);var might=pawn?.GetCompAbilityUserMight()?.ExistingMightData;
            if (path==null || might==null || might.MightAbilityPoints<1 || from<1 || from>=3 || Rank(pawn)!=from) return false;
            Writable(path).SealUpgrades=from;
            might.MightAbilityPoints--;
            return true;
        }
    }
}
