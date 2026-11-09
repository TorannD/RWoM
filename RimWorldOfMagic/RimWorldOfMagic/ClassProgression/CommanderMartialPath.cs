using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal sealed class CommanderData : AdditionalMartialData
    {
        internal int Serial, LastDay, CrewRank = 1, RallyRank = 1, SignalsRank = 1, OperationUntil;
        internal List<int> Supplies = new(), Evasions = new(), Repairs = new(), Days = new(), Emergencies = new();
        internal bool Operation;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref Serial,"serial");
            Scribe_Values.Look(ref LastDay,"nextService");
            Scribe_Values.Look(ref CrewRank,"crewRank",1);
            Scribe_Values.Look(ref RallyRank,"rallyRank",1);
            Scribe_Values.Look(ref SignalsRank,"signalsRank",1);
            Scribe_Values.Look(ref Operation,"everyoneHome");
            Scribe_Values.Look(ref OperationUntil,"operationUntil");
            Scribe_Collections.Look(ref Supplies,"supplies",LookMode.Value);
            Scribe_Collections.Look(ref Evasions,"rescues",LookMode.Value);
            Scribe_Collections.Look(ref Repairs,"repairs",LookMode.Value);
            Scribe_Collections.Look(ref Days,"days",LookMode.Value);
            Scribe_Collections.Look(ref Emergencies,"emergencies",LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Supplies ??= new(); Evasions ??= new(); Repairs ??= new(); Days ??= new(); Emergencies ??= new();
            }
        }
    }

    internal sealed class CommanderMartialPath : AdditionalMartialPath
    {
        internal const string PathId = "rwom.commander";
        internal const string Crew = "TM_CommanderWorkCrew", Rally = "TM_CommanderRally", Evac = "TM_CommanderOperationEvac", Aid = "TM_CommanderOperationAid";
        private static readonly string[] books = {"TM_ReadCommanderProvisioner","TM_ReadCommanderSignals"};
        internal override string Id => PathId;
        internal override string Book => "BookOfCommander";
        internal override string[] Choices => books;
        internal static DisciplineProgress Path(Pawn p) => ClassIdentity.TryGetState(p,out var state) ? state.Find(PathId) : null;
        internal static Pawn Owner(Pawn actor)
        {
            if (Path(actor) != null || actor?.story?.traits?.HasTrait(TorannMagicDefOf.TM_Commander) == true) return actor;
            if (!SpiritRebirthState.TryGetPossessingSpirit(actor,out var soul) ||
                !SpiritRebirthState.TryGet(soul,out var spirit) || spirit.DepletionPending) return null;
            return Path(soul) != null || soul.story?.traits?.HasTrait(TorannMagicDefOf.TM_Commander) == true ? soul : null;
        }
        internal static CommanderData Data(DisciplineProgress path) => (CommanderData)(path.MartialData ??= new CommanderData());
        internal static CommanderData Existing(DisciplineProgress path) => path?.MartialData as CommanderData;
        internal static TMAbilityDef Def(string name) => DefDatabase<TMAbilityDef>.GetNamedSilentFail(name);
        internal override bool CanEnroll(CompAbilityUserMight might, string choice) =>
            might != null && books.Contains(choice) && Def(Crew) != null && TorannMagicDefOf.TM_ProvisionerAura != null;
        internal override IEnumerable<MightPower> Powers(CompAbilityUserMight might) => might.MightData.MightPowersC.Where(p=>p.learned);

        internal override void Restore(CompAbilityUserMight might, DisciplineProgress path)
        {
            TM_ClassState.AddRuntimeAbility(might,Def(Crew));
            if (!path.MartialAlternate) Grant(might,TorannMagicDefOf.TM_ProvisionerAura);
            if (path.Stage >= InitiationStage.Adept)
            {
                Grant(might,TorannMagicDefOf.TM_MoveOut);
                Grant(might,TorannMagicDefOf.TM_StayAlert);
            }
            if (path.Stage >= InitiationStage.Master)
            {
                Grant(might,TorannMagicDefOf.TM_HoldTheLine);
                TM_ClassState.AddRuntimeAbility(might,Def(Rally));
            }
            if (path.Stage != InitiationStage.Ascended) return;
            if (Existing(path)?.Operation == true)
            {
                TM_ClassState.AddRuntimeAbility(might,Def(Evac));
                TM_ClassState.AddRuntimeAbility(might,Def(Aid));
            }
            else Grant(might,TorannMagicDefOf.TM_CommanderAura);
        }
        private static void Grant(CompAbilityUserMight might, TMAbilityDef ability)
        {
            if (ability == null) return;
            var owner = might.MightData.MightPowersC.FirstOrDefault(p=>p.TMabilityDefs.Contains(ability));
            if (owner?.abilityDef == null) return;
            owner.learned = true;
            TM_ClassState.AddRuntimeAbility(might,owner.abilityDef);
        }
        internal static bool Ready(Pawn p)
        {
            var path=Path(p);
            var d=Existing(path);
            if (d == null) return false;
            return path.Stage switch {
                InitiationStage.Foundation => d.Supplies.Count>=12,
                InitiationStage.Adept => d.Evasions.Count>=8 || d.Repairs.Count>=16,
                InitiationStage.Master => (d.Days.Count>=10 || d.Emergencies.Count>=10) && p.GetCompAbilityUserMight()?.ExistingMightData?.MightUserLevel>=30,
                _ => false
            };
        }
        internal bool Claim(Pawn p,bool operation)
        {
            if (LegacyMartialConversion.AlternativeConflicts(p,operation)) return false;
            if (!Ready(p)) return false;
            var path=Path(p);var stage=path.Stage;
            var might=p.GetCompAbilityUserMight();
            bool definitions = stage switch {
                InitiationStage.Foundation => TorannMagicDefOf.TM_MoveOut!=null && TorannMagicDefOf.TM_StayAlert!=null,
                InitiationStage.Adept => TorannMagicDefOf.TM_HoldTheLine!=null && Def(Rally)!=null,
                InitiationStage.Master => operation ? Def(Evac)!=null && Def(Aid)!=null : TorannMagicDefOf.TM_CommanderAura!=null,
                _ => false
            };
            if (!definitions) return false;
            string trial=PathId+"."+stage;
            path.RecordCompletedTrial(trial);
            if (!ClassIdentity.Component(p).State.TryAdvanceAfterTrial(PathId,stage,trial)) return false;
            if (stage==InitiationStage.Master) Data(path).Operation=operation;
            Restore(might,path);
            return true;
        }
        internal static void WorkDone(Pawn p,int serial,bool supply,bool repair,bool emergency)
        {
            var path=Path(p);
            if (path==null || serial<=0 || path.Stage==InitiationStage.Ascended) return;
            var d=Data(path);
            if (path.Stage==InitiationStage.Foundation && supply) Add(d.Supplies,serial,12);
            if (path.Stage==InitiationStage.Adept && repair) Add(d.Repairs,serial,16);
            if (path.Stage==InitiationStage.Master && (supply || repair || emergency))
            {
                if (emergency) Add(d.Emergencies,serial,10);
                int now=Find.TickManager.TicksGame;
                if (now>=d.LastDay && Add(d.Days,serial,10)) d.LastDay=now+GenDate.TicksPerDay;
            }
        }
        internal static void Escape(Pawn p,Pawn ally)
        {
            var path=Path(p);
            if (path?.Stage == InitiationStage.Adept && ally.thingIDNumber>0) Add(Data(path).Evasions,ally.thingIDNumber,8);
        }
        private static bool Add(List<int> history,int id,int max)
        {
            if (history.Count>=max || history.Contains(id)) return false;
            history.Add(id);return true;
        }
        internal override string Requirements(Pawn p, DisciplineProgress path)
        {
            var d=Existing(path);
            string requirements = path.Stage switch {
                InitiationStage.Foundation => "TM_CommanderSupplyTrial".Translate(d?.Supplies.Count??0),
                InitiationStage.Adept => "TM_CommanderEvacTrial".Translate(d?.Evasions.Count??0,d?.Repairs.Count??0),
                InitiationStage.Master => "TM_CommanderServiceTrial".Translate(d?.Days.Count??0,d?.Emergencies.Count??0,p.GetCompAbilityUserMight()?.ExistingMightData?.MightUserLevel??0),
                _ => (d?.Operation==true?"TM_CommanderChoseOperation":"TM_CommanderChoseAura").Translate()
            };
            return requirements+"\n"+(path.MartialAlternate?"TM_CommanderDoctrineSignals":"TM_CommanderDoctrineSupply").Translate();
        }
        internal override float ControlsHeight(Pawn pawn,DisciplineProgress path) => (path.MartialAlternate?40f:0f)+(Ready(pawn)?40f:0f)+40f+(path.Stage>=InitiationStage.Master?40f:0f);
        internal override void DrawControls(Pawn p,DisciplineProgress path,float width,ref float y)
        {
            if(path.MartialAlternate)
            {
                int rank=Existing(path)?.SignalsRank??1;
                Widgets.Label(new Rect(0,y,width-44,32),"TM_ReadCommanderSignals".Translate()+"  "+rank+"/5");
                if(rank<5 && p.GetCompAbilityUserMight()?.ExistingMightData?.MightAbilityPoints>0 &&
                    Widgets.ButtonText(new Rect(width-42,y,42,30),"+1")) TrainSignals(p,rank);
                y+=40;
            }
            RankRow(p,path,width,ref y,false);
            if(path.Stage>=InitiationStage.Master) RankRow(p,path,width,ref y,true);
            if(!Ready(p)) return;
            if(Widgets.ButtonText(new Rect(0,y,width,32),"TM_CommanderAdvance".Translate()))
            {
                if(path.Stage==InitiationStage.Master)
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>{
                        new FloatMenuOption(TorannMagicDefOf.TM_CommanderAura.LabelCap,()=>Claim(p,false)),
                        new FloatMenuOption("TM_CommanderEveryoneHome".Translate(),()=>Claim(p,true))
                    }));
                else Claim(p,false);
            }
            y+=40;
        }
        private static void RankRow(Pawn pawn,DisciplineProgress path,float width,ref float y,bool rally)
        {
            var data=Existing(path);
            int rank=(rally?data?.RallyRank:data?.CrewRank)??1;
            var ability=Def(rally?Rally:Crew);
            Widgets.Label(new Rect(0,y,width-44,32),(ability?.LabelCap.ToString()??"")+"  "+rank+"/3");
            if(rank<3 && pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightAbilityPoints>0 &&
                Widgets.ButtonText(new Rect(width-42,y,42,30),"+1")) Train(pawn,rally,rank);
            y+=40;
        }
        internal static bool TrainSignals(Pawn p,int shown)
        {
            var path=Path(p);
            var might=p?.GetCompAbilityUserMight()?.ExistingMightData;
            if(path?.MartialAlternate!=true || might==null || might.MightAbilityPoints<1) return false;
            var d=Data(path);
            if(shown!=d.SignalsRank || shown<1 || shown>=5) return false;
            d.SignalsRank++; might.MightAbilityPoints--; return true;
        }
        internal static float Acceptance(Pawn p,float native)
        {
            var path=Path(p);
            return path?.MartialAlternate==true ? Mathf.Min(.95f,native+.02f*Mathf.Clamp(Existing(path)?.SignalsRank??1,1,5)) : native;
        }
        internal static bool Train(Pawn p,bool rally,int shown)
        {
            var path=Path(p);
            var might=p?.GetCompAbilityUserMight()?.ExistingMightData;
            if(path==null || might==null || might.MightAbilityPoints<1 || (rally && path.Stage<InitiationStage.Master)) return false;
            var d=Data(path);int rank=rally?d.RallyRank:d.CrewRank;
            if(rank!=shown || rank>=3 || rank<1) return false;
            if(rally) d.RallyRank++; else d.CrewRank++;
            might.MightAbilityPoints--;
            return true;
        }
    }
}
