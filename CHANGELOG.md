# Changelog

## 0.1.0

- Crafting stations whose build ranges reach each other form linked clusters, and any station's circle in a cluster counts for every station type in that cluster when building.
- A cluster can hold only one station of each type. A second station of a type that would join the cluster is left out and works like vanilla, so benches cannot be chained to extend a cluster.
- Placing a station that repeats a type in its cluster now shows a message saying it will not extend the cluster.
- Added the `LinkSlack` and `Indicator` settings.
- Added a build range indicator that shows the rings of linked stations while the Hammer is out.
- Added the `benchlink` console command to list nearby clusters.
