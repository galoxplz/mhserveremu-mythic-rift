# Cosmic Rift Terminal Compatibility Audit

Date: 2026-05-03

Purpose: identify terminal maps that are risky for the Cosmic Rift loop because their normal terminal flow depends on native checkpoints, transition nodes, doors, elevators, hotspot triggers, or scripted mission progression. Cosmic Rift currently wants a clean loop: enter map, kill quota, spawn random boss on the player, kill boss, finish.

Important baseline finding from earlier testing: Green terminal `StartTarget` data can resolve into native `RegionBand` variants. The current code now prefers the configured `AltRegions/*RegionL60` terminal region refs for Rift content, matching MonEll's local fix direction and avoiding the older `RegionBase` / `RegionBand` drift where possible.

## Recommended Random Pool

For the next MonEll/team test pass, the safe random pool can include every terminal whose transition chain has no mission-gated door, miniboss gate, Kismet gate, hotspot gate, or spawner gate.

| Terminal | Status | Why |
|---|---|---|
| Shocker | Safe candidate | Simple start target plus boss checkpoint target. No extra transition node found. |
| Doctor Octopus / Kingpin Warehouse | Safe candidate | Simple start target plus boss checkpoint target. No extra transition node found. |
| Taskmaster | Safe candidate | Simple start target plus boss checkpoint target. No extra transition node found. |
| Hood | Safe candidate | Upper/lower deck transition uses open transition targets; no mission action gates it. |
| Mister Sinister | Safe candidate | Boss-room transition uses open transition targets; no mission action gates it. |
| MODOK / AIM Facility | Safe candidate | MODOK transition uses open transition targets; no mission action gates it. |
| Mandarin / HYDRA Island | Safe candidate | Boss transition uses Mandarin portal targets; no mission action gates it. |
| Kingpin / Fisk Tower | Safe candidate | Elevator/office transition targets exist, but no mission action gates them. |

These are the best candidates for proving the full multiplayer Rift loop without terminal-specific scripts interfering. The remaining risk with multi-area terminals is practical rather than scripted: players must still be able to reach enough enemy population naturally before the Rift boss spawns.

## Risk List

| Terminal | Risk | Evidence | Recommendation |
|---|---|---|---|
| Shocker | Low | `DailyGShockerSubwayStartTarget`, `DailyGShockerCPTarget`; native checkpoint objective only. | Keep in random pool. |
| Doctor Octopus / Kingpin Warehouse | Low | `DailyGKPWarehouseStartTarget`, `DailyGKPBossCPTarget`; native checkpoint objective only. | Keep in random pool. |
| Taskmaster | Low | `DailyGTaskmasterStartTarget`, `DailyGTaskmasterBossCPTarget`; native checkpoint objective only. | Keep in random pool. |
| Hood | Low-medium | Has upper/lower deck transition: `DailyGHoodsShipUpperToLowerNod`, `DailyGHoodsShipUpperExitTarget`, `DailyGHoodsShipLowerEntryTarg`, and a checkpoint target. No Kismet, spawner trigger, entity-state action, hotspot gate, or miniboss gate was found in the mission flow. | Keep in random pool, monitor kill-count pacing. |
| Magneto / Stryker Bunker | High / known issue | Has bunker transition node and door targets: `DailyGBunkerToMagnetoNode`, `DailyGPyroToMagnetoTarget`, `DailyGMagnetoBunkerStartTarget`, `BunkerDoor`, `BunkerDoor1`. MonEll already reported Bunker entering/behaving incorrectly. | Registered on L60 for fixed validation, but keep excluded from random pool until transition handling is proven safe. |
| Mister Sinister | Low-medium | Has boss transition node: `DailyGSinisterLabBossNode`, `DailyGSinisterLabBossEXT`, `DailyGSinisterLabBossINT`, plus checkpoint target. No Kismet, spawner trigger, entity-state action, hotspot gate, or miniboss gate was found in the mission flow. | Keep in random pool, monitor kill-count pacing. |
| MODOK / AIM Facility | Low-medium | Has MODOK transition node: `DailyGAIMFacToModokNode`, `DailyGAIMFacToModokTarget`, `DailyGAIMFacBossStartTarget`, plus checkpoint target. No Kismet, spawner trigger, entity-state action, hotspot gate, or miniboss gate was found in the mission flow. | Keep in random pool, monitor kill-count pacing. |
| Mandarin / HYDRA Island | Low-medium | Has level 2 to boss transition: `DailyGHYDRAIslandLvl2ToBossNod`, `DailyGHYDRAIslandLvl2EXITTarg`, `DailyGMandarinBossEntryTarget`, plus checkpoint target. No Kismet, spawner trigger, entity-state action, hotspot gate, or miniboss gate was found in the mission flow. | Keep in random pool, monitor kill-count pacing. |
| Doctor Doom / Castle Doom | High | Has elevator/boss-room transition and multiple sub-boss checkpoints: `DailyGElevatorToBossRoomNode`, `DailyGDoomBossElevatorTarget`, `DailyGDoomElevatorTarget`, `BunkerDoor`, sub-boss checkpoint targets. | Do not add to random pool yet. |
| Kingpin / Fisk Tower | Low-medium | Has floor-to-boss node and elevator/office door target: `DailyGFiskTowerDToBossNode`, `DailyGFiskTowerFloorDTarget`, `DailyGFiskTowerBossEntryTarget`, `ElevatorPortal1`, `OfficeDoorwayPortalFlat1`. No Kismet, spawner trigger, entity-state action, hotspot gate, or miniboss gate was found in the mission flow. | Keep in random pool, monitor kill-count pacing. |
| Kurse / Asgard Instance | Medium-high | Static region, but mission has multiple checkpoint objectives and a hotspot-enter condition. | Do not add to random pool until manually validated. |
| Juggernaut / Purifier Church | High | Has exterior-to-interior boss node and door transition: `DailyGBossEXTToBossINTNode`, `DailyGJuggyBossEXTTarget`, `DailyGJuggyBossINTTarget`, `SP03FPDoor`, `PurifierJuggyTransition1`. | Do not add to random pool yet. |
| K'lrt / Hightown Invasion | High | Has hotel transition node and several mission hotspot/spawner triggers: `DailyGHighTownToHotelNode`, `DailyGHighTownInvasionHotelEntryTarget`, `DailyGHighTownInvasionHotelDestTarget`, multiple `SpawnerTrigger` and `HotspotEnter` objectives. | Do not add to random pool yet. |
| Ultron / Times Square | Blocker / known issue | Has restaurant/street/roof/hotel transition nodes and mission action that kills/despawns exit doors: `DailyGTimesSquareRestToStreetNode`, `DailyGTimesSquareRoofToHotelNode`, `DestructibleExitDoors`. MonEll's local L60-region change appears promising, but repeated/multiplayer behavior still needs validation. | Registered on L60 for fixed validation, but keep excluded from random pool until confirmed safe. |

## Practical Decision

The Rift loop should not depend on native terminal mission progression. Any terminal with native transition nodes can still be useful later, but only after one of these approaches is implemented:

- Force enough kill quota targets into the first reachable combat area.
- Spawn Rift enemies independently from the native terminal population.
- Teleport the player directly between configured Rift-safe areas.
- Build a curated Rift-only map pool from terminal areas that are known to be open and self-contained.

Until then, the safest pre-production direction is to keep the validated non-gated terminal pool active, exclude known gated/problematic terminals, and grow the pool one terminal at a time after local and TAHITI validation.

## First Non-Terminal Map Expansion

The next content expansion adds map-only Rift entries from private combat regions outside the terminal folder. These entries are eligible as maps only; they do not supply bosses or loot tables. Bosses remain selected from the validated terminal boss pool.

| Map id | Region | Start target | Why selected |
|---|---|---|---|
| `bronx-zoo` | `BronxZooRegionL60` | `ZooEntryTarget` | Large private one-shot map with many populated areas and no terminal boss dependency. |
| `wakanda-jungle` | `WakandaP1RegionL60` | `WakandaP1EntryTarget` | Private one-shot map with multiple populated areas and no registered metagame in the region data. |
| `hydra-island-one-shot` | `HYDRAIslandPartDeuxRegionL60` | `Hydra1ShotEntryTarget` | Private one-shot map with many populated areas; selected for HYDRA visual variety. |
| `daily-bugle` | `OpDailyBugleRegionL11To60` | `OpsDailyBugleStartTarget` | Private operation/event map with several populated areas and no terminal mission dependency. |
| `dr-strange-times-square` | `DrStrangeTimesSquareRegionCosmic` | `DrStrangeTimesSquareEntryTargetCosmic` | Private static scenario map with simple entry data and no metagame listed. |

Recommended smoke-test command sequence:

```text
rift validatecontent
rift validaterandompool 1 1 10
rift prepbeacon 1 5
rift armbeaconfixed bronx-zoo 10
rift armbeaconfixed wakanda-jungle 10
rift armbeaconfixed hydra-island-one-shot 10
rift armbeaconfixed daily-bugle 10
rift armbeaconfixed dr-strange-times-square 10
```

Use one beacon after each `armbeaconfixed` command. Expected result: teleport succeeds, kill quota progresses from the selected map population, the random terminal boss spawns only after quota completion, and cleanup works after completion, abandonment, or timeout.

## Special Cosmic Doop Rift

The data also contains the remembered space Doop zone:

| Map id | Region | Start target | Population | Boss | Random behavior |
|---|---|---|---|---|---|
| `cosmic-doop-sector` | `CosmicDoopSectorSpaceRegion` | `CosmicDoopSectorSpaceStartTarget` | `EGDoopZonePop` | `CosmicDoopOverlord` | Special 5% chance, fixed own boss, kill quota 100 |

This entry is intentionally not treated as a normal map-only entry. It has its own fixed boss and loot table, and it is not added to the normal random boss-source pool.

Direct smoke test:

```text
rift validatecontent
rift prepbeacon 1 1
rift armbeaconfixed cosmic-doop-sector 10
```

Use one beacon after arming. Expected result: the run enters the Cosmic Doop space region, counts native Doop population kills, spawns `CosmicDoopOverlord` after quota completion, and completes only when that boss dies.

## Detailed Recheck Notes

The following terminals were rechecked specifically because they have transition nodes but may not have special progression gates:

| Terminal | Recheck result |
|---|---|
| Hood | Transition is `OpenTransitionSmlSoft` -> `OpenTransitionSmlFlat`; mission has only boss death objective and native region shutdown. |
| Mister Sinister | Transition is `OpenTransitionMedSoft2` -> `OpenTransitionMedSoftFlat`; mission has only boss death objective and native region shutdown. |
| MODOK | Transition is `OpenTransitionSmlSoft` -> `OpenTransitionSmlSoftFlat`; mission has only boss death objective and native region shutdown. |
| Mandarin | Transition is `MandarinPortal` -> `MandarinPortal2`; mission has only boss death objective and native region shutdown. |
| Kingpin / Fisk Tower | Transition is `ElevatorPortal1` -> `OfficeDoorwayPortalFlat1`; mission has only boss death objective and native region shutdown. |

No `Kismet`, `SpawnerTrigger`, `EntitySetState`, `EntityCreate`, `HotspotEnter`, or miniboss-gated action was found for these five terminal flows.
