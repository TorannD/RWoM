using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_ShatterSentinel : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if(comp.IsMagicUser)
            {
                if(comp.summonedSentinels.Count > 0)
                {
                    for(int i =0; i < comp.summonedSentinels.Count; i++)
                    {
                        Thing sentinel = comp.summonedSentinels[i];
                        if (!sentinel.DestroyedOrNull())
                        {
                            Map sMap = sentinel.Map;
                            ShatterSentinel(sentinel, sMap);
                            if (!sentinel.Destroyed)
                            {
                                sentinel.Destroy(DestroyMode.Vanish);
                            }
                        }
                        else
                        {
                            comp.summonedSentinels.Remove(comp.summonedSentinels[i]);
                        }
                    }
                }
                else
                {

                }
            }
            return true;
        }

        private void ShatterSentinel(Thing sentinel, Map map)
        {
            float radius = 4f;
            Vector3 center = sentinel.DrawPos;            

            List<IntVec3> damageRing = GenRadial.RadialCellsAround(sentinel.Position, radius, true).ToList();
            List<IntVec3> outsideRing = GenRadial.RadialCellsAround(sentinel.Position, radius, false).Except(GenRadial.RadialCellsAround(sentinel.Position, radius - 1, true)).ToList();
            for (int i = 0; i < damageRing.Count; i++)
            {
                List<Thing> allThings = damageRing[i].GetThingList(map);
                for (int j = 0; j < allThings.Count; j++)
                {
                    if (allThings[j] is Pawn)
                    {
                        Pawn p = allThings[j] as Pawn;
                        TM_Action.DamageEntities(p, p.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Blunt, BodyPartHeight.Undefined, BodyPartDepth.Outside, null), Rand.Range(14, 22), DamageDefOf.Blunt, this.CasterPawn);
                    }
                    else if (allThings[j] is Building)
                    {
                        TM_Action.DamageEntities(allThings[j], null, Rand.Range(56, 88), DamageDefOf.Blunt, this.CasterPawn);
                    }
                    else
                    {
                        if (Rand.Chance(.1f))
                        {
                            GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.Filth_RubbleRock), damageRing[i], map, ThingPlaceMode.Near);
                        }
                    }
                }
            }
            for (int i = 0; i < outsideRing.Count; i++)
            {
                IntVec3 intVec = outsideRing[i];
                if (intVec.IsValid && intVec.InBoundsWithNullCheck(map))
                {
                    Vector3 moteDirection = TM_Calc.GetVector(sentinel.DrawPos.ToIntVec3(), intVec);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Rubble, sentinel.DrawPos, map, Rand.Range(.3f, .6f), .2f, .02f, .05f, Rand.Range(-100, 100), Rand.Range(8f, 13f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 0);
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, sentinel.DrawPos, map, Rand.Range(.9f, 1.2f), .3f, .02f, Rand.Range(.25f, .4f), Rand.Range(-100, 100), Rand.Range(5f, 8f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 0);
                    GenExplosion.DoExplosion(intVec, map, .4f, DamageDefOf.Blunt, this.CasterPawn, 0, 0, SoundDefOf.Pawn_Melee_Punch_HitBuilding, null, null, null, ThingDefOf.Filth_RubbleRock, .4f, 1, false, null, 0f, 1, 0, false);
                    //FleckMaker.ThrowSmoke(intVec.ToVector3Shifted(), base.Map, Rand.Range(.6f, 1f));
                }
            }
        }
    }
}
