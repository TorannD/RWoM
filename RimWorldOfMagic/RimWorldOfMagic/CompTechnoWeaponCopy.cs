using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class Thing_TechnoWeaponCopy : ThingWithComps
    {
        private ThingDef Template => this.TryGetComp<CompTechnoWeaponCopy>()?.Template;
        public override Graphic Graphic => Template?.graphicData?.GraphicColoredFor(this) ?? base.Graphic;
        public override string DescriptionFlavor => Template == null ? base.DescriptionFlavor : Template.description + "\n\n" + def.description;
    }

    public class CompProperties_TechnoWeaponCopy : CompProperties
    {
        public CompProperties_TechnoWeaponCopy() => compClass = typeof(CompTechnoWeaponCopy);
    }

    public class CompTechnoWeaponCopy : ThingComp
    {
        private ThingDef template;
        private int power;
        private float cooldown, touch, near, medium, far;
        internal ThingDef Template => template;

        internal void Capture(Thing source, int rank)
        {
            template = source.def;
            power = rank;
            cooldown = source.GetStatValue(StatDefOf.RangedWeapon_Cooldown) * (1 - .02f * power);
            float aim = 1 + .01f * power;
            touch = source.GetStatValue(StatDefOf.AccuracyTouch) * aim;
            near = source.GetStatValue(StatDefOf.AccuracyShort) * aim;
            medium = source.GetStatValue(StatDefOf.AccuracyMedium) * aim;
            far = source.GetStatValue(StatDefOf.AccuracyLong) * aim;
            SetVerb();
        }

        private void SetVerb()
        {
            var model = template?.Verbs.FirstOrDefault();
            var verb = parent.TryGetComp<CompEquippable>()?.PrimaryVerb;
            if (model == null || verb == null) return;
            var copy = parent.def.Verbs[0].MemberwiseClone();
            copy.defaultProjectile = model.defaultProjectile;
            copy.range = model.range * (1 + .02f * power);
            copy.warmupTime = model.warmupTime * (1 - .02f * power);
            copy.burstShotCount = Mathf.Max(1, Mathf.RoundToInt(model.burstShotCount * (1 + .02f * power)));
            copy.ticksBetweenBurstShots = Mathf.Max(1, Mathf.RoundToInt(model.ticksBetweenBurstShots * (1 - .02f * power)));
            copy.soundCast = model.soundCast;
            verb.verbProps = copy;
        }

        public override float GetStatOffset(StatDef stat)
        {
            if (template == null) return 0f;
            float value;
            if (stat == StatDefOf.RangedWeapon_DamageMultiplier) value = 1 + .02f * power;
            else if (stat == StatDefOf.RangedWeapon_Cooldown) value = cooldown;
            else if (stat == StatDefOf.AccuracyTouch) value = touch;
            else if (stat == StatDefOf.AccuracyShort) value = near;
            else if (stat == StatDefOf.AccuracyMedium) value = medium;
            else if (stat == StatDefOf.AccuracyLong) value = far;
            else return 0f;
            return value - stat.Worker.GetBaseValueFor(StatRequest.For(parent));
        }

        public override string TransformLabel(string label) => template == null ? label : template.LabelCap + " (" + label + ")";

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref template,"TM_weaponTemplate");
            Scribe_Values.Look(ref power,"TM_copyPower");
            Scribe_Values.Look(ref cooldown,"TM_copyCooldown");
            Scribe_Values.Look(ref touch,"TM_copyTouch");
            Scribe_Values.Look(ref near,"TM_copyShort");
            Scribe_Values.Look(ref medium,"TM_copyMedium");
            Scribe_Values.Look(ref far,"TM_copyLong");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                LongEventHandler.ExecuteWhenFinished(Restore);
        }

        private void Restore()
        {
            if (template != null) SetVerb();
            else if (parent.ParentHolder is Pawn_EquipmentTracker equipment)
                equipment.pawn.GetCompAbilityUserMagic()?.RestoreTechnoWeapon(parent);
        }
    }
}
