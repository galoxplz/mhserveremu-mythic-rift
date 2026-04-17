# Mythic Rift Admin Test Guide

## Goal

This guide is meant to let TAHITI admins test the current **Cosmic Rift Beacon** flow without any custom client-side vendor or launcher work.

Current player-facing identity:

- `Cosmic Rift Beacon`

Current technical base:

- `PortalToRandomDungeon`

## Current Testing Assumptions

- no manual client patch is required for the current server-side test flow
- the beacon is granted server-side
- the actual item still uses the game's existing `PortalToRandomDungeon` prototype as its technical base
- the current prototype remains admin-oriented while the final player entry flow is still being refined

## Preferred Direct Beacon Test

Use this when you want to validate the direct Cosmic Rift launcher path without changing normal Danger Room behavior globally.

1. Prepare the player:

```text
rift prepbeacon 1 1
```

2. Confirm the chosen launcher identity:

```text
rift beacon
rift validatecontent
```

3. Inspect the current beacon state:

```text
rift beaconmode
```

4. Use the granted `PortalToRandomDungeon` item in-game.

5. Inspect the launcher state and active run:

```text
rift beaconmode
rift runs
rift run [runId]
```

Expected result:

- the granted beacon item itself creates the Rift run directly on use
- `rift beaconmode` should report at least `trackedCosmicRiftBeaconCharges=1` before use
- `rift beaconmode` should report `teleportAttempted=true`
- `rift beaconmode` should report `teleportSucceeded=true`
- `rift beaconmode` should report the created `runId`, selected `content`, `region`, and `entryTarget`
- `rift run [runId]` should show a `bossSource` that may differ from the selected map content
- `rift run [runId]` should show `competitiveEligibility=bossUnlock:X | bossKill:Y` so admins can verify who qualified for the next-level unlock rule
- when the pool allows it, the random `bossSource` now avoids matching the selected map entry so admins can validate true map/boss mixing more easily
- `rift run [runId]` should show `regionScalingApplied=true` once the run is bound to the live region
- the tracked beacon charge should go down after a successful use
- beacon interception should still work even if the live server stacks or reuses `PortalToRandomDungeon` item instances differently than the local dev environment
- for competitive progression, only players who were inside the Rift at boss unlock and still inside the Rift at boss death should unlock the next difficulty
- the default `PortalToRandomDungeon` / Danger Room behavior is not changed globally for non-beacon use
- the run should emit in-game custom system messages when it starts, when the boss is unlocked, and when it completes or fails
- the beacon use itself should also emit an immediate in-game confirmation showing the selected map, boss, level, timer, and teleport result
- if the direct launch cannot enter the selected Rift terminal, the created pending run should now abort instead of staying stuck indefinitely
- if a run is created after the player has already entered the matching terminal, it should still bind when that terminal is an equivalent runtime variant of the configured region

Optional random preview before launching:

```text
rift previewrandom 5 1 1 10
```

This previews five random map/boss combinations without creating live runs.

Deep random-pool validation:

```text
rift validaterandompool 1 1 10
```

This validates every currently allowed random map/boss combination and reports any invalid pairings before in-game testing.

## Fixed Content Beacon Test

Use this when you want to validate a specific V1 terminal without relying on random selection.

Recommended content ids:

- `shocker`
- `doctor-octopus`
- `taskmaster`
- `hood`
- `magneto`
- `sinister`
- `modok`
- `mandarin`
- `kingpin`
- `ultron`

Example flow:

```text
rift prepbeacon 1 1
rift validatecontent
rift armbeaconfixed taskmaster 10
rift beaconmode
```

Then:

1. Use the granted `PortalToRandomDungeon` item in-game.
2. Inspect the result:

```text
rift beaconmode
rift runs
rift run [runId]
```

Expected result:

- `rift beaconmode` should show `fixedContent=taskmaster` while armed
- after item use, `rift beaconmode` should show the created `runId`
- the run should report `content=taskmaster`
- the run should report `bossSource=taskmaster`
- teleport should target the `entryTarget` resolved for the selected terminal
- normal unarmed Danger Room behavior should remain unchanged globally

## Legacy Intent-Based Smoke Test

Use this when you want to validate the older server-side intent capture flow.

1. Prepare the player:

```text
rift prepbeacon 1 1
```

2. Use the granted `PortalToRandomDungeon` item in-game.

3. Confirm that the launcher intent was captured:

```text
rift itemintent
```

4. Convert the intent into a Rift run using the highest unlocked level and a 10-minute timer:

```text
rift consumeintentauto 10
```

5. Inspect the active run:

```text
rift runs
```

## Slightly More Controlled Test

Use this when you want to validate a specific unlocked level or choose the timer manually.

1. Unlock a target level and grant one beacon:

```text
rift prepbeacon 5 1
```

2. Use the granted item in-game.

3. Check that the pending intent exists:

```text
rift itemintent
```

4. Consume the intent manually:

```text
rift consumeintent 5 12
```

This creates a run at Rift level 5 with a 12-minute timer.

## Terminal Test Flow

Once a run has been created, the current server prototype can already bind to a real terminal region and track progress.

Recommended flow:

1. Create or consume a run.
2. Enter the expected terminal region with the testing character.
3. Let the server auto-bind the run when the participant enters the correct target region.
4. Kill enemies until the quota is reached.
5. Kill the expected boss.
6. Inspect the run result.

Additional validation for the current random-boss implementation:

- use `rift run [runId]` after the run is bound
- confirm that the selected `bossSource` is present in the run details
- confirm that the run only completes when the spawned/configured Rift boss dies after the quota
- confirm that ordinary prototype-matching enemies killed before quota do not complete or corrupt the run

## Forced Map/Boss Mix Test

Use this when you want to force a specific terminal map with a different terminal boss for debugging.

Example:

```text
rift createmix taskmaster ultron 1 1 50 10
rift bind [runId]
rift start [runId]
rift run [runId]
```

Expected result:

- the run should report `content=taskmaster`
- the run should report `bossSource=ultron`
- once the quota is reached, the spawned/configured Ultron boss should be the one that completes the run

## Progression Persistence Test

Use this when you want to confirm that Rift progression survives saving and relogging.

1. Prepare a player at Rift level 1:

```text
rift prepbeacon 1 1
```

2. Use the beacon and create a level 1 run:

```text
rift itemintent
rift consumeintentauto 10
```

3. Complete the run successfully.

4. Confirm that level 2 is now unlocked:

```text
rift access 2
```

5. Trigger a normal save/relog cycle for the player.

6. After logging back in, confirm that level 2 is still unlocked:

```text
rift access 2
```

7. Grant another beacon and consume it again:

```text
rift givebeacon 1
rift itemintent
rift consumeintentauto 10
```

Expected result:

- the new run should use the player's persisted highest unlocked Rift level
- the player should be able to chain into higher difficulty runs across sessions

## Multi-Rift Chain Test

Use this when you want to validate the intended loop: complete a Rift, unlock the next level, and chain into the next one cleanly.

1. Prepare the player:

```text
rift prepbeacon 1 3
rift progression
```

2. Use one beacon and create the first run:

```text
rift itemintent
rift consumeintentauto 10
rift progression
```

3. Complete the run successfully.

4. Confirm that level 2 is unlocked:

```text
rift access 2
rift progression
```

5. Use a second beacon and consume it again.

6. Complete the second run successfully.

7. Confirm that level 3 is unlocked:

```text
rift access 3
rift progression
```

Expected behavior:

- each completed run unlocks the next Rift level
- the next beacon consumption uses the player's highest unlocked level
- a player or party cannot create a second pending or active Rift run while one is already in progress

## Session Safety and Cleanup Behavior

The current prototype now includes two automatic safety behaviors:

- if all tracked participants are offline for more than 2 minutes, an in-progress Rift run is aborted automatically
- completed or aborted runs are removed automatically after a 5-minute retention window

This helps keep the server clean during repeated test sessions and long uptimes.

Recommended validation:

1. Start a Rift run.
2. Disconnect all tracked participants.
3. Wait a little over 2 minutes.
4. Reconnect and inspect the run list.

Expected result:

- the run should no longer remain indefinitely in a pending or active state
- the server should avoid building up stale Mythic Rift runs over time

## Group Continuity Rules

Current intended behavior, matching the project direction:

- if the leader disconnects, the Rift remains valid
- no special punishment or reset happens because the leader left
- the disconnected player can simply remain outside the Rift
- the run stays valid as long as the remaining players kill enough enemies to unlock the boss and then kill the boss
- players currently present in the bound Rift region continue to be folded into run participation tracking while the run is active

Practical consequence:

- a group can keep pushing the Rift even if party composition changes mid-run
- success is determined by completing the Rift objective, not by preserving the original party shape

Useful commands during this phase:

```text
rft beaconmode
rft armbeacon [minutes]
rft disarmbeacon
rift progression
rift runs
rift run [runId]
rift tick [runId]
```

If a manual bind is needed during debugging:

```text
rift bind [runId]
rift start [runId]
```

## Useful Commands

Identity and launcher:

```text
rift beacon
rft beaconmode
rft armbeacon [minutes]
rft disarmbeacon
rift entrypoints
rift launchcandidates
rift launchplan portal-to-random-dungeon
```

Player preparation:

```text
rift prepbeacon [level] [count]
rift givebeacon [count]
rift access [level]
rift progression
rift setaccess [level]
```

Launcher flow:

```text
rift itemintent
rift consumeintent [level] [minutes]
rift consumeintentauto [minutes]
```

Run inspection:

```text
rift runs
rift run [runId]
```

Fallback admin testing:

```text
rift requestportal [level] [minutes]
rift requestfixedauto taskmaster 1 10
rift requestfixedauto hood 1 10
rift requestfixedauto sinister 1 10
rift requestfixedauto kingpin 1 10
```

## What Admins Should Expect

At the current stage, this prototype already supports:

- a server-side launcher identity built around `Cosmic Rift Beacon`
- server-side granting of the beacon item
- capture of a launcher intent when the item is used
- conversion of that intent into a Rift run
- level access validation
- automatic use of the player's highest unlocked Rift level
- a default 10-minute launcher timer
- terminal-region binding
- kill quota tracking
- boss unlock and boss completion logic
- success/failure reward resolution

## Current Limitations

- the visible item name in the client is still tied to the base game prototype unless later changed through patcher-delivered game files
- there is no final capital NPC or interactable yet
- progression now persists in the Player save data, but still needs broader long-session testing
- the current launcher flow is meant for admin testing and iteration, not final player UX

## Why This Is TAHITI-Friendly

- no manual client patch is required for the current test flow
- no custom client-side vendor is required
- no database migration is required at this stage
- the implementation is localized and reviewable as a server patch
- if later game-file work is desired, it can be discussed separately as a patcher-delivered enhancement rather than a prerequisite
