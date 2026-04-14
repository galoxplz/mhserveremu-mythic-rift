# Mythic Rift V1 Spec

## Document Goal

- Define a first playable, simple, and integrable version of Mythic Rift.
- Serve as the basis for local implementation and then a patch proposal for TAHITI.

## V1 Scope

- Instanced mode for 1 to 5 players.
- An interaction launches the content.
- The exact entry point will be defined later.
- A map is randomly selected from an approved pool.
- A final boss is randomly selected from an approved pool.
- The run is limited by a timer.
- Level progression is supported.
- End rewards reuse existing systems as much as possible.
- V1 must not depend on a manually distributed client patch.

## V1 Objective

- Deliver a first mode that is fun, replayable, and stable.
- Minimize technical debt.
- Avoid systems that are too ambitious for the first iteration.

## V1 Gameplay Loop

1. The player or group interacts with the Rift entry point.
2. The system determines the requested Rift level.
3. The server verifies that the level is unlocked for the player or leader.
4. The server creates a Rift instance.
5. The server selects a random map from the V1 pool.
6. The server selects a random boss from the V1 pool.
7. Players enter the instance.
8. The timer starts.
9. Players progress until the main objective is completed.
10. If the final boss dies before the timer ends, the run succeeds.
11. If the timer expires first, the run fails.
12. On success, players receive end-of-run loot.
13. On success, the next level is unlocked.

## V1 Group Rules

- Allowed group size: 1 to 5 players.
- The mode must work in solo without group prerequisites.
- In a group, V1 should ideally use the level selected by the leader.
- All players present in the instance at the start participate in the run.

## Victory Condition

- The run is won when the Rift final boss dies before the timer expires.

## Failure Condition

- The run is lost when the timer expires before the final boss dies.

## Timer

- Exact timer values will be defined later.
- V1 should support a visible timer through existing mission systems if possible.
- The timer should ideally start when the run becomes active.

## Level Progression

- Levels start at 1.
- Progression is potentially open-ended.
- Success at level N unlocks N+1.

## Loot and Rewards

- On timed success, the run grants an end reward.
- That reward should primarily reuse:
  - existing random world drops
  - the selected final boss's normal drops
- In addition, V1 applies a significant SIF / RIF bonus.
- The bonus must be configurable and easy for TAHITI to tune.

## V1 UI / UX

- Reuse existing mission / metagame widgets if possible.
- Avoid heavy custom UI in V1.
- If game file changes are required for entry-point presentation, they should ideally be deployable through the Patcher.

## TAHITI Compatibility

- The patch must be readable and clean.
- Core logic should stay server-side as much as possible.
- Any dependency on game files should remain minimal and Patcher-compatible.
