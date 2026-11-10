using RimWorld;
using System;
using Verse;
using AbilityUser;
using System.Linq;

namespace TorannMagic
{
    class Verb_Reversal : Verb_UseAbility  
    {
        internal const string Order = "TM_SpiritCastReversal";

        protected override bool TryCastShot() => Apply(CasterPawn, CasterPawn);

        internal static ThingDef IncomingRound(DamageInfo hit)
        {
            var weapon = hit.Weapon;
            if (hit.Def == null || hit.Def.isExplosive || weapon == null || hit.WeaponBodyPartGroup != null) return null;
            bool Matches(ThingDef round) => round?.projectile?.damageDef == hit.Def &&
                round.projectile.explosionRadius <= 0 && typeof(Projectile).IsAssignableFrom(round.thingClass);
            if (Matches(weapon)) return weapon;
            foreach (var verb in weapon.Verbs)
                if (Matches(verb.defaultProjectile)) return verb.defaultProjectile;
            return null;
        }

        internal static bool TryReflect(Pawn body, DamageInfo hit)
        {
            var attacker=hit.Instigator;
            if (hit.Amount <= 0 || hit.Def == null || hit.Def.isExplosive || body?.Spawned != true ||
                attacker?.Spawned != true || attacker == body || attacker.Map != body.Map ||
                (attacker is not Pawn && attacker is not Building_Turret)) return false;
            var marker=body.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ReversalHD)?.TryGetComp<HediffComp_Reversal>();
            var owner=marker?.SkillOwner;
            if (owner == null) return false;

            bool melee=attacker is Pawn && (hit.WeaponBodyPartGroup != null || hit.Weapon?.IsMeleeWeapon == true ||
                hit.Weapon == attacker.def && attacker.def.race != null);
            if (melee)
            {
                if (!attacker.Position.InHorDistOf(body.Position,2f)) return false;
                attacker.TakeDamage(new DamageInfo(hit.Def,hit.Amount,hit.ArmorPenetrationInt,hit.Angle,body,
                    intendedTarget:attacker));
            }
            else
            {
                var round=IncomingRound(hit);
                if (round == null) return false;
                // No weapon is attached to a returned round, so two stances cannot reflect it back and forth.
                TM_CopyAndLaunchProjectile.CopyAndLaunchThing(round,body,attacker,attacker,ProjectileHitFlags.All);
            }
            var flash=body.DrawPos+(attacker.DrawPos-body.DrawPos)/20f;
            TM_MoteMaker.ThrowSparkFlashMote(flash,body.Map,2f);
            var comp=owner.GetCompAbilityUserMight();
            int rank=comp?.ExistingMightData?.MightPowerSkill_Reversal.FirstOrDefault(s=>s.label=="TM_Reversal_ver")?.level ?? 0;
            if (rank>0) comp.SiphonReversal(rank,body);
            return true;
        }

        internal static bool Apply(Pawn body, Pawn source)
        {
            var comp = source?.GetCompAbilityUserMight();
            if (body == null || body.Dead || comp == null) return false;
            int rank = comp.MightData.MightPowerSkill_Reversal.FirstOrDefault(skill => skill.label == "TM_Reversal_pwr")?.level ?? 0;
            float strength = (8 + 2 * rank) * comp.mightPwr;
            return Strengthen(body,source,strength);
        }

        internal static bool Strengthen(Pawn body, Pawn source, float strength)
        {
            if (body == source)
            {
                HealthUtility.AdjustSeverity(body,TorannMagicDefOf.TM_ReversalHD,strength);
                return true;
            }
            var prior=body.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ReversalHD);
            if (prior != null)
            {
                if (prior.TryGetComp<HediffComp_Reversal>()?.SkillOwner != source) return false;
                prior.Severity+=strength;
                return true;
            }
            var stance=HediffMaker.MakeHediff(TorannMagicDefOf.TM_ReversalHD,body);
            var marker=stance.TryGetComp<HediffComp_Reversal>();
            if (marker == null) return false;
            marker.Bind(source);
            stance.Severity=strength;
            body.health.AddHediff(stance);
            return body.health.hediffSet.hediffs.Contains(stance);
        }
    }
}
