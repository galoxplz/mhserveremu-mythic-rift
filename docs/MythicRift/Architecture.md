# Mythic Rift Server Architecture

## Document Goal

- Define a clean server architecture for Mythic Rift.
- Provide an implementation basis for local work and then a patch proposal for TAHITI.
- Build on confirmed real content from extracted data.

## Context

- The mode targets 1 to 5 player content.
- The server creates an instanced run.
- The run selects a map and a boss from a V1 pool.
- The run is timed.
- Progression is potentially infinite by level.
- End rewards should reuse existing boss loot and world drops as much as possible, with SIF/RIF bonus on top.
- The current preferred TAHITI-facing direction is now a `consumable item -> private portal -> Rift region` flow, using `PortalToRandomDungeon` as the best candidate object model and Bovineheim/Cow Level as the portal-flow reference.

## Architecture Principles

- Isolate Mythic Rift logic into clear components.
- Reuse existing server systems instead of reinventing them.
- Keep configuration points centralized.
- Avoid diffuse and hard-to-maintain changes.
- Make the patch easy for TAHITI to read and tune.
- Prioritize a server-side implementation.
- Avoid any dependency on a manually distributed client patch.
- If game files must change, prefer a Patcher-compatible distribution model.

## Core Components

- MythicRiftEntryService
- MythicRiftLauncherService
- MythicRiftRunConfig
- MythicRiftManager
- MythicRiftRunState
- MythicRiftContentEntry pool
- MythicRiftScaling
- reward resolution inside MythicRiftManager
- progression persistence via Player persistent data

## Key Runtime Responsibilities

- receive a run request
- validate requested level access
- build and register the run
- bind the run to a region / instance
- track participants
- track timer state
- track kill quota
- detect expected boss death
- resolve success or failure
- grant rewards
- unlock the next Rift level

## TAHITI Patch Strategy

- minimize changes to the server core
- prefer new dedicated classes and services
- touch existing entry points carefully
- do not require a manual client patch
- if game files are required, aim for Patcher-compatible delivery

## Entry Point Note

- The entry layer can now support logical launcher definitions such as `default`, `capital-hub`, or a future `consumable-portal` flow without assuming a specific clickable object yet.
- The current preferred launcher candidate is a `PortalToRandomDungeon`-style consumable because it appears easier to integrate cleanly than a shop-gated Bovineheim-specific item.
- The first implemented no-client-patch seller pass is now a server-scoped `Danger Room` hub vendor injection, so testers can buy the launcher without admin grant commands before the final NPC is locked.
- The preferred final seller target after TAHITI confirms the exact NPC is still `DangerRoomScenarioVendor`.
- `DangerRoomVendorWeaponMadisonJeffries` remains the best named-NPC fallback if TAHITI later wants stronger feature identity after the safer vendor-path rollout is validated.
