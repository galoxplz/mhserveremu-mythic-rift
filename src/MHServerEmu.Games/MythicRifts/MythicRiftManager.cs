using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Parties;
using MHServerEmu.Games.UI;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private const float TimedSuccessBonusRarityPct = 0.10f;
        private const float TimedSuccessBonusSpecialPct = 0.15f;
        private static readonly bool SuspendNativeTerminalMissionsDuringRifts = true;
        private static readonly bool SuspendNativeRegionEventMissionsDuringRifts = true;
        private static readonly int[] TimeWarningThresholdSeconds = { 540, 480, 420, 360, 300, 240, 180, 120, 60, 30 };
        private static readonly int[] KillProgressMilestonePercents = { 25, 50, 75 };
        private static readonly TimeSpan ParticipantDisconnectAbortGracePeriod = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan PendingRunBindGracePeriod = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan CompletedRunRetention = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan NativeBossSuppressionScanInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan RiftObjectiveWidgetRefreshInterval = TimeSpan.FromSeconds(2);
        private const string RiftExitPortalPrototypeName = "Entity/Transitions/CowLevelTransition.prototype";
        private const float SpecialRandomMapChance = 0.05f;
        private static readonly MythicRiftContentDefinition[] DefaultContentDefinitions =
        {
            new(
                "shocker",
                "Shocker Terminal",
                45,
                "Regions/EndGame/Terminals/Green/ShockerSubway/AltRegions/DailyGShockerSubwayRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G01ShockerSubwayDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD01GShocker.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/AbandonedSubway/ShockerTerminalLoot.prototype"),
            new(
                "doctor-octopus",
                "Doctor Octopus Terminal",
                50,
                "Regions/EndGame/Terminals/Green/KingpinsWarehouse/AltRegions/DailyGKPWarehouseRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G02DoctorOctopusDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD02GDoctorOctopus.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/KingpinsWarehouse/DrOctopusTerminalLoot.prototype"),
            new(
                "taskmaster",
                "Taskmaster Terminal",
                50,
                "Regions/EndGame/Terminals/Green/Taskmaster/AltRegions/DailyGTaskmasterRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G03TaskmasterDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD03GTaskmaster.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/TaskmasterInstitute/TaskmasterTerminalLoot.prototype"),
            new(
                "hood",
                "Hood Terminal",
                55,
                "Regions/EndGame/Terminals/Green/HoodsShip/AltRegions/DailyGHoodsShipRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G04HoodDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD04GHood.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/HoodsHideout/HoodTerminalLoot.prototype"),
            new(
                "magneto",
                "Magneto Terminal",
                60,
                "Regions/EndGame/Terminals/Green/MagnetoBunker/AltRegions/DailyGStrykerBunkerRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G05MagnetoDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD05GMagneto.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/StrykerCommandBunker/MagnetoTerminalLoot.prototype",
                RandomMapEligible: false,
                RandomBossEligible: false),
            new(
                "sinister",
                "Mister Sinister Terminal",
                60,
                "Regions/EndGame/Terminals/Green/SinistersLab/AltRegions/DailyGSinisterLabRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G06MisterSinisterDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD06GMrSinister.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/SinisterLab/MisterSinisterTerminalLoot.prototype"),
            new(
                "modok",
                "MODOK Terminal",
                60,
                "Regions/EndGame/Terminals/Green/AIMFacility/AltRegions/DailyGAIMFacilityRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G07MODOKDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD07GMODOK.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/AIMWeaponFacility/ModokTerminalLoot.prototype"),
            new(
                "mandarin",
                "Mandarin Terminal",
                65,
                "Regions/EndGame/Terminals/Green/HYDRAIsland/AltRegions/DailyGHYDRAIslandRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G08MandarinDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD08GMandarin.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/HydraIsland/MandarinTerminalLoot.prototype"),
            new(
                "kingpin",
                "Kingpin Terminal",
                65,
                "Regions/EndGame/Terminals/Green/FiskTower/AltRegions/DailyGFiskTowerRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G10FiskTowerDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD10GKingpin.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/FiskTower/KingpinLTerminalLoot.prototype"),
            new(
                "ultron",
                "Ultron Terminal",
                70,
                "Regions/EndGame/Terminals/Green/TimesSquare/AltRegions/DailyGTimesSquareRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G14UltronDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD14GUltronTerminal.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/TimesSquare/UltronTerminalLoot.prototype",
                RandomMapEligible: false,
                RandomBossEligible: false),
            new(
                "bronx-zoo",
                "Bronx Zoo",
                80,
                "Regions/EndGame/OneShotMissions/NonChapterBound/BronxZoo/AltRegions/BronxZooRegionL60.prototype",
                null,
                null,
                null,
                RandomBossEligible: false),
            new(
                "wakanda-jungle",
                "Wakanda Jungle",
                65,
                "Regions/EndGame/OneShotMissions/NonChapterBound/WakandaPart1/AltRegions/WakandaP1RegionL60.prototype",
                null,
                null,
                null,
                RandomBossEligible: false),
            new(
                "hydra-island-one-shot",
                "HYDRA Island One-Shot",
                65,
                "Regions/EndGame/OneShotMissions/NonChapterBound/HydraIslandPartDeux/AltRegions/HYDRAIslandPartDeuxRegionL60.prototype",
                null,
                null,
                null,
                RandomBossEligible: false),
            new(
                "daily-bugle",
                "Daily Bugle Operation",
                55,
                "Regions/Operations/Events/DailyBugle/OpDailyBugleRegionL11To60.prototype",
                null,
                null,
                null,
                RandomBossEligible: false),
            new(
                "dr-strange-times-square",
                "Doctor Strange Times Square",
                45,
                "Regions/EndGame/StaticScenarios/DrStrangeEvent/Cosmic/DrStrangeTimesSquareRegionCosmic.prototype",
                null,
                null,
                null,
                RandomBossEligible: false),
            new(
                "cosmic-doop-sector",
                "Cosmic Doop Sector",
                100,
                "Regions/EndGame/Special/CosmicDoopSectorSpace/CosmicDoopSectorSpaceRegion.prototype",
                "Missions/Prototypes/BonusMissions/OMDoopZone.prototype",
                "Entity/Characters/Mobs/DoopAllChapters/CosmicDoop/CosmicDoopOverlord.prototype",
                "Loot/Tables/Mob/NormalMobs/CosmicDoopOverlordTable.prototype",
                RandomBossEligible: false,
                IsSpecialRandomMap: true,
                UseOwnBossSourceWhenSelected: true),
        };

        private readonly List<MythicRiftContentEntry> _contentPool = new();
        private readonly Dictionary<ulong, MythicRiftRunState> _activeRuns = new();
        private readonly Dictionary<ulong, Event<EntityDeadGameEvent>.Action> _regionEntityDeadActions = new();
        private readonly Dictionary<ulong, int> _highestUnlockedRiftLevelByPlayer = new();
        private readonly Dictionary<ulong, int> _preferredLaunchRiftLevelByPlayer = new();
        private readonly Dictionary<ulong, string> _lastCompletedMapContentIdByPlayer = new();
        private readonly Dictionary<ulong, TimeSpan> _nextNativeBossSuppressionScanAt = new();
        private readonly Dictionary<ulong, TimeSpan> _nextRiftObjectiveWidgetRefreshAt = new();
        private readonly Dictionary<ulong, HashSet<Mission>> _serverSuspendedNativeObjectiveMissionsByRun = new();
        private ulong _nextRunId = 1;

        public Game Game { get; }

        public MythicRiftManager(Game game)
        {
            Game = game;
            RegisterDefaultContent();
        }

        public IReadOnlyList<MythicRiftContentEntry> ContentPool => _contentPool;
        public IReadOnlyList<MythicRiftContentEntry> RandomEligibleContentPool => RandomMapEligibleContentPool;
        public IReadOnlyList<MythicRiftContentEntry> RandomMapEligibleContentPool => _contentPool.Where(entry => entry.RandomMapEligible).ToList();
        public IReadOnlyList<MythicRiftContentEntry> RandomBossEligibleContentPool => _contentPool.Where(entry => entry.RandomBossEligible && entry.HasValidBossSource).ToList();
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

        public int GetPreferredLaunchRiftLevel(ulong playerDbId)
        {
            int highestUnlockedLevel = GetHighestUnlockedRiftLevel(playerDbId);
            if (playerDbId == 0)
                return highestUnlockedLevel;

            if (_preferredLaunchRiftLevelByPlayer.TryGetValue(playerDbId, out int preferredLevel) == false || preferredLevel <= 0)
                return highestUnlockedLevel;

            return Math.Min(Math.Max(preferredLevel, 1), highestUnlockedLevel);
        }

        public bool TrySetPreferredLaunchRiftLevel(ulong playerDbId, int riftLevel, out int appliedLevel, out string errorMessage)
        {
            appliedLevel = GetPreferredLaunchRiftLevel(playerDbId);
            errorMessage = string.Empty;

            if (playerDbId == 0)
            {
                errorMessage = "Player not found.";
                return false;
            }

            if (riftLevel <= 0)
            {
                errorMessage = "Rift level must be greater than zero.";
                return false;
            }

            int highestUnlockedLevel = GetHighestUnlockedRiftLevel(playerDbId);
            if (riftLevel > highestUnlockedLevel)
            {
                errorMessage = $"Requested Rift level {riftLevel} is locked. Highest unlocked level: {highestUnlockedLevel}.";
                return false;
            }

            _preferredLaunchRiftLevelByPlayer[playerDbId] = riftLevel;
            appliedLevel = riftLevel;
            return true;
        }

        public int UseHighestUnlockedLaunchRiftLevel(ulong playerDbId)
        {
            if (playerDbId == 0)
                return 1;

            _preferredLaunchRiftLevelByPlayer.Remove(playerDbId);
            return GetHighestUnlockedRiftLevel(playerDbId);
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

            MythicRiftContentEntry bossContent = SelectBossContentForFixedMap(content);

            return CreateRunConfig(content, bossContent, riftLevel, requestedPlayerCount, killQuota, timeLimit);
        }

        public MythicRiftRunConfig CreateDebugRunConfig(string contentId, string bossContentId, int riftLevel, int requestedPlayerCount, int killQuota, TimeSpan timeLimit)
        {
            MythicRiftContentEntry content = GetContent(contentId);
            MythicRiftContentEntry bossContent = GetContent(bossContentId);
            if (content == null || bossContent == null)
                return null;

            if (bossContent.HasValidBossSource == false)
                return null;

            return CreateRunConfig(content, bossContent, riftLevel, requestedPlayerCount, killQuota, timeLimit);
        }

        public MythicRiftRunConfig CreateRandomDebugRunConfig(int riftLevel, int requestedPlayerCount, int killQuota, TimeSpan timeLimit, IReadOnlyCollection<string> excludedMapContentIds = null)
        {
            MythicRiftContentEntry content = SelectRandomMapContent(excludedMapContentIds);
            MythicRiftContentEntry bossContent = SelectBossContentForRandomMap(content);
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

        public MythicRiftRunState CreateRandomDebugRun(int riftLevel, int requestedPlayerCount, int killQuota, TimeSpan timeLimit, IReadOnlyCollection<string> excludedMapContentIds = null)
        {
            MythicRiftRunConfig config = CreateRandomDebugRunConfig(riftLevel, requestedPlayerCount, killQuota, timeLimit, excludedMapContentIds);
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
            SendStopRiftTimer(runState);
            TryRestoreRegionDifficultyScaling(runState);
            RestoreSuspendedNativeObjectiveMissions(runState);
            RequestRunRegionShutdownWhenVacant(runState);
            bool removed = _activeRuns.Remove(runId);

            if (removed && regionId != 0)
                CleanupRegionListener(regionId);

            if (removed)
            {
                _nextNativeBossSuppressionScanAt.Remove(runId);
                _nextRiftObjectiveWidgetRefreshAt.Remove(runId);
                _serverSuspendedNativeObjectiveMissionsByRun.Remove(runId);
            }

            return removed;
        }

        public bool StartRun(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            runState.Start(currentTime);
            if (runState.Status == MythicRiftRunStatus.Active)
            {
                SendStartRiftTimer(runState);
                SuppressNativeTerminalBosses(runState, currentTime, force: true);
                RefreshRiftObjectiveWidgets(runState, currentTime, force: true);
                NotifyRunStarted(runState);
            }

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

            return CompleteRunFailure(runState, currentTime, "Time expired. Base boss rewards only.", returnParticipantsToHub: true);
        }

        public bool MarkRunAborted(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            return AbortRun(runState, currentTime, "The Rift was abandoned. A new Beacon is required to start another run.");
        }

        public int ReturnRunParticipantsToDangerRoomHub(MythicRiftRunState runState, Player fallbackPlayer = null, bool includePlayersAlreadyOutsideRunRegion = true)
        {
            if (runState == null || TryResolveDangerRoomHubStartTarget(out PrototypeId dangerRoomHubStartTarget) == false)
                return 0;

            HashSet<ulong> playerDbIds = new(runState.ParticipantPlayerDbIds);
            if (runState.RegionId != 0)
            {
                Region region = Game.RegionManager.GetRegion(runState.RegionId);
                if (region != null)
                {
                    foreach (Player regionPlayer in new PlayerIterator(region))
                    {
                        if (regionPlayer?.DatabaseUniqueId != 0)
                            playerDbIds.Add(regionPlayer.DatabaseUniqueId);
                    }
                }
            }

            if (playerDbIds.Count == 0 && fallbackPlayer?.DatabaseUniqueId != 0)
                playerDbIds.Add(fallbackPlayer.DatabaseUniqueId);

            int teleportedPlayerCount = 0;
            foreach (ulong playerDbId in playerDbIds)
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
                if (player?.CurrentAvatar?.IsInWorld != true)
                    continue;

                if (includePlayersAlreadyOutsideRunRegion == false && IsPlayerInRunRegion(player, runState) == false)
                    continue;

                Teleporter.DebugTeleportToTarget(player, dangerRoomHubStartTarget, GameDatabase.GlobalsPrototype.DifficultyTierDefault);
                teleportedPlayerCount++;
            }

            return teleportedPlayerCount;
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

            return CompleteRunFailure(runState, currentTime, "Time expired. Base boss rewards only.", returnParticipantsToHub: true);
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
                SuppressNativeTerminalBosses(runState, currentTime);
                RefreshRiftObjectiveWidgets(runState, currentTime);

                if (runState.HasExpired(currentTime))
                {
                    CompleteRunFailure(runState, currentTime, "Time expired. Base boss rewards only.", returnParticipantsToHub: true);
                }
                else
                {
                    TryNotifyRunTimeWarnings(runState, currentTime);
                }

                if (TryAbortRunForDisconnectedParticipants(runState, currentTime))
                    continue;

                if (TryAbortRunForParticipantExit(runState, currentTime))
                {
                    runsToRemove ??= new();
                    runsToRemove.Add(runState.Config.RunId);
                    continue;
                }

                if (TryAbortStalePendingRun(runState, currentTime))
                    continue;

                if (ShouldRemoveCompletedRunBecauseRegionIsEmpty(runState))
                {
                    runsToRemove ??= new();
                    runsToRemove.Add(runState.Config.RunId);
                    continue;
                }

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

            HashSet<string> excludedMapContentIds = useRandomContent
                ? BuildRandomMapExclusions(player, party)
                : null;

            MythicRiftRunState runState = useRandomContent
                ? CreateRandomDebugRun(riftLevel, requestedPlayerCount, killQuota, timeLimit, excludedMapContentIds)
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

        private MythicRiftContentEntry SelectRandomMapContent(IReadOnlyCollection<string> excludedContentIds = null)
        {
            List<MythicRiftContentEntry> eligibleContent = _contentPool.Where(entry => entry.RandomMapEligible).ToList();
            if (eligibleContent.Count == 0)
                return null;

            if (excludedContentIds != null && excludedContentIds.Count > 0 && eligibleContent.Count > excludedContentIds.Count)
            {
                List<MythicRiftContentEntry> filteredContent = eligibleContent
                    .Where(entry => excludedContentIds.Any(excludedId => string.Equals(excludedId, entry.Id, StringComparison.OrdinalIgnoreCase)) == false)
                    .ToList();

                if (filteredContent.Count > 0)
                    eligibleContent = filteredContent;
            }

            List<MythicRiftContentEntry> specialContent = eligibleContent.Where(entry => entry.IsSpecialRandomMap).ToList();
            List<MythicRiftContentEntry> standardContent = eligibleContent.Where(entry => entry.IsSpecialRandomMap == false).ToList();
            if (specialContent.Count > 0 && standardContent.Count > 0 && Game.Random.NextFloat() < SpecialRandomMapChance)
                return PickRandomContent(specialContent);

            if (standardContent.Count > 0)
                return PickRandomContent(standardContent);

            return PickRandomContent(eligibleContent);
        }

        private MythicRiftContentEntry SelectBossContentForRandomMap(MythicRiftContentEntry mapContent)
        {
            if (mapContent?.UseOwnBossSourceWhenSelected == true && mapContent.HasValidBossSource)
                return mapContent;

            return SelectRandomBossContent(mapContent);
        }

        private MythicRiftContentEntry SelectBossContentForFixedMap(MythicRiftContentEntry mapContent)
        {
            if (mapContent == null)
                return null;

            if (mapContent.HasValidBossSource && (mapContent.RandomBossEligible || mapContent.UseOwnBossSourceWhenSelected))
                return mapContent;

            return SelectRandomBossContent(mapContent);
        }

        private MythicRiftContentEntry SelectRandomBossContent(MythicRiftContentEntry mapContent)
        {
            List<MythicRiftContentEntry> eligibleContent = _contentPool.Where(entry => entry.RandomBossEligible && entry.HasValidBossSource).ToList();
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

            return PickRandomContent(eligibleContent);
        }

        private MythicRiftContentEntry PickRandomContent(IReadOnlyList<MythicRiftContentEntry> eligibleContent)
        {
            if (eligibleContent == null || eligibleContent.Count == 0)
                return null;

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

        private int SuppressNativeTerminalBosses(MythicRiftRunState runState, TimeSpan currentTime, bool force = false)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active || runState.RegionId == 0)
                return 0;

            if (force == false &&
                _nextNativeBossSuppressionScanAt.TryGetValue(runState.Config.RunId, out TimeSpan nextScanAt) &&
                currentTime < nextScanAt)
            {
                return 0;
            }

            _nextNativeBossSuppressionScanAt[runState.Config.RunId] = currentTime + NativeBossSuppressionScanInterval;

            PrototypeId nativeBossProtoRef = runState.Config.Content?.BossProtoRef ?? PrototypeId.Invalid;
            if (nativeBossProtoRef == PrototypeId.Invalid)
                return 0;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return 0;

            List<Agent> nativeBossesToDestroy = null;
            foreach (Entity entity in region.Entities)
            {
                if (entity is not Agent agent)
                    continue;

                if (agent.IsDestroyed || agent.IsDead || agent.IsInWorld == false)
                    continue;

                if (runState.BossEntityId != 0 && agent.Id == runState.BossEntityId)
                    continue;

                if (agent.IsAPrototype(nativeBossProtoRef) == false)
                    continue;

                nativeBossesToDestroy ??= new();
                nativeBossesToDestroy.Add(agent);
            }

            if (nativeBossesToDestroy == null)
                return 0;

            int destroyedCount = 0;
            foreach (Agent nativeBoss in nativeBossesToDestroy)
            {
                Logger.Info(
                    $"Mythic Rift run {runState.Config.RunId} suppressed native terminal boss {nativeBoss.PrototypeName} " +
                    $"so Rift boss {runState.Config.BossProtoRef.GetNameFormatted() ?? "unknown"} remains quota-gated.");
                nativeBoss.Destroy();
                destroyedCount++;
            }

            return destroyedCount;
        }

        public bool TryRefreshNativeObjectiveWidgetOverride(MissionObjective objective)
        {
            Mission mission = objective?.Mission;
            MythicRiftRunState runState = FindActiveRunForNativeObjectiveMission(mission, null);
            if (runState == null)
                return false;

            Region region = mission.Region;
            if (region == null)
                return false;

            RefreshRiftObjectiveWidgetsForMission(region, mission, runState, Game.CurrentTime);
            return true;
        }

        public bool TrySendNativeMissionUpdateOverride(Mission mission, Player player, MissionUpdateFlags missionFlags, MissionObjectiveUpdateFlags objectiveFlags)
        {
            if (missionFlags == MissionUpdateFlags.None && objectiveFlags == MissionObjectiveUpdateFlags.None)
                return false;

            MythicRiftRunState runState = FindActiveRunForNativeObjectiveMission(mission, player);
            if (runState == null)
                return false;

            SendNativeMissionTrackerSuppression(player, mission, runState);
            return true;
        }

        public bool TrySendNativeObjectiveUpdateOverride(MissionObjective objective, Player player, MissionObjectiveUpdateFlags objectiveFlags)
        {
            if (objectiveFlags == MissionObjectiveUpdateFlags.None)
                return false;

            Mission mission = objective?.Mission;
            MythicRiftRunState runState = FindActiveRunForNativeObjectiveMission(mission, player);
            if (runState == null)
                return false;

            SendNativeObjectiveTrackerSuppression(player, mission, objective, runState);
            return true;
        }

        private MythicRiftRunState FindActiveRunForNativeObjectiveMission(Mission mission, Player player)
        {
            if (mission == null || mission.PrototypeDataRef == PrototypeId.Invalid)
                return null;

            Region region = mission.Region ?? player?.GetRegion();
            if (region == null)
                return null;

            foreach (MythicRiftRunState runState in _activeRuns.Values)
            {
                if (runState.Status != MythicRiftRunStatus.Active || runState.RegionId == 0)
                    continue;

                if (runState.RegionId != region.Id && IsMatchingRunRegion(region, runState) == false)
                    continue;

                if (player != null && IsPlayerInRunRegion(player, runState) == false)
                    continue;

                if (IsNativeObjectiveMissionForRun(mission, runState))
                    return runState;
            }

            return null;
        }

        private static bool IsNativeObjectiveMissionForRun(Mission mission, MythicRiftRunState runState)
        {
            if (mission == null || runState?.Config == null)
                return false;

            bool isNativeTerminalMission = mission.PrototypeDataRef == runState.Config.MissionProtoRef;
            bool shouldControlTerminalMission = isNativeTerminalMission && SuspendNativeTerminalMissionsDuringRifts;
            bool shouldControlRegionEventMission = mission.IsRegionEventMission && SuspendNativeRegionEventMissionsDuringRifts;

            return shouldControlTerminalMission || shouldControlRegionEventMission;
        }

        private void RefreshRiftObjectiveWidgets(MythicRiftRunState runState, TimeSpan currentTime, bool force = false)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active || runState.RegionId == 0)
                return;

            if (force == false &&
                _nextRiftObjectiveWidgetRefreshAt.TryGetValue(runState.Config.RunId, out TimeSpan nextRefreshAt) &&
                currentTime < nextRefreshAt)
            {
                return;
            }

            _nextRiftObjectiveWidgetRefreshAt[runState.Config.RunId] = currentTime + RiftObjectiveWidgetRefreshInterval;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return;

            using var missionHandle = HashSetPool<Mission>.Instance.Get(out HashSet<Mission> missions);
            AddNativeTerminalMission(missions, region.MissionManager, runState.Config.MissionProtoRef);
            AddNativeRegionEventMissions(missions, region.MissionManager);

            foreach (Player player in new PlayerIterator(region))
                AddNativeTerminalMission(missions, player?.MissionManager, runState.Config.MissionProtoRef);

            foreach (Mission mission in missions)
            {
                TrySuspendNativeObjectiveMissionForRun(runState, mission);
                SuppressNativeMissionTrackerForRunPlayers(mission, runState);
                RefreshRiftObjectiveWidgetsForMission(region, mission, runState, currentTime);
            }
        }

        private void ClearRiftObjectiveWidgets(MythicRiftRunState runState)
        {
            if (runState == null || runState.RegionId == 0)
                return;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return;

            using var missionHandle = HashSetPool<Mission>.Instance.Get(out HashSet<Mission> missions);
            AddNativeTerminalMission(missions, region.MissionManager, runState.Config.MissionProtoRef);
            AddNativeRegionEventMissions(missions, region.MissionManager);

            foreach (Player player in new PlayerIterator(region))
                AddNativeTerminalMission(missions, player?.MissionManager, runState.Config.MissionProtoRef);

            foreach (Mission mission in missions)
                ClearRiftObjectiveWidgetsForMission(region, mission);
        }

        private static void AddNativeTerminalMission(HashSet<Mission> missions, MissionManager missionManager, PrototypeId missionRef)
        {
            if (missions == null || missionManager == null || missionRef == PrototypeId.Invalid)
                return;

            Mission mission = missionManager.FindMissionByDataRef(missionRef);
            if (mission != null)
                missions.Add(mission);
        }

        private static void AddNativeRegionEventMissions(HashSet<Mission> missions, MissionManager missionManager)
        {
            if (missions == null || missionManager == null)
                return;

            foreach (PrototypeId missionRef in missionManager.ActiveMissions)
            {
                Mission mission = missionManager.FindMissionByDataRef(missionRef);
                if (mission?.IsRegionEventMission == true)
                    missions.Add(mission);
            }
        }

        private void TrySuspendNativeObjectiveMissionForRun(MythicRiftRunState runState, Mission mission)
        {
            if (runState?.Config == null || mission == null)
                return;

            bool isNativeTerminalMission = mission.PrototypeDataRef == runState.Config.MissionProtoRef;
            bool shouldSuspendTerminalMission = isNativeTerminalMission && SuspendNativeTerminalMissionsDuringRifts;
            bool shouldSuspendRegionEventMission = mission.IsRegionEventMission && SuspendNativeRegionEventMissionsDuringRifts;

            if (shouldSuspendTerminalMission == false && shouldSuspendRegionEventMission == false)
                return;

            if (mission.PrototypeDataRef == PrototypeId.Invalid || mission.IsSuspended)
                return;

            if (mission.SetSuspendedState(true) == false)
                return;

            if (_serverSuspendedNativeObjectiveMissionsByRun.TryGetValue(runState.Config.RunId, out HashSet<Mission> suspendedMissions) == false)
            {
                suspendedMissions = new();
                _serverSuspendedNativeObjectiveMissionsByRun[runState.Config.RunId] = suspendedMissions;
            }

            suspendedMissions.Add(mission);
            string missionKind = isNativeTerminalMission ? "native terminal" : "region event";
            Logger.Info($"Mythic Rift run {runState.Config.RunId} suspended {missionKind} mission {mission.PrototypeName} while the Rift is active.");
        }

        private void RestoreSuspendedNativeObjectiveMissions(MythicRiftRunState runState)
        {
            if (runState?.Config == null)
                return;

            if (_serverSuspendedNativeObjectiveMissionsByRun.TryGetValue(runState.Config.RunId, out HashSet<Mission> suspendedMissions) == false)
                return;

            int restoredCount = 0;
            foreach (Mission mission in suspendedMissions)
            {
                if (mission?.IsSuspended != true)
                    continue;

                if (mission.SetSuspendedState(false))
                    restoredCount++;
            }

            if (restoredCount > 0)
                Logger.Info($"Mythic Rift run {runState.Config.RunId} restored {restoredCount} suspended native objective mission(s).");
        }

        private void RefreshRiftObjectiveWidgetsForMission(Region region, Mission mission, MythicRiftRunState runState, TimeSpan currentTime)
        {
            UIDataProvider uiDataProvider = region?.UIDataProvider;
            if (uiDataProvider == null || mission == null || runState?.Config == null)
                return;

            PrototypeId missionRef = mission.PrototypeDataRef;
            if (missionRef == PrototypeId.Invalid)
                return;

            bool removeMissionNameWidget = false;
            foreach (MissionObjective objective in mission.Objectives)
            {
                MissionObjectivePrototype objectiveProto = objective?.Prototype;
                if (objectiveProto == null)
                    continue;

                RefreshRiftObjectiveWidget(uiDataProvider, missionRef, objectiveProto.MetaGameWidget, runState, currentTime, ref removeMissionNameWidget);
                SuppressNativeObjectiveWidget(uiDataProvider, missionRef, objectiveProto.MetaGameWidgetFail, ref removeMissionNameWidget);
            }

            if (removeMissionNameWidget)
                SuppressMissionNameWidget(uiDataProvider, missionRef);
        }

        private void SuppressNativeMissionTrackerForRunPlayers(Mission mission, MythicRiftRunState runState)
        {
            if (mission == null || runState == null)
                return;

            foreach (Player player in GetRunPlayers(runState))
            {
                if (IsPlayerInRunRegion(player, runState) == false)
                    continue;

                SendNativeMissionTrackerSuppression(player, mission, runState);
            }
        }

        private static void SendNativeMissionTrackerSuppression(Player player, Mission mission, MythicRiftRunState runState)
        {
            if (player == null || mission == null || mission.PrototypeDataRef == PrototypeId.Invalid)
                return;

            try
            {
                NetMessageMissionUpdate missionMessage = NetMessageMissionUpdate.CreateBuilder()
                    .SetMissionPrototypeId((ulong)mission.PrototypeDataRef)
                    .SetMissionState((uint)MissionState.Inactive)
                    .SetSuppressNotification(true)
                    .SetSuspendedState(true)
                    .Build();

                player.SendMessage(missionMessage);

                foreach (MissionObjective objective in mission.Objectives)
                    SendNativeObjectiveTrackerSuppression(player, mission, objective, runState);
            }
            catch (Exception e)
            {
                Logger.Warn($"SendNativeMissionTrackerSuppression(): failed for mission {mission.PrototypeName}: {e.Message}");
            }
        }

        private static void SendNativeObjectiveTrackerSuppression(Player player, Mission mission, MissionObjective objective, MythicRiftRunState runState)
        {
            if (player == null || mission == null || objective == null || mission.PrototypeDataRef == PrototypeId.Invalid)
                return;

            uint requiredCount = (uint)Math.Max(runState?.Config?.KillQuota ?? 1, 1);
            uint currentCount = (uint)Math.Clamp(runState?.CurrentKillCount ?? 0, 0, (int)requiredCount);
            bool isGenericCounter = UsesGenericFractionWidget(objective);

            // The client can keep one native bounty counter visible. For that one, keep a generic
            // objective alive but replace its numbers with the Rift quota. Everything else is hidden.
            MissionObjectiveState objectiveState = isGenericCounter ? MissionObjectiveState.Active : MissionObjectiveState.Invalid;

            NetMessageMissionObjectiveUpdate objectiveMessage = NetMessageMissionObjectiveUpdate.CreateBuilder()
                .SetMissionPrototypeId((ulong)mission.PrototypeDataRef)
                .SetObjectiveIndex(objective.PrototypeIndex)
                .SetObjectiveState((uint)objectiveState)
                .SetCurrentCount(currentCount)
                .SetRequiredCount(requiredCount)
                .SetFailCurrentCount(0)
                .SetFailRequiredCount(0)
                .SetSuppressNotification(true)
                .SetSuspendedState(isGenericCounter == false)
                .Build();

            player.SendMessage(objectiveMessage);
        }

        private static bool UsesGenericFractionWidget(MissionObjective objective)
        {
            PrototypeId widgetRef = objective?.Prototype?.MetaGameWidget ?? PrototypeId.Invalid;
            if (widgetRef == PrototypeId.Invalid)
                return false;

            return GameDatabase.GetPrototype<MetaGameDataPrototype>(widgetRef) is UIWidgetGenericFractionPrototype;
        }

        private static void ClearRiftObjectiveWidgetsForMission(Region region, Mission mission)
        {
            UIDataProvider uiDataProvider = region?.UIDataProvider;
            if (uiDataProvider == null || mission == null)
                return;

            PrototypeId missionRef = mission.PrototypeDataRef;
            if (missionRef == PrototypeId.Invalid)
                return;

            foreach (MissionObjective objective in mission.Objectives)
            {
                MissionObjectivePrototype objectiveProto = objective?.Prototype;
                if (objectiveProto == null)
                    continue;

                if (objectiveProto.MetaGameWidget != PrototypeId.Invalid)
                    uiDataProvider.DeleteWidget(objectiveProto.MetaGameWidget, missionRef);

                if (objectiveProto.MetaGameWidgetFail != PrototypeId.Invalid)
                    uiDataProvider.DeleteWidget(objectiveProto.MetaGameWidgetFail, missionRef);
            }

            SuppressMissionNameWidget(uiDataProvider, missionRef);
        }

        private void RefreshRiftObjectiveWidget(UIDataProvider uiDataProvider, PrototypeId missionRef, PrototypeId widgetRef, MythicRiftRunState runState, TimeSpan currentTime, ref bool removeMissionNameWidget)
        {
            if (widgetRef == PrototypeId.Invalid)
                return;

            MetaGameDataPrototype metaDataProto = GameDatabase.GetPrototype<MetaGameDataPrototype>(widgetRef);
            if (metaDataProto == null)
                return;

            removeMissionNameWidget |= metaDataProto.DisplayMissionName;

            if (metaDataProto is UIWidgetGenericFractionPrototype)
            {
                UIWidgetGenericFraction fractionWidget = uiDataProvider.GetWidget<UIWidgetGenericFraction>(widgetRef, missionRef);
                if (fractionWidget == null)
                    return;

                int requiredCount = Math.Max(runState.Config.KillQuota, 1);
                int currentCount = Math.Clamp(runState.CurrentKillCount, 0, requiredCount);
                fractionWidget.SetCount(currentCount, requiredCount);

                TimeSpan remaining = runState.GetTimeRemaining(currentTime);
                if (remaining > TimeSpan.Zero)
                    fractionWidget.SetTimeRemaining((long)remaining.TotalMilliseconds);

                fractionWidget.SetAreaContext(missionRef);
                return;
            }

            // Keep native terminal logic alive, but hide terminal-specific HUD text such as "Defeat Kingpin".
            uiDataProvider.DeleteWidget(widgetRef, missionRef);
        }

        private static void SuppressNativeObjectiveWidget(UIDataProvider uiDataProvider, PrototypeId missionRef, PrototypeId widgetRef, ref bool removeMissionNameWidget)
        {
            if (uiDataProvider == null || widgetRef == PrototypeId.Invalid)
                return;

            MetaGameDataPrototype metaDataProto = GameDatabase.GetPrototype<MetaGameDataPrototype>(widgetRef);
            if (metaDataProto != null)
                removeMissionNameWidget |= metaDataProto.DisplayMissionName;

            uiDataProvider.DeleteWidget(widgetRef, missionRef);
        }

        private static void SuppressMissionNameWidget(UIDataProvider uiDataProvider, PrototypeId missionRef)
        {
            PrototypeId missionNameWidgetRef = GameDatabase.UIGlobalsPrototype?.MetaGameWidgetMissionName ?? PrototypeId.Invalid;
            if (uiDataProvider == null || missionNameWidgetRef == PrototypeId.Invalid)
                return;

            uiDataProvider.DeleteWidget(missionNameWidgetRef, missionRef);
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
                        CaptureSuccessfulCompletionEligibility(runState);
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
                            CaptureBossUnlockEligibility(runState);
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
                RefreshRiftObjectiveWidgets(runState, Game.CurrentTime, force: true);

                if (runState.BossUnlocked && previousKillCount < runState.Config.KillQuota)
                {
                    if (TrySpawnConfiguredBoss(runState, evt.Defender))
                    {
                        CaptureBossUnlockEligibility(runState);
                        NotifyBossUnlocked(runState);
                    }
                    else
                    {
                        Logger.Warn($"Mythic Rift run {runState.Config.RunId} unlocked boss {runState.Config.BossProtoRef.GetNameFormatted() ?? "unknown"} but the initial spawn attempt failed.");
                    }
                    Logger.Info($"Mythic Rift run {runState.Config.RunId} unlocked its boss after reaching {runState.CurrentKillCount}/{runState.Config.KillQuota} kills.");
                }
                else
                {
                    TryNotifyKillProgress(runState);
                }
            }
        }

        private static bool IsExpectedBossKill(MythicRiftRunState runState, in EntityDeadGameEvent evt)
        {
            if (runState == null || evt.Defender == null)
                return false;

            if (runState.BossUnlocked == false)
                return false;

            if (runState.BossEntityId != 0 && runState.BossEntityId == evt.Defender.Id)
                return true;

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

        private void RegisterRegionPlayersAsParticipants(MythicRiftRunState runState, Region region)
        {
            if (runState == null || region == null)
                return;

            foreach (Player player in new PlayerIterator(region))
            {
                runState.MarkParticipantSeenInRunRegion(player.DatabaseUniqueId);

                if (runState.RegisterParticipant(player.DatabaseUniqueId) && runState.Status == MythicRiftRunStatus.Active)
                {
                    SendStartRiftTimer(runState, player);
                    Game.ChatManager.SendChatFromCustomSystem(
                        player,
                        BuildJoinMessage(runState, Game.CurrentTime),
                        showSender: false);
                }
            }
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

            HashSet<ulong> recipientDbIds = new(runState.ProgressionEligiblePlayerDbIds);
            if (recipientDbIds.Count == 0)
            {
                Logger.Info($"Mythic Rift run {runState.Config.RunId} completed but no players satisfied the competitive progression rule for level unlocks.");
                return;
            }

            foreach (ulong playerDbId in recipientDbIds)
            {
                int unlockedLevel = GrantNextRiftLevel(playerDbId, runState.Config.RiftLevel);
                Logger.Info($"Mythic Rift run {runState.Config.RunId} unlocked Rift level {unlockedLevel} for playerDbId=0x{playerDbId:X}.");
            }
        }

        private HashSet<string> BuildRandomMapExclusions(Player requester, Party party)
        {
            HashSet<string> excludedContentIds = new(StringComparer.OrdinalIgnoreCase);
            TryAddPlayerRandomMapExclusions(excludedContentIds, requester);
            if (party == null)
                return excludedContentIds;

            foreach (var kvp in party)
            {
                Player partyMember = Game.EntityManager.GetEntityByDbGuid<Player>(kvp.Value.PlayerDbId);
                TryAddPlayerRandomMapExclusions(excludedContentIds, partyMember, kvp.Value.PlayerDbId);
            }

            return excludedContentIds;
        }

        private void TryAddPlayerRandomMapExclusions(HashSet<string> excludedContentIds, Player player, ulong playerDbId = 0)
        {
            if (excludedContentIds == null)
                return;

            ulong resolvedPlayerDbId = player?.DatabaseUniqueId ?? playerDbId;
            TryAddLastCompletedMapContentId(excludedContentIds, resolvedPlayerDbId);
            TryAddCurrentRegionMapContentId(excludedContentIds, player?.GetRegion());
        }

        private void TryAddLastCompletedMapContentId(HashSet<string> excludedContentIds, ulong playerDbId)
        {
            if (excludedContentIds == null || playerDbId == 0)
                return;

            if (_lastCompletedMapContentIdByPlayer.TryGetValue(playerDbId, out string contentId) == false)
                return;

            if (string.IsNullOrWhiteSpace(contentId))
                return;

            excludedContentIds.Add(contentId);
        }

        private void TryAddCurrentRegionMapContentId(HashSet<string> excludedContentIds, Region region)
        {
            if (excludedContentIds == null || region == null)
                return;

            MythicRiftContentEntry currentContent = ResolveContentByRegion(region);
            string contentId = currentContent?.Id;
            if (string.IsNullOrWhiteSpace(contentId))
                return;

            excludedContentIds.Add(contentId);
        }

        private MythicRiftContentEntry ResolveContentByRegion(Region region)
        {
            if (region == null)
                return null;

            return _contentPool.FirstOrDefault(content => ContentMatchesRegion(content, region));
        }

        private static bool ContentMatchesRegion(MythicRiftContentEntry content, Region region)
        {
            if (content == null || region == null)
                return false;

            if (region.PrototypeDataRef == content.RegionProtoRef)
                return true;

            RegionPrototype currentRegionProto = region.Prototype;
            RegionPrototype expectedRegionProto = content.RegionProtoRef.As<RegionPrototype>();
            if (RegionPrototype.Equivalent(expectedRegionProto, currentRegionProto))
                return true;

            RegionConnectionTargetPrototype startTargetProto = content.StartTargetProtoRef.As<RegionConnectionTargetPrototype>();
            RegionPrototype startTargetRegionProto = startTargetProto?.Region.As<RegionPrototype>();
            return RegionPrototype.Equivalent(startTargetRegionProto, currentRegionProto);
        }

        private void TrackLastCompletedMapContent(MythicRiftRunState runState)
        {
            string contentId = runState?.Config?.Content?.Id;
            if (string.IsNullOrWhiteSpace(contentId))
                return;

            foreach (ulong playerDbId in runState.ParticipantPlayerDbIds)
            {
                if (playerDbId != 0)
                    _lastCompletedMapContentIdByPlayer[playerDbId] = contentId;
            }
        }

        private void CaptureBossUnlockEligibility(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            runState.SnapshotBossUnlockEligiblePlayers(GetCurrentRunRegionPlayerDbIds(runState));
        }

        private void CaptureSuccessfulCompletionEligibility(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            runState.SnapshotProgressionEligiblePlayers(GetCurrentRunRegionPlayerDbIds(runState));
        }

        private HashSet<ulong> GetCurrentRunRegionPlayerDbIds(MythicRiftRunState runState)
        {
            HashSet<ulong> playerDbIds = new();
            if (runState == null || runState.RegionId == 0)
                return playerDbIds;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return playerDbIds;

            foreach (Player player in new PlayerIterator(region))
            {
                if (player?.DatabaseUniqueId != 0)
                    playerDbIds.Add(player.DatabaseUniqueId);
            }

            return playerDbIds;
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

            if (content.HasValidMap == false || bossContent.HasValidBossSource == false)
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
            TrackLastCompletedMapContent(runState);
            TryAutoGrantCompletionRewards(runState);
            TryRestoreRegionDifficultyScaling(runState);
            ClearRiftObjectiveWidgets(runState);
            SendStopRiftTimer(runState);
            int eligibleUnlockCount = runState.ProgressionEligiblePlayerDbIds.Count;
            string successMessage = eligibleUnlockCount > 0
                ? $"Rift cleared. Next level unlocked for {eligibleUnlockCount} eligible player(s). Bonus loot granted."
                : "Rift cleared. Loot granted, but no players met the next-level unlock rule.";
            NotifyRunCompleted(runState, success: true, successMessage);
            TrySpawnReturnPortal(runState);
            return true;
        }

        private bool CompleteRunFailure(MythicRiftRunState runState, TimeSpan currentTime, string reason, bool returnParticipantsToHub = false)
        {
            if (runState == null)
                return false;

            runState.MarkFailed(currentTime);
            if (runState.Status != MythicRiftRunStatus.Failed)
                return false;

            ResolveRewardOutcome(runState);
            TrackLastCompletedMapContent(runState);
            TryAutoGrantCompletionRewards(runState);
            TryRestoreRegionDifficultyScaling(runState);
            ClearRiftObjectiveWidgets(runState);
            SendStopRiftTimer(runState);
            RequestRunRegionShutdownWhenVacant(runState);
            NotifyRunCompleted(runState, success: false, reason);

            if (returnParticipantsToHub)
            {
                int returnedPlayerCount = ReturnRunParticipantsToDangerRoomHub(runState, includePlayersAlreadyOutsideRunRegion: false);
                Logger.Info($"Mythic Rift run {runState.Config.RunId} returned {returnedPlayerCount} online participant(s) to the Danger Room hub after timer failure.");
            }

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

            TrackLastCompletedMapContent(runState);
            TryRestoreRegionDifficultyScaling(runState);
            ClearRiftObjectiveWidgets(runState);
            SendStopRiftTimer(runState);
            RequestRunRegionShutdownWhenVacant(runState);
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

                if (IsMatchingRunRegion(region, runState) == false)
                    continue;

                if (AttachRunToRegion(runState.Config.RunId, region) == false)
                    return;

                StartRun(runState.Config.RunId, currentTime);
                Logger.Info($"Mythic Rift run {runState.Config.RunId} auto-bound to region {region.PrototypeName} (0x{region.Id:X}) and started.");
                return;
            }
        }

        private static bool IsMatchingRunRegion(Region region, MythicRiftRunState runState)
        {
            if (region == null || runState?.Config == null)
                return false;

            if (region.PrototypeDataRef == runState.Config.RegionProtoRef)
                return true;

            RegionPrototype currentRegionProto = region.Prototype;
            RegionPrototype expectedRegionProto = runState.Config.RegionProtoRef.As<RegionPrototype>();
            if (RegionPrototype.Equivalent(expectedRegionProto, currentRegionProto))
                return true;

            RegionConnectionTargetPrototype startTargetProto = runState.Config.StartTargetProtoRef.As<RegionConnectionTargetPrototype>();
            RegionPrototype startTargetRegionProto = startTargetProto?.Region.As<RegionPrototype>();
            if (RegionPrototype.Equivalent(startTargetRegionProto, currentRegionProto))
                return true;

            return false;
        }

        private static bool IsPlayerInRunRegion(Player player, MythicRiftRunState runState)
        {
            Region region = player?.GetRegion();
            if (region == null || runState == null)
                return false;

            if (runState.RegionId != 0 && region.Id == runState.RegionId)
                return true;

            return IsMatchingRunRegion(region, runState);
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

        private bool TryAbortRunForParticipantExit(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active || runState.RegionId == 0)
                return false;

            foreach (ulong participantPlayerDbId in runState.ParticipantPlayerDbIds)
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(participantPlayerDbId);
                if (player == null || player.GetRegion() == null)
                    continue;

                if (runState.HasParticipantBeenSeenInRunRegion(participantPlayerDbId) == false)
                    continue;

                if (IsPlayerInRunRegion(player, runState))
                    continue;

                string reason = "A participant left the Rift before completion. The Rift has closed and a new Beacon is required.";
                if (AbortRun(runState, currentTime, reason) == false)
                    return false;

                int returnedPlayerCount = ReturnRunParticipantsToDangerRoomHub(runState, player, includePlayersAlreadyOutsideRunRegion: false);
                Logger.Info($"Mythic Rift run {runState.Config.RunId} returned {returnedPlayerCount} online participant(s) to the Danger Room hub after participant exit.");
                return true;
            }

            return false;
        }

        private void RequestRunRegionShutdownWhenVacant(MythicRiftRunState runState)
        {
            if (runState == null || runState.RegionId == 0)
                return;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null || region.ShutdownRequested)
                return;

            region.RequestShutdown();
            Logger.Info($"Mythic Rift run {runState.Config.RunId} requested shutdown for region {region.PrototypeName} (0x{region.Id:X}).");
        }

        private bool TrySpawnReturnPortal(MythicRiftRunState runState)
        {
            if (runState == null || runState.RegionId == 0)
                return false;

            if (runState.ExitPortalEntityId != 0 && Game.EntityManager.GetEntity<Transition>(runState.ExitPortalEntityId) != null)
                return true;

            if (TryResolveDangerRoomHubStartTarget(out PrototypeId dangerRoomHubStartTarget) == false)
                return false;

            PrototypeId exitPortalProtoRef = GameDatabase.GetPrototypeRefByName(RiftExitPortalPrototypeName);
            if (exitPortalProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, $"TrySpawnReturnPortal(): Failed to resolve {RiftExitPortalPrototypeName}");

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return false;

            if (TryGetReturnPortalSpawnLocation(runState, region, out Vector3 spawnPosition, out Orientation spawnOrientation, out Cell spawnCell) == false)
                return false;

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = exitPortalProtoRef;
            settings.RegionId = region.Id;
            settings.Position = spawnPosition;
            settings.Orientation = spawnOrientation;
            settings.Cell = spawnCell;
            settings.Lifespan = CompletedRunRetention;
            settings.SourceEntityId = GetFirstRunAvatarId(region);

            using PropertyCollection settingsProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            settingsProperties[PropertyEnum.Interactable] = (int)TriBool.True;
            settingsProperties[PropertyEnum.InteractableUsesLeft] = -1;
            settingsProperties[PropertyEnum.Visible] = true;
            settings.Properties = settingsProperties;

            Transition exitPortal = Game.EntityManager.CreateEntity(settings) as Transition;
            if (exitPortal == null)
                return Logger.WarnReturn(false, "TrySpawnReturnPortal(): Failed to create return portal entity.");

            if (exitPortal.ConfigureDirectTarget(dangerRoomHubStartTarget) == false)
            {
                exitPortal.Destroy();
                return false;
            }

            runState.AttachExitPortal(exitPortal.Id);
            Logger.Info($"Mythic Rift run {runState.Config.RunId} spawned return portal {exitPortal.PrototypeName} (0x{exitPortal.Id:X}) to Danger Room hub.");
            return true;
        }

        public bool TryUseReturnPortal(Player player, Transition transition)
        {
            if (player == null || transition == null)
                return false;

            MythicRiftRunState runState = _activeRuns.Values.FirstOrDefault(run => run.ExitPortalEntityId == transition.Id);
            if (runState == null || runState.Status != MythicRiftRunStatus.Success)
                return false;

            if (TryResolveDangerRoomHubStartTarget(out PrototypeId dangerRoomHubStartTarget) == false)
                return false;

            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Debug);
            teleporter.DifficultyTierRef = GameDatabase.GlobalsPrototype.DifficultyTierDefault;
            bool teleported = teleporter.TeleportToTarget(dangerRoomHubStartTarget);
            if (teleported)
                Logger.Info($"Mythic Rift run {runState.Config.RunId} used return portal 0x{transition.Id:X} for playerDbId=0x{player.DatabaseUniqueId:X}.");
            else
                Logger.Warn($"Mythic Rift run {runState.Config.RunId} failed to use return portal 0x{transition.Id:X} for playerDbId=0x{player.DatabaseUniqueId:X}.");

            return teleported;
        }

        private static ulong GetFirstRunAvatarId(Region region)
        {
            if (region == null)
                return 0;

            foreach (Player player in new PlayerIterator(region))
            {
                Avatar avatar = player.CurrentAvatar;
                if (avatar != null && avatar.IsInWorld)
                    return avatar.Id;
            }

            return 0;
        }

        private bool TryGetReturnPortalSpawnLocation(MythicRiftRunState runState, Region region, out Vector3 position, out Orientation orientation, out Cell cell)
        {
            position = Vector3.Zero;
            orientation = Orientation.Zero;
            cell = null;

            if (region == null)
                return false;

            if (runState?.BossEntityId != 0)
            {
                WorldEntity bossEntity = Game.EntityManager.GetEntity<WorldEntity>(runState.BossEntityId);
                if (bossEntity?.IsInWorld == true && bossEntity.Region == region)
                {
                    position = bossEntity.RegionLocation.Position;
                    orientation = bossEntity.RegionLocation.Orientation;
                    cell = bossEntity.Cell;
                    return FinalizeReturnPortalSpawnLocation(region, ref position, ref cell);
                }
            }

            foreach (Player player in new PlayerIterator(region))
            {
                Avatar avatar = player?.CurrentAvatar;
                if (avatar?.IsInWorld != true || avatar.Region != region)
                    continue;

                position = avatar.RegionLocation.Position + avatar.Forward * 150f;
                orientation = avatar.RegionLocation.Orientation;
                cell = avatar.Cell;
                return FinalizeReturnPortalSpawnLocation(region, ref position, ref cell);
            }

            return false;
        }

        private static bool FinalizeReturnPortalSpawnLocation(Region region, ref Vector3 position, ref Cell cell)
        {
            position = RegionLocation.ProjectToFloor(region, position);
            cell ??= region.GetCellAtPosition(position);
            return cell != null;
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
                "All participants disconnected. The Rift has closed.");
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
                "The Rift could not be opened correctly. Please try again with a new Beacon.");
        }

        private void NotifyRunStarted(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            string message = $"[Cosmic Rift] Rift started: {runState.Config.Content.DisplayName} | Level {runState.Config.RiftLevel} | Timer: {FormatDuration(runState.Config.TimeLimit)}. Defeat {runState.Config.KillQuota} enemies to summon the Rift boss.";
            NotifyRunPlayers(runState, message);
        }

        private void NotifyBossUnlocked(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            string message = $"[Cosmic Rift] Enemy quota complete. Final boss summoned: {ResolveBossDisplayName(runState.Config)}. Defeat the boss before the timer expires.";
            NotifyRunPlayers(runState, message);
        }

        private void TryNotifyKillProgress(MythicRiftRunState runState)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active || runState.BossUnlocked)
                return;

            int requiredCount = Math.Max(runState.Config.KillQuota, 1);
            int progressPercent = (int)Math.Floor((double)runState.CurrentKillCount * 100d / requiredCount);
            foreach (int milestonePercent in KillProgressMilestonePercents)
            {
                if (progressPercent < milestonePercent)
                    continue;

                if (runState.MarkKillProgressMilestoneSent(milestonePercent) == false)
                    continue;

                NotifyRunPlayers(runState, $"[Cosmic Rift] Progress: {runState.CurrentKillCount}/{requiredCount} enemies defeated.");
            }
        }

        private void NotifyRunCompleted(MythicRiftRunState runState, bool success, string statusMessage)
        {
            if (runState == null)
                return;

            string message = success
                ? $"[Cosmic Rift] Rift complete! {ResolveBossDisplayName(runState.Config)} defeated in {runState.Config.Content.DisplayName}. Level {runState.Config.RiftLevel} cleared. {statusMessage}"
                : $"[Cosmic Rift] Rift closed: {runState.Config.Content.DisplayName} | Level {runState.Config.RiftLevel}. {statusMessage}";
            NotifyRunPlayers(runState, message);
        }

        private void TryNotifyRunTimeWarnings(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active || runState.ExpiresAt.HasValue == false)
                return;

            TimeSpan remaining = runState.GetTimeRemaining(currentTime);
            int remainingSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
            foreach (int thresholdSeconds in TimeWarningThresholdSeconds)
            {
                if (remainingSeconds > thresholdSeconds)
                    continue;

                if (runState.MarkTimeWarningSent(thresholdSeconds) == false)
                    continue;

                NotifyRunPlayers(runState, $"[Cosmic Rift] Time remaining: {FormatDuration(TimeSpan.FromSeconds(thresholdSeconds))}.");
            }
        }

        private static string BuildJoinMessage(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null)
                return "[Cosmic Rift] You joined an active Rift.";

            string remaining = FormatDuration(runState.GetTimeRemaining(currentTime));
            if (runState.BossUnlocked)
                return $"[Cosmic Rift] You joined an active Rift. Final boss: {ResolveBossDisplayName(runState.Config)}. Time remaining: {remaining}.";

            int remainingKills = Math.Max(runState.Config.KillQuota - runState.CurrentKillCount, 0);
            return $"[Cosmic Rift] You joined an active Rift. Defeat {remainingKills} more enemies to summon the Rift boss. Time remaining: {remaining}.";
        }

        private void SendStartRiftTimer(MythicRiftRunState runState, Player player = null)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active)
                return;

            TimeSpan remaining = runState.GetTimeRemaining(Game.CurrentTime);
            if (remaining <= TimeSpan.Zero)
                return;

            try
            {
                NetMessageStartPvPTimer message = NetMessageStartPvPTimer.CreateBuilder()
                    .SetMetaGameId(runState.Config.RunId)
                    .SetStartTime((uint)Math.Min(remaining.TotalMilliseconds, uint.MaxValue))
                    .SetEndTime(0)
                    .SetLowTimeWarning((uint)TimeSpan.FromMinutes(2).TotalMilliseconds)
                    .SetCriticalTimeWarning((uint)TimeSpan.FromMinutes(1).TotalMilliseconds)
                    .SetLabelOverrideTextId(0UL)
                    .Build();

                if (player != null)
                {
                    player.SendMessage(message);
                    return;
                }

                SendMessageToRunPlayers(runState, message);
            }
            catch (Exception e)
            {
                Logger.Warn($"Mythic Rift run {runState.Config.RunId} failed to send optional timer UI packet: {e.Message}");
            }
        }

        private void SendStopRiftTimer(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            NetMessageStopPvPTimer message = NetMessageStopPvPTimer.CreateBuilder()
                .SetMetaGameId(runState.Config.RunId)
                .Build();

            SendMessageToRunPlayers(runState, message);
        }

        private void NotifyRunPlayers(MythicRiftRunState runState, string message)
        {
            if (runState == null || string.IsNullOrWhiteSpace(message))
                return;

            foreach (Player player in GetRunPlayers(runState))
                Game.ChatManager.SendChatFromCustomSystem(player, message, showSender: false);
        }

        private void SendMessageToRunPlayers(MythicRiftRunState runState, Google.ProtocolBuffers.IMessage message)
        {
            if (runState == null || message == null)
                return;

            foreach (Player player in GetRunPlayers(runState))
                player.SendMessage(message);
        }

        private IEnumerable<Player> GetRunPlayers(MythicRiftRunState runState)
        {
            if (runState == null)
                yield break;

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

                yield return player;
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

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
                duration = TimeSpan.Zero;

            int totalSeconds = (int)Math.Ceiling(duration.TotalSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            if (minutes > 0 && seconds > 0)
                return $"{minutes} min {seconds} sec";

            if (minutes > 0)
                return $"{minutes} min";

            return $"{seconds} sec";
        }

        private static bool ShouldAutoRemoveRun(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.CompletedAt.HasValue == false)
                return false;

            return currentTime - runState.CompletedAt.Value >= CompletedRunRetention;
        }

        private bool ShouldRemoveCompletedRunBecauseRegionIsEmpty(MythicRiftRunState runState)
        {
            if (runState == null || runState.CompletedAt.HasValue == false || runState.RegionId == 0)
                return false;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return true;

            foreach (Player _ in new PlayerIterator(region))
                return false;

            return true;
        }

        private static bool TryResolveDangerRoomHubStartTarget(out PrototypeId startTargetRef)
        {
            startTargetRef = PrototypeId.Invalid;

            RegionPrototype dangerRoomHubRegion = ((PrototypeId)RegionPrototypeId.DangerRoomHubRegion).As<RegionPrototype>();
            if (dangerRoomHubRegion == null || dangerRoomHubRegion.StartTarget == PrototypeId.Invalid)
                return false;

            startTargetRef = dangerRoomHubRegion.StartTarget;
            return true;
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
                BossLootTableProtoRef = ResolvePrototype(definition.BossLootTablePrototypeName),
                RandomMapEligible = definition.RandomMapEligible,
                RandomBossEligible = definition.RandomBossEligible,
                IsSpecialRandomMap = definition.IsSpecialRandomMap,
                UseOwnBossSourceWhenSelected = definition.UseOwnBossSourceWhenSelected
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
            string BossLootTablePrototypeName,
            bool RandomMapEligible = true,
            bool RandomBossEligible = true,
            bool IsSpecialRandomMap = false,
            bool UseOwnBossSourceWhenSelected = false);
    }
}
