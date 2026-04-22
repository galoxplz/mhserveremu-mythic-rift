# Mythic Rift Implementation Status

## Document Goal

- Keep a concrete record of what is already implemented in the server prototype.
- Make local testing easier.
- Later help prepare a readable patch package for TAHITI.

## Current State

- The Mythic Rift server prototype exists in the MHServerEmu repo.
- The system remains localized and adds few coupling points with the rest of the server.
- The current logic is mainly debug/admin-oriented, which is intentional while stabilizing the core before wiring a real player entry point.
- The server build is stable again as long as bin/obj outputs are redirected to a writable folder outside the repo.
- A dedicated server-side entry layer now exists to prepare a future player-facing entry point without assuming a specific clickable object yet.
- Logical entry points can now be registered server-side even though no concrete in-game launcher has been chosen yet.
- A more concrete TAHITI-friendly direction now exists: a consumable portal launcher modeled after `PortalToRandomDungeon`, reusing a private direct-portal flow similar to Bovineheim/Cow Level.
- Random Rift runs now decouple the selected terminal map from the selected boss source, so the current prototype can produce a random dungeon with a different random terminal boss.

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

## Current Playable Terminal Pool

- Shocker
- Doctor Octopus
- Taskmaster
- Hood
- Magneto
- Mister Sinister
- MODOK
- Mandarin
- Kingpin
- Ultron

## What The Prototype Already Does

- build an in-memory Rift run
- process Rift requests through a headless server-side entry layer
- accept a server-side run request for a player or group
- choose random or fixed content from the V1 pool
- choose random or fixed content from an expanded curated terminal pool while keeping more complex terminals out until validated
- distinguish between the registered terminal catalog and the subset currently eligible for random selection
- choose a random map source and a random boss source independently for random Rift runs
- avoid selecting the same boss-source entry as the chosen map when the random pool offers alternatives
- avoid immediately repeating the last completed Rift terminal map for the requester or party when another random map is available
- avoid re-rolling the terminal content that the requester or party is currently standing in when chaining the next random Rift from a still-open terminal region
- use a default kill quota specific to the selected terminal content
- calculate D3-like Rift level scaling
- compress Mythic Rift levels onto a slower D3-equivalent curve for Marvel Heroes terminal balance
- normalize D3 Greater Rift group health buckets to solo, so group health scaling becomes `1x / 2x / 3x / 4x` for `1 / 2 / 3 / 4-5` players
- track and mirror the highest unlocked Rift level per player during the session
- persist the highest unlocked Rift level inside the Player's persistent data
- verify whether a player can access a given Rift level
- manage a server-side timer
- fail a run automatically on expiration
- abort a run automatically if all tracked participants stay offline too long
- remove completed or abandoned runs automatically after retention
- auto-bind a pending run to the real target terminal region when a participant enters it
- auto-start the timer once that region binding is established
- bind a run to an existing region
- count real kills in the bound region
- unlock the boss when the kill quota is reached
- spawn the selected Rift boss server-side when the kill quota is reached
- recognize the death of the expected boss
- mark the run successful when the expected boss dies after the quota
- apply the Rift difficulty snapshot to the bound region by reducing player-to-mob damage and increasing mob-to-player damage for the duration of the run
- restore the region damage state automatically when the run ends or is removed
- prevent pre-quota kills of prototype-matching enemies from hijacking boss tracking before the Rift boss phase is unlocked
- prepare an end reward based on success or failure
- distribute boss loot to a single player
- distribute boss loot to all tracked participants of the run
- automatically distribute end-of-run rewards to tracked participants when the run completes
- automatically unlock the next Rift level on success
- resolve a future Rift launcher item into a validated run request through a dedicated launcher service
- prepare a testable `item prototype -> launch plan -> run request` flow without wiring the global item `OnUse` path yet
- register a pending launcher intent when a recognized launcher item is used
- consume that intent later to convert it cleanly into a Rift run
- auto-consume that intent at the player's highest currently unlocked Rift level
- apply a default 10-minute launcher timer when no other time limit is provided
- distribute the `Cosmic Rift Beacon` directly from the server without relying on a custom client-side vendor
- track the specific granted `Cosmic Rift Beacon` item instances server-side
- let tracked beacon instances launch a Rift directly on use, without needing a prior intent-consume step
- allow tracked beacon launches to fall back to player-level tracked charges when inventory stacking or item instance ids differ on the live server
- suppress the native `PortalToRandomDungeon` / Danger Room `OnUse` continuation whenever Mythic Rift has explicitly intercepted that item use
- consume the actual launcher stack only after the Mythic Rift launch has been committed cleanly, so a failed interception no longer leaks back into the native scenario flow
- keep tracked beacon charges and scoped beacon overrides intact if the Rift launch fails before the teleport step is committed
- make `rift itemintent` explicitly point admins to `rift beaconmode` when the direct beacon path has already intercepted the item use and no legacy intent is pending
- support a scoped per-player beacon override so the next valid `PortalToRandomDungeon` use can create a Rift directly
- support a scoped per-player fixed-content beacon override so a specific V1 terminal can be validated without random selection
- keep normal `PortalToRandomDungeon` / Danger Room behavior intact unless that scoped override is explicitly armed first
- attempt to teleport the player to the selected Rift region start target immediately after a successful armed beacon launch
- abort a newly created run immediately if the direct beacon launch cannot resolve or reach a valid Rift start target
- auto-bind pending runs against equivalent terminal region variants, not just exact prototype matches
- award next-level progression competitively: a player must be inside the Rift when the kill quota unlocks the boss and still be inside the Rift when that boss dies
- expose competitive progression snapshots in `rift run`, so admins can inspect how many players qualified at boss unlock and at boss death
- emit custom in-game system messages when a Rift starts, when the quota unlocks the final boss, and when the run succeeds, fails, or aborts
- retry the configured random boss spawn on later eligible kills if the first spawn attempt fails exactly on quota unlock

## Current Reward Logic

- timed success:
  - selected boss loot
  - temporary bonus applied only during the loot roll
  - current bonus values:
    - RIF +10%
    - SIF +15%
- failure:
  - selected boss loot without bonus
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
  - this working direction currently uses `PortalToRandomDungeon` as the best candidate for a simple-to-obtain launcher item
  - that launcher is intentionally random-only, which matches the GRIFT concept well
  - a server-side `launch plan` concept now exists as well, so the future consumable flow can already describe the expected item, private portal, launch model, and patcher compatibility before the final game-file implementation is chosen
  - an initial shortlist of launcher item candidates is now registered server-side so the item research done in extracted game data remains visible inside the project itself
  - current recommendation:
    - `PortalToRandomDungeon` as the officially chosen base
    - `PortalToRandomMaxAffixDungeon` as the strongest newly identified alternative from TAHITI review
    - `PortalToCowLevelOneTimeUse` as the best technical fallback
    - `PortalToBovineheim` mainly as a behavior reference rather than a final product-facing choice
    - `DevOnly` / `Test` / `Unused` items are real leads in the data, but are currently treated as research candidates, not final production choices
  - important note from MonEll's Calligraphy review:
    - `PortalToRandomDungeon` is marked `DesignState: DevelopmentOnly`
    - `PortalToRandomMaxAffixDungeon` also appears to be `DevelopmentOnly`
    - the codebase-wide approval threshold is currently `Live`, so neither prototype is ideal as a final long-term launcher without TAHITI-side patching or an approved substitute
    - `PortalToRandomMaxAffixDungeon` still looks attractive because MonEll reports that it is not referenced anywhere else, which lowers the risk of colliding with an existing live gameplay path
  - current implemented seller pass for no-client-patch testing:
    - interacting with a vendor inside the `Danger Room` hub now injects one `PortalToRandomDungeon`-based `Cosmic Rift Beacon` into that player's vendor stock
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
- The project is now at the stage where a server-granted `Cosmic Rift Beacon` can be used directly in-game to create a Rift run.
- A first server-side seller pass now exists as well:
  - a player can open a vendor inside the `Danger Room` hub, buy the injected beacon, and test the Rift flow without an admin grant command
  - the final seller can still be narrowed later once TAHITI confirms which vendor should own the feature permanently
- Vendor-bought beacons are now intercepted from top-level item use as well, so `PortalToRandomDungeon` variants that do not reliably fall through the same `UsePower` path still route into Mythic Rift correctly.
- Important constraint:
  - this direct behavior is scoped to tracked beacon instances granted by the server
  - normal non-beacon `PortalToRandomDungeon` / Danger Room behavior must remain unchanged
- For random runs, the direct beacon path now creates a random terminal map plus a separately selected random boss source from the current playable pool.
- `rift access [level]`
- `rift progression`
- `rift setaccess [level]`
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
- `rift reward [runId]`
- `rift rewardall [runId]`
- `rift remove [runId]`

## Important Technical Decision For TAHITI

- The prototype is still designed as a focused code patch.
- It does not require a full server reinstall.
- It introduces no database migration at this stage.
- It is explicitly framed to avoid dependence on a manual client patch.
- If game files are needed later, they should ideally be deployable through the Patcher.
- The current preferred player-facing direction is now an item-driven portal flow based on `PortalToRandomDungeon`, because it looks easier to integrate cleanly than a shop-gated Bovineheim-specific item.

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
