using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class LegacyMartialConversion
    {
        // Read only the lists actually present in the save. The MightData property
        // getters would otherwise manufacture default owners while diagnosing a pawn.
        private sealed class Profile
        {
            internal readonly string Id, Adept, Master, Ultimate;
            internal readonly Func<MightData,List<MightPower>> Owners;
            internal Profile(string id,Func<MightData,List<MightPower>> owners,string adept=null,string master=null,string ultimate=null)
            { Id=id; Owners=owners; Adept=adept; Master=master; Ultimate=ultimate; }
        }
        private static readonly Dictionary<string,Profile> profiles=new()
        {
            ["Gladiator"]=new("rwom.gladiator",d=>d.mightPowerG,"TM_Fortitude","TM_Cleave","TM_Whirlwind"),
            ["TM_Sniper"]=new("rwom.sniper",d=>d.mightPowerS,"TM_Headshot","TM_DisablingShot","TM_ShadowSlayer"),
            ["Bladedancer"]=new("rwom.bladedancer",d=>d.mightPowerB,"TM_BladeSpin","TM_SeismicSlash","TM_PhaseStrike"),
            ["Ranger"]=new("rwom.ranger",d=>d.mightPowerR,"TM_PoisonTrap",null,"TM_ArrowStorm"),
            ["Faceless"]=new("rwom.faceless",d=>d.mightPowerF,"TM_Transpose","TM_Reversal","TM_Possess"),
            ["TM_Psionic"]=new("rwom.psionic",d=>d.mightPowerP,"TM_PsionicBlast","TM_PsionicDash","TM_PsionicStorm"),
            ["DeathKnight"]=new("rwom.deathknight",d=>d.mightPowerDK,"TM_Spite","TM_LifeSteal","TM_GraveBlade"),
            ["TM_Monk"]=new("rwom.monk",d=>d.mightPowerM,"TM_TigerStrike","TM_DragonStrike","TM_ChiBurst"),
            ["TM_Commander"]=new("rwom.commander",d=>d.mightPowerC,"TM_MoveOut","TM_HoldTheLine","TM_CommanderAura"),
            ["TM_SuperSoldier"]=new("rwom.supersoldier",d=>d.mightPowerSS),
            ["TM_Apothecary"]=new("rwom.apothecary",d=>d.mightPowerApothecary,null,"TM_PoisonFlask","TM_Elixir")
        };

        internal static bool TryImport(Pawn pawn,CompClassProgression comp)
        {
            var traits=pawn.story.traits.allTraits;
            var sources=traits.Where(t=>t?.def!=null && (profiles.ContainsKey(t.def.defName) ||
                t.def.defName=="TM_TheShadow" || TM_ClassGroups.MagicEntriesField.Any(e=>e.trait==t.def.defName) ||
                TM_ClassUtility.CustomClasses?.Any(c=>c?.classTrait==t.def)==true)).Take(2).ToList();
            if(sources.Count!=1 || sources[0].Suppressed || !profiles.TryGetValue(sources[0].def.defName,out var profile))return false;
            var source=sources[0];
            var customs=TM_ClassUtility.CustomClasses?.Where(c=>c?.classTrait==source.def).ToList();
            if(customs?.Count>0 && (profile.Id!="rwom.apothecary" || customs.Count!=1 ||
                customs[0].fullScript?.defName!="BookOfApothecary" || customs[0].traitDegree!=source.Degree ||
                !customs[0].isFighter || customs[0].isMage || customs[0].isAdvancedClass)) return false;
            var might=pawn.GetCompAbilityUserMight();
            var data=might?.ExistingMightData;
            var owners=data==null?null:profile.Owners(data);
            if(owners==null || owners.Count==0 || owners.Any(o=>o?.TMabilityDefs==null || o.TMabilityDefs.Count==0 || o.TMabilityDefs.Any(a=>a==null))) return false;
            bool Learned(string name)=>name!=null && owners.Any(o=>o.learned && o.TMabilityDefs.Any(a=>a.defName==name));
            string school=null;
            if(profile.Id=="rwom.supersoldier")
            {
                var selected=SuperSoldierMartialPath.Schools.Where(Learned).ToArray();
                if(selected.Length>1) return false;
                school=selected.FirstOrDefault();
            }
            if(!IsAvailable(profile.Id)) return false;
            var stage=Learned(profile.Adept)?InitiationStage.Adept:InitiationStage.Foundation;
            if(profile.Id=="rwom.apothecary" && data.mightPowerCustom?.Any(o=>o?.learned==true &&
                o.TMabilityDefs?.Any(a=>a?.defName=="TM_FireSuppressionFlask")==true)==true) stage=InitiationStage.Adept;
            if(Learned(profile.Master)) stage=InitiationStage.Master;
            var ultimate=profile.Ultimate==null?null:owners.FirstOrDefault(o=>o.learned && o.TMabilityDefs.Any(a=>a.defName==profile.Ultimate));
            if(ultimate!=null && (data.MightUserLevel>=30 || ultimate.level>0 || might.AbilityData?.Powers?.Any(a=>a?.Def!=null && ultimate.TMabilityDefs.Contains(a.Def))==true))
                stage=InitiationStage.Ascended;
            if(!comp.BeginNewProgression(Vocation.Warrior)) return false;
            comp.State.ChooseMartialFoundation(profile.Id,false);
            var path=comp.State.Find(profile.Id);
            path.ImportLegacyStage(source.def.defName,stage);
            path.ImportedMartialStage=stage;
            if(ultimate!=null) path.RetainedMartialUltimate=profile.Ultimate;
            if(profile.Id=="rwom.gladiator")
            {
                if(Learned("TM_Fortitude")) path.Defense=GladiatorDefense.Fortitude;
                if(Learned("TM_Cleave")) path.Attack=GladiatorAttack.Cleave;
                if(stage==InitiationStage.Ascended) path.Ultimate=GladiatorUltimate.Whirlwind;
            }
            else if(profile.Id=="rwom.sniper")
            {
                if(Learned("TM_Headshot")) path.ShotChoice=SniperShotChoice.Headshot;
                if(stage==InitiationStage.Ascended) path.SniperUltimate=SniperUltimate.ShadowSlayer;
            }
            else if(profile.Id=="rwom.ranger" && stage==InitiationStage.Ascended) path.RangerUltimate=RangerUltimate.ArrowStorm;
            else if(profile.Id=="rwom.bladedancer" && stage==InitiationStage.Ascended) path.BladeUltimate=BladeUltimate.PhaseStrike;
            else if(school!=null) path.MartialData=new SuperSoldierTraining{School=school};
            return true;
        }
        internal static bool AlternativeConflicts(Pawn pawn,bool alternative)
        {
            if(!alternative || !ClassIdentity.TryGetState(pawn,out var state)) return false;
            var path=state.Disciplines.FirstOrDefault(d=>d.Stage==InitiationStage.Master && d.RetainedMartialUltimate!=null);
            if(path==null) return false;
            if(pawn.Spawned)
            {
                var def=DefDatabase<TMAbilityDef>.GetNamedSilentFail(path.RetainedMartialUltimate);
                Messages.Message("TM_ImportedMartialUltimate".Translate(def?.LabelCap.ToString()??path.RetainedMartialUltimate),pawn,MessageTypeDefOf.RejectInput,false);
            }
            return true;
        }
        private static bool IsAvailable(string id) => id=="rwom.gladiator" || id=="rwom.sniper" || id=="rwom.bladedancer" || id=="rwom.ranger" ||
            AdditionalMartialRegistry.ForId(id)!=null;

        // Supply only trial tools which never existed in classic. Class-grant
        // functions can create health/resource owners and learn locked families.
        internal static bool RestoreImportedTools(CompAbilityUserMight might)
        {
            if(!ClassIdentity.TryGetState(might.Pawn,out var state))return false;
            var path=state.Disciplines.FirstOrDefault(p=>p.ImportedTrait!=null && p.Domain==DisciplineDomain.Might);
            if(path==null || path.Stage!=path.ImportedMartialStage) return false;
            void Tool(string name)
            {
                var def=DefDatabase<TMAbilityDef>.GetNamedSilentFail(name);
                if(def!=null) TM_ClassState.AddRuntimeAbility(might,def);
            }
            switch(path.ClassId)
            {
                case "rwom.bladedancer": Tool("TM_BladePassage");break;
                case "rwom.monk": Tool(MonkMartialPath.SealName);break;
                case "rwom.commander": Tool(CommanderMartialPath.Crew);if(path.Stage>=InitiationStage.Master)Tool(CommanderMartialPath.Rally);break;
                case "rwom.faceless": Tool(FacelessMartialPath.Echo);break;
            }
            return true;
        }
    }
}
