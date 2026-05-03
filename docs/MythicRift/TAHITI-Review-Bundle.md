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
- random map plus separately selected random terminal boss source
- safe launcher interception so a successful Rift click no longer falls through into the normal Danger Room scenario path
- timed Rift runs
- D3-inspired scaling
- kill quota before boss unlock
- randomized Rift boss is hidden until the quota is complete, then spawned server-side
- boss completion logic
- terminal-native boss/objective HUD suppression during active Rift runs, so randomized bosses do not leave misleading terminal objectives on screen
- temporary server-side suspension of the native terminal mission inside the Rift instance, restored when the run is removed, so the normal terminal objective tracker does not compete with the Rift objective
- temporary server-side suspension of active `Region Events` missions inside the Rift instance, restored when the run is removed, because the lighter client-side-only suppression did not hide that tracker reliably
- native `Mission` / `MissionObjective` update interception for controlled terminal objectives while a Rift is active, so terminal bounty counters do not rebuild on the client after suppression
- best-effort reuse of any remaining native generic fraction tracker as the active Rift kill quota counter
- no-client-patch player feedback through chat messages and `rift status`, instead of relying on native terminal objective tracker text for Rift-specific UX
- success / failure reward resolution
- timed success SIF/RIF bonus
- persistent Rift level progression
- chained progression from one Rift level to the next
- player-selected launch level through `rift level [level|max]`, so unlocked players can farm lower levels with the next beacon
- server-side beacon granting
- a first no-admin seller pass inside the `Danger Room` hub, so testers can buy the launcher directly from an in-game vendor
- forced teleport resolution to the configured Rift region, avoiding terminal `RegionBand` drift from some native start targets
- shutdown requests for completed/aborted Rift terminal regions once vacant, so later runs do not inherit stale instance state
- competitive next-level progression based on who was inside the Rift at boss unlock and boss death
- automatic cleanup / safety behavior for stale runs
- timer expiration now fails the Rift and returns online participants still inside the Rift to the Danger Room hub

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

Registered / known but excluded from random selection for now:

- Magneto / Stryker Bunker
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
