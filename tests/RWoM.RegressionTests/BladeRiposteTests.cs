using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using TorannMagic.FleshDevelopment;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class BladeRiposteTests
{
    internal static void Verify(RuntimeFixture fixture, Pawn pawn, Pawn? source=null)
    {
        var state=ClassIdentity.Component(source??pawn).State;
        var path=state.Find("rwom.bladedancer")!;
        var originalPath=path;
        var disciplines=(List<DisciplineProgress>)state.GetType().GetField("disciplines",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(state)!;
        int index=disciplines.IndexOf(path);
        var field=BindingFlags.NonPublic|BindingFlags.Instance;
        int Get(string name)=>(int)path.GetType().GetField(name,field)!.GetValue(path)!;
        var savedFields=new[]{"BladeChallengerId","BladeReplyUntil","BladeReplyBody","BladeReplies"}.ToDictionary(k=>k,Get);
        var oldGear=pawn.equipment;
        var oldTick=Find.TickManager.TicksGame;
        var relations=typeof(Faction).GetField("relations",field)!;
        var oldRelations=relations.GetValue(pawn.Faction);
        try
        {
            var enemy=PawnWithComps(false,false).pawn;
            var faction=new Faction{def=new FactionDef()};
            Set(faction,"relations",new List<FactionRelation>{new(){other=pawn.Faction,kind=FactionRelationKind.Hostile}});
            Set(pawn.Faction,"relations",new List<FactionRelation>{new(){other=faction,kind=FactionRelationKind.Hostile}});
            enemy.kindDef=new PawnKindDef{race=enemy.def};enemy.mindState=new Pawn_MindState(enemy);enemy.guest=new Pawn_GuestTracker(enemy);
            enemy.thingIDNumber=711;Set(enemy,"factionInt",faction);Set(enemy,"mapIndexOrState",(sbyte)0);Set(enemy,"positionInt",pawn.Position+IntVec3.East);
            Verb_MeleeAttackDamage Equip(Pawn actor)
            {
                var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));def.category=ThingCategory.Item;def.equipmentType=EquipmentType.Primary;def.tools=[new Tool{power=12}];def.comps=[];
                var sword=new ThingWithComps{def=def};var eq=new CompEquippable{parent=sword};eq.Initialize(new CompProperties{compClass=typeof(CompEquippable)});
                Set(sword,"comps",new List<ThingComp>{eq});actor.equipment=new Pawn_EquipmentTracker(actor);actor.equipment.AddEquipment(sword);
                return new Verb_MeleeAttackDamage{caster=actor,verbTracker=eq.verbTracker,verbProps=new VerbProperties{verbClass=typeof(Verb_MeleeAttackDamage)}};
            }
            var blade=Equip(pawn);var opposing=Equip(enemy);var melee=new DamageDef();
            void Attack(Verb_MeleeAttackDamage verb,Pawn target,float damage,bool unrelated=false,bool fatal=false)
            {
                FleshCombatPatches.Prefix(verb,target,out var hit);
                try
                {
                    var result=new DamageWorker.DamageResult{totalDamageDealt=damage};
                    var info=new DamageInfo(melee,12f,instigator:verb.CasterPawn,weapon:unrelated?null:verb.EquipmentSource.def);
                    FleshCombatPatches.Record(target,info,result);
                    if(fatal) Set(target.health,"healthState",PawnHealthState.Dead);
                    FleshCombatPatches.Postfix(verb,hit,ref result);
                    FleshCombatPatches.Postfix(verb,hit,ref result);
                }
                finally {FleshCombatPatches.Finalizer(null!,hit);}
            }

            Set(Find.TickManager,"ticksGameInt",500);
            Attack(opposing,pawn,0);Attack(blade,enemy,12);
            Check.Equal(0,Get("BladeReplies"),"absorbed attack opens no window");
            Attack(opposing,pawn,12,unrelated:true);Attack(blade,enemy,12);
            Check.Equal(0,Get("BladeReplies"),"unrelated packet opens no window");
            Attack(opposing,pawn,12);
            Check.Equal(enemy.thingIDNumber,Get("BladeChallengerId"),"positive hostile weapon hit marked");
            Set(Find.TickManager,"ticksGameInt",740);Attack(blade,enemy,12);
            Check.Equal(0,Get("BladeReplies"),"window expires at 240 ticks");
            Set(Find.TickManager,"ticksGameInt",800);Attack(opposing,pawn,12);
            var data=path;var file=Path.Combine(fixture.WorkingDirectory,"blade-riposte.xml");
            try {Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref data,"discipline");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
            data=null!;
            try {Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref data,"discipline");Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
            Check.Equal(1040,(int)data.GetType().GetField("BladeReplyUntil",field)!.GetValue(data)!,"saved deadline");
            Check.Equal(enemy.thingIDNumber,(int)data.GetType().GetField("BladeChallengerId",field)!.GetValue(data)!,"saved attacker");
            Check.Equal(pawn.thingIDNumber,(int)data.GetType().GetField("BladeReplyBody",field)!.GetValue(data)!,"saved physical defender");
            disciplines[index]=path=data;
            if(source!=null)
            {
                Set(path,"BladeReplyBody",source.thingIDNumber);
                Attack(blade,enemy,12);
                Check.Equal(0,Get("BladeReplies"),"other body cannot finish the pending counter");
                Set(path,"BladeReplyBody",pawn.thingIDNumber);
            }
            Attack(blade,enemy,0);Attack(blade,enemy,12,fatal:true);
            Check.Equal(1,Get("BladeReplies"),"killing counter counts once; shielded counter does not consume");
            Check.Equal(0,Get("BladeChallengerId"),"window consumed");
        }
        finally
        {
            disciplines[index]=originalPath;
            foreach(var pair in savedFields) Set(originalPath,pair.Key,pair.Value);
            pawn.equipment=oldGear;Set(Find.TickManager,"ticksGameInt",oldTick);relations.SetValue(pawn.Faction,oldRelations);
        }
    }
}
