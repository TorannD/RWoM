using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    [HarmonyPatch(typeof(QuestManager), nameof(QuestManager.Notify_PlantHarvested))]
    internal static class ApothecaryProductHarvest
    {
        // Vanilla calls this only for a successfully produced, positive primary yield.
        private static void Postfix(Pawn worker, Thing harvested)
        {
            var owner=ApothecaryMartialPath.Owner(worker);
            var path=ApothecaryMartialPath.Path(owner);
            if (path==null || harvested==null || harvested.stackCount<=0 || worker.jobs?.curDriver is not JobDriver_PlantWork ||
                worker.CurJob.targetA.Thing is not Plant plant || !plant.Spawned || plant.Map!=worker.Map ||
                !plant.HarvestableNow || plant.Blighted || plant.LeaflessNow || plant.def.plant?.harvestedThingDef!=harvested.def) return;
            var stock=ApothecaryMartialPath.Stock(owner);
            if (stock==null) return;
            var skill=owner.GetCompAbilityUserMight().MightData.MightPowerSkill_Herbalist.FirstOrDefault(s=>s.label=="TM_Herbalist_ver");
            float before=stock.Severity;
            stock.Severity += Rand.Range(1f,2f) * (2f+.1f*(skill?.level??0)) * plant.Growth * (path.MartialAlternate ? 1f : 1.05f);
            if (stock.Severity>before) ApothecaryMartialPath.Harvest(owner,plant.thingIDNumber);
        }
    }

    internal static class ApothecaryEpisodes
    {
        private sealed class Flag { public bool Intentional, Detoxed; }
        private static readonly ConditionalWeakTable<Hediff,Flag> flags=new();
        internal static bool Wound(Hediff h) => h is Hediff_Injury injury && !injury.IsPermanent() && injury.Severity>0 &&
            injury.ageTicks <= GenDate.TicksPerDay && (!flags.TryGetValue(h,out var f) || !f.Intentional);
        internal static bool Available(Hediff illness) => illness != null && (!flags.TryGetValue(illness,out var f) || !f.Detoxed);
        internal static void Detoxed(Hediff illness) => flags.GetOrCreateValue(illness).Detoxed=true;

        [HarmonyPatch(typeof(Pawn_HealthTracker),nameof(Pawn_HealthTracker.AddHediff),new[]{typeof(Hediff),typeof(BodyPartRecord),typeof(DamageInfo?),typeof(DamageWorker.DamageResult)})]
        private static class MarkFriendlyWound
        {
            private static void Postfix(Pawn ___pawn,Hediff hediff,BodyPartRecord part,DamageInfo? dinfo)
            {
                if (!ClassProgressionFeature.Enabled || hediff is not Hediff_Injury || !dinfo.HasValue ||
                    ___pawn.Faction?.IsPlayer!=true || dinfo.Value.Instigator?.Faction!=___pawn.Faction) return;
                foreach(var h in ___pawn.health.hediffSet.hediffs)
                    if(h is Hediff_Injury && h.Part==(part??hediff.Part) && h.def==hediff.def) flags.GetOrCreateValue(h).Intentional=true;
            }
        }
        [HarmonyPatch(typeof(Hediff),nameof(Hediff.ExposeData))]
        private static class SaveFlags
        {
            private static void Postfix(Hediff __instance)
            {
                if(Scribe.mode!=LoadSaveMode.Saving && Scribe.mode!=LoadSaveMode.LoadingVars) return;
                if(__instance is not Hediff_Injury && __instance.def!=HediffDefOf.FoodPoisoning) return;
                flags.TryGetValue(__instance,out var flag);
                if(Scribe.mode==LoadSaveMode.Saving && flag==null) return;
                bool intentional=flag?.Intentional??false, detoxed=flag?.Detoxed??false;
                Scribe_Values.Look(ref intentional,"rwomApothecaryIntentional");
                Scribe_Values.Look(ref detoxed,"rwomApothecaryDetoxed");
                if(Scribe.mode==LoadSaveMode.LoadingVars && (intentional||detoxed))
                {
                    flag=flags.GetOrCreateValue(__instance);flag.Intentional=intentional;flag.Detoxed=detoxed;
                }
            }
        }
    }

    [HarmonyPatch(typeof(TendUtility),nameof(TendUtility.DoTend),new[]{typeof(Pawn),typeof(Pawn),typeof(Medicine)})]
    internal static class ApothecaryWoundTrial
    {
        private static void Prefix(Pawn doctor,Pawn patient,out List<Hediff> __state)
        {
            __state=null;
            var path=ApothecaryMartialPath.Path(doctor);
            if (path==null || path.Stage==InitiationStage.Foundation || path.Stage==InitiationStage.Ascended ||
                doctor?.Spawned!=true || doctor==patient || patient?.Spawned!=true || patient.Dead || patient.IsPrisoner ||
                patient.Faction==null || patient.Faction!=doctor.Faction || patient.Map!=doctor.Map ||
                doctor.jobs?.curDriver is not JobDriver_TendPatient || doctor.CurJob.targetA.Thing!=patient) return;
            foreach(var wound in patient.health.hediffSet.hediffs)
                if(ApothecaryEpisodes.Wound(wound) && wound.TendableNow()) (__state??=new()).Add(wound);
        }
        private static void Postfix(Pawn doctor,List<Hediff> __state)
        {
            if(__state==null) return;
            foreach(var wound in __state)
                if(!wound.TendableNow()) { ApothecaryMartialPath.Treatment(doctor,wound.loadID,false);break; }
        }
    }

    public sealed class ApothecaryAntitoxinAbility : MightAbility
    {
        public ApothecaryAntitoxinAbility() { }
        public ApothecaryAntitoxinAbility(CompAbilityUser user):base(user) { }
        public ApothecaryAntitoxinAbility(AbilityData data):base(data) { }
        public ApothecaryAntitoxinAbility(Pawn pawn,AbilityUser.AbilityDef def):base(pawn,def) { }
        public override void PostAbilityAttempt() { } // A verified, eligible episode pays on contact.
        public override bool CanCastPowerCheck(AbilityContext context,out string reason)
        {
            if(!base.CanCastPowerCheck(context,out reason)) return false;
            var stock=ApothecaryMartialPath.Stock(Pawn);
            if(stock!=null && stock.Severity>=stock.MaxSeverity*.20f) return true;
            reason="TM_ApothecaryAntitoxinLow".Translate();return false;
        }
    }

    public sealed class Verb_ApothecaryAntitoxin : Verb_SpiritTargetable
    {
        internal const string HostedOrder="TM_SpiritCastAntitoxin";
        internal static bool Funded(Pawn body,MightAbility power) => power is ApothecaryAntitoxinAbility &&
            ApothecaryMartialPath.Owner(body)==power.Pawn &&
            ApothecaryMartialPath.Path(power.Pawn)?.Stage>=InitiationStage.Adept &&
            ApothecaryMartialPath.Stock(power.Pawn) is Hediff_ApothecaryHerbs stock && stock.Severity>=stock.MaxSeverity*.20f;
        internal static Verb_ApothecaryAntitoxin For(Pawn body,MightAbility power) =>
            new Verb_ApothecaryAntitoxin{caster=body,Ability=power,verbProps=power.mightDef.MainVerb};
        private static Hediff Episode(Pawn caster,Pawn target)
        {
            var path=ApothecaryMartialPath.Path(ApothecaryMartialPath.Owner(caster));
            if(path==null || path.Stage<InitiationStage.Adept || caster?.Spawned!=true || caster.Dead || caster.Downed ||
                target?.Spawned!=true || target.Map!=caster.Map || target.Dead || target.IsPrisoner ||
                target.Faction!=caster.Faction || !caster.CanReachImmediate(target,PathEndMode.Touch)) return null;
            var illness=target.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.FoodPoisoning);
            return ApothecaryEpisodes.Available(illness) && illness.Severity>0 ? illness : null;
        }
        public override bool CanHitTargetFrom(IntVec3 root,LocalTargetInfo target) => Episode(CasterPawn,target.Pawn)!=null;
        protected override bool TryCastShot()
        {
            var p=CasterPawn;var owner=ApothecaryMartialPath.Owner(p);
            if(Ability!=null && Ability.Pawn!=owner) return false;
            var illness=Episode(p,currentTarget.Pawn);var stock=ApothecaryMartialPath.Stock(owner);
            if (illness==null || stock==null || stock.Severity<stock.MaxSeverity*.20f) return false;
            ApothecaryEpisodes.Detoxed(illness);
            illness.Severity*=.80f;
            stock.Severity-=stock.MaxSeverity*.20f;
            if(Ability!=null) Ability.CooldownTicksLeft=Compatibility.AbilityTiming.Cooldown(2400);
            ApothecaryMartialPath.Treatment(owner,illness.loadID,true);
            return true;
        }
    }
}
