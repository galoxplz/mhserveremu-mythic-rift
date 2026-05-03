# Mythic Rift Admin Test Guide

## Goal

This guide is meant to let TAHITI admins test the current **Cosmic Rift Beacon** flow without any custom client-side vendor or launcher work.

Current player-facing identity:

- `Cosmic Rift Beacon`

Current technical base:

- `PortalToRandomMaxAffixDungeon`

## Current Testing Assumptions

- no manual client patch is required for the current server-side test flow
- the beacon can now be obtained either from a Danger Room hub vendor or from a server grant command
- the actual item now prefers the game's `PortalToRandomMaxAffixDungeon` prototype as its technical base
- `PortalToRandomDungeon` remains accepted as a legacy compatibility fallback for older test flows
- because both prototypes appear to be `DesignState: DevelopmentOnly`, TAHITI should patch the chosen live test item to `DesignState=Live`
- `PortalToRandomMaxAffixDungeon` is treated as the dedicated preferred launcher base, so it can launch a Rift even if the live server creates or clones the purchased item without preserving the previous in-memory tracking entry
- `rift diagbeacon` is a server-side prerequisite check only; it does not prove the final client click path, so live item-click tests should also check the `[MythicRiftLauncher]` logs if the item still reaches native Danger Room behavior
- the current prototype remains admin-oriented while the final player entry flow is still being refined

## Preferred No-Admin Vendor Test

Use this when you want to validate the newest server-only player flow with no admin item grant.

1. Move a character to the `Danger Room` hub.
2. Open a vendor there.
3. Confirm that the vendor now offers one injected `PortalToRandomMaxAffixDungeon`-based launcher entry for this player.
4. Buy the item from that vendor.
5. Use the purchased item in-game.
6. Inspect the launcher state and active run:

```text
rift beaconmode
rift status
rift diagbeacon 1 10
rift runs
rift run [runId]
rift enter [runId]
```

Expected result:

- no admin grant command is required to obtain a test launcher item
- the current seller pass is intentionally scoped to `Danger Room` hub vendors rather than a single hard-locked NPC
- this keeps the implementation server-only and easy for TAHITI to iterate before they choose the permanent seller
- after purchase, the item should launch through the Mythic Rift path exactly like a server-granted beacon
- after a committed Rift launch, the purchased launcher item should be consumed
- `rift status` should show the invoking player's active Rift without needing the admin-only `runId` list
- admins can use `rift enter [runId]` to teleport into a registered Rift run for inspection; if the run is already bound, it enters the existing region instance, otherwise it enters the configured start target and lets auto-bind attach the run
- player-facing chat should describe the Rift at a high level: map, level, timer, and enemy quota; the random boss name is revealed when the quota is completed and the boss is summoned
- the active random test pool still excludes `Ultron Terminal` and `Magneto / Stryker Bunker`, but both are now registered on their L60 terminal variants for fixed validation
- the random map pool now also includes a first curated set of non-terminal private combat maps; these are map-only entries, so bosses still come from the validated terminal boss pool
- `Cosmic Doop Sector` is registered as a special Rift map with a dedicated fixed boss and a 5% random selection chance; it does not use the normal random boss pool
- the server hides terminal-native objective HUD widgets during active Rift runs so a map like Fisk Tower should no longer keep showing a misleading native objective such as "defeat Kingpin" when the Rift boss is different
- the server temporarily suspends the native terminal mission while the Rift is active, so the normal terminal objective tracker should be hidden rather than modified
- the server temporarily suspends active `Region Events` missions inside the Rift instance, because the lighter client-side-only suppression did not hide that tracker reliably; this is scoped to the Rift region and restored when the run is cleaned up
- the server intercepts native `Mission` / `MissionObjective` updates for controlled terminal objectives before they reach the client, so terminal bounty counters should not reappear after the Rift starts
- if the client keeps a generic counter widget visible, the server forces that counter to the active Rift kill quota; chat messages and `rift status` remain the authoritative no-client-patch fallback
- the launcher now uses the configured Rift region during teleport instead of trusting the native target region baked into some terminal start targets; current terminal entries prefer `AltRegions/*RegionL60` to avoid `RegionBand` drift
- successful Rift clears should spawn a return portal that takes players back to the Danger Room hub
- completed or aborted Rift regions now request shutdown once they become vacant, so re-entering the same terminal later should create a fresh instance instead of reusing stale kill-state
- guaranteed chat timer warnings are now sent at 9, 8, 7, 6, 5, 4, 3, 2, and 1 minute remaining, plus 30 seconds remaining
- guaranteed chat kill-progress messages are sent at 25%, 50%, and 75% enemy quota progress
- the purchased launcher is now intercepted at top-level item use, so vendor-bought `PortalToRandomMaxAffixDungeon` variants do not need to rely on reaching the exact `UsePower` branch before Rift launch begins
- if the client sends the item's `OnUsePower` directly without a reliable item source id, the server now searches the player's inventory for an owned `PortalToRandomMaxAffixDungeon` and intercepts that activation before native Danger Room scenario logic runs
- the same vendor purchase flow still recognizes legacy `PortalToRandomDungeon` stock added through TAHITI patcher files, not only items injected by the server-side seller pass
- once the final seller is chosen, this region-scoped seller pass can be narrowed to that specific vendor with a small follow-up patch
- `rift diagbeacon 1 10` can now be used as a server-side self-check before live clicking the item, to verify prototype resolution, vendor item spec creation, temporary owned-item usability, launcher recognition, and Rift request conversion without depending on a successful client click
- if the item still opens `DRRegionUniqueTutorialFight` or another native Danger Room scenario, capture the server log lines containing `[MythicRiftLauncher]`; those lines now show whether the click was intercepted, which item id/prototype was found, and whether the fallback path saw the chosen beacon power
- if a native objective tracker still remains visible during the Rift, run `rift objectives` while standing in the Rift region and capture the output; it lists active region/player missions, objective widgets, and the region UI data provider so any remaining non-standard tracker source can be identified

Player-selected level test:

```text
rift status
rift level
rift level 1
rift level max
```

Expected result:

- `rift status` shows the highest unlocked level and the next beacon launch level when no Rift is active
- `rift level` shows the current selected launch level and the highest unlocked level
- `rift level [number]` sets the next beacon launch level only if that level is already unlocked
- `rift level max` returns the next beacon launch level to the player's highest unlocked level
- the next purchased or granted beacon uses the selected launch level, not always the highest unlocked level

Player-facing stop test:

```text
rift abandon
```

Expected result:

- solo players can abandon their own active Rift without an admin knowing the `runId`
- any participant can intentionally abandon the active Rift; this cancels the Rift for the run because leaving costs the key/run attempt
- online run participants are returned to the Danger Room hub
- the run is aborted and removed immediately so the player or party can start another fresh Rift

Natural leave / town teleport test:

1. Buy and launch a Beacon.
2. Kill some enemies, but do not finish the Rift.
3. Use a normal return-to-town / hub teleport.
4. Run:

```text
rift status
rift runs
```

Expected result:

- leaving the bound Rift region before completion closes the Rift attempt
- the player who already returned to town stays outside the Rift
- any online participants still inside the Rift are returned to the Danger Room hub
- the run is removed so a fresh Beacon can start a new Rift
- chat should clearly say that the Rift closed because someone left before completion and that a new Beacon is required

Timer expiration test:

1. Launch a Rift with a very short timer from an admin command, or wait for the normal timer to expire.
2. Do not kill the Rift boss before expiration.
3. Run:

```text
rift status
rift runs
```

Expected result:

- the Rift fails when the timer expires
- the player receives a clear failure message in chat
- online participants still inside the Rift are returned to the Danger Room hub automatically
- `rift status` should no longer show an active Cosmic Rift for the player

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

4. Use the granted `PortalToRandomMaxAffixDungeon` item in-game.

5. Inspect the launcher state and active run:

```text
rift beaconmode
rift runs
rift run [runId]
rift enter [runId]
```

Expected result:

- the granted beacon item itself creates the Rift run directly on use
- `rift beaconmode` should report at least `trackedCosmicRiftBeaconCharges=1` before use
- `rift diagbeacon 1 10` should report `diagResult=ok` before the live in-game click if the current server-side beacon setup is healthy
- `rift beaconmode` should report `teleportAttempted=true`
- `rift beaconmode` should report `teleportSucceeded=true`
- `rift beaconmode` should report the created `runId`, selected `content`, `region`, and `entryTarget`
- `rift run [runId]` should show a `bossSource` that may differ from the selected map content
- `rift run [runId]` should show `competitiveEligibility=bossUnlock:X | bossKill:Y` so admins can verify who qualified for the next-level unlock rule
- when an alternative exists in the random pool, the next random Rift should avoid immediately repeating the last terminal map played by the requester or party members
- when the pool allows it, the random `bossSource` now avoids matching the selected map entry so admins can validate true map/boss mixing more easily
- the terminal's native linked boss should be suppressed by the server during the Rift so players cannot kill the normal terminal boss before the quota
- the only boss that should complete the Rift is the configured/spawned Rift boss, and only after the enemy quota is complete
- `rift run [runId]` should show `regionScalingApplied=true` once the run is bound to the live region
- the tracked beacon charge should go down after a successful use
- beacon interception should still work even if the live server stacks or reuses `PortalToRandomMaxAffixDungeon` item instances differently than the local dev environment
- once Mythic Rift has intercepted the click, the item should no longer fall through into any normal scenario result for that same use
- for competitive progression, only players who were inside the Rift at boss unlock and still inside the Rift at boss death should unlock the next difficulty
- legacy `PortalToRandomDungeon` / Danger Room behavior is not changed globally for non-beacon use
- the run should emit in-game custom system messages when it starts, when the boss is unlocked, and when it completes or fails
- the beacon use itself should also emit an immediate player-facing confirmation showing the selected map, boss, level, and time limit
- after the quota and Rift boss are completed, a normal town/hub teleport should not invalidate the run because success is already recorded at boss death
- if the direct launch cannot enter the selected Rift terminal, the created pending run should now abort instead of staying stuck indefinitely
- if a run is created after the player has already entered the matching terminal, it should still bind when that terminal is an equivalent runtime variant of the configured region
- if a run becomes impossible or a tester wants to stop it manually, admins can use `rift abort [runId]`

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

Use this when you want to validate a specific V1 terminal or curated non-terminal map without relying on random selection.

Recommended terminal content ids:

- `shocker`
- `doctor-octopus`
- `taskmaster`
- `hood`
- `sinister`
- `modok`
- `mandarin`
- `kingpin`

Registered but intentionally not random-selected right now:

- `magneto`
- `ultron`

Curated non-terminal map ids:

- `bronx-zoo`
- `wakanda-jungle`
- `hydra-island-one-shot`
- `daily-bugle`
- `dr-strange-times-square`

Special low-chance map id:

- `cosmic-doop-sector`

Example flow:

```text
rift prepbeacon 1 1
rift validatecontent
rift armbeaconfixed taskmaster 10
rift beaconmode
```

Then:

1. Use the granted `PortalToRandomMaxAffixDungeon` item in-game.
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
- terminal fixed-content runs should report the selected terminal as the boss source
- non-terminal fixed-content runs should report the selected map as `content`, with a separate terminal `bossSource`
- for party tests, the Rift should no longer auto-close immediately just because another party member is still zoning; only a player who has actually been seen inside the Rift can now trigger the "participant left early" failure path
- teleport should target the `entryTarget` resolved for the selected terminal
- normal unarmed Danger Room behavior should remain unchanged globally

## Legacy Intent-Based Smoke Test

Use this when you want to validate the older server-side intent capture flow.

Important:

- if direct beacon interception is working, `rift itemintent` may correctly report no pending intent
- in that case, use `rift beaconmode` to inspect the direct launcher result instead

1. Prepare the player:

```text
rift prepbeacon 1 1
```

2. Use the granted `PortalToRandomMaxAffixDungeon` item in-game.

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
5. Confirm the Rift boss is summoned only after quota completion, then kill that spawned Rift boss.
6. Inspect the run result.

Additional validation for the current random-boss implementation:

- use `rift run [runId]` after the run is bound
- confirm that the selected `bossSource` is present in the run details
- confirm that the terminal's native linked boss is not available as an early completion target
- confirm that the run only completes when the spawned/configured Rift boss dies after the quota
- confirm that ordinary prototype-matching enemies killed before quota do not complete or corrupt the run

## Forced Map/Boss Mix Test

Use this when you want to force a specific terminal map with a different terminal boss for debugging.

Example:

```text
rift createmix taskmaster mandarin 1 1 50 10
rift bind [runId]
rift start [runId]
rift run [runId]
```

Expected result:

- the run should report `content=taskmaster`
- the run should report `bossSource=mandarin`
- once the quota is reached, the spawned/configured Mandarin boss should be the one that completes the run

## Non-Terminal Map Smoke Tests

Use these to force each new curated map directly through the same beacon flow players will use. The command arms the next beacon click for a fixed map, while the boss still comes from the terminal boss pool.

```text
rift validatecontent
rift validaterandompool 1 1 10
rift prepbeacon 1 5
rift armbeaconfixed bronx-zoo 10
```

Use one beacon, finish or abandon the run, then repeat with:

```text
rift armbeaconfixed wakanda-jungle 10
rift armbeaconfixed hydra-island-one-shot 10
rift armbeaconfixed daily-bugle 10
rift armbeaconfixed dr-strange-times-square 10
```

Special Doop Rift direct test:

```text
rift armbeaconfixed cosmic-doop-sector 10
```

For each map, check:

- the player teleports into the named map instead of a terminal
- `rift beaconmode` shows `teleportSucceeded=true`
- normal non-terminal maps show `content=[map id]` and a terminal `bossSource`
- `cosmic-doop-sector` shows `content=cosmic-doop-sector` and `bossSource=cosmic-doop-sector`
- enemy kill count progresses from the native population in that map
- the random Rift boss spawns only after quota completion
- for `cosmic-doop-sector`, the fixed boss should be `CosmicDoopOverlord`
- for `cosmic-doop-sector`, the current quota is `100` because a 35-kill version was completed in roughly 15 seconds during local testing
- completion, abandon, and timer failure clean the instance so a later run starts fresh

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

2. Use one beacon in-game and inspect the created run:

```text
rift beaconmode
rift runs
rift progression
```

Important:

- when direct beacon interception is active, do not use `rift consumeintentauto`
- the intended flow is now direct item use -> auto-created Rift run -> auto-teleport to the selected terminal
- `rift itemintent` is only for the older legacy intent path or for smoke-testing fallback behavior

3. Complete the run successfully.

4. Confirm that level 2 is unlocked:

```text
rift access 2
rift progression
```

5. Use a second beacon in-game.

6. Complete the second run successfully.

7. Confirm that level 3 is unlocked:

```text
rift access 3
rift progression
```

8. If you launch the next random Rift while still standing inside the previous terminal, confirm that the next selected map is not that same terminal when another map is available.

Expected behavior:

- each completed run unlocks the next Rift level
- the next beacon use resolves to the player's highest unlocked level automatically
- the current intended loop is one beacon use per Rift run, not one beacon chaining multiple terminals by itself
- a player or party cannot create a second pending or active Rift run while one is already in progress

## Current Tuning Notes

These notes are important when reviewing test-center feedback.

- the current updated build is intended to keep successful beacon clicks inside the Mythic Rift flow instead of falling back into a normal Danger Room result
- the current updated build also excludes the requester's current terminal region and last completed terminal map from the next random pick when alternatives exist
- if a tester reports "sometimes it turned back into a regular Danger Room" or "I got the same dead terminal again with no mobs", first confirm they were on the newest build
- boss loot is still inherited from the reused terminal boss loot tables for now, so observations such as cube shard drops are expected at this stage
- this means the current prototype validates gameplay flow first, not final reward identity
- random enemy replacement is not implemented yet; terminal populations are still native for now because replacing them server-side needs a separate safety pass against map scripts and mission logic
- the current frozen test tuning now keeps the D3 percentage logic but compresses one Mythic Rift level into `0.40` D3 Greater Rift levels so Marvel terminals scale more realistically
- group health scaling is now explicitly locked to `1x / 2x / 3x / 4x` for `1 / 2 / 3 / 4-5` players
- `rift scale [level] [players]` and `rift run [runId]` now expose `d3EquivalentLevel` and `groupHealth` so admins can inspect the applied snapshot directly
- very high Rift levels are still expected to be aspirational rather than everyday test targets
- practical gameplay and multiplayer validation should focus first on lower and mid levels such as `1`, `5`, `10`, and `25`, then `50` for stretch testing
- if a run becomes impossible during testing, admins should abort it with `rift abort [runId]` instead of forcing a relog loop
- for player-like testing, prefer `rift abandon`; keep `rift abort [runId]` and `rift remove [runId]` as admin cleanup tools

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
rift beaconmode
rift armbeacon [minutes]
rift disarmbeacon
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
rift beaconmode
rift armbeacon [minutes]
rift disarmbeacon
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
rift status
rift abandon
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
- player-visible active Rift status through `rift status`
- player abandon flow through `rift abandon`; leaving cancels the active Rift and returns online participants to the Danger Room hub
- automatic closure when a tracked participant leaves the active Rift region before completion

## Current Limitations

- the visible item name in the client is still tied to the base game prototype unless later changed through patcher-delivered game files
- there is no final capital NPC or interactable yet
- progression now persists in the Player save data, but still needs broader long-session testing
- the current launcher flow is meant for admin testing and iteration, not final player UX

## Why This Is TAHITI-Friendly

- no manual client patch is required for the current test flow
- no custom client-side vendor is required
- the newest server build can now expose the launcher directly from a Danger Room hub vendor for no-admin flow testing
- no database migration is required at this stage
- the implementation is localized and reviewable as a server patch
- if later game-file work is desired, it can be discussed separately as a patcher-delivered enhancement rather than a prerequisite
