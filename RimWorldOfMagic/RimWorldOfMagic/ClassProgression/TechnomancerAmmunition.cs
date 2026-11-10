using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    internal static class TechnomancerAmmunition
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var spawn = AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.Spawn),
                new[] { typeof(ThingDef), typeof(IntVec3), typeof(Map), typeof(WipeMode) });
            var substitute = AccessTools.Method(typeof(TechnomancerAmmunition), nameof(SpawnRound));
            foreach (var instruction in instructions)
            {
                if (!instruction.Calls(spawn)) { yield return instruction; continue; }
                yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
                yield return new CodeInstruction(OpCodes.Call, substitute).MoveBlocksFrom(instruction);
            }
        }

        internal static CompAbilityUserMagic Owner(Pawn body)
        {
            if (body?.Spawned != true || !body.RaceProps.Humanlike || body.Dead || body.Downed) return null;
            bool Eligible(CompAbilityUserMagic magic) => magic?.ExistingMagicData != null && magic.HasTechnoWeapon &&
                (magic.Pawn.story?.traits?.HasTrait(TorannMagicDefOf.Technomancer) == true ||
                ClassIdentity.IsTechnomancer(magic.Pawn) || ClassIdentity.IsChaosMage(magic.Pawn) ||
                TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_TechnoWeapon, magic, null));
            var local = body.GetCompAbilityUserMagic();
            if (Eligible(local)) return local;
            if (!SpiritRebirthState.TryGetPossessingSpirit(body,out var soul) ||
                SpiritHostedBolt.Find(body,soul,TorannMagicDefOf.TM_TechnoWeapon)==null) return null;
            var original = soul.GetCompAbilityUserMagic();
            return Eligible(original) ? original : null;
        }

        private static Thing SpawnRound(ThingDef vanilla, IntVec3 cell, Map map, WipeMode mode, Verb_LaunchProjectile verb)
        {
            var body = verb.caster as Pawn;
            var weapon = verb.EquipmentSource;
            var model = weapon?.TryGetComp<CompTechnoWeaponCopy>()?.Template ?? weapon?.def;
            var magic = body != null && verb.Bursting && body.GetPosture()==PawnPosture.Standing &&
                body.equipment?.Primary == weapon && model?.IsRangedWeapon == true && model.techLevel>=TechLevel.Industrial
                ? Owner(body) : null;
            if (magic?.Mana == null || !magic.useElementalShotToggle || magic.Mana.CurLevel < .02f)
                return GenSpawn.Spawn(vanilla,cell,map,mode);
            int rank = magic.MagicData.MagicPowerSkill_TechnoWeapon.FirstOrDefault(s=>s.label=="TM_TechnoWeapon_ver")?.level ?? 0;
            if (!Rand.Chance(.2f+.01f*rank)) return GenSpawn.Spawn(vanilla,cell,map,mode);

            float roll=Rand.Value;
            bool fire=roll<=.33f, ice=!fire && roll<=.66f;
            var round=GenSpawn.Spawn(ThingDef.Named(fire?"Bullet_ES_Fire":ice?"Bullet_ES_Ice":"Bullet_ES_Lit"),cell,map,mode);
            if (round is Projectile_ES_Fire flame) flame.Capture(magic,rank);
            if (round is Bullet_TechnoElemental bullet && magic.Pawn!=body) bullet.SetSource(magic.Pawn);
            magic.Mana.CurLevel -= System.Math.Max(0f,fire ? .02f-.0008f*rank : ice ? .01f-.0004f*rank : .015f-.0006f*rank);
            magic.MagicUserXP += fire ? 4 : ice ? 2 : 3;
            return round;
        }
    }

    public class Bullet_TechnoElemental : Bullet
    {
        private Pawn source;
        internal void SetSource(Pawn pawn) => source = pawn;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref source,"TM_elementalAmmoSource");
        }
        protected override void Impact(Thing hitThing,bool blockedByShield=false)
        {
            var physical=launcher;
            if (source!=null) launcher=source;
            try { base.Impact(hitThing,blockedByShield); }
            finally { launcher=physical; }
        }
    }
}
