# Cosmic Rift Reward Tuning Guide

This guide is for TAHITI admins who want to tune Cosmic Rift rewards without rebuilding the server.

## Current Capability

Cosmic Rift rewards are controlled by:

```text
Data/Game/MythicRift/CosmicRiftRewards.json
```

The file can be edited on a running server, then reloaded in-game with:

```text
rift rewardconfig reload
```

Inspect the currently loaded profile with:

```text
rift rewardconfig
```

## What Can Be Tuned

- Enable or disable the reward profile.
- Grant or disable primary boss loot on success.
- Grant or disable primary boss loot on failure.
- Suppress native Rift boss ground drops to prevent double loot.
- Apply timed-success RIF / SIF bonuses.
- Apply extra checkpoint RIF / SIF bonuses.
- Apply failure RIF / SIF bonuses.
- Choose default reward delivery: `inventory` or `ground`.
- Replace the primary boss loot table by Rift level ranges.
- Add extra reward tables with chance and roll count.
- Filter reward rules by:
  - minimum Rift level
  - maximum Rift level
  - classic Rift only
  - checkpoint Rift only
  - map/content id
  - boss-source id

## Important Behavior

- Primary loot override entries are checked in order. The first matching enabled entry wins.
- Extra loot table entries are all checked independently. Every matching enabled entry can roll.
- `maxRiftLevel: 0` means no upper limit.
- `checkpointOnly: true` means only boss-only checkpoint Rifts.
- `classicOnly: true` means normal kill-count + boss Rifts only.
- `delivery: "inventory"` grants rewards directly to the player.
- `delivery: "ground"` spawns loot in-world.
- The JSON selects existing server loot table prototypes. Creating a brand-new loot table still needs the normal TAHITI data/patcher/live-tuning workflow.

## Safe Default

The current default keeps Cosmic Rift gameplay functional while avoiding native boss double-drops:

```json
{
  "enabled": true,
  "grantBossLootOnSuccess": true,
  "grantBossLootOnFailure": true,
  "suppressNativeRiftBossLoot": true,
  "defaultDelivery": "inventory"
}
```

## Example: Replace Primary Loot By Level Bands

This makes different Rift level ranges use different primary loot tables.

```json
"primaryLootTableOverrides": [
  {
    "id": "levels-1-24-primary",
    "enabled": true,
    "lootTablePrototype": "Loot/Tables/Example/LowTierRiftRewards.prototype",
    "minRiftLevel": 1,
    "maxRiftLevel": 24,
    "delivery": "inventory"
  },
  {
    "id": "levels-25-49-primary",
    "enabled": true,
    "lootTablePrototype": "Loot/Tables/Example/MidTierRiftRewards.prototype",
    "minRiftLevel": 25,
    "maxRiftLevel": 49,
    "delivery": "inventory"
  },
  {
    "id": "levels-50-plus-primary",
    "enabled": true,
    "lootTablePrototype": "Loot/Tables/Example/HighTierRiftRewards.prototype",
    "minRiftLevel": 50,
    "maxRiftLevel": 0,
    "delivery": "inventory"
  }
]
```

## Example: Extra Checkpoint Reward

This adds an extra guaranteed roll only on boss-only checkpoint Rifts.

```json
"extraLootTables": [
  {
    "id": "checkpoint-bonus",
    "enabled": true,
    "lootTablePrototype": "Loot/Tables/Example/CheckpointBonusRewards.prototype",
    "chancePercent": 100.0,
    "rolls": 1,
    "minRiftLevel": 5,
    "maxRiftLevel": 0,
    "successOnly": true,
    "checkpointOnly": true,
    "delivery": "inventory"
  }
]
```

## Example: Level 50+ Chase Drop

This adds a 10% extra reward roll on successful level 50+ Rifts.

```json
"extraLootTables": [
  {
    "id": "level-50-plus-chase",
    "enabled": true,
    "lootTablePrototype": "Loot/Tables/Example/RiftChaseDrop.prototype",
    "chancePercent": 10.0,
    "rolls": 1,
    "minRiftLevel": 50,
    "maxRiftLevel": 0,
    "successOnly": true,
    "delivery": "ground"
  }
]
```

## Example: Special Doop Reward

This targets only the Cosmic Doop Sector content id.

```json
"extraLootTables": [
  {
    "id": "cosmic-doop-sector-bonus",
    "enabled": true,
    "lootTablePrototype": "Loot/Tables/Example/CosmicDoopBonusRewards.prototype",
    "chancePercent": 100.0,
    "rolls": 2,
    "minRiftLevel": 25,
    "maxRiftLevel": 0,
    "successOnly": true,
    "contentIds": [ "cosmic-doop-sector" ],
    "bossSourceIds": [ "cosmic-doop-sector" ],
    "delivery": "ground"
  }
]
```

## Test Procedure

1. Edit `Data/Game/MythicRift/CosmicRiftRewards.json`.
2. Run:

```text
rift rewardconfig reload
```

3. Confirm the loaded profile:

```text
rift rewardconfig
```

4. Run a controlled Rift:

```text
rift armbeaconfixed taskmaster 10
```

5. Complete the Rift and inspect:

```text
rift run [runId]
```

The run diagnostics include:

- reward profile
- primary loot source id
- primary delivery mode
- bonus RIF / SIF
- extra reward tables selected for that run

## Practical Recommendation

For Test Center, start with conservative level bands:

- `1-24`: low / baseline reward table
- `25-49`: mid reward table
- `50+`: high reward table or chase reward
- checkpoint-only: small extra reward so levels `5`, `10`, `15`, etc. feel special

Keep `suppressNativeRiftBossLoot=true` unless deliberately testing native boss drops, because disabling it can reintroduce double rewards.
