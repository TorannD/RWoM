using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;
using System.Collections.Generic;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class Projectile_ArcaneBolt : Projectile_AbilityBase
    {
        private int rotationOffset = 0;

        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            Pawn pawn = launcher as Pawn;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            GenExplosion.DoExplosion(
                Position, Map, def.projectile.explosionRadius, TMDamageDefOf.DamageDefOf.TM_Arcane, launcher,
                damAmount: Mathf.RoundToInt(Rand.Range(5, def.projectile.GetDamageAmount(1))* comp.arcaneDmg),
                armorPenetration: 1,
                explosionSound: def.projectile.soundExplode,
                weapon: def,
                projectile: equipmentDef,
                intendedTarget: intendedTarget.Thing
            );
        }

        public override void Draw()
        {
            this.rotationOffset += Rand.Range(20, 36);
            if(this.rotationOffset > 360)
            {
                this.rotationOffset = 0;
            }
            Mesh mesh = MeshPool.GridPlane(this.def.graphicData.drawSize);
            Graphics.DrawMesh(mesh, DrawPos, (Quaternion.AngleAxis(rotationOffset, Vector3.up) * ExactRotation), def.DrawMatSingle, 0);
            Comps_PostDraw();
        }

    }    
}


