# Mythic Rift Implementation Status

## Document Goal

- Keep a concrete record of what is already implemented in the server prototype.
- Make local testing easier.
- Later help prepare a readable patch package for TAHITI.

## Current State

- The Mythic Rift server prototype exists in the MHServerEmu repo.
- The system remains localized and adds few coupling points with the rest of the server.
- The current logic still includes deep admin/debug tooling, but the primary local flow is now player-like: buy the beacon from a Danger Room hub vendor, use it, and enter a random Cosmic Rift.
- The server build is stable again as long as bin/obj outputs are redirected to a writable folder outside the repo.
- A dedicated server-side entry layer now exists to prepare a future player-facing entry point without assuming a specific clickable object yet.
- Logical entry points can now be registered server-side even though no concrete in-game launcher has been chosen yet.
- Two TAHITI-friendly consumable launcher families are active: `PortalToRandomMaxAffixDungeon` selects Standard mode, while `PortalToDangerRoomRandomThemeNoAffixesPurple` selects Endless mode. Stock `PortalToRandomDungeon` behavior stays outside the Rift path.
- Danger Room vendors present those families as separate `Mythic Rift Scenario` and `Endless Rift Scenario` items. The item selects the mode directly; there is no global 30-wave config toggle.
- Random Rift runs now decouple the selected map from the selected boss source, so the current prototype can produce a random dungeon or curated non-terminal map with a different random terminal boss.
- Terminal Rift entries now prefer the `AltRegions/*RegionL60` variants instead of the older base terminal region refs, matching MonEll's local finding that native start targets can otherwise resolve into `RegionBand` variants.
- Successful Rift clears now spawn a return portal back to the Danger Room hub; cleanup is requested after the completed Rift region becomes empty.
- Successfully cleared Rift regions are valid launcher locations, so players can carry extra launcher items and chain into the next run without returning to the vendor after every clear.

## Main Files

- `src/MHServerEmu.Games/MythicRifts/MythicRiftManager.cs`
- `src/MHServerEmu.Games/MythicRifts/MythicRiftEntryService.cs`
- `src/MHServerEmu.Games/MythicRifts/MythicRiftEntryRequest.cs`
- `src/MHServerEmu.Games/MythicRifts/MythicRiftEntryResult.cs`
- `src/MHServerEmu.Games/MythicRifts/MythicRiftRunState.cs`
- `src/MHServerEmu.Games/MythicRifts/MythicRiftRunConfig.cs`
- `src/MHServerEmu.Games/MythicRifts/MythicRiftScaling.cs`
- `src/MHServerEmu.Games/MythicRifts/MythicRiftRewardOutcome.cs`
- `src/MHServerEmu/Commands/Implementations/MythicRiftCommands.cs`

## Current Random-Eligible Terminal Map Pool

- Shocker
- Doctor Octopus
- Taskmaster
- Hood
- Mister Sinister
- MODOK
  - AIM Facility is random-eligible and uses a replacement Rift boss; MODOK is excluded from boss-source selection after player feedback reported flaky movement / attack behavior
- Mandarin
- Kingpin

## Current Random-Eligible Non-Terminal Map Pool

These entries are map-only. They can be selected as Rift maps, but they do not provide bosses or loot tables; the boss still comes from the validated terminal boss pool.

- Bronx Zoo
- Wakanda Jungle
- HYDRA Island One-Shot
- Daily Bugle Operation
  - uses Rift custom population to compensate for low native enemy density
- Civil War Airport - Captain America
- Civil War Airport - Iron Man
- Civil War Bazaar
  - the Civil War scenarios are solo-only because their SIP region prototypes declare `playerLimit=1`

## Current Special Low-Chance Map Pool

These entries can be selected randomly only through the special branch, currently capped at a combined 5% chance before normal map selection.

- Cosmic Doop Sector
  - region: `CosmicDoopSectorSpaceRegion`
  - population: `EGDoopZonePop`
  - boss: `CosmicDoopOverlord`
  - loot: `CosmicDoopOverlordTable`
  - kill quota: `100`
  - random level gate: level `25+`
  - direct test id: `cosmic-doop-sector`
- Doctor Strange Times Square / Dimensions Collide
  - uses custom Rift population and a random Rift boss
  - retains a known multiplayer region-streaming risk
- March to Axis
  - level `15+`, custom Rift population, random Rift boss
  - native raid HUD is suppressed; native raid scripting still needs live validation
- Muspelheim Raid
  - level `15+`, custom Rift population, random Rift boss
  - native raid scripting still needs live validation

## Registered But Random-Excluded Content

- Magneto
  - registered on the L60 terminal region for fixed validation, but still excluded from random selection until the bunker transition / door flow is validated safely
- Ultron
  - registered on the L60 terminal region for fixed validation after MonEll's local branch showed this path progressing better, but still excluded from random selection until multiplayer and repeated-run tests confirm it is safe

## What The Prototype Already Does

- build an in-memory Rift run
- process Rift requests through a headless server-side entry layer
- accept a server-side run request for a player or group
- choose random or fixed content from the V1 pool
- choose random or fixed content from an expanded curated terminal pool while keeping more complex terminals out until validated
- use L60 terminal region variants for current terminal content, avoiding native `RegionBand` drift from terminal start targets
- distinguish between the registered terminal catalog and the subset currently eligible for random selection
- choose a random map source and a random boss source independently for random Rift runs
- register boss-only sources independently from maps, beginning with Pyro, A.I.M. Doctor Octopus, Wizard, Bullseye, Elektra, Black Cat, Blob, Green Goblin, Rhino, and Venom
- distinguish random map eligibility from random boss eligibility, allowing curated non-terminal maps without accidentally using them as boss sources
- enforce per-map player limits during fixed and random run creation so solo-only regions cannot be selected for party runs
- support special low-chance random maps, currently used by `Cosmic Doop Sector` at 5%, with fixed own boss selection
- avoid selecting the same boss-source entry as the chosen map when the random pool offers alternatives
- avoid immediately repeating the last completed Rift terminal map for the requester or party when another random map is available
- avoid recent map repeats for the requester or party by keeping a short server-side recent-map history, reducing streaks like repeated Fisk Tower / Hydra Island while still falling back gracefully when the pool is exhausted
- avoid re-rolling the terminal content that the requester or party is currently standing in when chaining the next random Rift from a still-open terminal region
- use a default kill quota specific to the selected terminal content
- calculate D3-like Rift level scaling
- compress Mythic Rift levels onto a softer D3-equivalent curve for Marvel Heroes terminal balance, preserving early-level pacing while reducing mid/high-level spikes
- normalize D3 Greater Rift group health buckets to solo, so group health scaling becomes `1x / 2x / 3x / 4x` for `1 / 2 / 3 / 4-5` players
- track and mirror the highest unlocked Rift level per player during the session
- persist the highest unlocked Rift level inside the Player's persistent data
- verify whether a player can access a given Rift level
- let a player arm a one-shot lower unlocked launch level through `rift level [level]`, while `rift level max` clears that one-shot selection
- let admins reset an individual tester's Rift progression, clear any one-shot launch level, and disarm scoped beacon overrides through `rift resetprogress`
- manage a server-side timer
- fail a run automatically on expiration
- abort a run automatically if all tracked participants stay offline too long
- remove completed or abandoned runs automatically after retention or after the completed Rift region becomes empty
- mark a tracked online participant as an early exit if they leave the bound Rift region before completion, removing that player from rewards/unlocks while allowing remaining players inside to continue
- abort an active Rift after all eligible participants have left the Rift region before completion
- provide a player-facing `rift recover` escape hatch that clears temporary launch state and safely abandons/removes the player's active run if a test session gets stuck
- auto-bind a pending run to the real target terminal region when a participant enters it
- auto-start the timer once that region binding is established
- bind a run to an existing region
- count real kills in the bound region
- unlock the boss when the kill quota is reached
- spawn the selected Rift boss server-side when the kill quota is reached
- keep the random boss unrevealed in normal player-facing start messaging until the quota is completed
- recognize the death of the expected boss
- mark the run successful when the expected boss dies after the quota
- spawn a Rift return portal after successful boss completion so players have a clear in-world exit back to the Danger Room hub
- block native story/campaign transitions inside active or cleared boss-only checkpoint Rift rooms, while still allowing the official Cosmic Rift return portal
- apply the Rift difficulty snapshot to the bound region by reducing player-to-mob damage and increasing mob-to-player damage for the duration of the run
- restore the region damage state automatically when the run ends or is removed
- prevent pre-quota kills of prototype-matching enemies from hijacking boss tracking before the Rift boss phase is unlocked
- suppress terminal-native objective HUD widgets during active Rift runs so native terminal boss objectives do not mislead players after the boss pool is randomized
- temporarily suspend the native terminal mission during Rift runs, scoped to the active Rift instance and restored when the run is removed, so native terminal objectives do not compete with the Rift objective
- temporarily suspend active region-event missions during Rift runs, scoped only to the Rift region instance and restored when the run is removed, so the native "Region Events" tracker does not compete with the Rift objective
- keep region HUD cleanup inside the Mythic Rift module: a Rift-owned controller removes every non-Rift widget on a 500ms refresh while the run is active
- suppress native metagame widgets in addition to mission widgets, covering scripted scenarios and raids such as March to Axis
- leave the stock `UIDataProvider` implementation unchanged
- support an optional config-driven 30-wave cycle that resets after wave 30, uses the supplied per-boss health table, and spawns multiple bosses at milestone waves
- build every multi-boss wave from distinct random boss entries and distinct boss prototypes, so wave 30 does not spawn six copies of one boss
- add one-time kill-progress encounters at 25% (Champion invasion), 50% (random mini-boss), and 75% (Elite strike team)
- grant every final-wave boss's own controlled loot table after success, with native death loot still suppressed
- support JSON-configured guaranteed items filtered by level, wave, checkpoint, map, and any boss in the final wave
- ship guaranteed ground cube-shard crates on every successful floor, with additional crates on waves 20-30 and checkpoint clears
- intercept native `Mission` / `MissionObjective` update packets for controlled terminal objectives while a Rift is active, preventing terminal bounty counters from rebuilding on the client after suppression
- reuse any remaining native generic fraction tracker widget as a best-effort no-client-patch kill counter by forcing it to the active Rift kill quota
- add server-driven Danger Room UI widgets for the Rift kill quota, timer, and selected Rift level by reusing client-known widget prototypes
- resolve those client-known widget prototypes by prototype path first, falling back to known data refs only if path resolution fails, so the UI layer is more resilient across local and Test Center builds
- show a localized center-screen entry banner once per player per run when the player actually enters the active Rift region
- show a localized short top-left completion banner, `COSMIC RIFT CLEARED`, to online run players when the Rift boss dies and the run is marked successful
- rotate completion banner locale string ids across repeated runs to reduce client-side duplicate/stale banner issues in same-map test loops
- load generated Mythic Rift localized strings through the achievement string dump so banners/widgets can display Rift levels without a client `.sip` patch
- local validation on 2026-05-09 confirmed the no-client-patch UI path renders in-game as a Danger Room-style top HUD frame with `Level N`, a kill progress bar, and a countdown timer
- apply a WoW Mythic+-style 15-second timer penalty when a player avatar dies inside an active Rift region
- weight elite kill count progress so harder enemies contribute more: champions = 3, elites = 5, mini-bosses = 8, normal enemies = 1
- enable Rift-only native population respawns with a 20-second delay for the private Rift region; this is a conservative first step toward denser Rift gameplay without injecting custom mobs or changing normal terminals
- optimize the active Rift loop by using HUD-only refreshes for kill/death events, moving full native objective suppression to a slower periodic pass, and caching resolved Rift UI widget prototype refs
- add `rift perf` admin diagnostics for active Rift region load: total entities, agent counts, hostile/simulated agents, players, participants, early exits, respawn-enabled areas, kill progress, and timer remaining
- avoid relying on native terminal objective tracker text for Rift UX; chat messages and `rift status` remain the authoritative no-client-patch fallback
- prepare an end reward based on success or failure
- distribute boss loot to a single player
- distribute boss loot to all eligible tracked participants of the run
- automatically distribute end-of-run rewards to eligible tracked participants when the run completes
- automatically unlock the next Rift level on success
- resolve a future Rift launcher item into a validated run request through a dedicated launcher service
- prepare a testable `item prototype -> launch plan -> run request` flow without wiring the global item `OnUse` path yet
- register a pending launcher intent when a recognized launcher item is used
- consume that intent later to convert it cleanly into a Rift run
- auto-consume that intent at the player's selected launch level, defaulting to their highest currently unlocked Rift level
- apply a default 10-minute launcher timer when no other time limit is provided
- distribute the `Cosmic Rift Beacon` / `Mythic Rift Scenario` directly from the server without relying on a custom client-side vendor
- inject the preferred beacon into the Danger Room rewards vendor stock by default, so testers no longer need an admin grant for the basic flow
- present the injected vendor item as `DangerRoomScenarioCrateUniqueCableFight`, localized as `Mythic Rift Scenario`, while keeping `PortalToRandomMaxAffixDungeon` as the technical base/fallback
- provide vendor-open and purchase chat hints as a fallback when a test server/client still shows stock-looking text
- track the specific granted `Cosmic Rift Beacon` item instances server-side
- register vendor-bought beacon items when purchased
- let tracked beacon instances launch a Rift directly on use, without needing a prior intent-consume step
- consume the committed launcher item after the Rift launch succeeds
- allow tracked beacon launches to fall back to player-level tracked charges when inventory stacking or item instance ids differ on the live server
- allow the preferred unused beacon base, `PortalToRandomMaxAffixDungeon`, and the `DangerRoomScenarioCrateUniqueCableFight` presentation shell to launch directly even when no in-memory tracked charge exists, so patcher-added vendor stock and live-server item cloning do not accidentally fall back into the native Danger Room tutorial/scenario path
- intercept client power activations for the chosen beacon's `OnUsePower` as a fallback when the client does not send a reliable item source id
- emit `[MythicRiftLauncher]` server logs for both successful beacon interception and failed chosen-beacon power interception, making live Test Center debugging easier
- suppress the native scenario continuation whenever Mythic Rift has explicitly intercepted a compatible beacon item use
- consume the actual launcher stack only after the Mythic Rift launch has been committed cleanly, so a failed interception no longer leaks back into the native scenario flow
- keep tracked beacon charges and scoped beacon overrides intact if the Rift launch fails before the teleport step is committed
- make `rift itemintent` explicitly point admins to `rift beaconmode` when the direct beacon path has already intercepted the item use and no legacy intent is pending
- support a scoped per-player beacon override so the next valid chosen beacon use can create a Rift directly
- support a scoped per-player fixed-content beacon override so a specific V1 terminal can be validated without random selection
- keep normal stock `PortalToRandomDungeon` / Danger Room behavior intact by not accepting it as a Rift launcher
- attempt to teleport the player to the selected Rift region start target immediately after a successful armed beacon launch
- force the teleport to use the configured Rift region prototype together with the start-target area/cell/entity data, so native terminal `RegionBand` variants do not silently replace the intended Rift region
- build the launch roster from online party members standing in the leader's current region
- admit party members only after their Rift teleport succeeds, then freeze health scaling from the admitted count
- exclude failed/offline/out-of-region party members from the run roster and reward eligibility
- abort a newly created run immediately if the direct beacon launch cannot resolve or reach a valid Rift start target
- auto-bind pending runs against equivalent terminal region variants, not just exact prototype matches
- award next-level progression competitively: a player must be inside the Rift when the kill quota unlocks the boss and still be inside the Rift when that boss dies
- advance each eligible player's personal progression by at most one level, preventing a high-level friend from replacing the lower player's personal maximum
- expose competitive progression snapshots in `rift run`, so admins can inspect how many players qualified at boss unlock and at boss death
- emit custom in-game system messages when a Rift starts, when the quota unlocks the final boss, and when the run succeeds, fails, or aborts
- use player-facing chat wording for the live loop instead of admin/debug wording
- send a best-effort client timer through the existing timer packet when the Rift starts and stop it when the Rift ends
- send guaranteed chat time warnings every minute from 9 min to 1 min, plus 30 sec remaining
- send guaranteed chat kill progress at 25%, 50%, and 75% enemy quota progress
- expose a user-level `rift status` command so a player can inspect their own active Rift without admin-only run lists
- expose a user-level `rift abandon` command so a participant can intentionally leave, cancel the active Rift, return online participants to the Danger Room hub, and start fresh without relogging
- expose an admin `rift objectives` diagnostic command to inspect the active region/player mission tracker state and UI widgets when native objective suppression needs debugging
- make `rift objectives` force-refresh the Rift UI widgets before dumping the provider and emit `riftUi.*` diagnostics for widget resolution, context, and post-refresh provider state
- treat natural return-to-town / hub teleports out of the active Rift region as a failed/abandoned Rift attempt, so players cannot park or re-enter a stale Rift after leaving
- fail timed-out Rifts and return online participants who are still inside the Rift to the Danger Room hub automatically
- request shutdown of completed/aborted Rift regions when they become vacant, so later runs do not inherit stale terminal instance state
- only treat a player as an early exit after that player has actually been seen inside the active Rift region, preventing immediate party-run aborts while members are still zoning in
- let remaining players keep progressing after another player leaves early, including after party leadership changes
- retry the configured random boss spawn on later eligible kills if the first spawn attempt fails exactly on quota unlock
- keep the current group launch rule leader-driven: the active party leader starts the run, and intended participants should be in the Danger Room hub before beacon use
- log party id, leader db id, and requester db id for Rift requests and non-leader rejections, so leader-swap issues can be diagnosed from Test Center logs

## Current Reward Logic

- Reward tuning can now be adjusted server-side through:
  - `Data/Game/MythicRift/CosmicRiftRewards.json`
  - `rift rewardconfig`
  - `rift rewardconfig reload`
- The default file intentionally matches the previous hard-coded behavior, so the feature does not change unless admins edit the JSON.
- timed success:
  - selected boss loot
  - temporary bonus applied only during the loot roll
  - default bonus values:
    - RIF +10%
    - SIF +15%
- checkpoint timed success:
  - selected boss loot
  - normal timed success bonus
  - extra checkpoint bonus applied only during the loot roll:
    - RIF +5%
    - SIF +10%
- failure:
  - selected boss loot without bonus
- optional extra reward tables can be added in the JSON with:
  - `lootTablePrototype`
  - `chancePercent`
  - `rolls`
  - min/max Rift level gates
  - classic/checkpoint filters
  - content id / boss source filters
- primary boss loot can be replaced in the JSON with `primaryLootTableOverrides`, including min/max Rift level gates, classic/checkpoint filters, and content id / boss source filters
- primary and extra reward tables support `delivery: inventory` or `delivery: ground`; player-owned ground delivery is the default
- loot-table aliases and grouped reward recipes support multiple independent table rolls without requiring one patched mega-table
- reward recipes can be filtered by boss source, map, level range, checkpoint/classic mode, and success/failure state
- end rewards are restricted to admitted participants still present in the Rift at completion
- Rift-spawned bosses always suppress native death loot, and Rift-spawned entities are not stamped with the terminal mission prototype, preventing native mission/event rewards from doubling with the controlled Cosmic Rift reward grant
- the reward flow is no longer purely manual: a completed run can now attempt to auto-distribute rewards to tracked participants

## Current Progression Logic

- each player has a highest unlocked Rift level
- the current value is now stored in the Player's persistent data and mirrored in server memory during the session
- default value:
  - level 1 available
- success at level N:
  - unlocks level N+1

## Current Run Request Logic

- The prototype can now process a run request that is closer to a future real entry flow.
- Current rules:
  - the requested level must be unlocked for the requesting player
  - if the player is in a party, only the leader can request a group run
  - the group size used for the run is derived from the existing party system
  - this helps move away from a purely admin/debug creation flow
  - after the request, if a participant enters the expected terminal region, the run can now auto-bind to that live region and start without a manual bind command
  - run request commands now go through a dedicated server-side entry service, which will make it easier to connect a future capital-hub launcher or patcher-compatible interactable
  - a logical entry point concept already exists with working placeholders such as `default` and `capital-hub`
  - an additional logical entry point now exists for a future `consumable-portal` launcher
  - this working direction now prefers `PortalToRandomMaxAffixDungeon` as the safest launcher item base for isolated Rift behavior
  - that launcher is intentionally random-only, which matches the GRIFT concept well
  - a server-side `launch plan` concept now exists as well, so the future consumable flow can already describe the expected item, private portal, launch model, and patcher compatibility before the final game-file implementation is chosen
  - an initial shortlist of launcher item candidates is now registered server-side so the item research done in extracted game data remains visible inside the project itself
  - current recommendation:
    - `PortalToRandomMaxAffixDungeon` as the officially chosen base
    - no active `PortalToRandomDungeon` fallback, because it is a stock item and should remain isolated from Cosmic Rift behavior
    - `PortalToCowLevelOneTimeUse` as the best technical fallback
    - `PortalToBovineheim` mainly as a behavior reference rather than a final product-facing choice
    - `DevOnly` / `Test` / `Unused` items are real leads in the data, but are currently treated as research candidates, not final production choices
  - important note from MonEll's Calligraphy review:
    - `PortalToRandomMaxAffixDungeon` also appears to be `DevelopmentOnly`
    - `PortalToRandomDungeon` is also marked `DesignState: DevelopmentOnly`, but is no longer part of the active Rift launcher path
    - the codebase-wide approval threshold is currently `Live`, so neither prototype is ideal as a final long-term launcher without TAHITI-side patching or an approved substitute
    - `PortalToRandomMaxAffixDungeon` is now treated as the preferred base because MonEll reports that it is not referenced anywhere else, which lowers the risk of colliding with an existing live gameplay path
  - current implemented seller pass for no-client-patch testing:
    - interacting with a vendor inside the `Danger Room` hub now injects one `PortalToRandomMaxAffixDungeon`-based `Cosmic Rift Beacon` into that player's vendor stock
    - the goal is to remove the admin-only item grant dependency before the final NPC choice is locked with TAHITI
    - this is intentionally region-scoped for now, because it is safer than hard-coding a guessed vendor prototype name before live validation
  - preferred final narrowing after TAHITI confirms the target NPC:
    - `DangerRoomScenarioVendor`
    - reason:
      - it is already a dedicated Danger Room vendor path
      - it is the closest semantic match for "buy a Rift entry consumable"
      - it is safer than reusing a generic weapon / armor / junk vendor
      - it should minimize the risk of leaking the item into unrelated vendors once the exact seller is locked
  - current named-NPC fallback:
    - `DangerRoomVendorWeaponMadisonJeffries`
    - this is attractive for long-term feature identity, but is currently treated as the second choice because it is more likely to share broader vendor behavior than the dedicated scenario vendor path
  - current product identity:
    - `Cosmic Rift`
  - recommended future player-facing item name:
    - `Cosmic Rift Beacon`

## Useful Admin Commands

- Player-safe commands:
  - `rift status`
  - `rift level [level|max]`
  - `rift abandon`
  - `rift recover`
- `rift list`
- `rift entrypoints`
- `rift validatecontent`
- `rift launchplan [entryPointId]`
- `rift launchcandidates`
- `rift beacon`
- `rift beaconmode`
- `rift armbeacon [minutes]`
- `rift armbeaconfixed [contentId] [minutes]`
- `rift disarmbeacon`
- `rift givebeacon [count]`
- `rift prepbeacon [level] [count]`
- `rift requestitem [itemPrototypeName] [level] [minutes]`
- `rift itemintent`
- `rift consumeintent [level] [minutes]`
- `rift consumeintentauto [minutes]`
- `rift scale [level] [players]`

Current practical launcher stage
- The project is now at the stage where a vendor-bought or server-granted `Cosmic Rift Beacon` can be used directly in-game to create a Rift run.
- A first server-side seller pass now exists as well:
  - a player can open a vendor inside the `Danger Room` hub, buy the injected beacon, and test the Rift flow without an admin grant command
  - the item is now present by default in the vendor stock and is tracked at purchase time
  - the launcher item is consumed after a committed Rift launch
  - the final seller can still be narrowed later once TAHITI confirms which vendor should own the feature permanently
- Vendor-bought beacons are now intercepted from top-level item use and client power activation fallback paths, so the chosen `PortalToRandomMaxAffixDungeon` base routes into Mythic Rift even if live-server vendor cloning does not preserve the expected in-memory tracking entry.
- Important constraint:
  - untracked direct behavior is scoped only to the preferred unused `PortalToRandomMaxAffixDungeon` beacon base
  - normal non-beacon `PortalToRandomDungeon` / Danger Room behavior must remain unchanged because it is no longer registered as a launcher fallback
- For random runs, the direct beacon path now creates a random map plus a separately selected random boss source from the current playable pool.
- The active Rift region now suppresses the terminal's native linked boss while the run is active, so the player cannot complete or loot the normal terminal boss before the Cosmic Rift quota is finished.
- Boss completion is now strictly quota-gated: even a matching boss entity cannot complete the run until the kill quota has unlocked the boss phase.
- Player-selected launch level now exists server-side: `rift level` shows the next beacon launch level, `rift level 50` lets an unlocked player arm one level-50 farming launch, and `rift level max` clears that one-shot selection.
- `rift access [level]`
- `rift progression`
- `rift setaccess [level]`
- `rift resetprogress`
- `rift request [level] [killQuota] [minutes]`
- `rift requestauto [level] [minutes]`
- `rift requestportal [level] [minutes]`
- `rift requestfixed [contentId] [level] [killQuota] [minutes]`
- `rift requestfixedauto [contentId] [level] [minutes]`
- `rift create [level] [players] [killQuota] [minutes]`
- `rift createmix [contentId] [bossContentId] [level] [players] [killQuota] [minutes]`
- `rift createfixed [contentId] [level] [players] [killQuota] [minutes]`
- `rift debugmix [contentId] [bossContentId] [level] [players] [killQuota] [minutes]`
- `rift previewrandom [count] [level] [players] [minutes]`
- `rift validaterandompool [level] [players] [minutes]`
- `rift run [runId]`
- `rift runs`
- `rift start [runId]`
- `rift bind [runId]`
- `rift kills [runId] [count]`
- `rift tick [runId]`
- `rift success [runId]`
- `rift fail [runId]`
- `rift abort [runId]`
- `rift rewardconfig [reload]`
- `rift reward [runId]`
- `rift rewardall [runId]`
- `rift remove [runId]`

## Important Technical Decision For TAHITI

- The prototype is still designed as a focused code patch.
- It does not require a full server reinstall.
- It introduces no database migration at this stage.
- It is explicitly framed to avoid dependence on a manual client patch.
- If game files are needed later, they should ideally be deployable through the Patcher.
- The current preferred player-facing direction is now an item-driven portal flow where `PortalToRandomMaxAffixDungeon` remains the technical launcher base, but vendor stock is presented through `DangerRoomScenarioCrateUniqueCableFight` so the client can display `Mythic Rift Scenario` without a custom client patch.
- The server still keeps explicit chat guidance when the Danger Room vendor opens and when the beacon is purchased, because this remains the safest fallback if presentation strings or prototype patches are missing on a test environment.
- Random enemy replacement for normal terminal maps is still intentionally deferred. The current server-side-safe implementation randomizes the terminal map and boss source, keeps native terminal enemy populations for terminal content, and now reserves the compact StoryRevamp / treasure-room maps for boss-only checkpoint levels.
- Every 5th Rift level now acts as a checkpoint tier: random level `5`, `10`, `15`, etc. selects a boss-only checkpoint room, summons a random validated Rift boss immediately, and requires that boss kill to unlock the next tier.
- Checkpoint rooms hide the kill-quota HUD widget and keep only the level/timer widgets, because showing a fake `1/1` quota confused the intended boss-only flow.
- Checkpoint progression eligibility is captured from players present at boss death, rather than from an instant boss-unlock snapshot at room start, so slower-loading group members are not excluded just because the boss spawned before their client finished zoning.
- If a checkpoint boss cannot spawn immediately, the run now stays active and retries the spawn instead of aborting the Rift during region/player anchor timing windows.
- `sabretooth-showdown`, `supervillain-rec-center`, `sc-kill-house`, and `tr-asgard-estate` are back in the automatic checkpoint random pool for validation after adding checkpoint-native hostile suppression and native transition blocking.
- Boss-only checkpoint rooms suppress native hostile population before the Rift boss spawns and do not enable native population respawns, so they behave more like controlled Rift boss arenas instead of story rooms.
- Daily Bugle Operation is back in the random map pool with Rift custom population enabled, so the low native population should no longer block kill-quota progression.
- Player-selected launch level is now explicitly separated from progression: `rift level [number]` only changes the next successful beacon launch and cannot lower `highestUnlockedRiftLevel`; after that launch, later beacons default back to highest unlocked level.
- Test helper commands that unlock access now protect existing higher progress; `rift resetprogress` is the explicit way to wipe a tester back to level 1.
- Checkpoint boss spawning now prefers positions in the player's current cell/room to reduce small-room cases where a boss appeared outside the playable map.
- Rift launcher use is now gated to the Danger Room hub or a successfully cleared Cosmic Rift. If a player tries to use the item in Story Mode, inside an active Rift, or another unsafe region, the server intercepts the item use and blocks the native scenario/story teleport fallback.
- Death release inside an active Rift is now handled by the Rift manager, keeping the player in the same Rift instance start target instead of letting StoryRevamp maps respawn into their native story-mode version.
- A basic no-client-patch player-facing level selector now exists through chat commands. A cleaner item/NPC UI for showing progression and selecting levels remains future UX polish because dynamic per-player item tooltip changes are not realistic without client-side UI/data support.

## Build / SDK Note

- The .NET SDK is available and working on this machine.
- The issue we hit was not a completely broken SDK, but mostly a write-access problem on the repo bin/obj folders.
- For future local builds, prefer a command like:

```powershell
dotnet build MHServerEmu.csproj -c Release -p:GenerateAssemblyInfo=false -p:GenerateTargetFrameworkAttribute=false -p:BaseIntermediateOutputPath=C:\Users\admin\Documents\Codex\build\iso-obj\ -p:MSBuildProjectExtensionsPath=C:\Users\admin\Documents\Codex\build\iso-obj\ -p:BaseOutputPath=C:\Users\admin\Documents\Codex\build\iso-bin\
```

- This keeps the repo cleaner and avoids access errors under `Desktop\PROJECT MHO`.
- Setting `MSBuildProjectExtensionsPath` to the same isolated obj root also avoids intermittent MSBuild dependency-resolution issues seen with redirected outputs.
- `GenerateAssemblyInfo=false` and `GenerateTargetFrameworkAttribute=false` are the safe fallback switches if redirected-output builds hit duplicate Gazillion assembly-attribute generation on this machine.
- Verified state:
  - full `MHServerEmu` build OK
  - historical `Gazillion` warnings may still appear
  - 0 error

## V1 Quota Notes

- The V1 content pool now includes a default kill quota per terminal.
- Current working values:
  - Taskmaster: 50
  - Hood: 55
  - Mister Sinister: 60
  - Kingpin: 65
- These are provisional tuning values and should be adjusted after real gameplay tests.
