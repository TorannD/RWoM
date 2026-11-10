using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal sealed class ShadowWalkerData : AdditionalMartialData
    {
        internal List<int> Opponents, Patients;
        internal bool LongVeil;
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref Opponents,"opponents",LookMode.Value);
            Scribe_Collections.Look(ref Patients,"patients",LookMode.Value);
            Scribe_Values.Look(ref LongVeil,"longVeil");
        }
    }
    internal sealed class ShadowWalkerMartialPath : AdditionalMartialPath
    {
        internal const string ClassId = "rwom.shadowwalker";
        private static readonly string[] Forms = { "TM_ReadShadowWalkerStrike","TM_ReadShadowWalkerPoison" };
        private static readonly HashSet<string> Skills = new() { "TM_ShadowStrike","TM_Nightshade","TM_VeilOfShadows" };
        private static readonly HashSet<string> Spells = new() { "TM_ShadowWalk","TM_SoulBond","TM_Blink","TM_Blink_I","TM_Blink_II","TM_Blink_III",
            "TM_Summon","TM_Summon_I","TM_Summon_II","TM_Summon_III" };
        internal override string Id => ClassId;
        internal override string Book => "BookOfShadow";
        internal override string[] Choices => Forms;
        internal static DisciplineProgress Path(Pawn pawn) => ClassIdentity.TryGetState(pawn,out var state) ? state.Find(ClassId) : null;
        internal static ShadowWalkerData Data(DisciplineProgress path) => (ShadowWalkerData)(path.MartialData ??= new ShadowWalkerData());
        internal override bool SupportsBook(ThingDef book) => book?.defName == Book && !TM_ClassUtility.CustomClasses.Any(c =>
            c != null && (c.fullScript == book || c.tornScript == book) && (c.classTrait?.defName!="TM_TheShadow" || !c.isFighter || !c.isMage ||
            c.isAdvancedClass || c.classHediff!=null || !Skills.SetEquals(c.classFighterAbilities?.Select(a=>a?.defName) ?? Enumerable.Empty<string>()) ||
            !Spells.SetEquals(c.classMageAbilities?.Select(a=>a?.defName) ?? Enumerable.Empty<string>())));
        internal override bool CanEnroll(CompAbilityUserMight might,string choice) => Forms.Contains(choice) && TorannMagicDefOf.TM_ShadowStrike!=null &&
            TorannMagicDefOf.TM_Nightshade!=null && TorannMagicDefOf.TM_VeilOfShadows!=null;
        internal override IEnumerable<MightPower> Powers(CompAbilityUserMight might) => might.MightData.MightPowersShadow.Where(p=>p.learned);
        internal override void Restore(CompAbilityUserMight might,DisciplineProgress path)
        {
            foreach(var power in might.MightData.MightPowersShadow)
            {
                var def=power.abilityDef;
                if(def==null) continue;
                bool starting=def==(path.MartialAlternate ? TorannMagicDefOf.TM_Nightshade : TorannMagicDefOf.TM_ShadowStrike);
                if(!starting && path.Stage<(def==TorannMagicDefOf.TM_VeilOfShadows ? InitiationStage.Master : InitiationStage.Adept)) continue;
                power.learned=true;
                TM_ClassState.AddRuntimeAbility(might,def);
            }
        }
        private static int CombatGoal(InitiationStage stage) => stage==InitiationStage.Foundation ? 3 : stage==InitiationStage.Adept ? 5 : 6;
        private static int RescueGoal(InitiationStage stage) => stage==InitiationStage.Foundation ? 3 : stage==InitiationStage.Adept ? 4 : 6;
        private static string Trial(DisciplineProgress path) => ClassId+"."+path.Stage;
        private static bool Ready(DisciplineProgress path) => path.CompletedTrials.Contains(Trial(path));
        internal static bool Enemy(Pawn pawn,Pawn target) => pawn?.Spawned==true && !pawn.Dead && !pawn.Downed &&
            target?.Spawned==true && !target.Dead && !target.Downed && !target.IsPrisoner && target.Map==pawn.Map && target.HostileTo(pawn);
        internal static void Hit(Pawn pawn,int id,bool poison,bool combined)
        {
            var path=Path(pawn);
            if(path==null || path.Stage>=InitiationStage.Ascended || Ready(path) || id<=0 ||
                path.Stage==InitiationStage.Foundation && poison!=path.MartialAlternate || path.Stage==InitiationStage.Master && !combined) return;
            var d=Data(path);d.Opponents??=new List<int>();
            if(!d.Opponents.Contains(id)) d.Opponents.Add(id);
            if(d.Opponents.Count>=CombatGoal(path.Stage)) path.RecordCompletedTrial(Trial(path));
        }
        internal static void Rescue(Pawn pawn,Pawn patient)
        {
            var path=Path(pawn);
            if(path==null || path.Stage>=InitiationStage.Ascended || Ready(path) || patient==null || patient.thingIDNumber<=0) return;
            var d=Data(path);d.Patients??=new List<int>();
            if(!d.Patients.Contains(patient.thingIDNumber)) d.Patients.Add(patient.thingIDNumber);
            if(d.Patients.Count>=RescueGoal(path.Stage)) path.RecordCompletedTrial(Trial(path));
        }
        internal static int StrikeDamage(Pawn pawn,int baseDamage) => Path(pawn) is {Stage:InitiationStage.Ascended, MartialData:ShadowWalkerData{LongVeil:false}}
            ? Mathf.RoundToInt(baseDamage*1.2f) : baseDamage;
        internal static int VeilTicks(Pawn pawn,int baseTicks) => Path(pawn) is {Stage:InitiationStage.Ascended, MartialData:ShadowWalkerData{LongVeil:true}}
            ? Mathf.RoundToInt(baseTicks*1.5f) : baseTicks;
        internal static bool Claim(Pawn pawn,bool longVeil=false)
        {
            var path=Path(pawn);var might=pawn.GetCompAbilityUserMight();
            if(path==null || !Ready(path) || might?.ExistingMightData==null || path.Stage==InitiationStage.Master && might.MightData.MightUserLevel<30) return false;
            if(!ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(ClassId,path.Stage,Trial(path))) return false;
            var d=Data(path);d.Opponents=null;d.Patients=null;
            if(path.Stage==InitiationStage.Ascended)d.LongVeil=longVeil;
            new ShadowWalkerMartialPath().Restore(might,path);return true;
        }
        internal override string Requirements(Pawn pawn,DisciplineProgress path)
        {
            var d=path.MartialData as ShadowWalkerData;
            if(path.Stage==InitiationStage.Ascended) return (d?.LongVeil==true?"TM_ShadowWalkerLongVeil":"TM_ShadowWalkerStrongStrike").Translate();
            string task=path.Stage==InitiationStage.Master?"TM_ShadowWalkerCombo":"TM_ShadowWalkerHits";
            var text=task.Translate(d?.Opponents?.Count??0,CombatGoal(path.Stage)).ToString()+" "+
                "TM_FacelessRescue".Translate(d?.Patients?.Count??0,RescueGoal(path.Stage)).ToString();
            return path.Stage==InitiationStage.Master ? text+" "+"TM_DKLevel".Translate(pawn.GetCompAbilityUserMight().MightData.MightUserLevel).ToString() : text;
        }
        internal override float ControlsHeight(Pawn pawn,DisciplineProgress path)=>Ready(path)?40f:0f;
        internal override void DrawControls(Pawn pawn,DisciplineProgress path,float width,ref float y)
        {
            if(!Ready(path))return;
            if(Widgets.ButtonText(new Rect(0,y,width,32),"TM_DKClaim".Translate()))
            {
                if(path.Stage!=InitiationStage.Master)Claim(pawn);
                else if(pawn.GetCompAbilityUserMight().MightData.MightUserLevel>=30)Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>{
                    new FloatMenuOption("TM_ShadowWalkerStrongStrike".Translate(),()=>Claim(pawn)),
                    new FloatMenuOption("TM_ShadowWalkerLongVeil".Translate(),()=>Claim(pawn,true))}));
            }
            y+=40;
        }
    }
}
