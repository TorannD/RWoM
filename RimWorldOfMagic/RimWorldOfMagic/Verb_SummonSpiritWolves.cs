using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse.AI;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_SummonSpiritWolves : ClassProgression.Verb_SpiritTargetable
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            var map = CasterPawn?.Map;
            return targ.IsValid && (targ.Thing != CasterPawn || verbProps.targetParams.canTargetSelf) &&
                map != null && targ.Cell.InBounds(map) && !targ.Cell.Fogged(map) &&
                targ.Cell.Walkable(map) && (root-targ.Cell).LengthHorizontal < verbProps.range &&
                TryFindShootLineFromTo(root,targ,out _);
        }

        protected override bool TryCastShot()
        {
            burstShotsLeft = 0;
            if (SpawnPack(CasterPawn,CasterPawn.Map)) return true;
            Messages.Message("InvalidSummon".Translate(),MessageTypeDefOf.RejectInput);
            return false;
        }

        protected override bool ApplyHostedEffect()
        {
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn,Ability);
            return owner != null && SpawnPack(owner,CasterPawn.Map);
        }

        private bool SpawnPack(Pawn caster, Map map)
        {
            bool hosted = caster != CasterPawn;
            var shot = hosted ? verbProps.defaultProjectile : Projectile;
            if (shot?.projectile == null || !currentTarget.IsValid || !currentTarget.Cell.InBounds(map)) return false;
            var cellList = GenRadial.RadialCellsAround(currentTarget.Cell, shot.projectile.explosionRadius, true)
                .Where(c => c.InBounds(map) && c.Standable(map)).ToList();
            if (cellList.Count == 0) return false;
            var def = (TMAbilityDef)Ability.Def;
            int Rank(string suffix) => hosted ? ClassProgression.SpiritHostedSummons.PackRank(caster, def, suffix)
                : TM_Calc.GetSkillLevel(caster, def, suffix);
            int pwrVal = Rank("_pwr"), verVal = Rank("_ver"), effVal = Rank("_eff");
            int summonCount = 5 + verVal;
            var enemies = map.mapPawns.AllPawnsSpawned.Where(p => !p.Dead && !p.Downed && !p.IsPrisoner &&
                p.Faction != null && p.Faction.HostileTo(caster.Faction)).ToList();
            var openCells = cellList.Where(c => c.GetFirstPawn(map) == null).InRandomOrder().Take(summonCount).ToList();
            var wolf = new SpawnThings { def=TorannMagicDefOf.TM_SpiritWolfR, kindDef=TorannMagicDefOf.TM_SpiritWolf, spawnCount=1, temporary=true };
            int spawned = 0;
            for (int i = 0; i < summonCount; i++)
            {
                var cell = i < openCells.Count ? openCells[i] : cellList.RandomElement();
                FleckMaker.ThrowSmoke(cell.ToVector3Shifted(),map,Rand.Range(3f,4f));
                var newPawn = TM_Action.SingleSpawnLoop(caster,wolf,cell,map,Rand.Range(1000,1200) + 120*effVal,true,false,caster.Faction,false);
                if (!(newPawn is Pawn animal) || !animal.Spawned) continue;
                spawned++;
                if (animal.playerSettings != null)
                {
                    animal.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
                    animal.playerSettings.medCare = MedicalCareCategory.NoCare;
                }
                HealthUtility.AdjustSeverity(animal, TorannMagicDefOf.TM_EnrageHD, .2f + (.1f * pwrVal));
                for (int j = 0; j < 3; j++)
                {
                    FleckMaker.ThrowSmoke(animal.Position.ToVector3Shifted(), map, Rand.Range(.5f, 1.1f));
                }
                if (enemies.Where(p => p.Spawned && p.Map == map && !p.Downed && p.Position != animal.Position &&
                    p.Position.InHorDistOf(animal.Position,30)).TryRandomElement(out Pawn enemy))
                {
                    Job defendJob = new Job(JobDefOf.AttackMelee, enemy);
                    animal.jobs.TryTakeOrderedJob(defendJob);
                }
            }
            return spawned > 0;
        }
    }
}
