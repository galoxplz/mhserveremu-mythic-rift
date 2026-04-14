using System.Globalization;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
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
                lines.Add($"{content.Id}: {content.DisplayName} | region={content.RegionProtoRef.GetNameFormatted()} | boss={content.BossProtoRef.GetNameFormatted()}");

            CommandHelper.SendMessages(client, lines);
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
                $"Rift level {snapshot.RiftLevel} | requested players={requestedPlayers} | effective players={snapshot.EffectivePlayerCount} | HP x{snapshot.HealthMultiplier:F3} | damage x{snapshot.DamageMultiplier:F3}");
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
                $"boss={config.BossProtoRef.GetNameFormatted()}",
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

            MythicRiftRunState runState = game.MythicRiftManager.RequestRun(
                player,
                riftLevel,
                killQuota,
                TimeSpan.FromMinutes(timeLimitMinutes),
                out string errorMessage);

            if (runState == null)
                return string.IsNullOrWhiteSpace(errorMessage) ? "Failed to request Mythic Rift run." : errorMessage;

            CommandHelper.SendMessages(client, BuildRunLines(runState, game.CurrentTime, includeResolvedRefs: true));
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

            MythicRiftRunState runState = game.MythicRiftManager.RequestFixedRun(
                player,
                contentId,
                riftLevel,
                killQuota,
                TimeSpan.FromMinutes(timeLimitMinutes),
                out string errorMessage);

            if (runState == null)
                return string.IsNullOrWhiteSpace(errorMessage) ? "Failed to request Mythic Rift run." : errorMessage;

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
                    $"runId={runState.Config.RunId} | status={runState.Status} | content={runState.Config.Content.DisplayName} | level={runState.Config.RiftLevel} | kills={runState.CurrentKillCount}/{runState.Config.KillQuota}");
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
                $"level={runState.Config.RiftLevel} | requestedPlayers={runState.Config.RequestedPlayerCount} | effectivePlayers={runState.Config.EffectivePlayerCount}",
                $"killQuota={runState.CurrentKillCount}/{runState.Config.KillQuota} | bossUnlocked={runState.BossUnlocked} | rewardsGranted={runState.RewardsGranted}",
                $"timeLimit={runState.Config.TimeLimit.TotalMinutes:0} min | remaining={runState.GetTimeRemaining(currentTime).TotalMinutes:0.##} min | HP x{runState.Config.Difficulty.HealthMultiplier:F3} | damage x{runState.Config.Difficulty.DamageMultiplier:F3}",
                $"regionId=0x{runState.RegionId:X} | bossEntityId=0x{runState.BossEntityId:X}",
                $"participants={runState.ParticipantCount} | rewardedPlayers={runState.RewardedPlayerCount}",
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
                lines.Add($"mission={runState.Config.MissionProtoRef.GetNameFormatted()}");
                lines.Add($"boss={runState.Config.BossProtoRef.GetNameFormatted()}");
                lines.Add($"bossLoot={runState.Config.BossLootTableProtoRef.GetNameFormatted()}");
            }

            return lines;
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
