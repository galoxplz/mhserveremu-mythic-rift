# Mythic Rift SIP Content Audit

Date: 2026-06-13

## Inventory

The current `Calligraphy.sip` contains:

- `975` concrete region prototypes with valid start targets
- `753` agent prototypes under `Entity/Characters/Bosses`

The boss count is a raw discovery count, not a safe boss count. It includes real bosses, phase variants, summons, markers, turrets, obelisks, power entities, and deprecated/test agents. Each boss still needs spawn, AI, bounds, and reward validation before entering the random pool.

## Cosmic Artifact Pool

The SIP directory `Entity/Items/Artifacts/Prototypes/SpecialArtifacts/CosmicArtifacts/` contains `85` approved, live item prototypes. The stock `Loot/Tables/Mob/Bosses/CosmicArtifactsTable.prototype` directly contains only `48`.

The `37` missing prototypes are all live-tuning enabled. Of those, `17` have `LootDropWeightMultiplier=0`, which explains why using ordinary loot selection rules would continue to hide part of the directory. Mythic Rift random item pools deliberately ignore ordinary drop weight and select uniformly from every approved, live item below the configured directory.

## Endless Launcher Candidate

Recommended unused technical base:

```text
Entity/Items/Consumables/Prototypes/DangerRoom/PortalToDangerRoomRandomThemeNoAffixesPurple.prototype
runtimeId=1020710291701049759
```

Audit properties:

- `DevelopmentOnly`
- `IsUsable=True`
- `LiveTuningDefaultEnabled=True`
- `LootDropWeightMultiplier=1`
- same Danger Room scenario OnUse power family as the current Rift launcher
- zero incoming stock prototype references
- visually distinct purple theme

It is not wired into Mythic Rift code or vendor stock yet. The current `PortalToRandomMaxAffixDungeon` launcher remains unchanged pending approval of the second item and the Endless mode contract.

## Native UI Finding

The March to Axis HUD problem was broader than mission objective widgets. Raid and static-scenario metagames write directly to the region `UIDataProvider`, so mission-only suppression could not prevent their top-of-screen UI from returning.

The Rift-owned UI controller keeps only these three widget/context pairs:

- Rift level
- Rift kill quota
- Rift timer

All other region widgets are purged from inside the `MythicRifts` module every 500ms during an active run. The stock `UIDataProvider` is not modified. Mission packet suppression still prevents controlled mission trackers from rebuilding, while the periodic purge handles native metagame widgets such as raid HUD elements.

## Added Region Tiers

Normal expanded maps:

- MODOK / A.I.M. Facility map, with MODOK still excluded as a boss source
- HYDRA Island One-Shot
- Civil War Airport - Captain America
- Civil War Airport - Iron Man
- Civil War Bazaar

The Civil War maps declare `playerLimit=1` in SIP. Rift selection therefore exposes them only to solo runs.

Low-frequency experimental maps:

- Doctor Strange Times Square / Dimensions Collide
- March to Axis
- Muspelheim Raid
- Cosmic Doop Sector

The special branch remains a combined `5%` map-selection chance. Axis and Muspelheim require Rift level 15 or higher. All three newly enabled scripted maps use Rift custom population so native encounter density cannot stall the kill quota.

## Why Experimental Maps Can Still Fail

- March to Axis and Muspelheim use raid region behavior and native raid metagames. Their HUD conflict is addressed, but their metagames may still alter doors, phases, encounter entities, or shutdown behavior.
- Doctor Strange Times Square previously produced `region has not finished downloading`. That points to client streaming or transition timing rather than native HUD ownership.
- Static scenarios can contain native scripted entities even when their UI is hidden. Live tests must confirm the native script cannot end or redirect the instance.

## Boss Pool Expansion

Boss selection is no longer required to come from a map entry. The first boss-only batch is:

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

Each uses a known terminal reward table as a safe default source. `CosmicRiftRewards.json` can override or replace those sources by boss content id, so the default table is not a permanent loot-design decision.

MODOK remains excluded as a boss source because movement/attack failures were reported. SIP presence alone does not prove that a boss AI can operate correctly when spawned outside its native encounter.

## Test Pass

```text
rift validaterandompool 20 1 10
rift validaterandompool 20 2 10
rift previewrandom 20 20 1 10
rift previewrandom 20 20 2 10

rift armbeaconfixed civil-war-airport-cap 10
rift armbeaconfixed civil-war-airport-iron-man 10
rift armbeaconfixed civil-war-bazaar 10
rift armbeaconfixed dr-strange-times-square 10
rift armbeaconfixed march-to-axis 10
rift armbeaconfixed muspelheim-raid 10
```

For Axis and Muspelheim, verify:

- only the Mythic Rift level/quota/timer HUD is visible
- native raid objective UI does not return after zoning or phase changes
- kill quota progresses from native and custom Rift enemies
- the spawned Rift boss appears near the player group
- native scripts do not shut down or redirect the region
- the Rift return portal remains usable after completion
