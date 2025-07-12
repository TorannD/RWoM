using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace TorannMagic
{
    public static class TM_MoteMaker
    {
        public static void ThrowGenericMote(ThingDef moteDef, Vector3 loc, Map map, float scale, float solidTime, float fadeIn, float fadeOut, int rotationRate, float velocity, float velocityAngle, float lookAngle)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(moteDef, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = rotationRate;
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity(velocityAngle, velocity);
            moteThrown.exactRotation = lookAngle;
            moteThrown.def.mote.solidTime = solidTime;
            moteThrown.def.mote.fadeInTime = fadeIn;
            moteThrown.def.mote.fadeOutTime = fadeOut;

            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowGenericFleck(FleckDef fleckDef, Vector3 loc, Map map, float scale, float solidTime, float fadeIn, float fadeOut, int rotationRate, float velocity, float velocityAngle, float lookAngle)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }

            FleckCreationData dataStatic = default(FleckCreationData);
            dataStatic.def = fleckDef;
            dataStatic.scale = scale;
            dataStatic.spawnPosition = loc;
            dataStatic.rotationRate = rotationRate;
            dataStatic.velocityAngle = velocityAngle;
            dataStatic.velocitySpeed = velocity;
            dataStatic.solidTimeOverride = solidTime;
            dataStatic.rotation = lookAngle;
            map.flecks.CreateFleck(dataStatic);
        }

        //public static void ThrowGenericMote(ThingDef moteDef, Vector3 loc, Map map, float scale, float solidTime, float fadeIn, float fadeOut, int rotationRate, float velocity, float velocityAngle, float lookAngle)
        public static Mote MakeOverlay(Thing target, ThingDef moteDef, Map map, Vector3 offset, float scale, float lookAngle, float fadeIn, float fadeOut, float solidTimeOverride, float growthRate)
        {
            Mote obj = (Mote)ThingMaker.MakeThing(moteDef);
            obj.Attach(target);
            obj.Scale = scale;
            obj.exactPosition = target.DrawPos + offset;            
            obj.exactRotation = lookAngle;
            obj.def.mote.fadeInTime = fadeIn;
            obj.def.mote.fadeOutTime = fadeOut;
            obj.def.mote.growthRate = growthRate;
            obj.solidTimeOverride = solidTimeOverride;
            GenSpawn.Spawn(obj, target.Position, map);
            return obj;
        }

        public static Mote MakeOverlay(TargetInfo target, ThingDef moteDef, Map map, Vector3 offset, float scale, float lookAngle, float fadeIn, float fadeOut, float solidTimeOverride, float growthRate)
        {
            Mote obj = (Mote)ThingMaker.MakeThing(moteDef);
            obj.Attach(target);
            obj.Scale = scale;
            obj.exactPosition = target.CenterVector3 + offset;            
            obj.exactRotation = lookAngle;
            obj.def.mote.fadeInTime = fadeIn;
            obj.def.mote.fadeOutTime = fadeOut;
            obj.def.mote.growthRate = growthRate;
            obj.solidTimeOverride = solidTimeOverride;
            GenSpawn.Spawn(obj, target.Cell, map);
            return obj;
        }

        public static void ThrowManaPuff(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_ManaPuff, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(-60, 60);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(0.6f, 0.75f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowBarrierMote(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_ManaPuff, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(300, 600);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(-0.6f, -0.75f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowNoteMote(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Note, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(0, 30);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(.75f, 2.5f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowTextMote(Vector3 loc, Map map, string text, Color color, float solidTime, float timeBeforeStartFadeout = -1f)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteText moteText = (MoteText)ThingMaker.MakeThing(TorannMagicDefOf.Mote_1sText, null);
            moteText.rotationRate = 0;
            moteText.exactPosition = loc;
            moteText.text = text;
            moteText.textColor = color;
            moteText.def.mote.solidTime = solidTime;
            moteText.SetVelocity(0, 0);
            GenSpawn.Spawn(moteText, loc.ToIntVec3(), map);
        }

        public static void ThrowDeceptionMaskMote(Vector3 loc, Map map, float scale, float solidTime)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_DeceptionMask, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = 0;
            moteThrown.exactPosition = loc;
            moteThrown.def.mote.solidTime = solidTime;
            moteThrown.SetVelocity(0, 0);
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowPossessMote(Vector3 loc, Map map, float scale, float solidTime)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Possess, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = 0;
            moteThrown.exactPosition = loc;
            moteThrown.def.mote.solidTime = solidTime;
            moteThrown.SetVelocity(0, 0);
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowExclamationMote(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Exclamation, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(0, 30);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(-60, 60), Rand.Range(.5f, .75f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowSparkFlashMote(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.Saturated)
            {
                return;
            }
            FleckMaker.Static(loc, map, TorannMagicDefOf.SparkFlash, scale);
        }

        public static void ThrowEnchantingMote(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Enchanting, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(150, 200);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(-0.35f, -0.75f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowCastingMote(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Casting, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(150, 200);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(0.35f, 0.75f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowCastingMote_Anti(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_AntiCasting, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(150, 200);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(0.35f, 0.75f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowCastingMote_Spirit(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_SpiritCasting, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(150, 250);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(0.35f, 0.75f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowSiphonMote(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Siphon, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(-60, 60);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(0.4f, 0.5f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowBoltMote(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.Saturated)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Bolt, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(-5, 5);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(2f, 3f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowPoisonMote(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Poison, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(-60, 60);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(0.4f, 0.5f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowDiseaseMote(Vector3 loc, Map map, float scale)
        {
            Mote baseMote = (Mote)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Disease, null);
            ThrowDiseaseMote(loc, map, scale, baseMote.def.mote.solidTime, baseMote.def.mote.fadeInTime, baseMote.def.mote.fadeOutTime);
        }

        public static void ThrowDiseaseMote(Vector3 loc, Map map, float scale, float solidTime, float fadeIn, float fadeOut)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Disease, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(-60, 60);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(1f, 2.5f));
            moteThrown.def.mote.solidTime = solidTime;
            moteThrown.def.mote.fadeInTime = fadeIn;
            moteThrown.def.mote.fadeOutTime = fadeOut;
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowArcaneMote(Vector3 loc, Map map, float scale)
        {
            Mote baseMote = (Mote)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Arcane, null);
            ThrowArcaneMote(loc, map, scale, baseMote.def.mote.solidTime, baseMote.def.mote.fadeInTime, baseMote.def.mote.fadeOutTime, 60, 1f);
        }

        public static void ThrowArcaneMote(Vector3 loc, Map map, float scale, float solidTime, float fadeIn, float fadeOut, int rotationRate, float velocity)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Arcane, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(-rotationRate, rotationRate);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(velocity, velocity * 1.5f));
            moteThrown.def.mote.solidTime = solidTime;
            moteThrown.def.mote.fadeInTime = fadeIn;
            moteThrown.def.mote.fadeOutTime = fadeOut;
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowShadowCleaveMote(Vector3 loc, Map map, float scale, float solidTime, float fadeIn, float fadeOut, int rotationRate, float velocity, float directionAngle)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_ShadowCleave, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = rotationRate;
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity(directionAngle, velocity);
            moteThrown.exactRotation = directionAngle;
            moteThrown.def.mote.solidTime = solidTime;
            moteThrown.def.mote.fadeInTime = fadeIn;
            moteThrown.def.mote.fadeOutTime = fadeOut;
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowArcaneWaveMote(Vector3 loc, Map map, float scale, float solidTime, float fadeIn, float fadeOut, int rotationRate, float velocity, float velocityAngle, float lookAngle)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_ArcaneWaves, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = rotationRate;
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity(velocityAngle, velocity);
            moteThrown.exactRotation = lookAngle;
            moteThrown.def.mote.solidTime = solidTime;
            moteThrown.def.mote.fadeInTime = fadeIn;
            moteThrown.def.mote.fadeOutTime = fadeOut;
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowRegenMote(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Regen, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(-60, 60);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(0.2f, 0.3f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowCrossStrike(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.Saturated)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_CrossStrike, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = 0f;
            moteThrown.exactRotation = Rand.Range(0, 3);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(0.05f, 0.1f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowBloodSquirt(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_BloodSquirt, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(-60, 60);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(1f, 2f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowShadowMote(Vector3 loc, Map map, float scale)
        {
            ThrowShadowMote(loc, map, scale, Rand.Range(-60, 60), Rand.Range(.5f, 3f), (float)Rand.Range(0, 360));
        }

        public static void ThrowShadowMote(Vector3 loc, Map map, float scale, int rotationRate, float velocity, float directionAngle)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Shadow, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = rotationRate;
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity(directionAngle, velocity);
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowFlames(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Flame, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(-100, 100);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(-60, 60), Rand.Range(1f, 2f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowTwinkle(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Twinkle, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(-10, 10);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(-30, 30), Rand.Range(1f, 2f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowTwinkle(Vector3 loc, Map map, float scale, float rotationRate, float velocity, float solidTime, float fadeIn, float fadeOut)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_Twinkle, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = rotationRate;
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(-30, 30), velocity);
            moteThrown.def.mote.solidTime = solidTime;
            moteThrown.def.mote.fadeInTime = fadeIn;
            moteThrown.def.mote.fadeOutTime = fadeOut;
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowMultiStrike(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.Saturated)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_MultiStrike, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(-60, 60);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(0.05f, 0.1f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowScreamMote(Vector3 loc, Map map, float scale, int r, int g, int b)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.Saturated)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_ScreamMote, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(-5, 5);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(0.2f, 0.5f));
            ColorInt colorInt = new ColorInt(r, g, b);
            Color arg_50_0 = colorInt.ToColor;
            moteThrown.SetColor(arg_50_0, false);
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);            
        }

        public static void ThrowArcaneDaggers(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TorannMagicDefOf.Mote_ArcaneDaggers, null);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = (float)Rand.Range(500, 800);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(0, 360), Rand.Range(0.2f, 0.3f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void MakePowerBeamMote(IntVec3 cell, Map map, float scale, float rot, float duration)
        {
            Mote mote = (Mote)ThingMaker.MakeThing(ThingDefOf.Mote_PowerBeam, null);
            MakePowerBeamMote(cell, map, scale, rot, duration, mote.def.mote.fadeInTime, mote.def.mote.fadeOutTime);
        }

        public static void MakePowerBeamMote(IntVec3 cell, Map map, float scale, float rot, float duration, float fadeIn, float fadeOut)
        {
            Mote mote = (Mote)ThingMaker.MakeThing(TorannMagicDefOf.Mote_PowerBeamBlue, null);
            mote.exactPosition = cell.ToVector3Shifted();
            mote.Scale = scale;
            mote.rotationRate = rot;
            mote.def.mote.solidTime = (duration - fadeIn - fadeOut);
            mote.def.mote.fadeInTime = fadeIn;
            mote.def.mote.fadeOutTime = fadeOut;
            GenSpawn.Spawn(mote, cell, map);
        }

        public static void MakePowerBeamMotePsionic(IntVec3 cell, Map map, float scale, float rot, float duration, float fadeIn, float fadeOut)
        {
            Mote mote = (Mote)ThingMaker.MakeThing(TorannMagicDefOf.Mote_PowerBeamPsionic, null);
            mote.exactPosition = cell.ToVector3Shifted();
            mote.Scale = scale;
            mote.rotationRate = rot;
            mote.def.mote.solidTime = (duration - fadeIn - fadeOut);
            mote.def.mote.fadeInTime = fadeIn;
            mote.def.mote.fadeOutTime = fadeOut;
            GenSpawn.Spawn(mote, cell, map);
        }

        public static void MakePowerBeamMoteColor(IntVec3 cell, Map map, float scale, float rot, float duration, float fadeIn, float fadeOut, Color color)
        {
            
            Mote mote = (Mote)ThingMaker.MakeThing(TorannMagicDefOf.Mote_PowerBeamGold, null);            
            mote.exactPosition = cell.ToVector3Shifted();
            mote.Scale = scale;
            mote.rotationRate = rot;
            mote.def.mote.solidTime = (duration - fadeIn - fadeOut);
            mote.def.mote.fadeInTime = fadeIn;
            mote.def.mote.fadeOutTime = fadeOut;
            //mote.Graphic.color = color;
            GenSpawn.Spawn(mote, cell, map);
        }

        public static void MakeBombardmentMote(IntVec3 cell, Map map, float scale, float rot, float duration)
        {
            Mote mote = (Mote)ThingMaker.MakeThing(ThingDefOf.Mote_Bombardment, null);
            MakeBombardmentMote(cell, map, scale, rot, duration, mote.def.mote.fadeInTime, mote.def.mote.fadeOutTime);
        }

        public static void MakeBombardmentMote(IntVec3 cell, Map map, float scale, float rot, float duration, float fadeIn, float fadeOut)
        {
            Mote mote = (Mote)ThingMaker.MakeThing(ThingDefOf.Mote_Bombardment, null);
            mote.exactPosition = cell.ToVector3Shifted();
            mote.Scale = scale * 6f;
            mote.rotationRate = rot;       
            mote.def.mote.solidTime = (duration - fadeIn - fadeOut);
            GenSpawn.Spawn(mote, cell, map);
        }
    }
}
