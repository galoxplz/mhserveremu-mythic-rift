# Cosmic Rift Player Progression Guide

Use this as the short playtest instruction sheet when Test Center starts from a fresh database.

## Quick Summary

- Every player starts with Cosmic Rift level `1` unlocked.
- Using a Rift launcher item starts the player's highest unlocked Rift level by default.
- Completing Rift level `N` unlocks Rift level `N+1`.
- Failing, abandoning, leaving early, or timing out does not unlock the next level.
- One launcher item is consumed per Rift attempt.
- Players can only launch from the Danger Room hub.

## Normal Player Flow

1. Go to the Danger Room hub.
2. Buy the Rift launcher item from the Danger Room vendor.
3. Optional: type `rift status` to see your highest unlocked level and next launch level.
4. Use the launcher item from the Danger Room hub.
5. Clear the Rift objective before the timer expires.
6. Kill the Rift boss when it appears.
7. Use the exit portal after completion to return to the Danger Room hub.
8. Buy/use another launcher item to continue to the next Rift level.

## Classic Rift Levels

Most Rift levels use the classic flow:

1. Enter a random Rift map.
2. Kill enemies until the progress bar / kill quota is complete.
3. The final boss spawns only after the kill quota is complete.
4. Kill the boss before the timer expires.
5. Eligible players unlock the next Rift level.

## Checkpoint Rift Levels

Every 10th level is a boss-only checkpoint:

- Rift levels `10`, `20`, `30`, etc. are mandatory checkpoint levels.
- These use smaller rooms instead of normal terminal maps.
- There is no kill quota phase.
- The boss spawns immediately and is tuned to be harder.
- Clearing level `10` unlocks level `11`, clearing level `20` unlocks level `21`, and so on.

## Group Progression Rules

Cosmic Rift progression is competitive but group-friendly:

- The run is valid for the group if the Rift objectives are completed before the timer expires.
- The party leader does not own the run after launch; if the leader disconnects or leaves, remaining players can still finish if they stay inside.
- Players do not need to be alive at the end, but they must still be present inside the Rift when the boss dies.
- In classic Rift levels, players should be inside the Rift when the kill quota unlocks the boss and still inside when the boss dies.
- In checkpoint Rift levels, players should be inside the Rift when the checkpoint boss dies.
- A player who leaves the Rift early becomes ineligible for rewards and next-level unlocks for that run.
- If everyone leaves, the Rift is cleaned up and a new launcher item is required.

For clean multiplayer tests, all intended party members should stand in the Danger Room hub before the launcher item is used.

## Player Commands

```text
rift status
```

Shows whether you have an active Rift, your highest unlocked Rift level, and the level your next launcher item will open.

```text
rift level
```

Shows your next launch level and highest unlocked level.

```text
rift level X
```

Arms one lower-level farming run, if level `X` is already unlocked. This does not lower your progression and is consumed after the next successful launcher use.

Example: if your highest unlocked level is `50`, `rift level 25` makes only the next launcher open level `25`. After that launch, future launchers go back to level `50` by default.

```text
rift level max
```

Clears the one-shot lower-level selection and makes the next launcher use your highest unlocked level.

```text
rift abandon
```

Cancels your active Rift attempt, returns online participants to the Danger Room hub, and allows a fresh attempt. This costs the current Rift attempt.

```text
rift recover
```

Emergency test command. Clears temporary launcher state and safely abandons/removes your active Rift if your session gets stuck.

## What Players Should Avoid During Playtests

- Do not use Rift launcher items outside the Danger Room hub.
- Do not relog as the first solution if a Rift is stuck; try `rift recover` first.
- Do not use admin-only commands such as `rift armbeaconfixed`, `rift setaccess`, or `rift resetprogress` during normal player-flow testing unless MonEll specifically asks for it.
- Do not expect `rift level X` to permanently set your farm level. It is intentionally one-shot.

## Admin Reset For Fresh Tests

Admins can reset a tester back to Rift level `1` with:

```text
rift resetprogress
```

This also clears any one-shot launch level selection.

## Suggested Fresh-Database Playtest Script

1. Confirm every tester starts with `rift status`.
2. Everyone should see highest unlocked Rift level `1`.
3. Solo player launches and clears level `1`.
4. Confirm `rift status` now shows highest unlocked level `2`.
5. Repeat until level `3` or `4` to confirm normal chaining.
6. Test `rift level 1` after unlocking higher levels, then launch once.
7. Confirm the following launch returns to the highest unlocked level by default.
8. Test a party run with all players standing in the Danger Room hub before launch.
9. Confirm all players who stay inside until boss death unlock the next level.
10. If a session gets stuck, use `rift recover` and report the map, boss, current `rift status`, and any server log around the issue.
