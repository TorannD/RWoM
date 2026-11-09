using System.Reflection;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class DeathKnightCareerTests
{
    private sealed class Definitions : IDisposable
    {
        private readonly HediffDef? previous = TorannMagicDefOf.TM_HateHD;
        private readonly Dictionary<TMAbilityDef, Type> types = new();
        internal Definitions()
        {
            TorannMagicDefOf.TM_HateHD = new HediffDef { defName="RegressionKnightHate",hediffClass=typeof(Hediff),
                initialSeverity=.5f,minSeverity=.001f,maxSeverity=200 };
            foreach(var def in new[]{TorannMagicDefOf.TM_WaveOfFear,TorannMagicDefOf.TM_Spite,TorannMagicDefOf.TM_GraveBlade})
            {
                types[def]=def.abilityClass;
                def.abilityClass=typeof(PawnAbility);
            }
        }
        public void Dispose()
        {
            TorannMagicDefOf.TM_HateHD=previous!;
            foreach(var (def,type) in types) def.abilityClass=type;
        }
    }
    internal static IDisposable Setup(RuntimeFixture f) => new Definitions();
    internal static void Verify(RuntimeFixture f,Pawn pawn,CompAbilityUserMight might,CompClassProgression comp,bool alternate)
    {
        var api=typeof(ClassIdentity).Assembly.GetType("TorannMagic.ClassProgression.DeathKnightMartialPath",true)!;
        const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
        object? Call(string method,params object[] args) => api.GetMethod(method,flags)!.Invoke(null,args);
        bool Claim() => (bool)Call("Claim",pawn,alternate)!;
        var actorData = might.MightData;
        void Reload()
        {
            var state=comp.State;
            var file=Path.Combine(f.WorkingDirectory,"deathknight-career.xml");
            try { Scribe.saver.InitSaving(file,"test"); Scribe_Deep.Look(ref state,"state"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
            state=null!;
            try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref state,"state"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
            Set(comp,"state",state);
        }
        var hate=TM_Calc.GetHateHediff(pawn);
        Check.NotNull(hate,"one native hate reservoir");
        var start=alternate?TorannMagicDefOf.TM_Spite:TorannMagicDefOf.TM_WaveOfFear;
        var later=alternate?TorannMagicDefOf.TM_WaveOfFear:TorannMagicDefOf.TM_Spite;
        bool Has(TMAbilityDef ability)=>might.AbilityData.Powers.Any(a=>a.Def==ability);
        Check.Equal(true,Has(start),"chosen first ability");
        Check.Equal(false,Has(later),"second active staged");
        Check.Equal(false,(bool)Call("HasLifeSteal",pawn)!,"no early lifesteal");
        hate.Severity=35f;
        DeathKnightGuardTests.Verify(pawn,alternate,true);
        int now=100;
        var patient=PawnWithComps(false,false).pawn;
        for(int stage=0;stage<3;stage++)
        {
            int repeats=alternate?new[]{5,8,12}[stage]:new[]{3,4,6}[stage];
            for(int i=0;i<repeats;i++)
            {
                Check.Equal(false,Claim(),"no early stage");
                if(alternate)
                {
                    Set(Find.TickManager,"ticksGameInt",now);
                    Call("Battle",pawn); Call("Battle",pawn);
                    now+=60000;
                }
                else
                {
                    patient.thingIDNumber=76000+i;
                    Call("Rescue",pawn,patient); Call("Rescue",pawn,patient);
                }
                if(i==1) Reload();
            }
            if(stage==2)
            {
                actorData.MightUserLevel=29;Check.Equal(false,Claim(),"level gate");
                actorData.MightUserLevel=30;
            }
            Check.Equal(true,Claim(),"confirmed stage");
            if(stage==0) Check.Equal(true,Has(later),"Adept grants remaining core");
            if(stage==1) Check.Equal(true,(bool)Call("HasLifeSteal",pawn)!,"Master lifesteal");
        }
        Check.Equal(35f,hate.Severity,"restoring staged abilities keeps hate");
        Check.Equal(!alternate,Has(TorannMagicDefOf.TM_GraveBlade),"exclusive capstone");
        var guard=typeof(ClassIdentity).Assembly.GetType("TorannMagic.ClassProgression.DeathKnightGuard",true)!.GetMethod("SpendGuard",flags)!;
        bool Spend()=>(bool)guard.Invoke(null,[pawn])!;
        DeathKnightGuardTests.Verify(pawn,alternate,false);
        Reload();Check.Equal(false,Spend(),"cooldown and capstone choice persist");
        Check.Equal(alternate?25f:35f,hate.Severity,"no duplicate charge");
        Check.Equal(false,Claim(),"cannot change capstone");
    }
}
