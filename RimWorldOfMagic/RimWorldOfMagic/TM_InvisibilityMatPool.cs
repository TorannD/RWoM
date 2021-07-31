using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace TorannMagic
{
    public static class TM_InvisibilityMatPool
    {
        private struct MaterialInfo
        {
            public string stackTrace;
        }

        private static Dictionary<Material, MaterialInfo> references = new Dictionary<Material, MaterialInfo>();

        private static Dictionary<Material, Material> materials = new Dictionary<Material, Material>();

        private static Color color = new Color(0.565f, 0.18f, 0.81f, 0.5f);

        private static readonly int NoiseTex = Shader.PropertyToID("_NoiseTex");

        public static Material GetInvisibleMat(Material baseMat)
        {
            if (!materials.TryGetValue(baseMat, out Material value))
            {
                value = Create(baseMat);
                value.shader = ShaderDatabase.Invisible;
                value.SetTexture(NoiseTex, TexGame.RippleTex);
                value.color = color;
                materials.Add(baseMat, value);
            }
            return value;
        }

        public static Material Create(Material material)
        {
            Material material2 = new Material(material);
            references[material2] = new MaterialInfo
            {
                stackTrace = (Prefs.DevMode ? Environment.StackTrace : "(unavailable)")
            };
            return material2;
        }
    }
}
