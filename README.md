# MHServerEmu Mythic Rift Prototype

This repository is a custom development fork focused on building a new endgame mode for **Marvel Heroes Omega**, inspired by **Diablo 3 Greater Rifts**.

The current target is a clean, reviewable prototype that can eventually be proposed as a **server-side-first patch** for TAHITI, with minimal client impact and no dependency on a manually distributed client patch.

## Project Goal

The goal of this project is to create a replayable Rift-like activity with:

- `1 to 5` player support
- random map selection
- random boss selection
- timed runs
- potentially infinite difficulty progression
- unlock of the next level on success
- rewards based on existing boss/world loot with a timed completion `SIF/RIF` bonus

## Design Direction

The current implementation is built around these constraints:

- keep the core logic server-side whenever possible
- reuse existing terminals, maps, bosses, missions, and scaling systems
- keep changes isolated and readable for future review
- remain compatible with a future TAHITI deployment strategy
- avoid dependence on a manual client patch
- if game file changes are ever needed, prefer a patcher-compatible approach

## Current Status

The repository already contains an in-progress Mythic Rift prototype in `src/MHServerEmu.Games/MythicRifts`.

Implemented pieces already include:

- run creation and tracking
- D3-inspired level scaling
- timer and expiration handling
- kill quota tracking
- boss unlock logic
- boss kill completion logic
- reward preparation and timed bonus handling
- in-memory progression unlocks
- admin/debug commands for local testing

## Project Documents

The working project documents are stored outside this repository in the local project workspace:

- French working docs: `C:\Users\admin\Desktop\PROJECT MHO\Docs`
- English shareable docs: `C:\Users\admin\Desktop\PROJECT MHO\Docs EN`

The most relevant documents for project review are:

- `PROJET-MYTHIC-RIFT-HIGH-LEVEL-EN.txt`
- `SPEC-V1-MYTHIC-RIFT-EN.txt`
- `ARCHITECTURE-SERVEUR-MYTHIC-RIFT-EN.txt`
- `ETAT-IMPLEMENTATION-MYTHIC-RIFT-EN.txt`

## Technical Base

This project is built on top of **MHServerEmu**, a server emulator for Marvel Heroes.

The currently targeted client version remains:

- `1.52.0.1700`
- also known as `2.16a`
- released on September 7, 2017

Original upstream project:

- [Crypto137/MHServerEmu](https://github.com/Crypto137/MHServerEmu)

Upstream documentation:

- [docs/Index.md](./docs/Index.md)

## Local Build Note

In this workspace, the safest build flow is to redirect `bin/obj` outputs outside the repository, for example into:

- `C:\Users\admin\Documents\Codex\build`

Reference command:

```powershell
dotnet build C:\Users\admin\Desktop\PROJECT MHO\MHServerEmu-master\src\MHServerEmu\MHServerEmu.csproj -c Release -p:BaseIntermediateOutputPath=C:\Users\admin\Documents\Codex\build\obj-cli\ -p:BaseOutputPath=C:\Users\admin\Documents\Codex\build\bin-cli\
```
