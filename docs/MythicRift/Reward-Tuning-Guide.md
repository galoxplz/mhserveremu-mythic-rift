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
- Rift bosses always suppress native death loot to prevent double rewards; the setting remains visible for compatibility with existing profiles.
- Apply timed-success RIF / SIF bonuses.
- Apply extra checkpoint RIF / SIF bonuses.
- Apply failure RIF / SIF bonuses.
- Choose default reward delivery: `inventory` or `ground`.
- Replace the primary boss loot table by Rift level ranges.
- Add extra reward tables with chance and roll count.
- Define short loot-table aliases so prototype paths only need to be written once.
- Group multiple table rolls into reusable reward recipes.
- Create per-boss reward profiles by filtering recipes with `bossSourceIds`.
- Create random item pools from an approved prototype directory, with independent chance, roll count, item level, and encounter filters.
- Add guaranteed item prototypes with quantity, wave, level, checkpoint, map, and boss filters.
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
- Every matching reward recipe is evaluated, and each table inside that recipe rolls independently.
- A recipe can act as a global base reward, a level-band reward, a map reward, or a per-boss reward.
- `lootTableAliases` can be used by recipes and by the legacy primary/extra table entries.
- `maxRiftLevel: 0` means no upper limit.
- `checkpointOnly: true` means only boss-only checkpoint Rifts.
- `classicOnly: true` means normal kill-count + boss Rifts only.
- `delivery: "inventory"` grants rewards directly to the player.
- `delivery: "ground"` spawns player-owned loot in-world and is the default.
- In multi-boss waves, each distinct final boss contributes its own controlled boss table once.
- A `bossSourceIds` filter matches any boss in the final wave.
- Random item pools discover approved, live item prototypes beneath `prototypeDirectoryPrefix`. They intentionally do not apply the prototypes' ordinary `LootDropWeightMultiplier`, because the configured Rift chance controls selection.
- Guaranteed items use `itemPrototypeRuntimeId`; they are validated as item prototypes when the run reward is resolved.
- The JSON selects existing server loot table prototypes. Creating a brand-new loot table still needs the normal TAHITI data/patcher/live-tuning workflow.

## Shipped Reward Profile

The shipped `worthwhile-v4-all-cosmic-artifacts` profile keeps each final boss's normal reward table and adds deliberate ground loot:

- every successful floor: one `CS Grant Crate: 10 Cube Shards`
- waves `20-29`: one additional 10-shard crate
- wave `30`: two additional 10-shard crates
- successful boss-only checkpoints: one additional 10-shard crate
- levels `1-9`: a `10%` Cosmic artifact roll
- levels `10-19`: a `20%` Cosmic artifact roll
- levels `20-29`: a `35%` Cosmic artifact roll
- levels `30+`: a `50%` Cosmic artifact roll
- checkpoint Rifts: an additional `30%` Cosmic artifact chance below level `20`, or `75%` at level `20+`
- Cosmic Doop Sector: three additional Overlord-table rolls and one guaranteed Cosmic artifact
- every successful clear: one attempt against the existing costume/card/token chase table; that table retains its internal `99%` no-drop rate

All configured rewards are player-owned ground drops. Failed Rifts grant no managed reward. The old unrelated level-band terminal-table rolls were removed so reward identity follows the bosses actually fought.

The profile discovers items under:

```text
Entity/Items/Artifacts/Prototypes/SpecialArtifacts/CosmicArtifacts/
```

SIP inspection found `85` approved, live Cosmic artifact prototypes there. The stock `CosmicArtifactsTable` directly exposes only `48`, so the Rift-owned pool adds the `37` omitted artifacts as well. This includes `17` omitted prototypes with an ordinary loot weight of zero; directory pools select uniformly from the configured directory and do not use normal-world loot weights.

Selected artifacts are created at item level `63`, matching the stock Cosmic artifact table. Because the Rift creates the selected item directly, the drop does not require the region to use Cosmic difficulty and works in a red Danger Room-hosted Rift.

The safety settings remain:

```json
{
  "enabled": true,
  "grantBossLootOnSuccess": true,
  "grantBossLootOnFailure": false,
  "suppressNativeRiftBossLoot": true,
  "defaultDelivery": "ground"
}
```

Failure rewards can still be added deliberately with a recipe that sets `successOnly: false`.

## Example: Guaranteed Cube Shards

This grants the existing 10-shard crate on every successful floor:

```json
"guaranteedItems": [
  {
    "id": "cube-shards-every-floor",
    "enabled": true,
    "itemPrototypeRuntimeId": 12443539321764519670,
    "quantity": 1,
    "minRiftLevel": 1,
    "maxRiftLevel": 0,
    "minWave": 1,
    "maxWave": 0,
    "successOnly": true,
    "checkpointOnly": false,
    "contentIds": [],
    "bossSourceIds": [],
    "delivery": "ground"
  }
]
```

Multiple matching guaranteed-item entries stack. This is how later waves and checkpoints add more shard crates without creating a patched loot table.

## Example: Random Item Directory Pool

This gives a successful Rift a 25% chance to select one level-63 item from every approved, live Cosmic artifact prototype in the directory:

```json
"randomItemPools": [
  {
    "id": "all-cosmic-artifacts",
    "enabled": true,
    "prototypeDirectoryPrefix": "Entity/Items/Artifacts/Prototypes/SpecialArtifacts/CosmicArtifacts/",
    "chancePercent": 25.0,
    "rolls": 1,
    "itemLevel": 63,
    "minRiftLevel": 1,
    "maxRiftLevel": 0,
    "successOnly": true,
    "checkpointOnly": false,
    "contentIds": [],
    "bossSourceIds": [],
    "delivery": "ground"
  }
]
```

Each roll is independent. Selection is uniform across eligible prototypes, including prototypes whose ordinary loot drop weight is zero.

## Example: Per-Boss Reward Recipe

The primary reward already gives Shocker his normal terminal table. This recipe adds an independent 25% supplemental roll. More distinct tables can be added to the same recipe without creating a patched mega-table.

```json
"lootTableAliases": {
  "shocker-terminal": "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/AbandonedSubway/ShockerTerminalLoot.prototype"
},
"rewardRecipes": [
  {
    "id": "shocker-boss-profile",
    "enabled": true,
    "minRiftLevel": 1,
    "maxRiftLevel": 0,
    "successOnly": true,
    "bossSourceIds": [ "shocker" ],
    "tables": [
      {
        "id": "bonus-roll",
        "lootTable": "shocker-terminal",
        "chancePercent": 25.0,
        "rolls": 1,
        "delivery": "ground"
      }
    ]
  }
]
```

To create a level-wide base reward, omit `bossSourceIds`. To target several bosses with one profile, list all of their content ids.
Set `grantBossLootOnSuccess: false` only when recipes are intended to replace the primary boss table entirely.

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
    "delivery": "ground"
  },
  {
    "id": "levels-25-49-primary",
    "enabled": true,
    "lootTablePrototype": "Loot/Tables/Example/MidTierRiftRewards.prototype",
    "minRiftLevel": 25,
    "maxRiftLevel": 49,
    "delivery": "ground"
  },
  {
    "id": "levels-50-plus-primary",
    "enabled": true,
    "lootTablePrototype": "Loot/Tables/Example/HighTierRiftRewards.prototype",
    "minRiftLevel": 50,
    "maxRiftLevel": 0,
    "delivery": "ground"
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
    "delivery": "ground"
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
- reward recipe/table ids selected for that run
- selected random-pool items and their item level
- guaranteed item ids, quantities, and delivery

## Practical Recommendation

For Test Center, start with conservative level bands:

- `1-24`: low / baseline reward table
- `25-49`: mid reward table
- `50+`: high reward table or chase reward
- checkpoint-only: small extra reward so levels `5`, `10`, `15`, etc. feel special

`suppressNativeRiftBossLoot` remains in the JSON for compatibility, but managed Rift bosses now always suppress native death loot. Use managed reward tables and recipes for all intended Rift boss drops.
