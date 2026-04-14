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
- MythicRiftRunConfig
- MythicRiftRunManager
- MythicRiftRunState
- MythicRiftContentPool
- MythicRiftDifficultyService
- MythicRiftRewardService
- MythicRiftProgressionService

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
