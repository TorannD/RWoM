using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.Integrations.CharacterEditorCompat
{
    // Loaded only when Character Editor is active (LoadFolders
    // IfModActive="void.charactereditor"). Opening the CE editor shows a small
    // draggable "grant class" strip next to it; the button opens the full
    // class picker. The strip watches the vanilla window stack and closes
    // itself when the CE editor closes. No CE-internal UI patching — the
    // adapter only touches the public StartEditor entry and reads the editor
    // window from the vanilla stack, so CE updates cannot break it. Without
    // CE it never loads at all.
    [StaticConstructorOnStartup]
    internal static class CharacterEditorCompatStartup
    {
        internal static void OpenPicker()
        {
            if (Find.WindowStack.Windows.OfType<RWoMClassGrantWindow>().Any()) return;
            Find.WindowStack.Add(new RWoMClassGrantWindow());
        }

        static CharacterEditorCompatStartup()
        {
            try
            {
                var editorType = AccessTools.TypeByName("CharacterEditor.CEditor");
                var startEditor = AccessTools.Method(editorType, "StartEditor");
                if (editorType == null || startEditor == null)
                {
                    Log.Warning("[RWoM Integrations] Character Editor entry point not found; grant strip disabled.");
                    return;
                }
                var harmony = new Harmony("huginn.rwom.integrations.charactereditor");
                harmony.Patch(startEditor,
                    postfix: new HarmonyMethod(typeof(CharacterEditorCompatStartup),
                        nameof(CharacterEditorCompatStartup.EditorOpened)));
                Log.Message("[RWoM Integrations] Character Editor class-grant bridge active.");
            }
            catch (Exception error)
            {
                Log.Warning("[RWoM Integrations] Character Editor bridge failed to initialize: " + error);
            }
        }

        private static void EditorOpened() => OpenPicker();
    }
}
