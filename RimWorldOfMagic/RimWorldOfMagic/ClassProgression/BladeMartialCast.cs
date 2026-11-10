using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class BladeMartialCast
    {
        internal const string Spin="TM_SpiritCastBladeSpin", Slash="TM_SpiritCastSeismicSlash", Passage="TM_SpiritCastBladePassage", Phase="TM_SpiritCastPhaseStrike";
        internal static bool Handles(string order) => order==Spin || order==Slash || order==Passage || order==Phase;
        internal static bool PhaseRank(TMAbilityDef def) => def!=null && (def==TorannMagicDefOf.TM_PhaseStrike ||
            def==TorannMagicDefOf.TM_PhaseStrike_I || def==TorannMagicDefOf.TM_PhaseStrike_II || def==TorannMagicDefOf.TM_PhaseStrike_III);
        internal static bool HasWeapon(Pawn body) => body?.Spawned==true && !body.Dead && !body.Downed &&
            body.equipment?.Primary is ThingWithComps weapon && !weapon.def.IsRangedWeapon;

        internal static int WeaponDamage(Pawn body,Pawn owner,float factor)
        {
            var weapon=body?.equipment?.Primary;
            if(weapon==null || weapon.def.IsRangedWeapon || owner?.GetCompAbilityUserMight() is not CompAbilityUserMight source)return 0;
            var weaponDps=weapon.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS,false)*.7f;
            var multiplier=weapon.GetStatValue(StatDefOf.MeleeWeapon_DamageMultiplier,false);
            return Mathf.RoundToInt(factor*source.mightPwr*multiplier*(body.GetStatValue(StatDefOf.MeleeDPS,false)+weaponDps));
        }

        private static Verb_SeismicSlash BoundSlash(Pawn body,MightAbility power) =>
            new Verb_SeismicSlash {Ability=power,caster=body,verbProps=power.mightDef.MainVerb};
        private static Verb_PhaseStrike BoundPhase(Pawn body,MightAbility power) =>
            new Verb_PhaseStrike {Ability=power,caster=body,verbProps=power.mightDef.MainVerb};

        internal static float StrikeDamage(Pawn body,Pawn owner)
        {
            var source=owner.GetCompAbilityUserMight();
            // An equipped melee weapon has a five-point floor. Until its first refresh
            // after load the component still contains its initializer of one.
            if (body==owner && source.weaponDamage>=5f) return source.weaponDamage;
            float weapon=body.equipment.Primary.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS,false);
            return Mathf.Max(5f,(weapon+body.GetStatValue(StatDefOf.MeleeDPS,false))*source.mightPwr);
        }

        internal static bool Valid(Pawn body,MightAbility power,string order,LocalTargetInfo target)
        {
            if(!SpiritHostedMartial.Ready(body,power,order))return false;
            if(order==Passage)return target.Pawn!=null && Verb_BladePassage.For(body,power).CanHitTargetFrom(body.Position,target);
            if(order==Phase)return target.IsValid && BoundPhase(body,power).CanHitTargetFrom(body.Position,target);
            return order==Spin || target.IsValid && target.Cell!=body.Position &&
                BoundSlash(body,power).CanHitTargetFrom(body.Position,target);
        }
        internal static bool Execute(Pawn body,MightAbility power,string order,LocalTargetInfo target)
        {
            if(!Valid(body,power,order,target))return false;
            if(order==Passage)return Verb_BladePassage.For(body,power).Execute(target);
            if(order==Phase)return BoundPhase(body,power).Execute(target);
            bool cast=order==Spin ? new Verb_BladeSpin {Ability=power,caster=body,verbProps=power.mightDef.MainVerb}.Spin()
                : BoundSlash(body,power).Slash(target);
            if(cast)power.PostAbilityAttempt();
            return cast;
        }
        internal static void ChooseTarget(Pawn body,Pawn soul,MightAbility power,string order)
        {
            var map=body.Map;
            bool Available(LocalTargetInfo t)=>body.Spawned && body.Map==map && SpiritHostedMartial.Find(body,soul,order)==power && Valid(body,power,order,t);
            Find.Targeter.BeginTargeting(power.mightDef.MainVerb.targetParams,t=>{
                if (!Available(t))return;
                var job=JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(order),soul,t);
                void Submit()=>body.jobs.TryTakeOrderedJob(job,JobTag.Misc);
                if(order==Phase)
                {
                    var cell=t.Cell;
                    Odyssey.ManualTravelSafety.Submit(body,job,map,cell,true,()=>t.Cell==cell && Available(t),Submit);
                    return;
                }
                if(order!=Passage){Submit();return;}
                var verb=Verb_BladePassage.For(body,power);
                if(verb.TryDestination(t,out var landing))
                    Odyssey.ManualTravelSafety.Submit(body,job,map,landing,true,
                        ()=>Available(t) && verb.TryDestination(t,out var now) && now==landing,Submit);
            },highlightAction:t=>GenDraw.DrawRadiusRing(body.Position,order==Passage?BladeCareer.PassageRange(soul):power.mightDef.MainVerb.range),
                targetValidator:Available,caster:body);
        }
    }
}
