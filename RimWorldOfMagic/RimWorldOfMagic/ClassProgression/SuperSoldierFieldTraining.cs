using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    [HarmonyPatch(typeof(Thing),nameof(Thing.TakeDamage))]
    internal static class SuperSoldierWeaponsTrial
    {
        private sealed class Receipt
        {
            internal Pawn Actor,Owner;
        }
        private static void Prefix(Thing __instance, DamageInfo dinfo, out Receipt __state)
        {
            __state=null;
            if(!ClassProgressionFeature.Enabled || __instance is not Pawn target || dinfo.Instigator is not Pawn attacker || dinfo.Weapon==null ||
                attacker.equipment?.Primary?.def!=dinfo.Weapon || attacker.Spawned!=true || !target.Spawned ||
                attacker.Map!=target.Map || attacker.Downed || attacker.Dead || target.Downed || target.Dead ||
                target.IsPrisoner || !target.HostileTo(attacker)) return;
            var owner=SuperSoldierMartialPath.Owner(attacker);
            var path=SuperSoldierMartialPath.Path(owner);
            if(path!=null && (path.Stage==InitiationStage.Adept || path.Stage==InitiationStage.Master) &&
                SuperSoldierMartialPath.Matches(attacker,SuperSoldierMartialPath.Existing(path)?.School)) __state=new Receipt{Actor=attacker,Owner=owner};
        }
        private static void Postfix(Thing __instance, Receipt __state, DamageWorker.DamageResult __result)
        {
            if(__state!=null && __result?.totalDamageDealt>0 && SuperSoldierMartialPath.Owner(__state.Actor)==__state.Owner)
                SuperSoldierMartialPath.Event(__state.Owner,__instance.thingIDNumber,true,Find.TickManager.TicksGame/GenDate.TicksPerDay);
        }
    }

    [HarmonyPatch(typeof(TendUtility),nameof(TendUtility.DoTend),new[]{typeof(Pawn),typeof(Pawn),typeof(Medicine)})]
    internal static class SuperSoldierMedicalTrial
    {
        private sealed class Receipt
        {
            internal Pawn Owner;
            internal readonly List<Hediff_Injury> Wounds=new();
        }
        private static void Prefix(Pawn doctor,Pawn patient,Medicine medicine,out Receipt __state)
        {
            __state=null;
            if(!ClassProgressionFeature.Enabled || medicine==null || doctor?.Spawned!=true || patient?.Spawned!=true || doctor.Map!=patient.Map ||
                doctor==patient || doctor.Dead || doctor.Downed || patient.Dead || patient.IsPrisoner ||
                doctor.Faction==null || patient.Faction!=doctor.Faction || doctor.jobs?.curDriver is not JobDriver_TendPatient ||
                doctor.CurJob.targetA.Thing!=patient) return;
            var owner=SuperSoldierMartialPath.Owner(doctor);
            var path=SuperSoldierMartialPath.Path(owner);
            if(path==null || path.Stage==InitiationStage.Ascended) return;
            foreach(var h in patient.health.hediffSet.hediffs)
                if(h is Hediff_Injury wound && ApothecaryEpisodes.Wound(wound) && wound.TendableNow())
                    (__state??=new Receipt{Owner=owner}).Wounds.Add(wound);
        }
        private static void Postfix(Pawn doctor,Receipt __state)
        {
            if(__state==null || SuperSoldierMartialPath.Owner(doctor)!=__state.Owner) return;
            foreach(var h in __state.Wounds)
                if(!h.TendableNow()) SuperSoldierMartialPath.Event(__state.Owner,h.loadID,false,Find.TickManager.TicksGame/GenDate.TicksPerDay);
        }
    }

    public sealed class SuperSoldierDressingAbility : MightAbility
    {
        public SuperSoldierDressingAbility() { }
        public SuperSoldierDressingAbility(CompAbilityUser user):base(user) { }
        public SuperSoldierDressingAbility(AbilityData data):base(data) { }
        public SuperSoldierDressingAbility(Pawn pawn,AbilityUser.AbilityDef def):base(pawn,def) { }
        public override bool CanCastPowerCheck(AbilityContext context,out string reason)
        {
            if(!base.CanCastPowerCheck(context,out reason)) return false;
            if(Verb_SuperSoldierFieldDressing.CanDress(Pawn)) return true;
            reason="TM_SoldierNeedsMedicine".Translate();return false;
        }
    }
    public sealed class Verb_SuperSoldierFieldDressing : Verb_SpiritTargetable
    {
        protected override bool TryCastShot() => Dress(CasterPawn);
        internal static bool CanDress(Pawn p)
        {
            var path=SuperSoldierMartialPath.Path(SuperSoldierMartialPath.Owner(p) ?? p);
            if (path==null || (!path.MartialAlternate && path.Stage<InitiationStage.Adept) || !p.Spawned || p.Downed || p.Dead || p.inventory==null) return false;
            return Injury(p)!=null && p.inventory.innerContainer.Any(t=>t is Medicine);
        }
        private static Hediff_Injury Injury(Pawn p) => p.health?.hediffSet?.hediffs.OfType<Hediff_Injury>().FirstOrDefault(h=>h.CanHealNaturally() && h.TendableNow());
        internal static bool Dress(Pawn p)
        {
            if(!CanDress(p)) return false;
            var wound=Injury(p);
            var med=p.inventory.innerContainer.OfType<Medicine>().OrderBy(m=>m.GetStatValue(StatDefOf.MedicalPotency)).FirstOrDefault();
            if(wound==null || med==null) return false;
            float quality=UnityEngine.Mathf.Clamp(med.GetStatValue(StatDefOf.MedicalPotency)*.4f,.1f,.5f);
            med.SplitOff(1).Destroy();
            wound.Tended(quality,1f);
            return true;
        }
    }
    public sealed class SuperSoldierDoctrineStat : StatPart
    {
        public bool fieldMedicine;
        private bool Applied(StatRequest req)
        {
            if(req.Thing is not Pawn p) return false;
            var path=SuperSoldierMartialPath.Path(SuperSoldierMartialPath.Owner(p) ?? p);
            var d=SuperSoldierMartialPath.Existing(path);
            return path?.Stage==InitiationStage.Ascended && d!=null && d.Support==fieldMedicine &&
                (fieldMedicine || SuperSoldierMartialPath.Matches(p,d.School));
        }
        public override void TransformValue(StatRequest req,ref float val)
        {
            if(Applied(req)) val+=fieldMedicine ? .15f : .07f;
        }
        public override string ExplanationPart(StatRequest req) => Applied(req)?(fieldMedicine?"TM_SoldierSurgeon":"TM_SoldierMarksman").Translate().ToString():null;
    }
}
