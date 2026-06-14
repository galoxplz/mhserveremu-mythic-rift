# Mythic Rift AI Agent Handoff

Last updated: 2026-06-14

This is the canonical catch-up document for an AI agent continuing Mythic Rift work. Read it before editing code.

## Critical Instructions

1. Work only in:

```text
C:\Users\mtzim\Desktop\CodexStuff\mhserveremu-mythic-rift
```

2. All new Rift code and Rift-related changes belong in this repository.
3. Do not reset, clean, checkout, or overwrite the current working tree. It contains substantial uncommitted work.
4. Do not revert user changes or unrelated changes.
5. Keep Rift behavior inside `src/MHServerEmu.Games/MythicRifts` whenever practical.
6. In particular, keep Rift HUD cleanup out of stock `UIDataProvider`. Its stock file currently has no diff.
7. Stock `PortalToRandomDungeon` must remain normal stock behavior. It is not a Mythic Rift launcher.
8. Use existing game prototypes and loot tables only after verifying that they resolve. SIP presence alone does not prove that a boss, region, or loot table works in a Rift.
9. Distinguish automated coverage from live validation. Multiplayer zoning, native metagames, AI, transitions, and client UI require in-game tests.

## Repository Identity

Current repository:

```text
C:\Users\mtzim\Desktop\CodexStuff\mhserveremu-mythic-rift
```

Current branch:

```text
codex/mythic-rift
```

Remotes:

```text
origin   https://github.com/mtzimas92/Test-Rifts.git
upstream https://github.com/galoxplz/mhserveremu-mythic-rift.git
```

Current committed HEAD:

```text
85211ab Document Cosmic Rift reward tuning workflow
```

At the time of this handoff, `HEAD`, `origin/codex/mythic-rift`, and `upstream/codex/mythic-rift` all point to `85211ab`. This means the committed baseline matches Galox's branch, but the working tree intentionally contains newer local work and no longer matches it byte-for-byte.

Do not run `git reset --hard`, `git clean`, or a destructive checkout to make the tree match upstream.

## Product Intent

Mythic Rift is a server-side endgame mode inspired by Diablo 3 Greater Rifts:

- one to five players
- private instanced region
- random or fixed map
- independently selected boss source
- timed kill quota followed by a boss wave
- persistent personal Rift progression
- optional 30-wave difficulty cycle
- distinct random multi-boss rosters
- one-time 25% / 50% / 75% milestone encounters
- controlled ground loot
- return portal to Danger Room
- no required custom client patch

The desired experience is varied, replayable, worthwhile, and administratively tunable. The user wants many usable maps and bosses from SIP data, deliberate rewards, and stronger per-boss reward control.

## Current Launcher And Exit

Launcher family:

```text
Technical base: PortalToRandomMaxAffixDungeon
Presentation:   DangerRoomScenarioCrateUniqueCableFight
Visible intent: Mythic Rift Scenario
```

The regular `PortalToRandomDungeon` item is deliberately excluded from Mythic Rift interception.

The completed Rift exit uses:

```text
Entity/Transitions/ReturnToLastBaseDR.prototype
```

This replaced the Cow/Bovineheim-flavored transition so the portal behaves as a return-to-base transition.

Players may leave a completed Rift independently. A cleared Rift also allows another launcher to be used for chaining runs.

## Core Code

Primary runtime files:

```text
src/MHServerEmu.Games/MythicRifts/MythicRiftManager.cs
src/MHServerEmu.Games/MythicRifts/MythicRiftLauncherService.cs
src/MHServerEmu.Games/MythicRifts/MythicRiftEntryService.cs
src/MHServerEmu.Games/MythicRifts/MythicRiftRunState.cs
src/MHServerEmu.Games/MythicRifts/MythicRiftRunConfig.cs
src/MHServerEmu.Games/MythicRifts/MythicRiftContentEntry.cs
src/MHServerEmu.Games/MythicRifts/MythicRiftScaling.cs
src/MHServerEmu.Games/MythicRifts/MythicRiftRewardTuning.cs
src/MHServerEmu/Commands/Implementations/MythicRiftCommands.cs
```

New focused helpers in the current working tree:

```text
MythicRiftConfig.cs
MythicRiftProgression.cs
MythicRiftUiController.cs
MythicRiftUiOwnership.cs
MythicRiftWaveProfile.cs
```

Reward configuration:

```text
src/MHServerEmu.Games/Data/Game/MythicRift/CosmicRiftRewards.json
```

Tests:

```text
src/MHServerEmu.Games.Tests/MythicRifts/
```

## Current Uncommitted Work

The working tree contains a large coherent Rift change set. Important modified files include:

```text
docs/MythicRift/Admin-Test-Guide.md
docs/MythicRift/Implementation-Status.md
docs/MythicRift/Player-Feedback-Triage.md
docs/MythicRift/Player-Progression-Guide.md
docs/MythicRift/Reward-Tuning-Guide.md
docs/MythicRift/Terminal-Compatibility-Audit.md
src/MHServerEmu.Games/Data/Game/MythicRift/CosmicRiftRewards.json
src/MHServerEmu.Games/MythicRifts/MythicRiftContentEntry.cs
src/MHServerEmu.Games/MythicRifts/MythicRiftLauncherService.cs
src/MHServerEmu.Games/MythicRifts/MythicRiftManager.cs
src/MHServerEmu.Games/MythicRifts/MythicRiftRewardTuning.cs
src/MHServerEmu.Games/MythicRifts/MythicRiftRunConfig.cs
src/MHServerEmu.Games/MythicRifts/MythicRiftRunState.cs
src/MHServerEmu.Games/MythicRifts/MythicRiftScaling.cs
src/MHServerEmu/Commands/Implementations/MythicRiftCommands.cs
src/MHServerEmu/Config.ini
```

There are also untracked Rift helpers, tests, and documents. Run `git status --short` before doing anything.

## Implemented Behavior

### Party Admission

- The launch roster is built from online party members in the requester's current region.
- A party member outside Danger Room/current launch region is intentionally excluded.
- Each member is admitted only after their Rift teleport succeeds.
- Health scaling is frozen from the admitted roster.
- Failed, offline, or out-of-region members do not receive rewards or progression.
- Active-run conflict detection prevents a player from being enrolled in competing runs.
- Teleport processing uses the registered run roster instead of relying on whoever is party leader later.
- Changing party leader should no longer change the already-created run.

The first two-player scenario to verify is both players standing in Danger Room before the leader uses the item.

### Personal Progression And Anti-Carry

- Highest unlocked Rift level is persistent per player.
- `rift level N` is a one-shot launch selection and does not lower progression.
- Clearing a lower selected level cannot reduce the saved maximum.
- A lower-level player completing a much higher friend's Rift advances by at most one personal level.
- Progression eligibility is based on Rift participation/presence, not the current party leader.
- Normal quota runs capture presence at boss unlock and boss death.
- Boss-only checkpoint rooms use boss-death presence so slower-loading players are not excluded immediately.

The progression helper is:

```text
MythicRiftProgression.ResolveNextUnlockedLevel
```

### Failure And Death Recovery

- Timeout/failure evacuation is deferred out of the death callback.
- Before hub transfer, the code attempts to restore a valid alive avatar state.
- Failed-run evacuation retries if players remain in the Rift.
- Death release in active Rifts is intercepted to keep the player in the same Rift instance.
- `rift abandon` intentionally ends a run.
- `rift recover` clears temporary launch state and escapes a stuck run.

This addresses the report where a timed-out dead player reached the hub but remained permanently dead. It still needs live reproduction testing.

### Boss Spawning

- Bosses spawn around admitted alive players.
- The anchor is the admitted player nearest the group's spatial center.
- Pets, Rocket Raccoon turrets, minions, killed entities, and the furthest-back player are not spawn anchors.
- Compact checkpoint rooms prefer a valid position in the player's current cell.
- Failed checkpoint boss spawns retry instead of immediately aborting the run.
- Multiple bosses are tracked individually.
- Completion occurs only after the configured number of Rift bosses die.
- Individual Rift bosses suppress native death loot.

MODOK remains excluded as a random boss source because of reports that he sometimes does not move or attack. A.I.M. Facility may still be used as a map with another Rift boss.

### UI Ownership

Rift UI consists of:

- Rift level
- kill quota, hidden in boss-only checkpoint rooms
- timer

Native mission packets are suppressed where needed. Raid/static-scenario metagames can create widgets directly, so `MythicRiftUiController` also removes every non-Rift widget from the active region every 500 ms.

The ownership rule is isolated in:

```text
MythicRiftUiOwnership.IsRiftOwnedWidget
```

The controller currently uses reflection to inspect `UIDataProvider._dataDict`, then calls the normal `DeleteWidget` API. This is intentionally contained in the Mythic Rift module.

Stock file:

```text
src/MHServerEmu.Games/UI/UIDataProvider.cs
```

must remain unmodified unless the user explicitly changes direction.

### Region And Content Variety

Random map and boss selection are decoupled. Map-only regions can use validated terminal boss sources.

Normal expanded maps include:

- standard validated L60 terminal maps
- Bronx Zoo
- Wakanda Jungle
- HYDRA Island One-Shot
- Daily Bugle with custom population
- Civil War Airport, Captain America side
- Civil War Airport, Iron Man side
- Civil War Bazaar

Civil War scenarios declare a one-player limit and are excluded from party random selection.

Special low-frequency maps share a combined 5% branch:

- Doctor Strange Times Square / Dimensions Collide
- March to Axis, level 15+
- Muspelheim Raid, level 15+
- Cosmic Doop Sector, level 25+

Boss-only checkpoint rooms are used every five levels.

Boss-only source expansion currently includes:

- Pyro
- A.I.M. Doctor Octopus
- Wizard
- Bullseye
- Elektra
- Black Cat
- Blob
- Green Goblin
- Rhino
- Venom

Do not add raw SIP boss results directly. The SIP contains phase actors, summons, markers, turrets, props, and broken AI variants mixed with real bosses.

### Optional 30-Wave Mode

Configuration:

```ini
[MythicRift]
EnableThirtyWaveMode=true
```

Set it to `false` and restart to restore classic compressed Greater Rift scaling and one final boss.

Wave mode repeats every 30 Rift levels:

| Waves | Boss count | Per-boss HP progression |
|---|---:|---|
| 1-9 | 1 | 1.0x to 5.0x |
| 10-19 | 2 | 2.5x to 7.0x |
| 20-24 | 3 | 5.0x to 7.0x |
| 25-27 | 4 | 5.0x to 6.0x |
| 28-29 | 5 | 6.5x to 7.0x |
| 30 | 6 | 7.0x |
| 31 | reset to wave 1 | 1 boss at 1.0x |

In wave mode, the old party health multiplier is not added on top of this table. The admitted player count is still tracked for run state.

### Reward Control

The reward system supports:

- hot-reloadable JSON
- primary boss-table overrides
- aliases
- multiple independent extra tables
- grouped reward recipes
- random item pools discovered from approved, live prototype directories
- min/max Rift levels
- classic/checkpoint filters
- map content filters
- boss-source filters
- roll chance and roll count
- ground or inventory delivery
- guaranteed item drops with level, wave, checkpoint, map, and boss filters
- boss-source matching against any boss in a multi-boss wave

Reload and inspect:

```text
rift rewardconfig reload
rift rewardconfig
```

Rift bosses always suppress native death loot. Controlled rewards default to player-owned ground drops. Failed Rifts grant no managed loot by default.

## Active Loot Profile

The active JSON profile is:

```text
worthwhile-v4-all-cosmic-artifacts
```

It grants every final boss's own table, then adds:

- every successful floor: one ground crate worth 10 cube shards
- waves 20-29: one additional 10-shard crate
- wave 30: two additional 10-shard crates
- successful boss-only checkpoints: one additional 10-shard crate
- levels 1-9: 10% Cosmic artifact chance
- levels 10-19: 20% Cosmic artifact chance
- levels 20-29: 35% Cosmic artifact chance
- levels 30+: 50% Cosmic artifact chance
- checkpoint levels 1-19: an additional 30% Cosmic artifact chance
- checkpoint levels 20+: an additional 75% Cosmic artifact chance
- Cosmic Doop Sector: three additional Overlord-table rolls and one guaranteed Cosmic artifact
- every successful clear: one roll against `SpecialsCostumesCardsTokens`

The final chase table retains its internal 99% no-drop rate, so it is approximately a 1% chase attempt rather than a guaranteed costume/card/token.

Per-boss recipes can target any boss present in a multi-boss wave. The profile deliberately removes unrelated random terminal-table rolls, but a larger curated catalog of artifacts, uniques, currencies, and chase items can still be added boss by boss.

Cosmic artifact rewards use a Rift-owned random item pool over `Entity/Items/Artifacts/Prototypes/SpecialArtifacts/CosmicArtifacts/`. SIP inspection found 85 approved, live item prototypes in that directory, while the stock `CosmicArtifactsTable` directly exposes only 48. The pool therefore includes all 37 omitted artifacts, including the 17 omitted prototypes whose ordinary loot weight is zero. Selection is uniform and artifacts are created directly at item level 63, so red Danger Room region difficulty does not prevent them from dropping.

## Proposed Endless Rift Launcher

The recommended second technical launcher base is:

```text
Entity/Items/Consumables/Prototypes/DangerRoom/PortalToDangerRoomRandomThemeNoAffixesPurple.prototype
runtimeId=1020710291701049759
```

SIP inspection found it is `DevelopmentOnly`, usable, live-tuning enabled, has normal drop weight, uses the same Danger Room scenario OnUse power family as the current launcher, and has no incoming stock prototype references. Purple is preferred over White because White is the parent of the colored variants. This candidate is identification-only: it is not yet registered, patched into vendor stock, or routed to an Endless mode.

Avoid weekly-event tables such as Cosmic Chaos and Odin's Bounty unless their live-tuning enable state is deliberately handled. Those tables can silently roll nothing when the event is inactive.

## Reported Bugs And Current Status

### Addressed In Code, Needs Live Validation

- Dead player remains stuck dead after timeout evacuation.
- Group portal works only after somebody creates a solo portal.
- Passing leader between runs reverts to native terminal/one-shot behavior.
- Lower-level players inherit the leader's high maximum.
- Bosses spawn on turrets, minions, or the player furthest behind.
- Native and managed rewards both drop.
- Native story portals remain active in checkpoint rooms.
- Axis/native scenario UI replaces or overlaps Rift UI.
- Multi-boss waves complete or reward after the first boss.

### Deliberately Excluded Or Mitigated

- MODOK flaky AI: excluded as a random boss source.
- March to Axis native HUD: Rift-owned periodic widget purge implemented.
- Civil War multiplayer: those maps are solo-only.
- Native checkpoint-room enemies: suppressed before Rift boss spawn.

### Still Open Or Requires Focused Testing

- Doctor Strange Times Square can report `region has not finished downloading`, especially when party members zone at different times.
- March to Axis and Muspelheim native metagames may still manipulate doors, phases, encounters, or region shutdown even when their UI is hidden.
- Party leader changes need an actual repeated-run two-client test.
- Leaving a party mid-Rift should not erase the frozen run roster; reward/progression behavior must be verified against the user's desired rule.
- Multiple players independently using the return portal needs repeated-run validation.
- Reward quantity and quality need live balance testing. JSON correctness does not prove the resulting item mix feels worthwhile.
- New boss-only SIP candidates need spawn, AI, bounds, death, and loot validation before random eligibility.

## Automated Verification

Most recent targeted run:

```text
dotnet test src\MHServerEmu.Games.Tests\MHServerEmu.Games.Tests.csproj --no-restore --nologo
```

Result:

```text
101 passed, 0 failed
```

Current full solution verification:

```text
MHServerEmu.Core.Tests: 237 passed
MHServerEmu.Games.Tests: 101 passed
Total: 338 passed, 0 failed
```

The full server build also passed with 0 warnings and 0 errors.

Before handing off new changes, run at minimum:

```powershell
dotnet test .\src\MHServerEmu.Games.Tests\MHServerEmu.Games.Tests.csproj --no-restore --nologo
```

For broad changes, run:

```powershell
dotnet test .\MHServerEmu.sln --no-restore --nologo
```

## Manual Test Priority

The detailed script is:

```text
docs/MythicRift/Tonight-Test-Checklist.md
```

Priority order:

1. Verify `rift rewardconfig reload` loads `worthwhile-v4-all-cosmic-artifacts` and reports 85 Cosmic artifact pool candidates.
2. Solo level 1 clear and inspect ground rewards.
3. Level 10 two-boss checkpoint; confirm completion only after boss two.
4. Level 30 six-boss clear; confirm one completion reward event.
5. Two players in Danger Room; confirm both enter and remain admitted.
6. Change party leader during and between runs.
7. High-level leader plus low-level player; confirm the low player advances only one personal level.
8. Die, let time expire, and confirm the player returns alive and usable.
9. March to Axis; confirm native top UI disappears and native scripting does not end the Rift.
10. Doctor Strange Times Square with two clients; capture any region-download failure.
11. Cosmic Doop Sector; verify jackpot ground drops and no inventory duplicate.
12. Each player uses the return portal independently.

For failures, capture before leaving:

```text
rift status
rift perf
rift objectives
rift run [runId]
rift beaconmode
```

Also record map id, Rift level, party size, leader, admitted player count, boss count, and server log timestamps.

## Useful Commands

Common player/admin commands include:

```text
rift status
rift level [level]
rift level max
rift abandon
rift recover
rift progression
rift setaccess [level]
rift resetprogress
rift prepbeacon [count] [level]
rift armbeacon
rift armbeaconfixed [contentId] [minutes]
rift disarmbeacon
rift beaconmode
rift previewrandom [level] [count] [players] [minutes]
rift validaterandompool [level] [players] [minutes]
rift scale [level] [players]
rift runs
rift run [runId]
rift perf
rift objectives
rift rewardconfig
rift rewardconfig reload
```

Consult `MythicRiftCommands.cs` for exact permissions and parameters.

## SIP And Data Resources

Available extracted-data inputs:

```text
C:\Users\mtzim\Desktop\CodexStuff\Calligraphy.sip
C:\Users\mtzim\Desktop\CodexStuff\mu_cdata.sip
```

The latest audit is:

```text
docs/MythicRift/SIP-Content-Audit.md
```

Current audit counts:

- 975 concrete region prototypes with valid start targets
- 753 raw agents under `Entity/Characters/Bosses`

These are discovery counts, not safe-content counts.

## Documentation Reading Order

After this file, read:

1. `Implementation-Status.md`
2. `Player-Feedback-Triage.md`
3. `Tonight-Test-Checklist.md`
4. `Reward-Tuning-Guide.md`
5. `SIP-Content-Audit.md`
6. `Admin-Test-Guide.md`
7. `Architecture.md`
8. `Terminal-Compatibility-Audit.md`

Some older documents describe pre-fix behavior. Prefer current code, this handoff, and the newest dated test notes when documents conflict.

## Recommended Next Work

The highest-value next steps are:

1. Run the live two-player checklist before making more structural changes.
2. Balance `worthwhile-v4-all-cosmic-artifacts` from actual drop observations.
3. Build a real per-boss reward catalog using verified always-enabled tables.
4. Expand the per-boss JSON catalog with curated artifacts, uniques, currencies, and chase rewards.
5. Diagnose Doctor Strange multiplayer streaming with timestamps and region bind logs.
6. Validate Axis/Muspelheim native metagame behavior beyond UI.
7. Expand bosses in small validated batches, never from raw SIP names alone.

## First Actions For A New Agent

Run:

```powershell
cd C:\Users\mtzim\Desktop\CodexStuff\mhserveremu-mythic-rift
git -c safe.directory=C:/Users/mtzim/Desktop/CodexStuff/mhserveremu-mythic-rift status --short
git -c safe.directory=C:/Users/mtzim/Desktop/CodexStuff/mhserveremu-mythic-rift branch --show-current
git -c safe.directory=C:/Users/mtzim/Desktop/CodexStuff/mhserveremu-mythic-rift log -5 --oneline --decorate
```

Then:

1. Read the documents above.
2. Inspect the current diff before editing.
3. Run the Games tests.
4. Do not assume a reported fix is live-validated.
5. Keep the user's current working tree intact.
