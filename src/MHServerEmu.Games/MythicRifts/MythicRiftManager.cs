using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Parties;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private const float TimedSuccessBonusRarityPct = 0.10f;
        private const float TimedSuccessBonusSpecialPct = 0.15f;
        private static readonly TimeSpan ParticipantDisconnectAbortGracePeriod = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan PendingRunBindGracePeriod = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan CompletedRunRetention = TimeSpan.FromMinutes(5);
        private static readonly MythicRiftContentDefinition[] DefaultContentDefinitions =
        {
            new(
                "shocker",
                "Shocker Terminal",
                45,
                "Regions/EndGame/Terminals/Green/ShockerSubway/DailyGShockerSubwayRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G01ShockerSubwayDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD01GShocker.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/AbandonedSubway/ShockerTerminalLoot.prototype"),
            new(
                "doctor-octopus",
                "Doctor Octopus Terminal",
                50,
                "Regions/EndGame/Terminals/Green/KingpinsWarehouse/DailyGKPWarehouseRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G02DoctorOctopusDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD02GDoctorOctopus.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/KingpinsWarehouse/DrOctopusTerminalLoot.prototype"),
            new(
                "taskmaster",
                "Taskmaster Terminal",
                50,
                "Regions/EndGame/Terminals/Green/Taskmaster/DailyGTaskmasterRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G03TaskmasterDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD03GTaskmaster.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/TaskmasterInstitute/TaskmasterTerminalLoot.prototype"),
            new(
                "hood",
                "Hood Terminal",
                55,
                "Regions/EndGame/Terminals/Green/HoodsShip/DailyGHoodsShipRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G04HoodDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD04GHood.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/HoodsHideout/HoodTerminalLoot.prototype"),
            new(
                "magneto",
                "Magneto Terminal",
                60,
                "Regions/EndGame/Terminals/Green/MagnetoBunker/DailyGStrykerBunkerRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G05MagnetoDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD05GMagneto.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/StrykerCommandBunker/MagnetoTerminalLoot.prototype"),
            new(
                "sinister",
                "Mister Sinister Terminal",
                60,
                "Regions/EndGame/Terminals/Green/SinistersLab/DailyGSinisterLabRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G06MisterSinisterDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD06GMrSinister.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/SinisterLab/MisterSinisterTerminalLoot.prototype"),
            new(
                "modok",
                "MODOK Terminal",
                60,
                "Regions/EndGame/Terminals/Green/AIMFacility/DailyGAIMFacilityRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G07MODOKDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD07GMODOK.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/AIMWeaponFacility/ModokTerminalLoot.prototype"),
            new(
                "mandarin",
                "Mandarin Terminal",
                65,
                "Regions/EndGame/Terminals/Green/HYDRAIsland/DailyGHYDRAIslandRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G08MandarinDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD08GMandarin.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/HydraIsland/MandarinTerminalLoot.prototype"),
            new(
                "kingpin",
                "Kingpin Terminal",
                65,
                "Regions/EndGame/Terminals/Green/FiskTower/DailyGFiskTowerRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G10FiskTowerDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD10GKingpin.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/FiskTower/KingpinLTerminalLoot.prototype"),
            new(
                "ultron",
                "Ultron Terminal",
                70,
                "Regions/EndGame/Terminals/Green/TimesSquare/DailyGTimesSquareRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G14UltronDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD14GUltronTerminal.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/TimesSquare/UltronTerminalLoot.prototype"),
        };

        private readonly List<MythicRiftContentEntry> _contentPool = new();
        private readonly Dictionary<ulong, MythicRiftRunState> _activeRuns = new();
        private readonly Dictionary<ulong, Event<EntityDeadGameEvent>.Action> _regionEntityDeadActions = new();
        private readonly Dictionary<ulong, int> _highestUnlockedRiftLevelByPlayer = new();
        private ulong _nextRunId = 1;

        public Game Game { get; }

        public MythicRiftManager(Game game)
        {
            Game = game;
            RegisterDefaultContent();
        }

        public IReadOnlyList<MythicRiftContentEntry> ContentPool => _contentPool;
        public IReadOnlyList<MythicRiftContentEntry> RandomEligibleContentPool => _contentPool.Where(entry => entry.RandomEligible).ToList();
        public IReadOnlyCollection<MythicRiftRunState> ActiveRuns => _activeRuns.Values;

        public MythicRiftDifficultySnapshot GetDifficultySnapshot(int riftLevel, int requestedPlayerCount)
        {
            return MythicRiftScaling.BuildSnapshot(riftLevel, requestedPlayerCount);
        }

        public int GetHighestUnlockedRiftLevel(ulong playerDbId)
        {
            if (playerDbId == 0)
                return 1;

            Player onlinePlayer = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
            if (onlinePlayer != null)
            {
                int persistentLevel = onlinePlayer.MythicRiftHighestUnlockedLevel;
                if (_highestUnlockedRiftLevelByPlayer.TryGetValue(playerDbId, out int cachedUnlockedLevel))
                    return Math.Max(Math.Max(cachedUnlockedLevel, persistentLevel), 1);

                return Math.Max(persistentLevel, 1);
            }

            return _highestUnlockedRiftLevelByPlayer.TryGetValue(playerDbId, out int unlockedLevel)
                ? Math.Max(unlockedLevel, 1)
                : 1;
        }

        public bool CanAccessRiftLevel(ulong playerDbId, int riftLevel)
        {
            if (riftLevel <= 0)
                return false;

            return riftLevel <= GetHighestUnlockedRiftLevel(playerDbId);
        }

        public int SetHighestUnlockedRiftLevel(ulong playerDbId, int unlockedLevel)
        {
            if (playerDbId == 0)
                return 1;

            int normalizedLevel = Math.Max(unlockedLevel, 1);
            _highestUnlockedRiftLevelByPlayer[playerDbId] = normalizedLevel;
            SyncOnlinePlayerRiftLevel(playerDbId, normalizedLevel);
            return normalizedLevel;
        }

        public int GrantNextRiftLevel(ulong playerDbId, int completedLevel)
        {
            if (playerDbId == 0)
                return 1;

            int nextUnlockedLevel = Math.Max(completedLevel + 1, 1);
            int currentUnlockedLevel = GetHighestUnlockedRiftLevel(playerDbId);
            if (nextUnlockedLevel <= currentUnlockedLevel)
                return currentUnlockedLevel;

            _highestUnlockedRiftLevelByPlayer[playerDbId] = nextUnlockedLevel;
            SyncOnlinePlayerRiftLevel(playerDbId, nextUnlockedLevel);
            return nextUnlockedLevel;
        }

        public MythicRiftRunConfig CreateDebugRunConfig(string contentId, int riftLevel, int requestedPlayerCount, int killQuota, TimeSpan timeLimit)
        {
            MythicRiftContentEntry content = GetContent(contentId);
            if (content == null)
                return null;

            return CreateRunConfig(content, content, riftLevel, requestedPlayerCount, killQuota, timeLimit);
        }

        public MythicRiftRunConfig CreateDebugRunConfig(string contentId, string bossContentId, int riftLevel, int requestedPlayerCount, int killQuota, TimeSpan timeLimit)
        {
            MythicRiftContentEntry content = GetContent(contentId);
            MythicRiftContentEntry bossContent = GetContent(bossContentId);
            if (content == null || bossContent == null)
                return null;

            return CreateRunConfig(content, bossContent, riftLevel, requestedPlayerCount, killQuota, timeLimit);
        }

        public MythicRiftRunConfig CreateRandomDebugRunConfig(int riftLevel, int requestedPlayerCount, int killQuota, TimeSpan timeLimit)
        {
            MythicRiftContentEntry content = SelectRandomMapContent();
            MythicRiftContentEntry bossContent = SelectRandomBossContent(content);
            if (content == null || bossContent == null)
                return null;

            return CreateRunConfig(content, bossContent, riftLevel, requestedPlayerCount, killQuota, timeLimit);
        }

        public MythicRiftRunState CreateRunState(MythicRiftRunConfig config)
        {
            if (config == null || config.IsValid == false)
                return null;

            return new MythicRiftRunState(config);
        }

        public MythicRiftRunState CreateDebugRun(string contentId, int riftLevel, int requestedPlayerCount, int killQuota, TimeSpan timeLimit)
        {
            MythicRiftRunConfig config = CreateDebugRunConfig(contentId, riftLevel, requestedPlayerCount, killQuota, timeLimit);
            return RegisterRun(config);
        }

        public MythicRiftRunState CreateDebugRun(string contentId, string bossContentId, int riftLevel, int requestedPlayerCount, int killQuota, TimeSpan timeLimit)
        {
            MythicRiftRunConfig config = CreateDebugRunConfig(contentId, bossContentId, riftLevel, requestedPlayerCount, killQuota, timeLimit);
            return RegisterRun(config);
        }

        public MythicRiftRunState CreateRandomDebugRun(int riftLevel, int requestedPlayerCount, int killQuota, TimeSpan timeLimit)
        {
            MythicRiftRunConfig config = CreateRandomDebugRunConfig(riftLevel, requestedPlayerCount, killQuota, timeLimit);
            return RegisterRun(config);
        }

        public MythicRiftRunState RequestRun(Player player, int riftLevel, int killQuota, TimeSpan timeLimit, out string errorMessage)
        {
            return RequestRunInternal(player, null, riftLevel, killQuota, timeLimit, useRandomContent: true, out errorMessage);
        }

        public MythicRiftRunState RequestFixedRun(Player player, string contentId, int riftLevel, int killQuota, TimeSpan timeLimit, out string errorMessage)
        {
            return RequestRunInternal(player, contentId, riftLevel, killQuota, timeLimit, useRandomContent: false, out errorMessage);
        }

        public MythicRiftRunState GetRun(ulong runId)
        {
            if (runId == 0)
                return null;

            return _activeRuns.TryGetValue(runId, out MythicRiftRunState runState) ? runState : null;
        }

        public MythicRiftRunState GetInProgressRunForPlayer(ulong playerDbId)
        {
            if (playerDbId == 0)
                return null;

            return _activeRuns.Values.FirstOrDefault(runState =>
                runState.IsInProgress &&
                runState.ParticipantPlayerDbIds.Contains(playerDbId));
        }

        public bool RemoveRun(ulong runId)
        {
            if (runId == 0)
                return false;

            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            ulong regionId = runState.RegionId;
            TryRestoreRegionDifficultyScaling(runState);
            bool removed = _activeRuns.Remove(runId);

            if (removed && regionId != 0)
                CleanupRegionListener(regionId);

            return removed;
        }

        public bool StartRun(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            runState.Start(currentTime);
            if (runState.Status == MythicRiftRunStatus.Active)
                NotifyRunStarted(runState);

            return runState.Status == MythicRiftRunStatus.Active;
        }

        public bool AddKills(ulong runId, int killCount)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null || killCount <= 0)
                return false;

            runState.AddKills(killCount);
            return true;
        }

        public bool MarkRunSuccess(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            return CompleteRunSuccess(runState, currentTime);
        }

        public bool MarkRunFailed(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            return CompleteRunFailure(runState, currentTime, "The timer expired. Base boss rewards only.");
        }

        public bool MarkRunAborted(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            return AbortRun(runState, currentTime, "The run was aborted.");
        }

        public bool AbortRunWithReason(ulong runId, TimeSpan currentTime, string reason)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            return AbortRun(runState, currentTime, reason);
        }

        public bool GrantRewardsToPlayer(ulong runId, Player player)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null || player == null)
                return false;

            if (runState.Status is not (MythicRiftRunStatus.Success or MythicRiftRunStatus.Failed))
                return false;

            runState.RegisterParticipant(player.DatabaseUniqueId);
            if (runState.HasRewardForPlayer(player.DatabaseUniqueId))
                return false;

            MythicRiftRewardOutcome rewardOutcome = runState.RewardOutcome ?? ResolveRewardOutcome(runState);
            if (rewardOutcome == null || rewardOutcome.HasBossLootTable == false)
                return false;

            Avatar avatar = player.CurrentAvatar;
            if (avatar == null)
                return false;

            PropertyId rarityPropertyId = new(PropertyEnum.LootBonusRarityPct);
            PropertyId specialPropertyId = new(PropertyEnum.LootBonusSpecialPct);

            float originalRarity = avatar.Properties[PropertyEnum.LootBonusRarityPct];
            float originalSpecial = avatar.Properties[PropertyEnum.LootBonusSpecialPct];
            bool hadRarityProperty = avatar.Properties.HasProperty(PropertyEnum.LootBonusRarityPct);
            bool hadSpecialProperty = avatar.Properties.HasProperty(PropertyEnum.LootBonusSpecialPct);

            try
            {
                if (rewardOutcome.BonusRarityPct > 0f)
                    avatar.Properties.AdjustProperty(rewardOutcome.BonusRarityPct, rarityPropertyId);

                if (rewardOutcome.BonusSpecialPct > 0f)
                    avatar.Properties.AdjustProperty(rewardOutcome.BonusSpecialPct, specialPropertyId);

                using LootInputSettings inputSettings = MHServerEmu.Core.Memory.ObjectPoolManager.Instance.Get<LootInputSettings>();
                inputSettings.Initialize(LootContext.Drop, player, avatar);
                Game.LootManager.GiveLootFromTable(rewardOutcome.BossLootTableProtoRef, inputSettings);

                runState.MarkRewardGrantedToPlayer(player.DatabaseUniqueId);
                Logger.Info($"Mythic Rift run {runState.Config.RunId} granted rewards to player {player}.");
                return true;
            }
            finally
            {
                if (hadRarityProperty)
                    avatar.Properties[PropertyEnum.LootBonusRarityPct] = originalRarity;
                else
                    avatar.Properties.RemoveProperty(rarityPropertyId);

                if (hadSpecialProperty)
                    avatar.Properties[PropertyEnum.LootBonusSpecialPct] = originalSpecial;
                else
                    avatar.Properties.RemoveProperty(specialPropertyId);
            }
        }

        public int GrantRewardsToRunPlayers(ulong runId)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null || runState.Status is not (MythicRiftRunStatus.Success or MythicRiftRunStatus.Failed))
                return 0;

            HashSet<ulong> recipientDbIds = new(runState.ParticipantPlayerDbIds);
            if (runState.RegionId != 0)
            {
                Region region = Game.RegionManager.GetRegion(runState.RegionId);
                if (region != null)
                {
                    foreach (Player regionPlayer in new PlayerIterator(region))
                        recipientDbIds.Add(regionPlayer.DatabaseUniqueId);
                }
            }

            int grantedCount = 0;
            foreach (ulong playerDbId in recipientDbIds)
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
                if (player == null)
                    continue;

                if (GrantRewardsToPlayer(runId, player))
                    grantedCount++;
            }

            return grantedCount;
        }

        public bool EvaluateRunTimer(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null || runState.HasExpired(currentTime) == false)
                return false;

            return CompleteRunFailure(runState, currentTime, "The timer expired. Base boss rewards only.");
        }

        public bool AttachRunToRegion(ulong runId, Region region)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null || region == null)
                return false;

            runState.AttachRegion(region.Id);
            RegisterRegionPlayersAsParticipants(runState, region);
            ApplyRunDifficultyToRegion(runState, region);
            EnsureRegionListener(region);
            return true;
        }

        public void Update(TimeSpan currentTime)
        {
            List<ulong> runsToRemove = null;

            foreach (MythicRiftRunState runState in _activeRuns.Values)
            {
                RegisterBoundRegionPlayersAsParticipants(runState);
                UpdateParticipantPresence(runState, currentTime);
                TryAutoBindAndStartPendingRun(runState, currentTime);

                if (runState.HasExpired(currentTime))
                {
                    CompleteRunFailure(runState, currentTime, "The timer expired. Base boss rewards only.");
                }

                if (TryAbortRunForDisconnectedParticipants(runState, currentTime))
                    continue;

                if (TryAbortStalePendingRun(runState, currentTime))
                    continue;

                if (ShouldAutoRemoveRun(runState, currentTime) == false)
                    continue;

                runsToRemove ??= new();
                runsToRemove.Add(runState.Config.RunId);
            }

            if (runsToRemove == null)
                return;

            foreach (ulong runId in runsToRemove)
            {
                if (RemoveRun(runId))
                    Logger.Info($"Mythic Rift run {runId} was removed automatically after retention cleanup.");
            }
        }

        public MythicRiftContentEntry GetContent(string contentId)
        {
            if (string.IsNullOrWhiteSpace(contentId))
                return null;

            return _contentPool.FirstOrDefault(entry => entry.Id.Equals(contentId, StringComparison.OrdinalIgnoreCase));
        }

        public void RegisterContent(MythicRiftContentEntry content)
        {
            if (content == null || content.IsValid == false)
            {
                Logger.Warn("RegisterContent(): invalid mythic rift content entry");
                return;
            }

            if (GetContent(content.Id) != null)
            {
                Logger.Warn($"RegisterContent(): duplicate mythic rift content id={content.Id}");
                return;
            }

            _contentPool.Add(content);
        }

        private MythicRiftRunState RegisterRun(MythicRiftRunConfig config)
        {
            MythicRiftRunState runState = CreateRunState(config);
            if (runState == null)
                return null;

            runState.SetRegisteredAt(Game.CurrentTime);
            _activeRuns[runState.Config.RunId] = runState;
            return runState;
        }

        private MythicRiftRunState RequestRunInternal(Player player, string contentId, int riftLevel, int killQuota, TimeSpan timeLimit, bool useRandomContent, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (player == null)
            {
                errorMessage = "Player not found.";
                return null;
            }

            if (riftLevel <= 0)
            {
                errorMessage = "Invalid Rift level.";
                return null;
            }

            Party party = player.GetParty();
            if (party != null && party.NumMembers > 1 && player.IsPartyLeader() == false)
            {
                errorMessage = "Only the party leader can request a group Mythic Rift run.";
                return null;
            }

            if (CanAccessRiftLevel(player.DatabaseUniqueId, riftLevel) == false)
            {
                int unlockedLevel = GetHighestUnlockedRiftLevel(player.DatabaseUniqueId);
                errorMessage = $"Requested Rift level {riftLevel} is locked. Highest unlocked level: {unlockedLevel}.";
                return null;
            }

            int requestedPlayerCount = ResolveRequestedPlayerCount(player, party);
            if (TryFindInProgressRunConflict(player, party, out MythicRiftRunState conflictingRun))
            {
                errorMessage = $"A Mythic Rift run is already in progress for this player or party (runId={conflictingRun.Config.RunId}, status={conflictingRun.Status}).";
                return null;
            }

            MythicRiftRunState runState = useRandomContent
                ? CreateRandomDebugRun(riftLevel, requestedPlayerCount, killQuota, timeLimit)
                : CreateDebugRun(contentId, riftLevel, requestedPlayerCount, killQuota, timeLimit);

            if (runState == null)
            {
                errorMessage = useRandomContent
                    ? "Failed to create Mythic Rift run."
                    : $"Failed to create Mythic Rift run for content id: {contentId}";
                return null;
            }

            RegisterInitialParticipants(runState, player, party);
            Logger.Info($"Mythic Rift run {runState.Config.RunId} requested by playerDbId=0x{player.DatabaseUniqueId:X} at level {riftLevel}.");
            return runState;
        }

        private MythicRiftContentEntry SelectRandomMapContent()
        {
            List<MythicRiftContentEntry> eligibleContent = _contentPool.Where(entry => entry.RandomEligible).ToList();
            if (eligibleContent.Count == 0)
                return null;

            int index = Game.Random.Next(0, eligibleContent.Count);
            return eligibleContent[index];
        }

        private MythicRiftContentEntry SelectRandomBossContent(MythicRiftContentEntry mapContent)
        {
            List<MythicRiftContentEntry> eligibleContent = _contentPool.Where(entry => entry.RandomEligible).ToList();
            if (eligibleContent.Count == 0)
                return null;

            if (mapContent != null && eligibleContent.Count > 1)
            {
                List<MythicRiftContentEntry> alternateBossContent = eligibleContent
                    .Where(entry => string.Equals(entry.Id, mapContent.Id, StringComparison.OrdinalIgnoreCase) == false)
                    .ToList();

                if (alternateBossContent.Count > 0)
                    eligibleContent = alternateBossContent;
            }

            int index = Game.Random.Next(0, eligibleContent.Count);
            return eligibleContent[index];
        }

        private void EnsureRegionListener(Region region)
        {
            if (region == null || _regionEntityDeadActions.ContainsKey(region.Id))
                return;

            Event<EntityDeadGameEvent>.Action action = (in EntityDeadGameEvent evt) => OnRegionEntityDead(region.Id, evt);
            region.EntityDeadEvent.AddActionBack(action);
            _regionEntityDeadActions[region.Id] = action;
        }

        private void CleanupRegionListener(ulong regionId)
        {
            if (regionId == 0)
                return;

            bool regionStillUsed = _activeRuns.Values.Any(run => run.RegionId == regionId);
            if (regionStillUsed)
                return;

            if (_regionEntityDeadActions.TryGetValue(regionId, out Event<EntityDeadGameEvent>.Action action) == false)
                return;

            Region region = Game.RegionManager.GetRegion(regionId);
            region?.EntityDeadEvent.RemoveAction(action);
            _regionEntityDeadActions.Remove(regionId);
        }

        private void ApplyRunDifficultyToRegion(MythicRiftRunState runState, Region region)
        {
            if (runState == null || region == null || runState.RegionDifficultyScalingApplied)
                return;

            float currentPlayerToMobDamageMultiplier = region.Properties[PropertyEnum.DamageRegionPlayerToMob];
            float currentMobToPlayerDamageMultiplier = region.Properties[PropertyEnum.DamageRegionMobToPlayer];

            runState.CaptureRegionDifficultyScaling(currentPlayerToMobDamageMultiplier, currentMobToPlayerDamageMultiplier);

            float effectiveHealthMultiplier = Math.Max(runState.Config.Difficulty.HealthMultiplier, 0.01f);
            float effectiveDamageMultiplier = Math.Max(runState.Config.Difficulty.DamageMultiplier, 0.01f);

            region.Properties[PropertyEnum.DamageRegionPlayerToMob] = currentPlayerToMobDamageMultiplier / effectiveHealthMultiplier;
            region.Properties[PropertyEnum.DamageRegionMobToPlayer] = currentMobToPlayerDamageMultiplier * effectiveDamageMultiplier;

            Logger.Info(
                $"Mythic Rift run {runState.Config.RunId} applied region difficulty scaling: " +
                $"playerToMob {currentPlayerToMobDamageMultiplier:F4}->{region.Properties[PropertyEnum.DamageRegionPlayerToMob]:F4}, " +
                $"mobToPlayer {currentMobToPlayerDamageMultiplier:F4}->{region.Properties[PropertyEnum.DamageRegionMobToPlayer]:F4}, " +
                $"hpMultiplier={runState.Config.Difficulty.HealthMultiplier:F4}, damageMultiplier={runState.Config.Difficulty.DamageMultiplier:F4}.");
        }

        private void TryRestoreRegionDifficultyScaling(MythicRiftRunState runState)
        {
            if (runState == null || runState.RegionDifficultyScalingApplied == false || runState.RegionId == 0)
                return;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region != null)
            {
                region.Properties[PropertyEnum.DamageRegionPlayerToMob] = runState.RegionPlayerToMobDamageMultiplierBeforeScaling;
                region.Properties[PropertyEnum.DamageRegionMobToPlayer] = runState.RegionMobToPlayerDamageMultiplierBeforeScaling;

                Logger.Info(
                    $"Mythic Rift run {runState.Config.RunId} restored region difficulty scaling: " +
                    $"playerToMob={runState.RegionPlayerToMobDamageMultiplierBeforeScaling:F4}, " +
                    $"mobToPlayer={runState.RegionMobToPlayerDamageMultiplierBeforeScaling:F4}.");
            }

            runState.ClearRegionDifficultyScaling();
        }

        private void OnRegionEntityDead(ulong regionId, in EntityDeadGameEvent evt)
        {
            foreach (MythicRiftRunState runState in _activeRuns.Values)
            {
                if (runState.RegionId != regionId || runState.Status != MythicRiftRunStatus.Active)
                    continue;

                RegisterParticipantsFromEvent(runState, evt);

                if (IsExpectedBossKill(runState, evt))
                {
                    runState.AttachBoss(evt.Defender.Id);

                    if (runState.BossUnlocked)
                    {
                        CompleteRunSuccess(runState, Game.CurrentTime);
                        Logger.Info($"Mythic Rift run {runState.Config.RunId} completed by defeating {evt.Defender.PrototypeName}.");
                    }

                    continue;
                }

                bool shouldCountKill = ShouldCountKill(evt);
                if (runState.BossUnlocked)
                {
                    if (runState.BossEntityId == 0 && shouldCountKill)
                    {
                        if (TrySpawnConfiguredBoss(runState, evt.Defender))
                        {
                            NotifyBossUnlocked(runState);
                        }
                        else
                        {
                            Logger.Warn($"Mythic Rift run {runState.Config.RunId} failed to retry spawn for boss {runState.Config.BossProtoRef.GetNameFormatted() ?? "unknown"} after quota unlock.");
                        }
                    }

                    continue;
                }

                if (shouldCountKill == false)
                    continue;

                int previousKillCount = runState.CurrentKillCount;
                runState.AddKills(1);

                if (runState.BossUnlocked && previousKillCount < runState.Config.KillQuota)
                {
                    if (TrySpawnConfiguredBoss(runState, evt.Defender))
                    {
                        NotifyBossUnlocked(runState);
                    }
                    else
                    {
                        Logger.Warn($"Mythic Rift run {runState.Config.RunId} unlocked boss {runState.Config.BossProtoRef.GetNameFormatted() ?? "unknown"} but the initial spawn attempt failed.");
                    }
                    Logger.Info($"Mythic Rift run {runState.Config.RunId} unlocked its boss after reaching {runState.CurrentKillCount}/{runState.Config.KillQuota} kills.");
                }
            }
        }

        private static bool IsExpectedBossKill(MythicRiftRunState runState, in EntityDeadGameEvent evt)
        {
            if (runState == null || evt.Defender == null)
                return false;

            if (runState.BossEntityId != 0 && runState.BossEntityId == evt.Defender.Id)
                return true;

            if (runState.BossUnlocked == false)
                return false;

            PrototypeId expectedBossRef = runState.Config.BossProtoRef;
            if (expectedBossRef == PrototypeId.Invalid)
                return false;

            return evt.Defender.IsAPrototype(expectedBossRef);
        }

        private static void RegisterParticipantsFromEvent(MythicRiftRunState runState, in EntityDeadGameEvent evt)
        {
            if (runState == null)
                return;

            if (evt.Killer != null)
                runState.RegisterParticipant(evt.Killer.DatabaseUniqueId);

            Player attackerOwner = evt.Attacker?.GetOwnerOfType<Player>();
            if (attackerOwner != null)
                runState.RegisterParticipant(attackerOwner.DatabaseUniqueId);

            if (evt.Defender?.TagPlayers == null)
                return;

            foreach (Player taggedPlayer in evt.Defender.TagPlayers.GetPlayers())
                runState.RegisterParticipant(taggedPlayer.DatabaseUniqueId);
        }

        private static void RegisterRegionPlayersAsParticipants(MythicRiftRunState runState, Region region)
        {
            if (runState == null || region == null)
                return;

            foreach (Player player in new PlayerIterator(region))
                runState.RegisterParticipant(player.DatabaseUniqueId);
        }

        private static int ResolveRequestedPlayerCount(Player player, Party party)
        {
            if (party != null)
                return Math.Clamp(party.NumMembers, 1, 5);

            return player != null ? 1 : 0;
        }

        private static void RegisterInitialParticipants(MythicRiftRunState runState, Player requester, Party party)
        {
            if (runState == null || requester == null)
                return;

            runState.RegisterParticipant(requester.DatabaseUniqueId);

            if (party == null)
                return;

            foreach (var kvp in party)
                runState.RegisterParticipant(kvp.Value.PlayerDbId);
        }

        private static MythicRiftRewardOutcome ResolveRewardOutcome(MythicRiftRunState runState)
        {
            if (runState == null)
                return null;

            bool timedSuccess = runState.Status == MythicRiftRunStatus.Success;

            MythicRiftRewardOutcome rewardOutcome = new()
            {
                BossLootTableProtoRef = runState.Config.BossLootTableProtoRef,
                TimedSuccessBonusApplied = timedSuccess,
                BonusRarityPct = timedSuccess ? TimedSuccessBonusRarityPct : 0f,
                BonusSpecialPct = timedSuccess ? TimedSuccessBonusSpecialPct : 0f
            };

            runState.SetRewardOutcome(rewardOutcome);
            return rewardOutcome;
        }

        private void GrantProgressionForSuccessfulRun(MythicRiftRunState runState)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Success)
                return;

            HashSet<ulong> recipientDbIds = new(runState.ParticipantPlayerDbIds);
            if (runState.RegionId != 0)
            {
                Region region = Game.RegionManager.GetRegion(runState.RegionId);
                if (region != null)
                {
                    foreach (Player player in new PlayerIterator(region))
                        recipientDbIds.Add(player.DatabaseUniqueId);
                }
            }

            foreach (ulong playerDbId in recipientDbIds)
            {
                int unlockedLevel = GrantNextRiftLevel(playerDbId, runState.Config.RiftLevel);
                Logger.Info($"Mythic Rift run {runState.Config.RunId} unlocked Rift level {unlockedLevel} for playerDbId=0x{playerDbId:X}.");
            }
        }

        private static bool ShouldCountKill(in EntityDeadGameEvent evt)
        {
            if (evt.Defender == null)
                return false;

            if (evt.Defender is Avatar)
                return false;

            if (evt.Defender is Agent == false)
                return false;

            if (evt.Defender.IsHostileToPlayers() == false)
                return false;

            if (evt.Defender.WorldEntityPrototype?.MissionEntityDeathCredit == false)
                return false;

            if (evt.Killer != null)
                return true;

            if (evt.Attacker?.GetOwnerOfType<Player>() != null)
                return true;

            return evt.Defender.TagPlayers.HasTags;
        }

        private MythicRiftRunConfig CreateRunConfig(MythicRiftContentEntry content, MythicRiftContentEntry bossContent, int riftLevel, int requestedPlayerCount, int killQuota, TimeSpan timeLimit)
        {
            if (content == null || bossContent == null)
                return null;

            MythicRiftDifficultySnapshot difficulty = GetDifficultySnapshot(riftLevel, requestedPlayerCount);

            return new MythicRiftRunConfig
            {
                RunId = _nextRunId++,
                RiftLevel = Math.Max(riftLevel, 1),
                Content = content,
                BossContent = bossContent,
                RequestedPlayerCount = Math.Max(requestedPlayerCount, 1),
                EffectivePlayerCount = difficulty.EffectivePlayerCount,
                KillQuota = ResolveKillQuota(content, killQuota),
                TimeLimit = timeLimit <= TimeSpan.Zero ? TimeSpan.FromMinutes(10) : timeLimit,
                RegionProtoRef = content.RegionProtoRef,
                StartTargetProtoRef = content.StartTargetProtoRef,
                MissionProtoRef = content.MissionProtoRef,
                BossProtoRef = bossContent.BossProtoRef,
                BossLootTableProtoRef = bossContent.BossLootTableProtoRef,
                Difficulty = difficulty
            };
        }

        private bool TrySpawnConfiguredBoss(MythicRiftRunState runState, WorldEntity anchorEntity)
        {
            if (runState == null || runState.RegionId == 0 || runState.BossEntityId != 0)
                return false;

            AgentPrototype bossProto = runState.Config.BossProtoRef.As<AgentPrototype>();
            if (bossProto == null)
                return false;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return false;

            Vector3 spawnPosition = anchorEntity?.RegionLocation.Position ?? Vector3.Zero;
            Orientation spawnOrientation = anchorEntity?.RegionLocation.Orientation ?? Orientation.Zero;
            Cell spawnCell = anchorEntity?.Cell ?? region.GetCellAtPosition(spawnPosition);
            if (spawnCell == null)
                return false;

            spawnPosition = RegionLocation.ProjectToFloor(region, spawnPosition);
            if (bossProto.Bounds != null)
                spawnPosition.Z += bossProto.Bounds.GetBoundHalfHeight();

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = bossProto.DataRef;
            settings.Position = spawnPosition;
            settings.Orientation = spawnOrientation;
            settings.RegionId = region.Id;
            settings.Cell = spawnCell;
            settings.IsPopulation = true;

            using PropertyCollection settingsProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            int level = spawnCell.Area.GetCharacterLevel(bossProto);
            settingsProperties[PropertyEnum.CharacterLevel] = level;
            settingsProperties[PropertyEnum.CombatLevel] = level;
            settingsProperties[PropertyEnum.DifficultyTier] = region.DifficultyTierRef;
            settingsProperties[PropertyEnum.Rank] = bossProto.Rank;
            settingsProperties[PropertyEnum.MissionPrototype] = runState.Config.MissionProtoRef;
            settings.Properties = settingsProperties;

            Agent bossAgent = Game.EntityManager.CreateEntity(settings) as Agent;
            if (bossAgent == null)
                return false;

            if (bossProto.ModifiersGuaranteed != null && bossProto.ModifiersGuaranteed.Length > 0)
            {
                foreach (PrototypeId boost in bossProto.ModifiersGuaranteed)
                    bossAgent.Properties[PropertyEnum.EnemyBoost, boost] = true;
            }

            runState.AttachBoss(bossAgent.Id);
            Logger.Info($"Mythic Rift run {runState.Config.RunId} spawned boss {bossAgent.PrototypeName} from boss pool entry {runState.Config.BossContent?.Id ?? "unknown"}.");
            return true;
        }

        private void TryAutoGrantCompletionRewards(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            if (runState.Status is not (MythicRiftRunStatus.Success or MythicRiftRunStatus.Failed))
                return;

            if (runState.RewardsGranted)
                return;

            int grantedCount = GrantRewardsToRunPlayers(runState.Config.RunId);
            Logger.Info($"Mythic Rift run {runState.Config.RunId} auto-granted completion rewards to {grantedCount} player(s).");
        }

        private bool CompleteRunSuccess(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null)
                return false;

            runState.MarkSuccess(currentTime);
            if (runState.Status != MythicRiftRunStatus.Success)
                return false;

            ResolveRewardOutcome(runState);
            GrantProgressionForSuccessfulRun(runState);
            TryAutoGrantCompletionRewards(runState);
            TryRestoreRegionDifficultyScaling(runState);
            NotifyRunCompleted(runState, success: true, "Rift cleared. Next level unlocked.");
            return true;
        }

        private bool CompleteRunFailure(MythicRiftRunState runState, TimeSpan currentTime, string reason)
        {
            if (runState == null)
                return false;

            runState.MarkFailed(currentTime);
            if (runState.Status != MythicRiftRunStatus.Failed)
                return false;

            ResolveRewardOutcome(runState);
            TryAutoGrantCompletionRewards(runState);
            TryRestoreRegionDifficultyScaling(runState);
            NotifyRunCompleted(runState, success: false, reason);
            Logger.Info($"Mythic Rift run {runState.Config.RunId} failed. reason={reason}");
            return true;
        }

        private bool AbortRun(MythicRiftRunState runState, TimeSpan currentTime, string reason)
        {
            if (runState == null)
                return false;

            runState.MarkAborted(currentTime);
            if (runState.Status != MythicRiftRunStatus.Aborted)
                return false;

            TryRestoreRegionDifficultyScaling(runState);
            NotifyRunCompleted(runState, success: false, reason);
            Logger.Info($"Mythic Rift run {runState.Config.RunId} aborted. reason={reason}");
            return true;
        }

        private void TryAutoBindAndStartPendingRun(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Pending || runState.RegionId != 0)
                return;

            foreach (ulong participantPlayerDbId in runState.ParticipantPlayerDbIds)
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(participantPlayerDbId);
                Region region = player?.GetRegion();
                if (region == null)
                    continue;

                if (region.PrototypeDataRef != runState.Config.RegionProtoRef)
                    continue;

                if (AttachRunToRegion(runState.Config.RunId, region) == false)
                    return;

                StartRun(runState.Config.RunId, currentTime);
                Logger.Info($"Mythic Rift run {runState.Config.RunId} auto-bound to region {region.PrototypeName} (0x{region.Id:X}) and started.");
                return;
            }
        }

        private void UpdateParticipantPresence(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.ParticipantCount == 0)
                return;

            foreach (ulong participantPlayerDbId in runState.ParticipantPlayerDbIds)
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(participantPlayerDbId);
                if (player == null)
                    continue;

                runState.TouchParticipantPresence(currentTime);
                return;
            }
        }

        private void RegisterBoundRegionPlayersAsParticipants(MythicRiftRunState runState)
        {
            if (runState == null || runState.RegionId == 0)
                return;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return;

            RegisterRegionPlayersAsParticipants(runState, region);
        }

        private bool TryAbortRunForDisconnectedParticipants(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.IsInProgress == false || runState.ParticipantCount == 0)
                return false;

            TimeSpan timeSinceLastParticipantSeen = currentTime - runState.LastParticipantOnlineAt;
            if (timeSinceLastParticipantSeen < ParticipantDisconnectAbortGracePeriod)
                return false;

            return AbortRun(
                runState,
                currentTime,
                $"Run aborted after all tracked participants were offline for {timeSinceLastParticipantSeen.TotalSeconds:0} seconds.");
        }

        private bool TryAbortStalePendingRun(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Pending || runState.RegionId != 0)
                return false;

            TimeSpan pendingDuration = currentTime - runState.RegisteredAt;
            if (pendingDuration < PendingRunBindGracePeriod)
                return false;

            return AbortRun(
                runState,
                currentTime,
                $"Run aborted because it did not bind to a Rift region within {PendingRunBindGracePeriod.TotalSeconds:0} seconds.");
        }

        private void NotifyRunStarted(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            string message = $"[Cosmic Rift] Level {runState.Config.RiftLevel} started in {runState.Config.Content.DisplayName}. Final boss: {ResolveBossDisplayName(runState.Config)}. Quota: 0/{runState.Config.KillQuota}. Timer: {runState.Config.TimeLimit.TotalMinutes:0} min.";
            NotifyRunPlayers(runState, message);
        }

        private void NotifyBossUnlocked(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            string message = $"[Cosmic Rift] Kill quota reached in {runState.Config.Content.DisplayName}. Final boss unlocked: {ResolveBossDisplayName(runState.Config)}.";
            NotifyRunPlayers(runState, message);
        }

        private void NotifyRunCompleted(MythicRiftRunState runState, bool success, string statusMessage)
        {
            if (runState == null)
                return;

            string outcome = success ? "SUCCESS" : runState.Status.ToString().ToUpperInvariant();
            string message = $"[Cosmic Rift] {outcome} | map={runState.Config.Content.DisplayName} | boss={ResolveBossDisplayName(runState.Config)} | level={runState.Config.RiftLevel} | {statusMessage}";
            NotifyRunPlayers(runState, message);
        }

        private void NotifyRunPlayers(MythicRiftRunState runState, string message)
        {
            if (runState == null || string.IsNullOrWhiteSpace(message))
                return;

            HashSet<ulong> recipientDbIds = new(runState.ParticipantPlayerDbIds);
            if (runState.RegionId != 0)
            {
                Region region = Game.RegionManager.GetRegion(runState.RegionId);
                if (region != null)
                {
                    foreach (Player regionPlayer in new PlayerIterator(region))
                        recipientDbIds.Add(regionPlayer.DatabaseUniqueId);
                }
            }

            foreach (ulong playerDbId in recipientDbIds)
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
                if (player == null)
                    continue;

                Game.ChatManager.SendChatFromCustomSystem(player, message, showSender: false);
            }
        }

        private static string ResolveBossDisplayName(MythicRiftRunConfig config)
        {
            if (config?.BossContent?.DisplayName == null)
                return config?.BossProtoRef.GetNameFormatted() ?? "Unknown Boss";

            string bossName = config.BossContent.DisplayName;
            const string terminalSuffix = " Terminal";
            if (bossName.EndsWith(terminalSuffix, StringComparison.OrdinalIgnoreCase))
                return bossName[..^terminalSuffix.Length];

            return bossName;
        }

        private static bool ShouldAutoRemoveRun(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.CompletedAt.HasValue == false)
                return false;

            return currentTime - runState.CompletedAt.Value >= CompletedRunRetention;
        }

        private void RegisterDefaultContent()
        {
            foreach (MythicRiftContentDefinition definition in DefaultContentDefinitions)
                RegisterContent(definition);
        }

        private void RegisterContent(MythicRiftContentDefinition definition)
        {
            if (definition == null)
                return;

            MythicRiftContentEntry content = new()
            {
                Id = definition.Id,
                DisplayName = definition.DisplayName,
                DefaultKillQuota = definition.DefaultKillQuota,
                RegionProtoRef = ResolvePrototype(definition.RegionPrototypeName),
                StartTargetProtoRef = ResolveStartTarget(definition.RegionPrototypeName),
                MissionProtoRef = ResolvePrototype(definition.MissionPrototypeName),
                BossProtoRef = ResolvePrototype(definition.BossPrototypeName),
                BossLootTableProtoRef = ResolvePrototype(definition.BossLootTablePrototypeName)
            };

            if (content.IsValid == false)
            {
                Logger.Warn($"RegisterContent(): failed to resolve mythic rift content id={definition.Id}");
                return;
            }

            RegisterContent(content);
        }

        private static PrototypeId ResolvePrototype(string prototypeName)
        {
            if (string.IsNullOrWhiteSpace(prototypeName))
                return PrototypeId.Invalid;

            PrototypeId prototypeRef = GameDatabase.GetPrototypeRefByName(prototypeName);
            if (prototypeRef == PrototypeId.Invalid)
                Logger.Warn($"ResolvePrototype(): failed to resolve {prototypeName}");

            return prototypeRef;
        }

        private static PrototypeId ResolveStartTarget(string regionPrototypeName)
        {
            PrototypeId regionProtoRef = ResolvePrototype(regionPrototypeName);
            if (regionProtoRef == PrototypeId.Invalid)
                return PrototypeId.Invalid;

            RegionPrototype regionProto = regionProtoRef.As<RegionPrototype>();
            if (regionProto == null)
                return PrototypeId.Invalid;

            return regionProto.StartTarget;
        }

        private static int ResolveKillQuota(MythicRiftContentEntry content, int killQuota)
        {
            if (killQuota > 0)
                return killQuota;

            if (content != null && content.DefaultKillQuota > 0)
                return content.DefaultKillQuota;

            return 50;
        }

        private void SyncOnlinePlayerRiftLevel(ulong playerDbId, int unlockedLevel)
        {
            if (playerDbId == 0)
                return;

            Player onlinePlayer = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
            if (onlinePlayer == null)
                return;

            onlinePlayer.MythicRiftHighestUnlockedLevel = unlockedLevel;
        }

        private bool TryFindInProgressRunConflict(Player requester, Party party, out MythicRiftRunState conflictingRun)
        {
            conflictingRun = requester != null
                ? GetInProgressRunForPlayer(requester.DatabaseUniqueId)
                : null;

            if (conflictingRun != null)
                return true;

            if (party == null)
                return false;

            foreach (var kvp in party)
            {
                conflictingRun = GetInProgressRunForPlayer(kvp.Value.PlayerDbId);
                if (conflictingRun != null)
                    return true;
            }

            return false;
        }

        private sealed record MythicRiftContentDefinition(
            string Id,
            string DisplayName,
            int DefaultKillQuota,
            string RegionPrototypeName,
            string MissionPrototypeName,
            string BossPrototypeName,
            string BossLootTablePrototypeName);
    }
}
