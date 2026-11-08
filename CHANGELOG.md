# Changelog

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
