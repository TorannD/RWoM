using System.Reflection;
using System.Xml.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class PsionicCareerTests
{
    const BindingFlags hidden=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    private sealed class Definitions : IDisposable
    {
        readonly HediffDef? old=TorannMagicDefOf.TM_PsionicHD;
        readonly Dictionary<TMAbilityDef,Type> types=new();
        readonly GameConditionManager oldManager=Find.Maps[0].gameConditionManager;
        readonly GameConditionDef oldSoothe=GameConditionDefOf.PsychicSoothe;
        internal Definitions(RuntimeFixture f)
        {
            var map=Find.Maps[0];map.gameConditionManager=new GameConditionManager(map);
            GameConditionDefOf.PsychicSoothe=new GameConditionDef{defName="TestSoothe"};
            map.gameConditionManager.ActiveConditions.Add(new GameCondition{def=GameConditionDefOf.PsychicSoothe});
            TorannMagicDefOf.TM_PsionicHD=new HediffDef{defName="TestPsi",hediffClass=typeof(Hediff),initialSeverity=90,minSeverity=.001f,maxSeverity=200};
            foreach(var def in new[]{TorannMagicDefOf.TM_PsionicBarrier,TorannMagicDefOf.TM_PsionicBlast,TorannMagicDefOf.TM_PsionicDash,TorannMagicDefOf.TM_PsionicStorm,
                DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_PsionicKineticCrucible")??f.AddAbility("TM_PsionicKineticCrucible")})
            {types[def]=def.abilityClass;def.abilityClass=def.defName=="TM_PsionicKineticCrucible"?typeof(PsionicCrucibleAbility):typeof(PawnAbility);}
        }
        public void Dispose(){Find.Maps[0].gameConditionManager=oldManager;GameConditionDefOf.PsychicSoothe=oldSoothe;foreach(var kv in types)kv.Key.abilityClass=kv.Value;TorannMagicDefOf.TM_PsionicHD=old!;}
    }
    internal static IDisposable Setup(RuntimeFixture f){RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(GameConditionDefOf));return new Definitions(f);}
    internal static void Verify(RuntimeFixture f,Pawn pawn,CompAbilityUserMight might,CompClassProgression comp,bool alternate)
    {
        var api=typeof(JobDriver_PsionicCrucible).Assembly.GetType("TorannMagic.ClassProgression.PsionicMartialPath")!;
        object? Call(string name,params object[] args)=>api.GetMethod(name,hidden)!.Invoke(null,args);
        void Service(float damage=0,int repair=0,int rescue=0,bool relief=false)=>Call("Service",pawn,damage,repair,rescue,relief);
        bool Claim(bool crucible=false)=>(bool)Call("Claim",pawn,crucible)!;
        void Reload()
        {
            var file=Path.Combine(f.WorkingDirectory,"psionic-state.xml");var state=comp.State;
            try{Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref state,"state");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
            state=null!;
            try{Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref state,"state");Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
            Set(comp,"state",state);
        }
        Check.Equal(alternate?1.08f:.90f,(float)Call("BlastFactor",pawn)!,"native blast discipline");
        Check.Equal(alternate?20f:18f,(float)Call("ProjectileCost",pawn,20f)!,"native barrier discipline");
        var hooks=typeof(JobDriver_PsionicCrucible).Assembly.GetType("TorannMagic.ClassProgression.PsionicService")!;
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;
        var id="rwom.test.psionichooks."+Guid.NewGuid();var patch=Activator.CreateInstance(ht,id)!;
        try {foreach(var type in hooks.GetNestedTypes(BindingFlags.NonPublic).Where(t=>t.GetCustomAttributes(false).Any(a=>a.GetType().Name=="HarmonyPatch")))
            {var processor=ht.GetMethod("CreateClassProcessor")!.Invoke(patch,[type])!;processor.GetType().GetMethod("Patch")!.Invoke(processor,null);}}
        finally{ht.GetMethod("UnpatchAll")!.Invoke(patch,[id]);}
        var native=XDocument.Load(Path.Combine(f.Repository,"v1.7","Defs","AbilityDefs","Abilities_TMagic_Psionic.xml")).Descendants("domain").Select(x=>x.Value).ToArray();
        Check.Equal(9,native.Length,"physical Core metadata");Check.Equal(true,native.All(s=>s=="Might"),"native power owner");
        var energy=(Hediff)Call("Energy",pawn)!;
        Check.NotNull(energy,"native psi bank");energy.Severity=71;
        Check.Equal(false,pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic),"traitless psionic path");
        Check.Equal(1,might.MightData.MightPowersP.Count(p=>p.learned&&p.abilityDef==TorannMagicDefOf.TM_PsionicAugmentation),"one native passive");
        Check.Equal(alternate,might.AbilityData.Powers.Any(a=>a.Def==TorannMagicDefOf.TM_PsionicBlast),"first choice");
        Check.Equal(!alternate,might.AbilityData.Powers.Any(a=>a.Def==TorannMagicDefOf.TM_PsionicBarrier),"projection choice");
        if(alternate){Service(199);Reload();Check.Equal(false,Claim(),"199 insufficent");Service(1);}
        else{for(int i=1;i<=20;i++){Check.Equal(false,Claim(),"incomplete repairs");Service(repair:i);Service(repair:i);if(i==5)Reload();}}
        Check.Equal(true,Claim(),"complementary psychic family unlocked");
        for(int i=1;i<=(alternate?30:10);i++)
        {
            Check.Equal(false,Claim(),"adept trial incomplete");Set(Find.TickManager,"ticksGameInt",2500*i);
            Service(rescue:alternate?0:i,relief:alternate);Service(rescue:alternate?0:i,relief:alternate);if(i==4)Reload();
        }
        Check.Equal(true,Claim(),"dash after trial");
        might.MightData.MightUserLevel=29;
        for(int i=1;i<=12;i++){Set(Find.TickManager,"ticksGameInt",100000+60000*i);Service(20);Service(20);if(i==3)Reload();}
        Check.Equal(false,Claim(alternate),"no level 29 graduation");might.MightData.MightUserLevel=30;
        Check.Equal(true,Claim(alternate),"single ascension");Check.Equal(false,Claim(!alternate),"no second ultimate");
        Reload();api.GetMethod("Restore",hidden)!.Invoke(Activator.CreateInstance(api,true),[might,comp.State.Find("rwom.psionic")]);
        Check.Equal(71f,energy.Severity,"restore preserves balance");
        Check.Equal(1,might.AbilityData.Powers.Count(a=>a.Def.defName==(alternate?"TM_PsionicKineticCrucible":"TM_PsionicStorm")),"one ultimate");
        if(alternate)PsionicCrucibleTests.Verify(f,pawn,might);
    }
}
