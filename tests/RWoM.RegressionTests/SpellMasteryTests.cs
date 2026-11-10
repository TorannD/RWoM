using System.Reflection;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpellMasteryTests
{
    static readonly Type Mastery = typeof(SpellMasteryRitual).Assembly.GetType("TorannMagic.ClassProgression.SpellMastery")!;
    const BindingFlags Internal = BindingFlags.Static | BindingFlags.NonPublic;

    public static IEnumerable<TestCase> Cases()
    {
        foreach (bool heal in new[] { false, true })
            yield return new("SpellMastery.NativeRitual." + (heal ? "Heal" : "Fireball"), r => SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                var pawn = c.Creator;
                var state = ClassIdentity.Component(pawn).State;
                var magic = pawn.GetCompAbilityUserMagic();
                var spell = heal ? TorannMagicDefOf.TM_Heal : TorannMagicDefOf.TM_Fireball;
                string id = heal ? "rwom.tm_heal" : "rwom.tm_fireball";
                if (heal) magic.spell_Heal = true;
                else
                {
                    state.TryAddFoundation("rwom.fire", DisciplineDomain.Magic);
                    Set(state.Find("rwom.fire"), "stage", InitiationStage.Master);
                    magic.MagicData.MagicPowersIF.Single(p => p.TMabilityDefs.Contains(spell)).learned = true;
                }
                var jobs = DefDatabase<JobDef>.AllDefsListForReading.ToArray();
                var defs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
                try
                {
                    DefDatabase<JobDef>.Add(new JobDef { defName = "TM_SpellMasteryRitual", driverClass = typeof(JobDriver_SpellMasteryRitual) });
                    var scrollDef = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                    scrollDef.defName = "SpellOf_Heal"; scrollDef.category = ThingCategory.Item; scrollDef.thingClass = typeof(Thing);
                    scrollDef.stackLimit = 10; scrollDef.destroyable = true; scrollDef.size = new IntVec2(1,1);
                    DefDatabase<ThingDef>.Add(scrollDef);
                    var scroll = new Thing { def = scrollDef, stackCount = 3, thingIDNumber = 75001 };
                    pawn.inventory.innerContainer.TryAdd(scroll, false);
                    foreach (int n in new[] { 85, 350 })
                        pawn.inventory.innerContainer.TryAdd(new Thing { def = c.Magicyte.def, stackCount = n, thingIDNumber = 75002+n }, false);
                    int Balance() => pawn.inventory.innerContainer.Where(t => t.def == c.Magicyte.def).Sum(t => t.stackCount);
                    var choices = (Array)Mastery.GetField("Choices", Internal)!.GetValue(null)!;
                    Job? Plan(int i) => (Job?)typeof(SpellMasteryRitual).GetMethod("Order",Internal)!.Invoke(null, [pawn,c.Circle,choices.GetValue(i)]);
                    bool Commit(Job job) => (bool)typeof(SpellMasteryRitual).GetMethod("Complete",Internal)!.Invoke(null,[pawn,job])!;
                    int first = heal ? 2 : 0;
                    int price = heal ? 100 : 200;
                    var order = Check.NotNull(Plan(first), "actual offered job");
                    Check.Equal(false, Commit(order), "unqueued preview cannot pay");
                    Check.Equal(435,Balance(),"not paid at preview");
                    pawn.Map.reservationManager.ReleaseAllForTarget(c.Circle);
                    Set(pawn.jobs,"curJob",order);
                    Check.Equal(false, Commit(order), "lost circle reservation cannot pay");
                    Check.Equal(true,pawn.Reserve(c.Circle,order,1,-1),"reserve ritual circle");
                    var driver = new JobDriver_SpellMasteryRitual { job = order, pawn = pawn };
                    Set(pawn.jobs,"curDriver",driver);
                    var toils = (IEnumerable<Toil>)typeof(JobDriver_SpellMasteryRitual).GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!;
                    var steps = toils.ToList();
                    Check.Equal(2500,steps[1].defaultDuration,"one-hour work phase precedes payment");
                    Check.Equal(435,Balance(),"approach and work leave stacks intact");
                    steps.Last().initAction();
                    Check.Equal(435-price,Balance(),"exact magicyte price; order=" + string.Join(",",order.countQueue) + " targets=" + string.Join(",",order.targetQueueB.Select(x=>x.Thing?.stackCount)) + " inventory=" + string.Join(",",pawn.inventory.innerContainer.Select(x=>x.def.defName+"/"+x.stackCount)));
                    Check.Equal(heal ? 2 : 3,scroll.stackCount,"only existing teaching scroll required");
                    Check.Equal(false,Commit(order),"successful callback cannot repeat");
                    Check.Equal(true,Plan(first)==null,"same choice not offered");
                    var changed = Check.NotNull(Plan(first+1), "second specialization offered");
                    pawn.Map.reservationManager.ReleaseAllForTarget(c.Circle);
                    Set(pawn.jobs,"curJob",changed);
                    Check.Equal(true,pawn.Reserve(c.Circle,changed,1,-1),"reserve circle for replacement");
                    Check.Equal(true,Commit(changed),"repeat ritual replaces specialization");
                    Check.Equal(435-2*price,Balance(),"repeat pays same price");
                    Check.Equal(heal ? "breadth" : "reach",state.Mastery(id),"one choice per permanent spell");
                    var owner = new MagicAbility(magic) { Def = spell };
                    magic.AbilityData.Powers.Add(owner);
                    string? Active(PawnAbility a) => (string?)Mastery.GetMethod("Active",Internal)!.Invoke(null,[pawn,a]);
                    Check.Equal(state.Mastery(id),Active(owner),"original permanent power gets selected specialization");
                    Check.Equal(true,Active(new MagicAbility(magic) { Def=spell })==null,"same-def copy cannot borrow mastery");
                    if (!heal)
                    {
                        using var hooks = new FleshStatTests.ProductionPatches();
                        hooks.Add(typeof(Projectile_Fireball).Assembly.GetType("TorannMagic.ClassProgression.FireballMasteryLaunchPatch")!);
                        var shot = new Projectile_Fireball { def = c.Magicyte.def, usedTarget = IntVec3.Zero, intendedTarget = IntVec3.Zero };
                        var verb = new Verb_UseAbility { Ability = owner, caster = pawn };
                        pawn.CurJob.verbToUse = verb;
                        var capture = shot.GetType().GetMethod("RecordMastery", BindingFlags.Instance | BindingFlags.NonPublic)!;
                        capture.Invoke(shot, [pawn,spell]);
                        var radius = shot.GetType().GetMethod("PrimaryRadius", BindingFlags.Instance | BindingFlags.NonPublic)!;
                        var damage = shot.GetType().GetMethod("PrimaryDamage", BindingFlags.Instance | BindingFlags.NonPublic)!;
                        Check.Equal(5f,(float)radius.Invoke(shot,[4f])!,"primary blast radius increases");
                        Check.Equal(20f,(float)damage.Invoke(shot,[20f])!,"breadth has no damage bonus");
                        var pathShot = Path.Combine(r.WorkingDirectory,"mastery-shot.xml");
                        var shotDef = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                        shotDef.defName="MasteryShotFixture"; shotDef.thingClass=typeof(Projectile_Fireball); shotDef.category=ThingCategory.Projectile; shotDef.projectile=new ProjectileProperties();
                        shotDef.size=new IntVec2(1,1); shotDef.stackLimit=1; shotDef.comps=[];
                        DefDatabase<ThingDef>.Add(shotDef); shot.def=shotDef;shot.thingIDNumber=75601;
                        try { Scribe.saver.InitSaving(pathShot,"regression"); Scribe_Deep.Look(ref shot,"shot"); Scribe.saver.FinalizeSaving(); }
                        finally { Scribe.ForceStop(); }
                        shot=null!;
                        try { Scribe.loader.InitLoading(pathShot); Scribe_Deep.Look(ref shot,"shot"); Scribe.loader.FinalizeLoading(); }
                        finally { Scribe.ForceStop(); }
                        RuntimeLog.AssertClean();
                        Check.Equal(5f,(float)radius.Invoke(shot,[4f])!,"in-flight choice survives reload");
                    }                    var path = Path.Combine(r.WorkingDirectory,"mastery-state.xml"); var saved = state;
                    try { Scribe.saver.InitSaving(path,"regression"); Scribe_Deep.Look(ref saved,"state"); Scribe.saver.FinalizeSaving(); }
                    finally { Scribe.ForceStop(); }
                    saved=null!;
                    try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref saved,"state"); Scribe.loader.FinalizeLoading(); }
                    finally { Scribe.ForceStop(); }
                    Check.Equal(state.Mastery(id),saved.Mastery(id),"new choice survives Scribe");
                    RuntimeLog.AssertClean();
                }
                finally
                {
                    DefDatabase<JobDef>.Clear();foreach(var j in jobs)DefDatabase<JobDef>.Add(j);
                    DefDatabase<ThingDef>.Clear();foreach(var t in defs)DefDatabase<ThingDef>.Add(t);
                }
            }));
    }
}
