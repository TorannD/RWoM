using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal enum EchoKind { None, Ember, Frost, Storm, Force, Remedy, Blade }

    internal static class FacelessImitation
    {
        internal const string StudyOrder = "TM_SpiritStudyFaceless", EchoOrder = "TM_SpiritCastFacelessEcho";
        internal static bool Handles(string job) => job == StudyOrder || job == EchoOrder;

        internal static bool ValidTarget(Pawn body, MightAbility ability, string job, Pawn target)
        {
            if (target == null || ability == null || body?.Spawned != true) return false;
            if (job == EchoOrder) return CanUseFrom(body,ability.Pawn,target,body.Position);
            return job == StudyOrder && target != body && target != ability.Pawn && target.Spawned &&
                target.Map == body.Map && !target.Dead && !target.Downed &&
                new Verb_Mimic {caster=body, Ability=ability, verbProps=ability.mightDef.MainVerb}
                    .CanHitTargetFrom(body.Position,target);
        }

        internal static EchoKind Kind(TMAbilityDef source)
        {
            if (source == null) return EchoKind.None;
            var id = source.defName;
            // Restrict the copy to explicit ability families, including their numbered ranks.
            if (id.EndsWith("_III")) id = id.Substring(0,id.Length-4);
            else if (id.EndsWith("_II")) id = id.Substring(0,id.Length-3);
            else if (id.EndsWith("_I")) id = id.Substring(0,id.Length-2);
            return id switch {
                "TM_Firebolt" or "TM_Fireclaw" or "TM_Fireball" => EchoKind.Ember,
                "TM_Icebolt" or "TM_FrostRay" => EchoKind.Frost,
                "TM_LightningBolt" or "TM_ChainLightning" => EchoKind.Storm,
                "TM_ArcaneBolt" or "TM_PsionicBlast" or "TM_PommelStrike" => EchoKind.Force,
                "TM_Heal" or "TM_FirstAid" => EchoKind.Remedy,
                "TM_Cleave" or "TM_SeismicSlash" or "TM_ArrowStorm" => EchoKind.Blade,
                _ => EchoKind.None
            };
        }
        private static void Include(List<TMAbilityDef> choices, CompAbilityUser owner, TMAbilityDef borrowed)
        {
            if (owner?.AbilityData?.Powers == null) return;
            foreach(var power in owner.AbilityData.Powers)
            {
                if (power?.Def is not TMAbilityDef def || def == borrowed) continue;
                var kind = Kind(def);
                if (kind != EchoKind.None && !choices.Any(x => Kind(x) == kind)) choices.Add(def);
            }
        }
        internal static bool Learn(Pawn pawn,Pawn model) => LearnFrom(pawn,pawn,model);

        internal static bool LearnFrom(Pawn pawn, Pawn owner, Pawn model)
        {
            var path = FacelessMartialPath.ActivePath(pawn,out var active);
            if (active != owner || owner == model) return false;
            var command = DefDatabase<TMAbilityDef>.GetNamedSilentFail(FacelessMartialPath.Echo);
            if (path == null || command == null || pawn == model || !pawn.Spawned || pawn.Dead || pawn.Downed ||
                model?.Spawned != true || model.Map != pawn.Map || model.Dead || model.Downed || model.RaceProps?.Humanlike != true) return false;
            var choices = new List<TMAbilityDef>();
            var magic = model.GetCompAbilityUserMagic();var might = model.GetCompAbilityUserMight();
            Include(choices, magic, magic?.mimicAbility);
            Include(choices, might, might?.mimicAbility);
            var previous = (path.MartialData as FacelessDisciplineData)?.Sample;
            if (previous != null) choices.RemoveAll(x => Kind(x) == Kind(previous));
            if (choices.Count == 0) return false;
            var selected = choices.RandomElement();
            TM_ClassState.AddRuntimeAbility(owner.GetCompAbilityUserMight(),command);
            FacelessMartialPath.Data(path).Sample = selected;
            FacelessMartialPath.Studied(path,Kind(selected));
            return true;
        }
        internal static bool CanUse(Pawn pawn, Pawn target, IntVec3 from) => CanUseFrom(pawn,pawn,target,from);

        internal static bool CanUseFrom(Pawn pawn, Pawn owner, Pawn target, IntVec3 from)
        {
            var path=FacelessMartialPath.ActivePath(pawn,out var active);
            if (active!=owner) return false;
            var kind=Kind((path?.MartialData as FacelessDisciplineData)?.Sample);
            if(kind==EchoKind.None || pawn?.Spawned!=true || pawn.Dead || pawn.Downed || target?.Spawned!=true || target.Dead ||
                target.Map!=pawn.Map || !from.InBounds(pawn.Map) || !target.Position.InHorDistOf(from,18) ||
                (from!=target.Position && !GenSight.LineOfSight(from,target.Position,pawn.Map))) return false;
            if(kind==EchoKind.Remedy) return target.Faction!=null && target.Faction==pawn.Faction && !target.IsPrisoner &&
                target.health.hediffSet.hediffs.OfType<Hediff_Injury>().Any(h=>h.CanHealNaturally() && h.Severity>0);
            return !pawn.WorkTagIsDisabled(WorkTags.Violent) && !target.Downed && !target.IsPrisoner && target.HostileTo(pawn);
        }
        internal static bool Perform(Pawn pawn,Pawn target) => PerformFrom(pawn,pawn,target);

        internal static bool PerformFrom(Pawn pawn, Pawn owner, Pawn target)
        {
            if (!CanUseFrom(pawn,owner,target,pawn.Position)) return false;
            var path=FacelessMartialPath.Path(owner);
            var d=FacelessMartialPath.Data(path);
            var kind=Kind(d.Sample);
            int rank=owner.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault(s=>s.label=="TM_Mimic_pwr")?.level??0;
            float factor=1f+.05f*Mathf.Clamp(rank,0,5)+(path.MartialAlternate ? .2f : 0f)+(d.Virtuoso ? .5f : 0f);
            if(kind==EchoKind.Remedy)
            {
                var injury=target.health.hediffSet.hediffs.OfType<Hediff_Injury>().Where(h=>h.CanHealNaturally() && h.Severity>0)
                    .OrderByDescending(h=>h.Severity).First();
                injury.Heal(4f*factor);
            }
            else
            {
                var type=kind switch {
                    EchoKind.Ember=>DamageDefOf.Flame, EchoKind.Frost=>DamageDefOf.Frostbite,
                    EchoKind.Storm=>TMDamageDefOf.DamageDefOf.TM_Lightning,
                    EchoKind.Blade=>DamageDefOf.Cut, _=>DamageDefOf.Blunt };
                if (type==null) return false;
                if (target.TakeDamage(new DamageInfo(type,Mathf.RoundToInt(12f*factor),instigator:pawn)).totalDamageDealt<=0) return true;
            }
            FacelessMartialPath.Performed(path);
            return true;
        }
    }

    public sealed class Verb_FacelessEcho : Verb_UseAbility
    {
        public override bool CanHitTargetFrom(IntVec3 root,LocalTargetInfo target) => FacelessImitation.CanUse(CasterPawn,target.Pawn,root);
        protected override bool TryCastShot() => FacelessImitation.Perform(CasterPawn,currentTarget.Pawn);
    }
}
