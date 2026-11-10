using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;

namespace TorannMagic
{
    public class Projectile_Spite : Projectile_AbilityBase
    {

        private int pwrVal = 0;
        private int verVal = 0;
        private float arcaneDmg = 0;
        private bool shotBound;

        internal void BindShot(Pawn owner, Pawn body)
        {
            if (shotBound) return;
            shotBound = true;
            var comp = owner?.GetCompAbilityUserMight();
            if (comp == null) return;
            bool mimic = owner.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true;
            var skills = mimic ? comp.MightData.MightPowerSkill_Mimic : comp.MightData.MightPowerSkill_Spite;
            string prefix = mimic ? "TM_Mimic_" : "TM_Spite_";
            pwrVal = skills.FirstOrDefault(x => x.label == prefix + "pwr")?.level ?? 0;
            verVal = skills.FirstOrDefault(x => x.label == prefix + "ver")?.level ?? 0;
            arcaneDmg = comp.mightPwr;
            if (ModOptions.Settings.Instance.AIHardMode && body != null && !body.IsColonist)
            {
                pwrVal = 3;
                verVal = 3;
            }

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref shotBound, "TM_spiteBound");
            Scribe_Values.Look(ref pwrVal, "TM_spitePower");
            Scribe_Values.Look(ref verVal, "TM_spiteVersatility");
            Scribe_Values.Look(ref arcaneDmg, "TM_spiteDamage");
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            // Old in-flight projectiles have no snapshot; recover their native launcher's ranks.
            if (!shotBound) BindShot(launcher as Pawn, launcher as Pawn);
            base.Impact(hitThing, blockedByShield);
            if (blockedByShield) return;
            Pawn victim = hitThing as Pawn;

            if(victim == null && this.intendedTarget != null && this.intendedTarget.Thing != null && this.intendedTarget.Thing is Pawn)
            {
                victim = this.intendedTarget.Thing as Pawn;
            }
            
            if (victim != null && !victim.Destroyed && !victim.Dead && victim.Spawned && victim.Map == impactMap)
            {
                TM_Action.DamageEntities(victim, null, this.def.projectile.GetDamageAmount(1, null) * (1+ .1f * pwrVal) * this.arcaneDmg, this.def.projectile.damageDef, this.launcher);
                if (Rand.Chance(.15f * verVal))
                {
                    int dmg = Mathf.RoundToInt(((this.def.projectile.GetDamageAmount(1, null) / 3)) * this.arcaneDmg);  //projectile = 15
                    TM_Action.DamageEntities(victim, null, dmg, DamageDefOf.Stun, this.launcher);
                }                
            }
        }

        protected override void Tick()
        {

            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_SpiritFlame, this.DrawPos, this.Map, Rand.Range(.3f, .4f), .05f, 0.05f, .1f, Rand.Range(-300, 300), Rand.Range(.2f, .6f), Rand.Range(0, 360), Rand.Range(0, 360));
            base.Tick();
        }

    }    
}


