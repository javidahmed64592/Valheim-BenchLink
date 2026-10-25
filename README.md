[![CI](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-benchlink/ci.yml?branch=main&style=flat-square&label=CI&logo=github)](https://github.com/javidahmed64592/valheim-benchlink/actions/workflows/ci.yml)
[![Package](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-benchlink/package.yml?branch=main&style=flat-square&label=Package&logo=github)](https://github.com/javidahmed64592/valheim-benchlink/actions/workflows/package.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# BenchLink

Link nearby crafting stations into clusters so one of each bench covers your whole base.
Inside a cluster, every bench's build range counts for every bench type in it.

![Workbench and Forge](https://github.com/javidahmed64592/Valheim-BenchLink/raw/main/images/game_screenshot_1.png)
![Stonecutter](https://github.com/javidahmed64592/Valheim-BenchLink/raw/main/images/game_screenshot_2.png)

## How It Works

Two stations link when they are closer than the larger of their build ranges, and links chain into clusters.
Build anywhere inside the range of **any** station in a cluster to use every station type in it.

A cluster holds **one station of each type**, so extra benches cannot be chained outwards.
A second bench of a type is not linked and works like vanilla, and you get a message when you place one.
The more upgraded bench keeps the cluster's slot, and on a tie the one built first does.
You get a message whenever the cluster's bench changes.

## Range Indicator

With the Hammer out, the rings of nearby benches are shown: gold if linked, white if not.
Look at a bench to turn its own ring blue.

## Configuration

Settings are in the BepInEx config file `javidahmed64592.benchlink.cfg`.
You can add extra metres to the link distance (`LinkSlack`) and turn the ring indicator off (`Indicator`).

The `benchlink` console command lists nearby clusters.
Only loaded stations count, and BenchLink only needs to be installed on your own game.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/javidahmed64592/Valheim-BenchLink/blob/main/LICENSE) file for details.
