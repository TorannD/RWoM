using System.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic.Utils;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public abstract class CompAbilityUserMagicMightBase : CompAbilityUser
    {
        public int customIndex = -2;

        public TMDefs.TM_CustomClass customClass = null;

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

            if(material != null)
                DrawMark(material, new Vector3(.28f, 1f, .28f));
        }
    }
}
