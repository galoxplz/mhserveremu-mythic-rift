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

This is a server-side endgame prototype inspired by Diablo 3 Greater Rifts, built on top of existing Marvel Heroes Omega terminal content, using `PortalToRandomDungeon` as the current technical launcher base and `Cosmic Rift Beacon` as the planned player-facing identity.

## Recommended Way To Present It

Suggested structure when introducing it:

1. Explain that it is intentionally server-first.
2. Explain that the current test flow does not require a manual client patch.
3. Explain that it reuses existing terminal content rather than inventing a fully custom dungeon stack from scratch.
4. Explain that the current launcher identity is `Cosmic Rift Beacon`, but the visible in-game item is still based on `PortalToRandomDungeon`.
5. Explain that this is now a reviewable prototype milestone, not just a design concept.

## Suggested Intro Message

You can present it roughly like this:

```text
This branch contains the current reviewable prototype for Cosmic Rift, a Diablo 3 Greater Rift-style endgame mode adapted to Marvel Heroes Omega.

The implementation is intentionally server-first and localized. The current test flow does not require a manual client patch and can be exercised through server-side commands plus existing game behavior.

The current launcher model uses PortalToRandomDungeon as the technical base, with Cosmic Rift Beacon as the intended player-facing identity later on.

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
- direct beacon use in-game from a server-granted `PortalToRandomDungeon`
- random terminal map plus separately selected random boss source
- safe interception of `PortalToRandomDungeon` so the Mythic Rift path no longer falls back into the normal Danger Room scenario behavior after a successful Rift launch
- safer chain-running from inside terminals because the random map picker now excludes the terminal region the requester / party is currently standing in
- persistent unlocked Rift progression
- chaining into higher Rift levels
- competitive next-level progression based on who was inside the Rift at boss unlock and boss death
- prevention of overlapping in-progress Rift runs for the same player / party
- automatic abort if all tracked participants stay offline too long
- automatic cleanup of stale completed or abandoned runs
- group continuity when the leader disconnects

## What Is Not Final Yet

These parts are still not final:

- final player-facing entry UX
- final visible in-game naming / tooltip / icon polish
- broader reward tuning after real gameplay feedback
- larger content pool beyond the current first terminals
- optional patcher-delivered presentation polish if TAHITI wants it

## Current V1 Content Pool

- Shocker
- Doctor Octopus
- Taskmaster
- Hood
- Magneto
- Mister Sinister
- MODOK
- Mandarin
- Kingpin
- Ultron

## Build Procedure

Use a writable output folder outside the repo for `bin` / `obj`.

Recommended command:

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

- `PortalToRandomDungeon`

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

- the current test flow uses the existing `PortalToRandomDungeon` game item as the technical base
- the newest server-side seller pass lets testers buy that launcher directly from a `Danger Room` hub vendor before any final NPC lock is chosen
- if TAHITI later wants a cleaner visible in-game identity, that would likely be done as optional patcher-delivered game-file polish

## Fastest Test Flow

This is the simplest demonstration path.

1. Prepare the player:

```text
rift prepbeacon 1 1
```

2. Confirm the current launcher identity:

```text
rift beacon
rift validatecontent
```

3. Buy the injected launcher from a `Danger Room` hub vendor or use a granted `PortalToRandomDungeon` item in-game.

4. Inspect the launched run:

```text
rift beaconmode
rift runs
rift run [runId]
```

Expected current behavior:

- the item use should stay in the Mythic Rift path instead of dropping a normal Danger Room scenario back into inventory
- if you chain the next random Rift while still standing in the previous terminal, the selector should avoid that same terminal content when another map is available

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
5. Kill the expected boss.
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
