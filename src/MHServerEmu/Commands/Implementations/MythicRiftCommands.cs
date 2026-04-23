using System.Globalization;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.MythicRifts;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
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
            int selectedLevel = game.MythicRiftManager.GetPreferredLaunchRiftLevel(player.DatabaseUniqueId);
            MythicRiftRunState inProgressRun = game.MythicRiftManager.GetInProgressRunForPlayer(player.DatabaseUniqueId);

            List<string> lines = new()
            {
                $"playerDbId=0x{player.DatabaseUniqueId:X} | highestUnlockedRiftLevel={unlockedLevel} | selectedLaunchRiftLevel={selectedLevel} | persistedPlayerValue={player.MythicRiftHighestUnlockedLevel}"
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
        [CommandDescription("Requests a random Mythic Rift run through the current chosen Cosmic Rift launcher base.")]
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
                LauncherItemPrototypeName = MythicRiftLauncherService.CosmicRiftBeaconPrototypeName,
                RiftLevel = riftLevel,
                TimeLimit = TimeSpan.FromMinutes(timeLimitMinutes)
            });

            if (result.Success == false)
                return string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Failed to request Mythic Rift portal run." : result.ErrorMessage;

            MythicRiftRunState runState = result.RunState;
            List<string> lines = BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: true);
            lines.InsertRange(0, BuildLaunchPlanLines(result.LaunchPlan));
            lines.Insert(0, $"Requested through official Cosmic Rift launcher base: {MythicRiftLauncherService.CosmicRiftBeaconPrototypeName}.");
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

            lines.AddRange(BuildOwnedLauncherItemLines(game, player));
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

        private static List<string> BuildOwnedLauncherItemLines(Game game, Player player)
        {
            List<string> lines = new();
            if (game == null || player == null)
                return lines;

            int ownedLauncherItems = 0;
            int ownedLauncherStacks = 0;
            InventoryIterationFlags flags = InventoryIterationFlags.PlayerGeneral
                | InventoryIterationFlags.PlayerGeneralExtra
                | InventoryIterationFlags.DeliveryBoxAndErrorRecovery;

            foreach (Inventory inventory in new InventoryIterator(player, flags))
            {
                foreach (var entry in inventory)
                {
                    Item item = game.EntityManager.GetEntity<Item>(entry.Id);
                    if (item == null)
                        continue;

                    bool preferredBeacon = game.MythicRiftLauncherService.IsPreferredCosmicRiftBeaconPrototype(item.PrototypeDataRef);
                    MythicRiftLauncherItemCandidate candidate = game.MythicRiftLauncherService.ResolveCandidate(item);
                    if (preferredBeacon == false && candidate == null)
                        continue;

                    ownedLauncherItems++;
                    ownedLauncherStacks += Math.Max(item.CurrentStackSize, 0);

                    int trackedCharges = game.MythicRiftLauncherService.GetTrackedBeaconChargeCount(player, item);
                    lines.Add($"ownedLauncherItem id={item.Id} | prototype={item.PrototypeDataRef.GetNameFormatted()} | stack={item.CurrentStackSize} | trackedCharges={trackedCharges} | preferredBeacon={preferredBeacon} | candidate={candidate?.PrototypeName ?? "none"} | recommendation={candidate?.Recommendation ?? "none"} | inventory={inventory.PrototypeDataRef.GetNameFormatted()} | slot={item.InventoryLocation.Slot} | onUsePower={item.OnUsePower.GetNameFormatted()}");
                }
            }

            lines.Insert(0, $"ownedLauncherItems={ownedLauncherItems} | ownedLauncherStacks={ownedLauncherStacks}");
            return lines;
        }

        [Command("vendorstock")]
        [CommandDescription("Inspects the current dialog target vendor and forces Cosmic Rift Beacon stock injection for troubleshooting.")]
        [CommandUsage("rift vendorstock")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string VendorStock(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            WorldEntity vendor = player.GetDialogTarget(true) ?? player.GetDialogTarget(false);
            if (vendor == null)
                return "No current dialog target vendor found. Open the Danger Room vendor first, then run `rift vendorstock` before buying.";

            player.EnsureMythicRiftVendorStock(vendor);

            PrototypeId vendorTypeProtoRef = vendor.Properties[PropertyEnum.VendorType];
            VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
            if (vendorTypeProto == null)
                return $"Current dialog target is not a valid vendor. target={vendor.PrototypeDataRef.GetNameFormatted()}";

            List<string> lines = new()
            {
                $"vendor={vendor.PrototypeDataRef.GetNameFormatted()} | vendorType={vendorTypeProtoRef.GetNameFormatted()} | region={vendor.Region?.PrototypeDataRef.GetNameFormatted() ?? "n/a"}"
            };

            List<PrototypeId> inventoryRefs = new();
            if (vendorTypeProto.GetInventories(inventoryRefs) == false || inventoryRefs.Count == 0)
            {
                lines.Add("vendorInventories=0");
                CommandHelper.SendMessages(client, lines);
                return string.Empty;
            }

            int totalBeaconItems = 0;
            foreach (PrototypeId inventoryRef in inventoryRefs)
            {
                Inventory inventory = player.GetInventoryByRef(inventoryRef);
                if (inventory == null)
                {
                    lines.Add($"inventory={inventoryRef.GetNameFormatted()} | missingOnPlayer=true");
                    continue;
                }

                int beaconItems = 0;
                List<string> beaconLines = new();
                foreach (var entry in inventory)
                {
                    Item item = game.EntityManager.GetEntity<Item>(entry.Id);
                    if (item == null)
                        continue;

                    bool preferredBeacon = game.MythicRiftLauncherService.IsPreferredCosmicRiftBeaconPrototype(item.PrototypeDataRef);
                    MythicRiftLauncherItemCandidate candidate = game.MythicRiftLauncherService.ResolveCandidate(item);
                    if (preferredBeacon == false && candidate == null)
                        continue;

                    beaconItems++;
                    totalBeaconItems++;
                    beaconLines.Add($"vendorLauncherItem inventory={inventory.PrototypeDataRef.GetNameFormatted()} | slot={entry.Slot} | itemId={item.Id} | prototype={item.PrototypeDataRef.GetNameFormatted()} | stack={item.CurrentStackSize} | preferredBeacon={preferredBeacon} | candidate={candidate?.PrototypeName ?? "none"}");
                }

                lines.Add($"inventory={inventory.PrototypeDataRef.GetNameFormatted()} | buyable={inventory.Prototype?.VendorInvContentsCanBeBought == true} | count={inventory.Count}/{inventory.GetCapacity()} | launcherItems={beaconItems}");
                lines.AddRange(beaconLines);
            }

            lines.Insert(1, $"vendorLauncherItemsTotal={totalBeaconItems}");
            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        [Command("diagbeacon")]
        [CommandDescription("Runs a server-side self-check for the current Cosmic Rift Beacon flow without requiring an actual client click. It validates prototype resolution, vendor item spec creation, temporary owned item usability, launcher recognition, and Rift request conversion, then cleans up the temporary run and item.")]
        [CommandUsage("rift diagbeacon [level] [minutes]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(2)]
        public string DiagBeacon(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            Avatar avatar = player?.CurrentAvatar;
            if (game == null || player == null || avatar == null)
                return "Game, player, or avatar not found.";

            if (TryParsePositiveInt(@params[0], out int riftLevel) == false)
                return "Invalid rift level.";

            if (TryParsePositiveInt(@params[1], out int timeLimitMinutes) == false)
                return "Invalid time limit.";

            List<string> lines = new();
            string chosenPrototypeName = MythicRiftLauncherService.CosmicRiftBeaconPrototypeName;
            string legacyPrototypeName = MythicRiftLauncherService.LegacyCosmicRiftBeaconPrototypeName;
            PrototypeId preferredPrototypeRef = game.MythicRiftLauncherService.ResolvePrototypeRefByName(MythicRiftLauncherService.PreferredCosmicRiftBeaconPrototypeName);
            PrototypeId legacyPrototypeRef = game.MythicRiftLauncherService.ResolvePrototypeRefByName(legacyPrototypeName);
            PrototypeId itemProtoRef = game.MythicRiftLauncherService.ResolveChosenBeaconPrototypeRef();
            ItemPrototype itemProto = itemProtoRef.As<ItemPrototype>();
            MythicRiftEntryPointDefinition portalEntryPoint = game.MythicRiftEntryService.GetEntryPoint(MythicRiftEntryService.PortalToRandomDungeonEntryPointId);
            MythicRiftLauncherItemCandidate candidate = game.MythicRiftEntryService.LauncherItemCandidates
                .FirstOrDefault(entry => string.Equals(entry.PrototypeName, chosenPrototypeName, StringComparison.OrdinalIgnoreCase));
            MythicRiftLauncherItemCandidate legacyCandidate = game.MythicRiftEntryService.LauncherItemCandidates
                .FirstOrDefault(entry => string.Equals(entry.PrototypeName, legacyPrototypeName, StringComparison.OrdinalIgnoreCase));
            string acceptedLaunchers = portalEntryPoint?.AcceptedCandidateItemPrototypeNames != null && portalEntryPoint.AcceptedCandidateItemPrototypeNames.Count > 0
                ? string.Join(",", portalEntryPoint.AcceptedCandidateItemPrototypeNames)
                : portalEntryPoint?.CandidateItemPrototypeName ?? "n/a";
            bool chosenAcceptedByEntryPoint = game.MythicRiftEntryService.EntryPointAcceptsLauncherItem(MythicRiftEntryService.PortalToRandomDungeonEntryPointId, chosenPrototypeName);
            bool legacyAcceptedByEntryPoint = game.MythicRiftEntryService.EntryPointAcceptsLauncherItem(MythicRiftEntryService.PortalToRandomDungeonEntryPointId, legacyPrototypeName);
            string currentRegionName = player.GetRegion()?.PrototypeDataRef.GetNameFormatted() ?? "n/a";

            lines.Add("diagScope=server-side beacon validation only | confirms=request conversion prerequisites | excludes=final client click and region bind");
            lines.Add($"chosenBeaconPrototype={chosenPrototypeName} | legacyFallback={legacyPrototypeName}");
            lines.Add($"prototypeResolved={(itemProtoRef != PrototypeId.Invalid)} | candidateRegistered={(candidate != null)} | candidateRecommendation={candidate?.Recommendation ?? "missing"}");
            lines.Add($"preferredPrototypeResolved={(preferredPrototypeRef != PrototypeId.Invalid)} | legacyPrototypeResolved={(legacyPrototypeRef != PrototypeId.Invalid)} | resolvedPrototypeRef={itemProtoRef.GetNameFormatted()}");
            lines.Add($"portalEntryPointRegistered={(portalEntryPoint != null)} | currentRegion={currentRegionName} | acceptedLaunchers={acceptedLaunchers}");
            lines.Add($"chosenAcceptedByEntryPoint={chosenAcceptedByEntryPoint} | legacyAcceptedByEntryPoint={legacyAcceptedByEntryPoint} | legacyCandidateRegistered={(legacyCandidate != null)} | legacyCandidateRecommendation={legacyCandidate?.Recommendation ?? "missing"}");

            if (portalEntryPoint == null)
            {
                lines.Add("diagResult=failed | reason=portal-to-random-dungeon entry point is not registered");
                CommandHelper.SendMessages(client, lines);
                return string.Empty;
            }

            if (candidate == null || chosenAcceptedByEntryPoint == false)
            {
                lines.Add("diagResult=failed | reason=chosen beacon candidate is not wired cleanly into the consumable portal entry point");
                CommandHelper.SendMessages(client, lines);
                return string.Empty;
            }

            if (itemProto == null)
            {
                lines.Add("diagResult=failed | reason=item prototype could not be resolved from game data");
                CommandHelper.SendMessages(client, lines);
                return string.Empty;
            }

            bool approvedForUse = itemProto.ApprovedForUse();
            bool liveTuningEnabled = itemProto.IsLiveTuningEnabled();
            bool vendorEnabled = itemProto.IsLiveTuningVendorEnabled();
            bool isUsable = itemProto.IsUsable;
            PrototypeId onUsePowerProtoRef = itemProto.GetOnUsePower();

            lines.Add($"approvedForUse={approvedForUse} | liveTuningEnabled={liveTuningEnabled} | vendorEnabled={vendorEnabled}");
            lines.Add($"isUsable={isUsable} | destinationFromVendor={itemProto.DestinationFromVendor} | onUsePower={onUsePowerProtoRef.GetNameFormatted()} | nativePortalTarget={itemProto.GetPortalTarget().GetNameFormatted()}");

            if (approvedForUse == false)
            {
                lines.Add("diagResult=failed | reason=beacon item prototype is not approved for use in this runtime");
                CommandHelper.SendMessages(client, lines);
                return string.Empty;
            }

            if (liveTuningEnabled == false)
            {
                lines.Add("diagResult=failed | reason=beacon item is not live-tuning enabled in this runtime");
                CommandHelper.SendMessages(client, lines);
                return string.Empty;
            }

            if (vendorEnabled == false)
            {
                lines.Add("diagResult=failed | reason=beacon item is not vendor-enabled in live tuning for this runtime");
                CommandHelper.SendMessages(client, lines);
                return string.Empty;
            }

            if (isUsable == false)
            {
                lines.Add("diagResult=failed | reason=beacon item prototype is not usable");
                CommandHelper.SendMessages(client, lines);
                return string.Empty;
            }

            if (onUsePowerProtoRef == PrototypeId.Invalid)
            {
                lines.Add("diagResult=failed | reason=beacon item has no OnUse power");
                CommandHelper.SendMessages(client, lines);
                return string.Empty;
            }

            ItemSpec itemSpec = game.LootManager.CreateItemSpec(itemProtoRef, LootContext.Vendor, player);
            lines.Add($"vendorItemSpecCreated={(itemSpec != null)}");
            if (itemSpec == null)
            {
                lines.Add("diagResult=failed | reason=CreateItemSpec(Vendor) returned null");
                CommandHelper.SendMessages(client, lines);
                return string.Empty;
            }

            Inventory inventory = player.GetInventory(itemProto.DestinationFromVendor);
            if (inventory == null)
            {
                lines.Add("diagResult=failed | reason=destination inventory could not be resolved for the beacon item");
                CommandHelper.SendMessages(client, lines);
                return string.Empty;
            }

            uint slot = inventory.GetFreeSlot(null, true);
            if (slot == Inventory.InvalidSlot)
            {
                lines.Add($"diagResult=failed | reason=no free slot in destination inventory {inventory.PrototypeDataRef.GetNameFormatted()}");
                CommandHelper.SendMessages(client, lines);
                return string.Empty;
            }

            Item tempItem = null;
            ulong tempRunId = 0;
            bool tempRunRemoved = false;
            bool trackedBeaconForgotten = false;

            try
            {
                using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
                settings.EntityRef = itemProtoRef;
                settings.ItemSpec = itemSpec;
                settings.InventoryLocation = new(player.Id, inventory.PrototypeDataRef, slot);

                if (player.IsInGame == false)
                    settings.OptionFlags &= ~EntitySettingsOptionFlags.EnterGame;

                tempItem = game.EntityManager.CreateEntity(settings) as Item;
                lines.Add($"temporaryOwnedItemCreated={(tempItem != null)} | destinationInventory={inventory.PrototypeDataRef.GetNameFormatted()}");
                if (tempItem == null)
                {
                    lines.Add("diagResult=failed | reason=temporary owned beacon instance could not be created");
                    CommandHelper.SendMessages(client, lines);
                    return string.Empty;
                }

                bool itemCanUseIgnoringPower = tempItem.CanUse(avatar, checkPower: false);
                bool itemCanUse = tempItem.CanUse(avatar);
                bool launcherCanHandleItem = game.MythicRiftLauncherService.CanHandleItem(tempItem);
                MythicRiftLauncherItemCandidate resolvedItemCandidate = game.MythicRiftLauncherService.ResolveCandidate(tempItem);
                bool chosenPrototypeRecognized = game.MythicRiftLauncherService.IsChosenBeaconPrototype(tempItem.PrototypeDataRef);
                lines.Add($"temporaryOwnedItemId={tempItem.Id} | currentStack={tempItem.CurrentStackSize} | canUseIgnoringPower={itemCanUseIgnoringPower} | canUse={itemCanUse} | launcherCanHandle={launcherCanHandleItem} | chosenPrototypeRecognized={chosenPrototypeRecognized}");
                lines.Add($"resolvedItemCandidate={resolvedItemCandidate?.PrototypeName ?? "none"} | resolvedItemRecommendation={resolvedItemCandidate?.Recommendation ?? "none"}");

                if (itemCanUseIgnoringPower == false)
                {
                    lines.Add("diagResult=failed | reason=temporary owned beacon item cannot be used even before power validation");
                    CommandHelper.SendMessages(client, lines);
                    return string.Empty;
                }

                if (itemCanUse == false)
                {
                    lines.Add("diagResult=failed | reason=temporary owned beacon item fails full CanUse validation");
                    CommandHelper.SendMessages(client, lines);
                    return string.Empty;
                }

                if (launcherCanHandleItem == false || chosenPrototypeRecognized == false || resolvedItemCandidate == null)
                {
                    lines.Add("diagResult=failed | reason=temporary owned beacon item is not accepted by Mythic Rift launcher candidate resolution");
                    CommandHelper.SendMessages(client, lines);
                    return string.Empty;
                }

                bool trackedRegistration = game.MythicRiftLauncherService.TryRegisterTrackedBeaconItem(player, tempItem);
                int trackedChargesAfterRegistration = game.MythicRiftLauncherService.GetTotalTrackedBeaconCharges(player.DatabaseUniqueId);
                lines.Add($"trackedRegistration={trackedRegistration} | trackedChargesAfterRegistration={trackedChargesAfterRegistration}");

                if (trackedRegistration == false || trackedChargesAfterRegistration <= 0)
                {
                    lines.Add("diagResult=failed | reason=temporary owned beacon item could not be registered as a tracked Mythic Rift beacon");
                    CommandHelper.SendMessages(client, lines);
                    return string.Empty;
                }

                MythicRiftLauncherUseResult useResult = game.MythicRiftLauncherService.TryRequestRunFromItem(
                    player,
                    tempItem,
                    riftLevel,
                    TimeSpan.FromMinutes(timeLimitMinutes));

                lines.Add($"requestFromOwnedItemSuccess={useResult.Success} | level={riftLevel} | timeLimit={timeLimitMinutes} min | requestError={useResult.ErrorMessage ?? string.Empty}");

                if (useResult.EntryResult?.LaunchPlan != null)
                    lines.AddRange(BuildLaunchPlanLines(useResult.EntryResult.LaunchPlan));

                MythicRiftRunState runState = useResult.EntryResult?.RunState;
                if (runState != null)
                {
                    tempRunId = runState.Config.RunId;
                    lines.AddRange(BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: true));
                }

                if (tempRunId != 0)
                {
                    tempRunRemoved = game.MythicRiftManager.RemoveRun(tempRunId);
                    lines.Add($"temporaryRunRemoved={tempRunRemoved} | runId={tempRunId}");
                }

                lines.Add(useResult.Success
                    ? "diagResult=ok | server-side beacon flow reached temporary owned item request conversion successfully"
                    : "diagResult=failed | server-side beacon flow broke during temporary owned item request conversion");
            }
            finally
            {
                if (tempItem != null)
                {
                    trackedBeaconForgotten = game.MythicRiftLauncherService.ForgetTrackedBeaconItem(player.DatabaseUniqueId, tempItem.Id);
                    tempItem.Destroy();
                }
            }

            lines.Add($"temporaryItemDestroyed={(tempItem != null)} | trackedBeaconForgotten={trackedBeaconForgotten}");
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

        [Command("status")]
        [CommandDescription("Displays the invoking player's active Cosmic Rift run.")]
        [CommandUsage("rift status")]
        [CommandUserLevel(AccountUserLevel.User)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Status(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            MythicRiftRunState runState = game.MythicRiftManager.GetInProgressRunForPlayer(player.DatabaseUniqueId);
            if (runState == null)
            {
                int unlockedLevel = game.MythicRiftManager.GetHighestUnlockedRiftLevel(player.DatabaseUniqueId);
                int selectedLevel = game.MythicRiftManager.GetPreferredLaunchRiftLevel(player.DatabaseUniqueId);
                return $"No active Cosmic Rift run. Highest unlocked Rift level: {unlockedLevel}. Next beacon launch level: {selectedLevel}.";
            }

            CommandHelper.SendMessages(client, BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: false));
            return string.Empty;
        }

        [Command("level")]
        [CommandDescription("Displays or changes the Cosmic Rift level used by the next beacon launch.")]
        [CommandUsage("rift level [level|max]")]
        [CommandUserLevel(AccountUserLevel.User)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Level(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            int unlockedLevel = game.MythicRiftManager.GetHighestUnlockedRiftLevel(player.DatabaseUniqueId);
            int selectedLevel = game.MythicRiftManager.GetPreferredLaunchRiftLevel(player.DatabaseUniqueId);

            if (@params.Length == 0)
                return $"Cosmic Rift launch level: {selectedLevel}. Highest unlocked: {unlockedLevel}. Use `rift level [1-{unlockedLevel}]` to farm a lower level, or `rift level max` to launch your highest unlocked level.";

            string requestedLevelText = @params[0];
            if (string.Equals(requestedLevelText, "max", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(requestedLevelText, "highest", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(requestedLevelText, "auto", StringComparison.OrdinalIgnoreCase))
            {
                int appliedLevel = game.MythicRiftManager.UseHighestUnlockedLaunchRiftLevel(player.DatabaseUniqueId);
                return $"Cosmic Rift launch level set to your highest unlocked level: {appliedLevel}.";
            }

            if (TryParsePositiveInt(requestedLevelText, out int requestedLevel) == false)
                return "Invalid Rift level. Use a positive number or `max`.";

            if (game.MythicRiftManager.TrySetPreferredLaunchRiftLevel(player.DatabaseUniqueId, requestedLevel, out int appliedLaunchLevel, out string errorMessage) == false)
                return errorMessage;

            return $"Cosmic Rift launch level set to {appliedLaunchLevel}. Highest unlocked: {unlockedLevel}.";
        }

        [Command("abandon")]
        [CommandDescription("Abandons the invoking player's active Cosmic Rift run and returns online participants to the Danger Room hub.")]
        [CommandUsage("rift abandon")]
        [CommandUserLevel(AccountUserLevel.User)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Abandon(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            if (game == null || player == null)
                return "Game or player not found.";

            MythicRiftRunState runState = game.MythicRiftManager.GetInProgressRunForPlayer(player.DatabaseUniqueId);
            if (runState == null)
                return "No active Cosmic Rift run to abandon.";

            ulong runId = runState.Config.RunId;
            string contentName = runState.Config.Content.DisplayName;
            int riftLevel = runState.Config.RiftLevel;

            if (game.MythicRiftManager.MarkRunAborted(runId, game.CurrentTime) == false)
                return $"Failed to abandon Cosmic Rift run {runId}.";

            int teleportedPlayerCount = game.MythicRiftManager.ReturnRunParticipantsToDangerRoomHub(runState, player);
            game.MythicRiftManager.RemoveRun(runId);
            return $"Cosmic Rift abandoned: runId={runId} | map={contentName} | level={riftLevel} | returnedPlayers={teleportedPlayerCount}. You can start another Rift.";
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

        [Command("objectives")]
        [CommandDescription("Diagnoses native mission and objective tracker state in the invoking player's current region.")]
        [CommandUsage("rift objectives")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Objectives(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection?.Game;
            Player player = playerConnection?.Player;
            Region region = player?.GetRegion();
            if (game == null || player == null || region == null)
                return "Game, player, or current region not found.";

            MythicRiftRunState runState = game.MythicRiftManager.GetInProgressRunForPlayer(player.DatabaseUniqueId);
            List<string> lines = new()
            {
                $"currentRegion={region.PrototypeDataRef.GetNameFormatted()} | regionId=0x{region.Id:X}",
                runState == null
                    ? "activeRift=none"
                    : $"activeRift={runState.Config.RunId} | status={runState.Status} | riftMission={runState.Config.MissionProtoRef.GetNameFormatted()} | map={runState.Config.Content.DisplayName}"
            };

            AppendMissionManagerDiagnostics(lines, "regionMissionManager", region.MissionManager, runState?.Config.MissionProtoRef ?? PrototypeId.Invalid);
            AppendMissionManagerDiagnostics(lines, "playerMissionManager", player.MissionManager, runState?.Config.MissionProtoRef ?? PrototypeId.Invalid);

            string uiDump = region.UIDataProvider?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(uiDump))
            {
                lines.Add("uiDataProvider=empty");
            }
            else
            {
                lines.Add("uiDataProvider:");
                foreach (string line in uiDump.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Take(80))
                    lines.Add(line);
            }

            CommandHelper.SendMessages(client, lines);
            return string.Empty;
        }

        private static void AppendMissionManagerDiagnostics(List<string> lines, string label, MissionManager missionManager, PrototypeId riftMissionRef)
        {
            if (lines == null)
                return;

            if (missionManager == null)
            {
                lines.Add($"{label}=null");
                return;
            }

            lines.Add($"{label}: activeMissions={missionManager.ActiveMissions.Count}");
            foreach (PrototypeId missionRef in missionManager.ActiveMissions.Take(40))
            {
                Mission mission = missionManager.FindMissionByDataRef(missionRef);
                if (mission == null)
                {
                    lines.Add($"{label}.mission={missionRef.GetNameFormatted()} | missing=true");
                    continue;
                }

                MissionPrototype missionProto = mission.Prototype;
                lines.Add(
                    $"{label}.mission={mission.PrototypeName} | state={mission.State} | suspended={mission.IsSuspended} | open={mission.IsOpenMission} | regionEvent={mission.IsRegionEventMission} | daily={mission.IsDailyMission} | hasClientInterest={missionProto?.HasClientInterest} | showTracker={missionProto?.ShowInMissionTracker} | riftMission={mission.PrototypeDataRef == riftMissionRef}");

                foreach (MissionObjective objective in mission.Objectives.Take(12))
                {
                    MissionObjectivePrototype objectiveProto = objective?.Prototype;
                    if (objectiveProto == null)
                        continue;

                    lines.Add(
                        $"{label}.objective[{objective.PrototypeIndex}] state={objective.State} | widget={objectiveProto.MetaGameWidget.GetNameFormatted()} | failWidget={objectiveProto.MetaGameWidgetFail.GetNameFormatted()} | name={objectiveProto.Name}");
                }
            }
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
