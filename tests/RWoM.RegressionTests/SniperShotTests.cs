using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SniperShotTests
{
    internal static void SpawnPatch(RuntimeFixture fixture)
    {
        const string id = "rwom.regression.sniper.spawn";
        var harmonyType = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        var harmony = Activator.CreateInstance(harmonyType, id)!;
        try
        {
            var patch = typeof(Verb_SniperShot).Assembly.GetType("TorannMagic.SniperRoundSpawn", true)!;
            var processor = harmonyType.GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            var ammo = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            ammo.thingClass = typeof(Bullet);
            var special = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            special.thingClass = typeof(FailingRound);
            var verb = new Verb_SniperShot { verbProps = new VerbProperties_Ability { defaultProjectile = special } };
            try
            {
                typeof(Verb_SniperShot).GetMethod("MakeRound", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(verb, [ammo]);
                throw new InvalidOperationException("Expected the test projectile to abort initialization.");
            }
            catch (TargetInvocationException error) when (error.InnerException is OperationCanceledException) {}
            Check.Equal(typeof(Bullet), ammo.thingClass, "shared ammunition class survived aborted shot");
        }
        finally { harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); }
    }

    private static void VerifyBallistics(Pawn pawn, int damage)
    {
        var ammo = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        ammo.thingClass = typeof(Bullet);
        ammo.projectile = new ProjectileProperties { damageDef = new DamageDef(), speed = 55f };
        Set(ammo.projectile, "damageAmountBase", 20);
        var bullet = new SniperAimedBullet { def = ammo };
        var cell = new IntVec3(20, 0, 15);
        bullet.Launch(pawn, pawn.Position.ToVector3Shifted(), cell, cell, ProjectileHitFlags.IntendedTarget);
        Check.Equal(damage, bullet.DamageAmount, "aimed round carries ranked damage into flight");
        Check.Equal(20, ammo.projectile.GetDamageAmount(null), "ordinary round still deals base damage");
    }
    public sealed class FailingRound : Projectile_AbilityBase
    {
        public override void PostMake()
        {
            Check.Equal(typeof(Bullet), def.thingClass, "ordinary round class intact during projectile construction");
            throw new OperationCanceledException();
        }
    }

    internal static void Progress(RuntimeFixture f,Pawn pawn,CompAbilityUserMight might,CompClassProgression comp,bool aimed)
    {
        var map=pawn.Map;
        map.info=new MapInfo {Size=new IntVec3(200,1,200)};
        map.cellIndices=new CellIndices(map); map.thingGrid=new ThingGrid(map); map.edificeGrid=new EdificeGrid(map); map.fogGrid=new FogGrid(map);
        var flags=BindingFlags.Static|BindingFlags.NonPublic;
        bool Observe(LocalTargetInfo target) => (bool)typeof(SniperSurvey).GetMethod("Observe",flags)!.Invoke(null,[pawn,target])!;
        void Reload()
        {
            var state=comp.State;var file=Path.Combine(f.WorkingDirectory,"sniper-recon.xml");
            try {Scribe.saver.InitSaving(file,"regression");Scribe_Deep.Look(ref state,"state");Scribe.saver.FinalizeSaving();} finally {Scribe.ForceStop();}
            state=null!;
            try {Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref state,"state");Scribe.loader.FinalizeLoading();} finally {Scribe.ForceStop();}
            Set(comp,"state",state);
        }
        var choice=aimed ? SniperShotChoice.Aimed : SniperShotChoice.Headshot;
        bool Choose(SniperShotChoice value) => (bool)typeof(SniperSurvey).GetMethod("Choose",flags)!.Invoke(null,[pawn,value])!;
        var shot=DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_SniperAimedShot") ?? f.AddAbility("TM_SniperAimedShot");
        var fracture = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_SniperArmorFracture") ?? f.AddAbility("TM_SniperArmorFracture");
        var oldFractureType = fracture.abilityClass;
        var head=TorannMagicDefOf.TM_Headshot;
        var oldType=shot.abilityClass;var oldHead=head.abilityClass;
        var enemy=new Faction {def=new FactionDef()};
        Set(enemy,"relations",new List<FactionRelation>{new(){other=pawn.Faction,kind=FactionRelationKind.Hostile}});
        Set(pawn.Faction,"relations",new List<FactionRelation>{new(){other=enemy,kind=FactionRelationKind.Hostile}});
        var target=PawnWithComps(false,false).pawn;
        target.kindDef=new PawnKindDef {race=target.def};target.mindState=new Verse.AI.Pawn_MindState(target);
        target.guest=new Pawn_GuestTracker(target);Set(target,"mapIndexOrState",(sbyte)0);
        try
        {
            fracture.abilityClass=shot.abilityClass=head.abilityClass=typeof(PawnAbility);
            for(int i=0;i<10;i++)
            {
                var cell=new IntVec3(16+i*12,0,14);
                Set(pawn,"positionInt",cell-new IntVec3(12,0,0));
                Set(target,"positionInt",cell);target.thingIDNumber=2300+i;
                Set(target,"factionInt",pawn.Faction);
                Check.Equal(false,Observe(target),"ally cannot count as threat");
                Set(target,"factionInt",enemy);
                LocalTargetInfo observation=aimed?new LocalTargetInfo(cell):new LocalTargetInfo(target);
                Check.Equal(false,Choose(choice),"cannot choose early");
                Check.Equal(true,Observe(observation),"new hostile or sector counts");
                Check.Equal(false,Observe(observation),"duplicate rejected");
                if(i==4 || i==9)
                {
                    Reload();
                    Check.Equal(false,Observe(observation),"same observation rejected after loading");
                }
            }
            Check.Equal(true,Choose(choice),"permanent shot choice");
            Check.Equal(false,Choose(aimed?SniperShotChoice.Headshot:SniperShotChoice.Aimed),"second choice refused");
            Reload();
            var training = typeof(SniperSurvey).Assembly.GetType("TorannMagic.ClassProgression.SniperTraining")!;
            bool Upgrade(bool breach, int rank) => (bool)training.GetMethod("Upgrade",flags)!.Invoke(null,[pawn,breach,rank])!;
            var data = might.MightData;
            if (aimed) VerifyBallistics(pawn, 22);
            data.MightAbilityPoints = 0;
            Check.Equal(false, Upgrade(true, 1), "no free upgrades");
            data.MightAbilityPoints = 4;
            Check.Equal(aimed, Upgrade(false, 1), "only aimed branch can train aimed shot");
            if (aimed)
            {
                VerifyBallistics(pawn, 25);
                Check.Equal(false, Upgrade(false, 1), "stale command cannot pay twice");
                Reload();
                Check.Equal(true, Upgrade(false, 2), "second paid rank survives load");
                VerifyBallistics(pawn, 28);
                Check.Equal(false, Upgrade(false, 3), "aimed cap");
            }
            Check.Equal(true, Upgrade(true, 1), "both paths can train breach");
            Reload();
            Check.Equal(true, Upgrade(true, 2), "breach has independent paid ranks");
            Check.Equal(false, Upgrade(true, 3), "breach cap");
            Check.Equal(aimed ? 0 : 2, data.MightAbilityPoints, "exact payment");
            var owned=aimed?shot:head;
            Check.Equal(1,might.AbilityData.Powers.Count(a=>a.Def==owned),"one chosen runtime ability");
            Check.Equal(1,might.AbilityData.Powers.Count(a=>a.Def==fracture),"both branches also receive fracture");
            might.AbilityData.Powers.RemoveAll(a=>a.Def==owned || a.Def==fracture);
            typeof(SniperSurvey).GetMethod("Restore",flags)!.Invoke(null,[might]);
            typeof(SniperSurvey).GetMethod("Restore",flags)!.Invoke(null,[might]);
            Check.Equal(1,might.AbilityData.Powers.Count(a=>a.Def==fracture),"fracture restored exactly once");
            Check.Equal(1,might.AbilityData.Powers.Count(a=>a.Def==owned),"runtime restored");
            if (!aimed)
            {
                var anti=TorannMagicDefOf.TM_AntiArmor;
                var formerType=anti.abilityClass;
                try
                {
                    anti.abilityClass=typeof(PawnAbility);
                    var record=data.MightPowersS.Single(p=>p.TMabilityDefs.Contains(anti));
                    record.learned=false;
                    might.RemovePawnAbility(anti);
                    typeof(SniperSurvey).GetMethod("Restore",flags)!.Invoke(null,[might]);
                    Check.Equal(false,might.AbilityData.Powers.Any(a=>a.Def==anti),"unlearned anti-armor remains locked");
                    record.learned=true;
                    typeof(SniperSurvey).GetMethod("Restore",flags)!.Invoke(null,[might]);
                    var retained=might.AbilityData.Powers.Single(a=>a.Def==anti);
                    retained.CooldownTicksLeft=91;
                    typeof(SniperSurvey).GetMethod("Restore",flags)!.Invoke(null,[might]);
                    Check.Same(retained,might.AbilityData.Powers.Single(a=>a.Def==anti),"keep existing anti-armor command");
                    Check.Equal(91,retained.CooldownTicksLeft,"retained shot preserves cooldown");
                }
                finally {anti.abilityClass=formerType;}
            }
        }
        finally {shot.abilityClass=oldType;head.abilityClass=oldHead;fracture.abilityClass=oldFractureType;}
    }
}
