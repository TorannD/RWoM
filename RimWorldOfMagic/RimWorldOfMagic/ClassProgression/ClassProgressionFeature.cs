using Verse;

namespace TorannMagic.ClassProgression
{
    // Single kill switch for the beta class progression. The value is snapshotted
    // once at startup (TorannMagicMod ctor, before Harmony patch application) from
    // the "new progression (beta)" mod setting; toggling the setting requires a
    // game restart. When disabled the mod behaves like the classic class system:
    // The progression comp still scribes its data. Gameplay uses legacy class
    // identity until the player enables the feature again.
    public static class ClassProgressionFeature
    {
        public static bool Enabled { get; set; }

        // Reads the setting through the shared Settings instance when the settings
        // Mod subclass has already run, or loads it from disk otherwise, so the
        // snapshot is correct regardless of Mod construction order.
        public static void SnapshotFromSettings(Verse.Mod mod)
        {
            var settings = TorannMagic.ModOptions.Settings.Instance;
            if (settings == null && mod?.Content != null)
                settings = Verse.LoadedModManager.ReadModSettings<TorannMagic.ModOptions.Settings>(
                    mod.Content.FolderName, nameof(TorannMagic.ModOptions.Controller));
            Enabled = settings?.newProgressionBeta ?? false;
        }
    }
}
