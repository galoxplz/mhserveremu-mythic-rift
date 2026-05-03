# TAHITI Mythic Rift Handover Procedure

## Goal

This document is meant to help present the current **Cosmic Rift** prototype to TAHITI leadership and give admins a clear, practical way to review and test it.

It is written for a technical reviewer such as MonEll who needs to understand:

- what the feature is
- what branch to look at
- what is already working
- how to build it
- how to test it
- what is still incomplete

## One-Sentence Summary

This is a server-side endgame prototype inspired by Diablo 3 Greater Rifts, built on top of existing Marvel Heroes Omega terminal content, using `PortalToRandomMaxAffixDungeon` as the current preferred technical launcher base, keeping `PortalToRandomDungeon` as a compatibility fallback, and `Cosmic Rift Beacon` as the planned player-facing identity.

## Recommended Way To Present It

Suggested structure when introducing it:

1. Explain that it is intentionally server-first.
2. Explain that the current test flow does not require a manual client patch.
3. Explain that it reuses existing terminal content rather than inventing a fully custom dungeon stack from scratch.
4. Explain that the current launcher identity is `Cosmic Rift Beacon`, but the visible in-game item is now preferably based on `PortalToRandomMaxAffixDungeon`, with `PortalToRandomDungeon` still supported as a legacy compatibility path.
5. Explain that this is now a reviewable prototype milestone, not just a design concept.

## Suggested Intro Message

You can present it roughly like this:

```text
This branch contains the current reviewable prototype for Cosmic Rift, a Diablo 3 Greater Rift-style endgame mode adapted to Marvel Heroes Omega.

The implementation is intentionally server-first and localized. The current test flow does not require a manual client patch and can be exercised through server-side commands plus existing game behavior.

The current launcher model uses PortalToRandomMaxAffixDungeon as the preferred technical base, with PortalToRandomDungeon kept for compatibility and Cosmic Rift Beacon as the intended player-facing identity later on.

At this stage, the prototype already supports timed runs, kill quota before boss unlock, boss completion, reward resolution, persistent Rift progression, chained higher-difficulty runs, server-side beacon granting, and basic group/session safety behavior.

I put together a compact review bundle and an admin test guide so you can inspect and test the feature without needing extra explanation from me each time.
```

## Repository And Branch

Repository:

- `https://github.com/galoxplz/mhserveremu-mythic-rift`

Branch to review:

- `codex/mythic-rift`

Current review target:

- latest head of `codex/mythic-rift`

## Best Docs To Share First

Start with these:

- `docs/MythicRift/TAHITI-Review-Bundle.md`
- `docs/MythicRift/Admin-Test-Guide.md`
- `docs/MythicRift/Implementation-Status.md`

If they want more design detail:

- `docs/MythicRift/Architecture.md`
- `docs/MythicRift/High-Level.md`
- `docs/MythicRift/Spec-V1.md`

## What Is Already Working

Current implemented prototype behavior:

- 1 to 5 player Rift runs
- random or fixed terminal selection from the current V1 pool
- D3-inspired level scaling
- kill quota before boss unlock
- boss death completion logic
- timed success / failed run handling
- end reward resolution
- timed success SIF/RIF bonus
- server-side granting of the beacon item
- direct beacon use in-game from a server-granted `PortalToRandomMaxAffixDungeon`
- direct beacon use in-game from vendor-bought or patcher-added `PortalToRandomMaxAffixDungeon` stock, even when the live server does not preserve an in-memory tracked charge for that exact item instance
- default Danger Room hub vendor stock injection for the current beacon, so the basic test flow no longer requires an admin item grant
- launcher item consumption after a committed Rift launch
- random map plus separately selected random terminal boss source
- suppression of the terminal's native linked boss during active Rift runs, so the normal terminal boss cannot be killed before quota or used as the completion target
- strict quota-gated boss completion: the spawned/configured Cosmic Rift boss is the valid completion boss only after the enemy quota is met
- suppression of terminal-native objective HUD widgets during active Rift runs, so a randomized boss does not leave players with misleading native terminal text such as "defeat Kingpin"
- temporary server-side suspension of the native terminal mission inside the Rift instance, restored when the run is removed, so the normal terminal objective tracker does not compete with the Rift objective
- temporary server-side suspension of active `Region Events` missions inside the Rift instance, restored when the run is removed, because client-side-only suppression did not hide that tracker reliably
- interception of native `Mission` / `MissionObjective` update packets for controlled terminal objectives while a Rift is active, so terminal bounty counters do not rebuild on the client after suppression
- best-effort reuse of any remaining native generic fraction tracker as the active Rift kill quota counter
- no-client-patch player feedback through chat messages and `rift status`, instead of relying on native terminal objective tracker text for Rift-specific UX
- safe interception of the chosen beacon base so the Mythic Rift path no longer falls back into a normal scenario behavior after a successful Rift launch
- safer chain-running from inside terminals because the random map picker now excludes the terminal region the requester / party is currently standing in
- persistent unlocked Rift progression
- chaining into higher Rift levels
- competitive next-level progression based on who was inside the Rift at boss unlock and boss death
- prevention of overlapping in-progress Rift runs for the same player / party
- forced Rift-region teleport resolution using the configured content region instead of trusting native terminal `StartTarget.Region`, preventing accidental `RegionBand` drift on affected terminals
- current terminal content now uses the L60 terminal region variants where available, following MonEll's local finding that these resolve the intended terminal regions more reliably
- successful Rift clears spawn a return portal back to the Danger Room hub
- best-effort party-member teleports when the leader launches the Rift beacon
- automatic abort if all tracked participants stay offline too long
- automatic cleanup of stale completed or abandoned runs
- automatic Rift closure when a tracked participant leaves the active Rift region before completion
- automatic Rift failure and return-to-hub when the timer expires
- automatic shutdown request for completed/aborted Rift terminal regions once they become vacant, so the next run gets a fresh instance
- user-level `rift status` and `rift abandon` commands for player-like testing without admin run management
- user-level `rift level [level|max]` command so unlocked players can choose a lower farming level before using the next beacon
- player-facing chat feedback at launch, boss unlock, completion, failure, and early Rift closure
- best-effort timer UI using existing server/client systems, with minute-by-minute chat warnings as the guaranteed fallback
- player-facing kill progress feedback at 25%, 50%, and 75% of the enemy quota
- group continuity when the leader disconnects

## What Is Not Final Yet

These parts are still not final:

- final player-facing entry UX
- final visible in-game naming / tooltip / icon polish
- broader reward tuning after real gameplay feedback
- larger content pool beyond the current first terminals
- optional patcher-delivered presentation polish if TAHITI wants it
- final polished Rift level selection UX beyond the current chat command, if TAHITI wants a cleaner NPC or interaction flow later
- scripted/gated terminals such as Ultron and Magneto / Stryker Bunker remain intentionally excluded from random selection; they are registered for fixed L60 validation before any decision to re-enable them randomly

## Current Random-Eligible V1 Content Pool

Terminal map + boss-source pool:

- Shocker
- Doctor Octopus
- Taskmaster
- Hood
- Mister Sinister
- MODOK
- Mandarin
- Kingpin

Curated non-terminal map-only pool:

- Bronx Zoo
- Wakanda Jungle
- HYDRA Island One-Shot
- Daily Bugle Operation
- Doctor Strange Times Square

Special low-chance pool:

- Cosmic Doop Sector
  - 5% random map chance
  - fixed boss source: Cosmic Doop Overlord
  - kill quota: `100`
  - direct test id: `cosmic-doop-sector`

Registered but random-excluded:

- Magneto / Stryker Bunker
- Ultron

## Build Procedure

On TAHITI/Linux, the normal solution build is expected to be enough:

```bash
dotnet build MHServerEmu.sln --configuration=Release
```

The redirected output command below is only a local Windows workaround for machines that hit temporary `bin` / `obj` file locks during rapid rebuilds.

Local Windows fallback command:

```powershell
dotnet build C:\Users\admin\Desktop\PROJECT MHO\MHServerEmu-master\src\MHServerEmu\MHServerEmu.csproj -c Release -p:GenerateAssemblyInfo=false -p:GenerateTargetFrameworkAttribute=false -p:BaseIntermediateOutputPath=C:\Users\admin\Documents\Codex\build\obj-cli\ -p:BaseOutputPath=C:\Users\admin\Documents\Codex\build\bin-cli\
```

Known verified state at this milestone:

- full server build succeeds
- historical Gazillion warnings may still appear
- local direct in-repo build can occasionally hit a temporary Gazillion/VBCSCompiler file lock during rapid rebuilds
- if redirected obj/bin builds hit duplicate Gazillion assembly attributes on this machine, keep the two `Generate*` switches shown above

## Important Current Identity Mapping

Player-facing feature name:

- `Cosmic Rift`

Planned player-facing launcher name:

- `Cosmic Rift Beacon`

Current technical launcher base:

- `PortalToRandomMaxAffixDungeon`

Current implemented seller path:

- server-scoped vendor injection inside the `Danger Room` hub

Why it is implemented this way right now:

- it removes the admin-only item grant dependency immediately
- it stays fully server-side and does not need a client patch
- it avoids hard-coding the wrong NPC before TAHITI confirms the final seller
- it gives TAHITI a usable no-admin test flow first, then a simple narrowing pass later

Preferred final seller once TAHITI confirms the exact NPC:

- `DangerRoomScenarioVendor`

Why this is still the preferred final fit:

- it already belongs to the Danger Room ecosystem, which matches the current launcher restriction
- it is a cleaner semantic match for selling a Rift-entry consumable than a generic reward, crafter, or weapon vendor
- it is a safer integration target than reusing a broader hub vendor or a more generic NPC vendor path
- it keeps `DangerRoomVendorWeaponMadisonJeffries` available later if TAHITI wants a stronger named-NPC identity after the first rollout is stable

Current fallback seller for a more character-driven presentation:

- `DangerRoomVendorWeaponMadisonJeffries`
- use this only if TAHITI explicitly prefers a named NPC over the safer dedicated scenario-vendor path

Important clarification:

- the current test flow prefers the existing `PortalToRandomMaxAffixDungeon` game item as the technical base
- the newest server-side seller pass lets testers buy that launcher directly from a `Danger Room` hub vendor before any final NPC lock is chosen
- if TAHITI later wants a cleaner visible in-game identity, that would likely be done as optional patcher-delivered game-file polish

## Fastest Test Flow

This is the simplest demonstration path.

1. Move the player to the Danger Room hub and open the Danger Room rewards vendor.

2. Buy the injected `PortalToRandomMaxAffixDungeon` launcher item.

3. Use the purchased item in-game.

4. Inspect the launched run:

```text
rift status
rift beaconmode
rift runs
rift run [runId]
```

Expected current behavior:

- the item is present by default in vendor stock
- buying it registers a tracked beacon charge
- using it launches a random Cosmic Rift instead of the native Danger Room tutorial
- the item is consumed after the committed launch
- `rift status` works without needing admin-only run lists
- the chat feedback is high-level and player-facing, not admin/debug oriented
- if the client accepts the existing timer packet, a visible timer should start; if not, chat still warns at key remaining-time thresholds

Legacy/admin preparation commands remain useful for troubleshooting:

```text
rift prepbeacon 1 1
rift beacon
rift validatecontent
```

Expected current behavior:

- the item use should stay in the Mythic Rift path instead of dropping a normal Danger Room scenario back into inventory
- if you chain the next random Rift while still standing in the previous terminal, the selector should avoid that same terminal content when another map is available
- if a player-like test run needs to be stopped manually, use `rift abandon`
- `rift abandon` intentionally costs the current Rift attempt and returns online participants to the Danger Room hub
- a normal return-to-town / hub teleport out of the active Rift also costs and closes the current Rift attempt if the Rift is not completed yet
- once quota plus Rift boss are completed, success is recorded at boss death, so teleporting out afterward should not invalidate the clear
- if an admin needs hard cleanup, use `rift abort [runId]` and then `rift remove [runId]`

## Recommended Review Test Plan

### Test 1: Basic Launcher Flow

Goal:

- verify beacon grant
- verify direct item use
- verify run creation
- verify terminal bind / teleport feedback

Commands:

```text
rift prepbeacon 1 1
rift beacon
rift validatecontent
rift beaconmode
rift runs
```

### Test 2: Basic Terminal Completion

Goal:

- verify bind/start flow
- verify kill quota
- verify boss completion

Suggested process:

1. Create or consume a run.
2. Enter the expected terminal region.
3. Let the server auto-bind the run.
4. Kill enemies until quota is met.
5. Confirm the Rift boss appears only after quota completion, then kill the spawned/configured Rift boss.
6. Inspect the run state and rewards.

Useful commands:

```text
rift run [runId]
rift progression
```

Also confirm:

- `competitiveEligibility=bossUnlock:X | bossKill:Y`
- only players counted at `bossKill` unlock the next difficulty

### Test 3: Progression Chain

Goal:

- verify progression from level 1 to 2 to 3

Commands:

```text
rift prepbeacon 1 3
rift progression
```

Then:

- use a beacon directly in-game
- complete the Rift
- verify:

```text
rift access 2
rift progression
```

Repeat again and verify:

```text
rift access 3
rift progression
```

### Test 4: Persistence Through Relog

Goal:

- verify that unlocked levels survive save/relog

Process:

1. Complete a Rift successfully.
2. Confirm the next level is unlocked.
3. Relog.
4. Re-run:

```text
rift access 2
rift progression
```

Expected:

- progression remains available after relog

### Test 5: Group Continuity

Goal:

- verify that leader disconnect does not invalidate the Rift

Expected behavior:

- if the leader disconnects, the Rift remains valid
- no special punishment occurs
- remaining players can still finish quota + boss and complete the Rift

### Test 6: Session Safety

Goal:

- verify stale-run cleanup behavior

Process:

1. Start a Rift run.
2. Disconnect all tracked participants.
3. Wait a little over 2 minutes.

## Current Review Notes

These points should help TAHITI interpret tester feedback correctly.

- reports that the beacon "sometimes becomes a regular Danger Room" belong to the older behavior that the newer interception pass was explicitly designed to stop
- reports that the same dead terminal was selected again are also expected to improve on the newer random-selection pass, because it now excludes the player's current terminal region and last completed terminal map when other maps are available
- reports that the normal terminal boss can be killed before quota should be retested on the newer native-boss suppression build
- inherited terminal boss loot is still used intentionally at this stage, so side drops such as cube shards are not a blocker by themselves
- random enemy replacement is not part of the current feature-complete test target yet; it should be treated as a later server-side safety pass
- that reward behavior should be treated as a temporary prototype state until the final reward layer is tuned
- current difficulty at higher Rift levels is still considered tuning-sensitive
- for efficient review cycles, multiplayer and gameplay-flow validation should come before final high-level balance conclusions
4. Reconnect and inspect:

```text
rift runs
```

Expected:

- the run is no longer left hanging forever in an in-progress state

## Most Useful Commands For Review

```text
rift beacon
rift prepbeacon [level] [count]
rift givebeacon [count]
rift beaconmode
rift validatecontent
rift previewrandom [count] [level] [players] [minutes]
rift validaterandompool [level] [players] [minutes]
rift access [level]
rift progression
rift runs
rift run [runId]
rift status
rift abandon
rift entrypoints
rift launchplan portal-to-random-dungeon
```


## Honest Current Risks / Remaining Validation

- reward numbers still need live feedback
- broader multiplayer testing is still needed
- the visible launcher item identity is not yet the final polished UX
- long-session testing should still be done on a real TAHITI-like environment
- reward distribution is still more permissive than next-level progression; only progression currently follows the strict competitive rule

## Bottom-Line Recommendation

Recommended next step for TAHITI:

- review the `codex/mythic-rift` branch
- run the fast smoke test
- run the progression chain test
- decide whether the current server-side direction is acceptable before any optional patcher-facing polish is discussed
