using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;

namespace TorannMagic
{
    class Projectile_ES_Fire : Projectile_AbilityBase
    {
        private Pawn source;
        private int versatility;
        private float scale=1f;
        private bool captured;

        internal void Capture(CompAbilityUserMagic magic, int rank)
        {
            source=magic.Pawn;
            versatility=rank;
            scale=magic.arcaneDmg;
            captured=true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref source,"TM_elementalAmmoSource");
            Scribe_Values.Look(ref versatility,"TM_elementalAmmoRank");
            Scribe_Values.Look(ref scale,"TM_elementalAmmoScale",1f);
            Scribe_Values.Look(ref captured,"TM_elementalAmmoCaptured");
        }

        protected override void Impact(Thing hitThing, bool blockedByShield=false)
        {
            var map=Map;
            var cell=Position;
            var physical=launcher;
            if (!captured && physical is Pawn old && old.GetCompAbilityUserMagic() is CompAbilityUserMagic magic)
                Capture(magic,magic.MagicData.MagicPowerSkill_TechnoWeapon.FirstOrDefault(s=>s.label=="TM_TechnoWeapon_ver")?.level ?? 0);
            try
            {
                if(source!=null) launcher=source;
                base.Impact(hitThing,blockedByShield);
                GenExplosion.DoExplosion(cell,map,1f + versatility*.05f,DamageDefOf.Burn,launcher,
                    Mathf.RoundToInt(def.projectile.GetDamageAmount(1,null)*scale*(1f+.02f*versatility)),0,
                    def.projectile.soundExplode,def,equipmentDef,intendedTarget.Thing);
            }
            finally { launcher=physical; }
        }
    }
}
