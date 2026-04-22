# TAHITI Mythic Rift Review Bundle

## What This Is

This is a server-side prototype for a new endgame mode inspired by Diablo 3 Greater Rifts, adapted for Marvel Heroes Omega.

Current product-facing identity:

- `Cosmic Rift`

Current launcher identity:

- `Cosmic Rift Beacon`

Current technical launcher base:

- `PortalToRandomMaxAffixDungeon`

## Why It Should Be Easy To Review

- localized implementation
- no full server reinstall required
- no database migration required
- no manual client patch required for the current test flow
- current testing can be done entirely through server-side commands and existing game behavior

## What Already Works

- direct beacon use in-game from a server-granted `PortalToRandomMaxAffixDungeon`
- random or fixed terminal selection from the V1 pool
- random terminal map plus separately selected random boss source
- safe launcher interception so a successful Rift click no longer falls through into the normal Danger Room scenario path
- timed Rift runs
- D3-inspired scaling
- kill quota before boss unlock
- boss completion logic
- success / failure reward resolution
- timed success SIF/RIF bonus
- persistent Rift level progression
- chained progression from one Rift level to the next
- server-side beacon granting
- a first no-admin seller pass inside the `Danger Room` hub, so testers can buy the launcher directly from an in-game vendor
- competitive next-level progression based on who was inside the Rift at boss unlock and boss death
- automatic cleanup / safety behavior for stale runs

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

## Current Group Rules

- 1 to 5 players
- 4 and 5 players share the same group scaling bucket
- group health scaling is now locked to `1x / 2x / 3x / 4x` for `1 / 2 / 3 / 4-5` players
- one Mythic Rift level now maps to `0.40` D3 Greater Rift levels in the frozen test tuning, so Marvel terminal balance stays more realistic during multiplayer validation
- if the leader disconnects, the Rift remains valid
- if the group changes mid-run, the Rift still remains valid
- success is determined by killing enough enemies to unlock the boss, then killing the boss

## How TAHITI Can Test It Right Now

Recommended first-pass test:

```text
rift prepbeacon 1 1
rift beacon
rift validatecontent
```

Buy the injected launcher from a `Danger Room` hub vendor, or use a granted `PortalToRandomMaxAffixDungeon` item in-game, then:

```text
rift beaconmode
rift runs
rift run [runId]
```

Recommended progression test:

```text
rift prepbeacon 1 3
rift progression
```

Then chain multiple successful runs and check:

```text
rift access 2
rift access 3
rift progression
```

For competitive progression validation, admins should also inspect:

```text
rift run [runId]
```

Expected:

- `competitiveEligibility=bossUnlock:X | bossKill:Y`
- only players counted in `bossKill` should unlock the next difficulty
- if the next random Rift is launched while the player is still inside the previous terminal, that same terminal content should be avoided when another random map is available

## What Still Needs Work Before A Near-Final V1

- broader long-session gameplay testing
- reward tuning after real test feedback
- more validation around group edge cases in live play
- final decision on player-facing entry UX
- optional patcher-delivered game-file polish if TAHITI wants a cleaner visible item identity later

## Best Follow-Up Docs

- `Admin-Test-Guide.md`
- `Implementation-Status.md`
- `Architecture.md`
