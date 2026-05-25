# Cosmic Rift Player Feedback Triage

This file tracks the first wider-player review pass shared by MonEll on 2026-05-18.

## Feedback Pass: 2026-05-21

- `rift status` works both inside and outside a Rift.
- `rift abandon` works.
- `rift level [X]` works, but testers found the selected level too sticky after clearing. The current follow-up makes it a true one-shot launch override: the next successful beacon uses that level, then later beacons default back to the highest unlocked level.
- MonEll flagged a possible `rift level` progression-destruction issue. Code review showed `rift level` does not write the highest-unlocked value, but the UX could look like progress was lost because the next launch level stayed lower. The command text now states that lower-level farming is one-shot, and access-prep helpers no longer lower existing progression by accident.
- Boss-only checkpoint rooms add good variety, and most tested rooms worked.
- `tr-asgard-estate`, `supervillain-rec-center`, and `sc-kill-house` reported boss spawns outside the playable room. The checkpoint boss spawn resolver now prefers valid positions in the player's current cell/room and falls back to the player's position rather than a forward offset that can cross walls.
- `sc-missile-silo`, `sc-mineshaft`, `sc-dino-graveyard`, `sc-fire-swamp`, `tr-norway-tomb`, and `tr-sacred-dojo` were reported as working.
- `sabretooth-showdown` is unstable as a clean checkpoint candidate: it can load, but reports included missing HUD/teleporter/rewards and it likely has a native Sabretooth encounter plus the Rift boss. It remains available for fixed command diagnostics but is excluded from automatic random checkpoint selection for V1.
- A tester asked for raid bosses at milestone levels such as 50 or 100. This fits the checkpoint-tier idea well, but should be treated as a later curated milestone-boss extension rather than added blindly to the normal V1 boss pool.
- Follow-up feedback reported that dying in `sc-fire-swamp` / `sc-mineshaft` could refresh into the story-mode version. Rift death release is now intercepted so respawn stays inside the same active Rift instance.
- Follow-up feedback reported that Mythic Rift items could be used in Story Mode and teleport to a story-mode version. Launcher use is now gated to the Danger Room hub and rejected elsewhere before native item behavior can run.

## Fixed / Improved In This Pass

- Map rotation felt too repetitive. The server now keeps a short recent-map history per requester and party member and excludes those recent picks when the random pool has alternatives.
- The dedicated launcher item should not reuse the generic Danger Room scenario name. The vendor path now tries to sell the existing presentation shell `DangerRoomScenarioCrateUniqueCableFight`, localized as `Mythic Rift Scenario`, while the technical launcher/fallback remains `PortalToRandomMaxAffixDungeon`.
- Admin reset already exists through `rift resetprogress`; this is intended for test cleanup, not automatic reset on relog.
- Compact StoryRevamp / showdown / treasure-room maps are now checkpoint boss rooms instead of classic quota maps. Every 10th Rift level routes to one of these rooms, summons a random validated boss immediately, and uses extra boss health tuning on top of normal Rift scaling.
- Checkpoint rooms now hide the kill-count bar, because players would otherwise see a misleading `1/1` style objective in a boss-only room.
- Checkpoint clears now get a small extra timed-success reward bonus so the mandatory tier gate feels more special than a normal Rift level.
- Checkpoint group progression now uses boss-death presence for eligibility, so slower-loading players should not be punished by the instant boss spawn as long as they are present when the boss dies.
- Checkpoint completion chat now says `Checkpoint cleared` instead of the generic Rift-complete wording, making the every-10-level tier gate easier for players to understand.
- Checkpoint boss spawn no longer aborts the run immediately if the first spawn attempt fails. The server retries while the run is active, which should prevent a transient spawn/anchor problem from making all later Rift launches appear broken.

## Expected / Current Design

- Rift progression persists across relog by design.
- A failed run does not reset a player's progression to level 1; it simply does not unlock the next level.
- Group completion unlocks the next level for eligible players who were present for the competitive requirements. This is intentional for group play, but should remain under review for anti-carry tuning.
- Loot is still prototype/boss-table based and not final. Cube shard inconsistency and underwhelming drops are expected until the reward layer becomes externally tunable.
- Random enemy replacement is not implemented for normal terminal maps yet. Terminals still use their native population while Rift map and boss source are randomized; the custom-spawn experiment remains useful for future content but is no longer the default direction for tiny treasure rooms.

## Needs More Test Logs

- One-shot runs sometimes reported no level/timer and no level-up. If this still happens on the latest build, capture `rift status`, `rift run [runId]`, `rift objectives`, and the server log around region bind.
- Leaving a party inside the Danger Room hub then starting a solo run reportedly bricked the launch into native goals. If this reproduces, capture `rift beaconmode`, `rift status`, current `!region info`, and whether the player re-entered the Danger Room hub before buying/using the item.
- Relogging and rejoining a run while the map/boss changed needs a focused reproduction because a live active run should keep its registered map/boss config.

## Design Backlog

- External reward tuning file with live reload/admin reload command is now started through `Data/Game/MythicRift/CosmicRiftRewards.json` and `rift rewardconfig reload`; next step is real TAHITI reward values.
- Better anti-carry / level unlock policy if high-level friends can push low-level players too far too quickly.
- Optional gauntlet / every-5th-room boss challenge model.
- Optional infinite-wave mode, likely as a separate Rift variant rather than replacing the current GRift-style flow.
- Treasure rooms, patrol-wave rooms, and `SHOWDOWN`-style content investigation in Open Calligraphy.
- Tune checkpoint boss health/rewards after TAHITI validates the every-10-level pacing.
- Watch player feedback on whether every-10-level checkpoints feel exciting or disruptive. The V1 direction is mandatory checkpoints, but the interval and reward bump are both easy tuning knobs.
- Extend the custom population system to Bugle-style low-population maps if they remain underfilled after focused tests.
