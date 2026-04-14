# Mythic Rift High-Level

## Status

- High-level framing document.
- Working basis for a local prototype and then a clean patch proposal for TAHITI server integration.

## Vision

- Create an endgame activity inspired by Diablo 3 Greater Rifts.
- The content must be replayable, scalable, timed, and suitable for solo play as well as small groups.
- The system should reuse as many existing Marvel Heroes and MHServerEmu building blocks as possible.

## Product Goal

- Add an instanced Rift-like game mode.
- The content is playable from 1 to 5 players.
- The player interacts with an in-game entry point.
- The exact entry point is not defined yet.
- On activation, the system selects a random map and a random boss.
- The target feeling is close to Diablo 3 rifts.

## High-Level Specs

- 1 to 5 player content.
- Activation by clicking/interacting with a location or object not yet defined.
- Random map selection.
- Random boss selection.
- Timed content.
- Exact timers will be defined later.
- Potentially infinite difficulty progression.
- Example breakpoints: 1, 2, 3, ..., 100, 200, etc.
- Completing a level unlocks the next one.

## Reward Philosophy

- Timed completion should grant loot.
- The core of the loot should remain close to the existing game economy.
- Priority goes to reusing random world drops.
- Priority also goes to reusing the selected boss's normal drops.
- The additional reward should mainly come from a significant SIF / RIF bonus.

## Product Constraints

- The content must work for solo and group play.
- It must reuse the game's existing scalability whenever possible.
- It must be possible to propose the feature to TAHITI as a clean patch.
- It must not rely on a manually distributed client patch.
- The core of the mode should stay server-side as much as possible.
- If game file changes are required, they should ideally be compatible with TAHITI's Patcher.
- Changes must be isolated, readable, and documented.
- Local testing must be possible before any integration proposal.

## Recommended MVP

- A single entry point.
- A limited first pool of maps.
- A limited first pool of bosses.
- A simple timer.
- A simple level progression system.
- An end reward based on existing loot plus SIF/RIF bonus.
- No complex affixes in V1.
- No heavy custom UI in V1.
- No dependency on a brand-new client UI for the mode to be playable.
