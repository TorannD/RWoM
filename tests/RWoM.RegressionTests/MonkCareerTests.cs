using System.Reflection;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class MonkCareerTests
{
    private sealed class Definitions : IDisposable
    {
        private readonly HediffDef? old=TorannMagicDefOf.TM_ChiHD;
        private readonly HediffDef? oldBody=TorannMagicDefOf.TM_MindOverBodyHD;
        private readonly Dictionary<TMAbilityDef,Type> types=new();
        internal Definitions(RuntimeFixture f)
        {
            TorannMagicDefOf.TM_ChiHD=new HediffDef{defName="RegressionMonkChi",hediffClass=typeof(Hediff),initialSeverity=.5f,minSeverity=.001f,maxSeverity=200};
            TorannMagicDefOf.TM_MindOverBodyHD=new HediffDef{defName="RegressionMonkBody",hediffClass=typeof(Hediff),initialSeverity=.5f,minSeverity=.001f,maxSeverity=4};
            foreach (var def in new[]{TorannMagicDefOf.TM_TigerStrike,TorannMagicDefOf.TM_DragonStrike,TorannMagicDefOf.TM_Meditate,TorannMagicDefOf.TM_ChiBurst,
                DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_MonkPalmSeal")??f.AddAbility("TM_MonkPalmSeal"),
                DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_MonkSilentMeridian")??f.AddAbility("TM_MonkSilentMeridian")})
            {
                types[def]=def.abilityClass;
                def.abilityClass=def.defName=="TM_MonkSilentMeridian"?typeof(MonkMeridianAbility):typeof(PawnAbility);
            }
        }
        public void Dispose()
        {
            foreach(var item in types)item.Key.abilityClass=item.Value;
            TorannMagicDefOf.TM_ChiHD=old!;
            TorannMagicDefOf.TM_MindOverBodyHD=oldBody!;
        }
    }
    internal static IDisposable Setup(RuntimeFixture f)=>new Definitions(f);
    private sealed class Wound : Hediff_Injury
    {
        internal bool bandaged;
        public override bool TendableNow(bool ignoreTimer=false)=>!bandaged;
    }
    internal static void Verify(RuntimeFixture f,Pawn pawn,CompAbilityUserMight might,CompClassProgression comp,bool alternate)
    {
        var asm=typeof(SniperSurvey).Assembly;
        var api=asm.GetType("TorannMagic.ClassProgression.MonkMartialPath",true)!;
        const BindingFlags hidden=BindingFlags.Static|BindingFlags.NonPublic;
        object? Call(string name,params object[] args)=>api.GetMethod(name,hidden)!.Invoke(null,args);
        var file=Path.Combine(f.WorkingDirectory,"monk-career.xml");
        void Reload()
        {
            var state=comp.State;
            try{Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref state,"state");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
            state=null!;
            try{Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref state,"state");Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
            Set(comp,"state",state);
        }
        int points=might.MightData.MightAbilityPoints;
        var chi=pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD);
        Check.NotNull(chi,"native chi reservoir supplied");
        chi.Severity=30;
        MonkNativeTrialTests.Verify(pawn,alternate);
        Check.Equal(0,might.AbilityData.Powers.Count(p=>p.Def==TorannMagicDefOf.TM_ChiBurst),"classic Chi child cannot give early ultimate");
        Check.Equal(1,might.AbilityData.Powers.Count(p=>p.Def==TorannMagicDefOf.TM_Meditate),"meditate unlocked");
        might.MightData.MightAbilityPoints=2;
        Check.Equal(true,(bool)Call("Upgrade",pawn,1)!,"rank purchase");
        Check.Equal(false,(bool)Call("Upgrade",pawn,1)!,"stale click rejected");
        Reload();
        Check.Equal(true,(bool)Call("Upgrade",pawn,2)!,"second purchase");
        Check.Equal(0,might.MightData.MightAbilityPoints,"exact spending");
        var med=asm.GetType("TorannMagic.ClassProgression.MonkMeditation",true)!.GetMethod("Finish",hidden)!;
        Check.Equal(alternate,(bool)med.Invoke(null,[pawn])!,"only cultivation route gains direct chi");
        Check.Equal(alternate?40f:30f,chi.Severity,"ranked chi award");
        var injury=new Wound{def=new HediffDef{defName="MonkPatientWound",tendable=true},bandaged=true};
        var wounds=new List<Hediff_Injury>{injury};
        for(int i=1;i<=(alternate?20:8);i++)
        {
            Check.Equal(false,(bool)Call("Claim",pawn,false)!,"no early advancement");
            if(alternate) {injury.loadID=i;Call("Treatment",pawn,wounds);Call("Treatment",pawn,wounds);}
            else Call("Interrupted",pawn);
            if(i==3) Reload();
        }
        Check.Equal(true,(bool)Call("Claim",pawn,false)!,"tiger unlocked");
        Check.Equal(1,might.AbilityData.Powers.Count(p=>p.Def==TorannMagicDefOf.TM_TigerStrike),"tiger owner");
        for(int i=0;i<10;i++)
        {
            Set(Find.TickManager,"ticksGameInt",1000+i*60000);
            Call("Cultivated",pawn);Call("Cultivated",pawn);
            Check.Equal(false,(bool)Call("Claim",pawn,false)!,"cultivation by itself cannot advance");
            Call("Treatment",pawn,wounds);
            if(i==4) Reload();
        }
        Check.Equal(true,(bool)Call("Claim",pawn,false)!,"dragon unlocked");
        Check.Equal(1,might.AbilityData.Powers.Count(p=>p.Def==TorannMagicDefOf.TM_DragonStrike),"dragon owner");
        var ally=PawnWithComps(false,false).pawn;ally.thingIDNumber=71123;
        for(int i=0;i<(alternate?10:8);i++)
        {
            Set(Find.TickManager,"ticksGameInt",800000+i*60000);
            if(alternate){Call("Relief",pawn);Call("Relief",pawn);}
            else{Call("Rescue",pawn,ally);Call("Rescue",pawn,ally);}
        }
        might.MightData.MightUserLevel=29;
        Check.Equal(false,(bool)Call("Claim",pawn,alternate)!,"level 29 gates graduation");
        Reload();
        might.MightData.MightUserLevel=30;
        Check.Equal(true,(bool)Call("Claim",pawn,alternate)!,"final choice");
        Check.Equal(false,(bool)Call("Claim",pawn,!alternate)!,"cannot change ultimate");
        var selected=alternate?DefDatabase<TMAbilityDef>.GetNamed("TM_MonkSilentMeridian"):TorannMagicDefOf.TM_ChiBurst;
        var other=alternate?TorannMagicDefOf.TM_ChiBurst:DefDatabase<TMAbilityDef>.GetNamed("TM_MonkSilentMeridian");
        Reload();
        might.AbilityData.Powers.RemoveAll(p=>p.Def==selected);
        var inst=Activator.CreateInstance(api,true)!;
        api.GetMethod("Restore",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(inst,[might,comp.State.Find("rwom.monk")]);
        Check.Equal(1,might.AbilityData.Powers.Count(p=>p.Def==selected),"chosen ultimate restored");
        Check.Equal(0,might.AbilityData.Powers.Count(p=>p.Def==other),"other ultimate excluded");
        Check.Equal(alternate?40f:30f,chi.Severity,"restore preserves existing chi");
        var bodyTraining=pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MindOverBodyHD);
        Check.NotNull(bodyTraining,"new monk restores physical training without legacy class trait");
        var trainingRank=might.MightData.MightPowerSkill_MindOverBody.Single(s=>s.label=="TM_MindOverBody_pwr");
        trainingRank.level=1;
        GrimoireKnowledge.SyncRank(pawn,TorannMagicDefOf.TM_MindOverBody,trainingRank.label,trainingRank.level);
        Check.Equal(1.5f,bodyTraining.Severity,"grimoire rank purchase refreshes new monk passive immediately");
        Check.Equal(alternate?40f:30f,chi.Severity,"rank refresh does not replenish chi");
        might.MightData.MightPowerSkill_MindOverBody.Single(s=>s.label=="TM_MindOverBody_pwr").level=3;
        api.GetMethod("Restore",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(inst,[might,comp.State.Find("rwom.monk")]);
        Check.Equal(3.5f,bodyTraining.Severity,"body training reflects retained rank");
        Check.Equal(1,pawn.health.hediffSet.hediffs.Count(h=>h.def==TorannMagicDefOf.TM_MindOverBodyHD),"restore does not duplicate body training");
        might.MightData.MightAbilityPoints=points;
        if(alternate) MonkMeridianTests.Verify(f,pawn,might);
    }
}
