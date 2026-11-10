using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.ClassProgression
{
    // New progression keeps the real equipment, including its comps, stuff and save identity.
    // Historical Spec_Base copies retain their old restoration path; never multiply them twice.
    internal static class SuperSoldierWeaponProfile
    {
        private sealed class VerbState
        {
            internal VerbProperties Baseline, Modified;
            internal string School;
            internal int Rank;
        }
        private static readonly ConditionalWeakTable<Verb,VerbState> verbs=new();
        private static readonly MethodInfo clone=AccessTools.Method(typeof(object),"MemberwiseClone");

        internal static bool TryGet(ThingWithComps weapon,out string school,out int rank)
        {
            school=null;rank=0;
            if(weapon?.def==null || weapon.Destroyed || weapon.def.defName?.Contains("Spec_Base")==true ||
               weapon.ParentHolder is not Pawn_EquipmentTracker tracker || tracker.Primary!=weapon || !weapon.def.IsRangedWeapon) return false;
            var body=tracker.pawn;
            var owner=SuperSoldierMartialPath.Owner(body);
            var path=SuperSoldierMartialPath.Path(owner);
            school=SuperSoldierMartialPath.Existing(path)?.School;
            var comp=owner?.GetCompAbilityUserMight();
            if(school==null || comp==null || !Matches(weapon.def,school)) return false;
            // The owner can resolve to a soul or another pawn whose comp never
            // initialized might data; this runs inside vanilla gizmo/verb/stat
            // paths, so a stale mid-save state must degrade to "no profile".
            var data=comp.MightData;
            if(data==null) return false;
            string selectedSchool=school;
            var power=data.MightPowersSS?.FirstOrDefault(p=>p?.abilityDef?.defName==selectedSchool);
            if(power?.learned!=true) return false;
            var local=body.GetCompAbilityUserMight();
            if(owner!=body && local?.customClass!=null && TM_ClassUtility.ClassHasAbility((TMAbilityDef)power.abilityDef,null,local)) return false;
            rank=data.GetSkill_Power((TMAbilityDef)power.abilityDef)?.level??0;
            return true;
        }
        private static bool Matches(ThingDef weapon,string school)
        {
            string name=weapon.defName??"",lower=name.ToLowerInvariant();
            switch(school)
            {
                case "TM_PistolSpec": return lower.Contains("pistol") || name.Contains("SMG") || lower.Contains("revolver") || Listed(name,"TM_Category_Pistols");
                case "TM_RifleSpec": return ((lower.Contains("rifle") || name.Contains("LMG")) && !lower.Contains("sniper")) || Listed(name,"TM_Category_Rifles");
                case "TM_ShotgunSpec": return lower.Contains("shotgun") || Listed(name,"TM_Category_Shotguns");
                default:return false;
            }
        }
        private static bool Listed(string weapon,string category) =>
            DefDatabase<WeaponCategoryList>.GetNamedSilentFail(category)?.weaponDefNames?.Any(name=>name==weapon || name=="*")==true;
        internal static bool HasWeapon(Pawn body) => TryGet(body?.equipment?.Primary,out _,out _);
        internal static bool UsesOriginalEquipment(Pawn body)
        {
            var owner=SuperSoldierMartialPath.Owner(body);
            // Equipment custody survives depletion. This selects migration only;
            // TryGet still requires an active source before applying any bonuses.
            if(owner==null && SpiritRebirthState.TryGetPossessingSpirit(body,out var retained)) owner=retained;
            if(SuperSoldierMartialPath.Path(owner)==null) return false;
            return owner==body || body?.GetCompAbilityUserMight()?.customClass==null;
        }
        internal static bool ReadyWeapon(Pawn body) => HasWeapon(body) || (body?.GetCompAbilityUserMight()?.specWpnRegNum??-1)>=0;
        internal static float Factor(ThingWithComps weapon,StatDef stat)
        {
            if(stat!=StatDefOf.RangedWeapon_DamageMultiplier && stat!=StatDefOf.RangedWeapon_Cooldown &&
               stat!=StatDefOf.AccuracyTouch && stat!=StatDefOf.AccuracyShort && stat!=StatDefOf.AccuracyMedium && stat!=StatDefOf.AccuracyLong) return 1;
            if(!TryGet(weapon,out string school,out int rank)) return 1;
            bool pistol=school=="TM_PistolSpec",rifle=school=="TM_RifleSpec";
            if(stat==StatDefOf.RangedWeapon_DamageMultiplier) return 1+(pistol ? .03f:.02f)*rank;
            if(stat==StatDefOf.RangedWeapon_Cooldown) return 1-(pistol ? .025f:rifle ? .01f:.03f)*rank;
            if(stat==StatDefOf.AccuracyTouch) return 1+(!pistol&&!rifle ? .01f:0)*rank;
            if(stat==StatDefOf.AccuracyShort) return 1+(rifle ? .01f:.015f)*rank;
            if(stat==StatDefOf.AccuracyMedium) return 1+(rifle ? .01f:.005f)*rank;
            return 1+(rifle ? .01f:0)*rank;
        }
        internal static void Refresh(ThingWithComps weapon,Verb verb)
        {
            if(verb?.verbProps==null) return;
            bool active=TryGet(weapon,out string school,out int rank);
            if(!verbs.TryGetValue(verb,out var state))
            {
                if(!active) return;
                state=new VerbState{Baseline=verb.verbProps};verbs.Add(verb,state);
            }
            // Respect a later addon replacement of the instance properties.
            if(verb.verbProps!=state.Modified && verb.verbProps!=state.Baseline) state.Baseline=verb.verbProps;
            if(!active)
            {
                if(verb.verbProps==state.Modified) verb.verbProps=state.Baseline;
                verbs.Remove(verb);return;
            }
            if(verb.verbProps==state.Modified && state.School==school && state.Rank==rank) return;
            var properties=(VerbProperties)clone.Invoke(state.Baseline,null);
            bool pistol=school=="TM_PistolSpec",rifle=school=="TM_RifleSpec";
            properties.range=state.Baseline.range*(1+(rifle ? .02f:.01f)*rank);
            properties.warmupTime=state.Baseline.warmupTime*(1-(pistol ? .025f:rifle ? .01f:.03f)*rank);
            properties.burstShotCount=Mathf.RoundToInt(state.Baseline.burstShotCount*(1+(pistol ? .02f:rifle ? .03f:0)*rank));
            properties.ticksBetweenBurstShots=Mathf.RoundToInt(state.Baseline.ticksBetweenBurstShots*(1-(pistol ? .03f:.02f)*rank));
            state.Modified=properties;state.School=school;state.Rank=rank;verb.verbProps=properties;
        }
    }

    [HarmonyPatch(typeof(CompEquippable),nameof(CompEquippable.PrimaryVerb),MethodType.Getter)]
    internal static class SuperSoldierWeaponVerbPatch
    {
        private static void Postfix(CompEquippable __instance,Verb __result) => SuperSoldierWeaponProfile.Refresh(__instance.parent,__result);
    }
    [HarmonyPatch(typeof(CompEquippable),nameof(CompEquippable.AllVerbs),MethodType.Getter)]
    internal static class SuperSoldierWeaponAllVerbsPatch
    {
        private static void Postfix(CompEquippable __instance,List<Verb> __result)
        {
            if(__result==null) return;
            foreach(var verb in __result) SuperSoldierWeaponProfile.Refresh(__instance.parent,verb);
        }
    }
    [HarmonyPatch(typeof(Verb),nameof(Verb.TryStartCastOn),new Type[]{typeof(LocalTargetInfo),typeof(LocalTargetInfo),typeof(bool),typeof(bool),typeof(bool),typeof(bool)})]
    internal static class SuperSoldierWeaponCastPatch
    {
        private static void Prefix(Verb __instance)
        {
            // Cached attack verbs may outlive a possession or equipment change.
            SuperSoldierWeaponProfile.Refresh(__instance.EquipmentSource,__instance);
        }
    }
    [HarmonyPatch(typeof(StatWorker),nameof(StatWorker.GetValueUnfinalized))]
    internal static class SuperSoldierWeaponStatPatch
    {
        private static void Postfix(StatRequest req,StatDef ___stat,ref float __result)
        {
            if(req.Thing is ThingWithComps weapon) __result*=SuperSoldierWeaponProfile.Factor(weapon,___stat);
        }
    }
}
