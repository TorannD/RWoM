# RWoM Core · RimWorld 1.7

[Русское руководство](README.ru.md) · [Developer API](API.md) · [Integration checklist for AI agents](AGENTS.md)

## Installing and playing

Enable Harmony, **RWoM Core (Ability Framework)**, then **A RimWorld of Magic (Continued)** and your class add-on. The Core package ID is `huginn.rwom.core`. Core supplies an API. The main mod supplies classes, the grimoire and the gameplay that awards spells. Core alone does not give a pawn mana or a class.

The main mod can run without Core. An add-on that directly references TorannMagic.Core.dll or uses its XML extension needs Core enabled. Install one copy of Core. Restart RimWorld after changing the load order or replacing an assembly.

## Adding a spell

1. Define the ability in the mod that implements its gameplay.
2. Give it a permanent, namespaced ID, for example `myaddon.glassward`. Add the [XML fragment](Examples/SpellIdentity.xml.txt) inside the existing ability Def. If it already has modExtensions, append the li to that list.
3. Declare the Core dependency in your add-on's About.xml. Use the [developer guide](API.md) for the project reference and dependency snippet.
4. After Def loading, resolve that ID and compare the returned Def to the one you authored. A missing Def should disable just that feature.
5. Keep the ID when renaming the Def. Test loading a copy of an existing save. Changing the ID is a data migration.

## Troubleshooting

* **XML cannot find SpellIdentityExtension.** Check Core is active and loaded before the add-on. Check the full class name against the XML example.
* **Lookup returns false.** Check capitalization, active expansions, the add-on's own load errors and whether Def loading finished. The grimoire may still have a saved record of an absent spell.
* **Duplicate spell ID warning.** The warning identifies the competing Def names. Give unrelated abilities distinct IDs. Fix this in the add-on before distributing a save migration.
* **Pawn has no resource.** Check the pawn actually has the corresponding need. The resource API does not create needs or initialize classes.

When reporting an integration issue include the game build, Core and add-on versions, load order, spell ID, Def name, whether this was a new game or an existing save, and the first related Player.log message.
For learned spells, ranks and the saved autocast toggle, see PawnSpellKnowledge.Read in the [API guide](API.md).
The API guide also covers hybrid and classic class identities and cast admission for external AI.
