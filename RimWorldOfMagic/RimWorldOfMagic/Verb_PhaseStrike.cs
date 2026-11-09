using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using TorannMagic.Compatibility;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class Verb_PhaseStrike : Verb_UseAbility  
    {
        private int verVal;
        private int pwrVal;
        private int dmgNum = 0;

        private static readonly Color bladeColor = new Color(160f, 160f, 160f);
        private static readonly Material bladeMat = MaterialPool.MatFrom("Spells/cleave", false);

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            return targ.IsValid && targ.Cell != root && TM_Teleport.CanMove(CasterPawn, targ.Cell)
                && !targ.Cell.Fogged(CasterPawn.Map) && (root - targ.Cell).LengthHorizontal < verbProps.range;
        }

        public static int GetWeaponDmg(Pawn pawn)
        {
            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            MightPowerSkill str = comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr");
            ThingWithComps weaponComp = pawn.equipment.Primary;
            float weaponDPS = weaponComp.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS, false) * .7f;
            float dmgMultiplier = weaponComp.GetStatValue(StatDefOf.MeleeWeapon_DamageMultiplier, false);
            float pawnDPS = pawn.GetStatValue(StatDefOf.MeleeDPS, false);
            float skillMultiplier = (.6f) * comp.mightPwr;
            return Mathf.RoundToInt(skillMultiplier * dmgMultiplier * (pawnDPS + weaponDPS));
        }

        protected override bool TryCastShot()
            => Execute(currentTarget);

        internal bool Execute(LocalTargetInfo target)
        {
            currentTarget=target;
            Odyssey.ManualTravelSafety.BeginAttempt(Ability);
            burstShotsLeft = 0;
            Pawn pawn = CasterPawn;
            bool canPay=Ability is MightAbility source && source.Pawn!=null && source.Pawn!=pawn
                ? ClassProgression.SpiritHostedMartial.Find(pawn,source.Pawn,ClassProgression.BladeMartialCast.Phase)==source &&
                    ClassProgression.SpiritHostedMartial.Ready(pawn,source,ClassProgression.BladeMartialCast.Phase)
                : Odyssey.ManualTravelSafety.CanPay(Ability);
            if (!canPay || pawn?.Spawned!=true || !CanHitTargetFrom(pawn.Position,currentTarget) || !Odyssey.ManualTravelSafety.CanComplete(pawn, pawn.Map, currentTarget.Cell))
            {
                Messages.Message("InvalidTargetLocation".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            if (pawn.equipment?.Primary == null || pawn.equipment.Primary.def.IsRangedWeapon)
            {
                Messages.Message("MustHaveMeleeWeapon".Translate(pawn.LabelCap), MessageTypeDefOf.RejectInput);
                return false;
            }

            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            TMAbilityDef ability = Ability?.Def as TMAbilityDef;
            if (comp == null || ability == null) return false;
            Pawn owner=Ability.Pawn ?? pawn;
            verVal = TM_Calc.GetSkillVersatilityLevel(owner, ability);
            pwrVal = TM_Calc.GetSkillPowerLevel(owner, ability);
            dmgNum = Mathf.RoundToInt(ClassProgression.BladeMartialCast.StrikeDamage(pawn,owner) * ability.weaponDamageFactor);
            if (!pawn.IsColonist && ModOptions.Settings.Instance.AIHardMode) dmgNum += 10;

            Map map = pawn.Map;
            IntVec3 destination = currentTarget.Cell;
            // Revalidate and move before any damage or rendering. The caster stays
            // spawned, retaining selection, draft state and the running ability job.
            if (!TM_Teleport.Move(pawn, destination)) return false;
            Odyssey.ManualTravelSafety.Settle(Ability);
            try
            {
                SearchForTargets(destination, 2f + .5f * verVal, map, pawn);
                DrawBlade(pawn.Position.ToVector3Shifted(), 4f + verVal);
                if (pawn.IsColonist)
                {
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BladeSweep, pawn.DrawPos, map,
                        1.4f + .4f * verVal, .04f, 0f, .18f, 1000, 0, 0, Rand.Range(0, 360));
                }
            }
            catch (Exception exception)
            {
                // The pawn already moved; preserve the completed cast if a
                // secondary effect fails instead of allowing a free teleport.
                TM_Log.Warning("Teleport", "Phase Strike completed, but its secondary effect failed: " + exception);
            }
            return true;
        }

        public void SearchForTargets(IntVec3 center, float radius, Map map, Pawn pawn)
        {       
            IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(center, radius, true);
            foreach (IntVec3 curCell in targets)
            {
                if (curCell.InBoundsWithNullCheck(map) && curCell.IsValid)
                {
                    FleckMaker.ThrowDustPuff(curCell, map, .2f);
                    // The old cast temporarily despawned its caster. Exclude only
                    // that pawn so its new position cannot hide a destination victim.
                    Pawn victim = curCell.GetThingList(map).OfType<Pawn>().FirstOrDefault(candidate => candidate != pawn);
                    if (victim != null && victim.Faction != pawn.Faction)
                    {
                        int damage=dmgNum;
                        if (Rand.Chance(.1f + .15f * pwrVal))
                        {
                            damage *= 3;
                            MoteMaker.ThrowText(victim.DrawPos, victim.Map, "Critical Hit", -1f);
                        }
                        DrawStrike(center, victim.Position.ToVector3(), map);
                        damageEntities(victim, null, damage, TMDamageDefOf.DamageDefOf.TM_PhaseStrike);
                    }
                }
            }
        }

        public void DrawStrike(IntVec3 center, Vector3 strikePos, Map map)
        {
            TM_MoteMaker.ThrowCrossStrike(strikePos, map, 1f);
            TM_MoteMaker.ThrowBloodSquirt(strikePos, map, 1.5f);
        }

        private void DrawBlade(Vector3 center, float magnitude)
        {

            Vector3 vector = center;
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            Vector3 s = new Vector3(magnitude, magnitude, (1.5f * magnitude));
            Matrix4x4 matrix = default(Matrix4x4);

            for (int i = 0; i < 6; i++)
            {
                float angle = Rand.Range(0, 360);
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, Verb_PhaseStrike.bladeMat, 0);
            }
        }

        public void damageEntities(Pawn victim, BodyPartRecord hitPart, int amt, DamageDef type)
        {
            DamageInfo dinfo;
            amt = (int)((float)amt * Rand.Range(.5f, 1.5f));
            dinfo = new DamageInfo(type, amt, 0, (float)-1, this.CasterPawn, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
            dinfo.SetAllowDamagePropagation(false);
            victim.TakeDamage(dinfo);
        }
    }
}
