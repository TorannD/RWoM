using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_HarvestPassion : HediffComp
    {
        public bool consumeJoy = false;
        public float reductionFactor = 1f;
        private bool initialized = false;        

        //unsaved
        bool shouldRemove = false;
        private int rotationDegree = 0;

        private int verVal = 0;
        private int pwrVal = 0;
        private int effVal = 0;
        private float arcaneDmg = 1f;

        public Pawn caster = null;
        internal int originalId;
        private Pawn Owner => originalId == 0 ? caster :
            ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(caster,out var soul) && soul.thingIDNumber == originalId ? soul : null;
        private bool Connected => caster?.Spawned == true && caster.Map == Pawn.Map && !caster.Dead && !caster.Downed &&
            Owner != null;

        public List<SkillRecord> validSkillPassions = new List<SkillRecord>();

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Pawn>(ref this.caster, "caster");
            Scribe_Values.Look(ref originalId, "harvestOriginalId", 0);
            Scribe_Values.Look(ref shouldRemove, "harvestFinished");
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0);
            Scribe_Values.Look<int>(ref this.effVal, "effVal", 0);
            Scribe_Values.Look<float>(ref this.arcaneDmg, "arcaneDmg", 1f);
        }
        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }


        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned && Pawn.skills != null && !Pawn.Dead;
            if (spawned && !caster.DestroyedOrNull() && !caster.Dead && !caster.Downed)
            {                
                CompAbilityUserMagic comp = Owner.GetCompAbilityUserMagic();
                if(comp != null && comp.MagicData != null)
                {
                    if (comp.MagicData.GetSkill_Power(TorannMagicDefOf.TM_HarvestPassion) != null)
                    {
                        pwrVal = comp.MagicData.GetSkill_Power(TorannMagicDefOf.TM_HarvestPassion).level;
                    }
                    if (comp.MagicData.GetSkill_Versatility(TorannMagicDefOf.TM_HarvestPassion) != null)
                    {
                        verVal = comp.MagicData.GetSkill_Versatility(TorannMagicDefOf.TM_HarvestPassion).level;
                    }
                    if (comp.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_HarvestPassion) != null)
                    {
                        effVal = comp.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_HarvestPassion).level;
                    }
                    arcaneDmg = comp.arcaneDmg;

                    if(!CheckAnyPassions())
                    {
                        shouldRemove = true;
                        //message
                    }
                }
                else
                {
                    shouldRemove = true;
                }                
            }
            else
            {
                shouldRemove = true;
            }
        }

        public bool CheckAnyPassions()
        {
            validSkillPassions.Clear();
            if (Owner?.skills == null || Pawn.skills == null) return false;
            foreach (SkillRecord skill in Pawn.skills.skills)
            {
                if (skill != null)
                {
                    if (Owner.skills.GetSkill(skill.def).passion < skill.passion)
                    {
                        validSkillPassions.Add(skill);
                    }
                }
            }
            return validSkillPassions.Count > 0;
        }

        public override bool CompShouldRemove => base.CompShouldRemove || this.shouldRemove;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Find.TickManager.TicksGame % 8 == 0)
            {
                bool flag = !base.Pawn.DestroyedOrNull() && base.Pawn.Spawned && !Pawn.Dead && Connected && !shouldRemove;
                if (flag)
                {
                    if (!initialized)
                    {
                        initialized = true;
                        Initialize();
                    }
                    else
                    {
                        TM_Action.DamageEntities(caster, null, 4f, 2f, DamageDefOf.Stun, caster);
                        TM_Action.DamageEntities(Pawn, null, 5f, 2f, DamageDefOf.Stun, caster);
                        DrawEffects();                            
                        if (Find.TickManager.TicksGame % 48 == 0)
                        {
                            if (caster.needs.joy != null && caster.needs.joy.CurLevel > .01f)
                            {
                                if (Pawn.needs.joy != null)
                                {
                                    float joyDrainPawn = Rand.Range(.04f, .1f) + (.01f * effVal);
                                    float joyDrainCaster = Mathf.Max(0f, Rand.Range(.03f, .06f) - (.01f * effVal));

                                    Need np = Pawn.needs.joy;
                                    np.CurLevel = Mathf.Clamp01(np.CurLevel - joyDrainPawn);
                                    Need nc = caster.needs.joy;
                                    nc.CurLevel = Mathf.Clamp01(nc.CurLevel - joyDrainCaster);
                                    if (np.CurLevel <= .01f && Rand.Chance(.25f + (.05f * pwrVal)))
                                    {
                                        StealPassion();
                                        if(CheckAnyPassions() && Rand.Chance(.1f + (.3f * pwrVal)))
                                        {
                                            StealPassion();
                                        }
                                        shouldRemove = true;
                                    }
                                }
                                else
                                {
                                    StealPassion();
                                    shouldRemove = true;
                                }
                            }
                            else
                            {
                                shouldRemove = true;
                                MoteMaker.ThrowText(caster.DrawPos, caster.Map, "Casting Failed", -1);
                            }
                        }
                    }                    
                }
                else
                {
                    shouldRemove = true;
                }
            }
        }

        public void StealPassion()
        {
            if (Connected && !shouldRemove && CheckAnyPassions())
            {
                SkillRecord sr = validSkillPassions.RandomElement();
                string count = "+";
                if(sr.passion == Passion.Major)
                {
                    count = "++";
                }
                Vector3 aboveHead = caster.DrawPos;
                aboveHead.z += .3f;
                MoteMaker.ThrowText(aboveHead, caster.Map, sr.def.defName.CapitalizeFirst() + count.ToString() + " harvested");
                Owner.skills.GetSkill(sr.def).passion = sr.passion;
                sr.passion = Passion.None;
            }

        }

        public void DrawEffects()
        {
            Vector3 headOffset = this.Pawn.DrawPos;
            headOffset.z += Rand.Range(-.2f, .2f);
            headOffset.x += Rand.Range(-.2f, .2f);
            rotationDegree += Rand.Range(8, 16);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_RedSwirl, headOffset, parent.pawn.Map, .6f, 0.2f, .1f, .2f, 30 + rotationDegree, 0, 0, rotationDegree);

            Vector3 throwVec = TM_Calc.GetVector(this.Pawn.DrawPos, caster.DrawPos);
            float throwAngle = (Quaternion.AngleAxis(90, Vector3.up) * throwVec).ToAngleFlat();
            float magnitude = (parent.pawn.Position - caster.Position).LengthHorizontal;
            for (int i = 0; i < 4; i++)
            {
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_EnergyStream, headOffset, Pawn.Map, Rand.Range(.4f, .8f), 0.15f, .02f + (.04f * i), .3f - (.06f * i), Rand.Range(-10, 10),  magnitude + (magnitude * .5f * i), throwAngle, Rand.Chance(.5f) ? throwAngle : throwAngle - 180);
            }
        }

    }
}
