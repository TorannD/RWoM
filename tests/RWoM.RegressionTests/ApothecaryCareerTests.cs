using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using TorannMagic.TMDefs;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;
internal static class ApothecaryCareerTests
{
    private sealed class Scope : IDisposable
    {
        private readonly Dictionary<TMAbilityDef,Type> originals=new();
        private readonly HediffDef old=TorannMagicDefOf.TM_ApothecaryHerbsHD;
        private readonly ThingDef[] things=DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        private readonly TM_CustomPowerDef[] customs=DefDatabase<TM_CustomPowerDef>.AllDefsListForReading.ToArray();
        internal Scope(RuntimeFixture f)
        {
            if(DefDatabase<ThingDef>.GetNamedSilentFail("BookOfApothecary")==null)
            {
                var book=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                book.defName="BookOfApothecary";DefDatabase<ThingDef>.Add(book);
            }
            TorannMagicDefOf.TM_ApothecaryHerbsHD=new HediffDef{defName="TM_ApothecaryHerbsHD",maxSeverity=100,initialSeverity=100,hediffClass=typeof(Hediff_ApothecaryHerbs)};
            TMAbilityDef Register(string name,Type type)
            {
                var def=DefDatabase<TMAbilityDef>.GetNamedSilentFail(name)??f.AddAbility(name);
                originals.TryAdd(def,def.abilityClass);def.abilityClass=type;return def;
            }
            foreach(var native in new[]{TorannMagicDefOf.TM_SoothingBalm,TorannMagicDefOf.TM_Elixir,TorannMagicDefOf.TM_PoisonFlask})
            {originals[native]=native.abilityClass;native.abilityClass=typeof(PawnAbility);}
            var flask=Register("TM_FireSuppressionFlask",typeof(PawnAbility));
            Register("TM_ApothecaryAntitoxin",typeof(ApothecaryAntitoxinAbility));
            Register("TM_ApothecaryGoldenHour",typeof(ApothecaryKitAbility));
            if(!DefDatabase<TM_CustomPowerDef>.AllDefsListForReading.Any(x=>x.customPower?.abilityDefs?.FirstOrDefault()==flask))
                DefDatabase<TM_CustomPowerDef>.Add(new TM_CustomPowerDef {defName="RegressionApothecaryFlask",customPower=new TM_CustomPower{
                    forFighter=true,abilityDefs=[flask],skills=[new TM_CustomSkill{label="TM_FireSuppressionFlask_eff",description="test",levelMax=3,costToLevel=1}]}});
        }
        public void Dispose()
        {
            foreach(var kv in originals)kv.Key.abilityClass=kv.Value;
            TorannMagicDefOf.TM_ApothecaryHerbsHD=old;
            DefDatabase<ThingDef>.Clear();foreach(var def in things)DefDatabase<ThingDef>.Add(def);
            DefDatabase<TM_CustomPowerDef>.Clear();foreach(var def in customs)DefDatabase<TM_CustomPowerDef>.Add(def);
        }
    }
    internal static IDisposable Setup(RuntimeFixture f)=>new Scope(f);
    internal static void Verify(RuntimeFixture f,Pawn pawn,CompAbilityUserMight might,CompClassProgression comp,bool alternate)
    {
        var api=typeof(ApothecaryKitAbility).Assembly.GetType("TorannMagic.ClassProgression.ApothecaryMartialPath",true)!;
        const BindingFlags methods=BindingFlags.Static|BindingFlags.NonPublic;
        object? Call(string name,params object[] args)=>api.GetMethod(name,methods)!.Invoke(null,args);
        void Reload()
        {
            var state=comp.State;var file=Path.Combine(f.WorkingDirectory,"apothecary.xml");
            try {Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref state,"state");Scribe.saver.FinalizeSaving();}
            finally {Scribe.ForceStop();}
            state=null!;
            try {Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref state,"state");Scribe.loader.FinalizeLoading();}
            finally {Scribe.ForceStop();} Set(comp,"state",state);
        }
        var nativeDefs=System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository,"v1.7","Defs","AbilityDefs","Abilities_TMagic_Apothecary.xml")).Root!.Elements().Where(d=>d.Element("modExtensions")?.Descendants("domain").Any()==true).ToList();
        Check.Equal(10,nativeDefs.Count,"native clinical families and scrolls");
        Check.Equal(true,nativeDefs.All(d=>d.Element("modExtensions")!.Descendants("domain").All(x=>x.Value=="Might")),"native RWoM Core identity");
        var canonicalBook=DefDatabase<ThingDef>.GetNamed("BookOfApothecary");
        var supports=api.GetMethod("SupportsBook",BindingFlags.NonPublic|BindingFlags.Instance)!;
        bool OwnBook()=>(bool)supports.Invoke(Activator.CreateInstance(api,true),[canonicalBook])!;
        Check.Equal(true,OwnBook(),"canonical book eligible");
        var external=new TM_CustomClass{fullScript=canonicalBook,isMage=true};
        TM_ClassUtility.CustomClasses.Add(external);
        try {Check.Equal(false,OwnBook(),"external book replacement retains classic ownership");}
        finally {TM_ClassUtility.CustomClasses.Remove(external);}
        var stock=(Hediff_ApothecaryHerbs)Call("Stock",pawn)!;
        Check.NotNull(stock,"real herbs health owner");
        Check.Equal(0f,stock.Severity,"enrollment cannot refill stock");
        ApothecaryClinicalTests.Harvest(pawn,stock);
        stock.Severity=38.5f;
        Check.Equal(false,pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Apothecary),"traitless enrollment");
        Check.Equal(1,might.AbilityData.Powers.Count(p=>p.Def==TorannMagicDefOf.TM_SoothingBalm),"initial balm");
        for(int i=0;i<30;i++)
        {
            Check.Equal(false,(bool)Call("Claim",pawn,false)!,"no early claim");
            Call("Harvest",pawn,14000+i);if(i==4) Reload();
            Call("Harvest",pawn,14000+i);
        }
        Check.Equal(true,(bool)Call("Claim",pawn,false)!,"unlock custom flask + antidote");
        var flask=might.MightData.MightPowersCustomAll.Single(p=>p.abilityDef.defName=="TM_FireSuppressionFlask");
        Check.Equal(true,flask.learned,"existing custom power owner granted");
        if (alternate) ApothecaryClinicalTests.Detox(pawn,stock);
        for(int i=0;i<(alternate?12:20);i++)
        {
            Check.Equal(false,(bool)Call("Claim",pawn,false)!,"not enough real cases");
            Call("Treatment",pawn,18000+i,alternate);if(i==4) Reload();
            Call("Treatment",pawn,18000+i,alternate);
        }
        Check.Equal(true,(bool)Call("Claim",pawn,false)!,"master poison flask");
        might.MightData.MightUserLevel=29;
        for(int i=0;i<12;i++)
        {
            Set(Find.TickManager,"ticksGameInt",60000*i+100);
            Call("Treatment",pawn,2040+i,false);if(i==5) Reload();Call("Treatment",pawn,2040+i,false);
        }
        Check.Equal(false,(bool)Call("Claim",pawn,alternate)!,"level 29 keeps completed trial");
        might.MightData.MightUserLevel=30;
        Check.Equal(true,(bool)Call("Claim",pawn,alternate)!,"level 30 ascension");
        Check.Equal(false,(bool)Call("Claim",pawn,!alternate)!,"exclusive ultimate");
        Reload();var path=comp.State.Find("rwom.apothecary")!;
        var chosen=alternate?DefDatabase<TMAbilityDef>.GetNamed("TM_ApothecaryGoldenHour"):TorannMagicDefOf.TM_Elixir;
        might.AbilityData.Powers.RemoveAll(p=>p.Def==chosen);
        api.GetMethod("Restore",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(Activator.CreateInstance(api,true),[might,path]);
        Check.Equal(1,might.AbilityData.Powers.Count(p=>p.Def==chosen),"one restored ultimate");
        Check.Equal(38.5f,stock.Severity,"stage restore preserves herbs");
        if (alternate) ApothecaryClinicalTests.Kit(f,pawn,might,stock);
    }
}
