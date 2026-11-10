using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class HediffComp_HostedStance : HediffComp
    {
        private Pawn source, body;
        private Hediff possession;
        internal TMAbilityDef ability;
        private bool projected;
        private int lastPulse = -1;
        internal CompAbilityUserMight TrainingOwner => projected && !CompShouldRemove ? source?.GetCompAbilityUserMight() : null;
        internal bool BeginPulse()
        {
            int now = Find.TickManager.TicksGame;
            if (CompShouldRemove || now == lastPulse) return false;
            lastPulse = now;
            return true;
        }
        internal bool Projected => projected;
        internal bool From(Pawn owner, Pawn host) => projected && source == owner && body == host && possession != null &&
            host.health.hediffSet.hediffs.Contains(possession);
        internal void Bind(Pawn owner, Pawn host)
        {
            source = owner; body = host; projected = true;
            possession = host.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD);
        }
        public override bool CompShouldRemove => projected && (body == null || !From(source,body) ||
            !SpiritRebirthState.TryGetPossessingSpirit(body, out var held) || held != source ||
            !SpiritRebirthState.TryGet(source,out var state) || state.DepletionPending ||
            ability != null && source.GetCompAbilityUserMight()?.Stamina == null);
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (ability == null || !Pawn.IsHashIntervalTick(250) || CompShouldRemove) return;
            var might = source.GetCompAbilityUserMight();
            if (might?.ExistingMightData != null) parent.Severity = SpiritHostedStance.Severity(might,ability);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref projected,"spiritSprintProjected");
            Scribe_Values.Look(ref lastPulse,"spiritStanceLastPulse",-1);
            Scribe_Defs.Look(ref ability,"spiritSprintAbility");
            Scribe_References.Look(ref possession,"spiritSprintPossession");
            Scribe_References.Look(ref source,"spiritSprintSource");
            Scribe_References.Look(ref body,"spiritSprintBody");
        }
    }

    internal static class SpiritHostedStance
    {
        internal const string Job = "TM_SpiritCastSprint";
        internal const string StrongBack = "TM_SpiritCastStrongBack", HeavyBlow = "TM_SpiritCastHeavyBlow",
            ThickSkin = "TM_SpiritCastThickSkin", FightersFocus = "TM_SpiritCastFightersFocus",
            GearRepair = "TM_SpiritCastGearRepair", InnerHealing = "TM_SpiritCastInnerHealing";
        internal static TMAbilityDef BaseAbility(string job) => job switch {
            Job => TorannMagicDefOf.TM_Sprint, StrongBack => TorannMagicDefOf.TM_StrongBack,
            HeavyBlow => TorannMagicDefOf.TM_HeavyBlow, ThickSkin => TorannMagicDefOf.TM_ThickSkin,
            FightersFocus => TorannMagicDefOf.TM_FightersFocus, GearRepair => TorannMagicDefOf.TM_GearRepair,
            InnerHealing => TorannMagicDefOf.TM_InnerHealing, _ => null
        };
        internal static bool SprintFamily(TMAbilityDef def) => Verb_Sprint.Rank(def) >= 0;
        internal static HediffDef Effect(TMAbilityDef def)
        {
            if (def == null) return null;
            if (SprintFamily(def)) return TorannMagicDefOf.TM_HediffSprint;
            if (def == TorannMagicDefOf.TM_GearRepair) return TorannMagicDefOf.TM_HediffGearRepair;
            if (def == TorannMagicDefOf.TM_InnerHealing) return TorannMagicDefOf.TM_HediffInnerHealing;
            if (def == TorannMagicDefOf.TM_HeavyBlow) return TorannMagicDefOf.TM_HediffHeavyBlow;
            if (def == TorannMagicDefOf.TM_StrongBack) return TorannMagicDefOf.TM_HediffStrongBack;
            if (def == TorannMagicDefOf.TM_ThickSkin) return TorannMagicDefOf.TM_HediffThickSkin;
            return def == TorannMagicDefOf.TM_FightersFocus ? TorannMagicDefOf.TM_HediffFightersFocus : null;
        }
        internal static bool Projected(Hediff effect) => effect?.TryGetComp<HediffComp_HostedStance>()?.Projected == true;

        private static Hediff Add(Pawn target, HediffDef def, float amount, Pawn soul, Pawn body)
        {
            if (target == null || target.Dead || def == null) return null;
            var existing = target.health.hediffSet.GetFirstHediffOfDef(def);
            if (existing != null)
            {
                if (existing.TryGetComp<HediffComp_HostedStance>()?.From(soul,body) != true) return null;
                existing.Severity += amount;
                return existing;
            }
            var effect = HediffMaker.MakeHediff(def, target);
            var origin = effect.TryGetComp<HediffComp_HostedStance>();
            if (origin == null) return null;
            origin.Bind(soul,body);
            effect.Severity = amount;
            target.health.AddHediff(effect);
            return target.health.hediffSet.hediffs.Contains(effect) ? effect : null;
        }

        private static float Rank(MightData data, string suffix)
        {
            return data.MightPowerSkill_Sprint.FirstOrDefault(skill => skill.label == "TM_Sprint_"+suffix)?.level ?? 0;
        }

        private static float Upkeep(CompAbilityUserMight might, TMAbilityDef def, bool regen)
        {
            var data = might.MightData;
            var training = data.MightPowerSkill_FieldTraining.FirstOrDefault(skill => skill.label=="TM_FieldTraining_eff")?.level ?? 0;
            float amount = regen ? def.upkeepRegenCost : def.upkeepEnergyCost;
            float efficiency = def.upkeepEfficiencyPercent == 0 ? 0 : SprintFamily(def) ? Rank(data,"eff") : data.GetSkill_Efficiency(def)?.level ?? 0;
            return amount * (1f - def.upkeepEfficiencyPercent * efficiency) * (1f - .03f * training);
        }

        private static Pawn HeldBody(Pawn soul)
        {
            return SpiritRebirthState.TryGet(soul,out var state) && !state.DepletionPending && state.HasValidHost(soul) ? state.Host : null;
        }

        internal static float Cost(Pawn soul, bool regen)
        {
            var body = HeldBody(soul);
            if (body == null) return 0;
            var might = soul.GetCompAbilityUserMight();
            if (might?.ExistingMightData == null) return 0;
            float total = 0;
            foreach (var effect in body.health.hediffSet.hediffs)
            {
                var origin = effect.TryGetComp<HediffComp_HostedStance>();
                if (origin?.ability != null && Effect(origin.ability) == effect.def && origin.From(soul,body))
                    total += Upkeep(might,origin.ability,regen);
            }
            return total;
        }

        internal static Hediff Regen(Pawn pawn, Hediff local)
        {
            if (local != null && !Projected(local)) return local;
            var body = HeldBody(pawn);
            var projected = body?.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnergyRegenHD);
            return projected?.TryGetComp<HediffComp_HostedStance>()?.From(pawn,body) == true ? projected : null;
        }

        internal static void TransferActive(Pawn soul, Pawn body)
        {
            var effects=soul.health.hediffSet.hediffs;
            CompAbilityUserMight owner=null;
            for(int i=effects.Count-1; i>=0; i--)
            {
                var legacy=effects[i];
                var def=legacy.def;
                var job=def==TorannMagicDefOf.TM_HediffSprint ? Job : def==TorannMagicDefOf.TM_HediffStrongBack ? StrongBack :
                    def==TorannMagicDefOf.TM_HediffHeavyBlow ? HeavyBlow : def==TorannMagicDefOf.TM_HediffThickSkin ? ThickSkin :
                    def==TorannMagicDefOf.TM_HediffFightersFocus ? FightersFocus : def==TorannMagicDefOf.TM_HediffGearRepair ? GearRepair :
                    def==TorannMagicDefOf.TM_HediffInnerHealing ? InnerHealing : null;
                if (job==null || body.health.hediffSet.HasHediff(def)) continue;
                var ability=SpiritHostedMartial.Find(body,soul,job);
                if (ability?.MightUser.ExistingMightData==null || ability.MightUser.Stamina==null) continue;
                var physical=Add(body,def,legacy.Severity,soul,body);
                if (physical==null) continue;
                physical.TryGetComp<HediffComp_HostedStance>().ability=ability.mightDef;
                soul.health.RemoveHediff(legacy);
                owner=ability.MightUser;
            }
            if (owner==null) return;
            owner.RefreshSpiritEconomy();
            owner.Stamina.CurLevel = Math.Min(owner.Stamina.CurLevel, owner.Stamina.MaxLevel);
        }

        internal static bool Ready(Pawn body, MightAbility ability)
        {
            var def = ability.mightDef;
            var might = ability.MightUser;
            if (def.chiCost != 0 || def.manaCost != 0 || def.bloodCost != 0 || def.requiredNeed != null ||
                def.requiredHediff != null || !def.consumeEnergy || might.Stamina == null || Effect(def) == null) return false;
            var effect = Effect(def);
            var buff = body.health.hediffSet.GetFirstHediffOfDef(effect);
            if (buff != null) return buff.TryGetComp<HediffComp_HostedStance>()?.From(ability.Pawn,body) == true;
            if (might.Pawn.health.hediffSet.HasHediff(effect)) return true;
            float price = might.ActualStaminaCost(def);
            return (!SprintFamily(def) || body.health.capacities.CapableOf(PawnCapacityDefOf.Moving)) && might.Stamina.CurLevel >= price &&
                might.Stamina.MaxLevel > Upkeep(might,def,false);
        }

        internal static float Severity(CompAbilityUserMight might, TMAbilityDef def)
        {
            var data = might.MightData;
            if (SprintFamily(def)) return .5f + Rank(data,"pwr");
            int Field(string suffix) => data.MightPowerSkill_FieldTraining.FirstOrDefault(s => s.label=="TM_FieldTraining_"+suffix)?.level ?? 0;
            if (def == TorannMagicDefOf.TM_HeavyBlow) return .95f + .19f * Field("pwr");
            int versatility=Field("ver");
            if (def == TorannMagicDefOf.TM_StrongBack) return versatility >= 8 ? 2.5f : versatility >= 3 ? 1.5f : .5f;
            if (def == TorannMagicDefOf.TM_ThickSkin) return versatility >= 12 ? 3.5f : versatility >= 7 ? 2.5f : versatility >= 2 ? 1.5f : .5f;
            return def == TorannMagicDefOf.TM_FightersFocus && versatility >= 1 ? 1.5f : .5f;
        }

        internal static void DiscardProjection(Pawn body, HediffDef def)
        {
            var active = body.health.hediffSet.GetFirstHediffOfDef(def);
            if (Projected(active)) body.health.RemoveHediff(active);
        }

        internal static bool Toggle(Pawn body, MightAbility ability)
        {
            var might = ability.MightUser;
            var soul = might.Pawn;
            var def = ability.mightDef;
            int rank = Verb_Sprint.Rank(def);
            Hediff Apply(Pawn target,HediffDef effect,float amount) => Add(target,effect,amount,soul,body);
            var effect = Effect(def);
            var buff = body.health.hediffSet.GetFirstHediffOfDef(effect);
            var legacy = soul.health.hediffSet.GetFirstHediffOfDef(effect);
            if (buff != null || legacy != null)
            {
                if (buff != null) body.health.RemoveHediff(buff);
                if (legacy != null) { soul.health.RemoveHediff(legacy); might.RefreshSpiritEconomy(); }
                Verb_Sprint.Refund(body,might,rank,Apply);
            }
            else
            {
                var stance = Apply(body,effect,Severity(might,def));
                if (stance == null) return false;
                stance.TryGetComp<HediffComp_HostedStance>().ability = def;
                might.Stamina.CurLevel = Math.Min(might.Stamina.CurLevel,might.Stamina.MaxLevel);
                Verb_Sprint.Burst(body,rank,Apply);
                FleckMaker.ThrowDustPuff(body.Position,body.Map,1f);
            }
            ability.PostAbilityAttempt();
            return true;
        }
    }

    // A later native source takes ownership of its own buff; an old projection cannot revoke it on exit.
    [HarmonyPatch(typeof(HealthUtility),nameof(HealthUtility.AdjustSeverity))]
    internal static class NativeStanceEffectOwner
    {
        static void Prefix(Pawn pawn, HediffDef hdDef, float sevOffset)
        {
            if (sevOffset <= 0 || hdDef == null || (hdDef != TorannMagicDefOf.TM_EvasionHD && hdDef != TorannMagicDefOf.TM_AdrenalineHD &&
                hdDef != TorannMagicDefOf.TM_EnergyRegenHD && hdDef != TorannMagicDefOf.TM_HediffSprint && hdDef != TorannMagicDefOf.TM_HediffHeavyBlow &&
                hdDef != TorannMagicDefOf.TM_HediffStrongBack && hdDef != TorannMagicDefOf.TM_HediffThickSkin && hdDef != TorannMagicDefOf.TM_HediffFightersFocus &&
                hdDef != TorannMagicDefOf.TM_HediffGearRepair && hdDef != TorannMagicDefOf.TM_HediffInnerHealing)) return;
            var old = pawn.health.hediffSet.GetFirstHediffOfDef(hdDef);
            if (SpiritHostedStance.Projected(old)) pawn.health.RemoveHediff(old);
        }
    }
}
