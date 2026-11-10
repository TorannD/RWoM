using System;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.FleshDevelopment
{
    public static class FleshActions
    {
        public static int Now => Current.Game == null ? 0 : Find.TickManager?.TicksGame ?? 0;
        public static int CooldownTicks(FleshSkill skill, int rank)
        {
            switch (skill)
            {
                case FleshSkill.Devour: return 300;
                case FleshSkill.Restore: return 60000 - 6000 * (Math.Max(1, Math.Min(3, rank)) - 1);
                case FleshSkill.Empower: case FleshSkill.Harden: return 18000;
                case FleshSkill.Feast: return 60000;
                default: return 0;
            }
        }
        public static float DevourDamage(int rank) => 2f + 2f * Math.Max(1, Math.Min(3, rank));
        public static string Rejection(Pawn pawn, FleshSkill skill)
        {
            var p = FleshRepository.TryGet(pawn);
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.Faction?.IsPlayer != true || !FleshRepository.IsRaisedUndead(pawn) || p == null || pawn.InMentalState) return "TM_FleshRejectUnavailable";
            if (p.GetRank(skill) <= 0 || skill > FleshSkill.Feast) return "TM_FleshRejectUnlearned";
            if ((skill == FleshSkill.Empower && p.EmpowerUntil > Now) || (skill == FleshSkill.Harden && p.HardenUntil > Now) || (skill == FleshSkill.Feast && (p.FeastUntil > Now || p.FeastRage))) return "TM_FleshRejectActive";
            if (p.GetReadyTick(skill) > Now) return "TM_FleshRejectCooldown";
            if (skill == FleshSkill.Restore && p.Experience <= 0f) return "TM_FleshRejectNoExperience";
            return null;
        }
        public static bool CanActivate(Pawn pawn, FleshSkill skill) => Rejection(pawn, skill) == null;
        public static void Upgrade(Pawn pawn, FleshSkill skill)
        {
            if (pawn != null && !pawn.Dead && pawn.Faction?.IsPlayer == true && FleshRepository.IsRaisedUndead(pawn) && FleshRepository.TryGet(pawn)?.TryUpgrade(skill) == true) FleshHealth.Sync(pawn);
        }
        public static void DebugLevelUp(Pawn pawn)
        {
            // MP replays debug commands under the issuing player's god-mode context.
            if (!DebugSettings.godMode) return;
            if (pawn == null || pawn.Dead || pawn.Faction?.IsPlayer != true || !FleshRepository.IsRaisedUndead(pawn)) return;
            var data = FleshRepository.TryGet(pawn);
            if (data == null || data.Level == int.MaxValue || data.SkillPoints == int.MaxValue) return;
            data.Level++;
            data.SkillPoints++;
            FleshHealth.Sync(pawn);
        }
        public static bool TryActivate(Pawn pawn, FleshSkill skill)
        {
            if (skill == FleshSkill.Devour || !CanActivate(pawn, skill)) return false;
            Tick(pawn);
            if (!CanActivate(pawn, skill)) return false;
            var p = FleshRepository.TryGet(pawn);
            switch (skill)
            {
                case FleshSkill.Restore:
                    RestoreNaturalFlesh(pawn);
                    p.Experience = 0f;
                    break;
                case FleshSkill.Empower: p.EmpowerUntil = Now + 18000; p.AddThirst(10f); break;
                case FleshSkill.Harden: p.HardenUntil = Now + 18000; p.AddThirst(15f); break;
                case FleshSkill.Feast: p.FeastUntil = Now + 3600; p.Thirst = p.MaxThirst * .5f; break;
                default: return false;
            }
            p.SetReadyTick(skill, Now + CooldownTicks(skill, p.GetRank(skill)));
            UpdateRage(pawn, p);
            FleshHealth.Sync(pawn);
            FleshEffects.Activated(pawn, skill);
            return true;
        }

        // MeatAmount already includes natural remaining coverage, body size, injury and difficulty.
        // Do not multiply by missing-part coverage again and do not invoke ButcherProducts (side effects).
        public static float Freshness(float progress, float rotStart, float desiccated)
        {
            if (progress <= 0f) return 1f;
            if (desiccated <= rotStart || progress >= desiccated) return 0f;
            if (progress < rotStart && rotStart > 0f) return 1f - .5f * progress / rotStart;
            return .5f * (desiccated - progress) / (desiccated - rotStart);
        }
        public static float MeatReward(float remainingMeat, float freshness) => Math.Max(0f, remainingMeat) * Math.Max(0f, Math.Min(1f, freshness));
        public static float BiteMeat(float remainingMeat, float partCoverage, float totalCoverage, float actualDamage, float partHealth)
        {
            if (totalCoverage <= 0f || partHealth <= 0f) return 0f;
            return Math.Max(0f, remainingMeat) * Math.Max(0f, Math.Min(1f, partCoverage / totalCoverage)) * Math.Max(0f, Math.Min(1f, actualDamage / partHealth));
        }
        public static float CorpseMeat(Corpse corpse)
        {
            if (corpse == null || corpse.Destroyed || !Organic(corpse.InnerPawn)) return 0f;
            var rot = corpse.TryGetComp<CompRottable>();
            float freshness = rot == null ? 1f : Freshness(rot.RotProgress, rot.PropsRot.TicksToRotStart, rot.PropsRot.TicksToDessicated);
            return MeatReward(corpse.InnerPawn.GetStatValue(StatDefOf.MeatAmount), freshness);
        }
        public static bool Organic(Pawn pawn) => pawn?.RaceProps != null && pawn.RaceProps.IsFlesh && pawn.RaceProps.meatDef != null;
        public static bool ValidTarget(Pawn actor, Thing target)
        {
            if (actor == null || !actor.Spawned || target == null || target.Destroyed || !target.Spawned || target.Map != actor.Map || target == actor) return false;
            if (target is Corpse corpse) return CorpseMeat(corpse) > 0f;
            // This is an explicitly ordered melee action, so neutral living targets are valid too.
            return target is Pawn living && !living.Dead && Organic(living);
        }
        public static bool TryDevour(Pawn pawn, Thing target)
        {
            if (!CanActivate(pawn, FleshSkill.Devour) || !ValidTarget(pawn, target) || !pawn.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly)) return false;
            var def = DefDatabase<JobDef>.GetNamedSilentFail("TM_FleshDevour");
            if (def == null) return false;
            return pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(def, target), JobTag.Misc);
        }
        public static bool CompleteDevour(Pawn pawn, Thing target)
        {
            if (!CanActivate(pawn, FleshSkill.Devour) || !ValidTarget(pawn, target) || !pawn.CanReachImmediate(target, PathEndMode.Touch)) return false;
            var p = FleshRepository.TryGet(pawn);
            var impactMap = target.Map;
            var impactPosition = target.DrawPos;
            bool consumed = false;
            if (target is Corpse corpse)
            {
                float meat = CorpseMeat(corpse);
                if (meat <= 0f) return false;
                Pawn body = corpse.InnerPawn;
                body.inventory?.DropAllNearPawn(corpse.Position, false, true);
                body.equipment?.DropAllEquipment(corpse.Position, false);
                body.apparel?.DropAll(corpse.Position, false);
                // Abandon consumption if an item could not be placed: never destroy someone's gear.
                if ((body.inventory?.innerContainer.Count ?? 0) > 0 || (body.equipment?.AllEquipmentListForReading.Count ?? 0) > 0 || (body.apparel?.WornApparel.Count ?? 0) > 0) return false;
                corpse.Destroy(DestroyMode.Vanish);
                if (!corpse.Destroyed) return false;
                p.AddExperience(meat);
                p.AddThirst(-meat / 5f);
                consumed = true;
            }
            else if (target is Pawn living)
            {
                var result = living.TakeDamage(new DamageInfo(DamageDefOf.Bite, DevourDamage(p.GetRank(FleshSkill.Devour)), instigator: pawn));
                FleshBiteAttacks.ApplyConditions(pawn, living, result, FleshBiteAttacks.Feasting(pawn));
                p.AddThirst(-result.totalDamageDealt / 5f);
                consumed = result.totalDamageDealt > 0f;
            }
            p.SetReadyTick(FleshSkill.Devour, Now + 300);
            UpdateRage(pawn, p);
            FleshHealth.Sync(pawn);
            if (consumed) FleshEffects.Consumed(pawn, impactMap, impactPosition);
            return true;
        }

        public static void RestoreNaturalFlesh(Pawn pawn)
        {
            // Removing only injuries/missing-part markers preserves implants even beneath a restored parent.
            // RestorePart recursively removes every hediff on that part, including implants, so is unsuitable.
            var set = pawn.health.hediffSet;
            foreach (var h in set.hediffs.ToList())
                if (h is Hediff_Injury || (h is Hediff_MissingPart && !set.PartOrAnyAncestorHasDirectlyAddedParts(h.Part)))
                    pawn.health.RemoveHediff(h);
        }

        // Consumes saved deadlines before applying side effects, so expiration is one-shot after loading.
        public static int Advance(FleshProgress p, int now)
        {
            int start = Math.Min(now, p.LastTick);
            int elapsed = Math.Max(0, now - start);
            int feastTicks = p.FeastUntil > start ? Math.Max(0, Math.Min(now, p.FeastUntil) - start) : 0;
            p.LastTick = now;
            p.AddThirst(elapsed / 6000f + feastTicks / 60f);
            int expired = 0;
            if (p.EmpowerUntil > 0 && now >= p.EmpowerUntil) { p.EmpowerUntil = 0; expired |= 1; }
            if (p.HardenUntil > 0 && now >= p.HardenUntil) { p.HardenUntil = 0; expired |= 2; }
            if (p.FeastUntil > 0 && now >= p.FeastUntil) p.FeastUntil = 0;
            return expired;
        }
        public static void Tick(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return;
            var p = FleshRepository.Ensure(pawn);
            if (p == null) return;
            // Capture threshold crossing before expiration, including catch-up while off-map.
            bool feastReachedMaximum = FeastReachedMaximum(p, Now);
            int previousTick = p.LastTick;
            int expired = Advance(p, Now);
            if ((expired & 1) != 0 && Rand.Chance(.1f))
            {
                foreach (var part in pawn.health.hediffSet.GetNotMissingParts().Where(x => (x.def == BodyPartDefOf.Arm || x.def == BodyPartDefOf.Leg) && !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(x)).ToList())
                {
                    if (pawn.Dead) break;
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, part.def.GetMaxHealth(pawn) * .1f, 999f, hitPart: part));
                }
            }
            if ((expired & 2) != 0 && !pawn.Dead)
            {
                var torso = pawn.RaceProps.body.corePart;
                pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, torso.def.GetMaxHealth(pawn) * .5f, 999f, hitPart: torso));
            }
            if (!pawn.Dead)
            {
                UpdateRage(pawn, p, feastReachedMaximum);
                FleshStatPatches.Refresh(pawn);
                if (previousTick / 250 != Now / 250) FleshHealth.Sync(pawn);
                FleshEffects.Tick(pawn, p, previousTick, Now);
            }
        }
        public static bool FeastReachedMaximum(FleshProgress p, int now)
        {
            int activeTicks = Math.Max(0, Math.Min(now, p.FeastUntil) - p.LastTick);
            return activeTicks > 0 && p.Thirst + activeTicks / 6000f + activeTicks / 60f >= p.MaxThirst;
        }
        public static void UpdateRage(Pawn pawn, FleshProgress p, bool feastReachedMaximum = false)
        {
            FleshStatPatches.Refresh(pawn);
            if (p.FeastRage && pawn.MentalStateDef != MentalStateDefOf.Berserk) p.FeastRage = false;
            if (p.FeastRage && p.Thirst <= p.MaxThirst * .2f) EndFeast(pawn, p);
            if (p.Thirst <= 0f) { p.FeastUntil = 0; return; }
            if (!p.FeastRage && (p.FeastUntil > Now || feastReachedMaximum) && p.Thirst >= p.MaxThirst && !pawn.InMentalState)
                p.FeastRage = pawn.mindState?.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, forced: true, forceWake: true) == true;
        }
        private static void EndFeast(Pawn pawn, FleshProgress p)
        {
            bool ownRage = p.FeastRage && pawn.MentalStateDef == MentalStateDefOf.Berserk;
            p.FeastUntil = 0;
            p.FeastRage = false;
            if (ownRage) pawn.MentalState.RecoverFromState();
            FleshHealth.Sync(pawn);
        }
        public static void StopFeast(Pawn owner, Pawn undead)
        {
            if (owner == null || owner.Dead || owner.Faction?.IsPlayer != true || undead == null || FleshRepository.Owner(undead) != owner) return;
            var p = FleshRepository.TryGet(undead);
            if (p != null) EndFeast(undead, p);
        }
        public static void StopAll(Pawn owner)
        {
            if (owner == null) return;
            foreach (Pawn pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive.ToList())
                if (FleshRepository.Owner(pawn) == owner) StopFeast(owner, pawn);
        }
    }
}
