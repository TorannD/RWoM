using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritHostedCommander
    {
        internal static void VisitAuras(Pawn soul,bool end)
        {
            if (soul?.health?.hediffSet==null) return;
            VisitAura(soul,TorannMagicDefOf.TM_ProvisionerAuraHD,TorannMagicDefOf.TM_ProvisionerAura,end);
            VisitAura(soul,TorannMagicDefOf.TM_TaskMasterAuraHD,TorannMagicDefOf.TM_TaskMasterAura,end);
            VisitAura(soul,TorannMagicDefOf.TM_CommanderAuraHD,TorannMagicDefOf.TM_CommanderAura,end);
        }
        private static void VisitAura(Pawn soul,HediffDef effect,TMAbilityDef ability,bool end)
        {
                var aura=effect==null ? null : soul.health.hediffSet.GetFirstHediffOfDef(effect);
                if(aura==null) return;
                if(end || AuraEmitter(soul)==null)
                {
                    soul.health.RemoveHediff(aura);
                    var power=soul.GetCompAbilityUserMight()?.ExistingMightData?.MightPowersC.FirstOrDefault(p=>p.abilityDef==ability);
                    if(power!=null) power.autocast=false;
                }
                else
                {
                    float adjustment=0;
                    aura.TryGetComp<HediffComp_ProvisionerAura>()?.CompPostTick(ref adjustment);
                    aura.TryGetComp<HediffComp_TaskMasterAura>()?.CompPostTick(ref adjustment);
                    aura.TryGetComp<HediffComp_CommanderAura>()?.CompPostTick(ref adjustment);
                }
        }
        internal static Pawn AuraEmitter(Pawn source)
        {
            if (source == null || source.Dead) return null;
            if (SpiritRebirthState.TryGet(source,out var state))
            {
                var body=state.Host;
                return !state.DepletionPending && body?.Spawned == true && !body.Dead &&
                    CommanderMartialPath.Owner(body)==source ? body : null;
            }
            return source.Spawned ? source : null;
        }
        internal static CommanderOperationAbility Operation(Pawn body,Pawn soul,string name)
        {
            if(body==soul || CommanderMartialPath.Owner(body)!=soul ||
                (name!=CommanderMartialPath.Evac && name!=CommanderMartialPath.Aid)) return null;
            var comp=soul?.GetCompAbilityUserMight();
            var powers=comp?.AbilityData.Powers.OfType<CommanderOperationAbility>()
                .Where(a=>a.Def.defName==name && ReferenceEquals(a.AbilityUser,comp)).Take(2).ToList();
            return powers?.Count==1 ? powers[0] : null;
        }
        internal static bool Ready(Pawn body,CommanderOperationAbility ability) => ability!=null &&
            body?.Faction==Faction.OfPlayer && !body.InMentalState && !body.IsPrisoner &&
            body.stances?.stunner?.Stunned!=true && ability.CooldownTicksLeft<=0 &&
            Operation(body,ability.Pawn,ability.Def.defName)==ability && JobDriver_CommanderOperation.ReadyFrom(body,ability.Pawn);

        internal static bool Target(Pawn body,CommanderOperationAbility ability,LocalTargetInfo target) =>
            Ready(body,ability) && target.IsValid && target.Cell.InBounds(body.Map) && !target.Cell.Fogged(body.Map) &&
            body.Position.InHorDistOf(target.Cell,14) && GenSight.LineOfSight(body.Position,target.Cell,body.Map);

        internal static IEnumerable<Gizmo> Commands(Pawn body,Pawn soul)
        {
            foreach(string name in new[]{CommanderMartialPath.Evac,CommanderMartialPath.Aid})
            {
                var power=Operation(body,soul,name);
                var job=DefDatabase<JobDef>.GetNamedSilentFail("TM_CommanderOperationJob");
                if(power==null || job==null) continue;
                var command=new Command_Action {defaultLabel=power.Def.LabelCap,defaultDesc=power.Def.description,icon=power.Def.uiIcon,
                    action=()=> {
                        if(body.CurJobDef==job || !Ready(body,power)) return;
                        Find.Targeter.BeginTargeting(new TargetingParameters {canTargetLocations=true,canTargetPawns=false,
                            canTargetBuildings=false,canTargetItems=false,validator=t=>Target(body,power,new LocalTargetInfo(t.Cell))},
                            target=> {if(body.CurJobDef!=job && Target(body,power,target))
                                body.jobs.TryTakeOrderedJob(power.MakeJobFor(body,target,AbilityContext.Player),JobTag.Misc);},
                            highlightAction:_=>GenDraw.DrawRadiusRing(body.Position,14),
                            targetValidator:target=>Target(body,power,target),caster:body);
                    }};
                if(body.CurJobDef==job || !Ready(body,power)) command.Disable("TM_CommanderOperationUnavailable".Translate());
                yield return command;
            }
        }
    }
}
