using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal sealed class PsionicProgress : AdditionalMartialData
    {
        internal float HostileDamage;
        internal int Repairs, Rescues, Relief, Days, NextDay, NextRelief, DisruptedUntil, CrucibleReadyAt;
        internal bool Crucible;
        internal List<int> RepairTargets=new(), RescuedTargets=new();
        public override void ExposeData()
        {
            Scribe_Values.Look(ref HostileDamage,"hostileDamage");
            Scribe_Values.Look(ref Repairs,"repairs");
            Scribe_Values.Look(ref Rescues,"rescues");
            Scribe_Values.Look(ref Relief,"relief");
            Scribe_Values.Look(ref Days,"days");
            Scribe_Values.Look(ref NextDay,"nextDay");
            Scribe_Values.Look(ref NextRelief,"nextRelief");
            Scribe_Values.Look(ref DisruptedUntil,"disruptedUntil");
            Scribe_Values.Look(ref CrucibleReadyAt,"crucibleReadyAt");
            Scribe_Values.Look(ref Crucible,"crucible");
            Scribe_Collections.Look(ref RepairTargets,"repairTargets",LookMode.Value);
            Scribe_Collections.Look(ref RescuedTargets,"rescuedTargets",LookMode.Value);
            if (Scribe.mode==LoadSaveMode.PostLoadInit) {RepairTargets??=new();RescuedTargets??=new();}
        }
    }

    internal sealed class PsionicMartialPath : AdditionalMartialPath
    {
        internal const string Career="rwom.psionic";
        internal const string Crucible="TM_PsionicKineticCrucible";
        internal override string Id=>Career;
        internal override string Book=>"BookOfPsionic";
        internal override string[] Choices { get; }={"TM_ReadPsionicProjector","TM_ReadPsionicAccelerator"};
        internal static DisciplineProgress Path(Pawn p)=> ClassIdentity.TryGetState(p,out var state)?state.Find(Career):null;
        internal static DisciplineProgress ActivePath(Pawn body,out Pawn owner) =>
            AdditionalMartialRegistry.ActingPath(body,Career,out owner);
        internal static PsionicProgress Read(DisciplineProgress p)=>p?.MartialData as PsionicProgress;
        internal static PsionicProgress Write(DisciplineProgress p)=>(PsionicProgress)(p.MartialData??=new PsionicProgress());
        internal static Hediff Energy(Pawn p)=>TorannMagicDefOf.TM_PsionicHD==null?null:p?.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_PsionicHD);
        internal static float Capacity(Pawn p)=>100f+(TorannMagicDefOf.TM_Artifact_PsionicBoostHD==null?0:p?.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_Artifact_PsionicBoostHD)?.Severity??0f);
        internal static float ProjectileCost(Pawn pawn,float original) => Path(pawn) is DisciplineProgress { MartialAlternate:false } ? original*.9f : original;
        internal static float BlastFactor(Pawn pawn) => Path(pawn) is DisciplineProgress p ? (p.MartialAlternate ? 1.08f : .90f) : 1f;
        internal static bool Quiet(Pawn pawn) => Read(Path(pawn)) is PsionicProgress progress &&
            (pawn.jobs?.curDriver is JobDriver_PsionicCrucible ||
                SpiritRebirthState.TryGet(pawn,out var soul) && soul.HasValidHost(pawn) && soul.Host?.jobs?.curDriver is JobDriver_PsionicCrucible hosted && hosted.SkillOwner==pawn ||
                progress.DisruptedUntil>Find.TickManager.TicksGame);
        internal override bool SupportsBook(ThingDef book) => book?.defName==Book &&
            !TM_ClassUtility.CustomClasses.Any(c=>c!=null && (c.fullScript?.defName==Book || c.tornScript?.defName==Book));
        internal static bool CanAwaken(Pawn pawn)
        {
            var conditions=pawn?.Map?.GameConditionManager;
            return pawn?.Spawned==true && (conditions?.ConditionIsActive(GameConditionDefOf.PsychicSoothe)==true ||
                conditions?.ConditionIsActive(GameConditionDefOf.PsychicDrone)==true || pawn.GetStatValue(StatDefOf.PsychicSensitivity,false)>1f);
        }
        internal override bool CanEnroll(CompAbilityUserMight might,string choice)=>might?.Pawn.health!=null && CanAwaken(might.Pawn) && TorannMagicDefOf.TM_PsionicHD!=null && TorannMagicDefOf.TM_PsionicAugmentation!=null && TorannMagicDefOf.TM_PsionicBarrier!=null && TorannMagicDefOf.TM_PsionicBlast!=null;
        internal override IEnumerable<MightPower> Powers(CompAbilityUserMight might)=>might.MightData.MightPowersP.Where(p=>p.learned);
        private static void Grant(CompAbilityUserMight might,TMAbilityDef def,bool active=true)
        {
            if(def==null)return;
            var owner=might.MightData.MightPowersP.FirstOrDefault(p=>p.TMabilityDefs.Contains(def));
            if(owner==null)return;
            owner.learned=true;
            if(active)TM_ClassState.AddRuntimeAbility(might,owner.abilityDef);
        }
        internal override void Restore(CompAbilityUserMight might,DisciplineProgress p)
        {
            if(Energy(might.Pawn)==null && TorannMagicDefOf.TM_PsionicHD!=null)might.Pawn.health.AddHediff(TorannMagicDefOf.TM_PsionicHD);
            Grant(might,TorannMagicDefOf.TM_PsionicAugmentation,false);
            if(!p.MartialAlternate || p.Stage>=InitiationStage.Adept)Grant(might,TorannMagicDefOf.TM_PsionicBarrier);
            if(p.MartialAlternate || p.Stage>=InitiationStage.Adept)Grant(might,TorannMagicDefOf.TM_PsionicBlast);
            if(p.Stage>=InitiationStage.Master)Grant(might,TorannMagicDefOf.TM_PsionicDash);
            if(p.Stage!=InitiationStage.Ascended)return;
            if(Read(p)?.Crucible==true)TM_ClassState.AddRuntimeAbility(might,DefDatabase<TMAbilityDef>.GetNamedSilentFail(Crucible));
            else Grant(might,TorannMagicDefOf.TM_PsionicStorm);
        }
        private static string Trial(DisciplineProgress p)=>Career+"."+p.Stage;
        internal static bool Ready(DisciplineProgress p)=>p!=null && p.Stage<InitiationStage.Ascended && p.CompletedTrials.Contains(Trial(p));
        internal static void Service(Pawn pawn,float hostileDamage=0,int repairId=0,int rescueId=0,bool relief=false)
        {
            var p=ActivePath(pawn,out _);
            RecordService(p,hostileDamage,repairId,rescueId,relief);
        }

        internal static void RecordService(DisciplineProgress p,float hostileDamage=0,int repairId=0,int rescueId=0,bool relief=false)
        {
            if(p==null || Ready(p) || p.Stage==InitiationStage.Ascended || (hostileDamage<=0 && repairId<=0 && rescueId<=0 && !relief))return;
            var d=Write(p);var now=Find.TickManager.TicksGame;
            if(p.Stage==InitiationStage.Foundation)
            {
                d.HostileDamage=Mathf.Min(200,d.HostileDamage+Mathf.Max(hostileDamage,0));
                if(repairId>0 && !d.RepairTargets.Contains(repairId)){d.RepairTargets.Add(repairId);d.Repairs++;}
                if(d.HostileDamage>=200 || d.Repairs>=20)p.RecordCompletedTrial(Trial(p));
            }
            else if(p.Stage==InitiationStage.Adept)
            {
                if(rescueId>0 && !d.RescuedTargets.Contains(rescueId)){d.RescuedTargets.Add(rescueId);d.Rescues++;}
                if(relief && now>=d.NextRelief){d.Relief++;d.NextRelief=now+2500;}
                if(d.Rescues>=10 || d.Relief>=30)p.RecordCompletedTrial(Trial(p));
            }
            else if(now>=d.NextDay)
            {
                d.NextDay=now+GenDate.TicksPerDay;
                if(++d.Days>=12)p.RecordCompletedTrial(Trial(p));
            }
        }
        internal static bool Claim(Pawn pawn,bool crucible)
        {
            if (LegacyMartialConversion.AlternativeConflicts(pawn,crucible)) return false;
            var p=Path(pawn);var might=pawn?.GetCompAbilityUserMight();
            if(!Ready(p) || might?.ExistingMightData==null)return false;
            if(p.Stage==InitiationStage.Master && (might.MightData.MightUserLevel<30 ||
                (crucible?DefDatabase<TMAbilityDef>.GetNamedSilentFail(Crucible):TorannMagicDefOf.TM_PsionicStorm)==null))return false;
            if(!ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(Career,p.Stage,Trial(p)))return false;
            if(p.Stage==InitiationStage.Ascended)Write(p).Crucible=crucible;
            new PsionicMartialPath().Restore(might,p);
            return true;
        }
        internal override string Requirements(Pawn pawn,DisciplineProgress p)
        {
            var d=Read(p);
            string trial=p.Stage switch {
                InitiationStage.Foundation=>"TM_PsionicFoundationTrial".Translate((int)(d?.HostileDamage??0),d?.Repairs??0),
                InitiationStage.Adept=>"TM_PsionicAdeptTrial".Translate(d?.Rescues??0,d?.Relief??0),
                InitiationStage.Master=>"TM_PsionicMasterTrial".Translate(d?.Days??0,pawn.GetCompAbilityUserMight().MightData.MightUserLevel),
                _=>(d?.Crucible==true?DefDatabase<TMAbilityDef>.GetNamedSilentFail(Crucible):TorannMagicDefOf.TM_PsionicStorm)?.LabelCap.ToString()??""
            };
            return (p.MartialAlternate ? "TM_PsionicAcceleratorInfo" : "TM_PsionicProjectorInfo").Translate()+"\n"+trial;
        }
        internal override float ControlsHeight(Pawn pawn,DisciplineProgress p)=>Ready(p)?40:0;
        internal override void DrawControls(Pawn pawn,DisciplineProgress p,float width,ref float y)
        {
            if(!Ready(p))return;
            if((p.Stage!=InitiationStage.Master || pawn.GetCompAbilityUserMight().MightData.MightUserLevel>=30) &&
                Widgets.ButtonText(new Rect(0,y,width,32),"TM_PsionicAdvance".Translate()))
            {
                if(p.Stage!=InitiationStage.Master)Claim(pawn,false);
                else Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>{
                    new FloatMenuOption(TorannMagicDefOf.TM_PsionicStorm.LabelCap,()=>Claim(pawn,false)),
                    new FloatMenuOption(DefDatabase<TMAbilityDef>.GetNamed(Crucible).LabelCap,()=>Claim(pawn,true))}));
            }
            y+=40;
        }
    }
}
