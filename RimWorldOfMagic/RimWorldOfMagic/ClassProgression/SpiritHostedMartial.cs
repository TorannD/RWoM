using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritHostedMartial
    {
        internal const string Maneuver = "TM_SpiritCastManeuver", SelfAid = "TM_SpiritCastFirstAid", Pommel = "TM_SpiritCastPommelStrike";
        internal const string Headshot = "TM_SpiritCastHeadshot";
        internal const string DisablingShot = "TM_SpiritCastDisablingShot", AntiArmor = "TM_SpiritCastAntiArmor";
        private const string Aimed = "TM_SpiritCastAimedShot", Fracture = "TM_SpiritCastArmorFracture";
        private const string ShadowSlayer = "TM_SpiritCastShadowSlayer";
        internal const string Sweep = "TM_SpiritCastGladiatorSweep";
        internal const string Grapple = "TM_SpiritCastGrapple";
        internal static bool GrappleRank(TMAbilityDef def) => def != null && (def == TorannMagicDefOf.TM_Grapple ||
            def == TorannMagicDefOf.TM_Grapple_I || def == TorannMagicDefOf.TM_Grapple_II || def == TorannMagicDefOf.TM_Grapple_III);
        internal static bool HasNativeOrder(string job) => PsionicDashCast.Handles(job) || job == Sweep || job == Grapple || job == GladiatorWhirlwind.JobName || BladeMartialCast.Handles(job) || job==Verb_AnimalFriend.Order || job==Verb_PoisonTrap.Order || job==Verb_RangerSafeTrail.Order || job==Verb_RangerShot.Order;
        internal static bool IsSniperShot(string job) => job==Headshot || job==DisablingShot || job==AntiArmor || job==Aimed || job==Fracture;
        internal static bool CanFire(Pawn body, MightAbility owner, Pawn target) =>
            Verb_SniperBallisticShot.FromHost(body, owner) is Verb_SniperBallisticShot ballistic
                ? target != null && ballistic.CanHitTargetFrom(body.Position, target) : Verb_SniperShot.CanFire(body, owner, target);
        internal static bool Fire(Pawn body, MightAbility owner, Pawn target) =>
            Verb_SniperBallisticShot.FromHost(body, owner) is Verb_SniperBallisticShot ballistic
                ? ballistic.FireFromHost(target) : Verb_SniperShot.FireFromHost(body, owner, target);
        private const string ManeuverEffect = "TM_GladiatorManeuverHD";

        internal static MightAbility Find(Pawn body, Pawn soul, string job)
        {
            bool native = body == soul && HasNativeOrder(job);
            if (native)
            {
                if (SpiritRebirthActionPolicy.Marked(body)) return null;
            }
            else if (!SpiritRebirthState.TryGetPossessingSpirit(body, out var held) || held != soul ||
                !SpiritRebirthState.TryGet(soul, out var state) || state.DepletionPending) return null;
            bool sprint = job == SpiritHostedStance.Job;
            TMAbilityDef def;
            if (job == Headshot) def = TorannMagicDefOf.TM_Headshot;
            else if (ApothecaryCast.Handles(job)) def=ApothecaryCast.Definition(job);
            else if (job == Verb_ApothecaryAntitoxin.HostedOrder) def=ApothecaryMartialPath.Ability(ApothecaryMartialPath.Toxin);
            else if (job == ApothecaryKitAbility.Order) def=ApothecaryMartialPath.Ability(ApothecaryMartialPath.Kit);
            else if(Verb_CommanderAuras.Handles(job)) def=Verb_CommanderAuras.Definition(job);
            else if(Verb_CommanderOrders.Handles(job)) def=Verb_CommanderOrders.Definition(job);
            else if (Verb_CommanderCoordination.Handles(job)) def=CommanderMartialPath.Def(job==Verb_CommanderCoordination.CrewOrder?CommanderMartialPath.Crew:CommanderMartialPath.Rally);
            else if (job == JobDriver_Meditate.HostedJob) def = TorannMagicDefOf.TM_Meditate;
            else if (job == MonkDragonCast.Order) def = TorannMagicDefOf.TM_DragonStrike;
            else if (job == Verb_ChiBurst.HostedOrder) def = TorannMagicDefOf.TM_ChiBurst;
            else if (job == JobDriver_SpiritTigerStrike.Order) def = TorannMagicDefOf.TM_TigerStrike;
            else if (job == Verb_MonkPalmSeal.HostedOrder) def = DefDatabase<TMAbilityDef>.GetNamedSilentFail(MonkMartialPath.SealName);
            else if (job == JobDriver_MonkMeridian.HostedOrder) def = DefDatabase<TMAbilityDef>.GetNamedSilentFail(MonkMartialPath.MeridianName);
            else if (DeathKnightCast.Handles(job)) def = DeathKnightCast.Definition(job);
            else if (SuperSoldierCast.Handles(job)) def = SuperSoldierCast.Definition(job);
            else if (job == DisablingShot) def = TorannMagicDefOf.TM_DisablingShot;
            else if (job == AntiArmor) def = TorannMagicDefOf.TM_AntiArmor;
            else if (job == ShadowSlayer) def = TorannMagicDefOf.TM_ShadowSlayer;
            else if (job == Sweep) def = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_GladiatorSweep");
            else if (job == Grapple) def = TorannMagicDefOf.TM_Grapple;
            else if (job == GladiatorWhirlwind.JobName) def = TorannMagicDefOf.TM_Whirlwind;
            else if (job == BladeMartialCast.Spin) def = TorannMagicDefOf.TM_BladeSpin;
            else if (job == BladeMartialCast.Slash) def = TorannMagicDefOf.TM_SeismicSlash;
            else if (job == BladeMartialCast.Passage) def=DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_BladePassage");
            else if (job == BladeMartialCast.Phase) def=TorannMagicDefOf.TM_PhaseStrike;
            else if (job == Verb_RangerSafeTrail.Order && RangerMastery.HasTrail(soul)) def=DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_RangerSafeTrail");
            else if (job == Verb_RangerShot.Order) def=TorannMagicDefOf.TM_ArrowStorm;
            else if (job == Verb_PoisonTrap.Order) def=TorannMagicDefOf.TM_PoisonTrap;
            else if (job == Verb_AnimalFriend.Order) def=TorannMagicDefOf.TM_AnimalFriend ?? DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_AnimalFriend");
            else if (job == BladeFormAbility.JobName && BladeAscension.HasForm(soul)) def = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_BladeSevenSteps");
            else if (FacelessImitation.Handles(job) && FacelessMartialPath.ActivePath(body,out var artist)!=null && artist==soul)
                def=job==FacelessImitation.StudyOrder ? TorannMagicDefOf.TM_Mimic : DefDatabase<TMAbilityDef>.GetNamedSilentFail(FacelessMartialPath.Echo);
            else if (job == Projectile_Possess.Order) def=TorannMagicDefOf.TM_Possess;
            else if (Verb_PsionicBarrier.Handles(job)) def=job==Verb_PsionicBarrier.SelfOrder ? TorannMagicDefOf.TM_PsionicBarrier : TorannMagicDefOf.TM_PsionicBarrier_Projected;
            else if (job == Verb_PsionicBlast.Order) def=TorannMagicDefOf.TM_PsionicBlast;
            else if (PsionicDashCast.Handles(job)) def=job==PsionicDashCast.StormOrder ? TorannMagicDefOf.TM_PsionicStorm : TorannMagicDefOf.TM_PsionicDash;
            else if (job == Verb_Transpose.Order) def=TorannMagicDefOf.TM_Transpose;
            else if (job == Verb_Reversal.Order) def=TorannMagicDefOf.TM_Reversal;
            else if (job == PsionicCrucibleAbility.JobName) def=DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_PsionicKineticCrucible");
            else if (job == RangerHuntAbility.JobName) def = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_RangerBreakthrough");
            else if (job == SniperOpeningAbility.JobName) def = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_SniperSingleOpening");
            else if (job == Aimed || job == Fracture) def = DefDatabase<TMAbilityDef>.GetNamedSilentFail(job == Aimed ? "TM_SniperAimedShot" : "TM_SniperArmorFracture");
            else if (job == SelfAid) def = TorannMagicDefOf.TM_FirstAid;
            else if (job == Pommel) def = TorannMagicDefOf.TM_PommelStrike;
            else if (job == Maneuver && ClassIdentity.TryGetState(soul, out var path) &&
                path.Find(JobDriver_GladiatorInitiation.ClassId)?.GladiatorManeuver == true)
                def = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_GladiatorManeuver");
            else if (SpiritHostedStance.BaseAbility(job) is TMAbilityDef stance) def = stance;
            else return null;

            bool Match(PawnAbility power) => power != null && (sprint ? SpiritHostedStance.SprintFamily(power.Def as TMAbilityDef) : power.Def == def || job==DisablingShot &&
                (power.Def==TorannMagicDefOf.TM_DisablingShot_I || power.Def==TorannMagicDefOf.TM_DisablingShot_II || power.Def==TorannMagicDefOf.TM_DisablingShot_III) || job == Grapple && GrappleRank(power.Def as TMAbilityDef) ||
                job==BladeMartialCast.Phase && BladeMartialCast.PhaseRank(power.Def as TMAbilityDef) ||
                job==Verb_Transpose.Order && Verb_Transpose.Family(power.Def as TMAbilityDef) ||
                job==Verb_RangerShot.Order && Verb_RangerShot.Family(power.Def as TMAbilityDef) || Verb_CommanderOrders.Matches(job,power.Def as TMAbilityDef) ||
                job==Verb_PsionicBlast.Order && Verb_PsionicBlast.Family(power.Def as TMAbilityDef) || ApothecaryCast.Matches(job,power.Def as TMAbilityDef));
            bool Local(CompAbilityUser comp) => comp?.AbilityData.AllPowers.Any(p=>Match(p) || DeathKnightCast.Matches(job,p?.Def as TMAbilityDef) || Verb_PsionicBarrier.Handles(job) &&
                p?.Def is TMAbilityDef local && local.MainVerb?.verbClass==typeof(Verb_PsionicBarrier)) == true;
            if (def == null || !native && (Local(body.GetCompAbilityUserMagic()) || Local(body.GetCompAbilityUserMight()))) return null;
            var source = soul.GetCompAbilityUserMight();
            var powers = source?.AbilityData.Powers.OfType<MightAbility>()
                .Where(a => (Match(a) || DeathKnightCast.Matches(job,a.Def as TMAbilityDef)) && ReferenceEquals(a.AbilityUser, source)).Take(2).ToList();
            return powers?.Count == 1 ? powers[0] : null;
        }

        internal static bool Ready(Pawn body, MightAbility ability, string job)
        {
            if (ability == null || !body.Spawned || body.Dead || body.Downed || body.InMentalState || body.IsPrisoner ||
                (ability.Pawn != body && body.Faction != Faction.OfPlayer) || ability.CooldownTicksLeft > 0) return false;
            var def = ability.mightDef;
            var might = ability.MightUser;
            if (ApothecaryCast.Handles(job)) return ApothecaryCast.Funded(body,ability,job);
            if (job == Verb_ApothecaryAntitoxin.HostedOrder) return Verb_ApothecaryAntitoxin.Funded(body,ability);
            if (job == ApothecaryKitAbility.Order) return ability is ApothecaryKitAbility &&
                ApothecaryMartialPath.Owner(body)==ability.Pawn && JobDriver_ApothecaryKit.Ready(body);
            if (job == BladeFormAbility.JobName) return ability is BladeFormAbility && JobDriver_BladeSevenSteps.Ready(body,ability.Pawn);
            if (job == PsionicCrucibleAbility.JobName) return ability is PsionicCrucibleAbility && JobDriver_PsionicCrucible.ReadyFrom(body,ability.Pawn);
            if (job == RangerHuntAbility.JobName) return ability is RangerHuntAbility && JobDriver_RangerBreakthrough.Ready(body,ability.Pawn);
            if (job == SniperOpeningAbility.JobName) return ability is SniperOpeningAbility opening &&
                JobDriver_SniperSingleOpening.Ready(body, opening.Pawn) && opening.VerbFor(body).HasCompatibleWeapon;
            if (job == JobDriver_Meditate.HostedJob) return MonkMartialPath.Owner(body) == ability.Pawn && MonkMartialPath.Chi(ability.Pawn) != null;
            if (job == Verb_ChiBurst.HostedOrder) return MonkMartialPath.Owner(body) == ability.Pawn &&
                MonkMartialPath.Chi(ability.Pawn) is Hediff burstChi && burstChi.Severity >= 100 * might.ActualChiCost(def);
            if (job == JobDriver_MonkMeridian.HostedOrder) return ability is MonkMeridianAbility &&
                MonkMartialPath.Owner(body)==ability.Pawn && JobDriver_MonkMeridian.Ready(body);
            if (job == Verb_MonkPalmSeal.HostedOrder || job == JobDriver_SpiritTigerStrike.Order || job == MonkDragonCast.Order) return MonkMartialPath.Owner(body)==ability.Pawn &&
                body.equipment?.Primary==null && !body.WorkTagIsDisabled(WorkTags.Violent) &&
                MonkMartialPath.Chi(ability.Pawn) is Hediff chi && chi.Severity>=100* might.ActualChiCost(def);
            if (SpiritHostedStance.BaseAbility(job) != null) return SpiritHostedStance.Ready(body,ability);
            // Commander auras reserve stamina through native upkeep, rather than spending it on activation.
            if ((!def.consumeEnergy && !Verb_CommanderAuras.Handles(job)) || def.staminaCost <= 0 || def.chiCost != 0 || def.bloodCost != 0 || def.manaCost != 0 ||
                def.requiredNeed != null || def.requiredHediff != null || might.ExistingMightData == null ||
                might.Stamina == null || might.Stamina.CurLevel < might.ActualStaminaCost(def)) return false;
            if (job==Verb_Reversal.Order) return !body.WorkTagIsDisabled(WorkTags.Violent) &&
                TorannMagicDefOf.TM_ReversalHD!=null && !body.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ReversalHD);
            if(Verb_CommanderCoordination.Handles(job)) return CommanderMartialPath.Owner(body)==ability.Pawn &&
                CommanderMartialPath.Path(ability.Pawn) is DisciplineProgress commander &&
                (job==Verb_CommanderCoordination.CrewOrder || commander.Stage>=InitiationStage.Master);
            if(Verb_CommanderAuras.Handles(job)) return SpiritHostedCommander.AuraEmitter(ability.Pawn)==body;
            if(Verb_CommanderOrders.Handles(job)) return CommanderMartialPath.Owner(body)==ability.Pawn;
            if (DeathKnightCast.Handles(job)) return DeathKnightCast.Funded(body,ability,job);
            if (SuperSoldierCast.Handles(job)) return SuperSoldierCast.Funded(body,ability,job);
            if (Verb_PsionicBarrier.Handles(job) || FacelessImitation.Handles(job) || job==Verb_Transpose.Order || job==Projectile_Possess.Order) return true;
            if (IsSniperShot(job)) return !body.WorkTagIsDisabled(WorkTags.Violent) &&
                (Verb_SniperBallisticShot.FromHost(body, ability)?.HasCompatibleWeapon ?? (Verb_SniperShot.WeaponVerb(body) != null));
            if (job == ShadowSlayer) return Verb_ShadowSlayerCloak.CanProject(body);
            if (job == Sweep) return body.equipment?.Primary?.def.IsMeleeWeapon == true &&
                !body.WorkTagIsDisabled(WorkTags.Violent) && JobDriver_GladiatorDefense.Path(ability.Pawn)?.Attack == GladiatorAttack.Sweep;
            if (job == Grapple) return !body.WorkTagIsDisabled(WorkTags.Violent) && ClassIdentity.Component(body)?.Circle == null;
            if (job == GladiatorWhirlwind.JobName) return !body.WorkTagIsDisabled(WorkTags.Violent) && ClassIdentity.Component(body)?.Circle == null;
            if (BladeMartialCast.Handles(job)) return !body.WorkTagIsDisabled(WorkTags.Violent) && BladeMartialCast.HasWeapon(body);
            if (job==Verb_RangerSafeTrail.Order) return RangerMastery.HasTrail(ability.Pawn);
            if (job==Verb_RangerShot.Order) return !body.WorkTagIsDisabled(WorkTags.Violent) && Verb_RangerShot.HasBow(body);
            if (job==Verb_PsionicBlast.Order) return !body.WorkTagIsDisabled(WorkTags.Violent) && Verb_PsionicBlast.HasInitialEnergy(ability.Pawn);
            if (PsionicDashCast.Handles(job)) return !body.WorkTagIsDisabled(WorkTags.Violent) && ClassIdentity.Component(body)?.Circle==null &&
                PsionicDashCast.HasEnergy(ability);
            if (job==Verb_PoisonTrap.Order) return !body.WorkTagIsDisabled(WorkTags.Violent);
            if (job==Verb_AnimalFriend.Order) return !might.animalBondingDisabled;
            if (job == Pommel) return !body.WorkTagIsDisabled(WorkTags.Violent) && body.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
            if (job == SelfAid) return Verb_FirstAid.EligibleWounds(body).Any();
            var effect = DefDatabase<HediffDef>.GetNamedSilentFail(ManeuverEffect);
            return job == Maneuver && ClassIdentity.Component(body)?.Circle == null &&
                effect?.comps?.Any(c => c.compClass == typeof(HediffComp_SpiritCastOrigin)) == true &&
                !body.health.hediffSet.HasHediff(effect);
        }

        internal static bool CanStrike(Pawn body, Pawn target, MightAbility ability) =>
            target?.Spawned == true && !target.Dead && target != body && target.Map == body.Map && target.HostileTo(body) &&
            body.Position.InHorDistOf(target.Position, ability.Def.MainVerb.range) &&
            body.CanReachImmediate(target, PathEndMode.Touch);

        internal static bool CanMelee(Pawn body, Pawn target, MightAbility ability,string job)
        {
            if (target?.Spawned != true || target.Map != body.Map) return false;
            if (job == Verb_MonkPalmSeal.HostedOrder) return Verb_MonkPalmSeal.Target(body,target);
            if (job == JobDriver_SpiritTigerStrike.Order) return !target.Downed && CanStrike(body,target,ability);
            if (job == Grapple) return Verb_Grapple.ForHost(body,ability).CanHitTargetFrom(body.Position,target);
            if (job == Sweep) return Verb_GladiatorSweep.ForHost(body,ability).CanHitTargetFrom(body.Position,target);
            return CanStrike(body,target,ability);
        }
        internal static bool UsesCombatTarget(string job) => job == Sweep || job == Grapple || job == Pommel || job == Verb_MonkPalmSeal.HostedOrder || job == JobDriver_SpiritTigerStrike.Order;

        internal static bool Strike(Pawn body, Pawn soul, Pawn target,string job = Pommel)
        {
            var ability = Find(body, soul, job);
            if (!Ready(body, ability, job) || !CanMelee(body, target, ability,job)) return false;
            if (job == Verb_MonkPalmSeal.HostedOrder)
            {
                if (!Verb_MonkPalmSeal.Strike(body,target)) return false;
            }
            else if (job == Sweep)
            {
                if (!Verb_GladiatorSweep.ForHost(body,ability).Strike(target)) return false;
            }
            else if (job == Grapple)
            {
                if (!Verb_Grapple.ForHost(body,ability).FireFromHost(target)) return false;
            }
            else Verb_PommelStrike.Strike(body, target, ability.MightUser);
            ability.PostAbilityAttempt();
            return true;
        }

        internal static bool Apply(Pawn body, Pawn soul, string job)
        {
            var ability = Find(body, soul, job);
            if (job == Pommel || !Ready(body, ability, job)) return false;
            if (SpiritHostedStance.BaseAbility(job) != null) return SpiritHostedStance.Toggle(body,ability);
            if (job == ShadowSlayer)
            {
                if (!Verb_ShadowSlayerCloak.Activate(body,soul)) return false;
            }
            else if (job == Verb_Reversal.Order)
            {
                if (!Verb_Reversal.Apply(body,soul)) return false;
            }
            else if (job == SelfAid)
            {
                var skills = ability.MightUser.MightData.MightPowerSkill_FirstAid;
                int Rank(string label) => skills.FirstOrDefault(s => s.label == label)?.level ?? 0;
                if (!Verb_FirstAid.Tend(body, Rank("TM_FirstAid_pwr"), Rank("TM_FirstAid_ver"))) return false;
            }
            else
            {
                var effect = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed(ManeuverEffect), body);
                effect.TryGetComp<HediffComp_SpiritCastOrigin>().Bind(soul);
                effect.Severity = 1f;
                body.health.AddHediff(effect);
                if (!body.health.hediffSet.hediffs.Contains(effect)) return false;
            }
            ability.PostAbilityAttempt();
            return true;
        }

        private static void FindTarget(Pawn body, Pawn soul, JobDef job, System.Func<Pawn, bool> valid)
        {
            Verse.Find.Targeter.BeginTargeting(new TargetingParameters { canTargetPawns = true, canTargetBuildings = false,
                canTargetItems = false, validator = t => valid(t.Thing as Pawn) }, target => {
                if (body.CurJobDef != job && valid(target.Pawn))
                {
                    Job order;
                    if (job.defName == SniperOpeningAbility.JobName)
                    {
                        if (!(Find(body,soul,job.defName) is SniperOpeningAbility opening)) return;
                        order = opening.MakeJobFor(body,target);
                    }
                    else if (job.defName==RangerHuntAbility.JobName)
                    {
                        if (!(Find(body,soul,job.defName) is RangerHuntAbility hunt)) return;
                        order=hunt.MakeJobFor(body,target);
                    }
                    else if (job.defName==ApothecaryKitAbility.Order)
                    {
                        if (!(Find(body,soul,job.defName) is ApothecaryKitAbility kit)) return;
                        order=kit.MakeJobFor(body,target);
                    }
                    else if (job.defName==Verb_RangerShot.Order) order=Verb_RangerShot.MakeOrder(body,soul,target);
                    else order = JobMaker.MakeJob(job, soul, target);
                    body.jobs.TryTakeOrderedJob(order, JobTag.Misc);
                }
            }, highlightAction: target => {
                if (IsSniperShot(job.defName) && Find(body,soul,job.defName) is MightAbility shot)
                    GenDraw.DrawRadiusRing(body.Position,Verb_SniperBallisticShot.FromHost(body,shot)?.EffectiveRange ?? shot.mightDef.MainVerb.range);
                else if(job.defName==RangerHuntAbility.JobName && Find(body,soul,job.defName) is RangerHuntAbility hunt)
                    GenDraw.DrawRadiusRing(body.Position,hunt.VerbFor(body).EffectiveRange);
                else if(job.defName == SniperOpeningAbility.JobName && Find(body,soul,job.defName) is SniperOpeningAbility opening)
                    GenDraw.DrawRadiusRing(body.Position, opening.VerbFor(body).EffectiveRange);
                else if(job.defName == Grapple && Find(body,soul,job.defName) is MightAbility hook)
                    GenDraw.DrawRadiusRing(body.Position,hook.mightDef.MainVerb.range);
                else if(job.defName==Projectile_Possess.Order && Find(body,soul,job.defName) is MightAbility control)
                    GenDraw.DrawRadiusRing(body.Position,control.mightDef.MainVerb.range);
                else if(job.defName==Verb_Transpose.Order && Find(body,soul,job.defName) is MightAbility transpose)
                    GenDraw.DrawRadiusRing(body.Position,transpose.mightDef.MainVerb.range);
                else if(FacelessImitation.Handles(job.defName) && Find(body,soul,job.defName) is MightAbility imitation)
                    GenDraw.DrawRadiusRing(body.Position,imitation.mightDef.MainVerb.range);
                else if(job.defName==Verb_RangerShot.Order && Find(body,soul,job.defName) is MightAbility volley)
                    GenDraw.DrawRadiusRing(body.Position,volley.mightDef.MainVerb.range);
                else if(job.defName==Verb_AnimalFriend.Order && Find(body,soul,job.defName) is MightAbility bond)
                    GenDraw.DrawRadiusRing(body.Position,bond.mightDef.MainVerb.range);
            }, targetValidator: target => valid(target.Pawn), caster: body);
        }

        internal static IEnumerable<Gizmo> Commands(IEnumerable<Gizmo> existing, Pawn body)
        {
            foreach (var command in existing) yield return command;
            if (!SpiritRebirthState.TryGetPossessingSpirit(body, out var soul)) yield break;
            foreach(var command in SpiritHostedCommander.Commands(body,soul)) yield return command;
            if (HediffComp_Psionic.CanProject(body,soul))
            {
                var source=soul.GetCompAbilityUserMight();
                if (source!=null)
                {
                    yield return source.MakePsionicToggle(false,()=>{
                        if (HediffComp_Psionic.CanProject(body,soul)) source.usePsionicAugmentationToggle=!source.usePsionicAugmentationToggle;
                    });
                    yield return source.MakePsionicToggle(true,()=>{
                        if (HediffComp_Psionic.CanProject(body,soul)) source.usePsionicMindAttackToggle=!source.usePsionicMindAttackToggle;
                    });
                }
            }
            if (RangerCareer.Path(soul)?.Stage==InitiationStage.Foundation && RangerCareer.CareBody(soul)==body &&
                RangerCareer.Companion(soul) is Pawn pet && RangerCareer.Eligible(soul,pet))
                yield return new Command_Action {defaultLabel="TM_RangerCareOrder".Translate(pet.LabelShort),
                    icon=TorannMagicDefOf.TM_AnimalFriend.uiIcon,action=()=>RangerCareer.OrderCare(soul,pet)};
            if (JobDriver_BladeWeaponService.CanProject(body,soul))
                yield return new Command_Action {defaultLabel="TM_BladeServiceOrder".Translate(),icon=TexButton.TMTex_Cleave,
                    action=()=> { if (JobDriver_BladeWeaponService.CanProject(body,soul)) JobDriver_BladeWeaponService.TargetFrom(body,soul); }};
            if (JobDriver_GladiatorDefense.Path(soul) is DisciplineProgress gladiator)
            {
                foreach (bool training in new[] {false,true})
                {
                    if (!JobDriver_GladiatorDefense.CanOfferProjected(body,soul,training)) continue;
                    string key = !training ? "TM_GladiatorUseCover" : gladiator.Stage==InitiationStage.Master ? "TM_GladiatorEvacTrial" :
                        gladiator.Stage==InitiationStage.Adept ? "TM_GladiatorHoldDrill" : "TM_GladiatorReliefDrill";
                    var cmd=new Command_Action {defaultLabel=key.Translate(), icon=TexButton.TMTex_Cleave,
                        action=()=> { if(JobDriver_GladiatorDefense.CanWorkFrom(body,soul,training))
                            JobDriver_GladiatorDefense.TargetFrom(body,soul,training); }};
                    if (!JobDriver_GladiatorDefense.CanWorkFrom(body,soul,training)) cmd.Disable("TM_SpiritRebirthCastUnavailable".Translate());
                    yield return cmd;
                }
            }
            if (JobDriver_LastCircle.CanProject(body,soul))
            {
                var job=DefDatabase<JobDef>.GetNamed(JobDriver_LastCircle.HostedJobName);
                var cmd=new Command_Action { defaultLabel="TM_GladiatorCastCircle".Translate(), icon=TexButton.TMTex_Cleave,
                    action=()=> {
                        if(body.CurJobDef!=job && JobDriver_LastCircle.ReadyFrom(body,soul))
                            body.jobs.TryTakeOrderedJob(JobMaker.MakeJob(job,body,soul),JobTag.Misc);
                    }};
                if(body.CurJobDef==job || !JobDriver_LastCircle.ReadyFrom(body,soul))cmd.Disable("TM_SpiritRebirthCastUnavailable".Translate());
                yield return cmd;
            }
            if (Find(body,soul,PsionicCrucibleAbility.JobName) is PsionicCrucibleAbility crucible)
            {
                var job=DefDatabase<JobDef>.GetNamed(PsionicCrucibleAbility.JobName);
                var cmd=new Command_Action{defaultLabel=crucible.Def.LabelCap,defaultDesc=crucible.Def.description,icon=crucible.Def.uiIcon,
                    action=()=>{if(body.CurJobDef!=job && Ready(body,crucible,PsionicCrucibleAbility.JobName))crucible.ChooseFrom(body);}};
                if(body.CurJobDef==job || !Ready(body,crucible,PsionicCrucibleAbility.JobName))cmd.Disable("TM_SpiritRebirthCastUnavailable".Translate());
                yield return cmd;
            }
            var cleave = GladiatorMastery.AutomaticSource(body,body.GetCompAbilityUserMight());
            if (cleave?.Pawn == soul && body.Faction == Faction.OfPlayer)
                yield return cleave.MakeCleaveCommand(() => {
                    if (body.Spawned && !body.Dead && body.Faction == Faction.OfPlayer &&
                        GladiatorMastery.AutomaticSource(body,body.GetCompAbilityUserMight()) == cleave)
                        cleave.useCleaveToggle = !cleave.useCleaveToggle;
                });
            foreach (var name in new[] { Maneuver, SelfAid, Pommel, Sweep, Grapple, GladiatorWhirlwind.JobName, BladeMartialCast.Spin, BladeMartialCast.Slash, BladeMartialCast.Passage, BladeMartialCast.Phase, BladeFormAbility.JobName, Verb_AnimalFriend.Order, Verb_PoisonTrap.Order, Verb_RangerSafeTrail.Order, Verb_RangerShot.Order, RangerHuntAbility.JobName, FacelessImitation.StudyOrder, FacelessImitation.EchoOrder, Verb_Reversal.Order, Verb_Transpose.Order, Projectile_Possess.Order, Verb_PsionicBarrier.SelfOrder, Verb_PsionicBarrier.FieldOrder, Verb_PsionicBlast.Order, PsionicDashCast.JobName, PsionicDashCast.StormOrder, Headshot, DisablingShot, AntiArmor, Aimed, Fracture, ShadowSlayer, SniperOpeningAbility.JobName, SpiritHostedStance.Job, SpiritHostedStance.StrongBack, SpiritHostedStance.HeavyBlow,
                SpiritHostedStance.ThickSkin, SpiritHostedStance.FightersFocus, SpiritHostedStance.GearRepair, SpiritHostedStance.InnerHealing, JobDriver_Meditate.HostedJob, Verb_MonkPalmSeal.HostedOrder, JobDriver_MonkMeridian.HostedOrder, JobDriver_SpiritTigerStrike.Order, MonkDragonCast.Order, Verb_ChiBurst.HostedOrder, DeathKnightCast.Spite, DeathKnightCast.Fear, DeathKnightCast.Blade, Verb_CommanderCoordination.CrewOrder, Verb_CommanderCoordination.RallyOrder, Verb_CommanderOrders.MoveOrder, Verb_CommanderOrders.AlertOrder, Verb_CommanderOrders.HoldOrder, Verb_CommanderAuras.ProvisionerOrder, Verb_CommanderAuras.TaskMasterOrder, Verb_CommanderAuras.CommanderOrder, SuperSoldierCast.Whip, SuperSoldierCast.Grenade, SuperSoldierCast.Charge, SuperSoldierCast.Dressing, SuperSoldierCast.Buckshot, SuperSoldierCast.Suppressing, SuperSoldierCast.Mortar, Verb_ApothecaryAntitoxin.HostedOrder, ApothecaryKitAbility.Order, ApothecaryCast.Balm, ApothecaryCast.Elixir, ApothecaryCast.Clarity, ApothecaryCast.Bliss, ApothecaryCast.Poison, ApothecaryCast.Fire })
            {
                var owner = Find(body, soul, name);
                var job = DefDatabase<JobDef>.GetNamedSilentFail(name);
                if (owner == null || job == null) continue;
                var cmd = new Command_Action { defaultLabel = owner.Def.LabelCap, defaultDesc = owner.Def.description,
                    icon = owner.Def.uiIcon, action = () => {
                        if (body.CurJobDef == job || !Ready(body, Find(body, soul, name), name)) return;
                        if (name == GladiatorWhirlwind.JobName) GladiatorWhirlwind.ChooseDestination(body,soul,owner);
                        else if (ApothecaryCast.Handles(name)) ApothecaryCast.Choose(body,soul,owner,name);
                        else if (name == Verb_ApothecaryAntitoxin.HostedOrder) FindTarget(body,soul,job,target =>
                            Ready(body,Find(body,soul,name),name) && Verb_ApothecaryAntitoxin.For(body,owner).CanHitTargetFrom(body.Position,target));
                        else if (name == ApothecaryKitAbility.Order) FindTarget(body,soul,job,target =>
                            Ready(body,Find(body,soul,name),name) && JobDriver_ApothecaryKit.Candidate(body,target));
                        else if (Verb_CommanderCoordination.Handles(name)) Verb_CommanderCoordination.Choose(body,owner,name);
                        else if (name == JobDriver_Meditate.HostedJob || name == JobDriver_MonkMeridian.HostedOrder) body.jobs.TryTakeOrderedJob(JobMaker.MakeJob(job, body.Position, soul), JobTag.Misc);
                        else if (DeathKnightCast.Handles(name)) DeathKnightCast.Choose(body,soul,owner,name);
                        else if (SuperSoldierCast.Handles(name)) SuperSoldierCast.Choose(body,soul,owner,name);
                        else if (name == MonkDragonCast.Order) MonkDragonCast.Choose(body,soul,owner);
                        else if (name == Verb_ChiBurst.HostedOrder) Verb_ChiBurst.Choose(body,soul,owner);
                        else if (PsionicDashCast.Handles(name)) PsionicDashCast.ChooseDestination(body,soul,owner);
                        else if (name==BladeMartialCast.Slash || name==BladeMartialCast.Passage || name==BladeMartialCast.Phase) BladeMartialCast.ChooseTarget(body,soul,owner,name);
                        else if (name == BladeFormAbility.JobName && owner is BladeFormAbility form) form.ChooseRoute(body);
                        else if (name==Verb_RangerSafeTrail.Order) Verb_RangerSafeTrail.Choose(body,owner);
                        else if (Verb_PsionicBarrier.Handles(name)) Verb_PsionicBarrier.Choose(body,soul,owner,name);
                        else if (name==Projectile_Possess.Order) FindTarget(body,soul,job,t=>
                            Ready(body,Find(body,soul,name),name) && Projectile_Possess.ValidTarget(body,owner,t));
                        else if (name==Verb_Transpose.Order) FindTarget(body,soul,job,t => t!=null &&
                            Ready(body,Find(body,soul,name),name) && Verb_Transpose.For(body,owner).CanHitTargetFrom(body.Position,t));
                        else if (FacelessImitation.Handles(name)) FindTarget(body,soul,job,t =>
                            Ready(body,Find(body,soul,name),name) && FacelessImitation.ValidTarget(body,owner,name,t));
                        else if (name==Verb_RangerShot.Order) FindTarget(body,soul,job,t => Ready(body,Find(body,soul,name),name) && Verb_RangerShot.For(body,owner).CanVolley(t));
                        else if (name==Verb_PsionicBlast.Order) Verb_PsionicBlast.Choose(body,soul,owner);
                        else if (name==Verb_PoisonTrap.Order) Verb_PoisonTrap.Choose(body,owner);
                        else if (name==Verb_AnimalFriend.Order) FindTarget(body,soul,job,target => Ready(body,Find(body,soul,name),name) && Verb_AnimalFriend.ValidTarget(body,target,body.Position,owner.mightDef.MainVerb.range));
                        else if (name == RangerHuntAbility.JobName && owner is RangerHuntAbility hunt)
                            FindTarget(body,soul,job,target => target!=null && Ready(body,Find(body,soul,name),name) &&
                                hunt.VerbFor(body).CanHitTargetFrom(body.Position,target));
                        else if (name == SniperOpeningAbility.JobName && owner is SniperOpeningAbility opening)
                            FindTarget(body,soul,job,target => target!=null && Ready(body,Find(body,soul,name),name) &&
                                opening.VerbFor(body).CanHitTargetFrom(body.Position,target));
                        else if (IsSniperShot(name))
                        {
                            bool Valid(Pawn target) => Ready(body, Find(body,soul,name),name) && CanFire(body,owner,target);
                            FindTarget(body,soul,job,Valid);
                        }
                        else if (UsesCombatTarget(name))
                        {
                            bool Valid(Pawn target) => Ready(body, Find(body, soul, name), name) && CanMelee(body, target, owner,name);
                            FindTarget(body, soul, job, Valid);
                        }
                        else
                            body.jobs.TryTakeOrderedJob(JobMaker.MakeJob(job, soul), JobTag.Misc);
                    } };
                if (body.CurJobDef == job || !Ready(body, owner, name))
                    cmd.Disable("TM_SpiritRebirthCastUnavailable".Translate());
                yield return cmd;
            }
        }
    }

    public sealed class JobDriver_SpiritHostedMartial : JobDriver
    {
        private bool attempted;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref attempted, "attempted");
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var soul = job.targetA.Thing as Pawn;
            string name = job.def.defName;
            if (!SpiritHostedMartial.HasNativeOrder(name) || pawn != soul)
                this.FailOn(() => !SpiritRebirthState.TryGetPossessingSpirit(pawn, out var current) || current != soul);
            var warmup = Toils_General.Wait(1);
            warmup.initAction = () => {
                var owner = SpiritHostedMartial.Find(pawn, soul, name);
                if (!SpiritHostedMartial.Ready(pawn, owner, name) ||
                    ApothecaryCast.Handles(name) && !ApothecaryCast.Valid(pawn,owner,job.targetB) ||
                    name == Verb_ApothecaryAntitoxin.HostedOrder && !Verb_ApothecaryAntitoxin.For(pawn,owner).CanHitTargetFrom(pawn.Position,job.targetB) ||
                    Verb_CommanderCoordination.Handles(name) && !Verb_CommanderCoordination.For(pawn,owner).CanHitTargetFrom(pawn.Position,job.targetB) ||
                    DeathKnightCast.Handles(name) && !DeathKnightCast.Valid(pawn,owner,name,job.targetB) ||
                    SuperSoldierCast.Handles(name) && !SuperSoldierCast.Valid(pawn,owner,name,job.targetB) ||
                    SpiritHostedMartial.IsSniperShot(name) && !SpiritHostedMartial.CanFire(pawn,owner,job.targetB.Pawn) ||
                    name == GladiatorWhirlwind.JobName && !GladiatorWhirlwind.CanLaunch(pawn,owner,job.targetB.Cell) ||
                    name == MonkDragonCast.Order && !MonkDragonCast.CanLaunch(pawn,owner,job.targetB) ||
                    name == Verb_ChiBurst.HostedOrder && !Verb_ChiBurst.Valid(pawn,owner,job.targetB) ||
                    PsionicDashCast.Handles(name) && !PsionicDashCast.CanLaunch(pawn,owner,job.targetB.Cell) ||
                    BladeMartialCast.Handles(name) && !BladeMartialCast.Valid(pawn,owner,name,job.targetB) ||
                    Verb_PsionicBarrier.Handles(name) && !Verb_PsionicBarrier.Valid(pawn,owner,job.targetB) ||
                    name==Projectile_Possess.Order && !Projectile_Possess.ValidTarget(pawn,owner,job.targetB.Pawn) ||
                    name==Verb_Transpose.Order && !Verb_Transpose.For(pawn,owner).CanHitTargetFrom(pawn.Position,job.targetB) ||
                    FacelessImitation.Handles(name) && !FacelessImitation.ValidTarget(pawn,owner,name,job.targetB.Pawn) ||
                    name==Verb_RangerSafeTrail.Order && !Verb_RangerSafeTrail.For(pawn,owner).CanHitTargetFrom(pawn.Position,job.targetB) ||
                    name==Verb_AnimalFriend.Order && !Verb_AnimalFriend.ValidTarget(pawn,job.targetB.Pawn,pawn.Position,owner.mightDef.MainVerb.range) ||
                    SpiritHostedMartial.UsesCombatTarget(name) &&
                    !SpiritHostedMartial.CanMelee(pawn, job.targetB.Pawn, owner,name)) { EndJobWith(JobCondition.Incompletable); return; }
                ticksLeftThisToil = UnityEngine.Mathf.Max(1, GenTicks.SecondsToTicks(
                    owner.Def.MainVerb.warmupTime * Compatibility.AbilityTiming.WarmupMultiplier));
            };
            yield return warmup;
            yield return Toils_General.Do(() => {
                if (attempted || pawn.CurJob != job || pawn.jobs.curDriver != this) { EndJobWith(JobCondition.Incompletable); return; }
                attempted = true;
                if(ApothecaryCast.Handles(name))
                {
                    EndJobWith(ApothecaryCast.Execute(pawn,soul,name,job.targetB) ? JobCondition.Succeeded : JobCondition.Incompletable);
                    return;
                }
                if(name == Verb_ApothecaryAntitoxin.HostedOrder)
                {
                    var power=SpiritHostedMartial.Find(pawn,soul,name);
                    // The clinical effect owns the herb payment and cooldown; never pay a second time here.
                    if(!SpiritHostedMartial.Ready(pawn,power,name) || !Verb_ApothecaryAntitoxin.For(pawn,power).CastFromHost(job.targetB))
                        EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if(Verb_CommanderAuras.Handles(name))
                {
                    var power=SpiritHostedMartial.Find(pawn,soul,name);
                    if(SpiritHostedMartial.Ready(pawn,power,name) && Verb_CommanderAuras.CastFromHost(pawn,power)) power.PostAbilityAttempt();
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }
                if(Verb_CommanderOrders.Handles(name))
                {
                    var power=SpiritHostedMartial.Find(pawn,soul,name);
                    if(SpiritHostedMartial.Ready(pawn,power,name) && Verb_CommanderOrders.CastFromHost(pawn,power)) power.PostAbilityAttempt();
                    else EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if(Verb_CommanderCoordination.Handles(name))
                {
                    var power=SpiritHostedMartial.Find(pawn,soul,name);
                    if(SpiritHostedMartial.Ready(pawn,power,name) && Verb_CommanderCoordination.For(pawn,power).Issue(job.targetB))
                        power.PostAbilityAttempt();
                    else EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (name == Verb_ChiBurst.HostedOrder)
                {
                    var power = SpiritHostedMartial.Find(pawn,soul,name);
                    if (Verb_ChiBurst.Valid(pawn,power,job.targetB) && Verb_ChiBurst.For(pawn,power).CastFromHost(job.targetB))
                        power.PostAbilityAttempt();
                    else EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (DeathKnightCast.Handles(name))
                {
                    if (!DeathKnightCast.Execute(pawn,soul,name,job.targetB)) EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (SuperSoldierCast.Handles(name))
                {
                    if (!SuperSoldierCast.Execute(pawn,soul,name,job.targetB)) EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (name == MonkDragonCast.Order)
                {
                    if (!MonkDragonCast.Launch(pawn,SpiritHostedMartial.Find(pawn,soul,name),job.targetB))
                        EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if(PsionicDashCast.Handles(name))
                {
                    if(!PsionicDashCast.Launch(pawn,SpiritHostedMartial.Find(pawn,soul,name),job.targetB.Cell))
                        EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (Verb_PsionicBarrier.Handles(name))
                {
                    var power=SpiritHostedMartial.Find(pawn,soul,name);
                    if (!SpiritHostedMartial.Ready(pawn,power,name) || !Verb_PsionicBarrier.Begin(pawn,power,job.targetB))
                        EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (name==Projectile_Possess.Order)
                {
                    var power=SpiritHostedMartial.Find(pawn,soul,name);
                    if (SpiritHostedMartial.Ready(pawn,power,name) && Projectile_Possess.LaunchFrom(pawn,power,job.targetB.Pawn))
                        power.PostAbilityAttempt();
                    else EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (name==Verb_Transpose.Order)
                {
                    var power=SpiritHostedMartial.Find(pawn,soul,name);
                    if (SpiritHostedMartial.Ready(pawn,power,name) && Verb_Transpose.For(pawn,power).Exchange(soul,job.targetB.Pawn))
                        power.PostAbilityAttempt();
                    else EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (FacelessImitation.Handles(name))
                {
                    var power=SpiritHostedMartial.Find(pawn,soul,name);
                    var target=job.targetB.Pawn;
                    if (SpiritHostedMartial.Ready(pawn,power,name) && FacelessImitation.ValidTarget(pawn,power,name,target) &&
                        (name==FacelessImitation.StudyOrder ? FacelessImitation.LearnFrom(pawn,soul,target) :
                            FacelessImitation.PerformFrom(pawn,soul,target))) power.PostAbilityAttempt();
                    else EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (name==Verb_RangerSafeTrail.Order)
                {
                    var power=SpiritHostedMartial.Find(pawn,soul,name);
                    if (SpiritHostedMartial.Ready(pawn,power,name) && Verb_RangerSafeTrail.For(pawn,power).MarkTrail(job.targetB)) power.PostAbilityAttempt();
                    else EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (name==Verb_AnimalFriend.Order)
                {
                    var power=SpiritHostedMartial.Find(pawn,soul,name);
                    if (SpiritHostedMartial.Ready(pawn,power,name) && Verb_AnimalFriend.TryBond(pawn,soul,job.targetB.Pawn,power.mightDef.MainVerb.range)) power.PostAbilityAttempt();
                    else EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (BladeMartialCast.Handles(name))
                {
                    if(!BladeMartialCast.Execute(pawn,SpiritHostedMartial.Find(pawn,soul,name),name,job.targetB))
                        EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (name == GladiatorWhirlwind.JobName)
                {
                    var power = SpiritHostedMartial.Find(pawn,soul,name);
                    if (!GladiatorWhirlwind.Launch(pawn,power,job.targetB.Cell)) EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (SpiritHostedMartial.IsSniperShot(name))
                {
                    var owner = SpiritHostedMartial.Find(pawn,soul,name);
                    if (!SpiritHostedMartial.Ready(pawn,owner,name) || !SpiritHostedMartial.Fire(pawn,owner,job.targetB.Pawn))
                        EndJobWith(JobCondition.Incompletable);
                    return;
                }
                bool success = SpiritHostedMartial.UsesCombatTarget(name) ? SpiritHostedMartial.Strike(pawn, soul, job.targetB.Pawn,name)
                    : SpiritHostedMartial.Apply(pawn, soul, name);
                if (!success) EndJobWith(JobCondition.Incompletable);
            });
        }
    }
}
