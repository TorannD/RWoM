using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using HarmonyLib;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_SoothingBalm : Verb_UseAbility
    {
        bool validTarg;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt)
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ != null && targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = true;
                }
                else
                {
                    //out of range
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {
            Map map = this.CasterPawn.Map;

            Pawn pawn = this.currentTarget.Thing as Pawn;
            Pawn caster = this.CasterPawn;
            CompAbilityUserMight comp = caster.GetCompAbilityUserMight();

            int verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_SoothingBalm, false);
            int pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_SoothingBalm, false);

            try
            {
                bool flag = pawn != null && !pawn.Dead && !TM_Calc.IsUndead(pawn);
                if (!pawn.DestroyedOrNull() && pawn.Spawned && map != null && pawn.health != null && pawn.health.hediffSet != null && flag)
                {                   
                    int num = 2 + Mathf.RoundToInt(.3f * verVal);
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_SoothingBalmHD, .3f - (.03f * verVal));
                    using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            BodyPartRecord rec = enumerator.Current;
                            bool flag2 = num > 0;

                            if (flag2)
                            {
                                int num2 = 1 + Mathf.RoundToInt(.2f * verVal);
                                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                                if (!this.CasterPawn.IsColonist && settingsRef.AIHardMode)
                                {
                                    num2 = 5;
                                }
                                IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                                Func<Hediff_Injury, bool> arg_BB_1;

                                arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                                for (int i = 0; i < num; i++)
                                {
                                    Vector3 pos = pawn.DrawPos;
                                    pos.x += Rand.Range(-.3f, .3f);
                                    pos.z += Rand.Range(-.3f, .3f);
                                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Healing_Small, pos, map, Rand.Range(.6f, 1f), .3f, .2f, .5f, 0, 0f, 0f, Rand.Range(0, 360));
                                }

                                foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                                {
                                    bool flag4 = num2 > 0;
                                    if (flag4)
                                    {
                                        bool flag5 = current.CanHealNaturally() && !current.IsPermanent() && current.TendableNow(false);
                                        if (flag5)
                                        {
                                            //current.Heal((float)((int)current.Severity + 1));
                                            if (!this.CasterPawn.IsColonist)
                                            {
                                                current.Heal(10 + (float)pwrVal * 3f); // power affects how much to heal
                                            }
                                            else
                                            {
                                                current.Heal((4.0f + (float)pwrVal)); // power affects how much to heal
                                            }
                                            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_SoothingBalmHD, .04f);
                                            Vector3 pos = pawn.DrawPos;
                                            pos.x += Rand.Range(-.3f, .3f);
                                            pos.z += Rand.Range(-.3f, .3f);
                                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Healing_Small, pos, map, Rand.Range(.6f, 1f), .3f, .2f, .5f, 0, 0f, 0f, Rand.Range(0, 360));
                                            num--;
                                            num2--;
                                            if(current.TendableNow())
                                            {
                                                float tendQuality = Rand.Range(.5f, .7f) + (pwrVal * .1f);
                                                current.Tended(tendQuality, 1f);
                                                pawn.records.Increment(RecordDefOf.TimesTendedTo);
                                                caster.records.Increment(RecordDefOf.TimesTendedOther);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                }
                else
                {
                    Messages.Message("TM_InvalidTarget".Translate(caster.LabelShort, this.Ability.Def.label), MessageTypeDefOf.NeutralEvent);
                }
            }
            catch (NullReferenceException ex)
            {
                //ex
            }
            return false;
        }
    }
}
