using System.Globalization;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.MythicRifts;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("rift")]
    [CommandGroupDescription("Debug and inspection commands for the Mythic Rift prototype.")]
    public class MythicRiftCommands : CommandGroup
    {
        [Command("list")]
        [CommandDescription("Lists the currently registered Mythic Rift content pool.")]
        [CommandUsage("rift list")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string List(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            IReadOnlyList<MythicRiftContentEntry> contentPool = game.MythicRiftManager.ContentPool;
            if (contentPool.Count == 0)
                return "No Mythic Rift content is registered.";

            List<string> lines = new(contentPool.Count + 1)
            {
                $"Mythic Rift content pool: {contentPool.Count} entries"
            };

            foreach (MythicRiftContentEntry content in contentPool.OrderBy(entry => entry.DisplayName))
                lines.Add($"{content.Id}: {content.DisplayName} | randomEligible={content.RandomEligible} | defaultKillQuota={content.DefaultKillQuota} | region={content.RegionProtoRef.GetNameFormatted()} | entryTarget={content.StartTargetProtoRef.GetNameFormatted()} | boss={content.BossProtoRef.GetNameFormatted()}");

            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("entrypoints")]
        [CommandDescription("Lists the currently registered Mythic Rift logical entry points.")]
        [CommandUsage("rift entrypoints")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string EntryPoints(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            IReadOnlyCollection<MythicRiftEntryPointDefinition> entryPoints = game.MythicRiftEntryService.EntryPoints;
            if (entryPoints.Count == 0)
                return "No Mythic Rift entry points are registered.";

            List<string> lines = new(entryPoints.Count + 1)
            {
                $"Mythic Rift entry points: {entryPoints.Count}"
            };

            foreach (MythicRiftEntryPointDefinition entryPoint in entryPoints.OrderBy(entry => entry.DisplayName))
            {
                lines.Add(
                    $"{entryPoint.Id}: {entryPoint.DisplayName} | launchModel={entryPoint.LaunchModel} | patcherFriendly={entryPoint.IsPatcherFriendly} | random={entryPoint.AllowsRandomContent} | fixed={entryPoint.AllowsFixedContentSelection} | candidateItem={entryPoint.CandidateItemPrototypeName ?? "n/a"} | candidatePortal={entryPoint.CandidateTransitionPrototypeName ?? "n/a"} | notes={entryPoint.Notes}");
            }

            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("validatecontent")]
        [CommandDescription("Validates that registered Mythic Rift content entries resolve a usable region and entry target.")]
        [CommandUsage("rift validatecontent")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string ValidateContent(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            IReadOnlyList<MythicRiftContentEntry> contentPool = game.MythicRiftManager.ContentPool;
            if (contentPool.Count == 0)
                return "No Mythic Rift content is registered.";

            List<string> lines = new(contentPool.Count + 1)
            {
                $"Mythic Rift content validation: {contentPool.Count} entries"
            };

            foreach (MythicRiftContentEntry content in contentPool.OrderBy(entry => entry.DisplayName))
            {
                RegionPrototype regionProto = content.RegionProtoRef.As<RegionPrototype>();
                RegionConnectionTargetPrototype startTargetProto = content.StartTargetProtoRef.As<RegionConnectionTargetPrototype>();

                bool regionValid = regionProto != null;
                bool startTargetValid = startTargetProto != null;
                bool targetMatchesRegion = regionValid && startTargetValid &&
                    RegionPrototype.Equivalent(startTargetProto.Region.As<RegionPrototype>(), regionProto);

                lines.Add(
                    $"{content.Id}: randomEligible={content.RandomEligible} | regionValid={regionValid} | startTargetValid={startTargetValid} | targetMatchesRegion={targetMatchesRegion} | region={content.RegionProtoRef.GetNameFormatted()} | entryTarget={content.StartTargetProtoRef.GetNameFormatted()}");
            }

            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("launchcandidates")]
        [CommandDescription("Lists current Mythic Rift launcher item candidates discovered from game data research.")]
        [CommandUsage("rift launchcandidates")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string LaunchCandidates(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            IReadOnlyList<MythicRiftLauncherItemCandidate> candidates = game.MythicRiftEntryService.LauncherItemCandidates;
            if (candidates.Count == 0)
                return "No Mythic Rift launcher item candidates are registered.";

            List<string> lines = new(candidates.Count + 1)
            {
                $"Mythic Rift launcher candidates: {candidates.Count}"
            };

            foreach (MythicRiftLauncherItemCandidate candidate in candidates.OrderByDescending(c => c.Recommendation == "chosen").ThenByDescending(c => c.Recommendation == "primary").ThenBy(c => c.PrototypeName))
            {
                lines.Add(
                    $"{candidate.PrototypeName}: source={candidate.SourceFamily} | recommendation={candidate.Recommendation} | patcherFriendly={candidate.PatcherFriendly} | shopLinked={candidate.IsShopLinked} | randomFit={candidate.SupportsRandomThemeIdentity} | lowRisk={candidate.IsLikelyUnusedOrLowRisk} | notes={candidate.Notes}");
            }

            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("beacon")]
        [CommandDescription("Displays the current player-facing Cosmic Rift Beacon identity and its server-side technical base.")]
        [CommandUsage("rift beacon")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Beacon(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            MythicRiftLauncherItemCandidate candidate = game.MythicRiftLauncherService.ResolveChosenCandidate();
            if (candidate == null)
                return "Chosen Cosmic Rift Beacon candidate not found.";

            List<string> lines = new()
            {
                $"playerFacingItem={MythicRiftLauncherService.CosmicRiftBeaconDisplayName} | technicalBase={MythicRiftLauncherService.CosmicRiftBeaconPrototypeName}",
                $"sourceFamily={candidate.SourceFamily} | patcherFriendly={candidate.PatcherFriendly} | shopLinked={candidate.IsShopLinked} | recommendation={candidate.Recommendation}",
                $"notes={candidate.Notes}",
                "distributionPlan=server-side item grant first, then optional loot/reward/vendor policy later if TAHITI wants it"
            };

            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("launchplan")]
        [CommandDescription("Displays the current launch plan for a Mythic Rift entry point.")]
        [CommandUsage("rift launchplan [entryPointId]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string LaunchPlan(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            MythicRiftPortalLaunchPlan launchPlan = game.MythicRiftEntryService.BuildLaunchPlan(@params[0]);
            if (launchPlan == null)
                return $"Unknown Mythic Rift entry point: {@params[0]}";

            CommandHelper.SendMessages(client, BuildLaunchPlanLines(launchPlan));
            return string.Empty;
        }

        [Command("itemintent")]
        [CommandDescription("Displays the current pending launcher intent for the invoking player, if any.")]
        [CommandUsage("rift itemintent")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string ItemIntent(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            MythicRiftLauncherIntent intent = game.MythicRiftLauncherService.GetPendingIntent(player.DatabaseUniqueId);
            if (intent == null)
            {
                MythicRiftLauncherUseResult lastResult = game.MythicRiftLauncherService.GetLastArmedLaunchResult(player.DatabaseUniqueId);
                int trackedBeaconCharges = game.MythicRiftLauncherService.GetTotalTrackedBeaconCharges(player.DatabaseUniqueId);
                if (lastResult != null || trackedBeaconCharges > 0)
                    return "No pending Mythic Rift launcher intent for this player. If you are testing direct beacon use, inspect `rift beaconmode` instead.";

                return "No pending Mythic Rift launcher intent for this player.";
            }

            int unlockedLevel = game.MythicRiftManager.GetHighestUnlockedRiftLevel(player.DatabaseUniqueId);
            CommandHelper.SendMessages(client, BuildLauncherIntentLines(intent, unlockedLevel));
            return string.Empty;
        }

        [Command("scale")]
        [CommandDescription("Displays the D3-inspired Mythic Rift scaling snapshot for a level and party size.")]
        [CommandUsage("rift scale [level] [players]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(2)]
        public string Scale(string[] @params, NetClient client)
        {
            if (TryParsePositiveInt(@params[0], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[1], out int requestedPlayers) == false)
                return "Invalid player count.";

            MythicRiftDifficultySnapshot snapshot = MythicRiftScaling.BuildSnapshot(riftLevel, requestedPlayers);

            return string.Create(CultureInfo.InvariantCulture,
                $"Rift level {snapshot.RiftLevel} | requested players={requestedPlayers} | effective players={snapshot.EffectivePlayerCount} | d3EquivalentLevel={snapshot.EquivalentD3RiftLevel:F2} | groupHealth x{snapshot.GroupHealthMultiplier:F3} | HP x{snapshot.HealthMultiplier:F3} | damage x{snapshot.DamageMultiplier:F3}");
        }

        [Command("access")]
        [CommandDescription("Displays the highest unlocked Rift level for the invoking player and whether a target level is accessible.")]
        [CommandUsage("rift access [level]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Access(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            if (TryParsePositiveInt(@params[0], out int riftLevel) == false)
                return "Invalid rift level.";

            int unlockedLevel = game.MythicRiftManager.GetHighestUnlockedRiftLevel(player.DatabaseUniqueId);
            bool canAccess = game.MythicRiftManager.CanAccessRiftLevel(player.DatabaseUniqueId, riftLevel);
            return $"Player unlocked level={unlockedLevel} | requested level={riftLevel} | accessible={canAccess}";
        }

        [Command("progression")]
        [CommandDescription("Displays the invoking player's current Mythic Rift progression state and any in-progress run.")]
        [CommandUsage("rift progression")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Progression(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            int unlockedLevel = game.MythicRiftManager.GetHighestUnlockedRiftLevel(player.DatabaseUniqueId);
            MythicRiftRunState inProgressRun = game.MythicRiftManager.GetInProgressRunForPlayer(player.DatabaseUniqueId);

            List<string> lines = new()
            {
                $"playerDbId=0x{player.DatabaseUniqueId:X} | highestUnlockedRiftLevel={unlockedLevel} | persistedPlayerValue={player.MythicRiftHighestUnlockedLevel}"
            };

            if (inProgressRun == null)
            {
                lines.Add("inProgressRun=none");
            }
            else
            {
                lines.Add($"inProgressRun={inProgressRun.Config.RunId} | status={inProgressRun.Status} | content={inProgressRun.Config.Content.Id} | level={inProgressRun.Config.RiftLevel}");
            }

            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("setaccess")]
        [CommandDescription("Sets the highest unlocked Rift level for the invoking player.")]
        [CommandUsage("rift setaccess [level]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string SetAccess(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            if (TryParsePositiveInt(@params[0], out int unlockedLevel) == false)
                return "Invalid level.";

            int appliedLevel = game.MythicRiftManager.SetHighestUnlockedRiftLevel(player.DatabaseUniqueId, unlockedLevel);
            return $"Player highest unlocked Rift level set to {appliedLevel}.";
        }

        [Command("debug")]
        [CommandDescription("Builds a debug run config for an existing Mythic Rift entry without starting a live run.")]
        [CommandUsage("rift debug [contentId] [level] [players] [killQuota] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(5)]
        public string Debug(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            string contentId = @params[0];
            if (TryParsePositiveInt(@params[1], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[2], out int requestedPlayers) == false)
                return "Invalid player count.";

            if (TryParsePositiveInt(@params[3], out int killQuota) == false)
                return "Invalid kill quota.";

            if (TryParsePositiveInt(@params[4], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftRunConfig config = game.MythicRiftManager.CreateDebugRunConfig(
                contentId,
                riftLevel,
                requestedPlayers,
                killQuota,
                TimeSpan.FromMinutes(timeLimitMinutes));

            if (config == null)
                return $"Unknown Mythic Rift content id: {contentId}";

            List<string> lines = new()
            {
                $"Mythic Rift debug config for {config.Content.DisplayName}",
                $"runId={config.RunId} | level={config.RiftLevel} | requestedPlayers={config.RequestedPlayerCount} | effectivePlayers={config.EffectivePlayerCount}",
                $"killQuota={config.KillQuota} | timeLimit={config.TimeLimit.TotalMinutes:0} min | HP x{config.Difficulty.HealthMultiplier:F3} | damage x{config.Difficulty.DamageMultiplier:F3}",
                $"region={config.RegionProtoRef.GetNameFormatted()}",
                $"mission={config.MissionProtoRef.GetNameFormatted()}",
                $"bossSource={config.BossContent?.Id ?? "n/a"} | boss={config.BossProtoRef.GetNameFormatted()}",
                $"bossLoot={config.BossLootTableProtoRef.GetNameFormatted()}"
            };

            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("previewrandom")]
        [CommandDescription("Previews random Mythic Rift map/boss combinations without creating live runs.")]
        [CommandUsage("rift previewrandom [count] [level] [players] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(4)]
        public string PreviewRandom(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            if (TryParsePositiveInt(@params[0], out int previewCount) == false)
                return "Invalid preview count.";

            if (TryParsePositiveInt(@params[1], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[2], out int requestedPlayers) == false)
                return "Invalid player count.";

            if (TryParsePositiveInt(@params[3], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            previewCount = Math.Clamp(previewCount, 1, 20);
            List<string> lines = new(previewCount + 1)
            {
                $"Random Mythic Rift preview: {previewCount} sample(s)"
            };

            for (int index = 0; index < previewCount; index++)
            {
                MythicRiftRunConfig config = game.MythicRiftManager.CreateRandomDebugRunConfig(
                    riftLevel,
                    requestedPlayers,
                    0,
                    TimeSpan.FromMinutes(timeLimitMinutes));

                if (config == null)
                {
                    lines.Add("Failed to resolve a random Rift config from the current pool.");
                    break;
                }

                lines.Add(
                    $"sample={index + 1} | map={config.Content.Id} | bossSource={config.BossContent?.Id ?? "n/a"} | sameEntry={(string.Equals(config.Content.Id, config.BossContent?.Id, StringComparison.OrdinalIgnoreCase))} | quota={config.KillQuota} | timer={config.TimeLimit.TotalMinutes:0} min");
            }

            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("validaterandompool")]
        [CommandDescription("Validates every currently random-eligible map/boss combination without creating live runs.")]
        [CommandUsage("rift validaterandompool [level] [players] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(3)]
        public string ValidateRandomPool(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            if (TryParsePositiveInt(@params[0], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[1], out int requestedPlayers) == false)
                return "Invalid player count.";

            if (TryParsePositiveInt(@params[2], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            IReadOnlyList<MythicRiftContentEntry> pool = game.MythicRiftManager.RandomEligibleContentPool;
            if (pool.Count == 0)
                return "No random-eligible Mythic Rift content is registered.";

            int totalCombos = 0;
            int validCombos = 0;
            int sameEntryCombos = 0;
            List<string> invalidLines = new();

            foreach (MythicRiftContentEntry mapContent in pool.OrderBy(entry => entry.Id))
            {
                foreach (MythicRiftContentEntry bossContent in pool.OrderBy(entry => entry.Id))
                {
                    totalCombos++;
                    bool sameEntry = string.Equals(mapContent.Id, bossContent.Id, StringComparison.OrdinalIgnoreCase);
                    if (sameEntry)
                        sameEntryCombos++;

                    MythicRiftRunConfig config = game.MythicRiftManager.CreateDebugRunConfig(
                        mapContent.Id,
                        bossContent.Id,
                        riftLevel,
                        requestedPlayers,
                        0,
                        TimeSpan.FromMinutes(timeLimitMinutes));

                    RegionPrototype regionProto = config?.RegionProtoRef.As<RegionPrototype>();
                    RegionConnectionTargetPrototype startTargetProto = config?.StartTargetProtoRef.As<RegionConnectionTargetPrototype>();
                    MissionPrototype missionProto = config?.MissionProtoRef.As<MissionPrototype>();
                    AgentPrototype bossProto = config?.BossProtoRef.As<AgentPrototype>();
                    bool configValid = config?.IsValid == true;
                    bool regionValid = regionProto != null;
                    bool startTargetValid = startTargetProto != null;
                    bool targetMatchesRegion = regionValid && startTargetValid &&
                        RegionPrototype.Equivalent(startTargetProto.Region.As<RegionPrototype>(), regionProto);
                    bool missionValid = missionProto != null;
                    bool bossValid = bossProto != null;
                    bool lootValid = config != null && config.BossLootTableProtoRef != PrototypeId.Invalid;

                    bool comboValid = configValid && regionValid && startTargetValid && targetMatchesRegion && missionValid && bossValid && lootValid;
                    if (comboValid)
                    {
                        validCombos++;
                        continue;
                    }

                    invalidLines.Add(
                        $"INVALID map={mapContent.Id} bossSource={bossContent.Id} | configValid={configValid} | regionValid={regionValid} | startTargetValid={startTargetValid} | targetMatchesRegion={targetMatchesRegion} | missionValid={missionValid} | bossValid={bossValid} | lootValid={lootValid}");
                }
            }

            List<string> lines = new()
            {
                $"Random pool validation: maps={pool.Count} | bosses={pool.Count} | totalCombos={totalCombos} | validCombos={validCombos} | invalidCombos={totalCombos - validCombos} | sameEntryCombos={sameEntryCombos}"
            };

            if (invalidLines.Count == 0)
            {
                lines.Add("All random-eligible map/boss combinations resolved successfully.");
            }
            else
            {
                lines.AddRange(invalidLines.Take(25));
                if (invalidLines.Count > 25)
                    lines.Add($"... {invalidLines.Count - 25} more invalid combinations omitted.");
            }

            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("debugmix")]
        [CommandDescription("Builds a debug run config for a specific map content and a different boss content without starting a live run.")]
        [CommandUsage("rift debugmix [contentId] [bossContentId] [level] [players] [killQuota] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(6)]
        public string DebugMix(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            string contentId = @params[0];
            string bossContentId = @params[1];
            if (TryParsePositiveInt(@params[2], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[3], out int requestedPlayers) == false)
                return "Invalid player count.";

            if (TryParsePositiveInt(@params[4], out int killQuota) == false)
                return "Invalid kill quota.";

            if (TryParsePositiveInt(@params[5], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftRunConfig config = game.MythicRiftManager.CreateDebugRunConfig(
                contentId,
                bossContentId,
                riftLevel,
                requestedPlayers,
                killQuota,
                TimeSpan.FromMinutes(timeLimitMinutes));

            if (config == null)
                return $"Unknown Mythic Rift content id or boss content id: {contentId} / {bossContentId}";

            List<string> lines = new()
            {
                $"Mythic Rift mixed debug config for map={config.Content.DisplayName} boss={config.BossContent?.DisplayName ?? "n/a"}",
                $"runId={config.RunId} | level={config.RiftLevel} | requestedPlayers={config.RequestedPlayerCount} | effectivePlayers={config.EffectivePlayerCount}",
                $"killQuota={config.KillQuota} | timeLimit={config.TimeLimit.TotalMinutes:0} min | HP x{config.Difficulty.HealthMultiplier:F3} | damage x{config.Difficulty.DamageMultiplier:F3}",
                $"region={config.RegionProtoRef.GetNameFormatted()}",
                $"mission={config.MissionProtoRef.GetNameFormatted()}",
                $"bossSource={config.BossContent?.Id ?? "n/a"} | boss={config.BossProtoRef.GetNameFormatted()}",
                $"bossLoot={config.BossLootTableProtoRef.GetNameFormatted()}"
            };

            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("create")]
        [CommandDescription("Creates a random Mythic Rift debug run and registers it in memory.")]
        [CommandUsage("rift create [level] [players] [killQuota] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(4)]
        public string Create(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            if (TryParsePositiveInt(@params[0], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[1], out int requestedPlayers) == false)
                return "Invalid player count.";

            if (TryParsePositiveInt(@params[2], out int killQuota) == false)
                return "Invalid kill quota.";

            if (TryParsePositiveInt(@params[3], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftRunState runState = game.MythicRiftManager.CreateRandomDebugRun(
                riftLevel,
                requestedPlayers,
                killQuota,
                TimeSpan.FromMinutes(timeLimitMinutes));

            if (runState == null)
                return "Failed to create a random Mythic Rift run.";

            CommandHelper.SendMessages(client, BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: true));
            return string.Empty;
        }

        [Command("createmix")]
        [CommandDescription("Creates a debug run with a fixed map content and a specific boss content, then registers it in memory.")]
        [CommandUsage("rift createmix [contentId] [bossContentId] [level] [players] [killQuota] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(6)]
        public string CreateMix(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            string contentId = @params[0];
            string bossContentId = @params[1];
            if (TryParsePositiveInt(@params[2], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[3], out int requestedPlayers) == false)
                return "Invalid player count.";

            if (TryParsePositiveInt(@params[4], out int killQuota) == false)
                return "Invalid kill quota.";

            if (TryParsePositiveInt(@params[5], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftRunState runState = game.MythicRiftManager.CreateDebugRun(
                contentId,
                bossContentId,
                riftLevel,
                requestedPlayers,
                killQuota,
                TimeSpan.FromMinutes(timeLimitMinutes));

            if (runState == null)
                return $"Failed to create a mixed Mythic Rift run for content id / boss content id: {contentId} / {bossContentId}";

            CommandHelper.SendMessages(client, BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: true));
            return string.Empty;
        }

        [Command("request")]
        [CommandDescription("Requests a Mythic Rift run for the invoking player or party using access validation.")]
        [CommandUsage("rift request [level] [killQuota] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(3)]
        public string Request(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            if (TryParsePositiveInt(@params[0], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[1], out int killQuota) == false)
                return "Invalid kill quota.";

            if (TryParsePositiveInt(@params[2], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftEntryResult result = game.MythicRiftEntryService.RequestRun(player, new MythicRiftEntryRequest
            {
                RiftLevel = riftLevel,
                KillQuotaOverride = killQuota,
                TimeLimit = TimeSpan.FromMinutes(timeLimitMinutes)
            });

            if (result.Success == false)
                return string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Failed to request Mythic Rift run." : result.ErrorMessage;

            MythicRiftRunState runState = result.RunState;
            CommandHelper.SendMessages(client, BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: true));
            return string.Empty;
        }

        [Command("requestauto")]
        [CommandDescription("Requests a Mythic Rift run using the content's default kill quota.")]
        [CommandUsage("rift requestauto [level] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(2)]
        public string RequestAuto(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            if (TryParsePositiveInt(@params[0], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[1], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftEntryResult result = game.MythicRiftEntryService.RequestRun(player, new MythicRiftEntryRequest
            {
                RiftLevel = riftLevel,
                TimeLimit = TimeSpan.FromMinutes(timeLimitMinutes)
            });

            if (result.Success == false)
                return string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Failed to request Mythic Rift run." : result.ErrorMessage;

            MythicRiftRunState runState = result.RunState;
            CommandHelper.SendMessages(client, BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: true));
            return string.Empty;
        }

        [Command("requestportal")]
        [CommandDescription("Requests a random Mythic Rift run through the officially chosen Cosmic Rift launcher base built on PortalToRandomDungeon.")]
        [CommandUsage("rift requestportal [level] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(2)]
        public string RequestPortal(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            if (TryParsePositiveInt(@params[0], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[1], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftEntryResult result = game.MythicRiftEntryService.RequestRun(player, new MythicRiftEntryRequest
            {
                EntryPointId = MythicRiftEntryService.PortalToRandomDungeonEntryPointId,
                LauncherItemPrototypeName = "PortalToRandomDungeon",
                RiftLevel = riftLevel,
                TimeLimit = TimeSpan.FromMinutes(timeLimitMinutes)
            });

            if (result.Success == false)
                return string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Failed to request Mythic Rift portal run." : result.ErrorMessage;

            MythicRiftRunState runState = result.RunState;
            List<string> lines = BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: true);
            lines.InsertRange(0, BuildLaunchPlanLines(result.LaunchPlan));
            lines.Insert(0, "Requested through official Cosmic Rift launcher base: PortalToRandomDungeon.");
            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("givebeacon")]
        [CommandDescription("Grants the currently chosen Cosmic Rift Beacon base item to the invoking player without requiring any client-side vendor changes.")]
        [CommandUsage("rift givebeacon [count]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string GiveBeacon(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            if (TryParsePositiveInt(@params[0], out int count) == false)
                return "Invalid beacon count.";

            if (game.MythicRiftLauncherService.TryGrantChosenLauncher(player, count, out PrototypeId itemProtoRef, out string errorMessage) == false)
                return string.IsNullOrWhiteSpace(errorMessage) ? "Failed to grant Cosmic Rift Beacon." : errorMessage;

            return $"Granted {count}x {MythicRiftLauncherService.CosmicRiftBeaconDisplayName} to the player using {itemProtoRef.GetNameFormatted()}.";
        }

        [Command("prepbeacon")]
        [CommandDescription("Prepares the invoking player for Cosmic Rift Beacon testing by unlocking a target Rift level and granting beacon items server-side.")]
        [CommandUsage("rift prepbeacon [level] [count]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(2)]
        public string PrepBeacon(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            if (TryParsePositiveInt(@params[0], out int unlockedLevel) == false)
                return "Invalid Rift level.";

            if (TryParsePositiveInt(@params[1], out int beaconCount) == false)
                return "Invalid beacon count.";

            int appliedLevel = game.MythicRiftManager.SetHighestUnlockedRiftLevel(player.DatabaseUniqueId, unlockedLevel);
            if (game.MythicRiftLauncherService.TryGrantChosenLauncher(player, beaconCount, out PrototypeId itemProtoRef, out string errorMessage) == false)
                return string.IsNullOrWhiteSpace(errorMessage) ? "Failed to prepare Cosmic Rift Beacon test flow." : errorMessage;

            return $"Prepared player for Cosmic Rift testing: unlockedLevel={appliedLevel} | grantedBeacons={beaconCount} | technicalBase={itemProtoRef.GetNameFormatted()}";
        }

        [Command("armbeacon")]
        [CommandDescription("Arms a scoped Cosmic Rift Beacon override for the invoking player. The next valid beacon use will create a Rift directly without changing default Danger Room behavior globally.")]
        [CommandUsage("rift armbeacon [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string ArmBeacon(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            if (TryParsePositiveInt(@params[0], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftArmedLauncherState armedState = game.MythicRiftLauncherService.ArmChosenLauncher(
                player,
                0,
                TimeSpan.FromMinutes(timeLimitMinutes));

            return $"Armed scoped Cosmic Rift Beacon override for next use: level=auto | timeLimit={armedState.TimeLimit.TotalMinutes:0} min | technicalBase={MythicRiftLauncherService.CosmicRiftBeaconPrototypeName}";
        }

        [Command("armbeaconfixed")]
        [CommandDescription("Arms a scoped Cosmic Rift Beacon override for a specific fixed V1 content id without changing normal Danger Room behavior globally.")]
        [CommandUsage("rift armbeaconfixed [contentId] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(2)]
        public string ArmBeaconFixed(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            string contentId = @params[0];
            if (string.IsNullOrWhiteSpace(contentId))
                return "Invalid content id.";

            if (game.MythicRiftManager.GetContent(contentId) == null)
                return $"Unknown Mythic Rift content id: {contentId}";

            if (TryParsePositiveInt(@params[1], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftArmedLauncherState armedState = game.MythicRiftLauncherService.ArmChosenLauncher(
                player,
                0,
                TimeSpan.FromMinutes(timeLimitMinutes),
                contentId);

            return $"Armed scoped Cosmic Rift Beacon override for next use: content={armedState.FixedContentId} | level=auto | timeLimit={armedState.TimeLimit.TotalMinutes:0} min | technicalBase={MythicRiftLauncherService.CosmicRiftBeaconPrototypeName}";
        }

        [Command("disarmbeacon")]
        [CommandDescription("Disarms the scoped Cosmic Rift Beacon override for the invoking player.")]
        [CommandUsage("rift disarmbeacon")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string DisarmBeacon(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            bool disarmed = game.MythicRiftLauncherService.DisarmChosenLauncher(player.DatabaseUniqueId);
            return disarmed
                ? "Scoped Cosmic Rift Beacon override disarmed."
                : "No scoped Cosmic Rift Beacon override was armed for this player.";
        }

        [Command("beaconmode")]
        [CommandDescription("Displays the invoking player's current Cosmic Rift Beacon state, including scoped override state, tracked beacon charges, and the result of the last launcher use.")]
        [CommandUsage("rift beaconmode")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string BeaconMode(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            MythicRiftArmedLauncherState armedState = game.MythicRiftLauncherService.GetArmedLauncherState(player.DatabaseUniqueId);
            MythicRiftLauncherUseResult lastResult = game.MythicRiftLauncherService.GetLastArmedLaunchResult(player.DatabaseUniqueId);
            int trackedBeaconCharges = game.MythicRiftLauncherService.GetTotalTrackedBeaconCharges(player.DatabaseUniqueId);

            List<string> lines = new();
            lines.Add($"trackedCosmicRiftBeaconCharges={trackedBeaconCharges}");
            if (armedState == null)
            {
                lines.Add("scopedBeaconOverride=inactive");
            }
            else
            {
                lines.Add($"scopedBeaconOverride=armed | fixedContent={(string.IsNullOrWhiteSpace(armedState.FixedContentId) ? "random" : armedState.FixedContentId)} | requestedLevel={(armedState.RequestedRiftLevel > 0 ? armedState.RequestedRiftLevel : "auto")} | timeLimit={armedState.TimeLimit.TotalMinutes:0} min | armedAt={armedState.ArmedAt}");
            }

            if (lastResult == null)
            {
                lines.Add("lastArmedLaunchResult=none");
            }
            else
            {
                lines.Add($"lastArmedLaunchResultSuccess={lastResult.Success} | consumedArmedMode={lastResult.ConsumedArmedLaunchMode} | item={lastResult.ItemPrototypeName ?? "n/a"} | level={lastResult.ResolvedRiftLevel} | timeLimit={lastResult.ResolvedTimeLimit.TotalMinutes:0} min");
                lines.Add($"lastArmedLaunchTeleportAttempted={lastResult.TeleportAttempted} | teleportSucceeded={lastResult.TeleportSucceeded} | teleportTarget={lastResult.TeleportTargetProtoRef.GetNameFormatted()}");
                MythicRiftRunState launchedRun = lastResult.EntryResult?.RunState;
                if (launchedRun != null)
                {
                    lines.Add($"lastArmedLaunchRunId={launchedRun.Config.RunId} | content={launchedRun.Config.Content.Id} | region={launchedRun.Config.RegionProtoRef.GetNameFormatted()} | entryTarget={launchedRun.Config.StartTargetProtoRef.GetNameFormatted()}");
                    lines.Add($"lastArmedLaunchRunStatus={launchedRun.Status} | regionId=0x{launchedRun.RegionId:X} | participants={launchedRun.ParticipantCount}");
                }

                if (string.IsNullOrWhiteSpace(lastResult.ErrorMessage) == false)
                    lines.Add($"lastArmedLaunchError={lastResult.ErrorMessage}");
                if (string.IsNullOrWhiteSpace(lastResult.TeleportErrorMessage) == false)
                    lines.Add($"lastArmedLaunchTeleportError={lastResult.TeleportErrorMessage}");
            }

            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("requestitem")]
        [CommandDescription("Simulates using a registered launcher item candidate and converts it into a Mythic Rift request.")]
        [CommandUsage("rift requestitem [itemPrototypeName] [level] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(3)]
        public string RequestItem(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            string itemPrototypeName = @params[0];
            if (TryParsePositiveInt(@params[1], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[2], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftLauncherItemCandidate candidate = game.MythicRiftEntryService.LauncherItemCandidates
                .FirstOrDefault(entry => string.Equals(entry.PrototypeName, itemPrototypeName, StringComparison.OrdinalIgnoreCase));

            if (candidate == null)
                return $"Unknown Mythic Rift launcher item candidate: {itemPrototypeName}";

            MythicRiftLauncherUseResult useResult = game.MythicRiftLauncherService.TryRequestRunFromPrototypeName(
                player,
                candidate.PrototypeName,
                riftLevel,
                TimeSpan.FromMinutes(timeLimitMinutes));

            if (useResult.Success == false)
                return string.IsNullOrWhiteSpace(useResult.ErrorMessage) ? "Failed to request Mythic Rift run from launcher item." : useResult.ErrorMessage;

            List<string> lines = new()
            {
                $"launcherItem={useResult.ItemPrototypeName} | candidateRecommendation={candidate.Recommendation} | portalTarget={useResult.PortalTargetRegionProtoRef.GetNameFormatted()}"
            };

            lines.AddRange(BuildLaunchPlanLines(useResult.EntryResult.LaunchPlan));
            lines.AddRange(BuildRunLines(useResult.EntryResult.RunState, game.CurrentTime, includeResolvedRefs: true));
            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("consumeintent")]
        [CommandDescription("Consumes the invoking player's pending launcher intent and turns it into a Mythic Rift run.")]
        [CommandUsage("rift consumeintent [level] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(2)]
        public string ConsumeIntent(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            if (TryParsePositiveInt(@params[0], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[1], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftLauncherUseResult useResult = game.MythicRiftLauncherService.ConsumePendingIntent(
                player,
                riftLevel,
                TimeSpan.FromMinutes(timeLimitMinutes));

            if (useResult.Success == false)
                return string.IsNullOrWhiteSpace(useResult.ErrorMessage) ? "Failed to consume Mythic Rift launcher intent." : useResult.ErrorMessage;

            List<string> lines = new();
            MythicRiftLauncherIntent clearedIntent = game.MythicRiftLauncherService.GetPendingIntent(player.DatabaseUniqueId);
            lines.Add($"launcherItem={useResult.ItemPrototypeName} | level={useResult.ResolvedRiftLevel} | timeLimit={useResult.ResolvedTimeLimit.TotalMinutes:0} min | portalTarget={useResult.PortalTargetRegionProtoRef.GetNameFormatted()} | pendingIntentCleared={(clearedIntent == null)}");
            lines.AddRange(BuildLaunchPlanLines(useResult.EntryResult.LaunchPlan));
            lines.AddRange(BuildRunLines(useResult.EntryResult.RunState, game.CurrentTime, includeResolvedRefs: true));
            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("consumeintentauto")]
        [CommandDescription("Consumes the invoking player's pending launcher intent using their highest unlocked Rift level and the default launcher timer unless overridden.")]
        [CommandUsage("rift consumeintentauto [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string ConsumeIntentAuto(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            if (TryParsePositiveInt(@params[0], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftLauncherUseResult useResult = game.MythicRiftLauncherService.ConsumePendingIntentAuto(
                player,
                TimeSpan.FromMinutes(timeLimitMinutes));

            if (useResult.Success == false)
                return string.IsNullOrWhiteSpace(useResult.ErrorMessage) ? "Failed to auto-consume Mythic Rift launcher intent." : useResult.ErrorMessage;

            List<string> lines = new();
            MythicRiftLauncherIntent clearedIntent = game.MythicRiftLauncherService.GetPendingIntent(player.DatabaseUniqueId);
            lines.Add($"launcherItem={useResult.ItemPrototypeName} | autoLevel={useResult.ResolvedRiftLevel} | timeLimit={useResult.ResolvedTimeLimit.TotalMinutes:0} min | portalTarget={useResult.PortalTargetRegionProtoRef.GetNameFormatted()} | pendingIntentCleared={(clearedIntent == null)}");
            lines.AddRange(BuildLaunchPlanLines(useResult.EntryResult.LaunchPlan));
            lines.AddRange(BuildRunLines(useResult.EntryResult.RunState, game.CurrentTime, includeResolvedRefs: true));
            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("createfixed")]
        [CommandDescription("Creates a fixed Mythic Rift debug run for a specific content id and registers it in memory.")]
        [CommandUsage("rift createfixed [contentId] [level] [players] [killQuota] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(5)]
        public string CreateFixed(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            string contentId = @params[0];
            if (TryParsePositiveInt(@params[1], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[2], out int requestedPlayers) == false)
                return "Invalid player count.";

            if (TryParsePositiveInt(@params[3], out int killQuota) == false)
                return "Invalid kill quota.";

            if (TryParsePositiveInt(@params[4], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftRunState runState = game.MythicRiftManager.CreateDebugRun(
                contentId,
                riftLevel,
                requestedPlayers,
                killQuota,
                TimeSpan.FromMinutes(timeLimitMinutes));

            if (runState == null)
                return $"Failed to create Mythic Rift run for content id: {contentId}";

            CommandHelper.SendMessages(client, BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: true));
            return string.Empty;
        }

        [Command("requestfixed")]
        [CommandDescription("Requests a fixed Mythic Rift run for the invoking player or party using access validation.")]
        [CommandUsage("rift requestfixed [contentId] [level] [killQuota] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(4)]
        public string RequestFixed(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            string contentId = @params[0];
            if (TryParsePositiveInt(@params[1], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[2], out int killQuota) == false)
                return "Invalid kill quota.";

            if (TryParsePositiveInt(@params[3], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftEntryResult result = game.MythicRiftEntryService.RequestRun(player, new MythicRiftEntryRequest
            {
                RiftLevel = riftLevel,
                ContentId = contentId,
                KillQuotaOverride = killQuota,
                TimeLimit = TimeSpan.FromMinutes(timeLimitMinutes)
            });

            if (result.Success == false)
                return string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Failed to request Mythic Rift run." : result.ErrorMessage;

            MythicRiftRunState runState = result.RunState;
            CommandHelper.SendMessages(client, BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: true));
            return string.Empty;
        }

        [Command("requestfixedauto")]
        [CommandDescription("Requests a fixed Mythic Rift run using that content's default kill quota.")]
        [CommandUsage("rift requestfixedauto [contentId] [level] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(3)]
        public string RequestFixedAuto(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            string contentId = @params[0];
            if (TryParsePositiveInt(@params[1], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[2], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            MythicRiftEntryResult result = game.MythicRiftEntryService.RequestRun(player, new MythicRiftEntryRequest
            {
                RiftLevel = riftLevel,
                ContentId = contentId,
                TimeLimit = TimeSpan.FromMinutes(timeLimitMinutes)
            });

            if (result.Success == false)
                return string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Failed to request Mythic Rift run." : result.ErrorMessage;

            MythicRiftRunState runState = result.RunState;
            CommandHelper.SendMessages(client, BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: true));
            return string.Empty;
        }

        [Command("runs")]
        [CommandDescription("Lists active Mythic Rift runs currently registered in memory.")]
        [CommandUsage("rift runs")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Runs(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            IReadOnlyCollection<MythicRiftRunState> activeRuns = game.MythicRiftManager.ActiveRuns;
            if (activeRuns.Count == 0)
                return "No active Mythic Rift runs are registered.";

            List<string> lines = new(activeRuns.Count + 1)
            {
                $"Active Mythic Rift runs: {activeRuns.Count}"
            };

            foreach (MythicRiftRunState runState in activeRuns.OrderBy(run => run.Config.RunId))
            {
                lines.Add(
                    $"runId={runState.Config.RunId} | status={runState.Status} | content={runState.Config.Content.DisplayName} | bossSource={runState.Config.BossContent?.DisplayName ?? "n/a"} | level={runState.Config.RiftLevel} | kills={runState.CurrentKillCount}/{runState.Config.KillQuota}");
            }

            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("run")]
        [CommandDescription("Displays details for one registered Mythic Rift run.")]
        [CommandUsage("rift run [runId]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Run(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            if (ulong.TryParse(@params[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong runId) == false || runId == 0)
                return "Invalid run id.";

            MythicRiftRunState runState = game.MythicRiftManager.GetRun(runId);
            if (runState == null)
                return $"Run not found: {runId}";

            CommandHelper.SendMessages(client, BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: true));
            return string.Empty;
        }

        [Command("start")]
        [CommandDescription("Starts a registered Mythic Rift run and arms its timer.")]
        [CommandUsage("rift start [runId]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Start(string[] @params, NetClient client)
        {
            return MutateRun(client, @params[0], (game, runId) => game.MythicRiftManager.StartRun(runId, game.CurrentTime), "Run started.");
        }

        [Command("kills")]
        [CommandDescription("Adds kill progress to a registered Mythic Rift run.")]
        [CommandUsage("rift kills [runId] [count]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(2)]
        public string Kills(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            if (TryParseRunId(@params[0], out ulong runId) == false)
                return "Invalid run id.";

            if (TryParsePositiveInt(@params[1], out int killCount) == false)
                return "Invalid kill count.";

            if (game.MythicRiftManager.AddKills(runId, killCount) == false)
                return $"Failed to add kills for run {runId}.";

            MythicRiftRunState runState = game.MythicRiftManager.GetRun(runId);
            CommandHelper.SendMessages(client, BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: false));
            return string.Empty;
        }

        [Command("success")]
        [CommandDescription("Marks a registered Mythic Rift run as successful.")]
        [CommandUsage("rift success [runId]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Success(string[] @params, NetClient client)
        {
            return MutateRun(client, @params[0], (game, runId) => game.MythicRiftManager.MarkRunSuccess(runId, game.CurrentTime), "Run marked as success.");
        }

        [Command("fail")]
        [CommandDescription("Marks a registered Mythic Rift run as failed.")]
        [CommandUsage("rift fail [runId]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Fail(string[] @params, NetClient client)
        {
            return MutateRun(client, @params[0], (game, runId) => game.MythicRiftManager.MarkRunFailed(runId, game.CurrentTime), "Run marked as failed.");
        }

        [Command("abort")]
        [CommandDescription("Aborts a registered Mythic Rift run.")]
        [CommandUsage("rift abort [runId]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Abort(string[] @params, NetClient client)
        {
            return MutateRun(client, @params[0], (game, runId) => game.MythicRiftManager.MarkRunAborted(runId, game.CurrentTime), "Run aborted.");
        }

        [Command("tick")]
        [CommandDescription("Evaluates the timer for a registered Mythic Rift run and fails it if time is over.")]
        [CommandUsage("rift tick [runId]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Tick(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            if (TryParseRunId(@params[0], out ulong runId) == false)
                return "Invalid run id.";

            bool expired = game.MythicRiftManager.EvaluateRunTimer(runId, game.CurrentTime);
            MythicRiftRunState runState = game.MythicRiftManager.GetRun(runId);
            if (runState == null)
                return $"Run not found: {runId}";

            List<string> lines = BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: false);
            lines.Insert(0, expired ? "Timer expired: run failed." : "Timer check complete: run still active.");
            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("remove")]
        [CommandDescription("Removes a registered Mythic Rift run from memory.")]
        [CommandUsage("rift remove [runId]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Remove(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            if (TryParseRunId(@params[0], out ulong runId) == false)
                return "Invalid run id.";

            return game.MythicRiftManager.RemoveRun(runId)
                ? $"Run removed: {runId}"
                : $"Run not found: {runId}";
        }

        [Command("reward")]
        [CommandDescription("Grants the resolved Mythic Rift reward for a finished run to the invoking player.")]
        [CommandUsage("rift reward [runId]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Reward(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            if (TryParseRunId(@params[0], out ulong runId) == false)
                return "Invalid run id.";

            Player player = playerConnection.Player;
            if (player == null)
                return "Player not found.";

            if (game.MythicRiftManager.GrantRewardsToPlayer(runId, player) == false)
                return $"Failed to grant rewards for run {runId}.";

            MythicRiftRunState runState = game.MythicRiftManager.GetRun(runId);
            List<string> lines = BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: false);
            lines.Insert(0, $"Rewards granted for run {runId}.");
            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("rewardall")]
        [CommandDescription("Grants the resolved Mythic Rift reward to all tracked participants of a finished run.")]
        [CommandUsage("rift rewardall [runId]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string RewardAll(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            if (TryParseRunId(@params[0], out ulong runId) == false)
                return "Invalid run id.";

            int grantedCount = game.MythicRiftManager.GrantRewardsToRunPlayers(runId);
            MythicRiftRunState runState = game.MythicRiftManager.GetRun(runId);
            if (runState == null)
                return $"Run not found: {runId}";

            List<string> lines = BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: false);
            lines.Insert(0, $"Rewards granted to {grantedCount} player(s) for run {runId}.");
            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("bind")]
        [CommandDescription("Binds a Mythic Rift run to the invoker's current region so live kills can advance the quota.")]
        [CommandUsage("rift bind [runId]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Bind(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            if (TryParseRunId(@params[0], out ulong runId) == false)
                return "Invalid run id.";

            Region region = playerConnection.Player?.GetRegion();
            if (region == null)
                return "Current region not found.";

            if (game.MythicRiftManager.AttachRunToRegion(runId, region) == false)
                return $"Failed to bind run {runId} to the current region.";

            MythicRiftRunState runState = game.MythicRiftManager.GetRun(runId);
            List<string> lines = BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: false);
            lines.Insert(0, $"Run {runId} bound to region {region.PrototypeName} (0x{region.Id:X}).");
            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        private static List<string> BuildRunLines(MythicRiftRunState runState, TimeSpan currentTime, bool includeResolvedRefs)
        {
            List<string> lines = new()
            {
                $"Mythic Rift run {runState.Config.RunId}",
                $"status={runState.Status} | content={runState.Config.Content.DisplayName} ({runState.Config.Content.Id})",
                $"bossSource={runState.Config.BossContent?.DisplayName ?? "n/a"} ({runState.Config.BossContent?.Id ?? "n/a"}) | regionScalingApplied={runState.RegionDifficultyScalingApplied}",
                $"level={runState.Config.RiftLevel} | requestedPlayers={runState.Config.RequestedPlayerCount} | effectivePlayers={runState.Config.EffectivePlayerCount}",
                $"killQuota={runState.CurrentKillCount}/{runState.Config.KillQuota} | bossUnlocked={runState.BossUnlocked} | rewardsGranted={runState.RewardsGranted}",
                $"timeLimit={runState.Config.TimeLimit.TotalMinutes:0} min | remaining={runState.GetTimeRemaining(currentTime).TotalMinutes:0.##} min | d3EquivalentLevel={runState.Config.Difficulty.EquivalentD3RiftLevel:F2} | groupHealth x{runState.Config.Difficulty.GroupHealthMultiplier:F3} | HP x{runState.Config.Difficulty.HealthMultiplier:F3} | damage x{runState.Config.Difficulty.DamageMultiplier:F3}",
                $"regionId=0x{runState.RegionId:X} | bossEntityId=0x{runState.BossEntityId:X}",
                $"participants={runState.ParticipantCount} | rewardedPlayers={runState.RewardedPlayerCount}",
                $"competitiveEligibility=bossUnlock:{runState.BossUnlockEligiblePlayerDbIds.Count} | bossKill:{runState.ProgressionEligiblePlayerDbIds.Count}",
                $"nextUnlockOnSuccess={runState.Config.RiftLevel + 1}"
            };

            MythicRiftRewardOutcome rewardOutcome = runState.RewardOutcome;
            if (rewardOutcome != null)
            {
                lines.Add(
                    $"rewardBossLoot={rewardOutcome.BossLootTableProtoRef.GetNameFormatted()} | timedBonus={rewardOutcome.TimedSuccessBonusApplied} | bonusRIF={rewardOutcome.BonusRarityPct:P0} | bonusSIF={rewardOutcome.BonusSpecialPct:P0}");
            }

            if (runState.ExpiresAt.HasValue)
                lines.Add($"startedAt={runState.StartedAt} | expiresAt={runState.ExpiresAt.Value} | completedAt={(runState.CompletedAt.HasValue ? runState.CompletedAt.Value.ToString() : "n/a")}");

            if (includeResolvedRefs)
            {
                lines.Add($"region={runState.Config.RegionProtoRef.GetNameFormatted()}");
                lines.Add($"entryTarget={runState.Config.StartTargetProtoRef.GetNameFormatted()}");
                lines.Add($"mission={runState.Config.MissionProtoRef.GetNameFormatted()}");
                lines.Add($"bossSource={runState.Config.BossContent?.Id ?? "n/a"}");
                lines.Add($"boss={runState.Config.BossProtoRef.GetNameFormatted()}");
                lines.Add($"bossLoot={runState.Config.BossLootTableProtoRef.GetNameFormatted()}");
                lines.Add($"regionDamageScalingOriginal=playerToMob:{runState.RegionPlayerToMobDamageMultiplierBeforeScaling:F3} mobToPlayer:{runState.RegionMobToPlayerDamageMultiplierBeforeScaling:F3}");
            }

            return lines;
        }

        private static List<string> BuildLaunchPlanLines(MythicRiftPortalLaunchPlan launchPlan)
        {
            if (launchPlan == null)
                return new() { "launchPlan=n/a" };

            return new()
            {
                $"launchEntryPoint={launchPlan.EntryPointId} | launchModel={launchPlan.LaunchModel} | patcherFriendly={launchPlan.IsPatcherFriendly}",
                $"launcherItem={launchPlan.LauncherItemPrototypeName ?? "n/a"} | transition={launchPlan.TransitionPrototypeName ?? "n/a"} | consumesItem={launchPlan.ConsumesLauncherItem} | privatePortal={launchPlan.CreatesPrivatePortal} | randomOnly={launchPlan.RandomContentOnly}",
                $"launchNotes={launchPlan.Notes}"
            };
        }

        private static List<string> BuildLauncherIntentLines(MythicRiftLauncherIntent intent, int unlockedLevel)
        {
            return new()
            {
                $"pendingLauncherItem={intent.ItemPrototypeName} | entryPoint={intent.EntryPointId} | portalTarget={intent.PortalTargetRegionProtoRef.GetNameFormatted()}",
                $"intentCreatedAt={intent.CreatedAt} | recommendedAutoLevel={unlockedLevel} | defaultTimeLimit={MythicRiftLauncherService.DefaultLauncherTimeLimit.TotalMinutes:0} min"
            };
        }

        private static string MutateRun(NetClient client, string runIdText, Func<Game, ulong, bool> operation, string successMessage)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            if (game == null)
                return "Game not found.";

            if (TryParseRunId(runIdText, out ulong runId) == false)
                return "Invalid run id.";

            if (operation(game, runId) == false)
                return $"Operation failed for run {runId}.";

            MythicRiftRunState runState = game.MythicRiftManager.GetRun(runId);
            if (runState == null)
                return $"Run not found: {runId}";

            List<string> lines = BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: false);
            lines.Insert(0, successMessage);
            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        private static bool TryParseRunId(string value, out ulong parsedValue)
        {
            return ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue) && parsedValue != 0;
        }

        private static bool TryParsePositiveInt(string value, out int parsedValue)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue) == false)
                return false;

            return parsedValue > 0;
        }
    }
}
