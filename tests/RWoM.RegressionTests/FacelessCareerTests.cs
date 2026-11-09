using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;

internal static class FacelessCareerTests
{
    private sealed class DefScope : IDisposable
    {
        private readonly Dictionary<TMAbilityDef,Type> types=new();
        internal DefScope(RuntimeFixture f)
        {
            var echo=DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_FacelessEcho")??f.AddAbility("TM_FacelessEcho");
            foreach(var def in new[]{echo,TorannMagicDefOf.TM_Disguise,TorannMagicDefOf.TM_Mimic,TorannMagicDefOf.TM_Transpose,
                TorannMagicDefOf.TM_Reversal,TorannMagicDefOf.TM_Possess,TorannMagicDefOf.TM_Firebolt,TorannMagicDefOf.TM_Icebolt,
                TorannMagicDefOf.TM_Heal,TorannMagicDefOf.TM_Cleave})
            { types[def]=def.abilityClass;def.abilityClass=typeof(PawnAbility); }
        }
        public void Dispose(){ foreach(var (def,type) in types) def.abilityClass=type; }
    }
    internal static IDisposable Setup(RuntimeFixture f)=>new DefScope(f);
    public static bool SkipNotifications() => false;
    public static bool ClearSight(ref bool __result){__result=true;return false;}
    private static float totalDamage;
    public static bool Injury(DamageInfo dinfo,ref DamageWorker.DamageResult __result)
    {
        totalDamage=dinfo.Amount;__result=new DamageWorker.DamageResult{totalDamageDealt=totalDamage};return false;
    }
    internal static void Verify(RuntimeFixture f,Pawn pawn,CompAbilityUserMight might,CompClassProgression comp,bool alternate)
    {
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;
        string id="rwom.regression.faceless."+Guid.NewGuid();var harmony=Activator.CreateInstance(ht,id)!;
        var asm=typeof(ClassIdentity).Assembly;
        var api=asm.GetType("TorannMagic.ClassProgression.FacelessMartialPath",true)!;
        var mimic=asm.GetType("TorannMagic.Verb_Mimic",true)!;
        
        const BindingFlags hidden=BindingFlags.Static|BindingFlags.NonPublic, instance=BindingFlags.Instance|BindingFlags.NonPublic;
        object? Call(string method,params object[] args)=>api.GetMethod(method,hidden)!.Invoke(null,args);
        void Patch(MethodBase method,string hook)
        {
            var hm=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(FacelessCareerTests).GetMethod(hook)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[method,hm,null,null,null]);
        }
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(DamageDefOf));
        var oldCut=DamageDefOf.Cut;DamageDefOf.Cut??=new DamageDef{defName="RegressionFacelessCut",harmsHealth=true};
        var oldWorld=Current.Game.World;
        var oldRelations=typeof(Faction).GetField("relations",instance)!.GetValue(pawn.Faction);
        try
        {
            Set(Current.Game,"questManager",new QuestManager());
            var map=pawn.Map;map.info=new MapInfo{Size=new IntVec3(32,1,32)};map.cellIndices=new CellIndices(map);map.edificeGrid=new EdificeGrid(map);
            map.thingGrid=new ThingGrid(map);
            var world=(RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));world.factionManager=new FactionManager();
            Set(world.factionManager,"ofPlayer",pawn.Faction);world.factionManager.AllFactionsListForReading.Add(pawn.Faction);Set(Current.Game,"worldInt",world);
            var foe=new Faction{def=new FactionDef()};
            Set(foe,"relations",new List<FactionRelation>{new(){other=pawn.Faction,kind=FactionRelationKind.Hostile}});
            Set(pawn.Faction,"relations",new List<FactionRelation>{new(){other=foe,kind=FactionRelationKind.Hostile}});
            Set(pawn,"positionInt",new IntVec3(14,0,14));
            var model=PawnWithComps(true,true);
            pawn.kindDef=new PawnKindDef{race=pawn.def};pawn.guest=new Pawn_GuestTracker(pawn);pawn.mindState=new Pawn_MindState(pawn);
            model.pawn.def.race.body=new BodyDef{corePart=new BodyPartRecord{def=new BodyPartDef{defName="FacelessTrialTorso"}}};
            model.pawn.kindDef=new PawnKindDef{race=model.pawn.def};model.pawn.guest=new Pawn_GuestTracker(model.pawn);model.pawn.mindState=new Pawn_MindState(model.pawn);
            Set(model.pawn,"mapIndexOrState",(sbyte)0);Set(model.pawn,"positionInt",pawn.Position+IntVec3.East);Set(model.pawn,"factionInt",foe);
            var verb=(Verb_UseAbility)Activator.CreateInstance(mimic,true)!;verb.caster=pawn;Set(verb,"currentTarget",new LocalTargetInfo(model.pawn));
            Patch(mimic.GetMethod("CanHitTargetFrom")!,nameof(ClearSight));Patch(typeof(Thing).GetMethod("TakeDamage")!,nameof(Injury));
            bool Copy()=>(bool)mimic.GetMethod("TryCastShot",instance)!.Invoke(verb,null)!;
            bool Claim()=>(bool)Call("Claim",pawn,alternate)!;
            var file=Path.Combine(f.WorkingDirectory,"faceless-career.xml");
            void Reload()
            {
                var state=comp.State;
                try{Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref state,"state");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
                state=null!;
                try{Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref state,"state");Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
                Set(comp,"state",state);
            }
            var independent=TorannMagicDefOf.TM_Cleave;
            might.AddPawnAbility(independent);var owned=might.AbilityData.Powers.Single(a=>a.Def==independent);
            var cleave=might.MightData.MightPowersG.Single(g=>g.TMabilityDefs.Contains(independent));cleave.learned=true;cleave.level=2;owned.CooldownTicksLeft=222;
            bool Has(TMAbilityDef d)=>might.AbilityData.Powers.Any(a=>a.Def==d);
            Check.Equal(false,Has(TorannMagicDefOf.TM_Transpose),"early exchange locked");
            model.might.AddPawnAbility(TorannMagicDefOf.TM_Mimic);
            Check.Equal(false,Copy(),"recursive mimic refused");
            model.magic.AddPawnAbility(TorannMagicDefOf.TM_Firebolt);
            Check.Equal(true,Copy(),"copy actual mage technique");
            var echo=might.AbilityData.Powers.Single(a=>a.Def.defName=="TM_FacelessEcho");
            echo.CooldownTicksLeft=123;
            Check.Equal(false,Copy(),"same technique cannot clear cooldown");
            model.magic.RemovePawnAbility(TorannMagicDefOf.TM_Firebolt);
            model.magic.AddPawnAbility(TorannMagicDefOf.TM_Icebolt);
            Check.Equal(true,Copy(),"changed magic family");Reload();
            model.magic.RemovePawnAbility(TorannMagicDefOf.TM_Icebolt);
            model.might.AddPawnAbility(independent);
            Check.Equal(true,Copy(),"martial technique may coexist with permanent owner");
            Check.Same(echo,might.AbilityData.Powers.Single(a=>a.Def.defName=="TM_FacelessEcho"),"changing source keeps echo cooldown owner");
            Check.Equal(123,echo.CooldownTicksLeft,"echo cooldown preserved");
            Check.Same(owned,might.AbilityData.Powers.Single(a=>a.Def==independent),"permanent runtime owner preserved");
            Check.Equal(2,cleave.level,"permanent rank preserved");Check.Equal(222,owned.CooldownTicksLeft,"independent native cooldown preserved");
            Check.Equal(true,Claim(),"three distinct learned techniques grant exchange");
            Check.Equal(true,Has(TorannMagicDefOf.TM_Transpose),"native exchange owner");
            var cast=new Verb_FacelessEcho{caster=pawn};Set(cast,"currentTarget",new LocalTargetInfo(model.pawn));
            bool Use()=>(bool)typeof(Verb_FacelessEcho).GetMethod("TryCastShot",instance)!.Invoke(cast,null)!;
            Check.Equal(true,model.pawn.HostileTo(pawn),"fixture hostile");
            Check.Equal(false,pawn.WorkTagIsDisabled(WorkTags.Violent),"fixture violence");
            Check.Equal(true,DamageDefOf.Cut != null,"fixture cut damage");
            Check.Equal(true,cast.CanHitTargetFrom(pawn.Position,model.pawn),"cast validation");
            int now=100;
            for(int stage=1;stage<=2;stage++)
            {
                for(int i=0;i<(alternate?(stage==1?5:8):(stage==1?4:6));i++)
                {
                    Check.Equal(false,Claim(),"pending trial");Set(Find.TickManager,"ticksGameInt",now);now+=60000;
                    if(alternate)
                    {
                        totalDamage=0;Check.Equal(true,Use(),"echo lands");Check.Equal(true,totalDamage>=14,"understudy damage");
                        Check.Equal(true,Use(),"repeat is legitimate damage but no second daily credit");
                    }
                    else
                    {
                        model.pawn.thingIDNumber=85400+i;Call("Rescue",pawn,model.pawn);Call("Rescue",pawn,model.pawn);
                    }
                    if(i==2) Reload();
                }
                if(stage==2){might.MightData.MightUserLevel=29;Check.Equal(false,Claim(),"level 30 required");might.MightData.MightUserLevel=30;}
                Check.Equal(true,Claim(),"next stage");
            }
            Check.Equal(true,Has(TorannMagicDefOf.TM_Reversal),"master reversal");
            Check.Equal(!alternate,Has(TorannMagicDefOf.TM_Possess),"exclusive ultimate");
            Reload();totalDamage=0;Check.Equal(true,Use(),"capstone survives reload");
            Check.Equal(alternate?20f:12f,totalDamage,"virtuoso or normal potency");
            model.might.RemovePawnAbility(independent);model.magic.AddPawnAbility(TorannMagicDefOf.TM_Heal);
            Check.Equal(true,Copy(),"medical echo from healer");
            Check.Equal(false,Use(),"medical echo cannot heal hostile target");
            Set(model.pawn,"factionInt",pawn.Faction);
            Check.Equal(false,Use(),"no heal trial or cost for healthy ally");
            var wound = new Hediff_Injury{pawn=model.pawn,def=new HediffDef{defName="RegressionEchoWound",injuryProps=new InjuryProps()}};
            Patch(typeof(Pawn_HealthTracker).GetMethod("Notify_HediffChanged")!,nameof(SkipNotifications));
            Set(wound,"severityInt",30f);model.pawn.health.hediffSet.hediffs.Add(wound);
            Check.Equal(true,Use(),"native injury healed");
            Check.Equal(true,wound.Severity<=26f && wound.Severity>22f,"bounded native healing");
        }
        finally {DamageDefOf.Cut=oldCut;ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);Set(pawn.Faction,"relations",oldRelations);Set(Current.Game,"worldInt",oldWorld);}
    }
}
