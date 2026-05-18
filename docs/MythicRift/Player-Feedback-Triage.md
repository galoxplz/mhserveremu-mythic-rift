# Cosmic Rift Player Feedback Triage

This file tracks the first wider-player review pass shared by MonEll on 2026-05-18.

## Fixed / Improved In This Pass

- Map rotation felt too repetitive. The server now keeps a short recent-map history per requester and party member and excludes those recent picks when the random pool has alternatives.
- The dedicated launcher item should not reuse the generic Danger Room scenario name. The vendor path now tries to sell the existing presentation shell `DangerRoomScenarioCrateUniqueCableFight`, localized as `Mythic Rift Scenario`, while the technical launcher/fallback remains `PortalToRandomMaxAffixDungeon`.
- Admin reset already exists through `rift resetprogress`; this is intended for test cleanup, not automatic reset on relog.

## Expected / Current Design

- Rift progression persists across relog by design.
- A failed run does not reset a player's progression to level 1; it simply does not unlock the next level.
- Group completion unlocks the next level for eligible players who were present for the competitive requirements. This is intentional for group play, but should remain under review for anti-carry tuning.
- Loot is still prototype/boss-table based and not final. Cube shard inconsistency and underwhelming drops are expected until the reward layer becomes externally tunable.
- Random enemy replacement is not implemented yet. Maps still use their native population while Rift map and boss source are randomized.

## Needs More Test Logs

- One-shot runs sometimes reported no level/timer and no level-up. If this still happens on the latest build, capture `rift status`, `rift run [runId]`, `rift objectives`, and the server log around region bind.
- Leaving a party inside the Danger Room hub then starting a solo run reportedly bricked the launch into native goals. If this reproduces, capture `rift beaconmode`, `rift status`, current `!region info`, and whether the player re-entered the Danger Room hub before buying/using the item.
- Relogging and rejoining a run while the map/boss changed needs a focused reproduction because a live active run should keep its registered map/boss config.

## Design Backlog

- External reward tuning file with live reload or admin reload command.
- Better anti-carry / level unlock policy if high-level friends can push low-level players too far too quickly.
- Optional gauntlet / every-5th-room boss challenge model.
- Optional infinite-wave mode, likely as a separate Rift variant rather than replacing the current GRift-style flow.
- Treasure rooms, patrol-wave rooms, and `SHOWDOWN`-style content investigation in Open Calligraphy.
- Dedicated custom population system for non-terminal maps, including Bugle-style low-population fixes.
