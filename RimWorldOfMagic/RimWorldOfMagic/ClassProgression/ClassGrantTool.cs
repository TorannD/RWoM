using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    // Public grant facade for editor integrations (Character Editor adapter in
    // RWoM Integrations). Runs the same transactions as the book and ritual
    // paths — vocation enrollment, foundation disciplines, learned starting
    // powers, potential consumption, vocation trait and class-gene sync — so a
    // granted pawn is indistinguishable from a normally enrolled one. No book
    // payment: editor grants are administration by definition.
    public static class ClassGrantTool
    {
        public sealed class GrantOption
        {
            public string Id;
            public string Label;
            public string Domain;
            public TraitDef LegacyTrait;
        }

        // Every class this tool can grant: magic foundation profiles by legacy
        // trait plus native martial paths by class trait. The caller shows its
        // own picker; the pawn state decides final admission.
        public static List<GrantOption> Options()
        {
            var result = new List<GrantOption>();
            foreach (var profile in ClassInitiation.FoundationProfiles)
            {
                if (profile?.legacyTrait == null) continue;
                result.Add(new GrantOption
                {
                    Id = profile.legacyTrait.defName,
                    // TraitDef.label is commonly null; the display name lives in
                    // the degree data. Fall back to the def name.
                    Label = DisplayName(profile.legacyTrait),
                    Domain = "Magic",
                    LegacyTrait = profile.legacyTrait
                });
            }
            foreach (var traitName in new[] { "Gladiator", "TM_Sniper", "Bladedancer", "Ranger", "TM_Commander",
                "DeathKnight", "TM_Psionic", "TM_Monk", "Faceless", "TM_SuperSoldier", "TM_Apothecary" })
            {
                var trait = DefDatabase<TraitDef>.GetNamedSilentFail(traitName);
                if (trait != null)
                    result.Add(new GrantOption { Id = traitName, Label = DisplayName(trait), Domain = "Might", LegacyTrait = trait });
            }
            return result;
        }

        // TraitDef.label is frequently null for class traits; the degree data
        // carries the display name.
        private static string DisplayName(TraitDef trait)
        {
            var degree = trait?.degreeDatas?.FirstOrDefault(d => !string.IsNullOrEmpty(d.label));
            return degree?.label ?? trait?.defName ?? "?";
        }

        public static bool TryGrant(Pawn pawn, string classId, out string error)
        {
            error = null;
            if (pawn == null || classId == null)
            {
                error = "no pawn or class";
                return false;
            }
            var option = Options().FirstOrDefault(o => o.Id == classId);
            if (option == null)
            {
                error = "unknown class id " + classId;
                return false;
            }
            if (!ClassEnrollment.HasNoPriorOwnership(pawn))
            {
                error = "pawn already owns class data";
                return false;
            }
            if (option.Domain == "Magic" &&
                ClassInitiation.TryGrantStartingFoundation(pawn, option.LegacyTrait))
            {
                return true;
            }
            if (option.Domain == "Might" &&
                typeof(JobDriver_GladiatorInitiation)
                    .GetMethod("TryGrantRolledFoundation", BindingFlags.Static | BindingFlags.NonPublic)!
                    .Invoke(null, new object?[] { pawn, option.LegacyTrait }) is true)
            {
                return true;
            }
            error = "the " + option.Label + " foundation refused this pawn";
            return false;
        }
    }
}
