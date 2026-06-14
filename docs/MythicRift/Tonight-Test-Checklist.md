# Mythic Rift Tonight Test Checklist

Date: 2026-06-14

## Before Starting

1. Rebuild and restart the server.
2. Open a Danger Room vendor and confirm it offers two separate launchers:
   - `Mythic Rift Scenario`: classic compressed scaling with one final boss
   - `Endless Rift Scenario`: repeating 30-wave scaling with the configured multi-boss milestones
3. In game, run:

```text
rift scale 1 1
rift scale 10 1
rift scale 20 1
rift scale 25 1
rift scale 28 1
rift scale 30 1
rift scale 31 1
```

Expected:

| Level | Wave | Bosses | Per-boss HP | Total boss HP |
|---|---:|---:|---:|---:|
| 1 | 1 | 1 | 1.00x | 1.00x |
| 10 | 10 | 2 | 2.50x | 5.00x |
| 20 | 20 | 3 | 5.00x | 15.00x |
| 25 | 25 | 4 | 5.00x | 20.00x |
| 28 | 28 | 5 | 6.50x | 32.50x |
| 30 | 30 | 6 | 7.00x | 42.00x |
| 31 | 1 | 1 | 1.00x | 1.00x |

## Test 1: Basic Wave Completion

```text
rift prepbeacon 10 2
rift level 10
rift armbeaconfixed bronx-zoo 10
```

Use one Endless Rift Scenario item.

Expected:

- Start message says `Wave 10/30` and `Bosses: 2`.
- Normal kill quota works.
- Two different random Rift bosses spawn around the player, not on pets or turrets.
- `rift run [runId]` lists two different boss ids and two different boss prototypes in `bossWave`.
- `rift status` reports `bosses=0/2 defeated` and `activeBosses=2`.
- Killing the first boss does not complete the Rift or drop completion rewards.
- Killing the second boss completes the Rift, drops rewards on the ground, and creates the return portal.

## Test 2: Maximum Wave

```text
rift setaccess 30
rift level 30
rift armbeaconfixed bronx-zoo 10
```

Use one Endless Rift Scenario.

Expected:

- Six random, distinct bosses spawn. No boss name or boss prototype should repeat.
- Bosses are distributed around the player group without stacking at one exact point.
- Status progresses from `0/6` through `6/6`.
- Completion and loot occur once, after the sixth boss.
- No individual boss produces native duplicate loot.
- The completion reward includes one controlled loot-table roll from each of the six bosses.

## Test 3: Milestone Encounters

Run a normal kill-quota Rift with enough enemies to observe all three thresholds.

Expected:

- At 25% kill progress, a three-enemy Champion invasion spawns once.
- At 50%, one random mini-boss spawns once.
- At 75%, a three-enemy Elite strike team spawns once.
- Each threshold sends a Cosmic Rift chat notice.
- Milestone enemies do not drop native loot.
- Milestone enemies count toward kill progress using the existing rank weights.
- A threshold never spawns twice, even if progress stalls at the same percentage.

## Test 4: Reset At Level 31

```text
rift setaccess 31
rift level 31
rift armbeaconfixed bronx-zoo 10
```

Use one Endless Rift Scenario.

Expected:

- Message says `Wave 1/30`.
- Only one boss spawns.
- `rift scale 31 1` reports `perBossHP x1.00`.
- Progression remains level-based; clearing level 31 unlocks level 32 normally.

## Test 5: Axis Native UI

```text
rift setaccess 20
rift level 20
rift armbeaconfixed march-to-axis 10
```

Use one Endless Rift Scenario.

Expected:

- Mythic Rift level, kill bar, and timer remain visible.
- Axis raid objective bars, ready checks, score panels, or native top UI do not persist for more than one second.
- `rift objectives` shows the Rift widgets and no lasting native tracker.
- Native Axis scripting does not teleport players, end the region, or replace the Rift objective.
- Three Rift bosses spawn after quota because level 20 is wave 20.

## Test 6: Other Experimental Areas

Repeat separately:

```text
rift armbeaconfixed muspelheim-raid 10
```

Use one Endless Rift Scenario, finish or abandon that run, then:

```text
rift armbeaconfixed dr-strange-times-square 10
```

Use one Endless Rift Scenario.

For each run verify:

- both players finish downloading the region
- native UI does not persist
- kill quota advances
- boss wave spawns
- return portal works

For Doctor Strange, record whether either client receives `region has not finished downloading`, including which player entered first.

## Test 7: Two-Player Party

1. Put both players in Danger Room before launch.
2. Make player 1 party leader.
3. On player 1:

```text
rift setaccess 10
rift level 10
rift armbeaconfixed bronx-zoo 10
```

4. Use one Endless Rift Scenario.

Expected:

- Both players enter.
- Status shows two admitted/effective players.
- Wave mode still uses two bosses at level 10 and does not add the old 2x party-health multiplier.
- Passing party leadership during the run does not cancel generation, rewards, or progression.
- Each player can use the return portal independently.

## Test 8: Solo-Only Civil War Guard

With two players in the launch party:

```text
rift previewrandom 20 20 2 10
```

Expected: no Civil War Airport or Bazaar map appears.

Then test solo:

```text
rift armbeaconfixed civil-war-airport-cap 10
```

Use one Endless Rift Scenario and finish or abandon the run. Repeat that sequence with:

```text
rift armbeaconfixed civil-war-airport-iron-man 10
```

Use one Endless Rift Scenario and finish or abandon the run. Then:

```text
rift armbeaconfixed civil-war-bazaar 10
```

Use one Endless Rift Scenario.

Expected: each works solo and uses only the Mythic Rift HUD.

## Test 9: Targeted Ground Loot

At server startup or after editing the reward JSON, run:

```text
rift rewardconfig reload
rift rewardconfig
```

Expected profile: `worthwhile-v4-all-cosmic-artifacts`.

The `rift rewardconfig` output should show seven random item pools. Each Cosmic artifact pool line should report:

```text
candidates=85
itemLevel=63
directory=Entity/Items/Artifacts/Prototypes/SpecialArtifacts/CosmicArtifacts/
```

Complete waves 1, 20, 25, and 30, then run `rift run [runId]`.

Expected:

| Wave | Guaranteed shard crates | Cube shards |
|---|---:|---:|
| 1 | 1 | 10 |
| 20 | 2, plus 1 if the map is a boss-only checkpoint | 20 or 30 |
| 25 | 2, plus 1 if the map is a boss-only checkpoint | 20 or 30 |
| 30 | 3, plus 1 if the map is a boss-only checkpoint | 30 or 40 |

- Shard crates and all managed loot appear on the ground and are owned by the eligible player.
- Each final-wave boss contributes its own configured boss loot table once.
- Per-boss JSON recipes match any boss in the final wave, not only the first boss.
- Cosmic artifact chances remain level-banded at 10%, 20%, 35%, and 50%.
- Checkpoint successes add a 30% Cosmic artifact roll below level 20 or a 75% Cosmic artifact roll at level 20+.
- Cosmic artifacts can drop even when the Rift region reports red difficulty.
- A successful Cosmic artifact roll produces a level-63 item from the full 85-prototype Cosmic artifact directory, including artifacts omitted from the stock 48-item table.
- Cosmic Doop Sector retains its deliberate jackpot recipe.
- The costume/card/token table is a chase attempt, not a guaranteed item.
- No duplicate inventory copy appears after ground loot spawns.

## Test 10: Standard And Endless Isolation

Prepare two launchers at Rift level 10:

```text
rift setaccess 10
rift level 10
rift givebeacon 1
rift giveendless 1
```

Use `Mythic Rift Scenario` first, finish or abandon the run, and then use `Endless Rift Scenario`. No server restart or configuration edit should be needed.

Expected:

- `Mythic Rift Scenario` reports Standard/classic mode, applies classic party health scaling, and summons one final boss.
- `Endless Rift Scenario` reports `Wave 10/30`, does not add the old party health multiplier, and summons two distinct bosses.
- Purchasing or holding both items does not make one item launch the other item's mode.
- Each active run retains the mode selected by the consumed launcher even if party leadership changes.

## Record For Any Failure

Run these before leaving the broken region:

```text
rift status
rift perf
rift objectives
```

Record:

- map and Rift level
- solo or party size
- party leader
- boss count spawned and killed
- whether native UI persisted
- whether loot appeared before the final boss
- whether either client saw a region-download error
