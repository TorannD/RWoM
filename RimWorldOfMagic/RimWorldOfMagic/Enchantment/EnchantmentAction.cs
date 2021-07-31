using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Enchantment
{
    public class EnchantmentAction
    {

        public EnchantmentActionType type = EnchantmentActionType.Null;
        public string actionLabel = "";

        public DamageDef damageDef;
        public float damageAmount = 0;
        public float damageVariation = 0;
        public float armorPenetration = 0;
        public float damageChance = 0f;
        public HediffDef hediffDef;
        public float hediffSeverity = 1f;
        public int hediffDurationTicks = 0;
        public float hediffChance = 0f;
        public bool onSelf = false;
        public bool friendlyFire = false;
        public float splashRadius = 0f;
    }

    public enum EnchantmentActionType
    {
        ApplyDamage,
        ApplyHediff,
        Null
    }
}
