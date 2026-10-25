[![CI](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-benchlink/ci.yml?branch=main&style=flat-square&label=CI&logo=github)](https://github.com/javidahmed64592/valheim-benchlink/actions/workflows/ci.yml)
[![Package](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-benchlink/package.yml?branch=main&style=flat-square&label=Package&logo=github)](https://github.com/javidahmed64592/valheim-benchlink/actions/workflows/package.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# BenchLink

Link nearby crafting stations into clusters so any bench's range counts for every bench type in the cluster.

In vanilla Valheim every crafting station only covers its own circle, and a piece that needs a Workbench can only be built inside a Workbench's circle.
Big bases end up with duplicate benches just to reach the far corners.
With BenchLink, benches whose ranges reach each other form a **linked cluster**, and inside a cluster every bench's circle counts for every bench type in that cluster.
A Workbench and a Forge close enough to link now let you build Workbench pieces anywhere the Forge reaches, and Forge pieces anywhere the Workbench reaches.

![Workbench and Forge](https://github.com/javidahmed64592/Valheim-BenchLink/raw/main/images/game_screenshot_1.png)
![Stonecutter](https://github.com/javidahmed64592/Valheim-BenchLink/raw/main/images/game_screenshot_2.png)

## How Linking Works

Each station has a build range, which grows with its upgrades.
Distances are measured on the ground, ignoring height, the same way vanilla does.
Two stations are linked when the distance between them is less than the larger of their two ranges (plus `LinkSlack`).
Links chain: if A links to B and B links to C, all three are one cluster, even if A and C are too far apart to link directly.

To build a piece that needs a station, you must be inside the circle of **any** station in a cluster that contains the needed station type.

| Station | Build range |
| ------- | ----------- |
| Workbench | about 20 m, +4 m per upgrade (up to 36 m) |
| Forge | about 20 m, +3 m per upgrade (up to 38 m) |
| Stonecutter | about 20 m |
| Artisan Table | about 40 m |
| Black Forge, Galdr Table | up to about 35 m |

## Range Indicator

While you have the Hammer out, BenchLink shows the build rings of every station in the cluster of each bench you are standing in range of.
These are the game's own bench rings, so they look like vanilla.
Rings of linked benches are tinted gold so you can see the cluster at a glance.

## Configuration

Settings are in the BepInEx config file `javidahmed64592.benchlink.cfg`.

| Setting | Default | Description |
| ------- | ------- | ----------- |
| `LinkSlack` | `0` | Extra metres (0 to 20) added when deciding if two stations link. Raise it to link benches a little further apart. |
| `Indicator` | `WhileBuilding` | `WhileBuilding` shows the rings above; `Off` removes them. |

## Console Command

`benchlink` prints the station clusters within 100 m of you: each member with its level, range and distance from you, which stations are linked and how far apart they are, and the area covered with and without linking.

## Known Limits

- Only loaded stations count. Stations in distant, unloaded zones are not part of any cluster until you get close.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/javidahmed64592/Valheim-BenchLink/blob/main/LICENSE) file for details.
