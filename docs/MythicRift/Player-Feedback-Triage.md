# Cosmic Rift Player Feedback Triage

This file tracks the first wider-player review pass shared by MonEll on 2026-05-18.

## Feedback Pass: 2026-06-02

- Google Sheet feedback reported a player dying, timing out, being kicked out of the Rift, then remaining stuck in a dead state until relog.
- Google Sheet feedback reported group portals bricking unless the player made one solo first, and leader swaps between runs sometimes reverting to terminal / one-shot behavior.
- Google Sheet feedback reported bosses spawning around Rocket Raccoon turrets, minions, or a player far behind the group.
- Google Sheet feedback reported double rewards, for example a medallion going directly to inventory and also dropping on the ground.
- Additional feedback reported that some treasure rooms still contain a native story/campaign portal plus the Rift return portal, which is confusing.
- A larger design suggestion proposed a seasonal / capped progression model with a rewarding endgame reset, plus boss-wave variety instead of only infinite HP scaling.

## Fixed / Improved From 2026-06-02 Feedback

- Rift-forced exits now try to resurrect a dead avatar before teleporting that player back to the Danger Room hub. This targets the timeout/death-stuck report directly.
- Party member teleports now use the run's registered participant roster instead of depending on the party leader still matching at the exact teleport moment. This should make leader swaps and party-state timing less likely to fall out of the Rift path.
- Rift boss spawning now prefers a real alive player in the Rift as the spawn anchor. It no longer uses the killed mob/turret/minion location as the primary boss spawn point.
- Rift-spawned bosses now suppress native boss loot by default through `suppressNativeRiftBossLoot=true`, leaving the Cosmic Rift reward manager as the controlled reward source and preventing native boss loot plus Rift-granted boss loot from doubling.

## Still Open From 2026-06-02 Feedback

- Axis / March to Axis UI issues should remain out of the normal random pool until the exact content id and native UI interaction are isolated.
- Anti-carry progression policy is still a design decision: current rules intentionally unlock the next level for eligible players present in the Rift, even if they joined a higher-level friend.
- The seasonal hard-stop / boss-wave idea is promising, but it is a V2 design discussion rather than a P0 bug fix.

## P1 Started From 2026-06-02 Feedback

- Treasure-room / StoryRevamp native exits are now intercepted during boss-only checkpoint Rifts. The official Cosmic Rift return portal still works, but native story/campaign exits in the same Rift room are blocked with a chat explanation so players are not sent to campaign/story flow by mistake.
- MODOK remains available as a fixed map test target, but the MODOK boss source is temporarily removed from the random boss pool until the "does not move / does not attack" report is reproduced or fixed. This keeps AIM Facility map variety while avoiding a flaky random final boss.
- Boss-only checkpoint rooms now suppress their native hostile population before the Rift boss is spawned, and native population respawns are not enabled for checkpoint rooms. This is intended to clean up native encounters such as Sabretooth and make the rooms behave like controlled Rift boss arenas.
- `sabretooth-showdown`, `supervillain-rec-center`, `sc-kill-house`, and `tr-asgard-estate` are no longer excluded from automatic every-5-level checkpoint selection. They still need focused Test Center validation, but the known native-boss/native-portal/spawn-location issues now have server-side mitigations.
- `daily-bugle` is now eligible as a normal random Rift map again, backed by Rift custom population spawns so the low native population should no longer brick kill-quota progression.

## Feedback Pass: 2026-05-21

- `rift status` works both inside and outside a Rift.
- `rift abandon` works.
- `rift level [X]` works, but testers found the selected level too sticky after clearing. The current follow-up makes it a true one-shot launch override: the next successful beacon uses that level, then later beacons default back to the highest unlocked level.
- MonEll flagged a possible `rift level` progression-destruction issue. Code review showed `rift level` does not write the highest-unlocked value, but the UX could look like progress was lost because the next launch level stayed lower. The command text now states that lower-level farming is one-shot, and access-prep helpers no longer lower existing progression by accident.
- Boss-only checkpoint rooms add good variety, and most tested rooms worked.
- `tr-asgard-estate`, `supervillain-rec-center`, and `sc-kill-house` reported boss spawns outside the playable room. Boss spawning now prefers valid positions in the player's current room/cell, and these maps have been re-enabled for automatic checkpoint selection for retesting.
- `sc-missile-silo`, `sc-mineshaft`, `sc-dino-graveyard`, `sc-fire-swamp`, `tr-norway-tomb`, and `tr-sacred-dojo` were reported as working.
- `sabretooth-showdown` was unstable as a clean checkpoint candidate because it likely had a native Sabretooth encounter plus the Rift boss. Checkpoint rooms now suppress native hostile population before the Rift boss spawns, so this map is back in automatic checkpoint selection for retesting.
- A tester asked for raid bosses at milestone levels such as 50 or 100. This fits the checkpoint-tier idea well, but should be treated as a later curated milestone-boss extension rather than added blindly to the normal V1 boss pool.
- Follow-up feedback reported that dying in `sc-fire-swamp` / `sc-mineshaft` could refresh into the story-mode version. Rift death release is now intercepted so respawn stays inside the same active Rift instance.
- Follow-up feedback reported that Mythic Rift items could be used in Story Mode and teleport to a story-mode version. Launcher use is now gated to the Danger Room hub or to a successfully cleared Rift, and rejected elsewhere before native item behavior can run.
- MonEll approved changing checkpoint pacing to every 5 levels and softening high-level scaling because stacked players were getting stuck around level 29-30.

## Fixed / Improved In This Pass

- Map rotation felt too repetitive. The server now keeps a short recent-map history per requester and party member and excludes those recent picks when the random pool has alternatives.
- The dedicated launcher item should not reuse the generic Danger Room scenario name. The vendor path now tries to sell the existing presentation shell `DangerRoomScenarioCrateUniqueCableFight`, localized as `Mythic Rift Scenario`, while the technical launcher/fallback remains `PortalToRandomMaxAffixDungeon`.
- Admin reset already exists through `rift resetprogress`; this is intended for test cleanup, not automatic reset on relog.
- Compact StoryRevamp / showdown / treasure-room maps are now checkpoint boss rooms instead of classic quota maps. Every 5th Rift level routes to one of the currently validated rooms, summons a random validated boss immediately, and uses extra boss health tuning on top of normal Rift scaling.
- Checkpoint rooms now hide the kill-count bar, because players would otherwise see a misleading `1/1` style objective in a boss-only room.
- Checkpoint clears now get a small extra timed-success reward bonus so the mandatory tier gate feels more special than a normal Rift level.
- Checkpoint group progression now uses boss-death presence for eligibility, so slower-loading players should not be punished by the instant boss spawn as long as they are present when the boss dies.
- Checkpoint completion chat now says `Checkpoint cleared` instead of the generic Rift-complete wording, making the every-5-level tier gate easier for players to understand.
- Checkpoint boss spawn no longer aborts the run immediately if the first spawn attempt fails. The server retries while the run is active, which should prevent a transient spawn/anchor problem from making all later Rift launches appear broken.

## Expected / Current Design

- Rift progression persists across relog by design.
- A failed run does not reset a player's progression to level 1; it simply does not unlock the next level.
- Group completion unlocks the next level for eligible players who were present for the competitive requirements. This is intentional for group play, but should remain under review for anti-carry tuning.
- Loot is still prototype/boss-table based and not final. Cube shard inconsistency and underwhelming drops are expected until the reward layer becomes externally tunable.
- Random enemy replacement is not implemented for normal terminal maps yet. Terminals still use their native population while Rift map and boss source are randomized; the custom-spawn experiment remains useful for future content but is no longer the default direction for tiny treasure rooms.
- Cosmic Doop Sector remains a special 5% map, but now only enters the random pool from Rift level 25 onward.
- Reward tuning can now override the primary loot table by level/range and choose `inventory` or `ground` delivery per primary/extra table.

## Needs More Test Logs

- One-shot runs sometimes reported no level/timer and no level-up. If this still happens on the latest build, capture `rift status`, `rift run [runId]`, `rift objectives`, and the server log around region bind.
- Leaving a party inside the Danger Room hub then starting a solo run reportedly bricked the launch into native goals. If this reproduces, capture `rift beaconmode`, `rift status`, current `!region info`, and whether the player re-entered the Danger Room hub before buying/using the item.
- Relogging and rejoining a run while the map/boss changed needs a focused reproduction because a live active run should keep its registered map/boss config.

## Design Backlog

- External reward tuning file with live reload/admin reload command is now started through `Data/Game/MythicRift/CosmicRiftRewards.json` and `rift rewardconfig reload`; next step is real TAHITI reward values for primary overrides and extra tables.
- Better anti-carry / level unlock policy if high-level friends can push low-level players too far too quickly.
- Optional infinite-wave mode, likely as a separate Rift variant rather than replacing the current GRift-style flow.
- Treasure rooms, patrol-wave rooms, and `SHOWDOWN`-style content investigation in Open Calligraphy.
- Tune checkpoint boss health/rewards after TAHITI validates the every-5-level pacing.
- Watch player feedback on whether every-5-level checkpoints feel exciting or disruptive. The V1 direction is mandatory checkpoints, but the interval and reward bump are both easy tuning knobs.
- Watch Daily Bugle after the custom-population pass and tune the custom spawn target/batch values if it feels too dense or still underfilled.
