using System;
using System.Linq;
using AbilityUser;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritHostedSummons
    {
        internal static readonly (string job, Func<TMAbilityDef> spell, Type effect)[] Families = {
            ("TM_SpiritSummonMinion", () => TorannMagicDefOf.TM_SummonMinion, typeof(Projectile_SummonMinion)),
            ("TM_SpiritDismissMinion", () => TorannMagicDefOf.TM_DismissMinion, typeof(Verb_DismissMinion)),
            ("TM_SpiritSummonElemental", () => TorannMagicDefOf.TM_SummonElemental, typeof(Projectile_SummonElemental)),
            ("TM_SpiritSummonPylon", () => TorannMagicDefOf.TM_SummonPylon, typeof(Projectile_SummonPylon)),
            ("TM_SpiritSummonExplosive", () => TorannMagicDefOf.TM_SummonExplosive, typeof(Projectile_SummonExplosive)),
            ("TM_SpiritSummonPoppi", () => TorannMagicDefOf.TM_SummonPoppi, typeof(Projectile_SummonPoppi)),
            ("TM_SpiritPack", () => TorannMagicDefOf.TM_SpiritWolves, typeof(Verb_SummonSpiritWolves)),
            ("TM_SpiritPack_I", () => TorannMagicDefOf.TM_SpiritWolves_I, typeof(Verb_SummonSpiritWolves)),
            ("TM_SpiritPack_II", () => TorannMagicDefOf.TM_SpiritWolves_II, typeof(Verb_SummonSpiritWolves)),
            ("TM_SpiritPack_III", () => TorannMagicDefOf.TM_SpiritWolves_III, typeof(Verb_SummonSpiritWolves))
        };

        internal static bool IsProjectile(Type effect) => effect != null && typeof(Projectile_AbilityBase).IsAssignableFrom(effect)
            && Families.Any(f => f.effect == effect);

        internal static bool Prepare(Projectile_AbilityBase shot, MagicAbility ability)
        {
            var source = shot?.TryGetComp<CompHostedSummon>();
            if (source == null || ability?.magicDef.MainVerb.defaultProjectile != shot.def ||
                !Families.Any(f => f.effect == shot.GetType() && f.spell() == ability.Def)) return false;
            source.Bind(ability);
            return true;
        }

        internal static bool Resolve(Projectile_AbilityBase shot, Pawn launcher, out Pawn owner, out CompHostedSummon source)
        {
            source = shot.TryGetComp<CompHostedSummon>();
            if (source?.Projected != true)
            {
                source = null;
                owner = launcher;
                return owner != null;
            }
            return source.Take(out owner);
        }

        internal static int PackRank(Pawn soul, TMAbilityDef def, string suffix)
        {
            var data = soul.GetCompAbilityUserMagic().ExistingMagicData;
            int rank = SpiritRebirthRuntime.SavedRank(data, "TM_SpiritWolves" + suffix);
            var might = soul.GetCompAbilityUserMight();
            if (might?.mimicAbility == def)
                rank = might.ExistingMightData?.mightPowerSkill_Mimic?.FirstOrDefault(s => s.label == "TM_Mimic" + suffix)?.level ?? 0;
            if (suffix == "_eff") return rank;
            rank = Math.Max(rank, SpiritRebirthRuntime.SavedRank(data, "TM_Cantrips" + suffix) / 5);
            var fieldTraining = might?.ExistingMightData?.mightPowerSkill_FieldTraining;
            int training = fieldTraining?.FirstOrDefault(s => s.label == "TM_FieldTraining" + suffix)?.level ?? 0;
            return Math.Max(rank, training >= 13 ? 2 : training >= 9 ? 1 : 0);
        }

        internal static bool CanPlace(TMAbilityDef def, Map map, IntVec3 cell)
        {
            bool pylon = def == TorannMagicDefOf.TM_SummonPylon;
            if (!pylon && def != TorannMagicDefOf.TM_SummonExplosive) return true;
            var structure = DefDatabase<ThingDef>.GetNamedSilentFail(pylon ? "DefensePylon" : "TM_ManaMine");
            return map != null && structure != null && cell.InBounds(map) && cell.Standable(map) &&
                !GenSpawn.WouldWipeAnythingWith(cell, Rot4.North, structure, map, _ => true);
        }
    }

    // The physical launcher and shoot line stay native. Only a projected shot carries a separate owner.
    public sealed class CompHostedSummon : ThingComp
    {
        private Pawn owner;
        private bool projected, spent, created;
        private float refund;
        private int power, versatility, elementRoll;
        private float scale = 1f;
        internal bool Projected => projected;
        internal Pawn Owner => projected ? owner : null;
        internal int Power => power;
        internal int Versatility => versatility;
        internal float Scale => scale;
        internal int ElementRoll => elementRoll;

        internal void Bind(MagicAbility ability)
        {
            owner = ability.Pawn;
            var def = ability.magicDef;
            var magic = ability.MagicUser;
            projected = true;
            scale = magic.arcaneDmg;
            if (def == TorannMagicDefOf.TM_SummonElemental) elementRoll = Rand.Range(0, 8);
            if (def != TorannMagicDefOf.TM_SummonPoppi)
                refund = magic.ActualManaCost(def) * (!owner.IsColonist && ModOptions.Settings.Instance.AIAggressiveCasting ? .5f : 1f);
            power = SpiritRebirthRuntime.SavedRank(magic.MagicData, def.defName + "_pwr");
            versatility = SpiritRebirthRuntime.SavedRank(magic.MagicData, def.defName + "_ver");
            if (def == TorannMagicDefOf.TM_SummonMinion &&
                (owner.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer) || magic.customClass?.classMageAbilities.Contains(TorannMagicDefOf.TM_Cantrips) == true))
            {
                power = Math.Max(power, SpiritRebirthRuntime.SavedRank(magic.MagicData, "TM_Cantrips_pwr") / 5);
                versatility = Math.Max(versatility, SpiritRebirthRuntime.SavedRank(magic.MagicData, "TM_Cantrips_ver") / 5);
            }
            if ((def == TorannMagicDefOf.TM_SummonElemental || def == TorannMagicDefOf.TM_SummonPylon ||
                def == TorannMagicDefOf.TM_SummonExplosive) && owner.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                var mimic = owner.GetCompAbilityUserMight()?.ExistingMightData?.mightPowerSkill_Mimic;
                power = mimic?.FirstOrDefault(s => s.label == "TM_Mimic_pwr")?.level ?? 0;
                versatility = mimic?.FirstOrDefault(s => s.label == "TM_Mimic_ver")?.level ?? 0;
            }
        }

        internal bool Take(out Pawn soul)
        {
            soul = owner;
            if (spent || soul == null || soul.Dead || soul.Discarded || soul.Destroyed) return false;
            spent = true;
            return true;
        }

        internal void NotifySpawned(Thing thing)
        {
            if (projected && thing?.Spawned == true) created = true;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (!projected || !spent || created || refund <= 0 || owner == null || owner.Dead || owner.Destroyed || owner.Discarded) return;
            var mana = owner.GetCompAbilityUserMagic()?.Mana;
            if (mana != null) mana.CurLevel = Math.Min(mana.MaxLevel, mana.CurLevel + refund);
            refund = 0;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref projected, "TM_hostedSummon");
            if (!projected) return;
            Scribe_References.Look(ref owner, "TM_summonSoul");
            Scribe_Values.Look(ref spent, "TM_summonSpent");
            Scribe_Values.Look(ref created, "TM_summonCreated");
            Scribe_Values.Look(ref refund, "TM_summonRefund");
            Scribe_Values.Look(ref power, "TM_summonPower");
            Scribe_Values.Look(ref versatility, "TM_summonVersatility");
            Scribe_Values.Look(ref scale, "TM_summonScale", 1f);
            Scribe_Values.Look(ref elementRoll, "TM_summonElement");
        }
    }
}
