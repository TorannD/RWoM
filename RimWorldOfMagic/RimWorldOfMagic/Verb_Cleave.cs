using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;
using System.Linq;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class Verb_Cleave : Verb_UseAbility_TrueBurst
    {
        DamageInfo dinfo;

        MightPowerSkill pwr;
        MightPowerSkill ver;
        MightPowerSkill str;

        float weaponDPS = 0;
        float dmgMultiplier = 1;
        float pawnDPS = 0;
        float skillMultiplier = 1;
        ThingWithComps weaponComp;
        int dmgNum = 0;

        private static readonly Color cleaveColor = new Color(160f, 160f, 160f);
        private static readonly Material cleavingMat = MaterialPool.MatFrom("Spells/cleave_straight", ShaderDatabase.Transparent, Verb_Cleave.cleaveColor);

        protected override bool TryCastShot()
        {

            bool flag10 = false;
            this.TargetsAoE.Clear();
            this.UpdateTargets();
            int shotsPerBurst = this.ShotsPerBurst;
            bool flag2 = this.UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && this.TargetsAoE.Count > 1;
            if (flag2)
            {
                this.TargetsAoE.RemoveRange(0, this.TargetsAoE.Count - 1);
            }
            for (int i = 0; i < this.TargetsAoE.Count; i++)
            {
                bool? flag3 = this.TryLaunchProjectile(this.verbProps.defaultProjectile, this.TargetsAoE[i]);
                bool hasValue = flag3.HasValue;
                if (hasValue)
                {
                    bool flag4 = flag3 == true;
                    if (flag4)
                    {
                        flag10 = true;
                    }
                    bool flag5 = flag3 == false;
                    if (flag5)
                    {
                        flag10 = false;
                    }
                }
            }

            CellRect cellRect = CellRect.CenteredOn(base.CasterPawn.Position, 1);
            Map map = base.CasterPawn.Map;
            cellRect.ClipInsideMap(map);

            IntVec3 centerCell = cellRect.CenterCell;
            CompAbilityUserMight comp = this.CasterPawn.GetCompAbilityUserMight();
            pwr = comp.MightData.MightPowerSkill_Cleave.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Cleave_pwr");
            str = comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr");
            ver = comp.MightData.MightPowerSkill_Cleave.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Cleave_ver");
            int dmgNum = 0;

            if (this.CasterPawn.equipment.Primary != null && !this.CasterPawn.equipment.Primary.def.IsRangedWeapon)
            {
                weaponComp = base.CasterPawn.equipment.Primary;
                weaponDPS = weaponComp.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS, false) *.7f;
                dmgMultiplier = weaponComp.GetStatValue(StatDefOf.MeleeWeapon_DamageMultiplier, false);
                pawnDPS = base.CasterPawn.GetStatValue(StatDefOf.MeleeDPS, false);
                skillMultiplier = (1.2f + (.025f * str.level));
                dmgNum = Mathf.RoundToInt(skillMultiplier * dmgMultiplier * (pawnDPS + weaponDPS));
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if(!this.CasterPawn.IsColonist && settingsRef.AIHardMode)
                {
                    dmgNum += 10;
                }
            }
            else
            {
                dmgNum = 4;
            }

            for (int i =0; i < 8; i ++)
            {
                IntVec3 searchCell = base.CasterPawn.Position + GenAdj.AdjacentCells8WayRandomized()[i];
                Pawn victim = searchCell.GetFirstPawn(map);
                if (victim != null && base.CasterPawn != null & dmgNum != 0 && victim.Faction != base.CasterPawn.Faction)
                {
                    
                    dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Cleave, dmgNum, 0, (float)-1, this.CasterPawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                    ApplyCleaveDamage(dinfo, this.CasterPawn, victim, map, ver.level);
                    DrawCleaving(victim, base.CasterPawn, 10);
                    i = 8;
                }                
            }
            
            this.burstShotsLeft = 0;
            this.PostCastShot(flag10, out flag10);
            return flag10;
        }

        public static void ApplyCleaveDamage(DamageInfo dinfo, Pawn caster, Pawn target, Map map, int ver)
        {

            bool flag = !dinfo.InstantPermanentInjury;
            if (flag)
            {
                bool flag2 = dinfo.Instigator != null;
                if (flag2)
                {
                    bool flag3 = caster != null && caster.PositionHeld != default(IntVec3) && !caster.Downed;
                    if (flag3)
                    {
                        System.Random random = new System.Random();
                        int rnd = GenMath.RoundRandom(random.Next(0, 100));
                        if (rnd < (ver * 15))
                        {
                            target.TakeDamage(dinfo);
                            FleckMaker.ThrowMicroSparks(target.Position.ToVector3(), map);
                        }
                        target.TakeDamage(dinfo);
                        FleckMaker.ThrowMicroSparks(target.Position.ToVector3(), map);
                        for (int i = 0; i < 8; i++)
                        {
                            IntVec3 intVec = target.PositionHeld + GenAdj.AdjacentCells[i];
                            Pawn cleaveVictim = new Pawn();
                            cleaveVictim = intVec.GetFirstPawn(map);
                            if (cleaveVictim != null && cleaveVictim.Faction != caster.Faction && cleaveVictim.HostileTo(caster.Faction))
                            {
                                cleaveVictim.TakeDamage(dinfo);
                                FleckMaker.ThrowMicroSparks(cleaveVictim.Position.ToVector3(), map);
                                rnd = GenMath.RoundRandom(random.Next(0, 100));
                                if (rnd < (ver * 15))
                                {
                                    cleaveVictim.TakeDamage(dinfo);
                                    FleckMaker.ThrowMicroSparks(cleaveVictim.Position.ToVector3(), map);
                                }
                                DrawCleaving(cleaveVictim, caster, 10);
                            }
                        }
                    }
                }
            }
        }

        public static void DrawCleaving(Pawn cleavedPawn, Pawn caster, int magnitude)
        {
            bool flag = !caster.Dead && !caster.Downed;
            if (flag)
            {
                Vector3 vector = cleavedPawn.Position.ToVector3();
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                float hyp = Mathf.Sqrt((Mathf.Pow(caster.Position.x - cleavedPawn.Position.x, 2)) + (Mathf.Pow(caster.Position.z - cleavedPawn.Position.z, 2)));
                float angleRad = Mathf.Asin(Mathf.Abs(caster.Position.x - cleavedPawn.Position.x) / hyp);
                float angle = Mathf.Rad2Deg * angleRad;
                //float angle = (float)Rand.Range(0, 360);
                Vector3 s = new Vector3(3f, 3f, 5f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, Verb_Cleave.cleavingMat, 0);
            }
        }  
    }
}
