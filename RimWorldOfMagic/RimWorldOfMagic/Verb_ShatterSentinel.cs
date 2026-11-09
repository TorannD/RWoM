using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_ShatterSentinel : ClassProgression.Verb_SpiritTargetable
    {
        protected override bool TryCastShot() => ApplyHostedEffect();

        protected override bool ApplyHostedEffect()
        {
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn,Ability);
            var comp = owner?.GetCompAbilityUserMagic();
            if (comp == null) return false;
            comp.PruneSentinels();
            bool shattered = false;
            foreach (var sentinel in comp.summonedSentinels.ToArray())
            {
                if (sentinel?.Spawned != true) continue;
                ShatterSentinel(sentinel, sentinel.Map, owner);
                if (!sentinel.Destroyed) sentinel.Destroy(DestroyMode.Vanish);
                comp.summonedSentinels.Remove(sentinel);
                shattered = true;
            }
            return shattered;
        }

        private void ShatterSentinel(Thing sentinel, Map map, Pawn owner)
        {
            IntVec3 origin = sentinel.Position;
            float radius = 4f;
            Vector3 center = sentinel.DrawPos;            

            List<IntVec3> damageRing = GenRadial.RadialCellsAround(origin, radius, true).ToList();
            List<IntVec3> outsideRing = GenRadial.RadialCellsAround(origin, radius, false).Except(GenRadial.RadialCellsAround(origin, radius - 1, true)).ToList();
            for (int i = 0; i < damageRing.Count; i++)
            {
                if (!damageRing[i].InBounds(map)) continue;
                List<Thing> allThings = damageRing[i].GetThingList(map).ToList();
                for (int j = 0; j < allThings.Count; j++)
                {
                    if (allThings[j] is Pawn)
                    {
                        Pawn p = allThings[j] as Pawn;
                        TM_Action.DamageEntities(p, p.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Blunt, BodyPartHeight.Undefined, BodyPartDepth.Outside, null), Rand.Range(14, 22), DamageDefOf.Blunt, owner);
                    }
                    else if (allThings[j] is Building)
                    {
                        TM_Action.DamageEntities(allThings[j], null, Rand.Range(56, 88), DamageDefOf.Blunt, owner);
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
                    Vector3 moteDirection = TM_Calc.GetVector(origin, intVec);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Rubble, center, map, Rand.Range(.3f, .6f), .2f, .02f, .05f, Rand.Range(-100, 100), Rand.Range(8f, 13f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 0);
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, center, map, Rand.Range(.9f, 1.2f), .3f, .02f, Rand.Range(.25f, .4f), Rand.Range(-100, 100), Rand.Range(5f, 8f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 0);
                    GenExplosion.DoExplosion(intVec, map, .4f, DamageDefOf.Blunt, owner, 0, 0, SoundDefOf.Pawn_Melee_Punch_HitBuilding_Generic, null, null, null, ThingDefOf.Filth_RubbleRock, .4f, 1, null, null, 0, false, null, 0f, 1, 0, false);
                    //FleckMaker.ThrowSmoke(intVec.ToVector3Shifted(), base.Map, Rand.Range(.6f, 1f));
                }
            }
        }
    }
}
