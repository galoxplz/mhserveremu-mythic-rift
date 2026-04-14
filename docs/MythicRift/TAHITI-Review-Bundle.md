# TAHITI Mythic Rift Review Bundle

## What This Is

This is a server-side prototype for a new endgame mode inspired by Diablo 3 Greater Rifts, adapted for Marvel Heroes Omega.

Current product-facing identity:

- `Cosmic Rift`

Current launcher identity:

- `Cosmic Rift Beacon`

Current technical launcher base:

- `PortalToRandomDungeon`

## Why It Should Be Easy To Review

- localized implementation
- no full server reinstall required
- no database migration required
- no manual client patch required for the current test flow
- current testing can be done entirely through server-side commands and existing game behavior

## What Already Works

- random or fixed terminal selection from the V1 pool
- timed Rift runs
- D3-inspired scaling
- kill quota before boss unlock
- boss completion logic
- success / failure reward resolution
- timed success SIF/RIF bonus
- persistent Rift level progression
- chained progression from one Rift level to the next
- server-side beacon granting
- launcher intent capture from the technical beacon base item
- conversion of that intent into a Rift run
- automatic cleanup / safety behavior for stale runs

## Current V1 Content Pool

- Taskmaster
- Hood
- Mister Sinister
- Kingpin

## Current Group Rules

- 1 to 5 players
- 4 and 5 players share the same group scaling bucket
- if the leader disconnects, the Rift remains valid
- if the group changes mid-run, the Rift still remains valid
- success is determined by killing enough enemies to unlock the boss, then killing the boss

## How TAHITI Can Test It Right Now

Recommended first-pass test:

```text
rift prepbeacon 1 1
rift beacon
```

Use the granted `PortalToRandomDungeon` item in-game, then:

```text
rift itemintent
rift consumeintentauto 10
rift runs
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
