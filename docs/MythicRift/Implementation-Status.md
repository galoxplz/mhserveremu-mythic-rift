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

## Main Files

- `src/MHServerEmu.Games/MythicRifts/MythicRiftManager.cs`
- `src/MHServerEmu.Games/MythicRifts/MythicRiftRunState.cs`
- `src/MHServerEmu.Games/MythicRifts/MythicRiftRunConfig.cs`
- `src/MHServerEmu.Games/MythicRifts/MythicRiftScaling.cs`
- `src/MHServerEmu.Games/MythicRifts/MythicRiftRewardOutcome.cs`
- `src/MHServerEmu/Commands/Implementations/MythicRiftCommands.cs`

## Registered V1 Content

- Taskmaster
- Hood
- Mister Sinister
- Kingpin

## What The Prototype Already Does

- build an in-memory Rift run
- accept a server-side run request for a player or group
- choose random or fixed content from the V1 pool
- calculate D3-like Rift level scaling
- track the highest unlocked Rift level per player in memory
- verify whether a player can access a given Rift level
- manage a server-side timer
- fail a run automatically on expiration
- bind a run to an existing region
- count real kills in the bound region
- unlock the boss when the kill quota is reached
- recognize the death of the expected boss
- mark the run successful when the expected boss dies after the quota
- prepare an end reward based on success or failure
- distribute boss loot to a single player
- distribute boss loot to all tracked participants of the run
- automatically unlock the next Rift level on success

## Current Reward Logic

- timed success:
  - selected boss loot
  - temporary bonus applied only during the loot roll
  - current bonus values:
    - RIF +10%
    - SIF +15%
- failure:
  - selected boss loot without bonus

## Current Progression Logic

- each player has a highest unlocked Rift level
- the current value is stored in server memory for the prototype
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

## Useful Admin Commands

- `rift list`
- `rift scale [level] [players]`
- `rift access [level]`
- `rift setaccess [level]`
- `rift request [level] [killQuota] [minutes]`
- `rift requestfixed [contentId] [level] [killQuota] [minutes]`
- `rift create [level] [players] [killQuota] [minutes]`
- `rift createfixed [contentId] [level] [players] [killQuota] [minutes]`
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

## Build / SDK Note

- The .NET SDK is available and working on this machine.
- The issue we hit was not a completely broken SDK, but mostly a write-access problem on the repo bin/obj folders.
- For future local builds, prefer a command like:

```powershell
dotnet build MHServerEmu.csproj -c Release -p:BaseIntermediateOutputPath=C:\Users\admin\Documents\Codex\build\obj-cli\ -p:BaseOutputPath=C:\Users\admin\Documents\Codex\build\bin-cli\
```

- This keeps the repo cleaner and avoids access errors under `Desktop\PROJECT MHO`.
- Verified state:
  - full `MHServerEmu` build OK
  - 0 warning
  - 0 error
