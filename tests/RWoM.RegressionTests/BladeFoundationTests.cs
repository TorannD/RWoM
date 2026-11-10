using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class BladeFoundationTests
{
    internal static void Verify(RuntimeFixture f, Pawn pawn, CompAbilityUserMight might, bool memory)
    {
        var focus = might.MightData.MightPowerSkill_BladeFocus.Single();
        Check.Equal(memory ? 4 : 3, focus.levelMax, "paid training ranks");
        var oldDegrees = TorannMagicDefOf.Bladedancer.degreeDatas;
        var stat = new StatDef {defName="MeleeHitChance"};
        var tree=XDocument.Load(Path.Combine(f.Repository,"v1.7/Defs/TraitDefs/Traits.xml")).Root!.Elements()
            .Single(x => (string?)x.Element("defName")=="Bladedancer");
        TorannMagicDefOf.Bladedancer.degreeDatas=tree.Element("degreeDatas")!.Elements().Select(x=>new TraitDegreeData {
            degree=(int)x.Element("degree")!, statOffsets=[new StatModifier{stat=stat,value=(float)x.Element("statOffsets")!.Element(stat.defName)!}]}).ToList();
        try
        {
            Check.Equal(memory?0f:3f,ClassPassiveEffects.StatOffset(pawn,stat),"initial classic focus or conditional memory");
            focus.level=focus.levelMax;
            Check.Equal(memory?0f:15f,ClassPassiveEffects.StatOffset(pawn,stat),"maximum classic focus");
            if (memory) Melee(pawn);
        }
        finally {focus.level=0;TorannMagicDefOf.Bladedancer.degreeDatas=oldDegrees;}
    }

    internal static void Melee(Pawn pawn)
    {
        var impl=BindingFlags.Static|BindingFlags.NonPublic;
        var patch=typeof(SniperSurvey).Assembly.GetType("TorannMagic.ClassProgression.BladeMemoryDamagePatch",true)!;
        var harmonyType=Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        string id="rwom.blade.memory."+Guid.NewGuid();
        var harmony=Activator.CreateInstance(harmonyType,id)!;
        var oldRelations=(List<FactionRelation>)typeof(Faction).GetField("relations",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(pawn.Faction)!;
        var oldEquipment=pawn.equipment;
        var oldTick=Find.TickManager.TicksGame;
        try
        {
            var processor=harmonyType.GetMethod("CreateClassProcessor")!.Invoke(harmony,[patch])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor,null);
            var enemyFaction=new Faction{def=new FactionDef()};
            Set(enemyFaction,"relations",new List<FactionRelation>{new(){other=pawn.Faction,kind=FactionRelationKind.Hostile}});
            Set(pawn.Faction,"relations",new List<FactionRelation>{new(){other=enemyFaction,kind=FactionRelationKind.Hostile}});
            Pawn Enemy(int id)
            {
                var enemy=PawnWithComps(false,false).pawn;
                enemy.kindDef=new PawnKindDef{race=enemy.def};enemy.mindState=new Pawn_MindState(enemy);
                enemy.guest=new Pawn_GuestTracker(enemy);
                enemy.thingIDNumber=id;Set(enemy,"mapIndexOrState",(sbyte)0);Set(enemy,"positionInt",pawn.Position+IntVec3.East);
                Set(enemy,"factionInt",enemyFaction);
                return enemy;
            }
            var a=Enemy(77);var b=Enemy(78);
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.category=ThingCategory.Item;def.equipmentType=EquipmentType.Primary;def.tools=[new Tool{power=20f}];def.comps=[];
            var sword=new ThingWithComps{def=def};var eq=new CompEquippable{parent=sword};
            eq.Initialize(new CompProperties{compClass=typeof(CompEquippable)});
            Set(sword,"comps",new List<ThingComp>{eq});
            pawn.equipment=new Pawn_EquipmentTracker(pawn);pawn.equipment.AddEquipment(sword);
            var verb=new Verb_MeleeAttackDamage{caster=pawn,verbTracker=eq.verbTracker,verbProps=new VerbProperties{verbClass=typeof(Verb_MeleeAttackDamage)}};
            var damage=new DamageDef();
            float Strike(Pawn target)
            {
                IEnumerable<DamageInfo> infos=[new DamageInfo(damage,100f),new DamageInfo(damage,5f)];
                object[] args=[verb,new LocalTargetInfo(target),infos];
                patch.GetMethod("Postfix",impl)!.Invoke(null,args);
                var applied=((IEnumerable<DamageInfo>)args[2]).ToArray();
                Check.Equal(5f,applied[1].Amount,"extra damage unchanged");
                return applied[0].Amount;
            }
            Set(Find.TickManager,"ticksGameInt",1000);
            Check.Equal(100f,Strike(a),"first target establishes stance");
            Check.Equal(100f,Strike(a),"same target no bonus");
            Check.Equal(110f,Strike(b),"switch at max rank");
            Check.Equal(100f,Strike(a),"cooldown blocks immediate switch");
            Set(Find.TickManager,"ticksGameInt",1360);
            Check.Equal(110f,Strike(b),"six seconds releases cooldown");
        }
        finally
        {
            harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);pawn.equipment=oldEquipment;
            Set(pawn.Faction,"relations",oldRelations);Set(Find.TickManager,"ticksGameInt",oldTick);
        }
    }
}
