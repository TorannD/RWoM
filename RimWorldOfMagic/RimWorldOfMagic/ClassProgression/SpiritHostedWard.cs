using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    // Spells that create a temporary effect on the physical body; their source ability is never cloned.
    public class HediffComp_SpiritCastOrigin : HediffComp
    {
        private Pawn soul;
        private bool projected;
        internal void Bind(Pawn source) { soul=source; projected=true; }
        internal bool Projected => projected;
        protected Pawn BoundOwner => projected ? (Valid ? soul : null) : Pawn;
        internal bool Valid => !projected || soul!=null && SpiritRebirthState.TryGetPossessingSpirit(Pawn,out var held) && held==soul;
        public override bool CompShouldRemove => !Valid;
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref soul,"projectedSoul");
            Scribe_Values.Look(ref projected,"projected");
        }
    }

    internal static class SpiritHostedWard
    {
        internal const string ShieldJob="TM_SpiritCastPaladinShield", DisguiseJob="TM_SpiritCastDisguise";
        private static bool InFamily(TMAbilityDef def,bool shield) => def!=null && (shield ?
            def==TorannMagicDefOf.TM_Shield || def==TorannMagicDefOf.TM_Shield_I || def==TorannMagicDefOf.TM_Shield_II || def==TorannMagicDefOf.TM_Shield_III :
            def==TorannMagicDefOf.TM_Disguise);

        internal static PawnAbility Find(Pawn body,Pawn soul,bool shield)
        {
            if (!SpiritRebirthState.TryGetPossessingSpirit(body,out var actual) || actual!=soul ||
                !SpiritRebirthState.TryGet(soul,out var state) || state.DepletionPending) return null;
            bool HasLocal(CompAbilityUser local) => local?.AbilityData.AllPowers.Any(a=>InFamily(a?.Def as TMAbilityDef,shield))==true;
            if (HasLocal(body.GetCompAbilityUserMagic()) || HasLocal(body.GetCompAbilityUserMight())) return null;
            CompAbilityUser source=shield?(CompAbilityUser)soul.GetCompAbilityUserMagic():soul.GetCompAbilityUserMight();
            var owners=source?.AbilityData.Powers.Where(a=>a!=null && ReferenceEquals(a.AbilityUser,source) &&
                (shield?a is MagicAbility:a is MightAbility) && InFamily(a.Def as TMAbilityDef,shield)).Take(2).ToList();
            return owners?.Count==1 ? owners[0] : null;
        }

        private static HediffDef Effect(PawnAbility ability,out float severity)
        {
            severity=0f;
            if (ability is MagicAbility magic)
            {
                var effects=magic.magicDef.MainVerb?.hediffsToApply;
                if(effects?.Count!=1 || effects[0].hediffDef!=TorannMagicDefOf.TM_HediffShield || effects[0].applyChance!=1f) return null;
                severity=effects[0].severity;
                return effects[0].hediffDef;
            }
            if (ability is not MightAbility might || might.MightUser.ExistingMightData==null) return null;
            var skills=might.MightUser.MightData.MightPowerSkill_Disguise;
            int power=skills.FirstOrDefault(s=>s.label=="TM_Disguise_pwr")?.level??0;
            int versatility=skills.FirstOrDefault(s=>s.label=="TM_Disguise_ver")?.level??0;
            severity=Verb_Disguise.Strength(might.Pawn,might.MightUser,versatility);
            return Verb_Disguise.Effect(power);
        }
        internal static bool Ready(Pawn body,PawnAbility owner)
        {
            if(owner==null || !body.Spawned || body.Dead || body.Downed || body.InMentalState || body.Faction!=Faction.OfPlayer ||
                body.IsPrisoner || owner.CooldownTicksLeft>0 || owner.Pawn==body || SpiritRebirthCasting.SkillOwner(body,owner)!=owner.Pawn) return false;
            var def=owner.Def as TMAbilityDef;
            if(def==null || def.chiCost!=0 || def.bloodCost!=0 || def.requiredHediff!=null || def.requiredNeed!=null || !def.consumeEnergy) return false;
            var effect=Effect(owner,out float severity);
            if(severity<=0 || effect?.comps?.Any(c=>c.compClass==typeof(HediffComp_SpiritCastOrigin))!=true ||
                body.health.hediffSet.HasHediff(effect))return false;
            if(owner is MagicAbility magic)
                return !body.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MuteHD) && magic.MagicUser.Mana!=null &&
                    magic.MagicUser.Mana.CurLevel>=magic.MagicUser.ActualManaCost(def);
            if(owner is not MightAbility might || might.MightUser.Stamina==null) return false;
            return might.MightUser.Stamina.CurLevel>=might.MightUser.ActualStaminaCost(def) &&
                !body.health.hediffSet.hediffs.Any(h=>h.def==TorannMagicDefOf.TM_DisguiseHD || h.def==TorannMagicDefOf.TM_DisguiseHD_I ||
                    h.def==TorannMagicDefOf.TM_DisguiseHD_II || h.def==TorannMagicDefOf.TM_DisguiseHD_III);
        }
        internal static bool Apply(Pawn body,Pawn soul,bool shield)
        {
            var owner=Find(body,soul,shield);
            if(!Ready(body,owner))return false;
            var def=Effect(owner,out var severity);
            var ward=HediffMaker.MakeHediff(def,body);
            var origin=ward.TryGetComp<HediffComp_SpiritCastOrigin>();
            if(origin==null)return false;
            origin.Bind(soul);ward.Severity=severity;body.health.AddHediff(ward);
            if(!body.health.hediffSet.hediffs.Contains(ward))return false;
            if(owner is MagicAbility magic)magic.PayForAttempt(body);
            else owner.PostAbilityAttempt();
            return true;
        }
        internal static IEnumerable<Gizmo> Commands(IEnumerable<Gizmo> existing,Pawn body)
        {
            foreach(var command in existing)yield return command;
            if(!SpiritRebirthState.TryGetPossessingSpirit(body,out var soul))yield break;
            for(int i=0;i<2;i++)
            {
                bool shield=i==0;
                var ability=Find(body,soul,shield);
                var job=DefDatabase<JobDef>.GetNamedSilentFail(shield?ShieldJob:DisguiseJob);
                if(ability==null || job==null)continue;
                var cmd=new Command_Action{defaultLabel=ability.Def.LabelCap,defaultDesc=ability.Def.description,
                    icon=ability.Def.uiIcon,action=()=>{
                        if(body.CurJobDef!=job && Ready(body,Find(body,soul,shield)))
                            body.jobs.TryTakeOrderedJob(JobMaker.MakeJob(job,soul),JobTag.Misc);
                    }};
                if(body.CurJobDef==job || !Ready(body,ability))cmd.Disable("TM_SpiritRebirthCastUnavailable".Translate());
                yield return cmd;
            }
        }
    }
    public sealed class JobDriver_SpiritHostedWard : JobDriver
    {
        private bool attempted;
        public override void ExposeData(){base.ExposeData();Scribe_Values.Look(ref attempted,"attempted");}
        public override bool TryMakePreToilReservations(bool errorOnFailed)=>true;
        protected override IEnumerable<Toil> MakeNewToils()
        {
            var soul=job.targetA.Thing as Pawn;
            bool shield=job.def.defName==SpiritHostedWard.ShieldJob;
            this.FailOn(()=>!SpiritRebirthState.TryGetPossessingSpirit(pawn,out var current) || current!=soul);
            var prep=Toils_General.Wait(1);
            prep.initAction=()=>{
                var ability=SpiritHostedWard.Find(pawn,soul,shield);
                if(ability==null){EndJobWith(JobCondition.Incompletable);return;}
                ticksLeftThisToil=UnityEngine.Mathf.Max(1,GenTicks.SecondsToTicks(ability.Def.MainVerb.warmupTime*Compatibility.AbilityTiming.WarmupMultiplier));
            };
            yield return prep;
            yield return Toils_General.Do(()=>{
                if(attempted || pawn.CurJob!=job || pawn.jobs.curDriver!=this){EndJobWith(JobCondition.Incompletable);return;}
                attempted=true;
                if(!SpiritHostedWard.Apply(pawn,soul,shield))EndJobWith(JobCondition.Incompletable);
            });
        }
    }
}
