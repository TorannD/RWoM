using System;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.SpellItems
{
    public static partial class SpellItemEffects
    {
        public static string TargetRejection(Pawn pawn, LocalTargetInfo target, CompProperties_SpellItem props)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map == null || props == null || !target.IsValid)
                return "TM_SpellItem_InvalidTarget";
            Map map = pawn.Map;
            if (target.HasThing && (target.Thing.Destroyed || !target.Thing.Spawned || target.Thing.Map != map))
                return "TM_SpellItem_InvalidTarget";
            IntVec3 cell = target.Cell;
            if (!cell.InBounds(map) || cell.Fogged(map)) return "TM_SpellItem_InvalidTarget";
            if (!((pawn.Position - cell).LengthHorizontal <= props.range)) return "TM_SpellItem_OutOfRange";
            // Blink, like the existing ability, can cross a wall to a revealed destination.
            if (props.spell != SpellItemKind.Blink && !GenSight.LineOfSight(pawn.Position, cell, map, skipFirstCell: true))
                return "TM_SpellItem_NoLineOfSight";

            switch (props.spell)
            {
                case SpellItemKind.EtherShell:
                    return Odyssey.EtherShell.TargetRejection(pawn, target.Thing as Pawn);
                case SpellItemKind.BreachSeal:
                    return Odyssey.BreachSeal.TargetRejection(pawn, target.Cell);
                case SpellItemKind.Fireball:
                case SpellItemKind.Lightning:
                    return null;
                case SpellItemKind.Heal:
                    Pawn patient = target.Thing as Pawn;
                    if (patient == null || patient.Dead || patient.RaceProps?.IsFlesh != true || TM_Calc.IsUndead(patient))
                        return "TM_SpellItem_InvalidTarget";
                    if (patient.HostileTo(pawn)) return "TM_SpellItem_HostileTarget";
                    return HealingTargets(patient).Length == 0 ? "TM_SpellItem_NoInjuries" : null;
                case SpellItemKind.Blink:
                    if (cell == pawn.Position) return "TM_SpellItem_SameCell";
                    if (!cell.Standable(map)) return "TM_SpellItem_InvalidTarget";
                    if (cell.GetThingList(map).Any(thing => thing is Pawn && thing != pawn)) return "TM_SpellItem_Occupied";
                    return null;
                default:
                    return ExtendedRejection(pawn, target, props.spell);
            }
        }

        public static bool TryApply(Pawn pawn, LocalTargetInfo target, CompProperties_SpellItem props)
        {
            // Revalidate on completion: a target can move or become invalid during warmup.
            if (TargetRejection(pawn, target, props) != null) return false;
            switch (props.spell)
            {
                case SpellItemKind.EtherShell:
                    return Odyssey.EtherShell.TryApply(pawn, target.Thing as Pawn);
                case SpellItemKind.BreachSeal:
                    return Odyssey.BreachSeal.TryApply(pawn, target.Cell);
                case SpellItemKind.Fireball:
                case SpellItemKind.Lightning:
                    ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail(props.spell == SpellItemKind.Fireball ? "TM_ItemFireballProjectile" : "TM_ItemLightningProjectile");
                    if (projectileDef?.projectile == null || projectileDef.thingClass == null || !typeof(Projectile).IsAssignableFrom(projectileDef.thingClass))
                        return false;
                    Projectile projectile = (Projectile)GenSpawn.Spawn(projectileDef, pawn.Position, pawn.Map);
                    projectile.Launch(pawn, pawn.DrawPos, target, target, ProjectileHitFlags.All);
                    // Payment belongs to the successful launch, not a later hit or miss.
                    return true;
                case SpellItemKind.Heal:
                    Pawn patient = target.Thing as Pawn;
                    Hediff_Injury[] injuries = HealingTargets(patient);
                    if (injuries.Length == 0) return false;
                    float[] severities = injuries.Select(injury => injury.Severity).ToArray();
                    try
                    {
                        foreach (Hediff_Injury injury in injuries) injury.Heal(8f);
                    }
                    catch (Exception exception)
                    {
                        Compatibility.TM_Log.Warning("SpellItems", "Item healing interrupted: " + exception.Message);
                        if (!injuries.Where((injury, index) => injury.Severity < severities[index]).Any()) return false;
                    }
                    Cosmetic(() => EmitHeal(patient));
                    return true;
                case SpellItemKind.Blink:
                    IntVec3 origin = pawn.Position;
                    if (!Odyssey.ManualTravelSafety.CanComplete(pawn, pawn.Map, target.Cell)) return false;
                    try
                    {
                        pawn.Position = target.Cell;
                        pawn.Notify_Teleported(endCurrentJob: false);
                    }
                    catch (Exception exception)
                    {
                        Compatibility.TM_Log.Warning("SpellItems", "Item teleport interrupted: " + exception.Message);
                        if (pawn.Position != target.Cell) return false;
                    }
                    Cosmetic(() => EmitBlink(pawn, origin));
                    return true;
                default:
                    return ApplyExtended(pawn, target, props.spell);
            }
        }

        // Rendering must not turn an already-applied effect into a free cast.
        private static void Cosmetic(Action draw)
        {
            try { draw(); }
            catch (Exception exception) { Compatibility.TM_Log.Warning("SpellItems", "Spell item visual failed: " + exception.Message); }
        }

        // Materialize before healing: Heal can remove injuries from the live hediff list.
        // Stable list order, one wound per part, and no spillover from a smaller wound.
        public static Hediff_Injury[] HealingTargets(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.RaceProps?.IsFlesh != true || pawn.health?.hediffSet == null || TM_Calc.IsUndead(pawn))
                return new Hediff_Injury[0];
            return pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>()
                .Where(injury => injury.Part != null && injury.Severity > 0f && injury.CanHealNaturally())
                .GroupBy(injury => injury.Part).Select(group => group.First()).Take(3).ToArray();
        }

        // Rendering boundaries are separate from gameplay so headless tests can stub them.
        public static void EmitHeal(Pawn pawn)
        {
            if (pawn?.Map?.flecks == null) return;
            Rand.PushState();
            try { FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.HealingCross); }
            finally { Rand.PopState(); }
        }

        public static void EmitBlink(Pawn pawn, IntVec3 origin)
        {
            if (pawn?.Map?.flecks == null) return;
            Rand.PushState();
            try
            {
                FleckMaker.Static(origin, pawn.Map, FleckDefOf.ExplosionFlash, .6f);
                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.ExplosionFlash, .6f);
            }
            finally { Rand.PopState(); }
        }
    }
}
