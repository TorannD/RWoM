using System.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic.Utils;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using HarmonyLib;
using TorannMagic.TMDefs;

namespace TorannMagic
{
    public abstract class CompAbilityUserTMBase : CompAbilityUser
    {
        public int customIndex = -2;

        public TMDefs.TM_CustomClass customClass = null;
        private List<TMDefs.TM_CustomClass> advClasses = null;
        public List<TMDefs.TM_CustomClass> AdvancedClasses
        {
            get
            {
                if (advClasses == null)
                {
                    advClasses = new List<TMDefs.TM_CustomClass>();
                    advClasses.Clear();
                }
                return advClasses;
            }
            set
            {
                if (advClasses == null)
                {
                    advClasses = new List<TMDefs.TM_CustomClass>();
                    advClasses.Clear();
                }
                advClasses = value;
            }
        }

        protected int age = -1;

        protected int autocastTick = 0;
        protected int nextAICastAttemptTick = 0;

        public bool canDeathRetaliate = false;
        protected bool deathRetaliating = false;
        protected int ticksTillRetaliation = 600;

        public float arcalleumCooldown = 0f;
        public float arcaneRes = 1;
        public float coolDown = 1;
        public float xpGain = 1;

        public List<TMDefs.TM_CustomClass> CombinedCustomClasses
        {
            get
            {
                List<TMDefs.TM_CustomClass> tempcc = new List<TMDefs.TM_CustomClass>();
                tempcc.Clear();
                tempcc.AddRange(AdvancedClasses);
                if (this.customClass != null)
                {
                    tempcc.Add(this.customClass);
                }
                return tempcc;
            }
        }

        public List<TMAbilityDef> CombinedCustomAbilities
        {
            get
            {
                List<TMAbilityDef> tempca = new List<TMAbilityDef>();
                tempca.Clear();
                if (this.customClass != null)
                {
                    foreach (TMAbilityDef ability in this.customClass.classFighterAbilities)
                    {
                        tempca.Add(ability);
                    }
                }
                if (this.AdvancedClasses != null && AdvancedClasses.Count > 0)
                {
                    foreach (TMDefs.TM_CustomClass cc in this.AdvancedClasses)
                    {
                        foreach (TMAbilityDef advAbility in cc.classFighterAbilities)
                        {
                            tempca.Add(advAbility);
                        }
                    }
                }
                return tempca;
            }
        }

        private static readonly SimpleCache<string, Material> traitCache = new SimpleCache<string, Material>(5);

        protected void DrawMark(Material material, Vector3 scale)
        {
            Vector3 vector = Pawn.Drawer.DrawPos;
            vector.x += .45f;
            vector.z += .45f;
            vector.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            const float angle = 0f;

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), scale);

            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        // Scan for a trait and draw mark if there is one that applies. If you know the trait, use the specific DrawMark above
        protected void DrawMark()
        {
            Material material = traitCache.GetOrCreate(Pawn.ThingID, () =>
            {
                Trait markTrait =
                    Pawn.story.traits.allTraits.FirstOrDefault(trait => TraitIconMap.ContainsKey(trait.def));
                return markTrait != null ? TraitIconMap.Get(markTrait.def).IconMaterial : null;
            }, 5);

            if (material != null)
            {
                if (customClass != null)
                {
                    material.color = customClass.classIconColor;                    
                }
                DrawMark(material, new Vector3(.28f, 1f, .28f));
            }
        }
    }
}
