using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static partial class SpiritMapCollapseTests
{
    private static void SoldierWeaponProfile(Pawn body,Pawn soul,Action? leave=null)
    {
        var api=Production("SuperSoldierWeaponProfile");
        var refresh=api.GetMethod("Refresh",BindingFlags.NonPublic|BindingFlags.Static)!;
        var factor=api.GetMethod("Factor",BindingFlags.NonPublic|BindingFlags.Static)!;
        var statFields=new[]{"RangedWeapon_DamageMultiplier","RangedWeapon_Cooldown","AccuracyTouch","AccuracyShort","AccuracyMedium","AccuracyLong"}.Select(n=>typeof(StatDefOf).GetField(n)!).ToArray();
        var originalStats=statFields.Select(f=>f.GetValue(null)).ToArray();
        foreach(var field in statFields) field.SetValue(null,new StatDef{defName=field.Name,defaultBaseValue=1,workerClass=typeof(StatWorker)});
        var harmonyType=Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        string patchId="rwom.test.soldierprofile."+Guid.NewGuid();var harmony=Activator.CreateInstance(harmonyType,patchId)!;
        try {
        foreach(string patch in new[]{"SuperSoldierWeaponVerbPatch","SuperSoldierWeaponAllVerbsPatch","SuperSoldierWeaponCastPatch","SuperSoldierWeaponStatPatch"}) {
            var processor=harmonyType.GetMethod("CreateClassProcessor")!.Invoke(harmony,[Production(patch)])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor,null);
        }
        SoldierWeaponTrial(body,soul,false,(_,__)=>{
            var weapon=body.equipment.Primary;var def=weapon.def;string name=def.defName;
            var oldStats=def.statBases;
            def.statBases=statFields.Select(f=>new StatModifier{stat=(StatDef)f.GetValue(null)!,value=2}).ToList();
            var source=soul.GetCompAbilityUserMight();var comps=weapon.AllComps.ToArray();
            var training=Production("SuperSoldierMartialPath").GetMethod("Training",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[ClassIdentity.Component(soul).State.Find("rwom.supersoldier")])!;
            try {
                foreach(string school in new[]{"Pistol","Rifle","Shotgun"}) {
                    def.defName="SoldierTest"+school;Set(training,"School","TM_"+school+"Spec");
                    var power=source.MightData.MightPowersSS.First(p=>p.abilityDef.defName=="TM_"+school+"Spec");power.learned=true;
                    source.MightData.GetSkill_Power((TMAbilityDef)power.abilityDef).level=5;
                    var baseline=new VerbProperties{verbClass=typeof(Verb_Shoot),range=30,warmupTime=2,burstShotCount=10,ticksBetweenBurstShots=20};
                    var verb=weapon.TryGetComp<CompEquippable>().PrimaryVerb;verb.verbProps=baseline;var other=new Verb_Shoot{verbProps=baseline};
                    Check.Same(verb,weapon.TryGetComp<CompEquippable>().PrimaryVerb,"real native getter refreshes instance");
                    Check.Equal(false,ReferenceEquals(baseline,verb.verbProps),"instance properties copied "+school);
                    Check.Same(baseline,other.verbProps,"other owner's shared properties unaffected "+school);
                    Check.Equal(30f,baseline.range,"shared baseline untouched");
                    float expectedRange=school=="Rifle"?33:31.5f;
                    Check.Equal(true,Math.Abs(verb.verbProps.range-expectedRange)<.0001f,"native range formula "+school);
                    Check.Equal(school=="Pistol"?17:18,verb.verbProps.ticksBetweenBurstShots,"native interval formula "+school);
                    Check.Equal(school=="Pistol"?11:school=="Rifle"?12:10,verb.verbProps.burstShotCount,"native burst formula "+school);
                    float damage=(float)factor.Invoke(null,[weapon,StatDefOf.RangedWeapon_DamageMultiplier])!;
                    Check.Equal(true,Math.Abs(damage-(school=="Pistol"?1.15f:1.1f))<.0001f,"native damage formula "+school);
                    float actual=StatDefOf.RangedWeapon_DamageMultiplier.Worker.GetValueUnfinalized(StatRequest.For(weapon),false);
                    Check.Equal(true,Math.Abs(actual-2*damage)<.0001f,"real stat worker applies profile to original weapon "+school);
                    float[] expected=school=="Pistol"?[1.15f,.875f,1,1.075f,1.025f,1]:school=="Rifle"?[1.1f,.95f,1,1.05f,1.05f,1.05f]:[1.1f,.85f,1.05f,1.075f,1.025f,1];
                    for(int i=0;i<statFields.Length;i++) {
                        var stat=(StatDef)statFields[i].GetValue(null)!;
                        Check.Equal(true,Math.Abs(stat.Worker.GetValueUnfinalized(StatRequest.For(weapon),false)-2*expected[i])<.0001f,"native worker coefficient "+school+"/"+stat.defName);
                    }
                    Check.Equal(true,(bool)api.GetMethod("ReadyWeapon",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[body])!,"original weapon qualifies without synthetic registry");
                    var modified=verb.verbProps;refresh.Invoke(null,[weapon,verb]);
                    Check.Same(modified,verb.verbProps,"repeated reads do not compound or allocate");
                    source.MightData.GetSkill_Power((TMAbilityDef)power.abilityDef).level=1;refresh.Invoke(null,[weapon,verb]);
                    Check.Equal(true,Math.Abs(verb.verbProps.range-(school=="Rifle"?30.6f:30.3f))<.0001f,"rank change recalculated from baseline");
                    Set(ClassIdentity.Component(soul).SpiritRebirth,"depletionPending",true);refresh.Invoke(null,[weapon,verb]);
                    Check.Same(baseline,verb.verbProps,"depletion restores native instance properties");
                    Check.Equal(1f,(float)factor.Invoke(null,[weapon,StatDefOf.RangedWeapon_DamageMultiplier])!,"depleted source grants no stats");
                    Check.Equal(2f,StatDefOf.RangedWeapon_DamageMultiplier.Worker.GetValueUnfinalized(StatRequest.For(weapon),false),"real stat worker loses borrowed bonus on depletion");
                    Set(ClassIdentity.Component(soul).SpiritRebirth,"depletionPending",false);
                    refresh.Invoke(null,[weapon,verb]);body.equipment.Remove(weapon);
                    try {
                        refresh.Invoke(null,[weapon,verb]);
                        Check.Same(baseline,verb.verbProps,"unequipped original loses temporary properties");
                        Check.Equal(2f,StatDefOf.RangedWeapon_DamageMultiplier.Worker.GetValueUnfinalized(StatRequest.For(weapon),false),"unequipped original loses temporary stats");
                    } finally {body.equipment.AddEquipment(weapon);}
                    Check.Same(weapon,body.equipment.Primary,"original weapon identity retained");
                    Check.Equal(comps.Length,weapon.AllComps.Count,"components neither copied nor removed");
                    for(int i=0;i<comps.Length;i++){Check.Same(comps[i],weapon.AllComps[i],"component identity");Check.Same(weapon,comps[i].parent,"component parent");}
                }
                if(leave!=null) {
                    var verb=weapon.GetComp<CompEquippable>().PrimaryVerb;
                    Check.Equal(true,verb.verbProps.range>30,"borrowed weapon enhanced before actual release");
                    leave();
                    Check.Same(verb,weapon.GetComp<CompEquippable>().PrimaryVerb,"existing native verb refreshes after release");
                    Check.Equal(30f,verb.verbProps.range,"completed host release removes borrowed range");
                    Check.Equal(2f,StatDefOf.RangedWeapon_DamageMultiplier.Worker.GetValueUnfinalized(StatRequest.For(weapon),false),"completed release removes borrowed damage");
                    Check.Same(weapon,body.equipment.Primary,"carrier retains its original weapon after release");
                    Check.Same(body.equipment.GetDirectlyHeldThings(),weapon.holdingOwner,"release preserves equipment custody");
                }
            } finally {def.defName=name;def.statBases=oldStats;}
        });
        SoldierConcurrentProfiles();
        } finally {harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony,[patchId]);for(int i=0;i<statFields.Length;i++)statFields[i].SetValue(null,originalStats[i]);}
    }

    private static void SoldierConcurrentProfiles()
    {
        // More than the historical twenty-definition pool, simultaneously retained.
        // Every school shares one weapon Def and baseline properties across its owners.
        var entries=new List<(Pawn Pawn,ThingWithComps Weapon,Verb Verb,VerbProperties Baseline,int Rank,string School)>();
        foreach(string school in new[]{"Pistol","Rifle","Shotgun"}) {
            var shared=SoldierReloadItem("Concurrent"+school,school).def;
            var baseline=new VerbProperties{verbClass=typeof(Verb_Shoot),range=30,warmupTime=2,burstShotCount=10,ticksBetweenBurstShots=20};
            Set(shared,"verbs",new List<VerbProperties>{baseline});
            for(int i=0;i<24;i++) {
                var (pawn,_,might)=PawnWithComps(false,true);pawn.thingIDNumber=84000+entries.Count;
                pawn.mindState=new Verse.AI.Pawn_MindState(pawn);pawn.equipment=new Pawn_EquipmentTracker(pawn);
                var progression=new CompClassProgression{parent=pawn};pawn.AllComps.Add(progression);
                var state=new ClassProgressionState(Vocation.Warrior);state.TryAddFoundation("rwom.supersoldier",DisciplineDomain.Might);Set(progression,"state",state);
                var training=Production("SuperSoldierMartialPath").GetMethod("Training",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[state.Find("rwom.supersoldier")])!;
                Set(training,"School","TM_"+school+"Spec");
                var power=might.MightData.MightPowersSS.First(p=>p.abilityDef.defName=="TM_"+school+"Spec");power.learned=true;
                int rank=i%6;might.MightData.GetSkill_Power((TMAbilityDef)power.abilityDef).level=rank;
                var weapon=SoldierReloadItem("UnusedInstance","weapon");weapon.def=shared;weapon.thingIDNumber=85000+entries.Count;
                pawn.equipment.AddEquipment(weapon);
                var verb=weapon.GetComp<CompEquippable>().PrimaryVerb;
                entries.Add((pawn,weapon,verb,baseline,rank,school));
            }
        }
        foreach(var entry in entries.AsEnumerable().Reverse()) {
            var verb=entry.Weapon.GetComp<CompEquippable>().PrimaryVerb;
            Check.Same(entry.Verb,verb,"concurrent owner's native verb retained");
            float expected=30*(1+(entry.School=="Rifle"?.02f:.01f)*entry.Rank);
            Check.Equal(true,Math.Abs(expected-verb.verbProps.range)<.0001f,"rank belongs to exact concurrent owner");
            Check.Equal(30f,entry.Baseline.range,"shared weapon definition never receives a rank");
            Check.Equal(-1,entry.Pawn.GetCompAbilityUserMight().specWpnRegNum,"no finite registry slot consumed");
            Check.Same(entry.Weapon,entry.Pawn.equipment.Primary,"more than twenty owners keep originals");
        }
        var first=entries[0];first.Pawn.equipment.Remove(first.Weapon);
        var refresh=Production("SuperSoldierWeaponProfile").GetMethod("Refresh",BindingFlags.NonPublic|BindingFlags.Static)!;
        refresh.Invoke(null,[first.Weapon,first.Verb]);
        Check.Same(first.Baseline,first.Verb.verbProps,"one owner's unequip restores only its baseline");
        var other=entries[5];
        Check.Equal(true,Math.Abs(other.Verb.verbProps.range-31.5f)<.0001f,"unrelated rank five owner remains enhanced");
    }
}
