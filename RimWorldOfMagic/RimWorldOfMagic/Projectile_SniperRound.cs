using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;

namespace TorannMagic
{
    public abstract class Projectile_SniperRound : Projectile_AbilityBase
    {
        protected bool shotBound;
        protected Pawn skillOwner;
        protected ThingDef firedWeapon;
        protected IntVec3 firingCell;
        protected int versatility, powerRank, strength, shotDamage;
        protected float accuracy, armorPen;
        protected Pawn EffectOwner => shotBound ? skillOwner : launcher as Pawn;

        protected void ApplyFriendlyFireGoodwill(Pawn victim,int penalty)
        {
            var attackerFaction = launcher?.Faction ?? EffectOwner?.Faction;
            var faction = victim.Faction;
            if (!victim.IsColonist && !victim.IsPrisoner && faction != null && attackerFaction != null &&
                faction != attackerFaction && !faction.HostileTo(attackerFaction))
                faction.TryAffectGoodwillWith(attackerFaction,penalty,true,false,TorannMagicDefOf.TM_OffensiveMagic,null);
        }

        internal void BindShot(Pawn body, MightAbility power, ThingDef round)
        {
            shotBound = true;
            skillOwner = power.Pawn;
            firedWeapon = body.equipment.Primary.def;
            firingCell = body.Position;
            armorPen = round.projectile.GetArmorPenetration();
            accuracy = body.GetStatValue(StatDefOf.ShootingAccuracyPawn);
            versatility = TM_Calc.GetSkillVersatilityLevel(skillOwner,power.mightDef);
            powerRank = TM_Calc.GetSkillPowerLevel(skillOwner,power.mightDef);
            strength = power.MightUser.MightData.MightPowerSkill_global_strength.FirstOrDefault(x=>x.label=="TM_global_strength_pwr")?.level ?? 0;
            CaptureWeaponDamage(body);
        }

        protected virtual void CaptureWeaponDamage(Pawn body) { }
        protected static float PhysicalWeaponDamage(Pawn body) =>
            body.GetCompAbilityUserMight()?.GetSkillDamage() ?? TM_Calc.GetSkillDamage_Range(body,1f);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref shotBound,"TM_boundSniperRound");
            if (!shotBound) return;
            Scribe_References.Look(ref skillOwner,"TM_shotOwner");
            Scribe_Defs.Look(ref firedWeapon,"TM_firedWeapon");
            Scribe_Values.Look(ref firingCell,"TM_firingCell",IntVec3.Invalid);
            Scribe_Values.Look(ref shotDamage,"TM_shotDamage");
            Scribe_Values.Look(ref powerRank,"TM_shotPower");
            Scribe_Values.Look(ref versatility,"TM_shotVersatility");
            Scribe_Values.Look(ref strength,"TM_shotStrength");
            Scribe_Values.Look(ref armorPen,"TM_shotPenetration");
            Scribe_Values.Look(ref accuracy,"TM_shotAccuracy");
        }
    }
}
