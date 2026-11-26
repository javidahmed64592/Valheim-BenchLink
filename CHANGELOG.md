# Changelog

## 1.0.1

- A workbench (or other station) still being placed no longer counts as a station in range. Before, selecting the workbench with the hammer let you remove pieces with no workbench nearby.
- No longer logs a warning about a missing LiberationSans SDF font.

## 1.0.0

- First stable release. The features are unchanged from 0.4.0.

## 0.4.0

- The minimap and the large map shade the area each loaded cluster covers: gold for linked clusters, white for stations on their own, with a brighter edge. It sits under the pins and pans and zooms with the map. Turn it off with the new `MapCoverage` setting under `[Look]`.
- The Shift+E markers now show each station's upgrade level next to its distance, such as "Level 3/5", so you can see how far every bench in the cluster is upgraded at once.
- Moved the source files into folders (`Core`, `Clusters`, `UI`, `Patches`), leaving only `Plugin.cs` at the root, with each Harmony patch in its own file under `Patches`. No behaviour change.

## 0.3.0

- `LinkSlack` is now synced from the server when it has the mod, so every player links benches the same way.
- Messages and hover text can now be translated (`Translations/English.json`).
- The README now says that only loaded stations link.
- Added a CI check that the version matches in every file.
- Moved `package.sh` into `.github/scripts` and made it the same for every mod: the name, version and DLL now come from `manifest.json` and the `.csproj`.

## 0.2.0

- Press **Shift+E** on any station to light up every station in its cluster, so you can find them. Each one glows gold and gets a marker on screen with its name and distance, through walls, clinging to the edge of the screen when the station is off to the side or behind you. A message says how many stations are linked, or that the station is a duplicate or on its own. Only you see it.
- The station hover text shows the Shift+E hint.
- New `HighlightDuration` setting under `[Look]`: how many seconds the highlight lasts (default 3).
- Bench range rings now lie on the floor around the bench instead of on the terrain under it, so on a raised floor or platform you can see them. They follow floors at the bench's level and steps up from it, but not roofs or upper storeys. This applies both to the game's own ring and to the range indicator.

## 0.1.1

- Updated the BepInExPack_Valheim dependency to 5.4.2360.
- Fixed the "cluster bench changed" message appearing after going through a portal. Changes are now only reported once the loaded stations have settled and differ from the last settled state.

## 0.1.0

- Nearby crafting stations form linked clusters, and any bench's build range counts for every bench type in its cluster.
- A cluster holds one station of each type, with messages when you place a duplicate or the cluster's bench changes.
- Range indicator while building: gold for linked benches, blue for the bench you look at.
- Added the `LinkSlack` and `Indicator` settings and the `benchlink` console command.
