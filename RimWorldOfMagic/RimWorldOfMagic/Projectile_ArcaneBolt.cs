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
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            Pawn pawn = this.launcher as Pawn;
            Pawn victim = hitThing as Pawn;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            GenExplosion.DoExplosion(base.Position, map, this.def.projectile.explosionRadius, TMDamageDefOf.DamageDefOf.TM_Arcane, this.launcher,  Mathf.RoundToInt(Rand.Range(5,this.def.projectile.GetDamageAmount(1, null))* comp.arcaneDmg), 1, this.def.projectile.soundExplode, def, this.equipmentDef, this.intendedTarget.Thing, null, 0f, 1, false, null, 0f, 1, 0.0f, false);
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


