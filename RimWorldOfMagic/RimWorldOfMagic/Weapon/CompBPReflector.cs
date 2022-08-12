using HarmonyLib;
using Verse;
using UnityEngine;

namespace TorannMagic.Weapon
{
    public class CompBPReflector : CompDeflector.CompDeflector
    {
        public override Verb ReflectionHandler(Verb newVerb)
        {
            CompAbilityUserMagic holder = GetPawn.GetCompAbilityUserMagic();
            bool canReflect = this.Props.canReflect && holder.IsMagicUser;
            Verb result;
            if (canReflect)
            {
                this.lastAccuracyRoll = this.ReflectionAccuracy();
                VerbProperties verbProperties = new VerbProperties
                {
                    hasStandardCommand = newVerb.verbProps.hasStandardCommand,
                    defaultProjectile = newVerb.verbProps.defaultProjectile,
                    range = newVerb.verbProps.range,
                    muzzleFlashScale = newVerb.verbProps.muzzleFlashScale,
                    warmupTime = 0f,
                    defaultCooldownTime = 0f,
                    soundCast = this.Props.deflectSound
                };
                switch (this.lastAccuracyRoll)
                {
                    case CompDeflector.CompDeflector.AccuracyRoll.CritialFailure:
                        {
                            verbProperties.accuracyLong = 999f;
                            verbProperties.accuracyMedium = 999f;
                            verbProperties.accuracyShort = 999f;
                            this.lastShotReflected = true;
                            break;
                        }
                    case CompDeflector.CompDeflector.AccuracyRoll.Failure:
                        verbProperties.accuracyLong = 0f;
                        verbProperties.accuracyMedium = 0f;
                        verbProperties.accuracyShort = 0f;
                        this.lastShotReflected = false;
                        break;
                    case CompDeflector.CompDeflector.AccuracyRoll.Success:
                        verbProperties.accuracyLong = 999f;
                        verbProperties.accuracyMedium = 999f;
                        verbProperties.accuracyShort = 999f;
                        this.lastShotReflected = true;
                        break;
                    case CompDeflector.CompDeflector.AccuracyRoll.CriticalSuccess:
                        {
                            verbProperties.accuracyLong = 999f;
                            verbProperties.accuracyMedium = 999f;
                            verbProperties.accuracyShort = 999f;
                            this.lastShotReflected = true;
                            break;
                        }
                }
                newVerb.verbProps = verbProperties;
                result = newVerb;
            }
            else
            {
                result = newVerb;
            }
            return result;
            //return base.ReflectionHandler(newVerb);
        }

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            bool flag = dinfo.Weapon != null;
            if (flag)
            {
                bool flag2 = !dinfo.Weapon.IsMeleeWeapon && dinfo.WeaponBodyPartGroup == null && GetPawn != null;
                if (flag2)
                {
                    bool hasCompActivatableEffect = this.HasCompActivatableEffect;
                    if (hasCompActivatableEffect)
                    {
                        bool? flag3 = new bool?((bool)AccessTools.Method(this.GetActivatableEffect.GetType(), "IsActive", null, null).Invoke(this.GetActivatableEffect, null));
                        bool flag4 = flag3 == false;
                        if (flag4)
                        {
                            absorbed = false;
                            return;
                        }
                    }
                    float deflectionChance = this.DeflectionChance;
                    float meleeSkill = GetPawn.skills.GetSkill(this.Props.deflectSkill).Level;
                    CompAbilityUserMagic holder = GetPawn.GetCompAbilityUserMagic();
                    deflectionChance += (meleeSkill * this.Props.deflectRatePerSkillPoint);
                    if (holder != null && !holder.IsMagicUser && (this.parent.def.defName == "TM_DefenderStaff" || this.parent.def.defName == "TM_BlazingPowerStaff"))
                    {
                        deflectionChance = 0;
                    }
                    int num = (int)(deflectionChance * 100f);
                    bool flag5 = Rand.Range(1, 100) > num;
                    if (flag5)
                    {
                        absorbed = false;
                        this.lastShotReflected = false;
                        return;
                    }
                    //splicing in TM handling of reflection
                    Thing instigator = dinfo.Instigator;
                    Vector3 drawPos = this.GetPawn.DrawPos;
                    drawPos.x += ((instigator.DrawPos.x - drawPos.x) / 20f) + Rand.Range(-.2f, .2f);
                    drawPos.z += ((instigator.DrawPos.z - drawPos.z) / 20f) + Rand.Range(-.2f, .2f);
                    TM_MoteMaker.ThrowSparkFlashMote(drawPos, this.GetPawn.Map, 2f);
                    Thing thing = new Thing();
                    thing.def = dinfo.Weapon;
                    if (instigator is Pawn shooterPawn)
                    {
                        if (!dinfo.Weapon.IsMeleeWeapon && dinfo.WeaponBodyPartGroup == null)
                        {
                            TM_CopyAndLaunchProjectile.CopyAndLaunchThing(shooterPawn.equipment.PrimaryEq.PrimaryVerb.verbProps.defaultProjectile, this.GetPawn, instigator, shooterPawn, ProjectileHitFlags.IntendedTarget, null);
                        }
                    }
                    //no longer using comp deflector handling
                    //this.ResolveDeflectVerb();
                    //this.GiveDeflectJob(dinfo);
                    dinfo.SetAmount(0);
                    absorbed = true; // true; 
                    return;
                }
            }
            absorbed = false;
        }
    }
}
