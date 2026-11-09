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

internal static class RangerFoundationTests
{
    internal static void Verify(RuntimeFixture f, Pawn pawn, CompAbilityUserMight might, CompClassProgression comp, bool trails)
    {
        var data=might.MightData;
        var skill=data.MightPowerSkill_RangerTraining.Single();
        Check.Equal(trails?4:3,skill.levelMax,"number of useful training upgrades");
        Check.Equal(false,pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger),"class is independent of legacy trait");
        Check.Equal(true,data.MightPowersR.Single(p=>p.TMabilityDefs.Contains(TorannMagicDefOf.TM_RangerTraining)).learned,"original training family");
        Check.Equal(false,data.MightPowersR.Single(p=>p.TMabilityDefs.Contains(TorannMagicDefOf.TM_BowTraining)).learned,"bow training not issued by new path");
        Check.Equal(1,might.AbilityData.Powers.Count(p=>p.Def==TorannMagicDefOf.TM_AnimalFriend),"shared animal ability ready on enrollment");
        VerifyPassive(f,pawn,might,trails);
    }

    internal static void VerifyPassive(RuntimeFixture f,Pawn pawn,CompAbilityUserMight might,bool trails)
    {
        var skill=might.MightData.MightPowerSkill_RangerTraining.Single();
        var oldMove=StatDefOf.MoveSpeed;
        var oldDegrees=TorannMagicDefOf.Ranger.degreeDatas;
        var pos=pawn.Position;
        var oldCarry=pawn.carryTracker;var oldJobs=pawn.jobs;
        var map=pawn.Map;var oldInfo=map.info;var oldIndices=map.cellIndices;var oldRoof=map.roofGrid;
        var dataFile=XDocument.Load(Path.Combine(f.Repository,"v1.7/Defs/TraitDefs/Traits.xml")).Root!.Elements().Single(e=>(string?)e.Element("defName")=="Ranger");
        try
        {
            StatDefOf.MoveSpeed=new StatDef{defName="MoveSpeed"};
            TorannMagicDefOf.Ranger.degreeDatas=dataFile.Element("degreeDatas")!.Elements().Select(e=>new TraitDegreeData{degree=(int)e.Element("degree")!,
                statOffsets=[new StatModifier{stat=StatDefOf.MoveSpeed,value=(float)e.Element("statOffsets")!.Element("MoveSpeed")!}]}).ToList();
            float Factor()=>ClassPassiveEffects.StatFactor(pawn,StatDefOf.MoveSpeed);
            Check.Equal(trails?0f:.15f,ClassPassiveEffects.StatOffset(pawn,StatDefOf.MoveSpeed),"classic native offset vs specialized path");
            map.info=new MapInfo{Size=new IntVec3(32,1,32)};map.cellIndices=new CellIndices(map);map.roofGrid=new RoofGrid(map);
            Set(pawn,"positionInt",new IntVec3(9,0,9));
            pawn.carryTracker=new Pawn_CarryTracker(pawn);pawn.jobs=new Pawn_JobTracker(pawn);
            var job=new Job(new JobDef{defName="TestRangerHaul",driverClass=typeof(JobDriver_HaulToCell)});
            pawn.jobs.curDriver=new JobDriver_HaulToCell{pawn=pawn,job=job};Set(pawn.jobs,"curJob",job);
            Check.Equal(1f,Factor(),"outbound empty haul has no speed bonus");
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));def.category=ThingCategory.Item;def.stackLimit=75;
            var goods=new Thing{def=def,stackCount=5};
            Check.Equal(true,pawn.carryTracker.innerContainer.TryAdd(goods),"genuine cargo holder");
            Check.Equal(trails?1.02f:1f,Factor(),"unroofed loaded haul");
            skill.level=skill.levelMax;
            Check.Equal(trails?1.10f:1f,Factor(),"trained haul");
            var roofs=(RoofDef[])typeof(RoofGrid).GetField("roofGrid",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(map.roofGrid)!;
            roofs[map.cellIndices.CellToIndex(pawn.Position)]=new RoofDef();
            Check.Equal(1f,Factor(),"roofed haul has no conditional factor");
            roofs[map.cellIndices.CellToIndex(pawn.Position)]=null!;
            pawn.jobs.curDriver=null;
            Check.Equal(1f,Factor(),"cargo outside hauling job has no conditional factor");
            pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Ranger));
            Check.Equal(0f,ClassPassiveEffects.StatOffset(pawn,StatDefOf.MoveSpeed),"legacy trait offset not duplicated");
            pawn.story.traits.allTraits.RemoveAll(t=>t.def==TorannMagicDefOf.Ranger);
        }
        finally
        {
            skill.level=0;TorannMagicDefOf.Ranger.degreeDatas=oldDegrees;StatDefOf.MoveSpeed=oldMove;
            pawn.carryTracker=oldCarry;pawn.jobs=oldJobs;map.info=oldInfo;map.cellIndices=oldIndices;map.roofGrid=oldRoof;
            Set(pawn,"positionInt",pos);
        }
    }
}
