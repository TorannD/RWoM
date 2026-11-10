using System.Reflection;
using System.Reflection.Emit;
using AbilityUser;
using TorannMagic;
using Verse;
namespace RWoM.RegressionTests;
internal static partial class AbilityProjectileTests
{
    static int roofImpacts, roofDestroys;
    static bool ObserveRoofImpact() { roofImpacts++; return false; }
    static bool ObserveRoofDestroy() { roofDestroys++; return false; }
    static void SetLaunchField<T>(Projectile projectile,string name,T value)
    {
        var setter=new DynamicMethod("RoofSet"+name,typeof(void),new[]{typeof(Projectile),typeof(T)},true);
        var il=setter.GetILGenerator(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld,typeof(Projectile).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!); il.Emit(OpCodes.Ret);
        ((Action<Projectile,T>)setter.CreateDelegate(typeof(Action<Projectile,T>)))(projectile,value);
    }
    private static readonly string[] IndoorGenericDefs = {
        "Buff_AMP", "Buff_RayofHope", "Buff_Shadow", "Buff_Soothe",
        "Projectile_ConsumeCorpse", "Projectile_ConsumeCorpse_Mass", "Projectile_CureDisease", "Projectile_Regenerate",
        "Projectile_RegrowLimb", "Projectile_SootheAnimal", "Projectile_DeathMark", "TM_Projectile_Priest", "TM_Paladin_Heal",
        "TM_Paladin_Shield", "TM_Projectile_PsionicBarrier", "Projectile_FoldReality", "Projectile_Blink", "Projectile_BriarPatch",
        "Projectile_DirtDevil", "Projectile_EyeOfTheStorm", "Projectile_SpiritStorm", "Projectile_PsionicStorm", "Projectile_Summon",
        "Projectile_TM_SpawnThing", "Projectile_Warlock"
    };
    static IEnumerable<TestCase> GenericRoofCases()
    {
        foreach(var name in IndoorGenericDefs)
        {
            string definition=name;
            yield return new("NecromancyRoof.GenericIndoor."+name,f=>World(f,(caster,victim,projectile)=>
            {
                projectile.def.defName=definition;
                var roofs=new RoofDef[400]; roofs[126]=new RoofDef {isThickRoof=true}; Set(projectile.Map.roofGrid,"roofGrid",roofs);
                projectile.Resolve();
                Check.Equal(false,projectile.WasDestroyed,"audited generic local carrier reaches impact");
                Check.Same(victim,projectile.Hit,"generic carrier preserves selected target");
                Check.Same(victim,EffectRecipients.Single(),"generic carrier does not redirect to caster");
            }));
        }
        foreach(var name in new[] {"Projectile_Default","Projectile_Geomancer","UnknownModCarrier","Buff_AMP"})
        {
            string definition=name;
            yield return new("NecromancyRoof.GenericBlocked."+name,f=>World(f,(caster,victim,projectile)=>
            {
                projectile.def.defName=definition;
                if(definition=="Buff_AMP") projectile.localAbilityDef=Blank<AbilityUser.AbilityDef>();
                var roofs=new RoofDef[400]; roofs[126]=new RoofDef {isThickRoof=true}; Set(projectile.Map.roofGrid,"roofGrid",roofs);
                projectile.Resolve();
                Check.Equal(true,projectile.WasDestroyed,"shared sky-capable/unrelated generic keeps native roof block");
                Check.Equal(0,EffectRecipients.Count,"blocked generic has no effects");
            }));
        }
    }
    static readonly HashSet<string> BlockedRoofClasses = new() { "Projectile_Sunfire", "Projectile_BloodMoon", "Projectile_HolyWrath",
        "Projectile_Firestorm", "Projectile_Blizzard", "Projectile_LightningStorm", "Projectile_OrbitalStrike", "Projectile_Scorn",
        "Projectile_LightSkipMass", "Projectile_Mk203GL", "Projectile_BreachingCharge" };
    public static IEnumerable<TestCase> RoofCases()
    {
        foreach(var test in GenericRoofCases()) yield return test;
        foreach(string name in new[] {"Projectile_RaiseUndead","Projectile_FogOfTorment","Projectile_HealingCircle",
            "Projectile_Teleport","Projectile_IgniteBlood","Projectile_AccelerateTime","Projectile_GraveBlade",
            "Projectile_Attraction","Projectile_Repulsion","Projectile_PsychicShock","Projectile_Encase","Projectile_LightningCloud",
            "Projectile_DistortSpirit","Projectile_SummonSpiritAnimals","Projectile_PsionicBlast","Projectile_Sunlight","Projectile_DryGround","Projectile_WetGround",
            "Projectile_SmokeCloud","Projectile_Extinguish","Projectile_EMP","Projectile_FertileLands","Projectile_Explosion","Projectile_SummonPoppi","Projectile_Sabotage",
            "Projectile_SummonMinion","Projectile_SummonExplosive","Projectile_SummonPylon","Projectile_SummonDemon","Projectile_SummonElemental","Projectile_SpiritPossession"}.Concat(BlockedRoofClasses))
        foreach(string roof in name is "Projectile_RaiseUndead" or "Projectile_FogOfTorment" or "Projectile_HealingCircle"
            ? new[] {"None","Constructed","ThinRock","ThickRock"} : new[] {"ThickRock"})
        {
            string spell=name, covering=roof;
            yield return new("NecromancyRoof."+spell+"."+covering, f=>World(f,(caster,victim,template)=>
            {
                var type=typeof(MagicAbility).Assembly.GetType("TorannMagic."+spell)!;
                var projectile=(Projectile_AbilityBase)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
                projectile.def=template.def; projectile.localAbilityDef=template.localAbilityDef;
                projectile.usedTarget=projectile.intendedTarget=new IntVec3(6,0,6);
                projectile.HitFlags=ProjectileHitFlags.IntendedTarget;
                SetLaunchField<Thing>(projectile,"launcher",caster); SetLaunchField(projectile,"destination",new UnityEngine.Vector3(6.5f,0,6.5f));
                Set(projectile,"mapIndexOrState",(sbyte)0); Set(projectile,"positionInt",new IntVec3(6,0,6));
                var roofGrid=new RoofDef[400];
                if(covering!="None") roofGrid[126]=new RoofDef {defName=covering,isThickRoof=covering=="ThickRock",isNatural=covering!="Constructed"};
                Set(projectile.Map.roofGrid,"roofGrid",roofGrid);
                var assembly=Assembly.Load("0Harmony"); var id="rwom.roof.dispatch."+Guid.NewGuid();
                var harmony=Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!,id)!;
                void Patch(MethodInfo target,string method)
                {
                    var hook=Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!,typeof(AbilityProjectileTests).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Static)!)!;
                    harmony.GetType().GetMethod("Patch")!.Invoke(harmony,[target,hook,null,null,null]);
                }
                try
                {
                    var impact=type.GetMethod("Impact",BindingFlags.NonPublic|BindingFlags.Instance)!;
                    Patch(impact.DeclaringType!.GetMethod("Impact",BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly)!,nameof(ObserveRoofImpact));
                    var destroy=type.GetMethod("Destroy",BindingFlags.Public|BindingFlags.Instance)!;
                    Patch(destroy.DeclaringType!.GetMethod("Destroy",BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly)!,nameof(ObserveRoofDestroy));
                    roofImpacts=roofDestroys=0;
                    typeof(Projectile_AbilityBase).GetMethod("ImpactSomething",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(projectile,null);
                    Check.Equal(BlockedRoofClasses.Contains(spell)?0:1,roofImpacts,"real JecsLite dispatch follows audited indoor/sky policy");
                    Check.Equal(BlockedRoofClasses.Contains(spell)?1:0,roofDestroys,"only audited indoor effects bypass roof cancellation");
                    Check.Equal(true,projectile.def.projectile.flyOverhead,"shared flight/interception policy unchanged");
                    Check.Same(roofGrid[126],projectile.Map.roofGrid.RoofAt(new IntVec3(6,0,6)),"map roof never mutated");
                }
                finally {harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,[id]);}
            }));
        }
    }
}







