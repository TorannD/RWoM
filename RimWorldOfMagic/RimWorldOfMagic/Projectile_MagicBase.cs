using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;
using Verse.Sound;
using RimWorld;
using System.Collections.Generic;

namespace TorannMagic
{
    public class Projectile_MagicBase : Projectile
    {
        protected override void ImpactSomething()
        {
            if (base.def.projectile.flyOverhead)
            {
                RoofDef roofDef = base.Map.roofGrid.RoofAt(base.Position);
                if (roofDef != null)
                {
                    if (roofDef.isThickRoof)
                    {
                        ThrowDebugText("hit-thick-roof", base.Position);
                        def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(base.Position, base.Map));
                        Destroy();
                        return;
                    }
                    if (base.Position.GetEdifice(base.Map) == null || base.Position.GetEdifice(base.Map).def.Fillage != FillCategory.Full)
                    {
                        RoofCollapserImmediate.DropRoofInCells(base.Position, base.Map);
                    }
                }
            }
            if (usedTarget.HasThing && CanHit(usedTarget.Thing))
            {
                Pawn pawn = usedTarget.Thing as Pawn;
                if (pawn != null && pawn.GetPosture() != 0 && (origin - destination).MagnitudeHorizontalSquared() >= 20.25f && !Rand.Chance(0.2f))
                {
                    ThrowDebugText("miss-laying", base.Position);
                    Impact(null);
                }
                else
                {
                    Impact(usedTarget.Thing);
                }
            }
            else
            {
                List<Thing> list = VerbUtility.ThingsToHit(base.Position, base.Map, CanHit);
                list.Shuffle();
                for (int i = 0; i < list.Count; i++)
                {
                    Thing thing = list[i];
                    Pawn pawn2 = thing as Pawn;
                    float num;
                    if (pawn2 != null)
                    {
                        num = 0.5f * Mathf.Clamp(pawn2.BodySize, 0.1f, 2f);
                        if (pawn2.GetPosture() != 0 && (origin - destination).MagnitudeHorizontalSquared() >= 20.25f)
                        {
                            num *= 0.2f;
                        }
                        if (launcher != null && pawn2.Faction != null && launcher.Faction != null && !pawn2.Faction.HostileTo(launcher.Faction))
                        {
                            num *= VerbUtility.InterceptChanceFactorFromDistance(origin, base.Position);
                        }
                    }
                    else
                    {
                        num = 1.5f * thing.def.fillPercent;
                    }
                    if (Rand.Chance(num))
                    {
                        ThrowDebugText("hit-" + num.ToStringPercent(), base.Position);
                        Impact(list.RandomElement());
                        return;
                    }
                    ThrowDebugText("miss-" + num.ToStringPercent(), base.Position);
                }
                Impact(null);
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            GenClamor.DoClamor(this, 12f, ClamorDefOf.Impact);
            if (!blockedByShield && def.projectile.landedEffecter != null)
            {
                def.projectile.landedEffecter.Spawn(base.Position, base.Map).Cleanup();
            }
            Destroy();
        }

        private void ThrowDebugText(string text, IntVec3 c)
        {
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(c.ToVector3Shifted(), base.Map, text);
            }
        }

    }    
}


