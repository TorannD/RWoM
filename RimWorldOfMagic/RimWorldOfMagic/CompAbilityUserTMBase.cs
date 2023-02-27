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
        // TODO - This should be moved into customClass. This will make the checks clearer (customClass != null) and allow us to load in immediately instead of checking every time if set.
        public int customIndex = -2;

        public TM_CustomClass customClass = null;
        private List<TM_CustomClass> advClasses;
        public List<TM_CustomClass> AdvancedClasses
        {
            get => advClasses ?? (advClasses = new List<TM_CustomClass>());
            set => advClasses = value;
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

        public float weaponDamage = 1;

        //public List<TMDefs.TM_CustomClass> CombinedCustomClasses
        //{
        //    get
        //    {
        //        List<TMDefs.TM_CustomClass> combinedCustomClasses = new List<TMDefs.TM_CustomClass>();
        //        combinedCustomClasses.AddRange(AdvancedClasses);
        //        if (this.customClass != null)
        //        {
        //            combinedCustomClasses.Add(this.customClass);
        //        }
        //        return combinedCustomClasses;
        //    }
        //}

        //public List<TMAbilityDef> CombinedCustomAbilities
        //{
        //    get
        //    {
        //        List<TMAbilityDef> combinedCustomAbilities = new List<TMAbilityDef>();
        //        if (this.customClass != null)
        //        {
        //            combinedCustomAbilities.AddRange(customClass.classFighterAbilities);
        //        }
        //        if (this.AdvancedClasses != null && AdvancedClasses.Count > 0)
        //        {
        //            foreach (TMDefs.TM_CustomClass cc in this.AdvancedClasses)
        //            {
        //                combinedCustomAbilities.AddRange(cc.classFighterAbilities);
        //            }
        //        }
        //        return combinedCustomAbilities;
        //    }
        //}

        private static readonly SimpleCache<string, Material> traitCache = new SimpleCache<string, Material>(5);

        protected void DrawMark(Material material, Vector3 scale, float xOffset = 0, float yOffset = 0)
        {
            Vector3 vector = Pawn.Drawer.DrawPos;
            vector.x += .45f + xOffset;
            vector.z += .45f + yOffset;
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
                for (int i = 0; i < Pawn.story.traits.allTraits.Count; i++)
                {
                    var iconInfo = TraitIconMap.TryGetValue(Pawn.story.traits.allTraits[i].def);
                    if (iconInfo != null) return iconInfo.IconMaterial;
                }
                return null;
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
