using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
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
        private static readonly MythicRiftContentDefinition[] DefaultContentDefinitions =
        {
            new(
                "taskmaster",
                "Taskmaster Terminal",
                "Regions/EndGame/Terminals/Green/Taskmaster/DailyGTaskmasterRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G03TaskmasterDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD03GTaskmaster.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/TaskmasterInstitute/TaskmasterTerminalLoot.prototype"),
            new(
                "hood",
                "Hood Terminal",
                "Regions/EndGame/Terminals/Green/HoodsShip/DailyGHoodsShipRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G04HoodDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD04GHood.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/HoodsHideout/HoodTerminalLoot.prototype"),
            new(
                "sinister",
                "Mister Sinister Terminal",
                "Regions/EndGame/Terminals/Green/SinistersLab/DailyGSinisterLabRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G06MisterSinisterDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD06GMrSinister.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/SinisterLab/MisterSinisterTerminalLoot.prototype"),
            new(
                "kingpin",
                "Kingpin Terminal",
                "Regions/EndGame/Terminals/Green/FiskTower/DailyGFiskTowerRegionBase.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G10FiskTowerDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD10GKingpin.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/FiskTower/KingpinLTerminalLoot.prototype"),
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
        public IReadOnlyCollection<MythicRiftRunState> ActiveRuns => _activeRuns.Values;

        public MythicRiftDifficultySnapshot GetDifficultySnapshot(int riftLevel, int requestedPlayerCount)
        {
            return MythicRiftScaling.BuildSnapshot(riftLevel, requestedPlayerCount);
        }

        public int GetHighestUnlockedRiftLevel(ulong playerDbId)
        {
            if (playerDbId == 0)
                return 1;

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
            return nextUnlockedLevel;
        }

        public MythicRiftRunConfig CreateDebugRunConfig(string contentId, int riftLevel, int requestedPlayerCount, int killQuota, TimeSpan timeLimit)
        {
            MythicRiftContentEntry content = GetContent(contentId);
            if (content == null)
                return null;

            MythicRiftDifficultySnapshot difficulty = GetDifficultySnapshot(riftLevel, requestedPlayerCount);

            return new MythicRiftRunConfig
            {
                RunId = _nextRunId++,
                RiftLevel = Math.Max(riftLevel, 1),
                Content = content,
                RequestedPlayerCount = Math.Max(requestedPlayerCount, 1),
                EffectivePlayerCount = difficulty.EffectivePlayerCount,
                KillQuota = Math.Max(killQuota, 1),
                TimeLimit = timeLimit <= TimeSpan.Zero ? TimeSpan.FromMinutes(10) : timeLimit,
                RegionProtoRef = content.RegionProtoRef,
                MissionProtoRef = content.MissionProtoRef,
                BossProtoRef = content.BossProtoRef,
                BossLootTableProtoRef = content.BossLootTableProtoRef,
                Difficulty = difficulty
            };
        }

        public MythicRiftRunConfig CreateRandomDebugRunConfig(int riftLevel, int requestedPlayerCount, int killQuota, TimeSpan timeLimit)
        {
            MythicRiftContentEntry content = SelectRandomContent();
            if (content == null)
                return null;

            return CreateDebugRunConfig(content.Id, riftLevel, requestedPlayerCount, killQuota, timeLimit);
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

        public bool RemoveRun(ulong runId)
        {
            if (runId == 0)
                return false;

            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            ulong regionId = runState.RegionId;
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

            runState.MarkSuccess(currentTime);
            ResolveRewardOutcome(runState);
            GrantProgressionForSuccessfulRun(runState);
            return runState.Status == MythicRiftRunStatus.Success;
        }

        public bool MarkRunFailed(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            runState.MarkFailed(currentTime);
            ResolveRewardOutcome(runState);
            return runState.Status == MythicRiftRunStatus.Failed;
        }

        public bool MarkRunAborted(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            runState.MarkAborted(currentTime);
            return runState.Status == MythicRiftRunStatus.Aborted;
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

            if (recipientDbIds.Count == 0 && runState.RegionId != 0)
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

            runState.MarkFailed(currentTime);
            return true;
        }

        public bool AttachRunToRegion(ulong runId, Region region)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null || region == null)
                return false;

            runState.AttachRegion(region.Id);
            RegisterRegionPlayersAsParticipants(runState, region);
            EnsureRegionListener(region);
            return true;
        }

        public void Update(TimeSpan currentTime)
        {
            foreach (MythicRiftRunState runState in _activeRuns.Values)
            {
                if (runState.HasExpired(currentTime) == false)
                    continue;

                runState.MarkFailed(currentTime);
                ResolveRewardOutcome(runState);
                Logger.Info($"Mythic Rift run {runState.Config.RunId} expired and failed automatically.");
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

        private MythicRiftContentEntry SelectRandomContent()
        {
            if (_contentPool.Count == 0)
                return null;

            int index = Game.Random.Next(0, _contentPool.Count);
            return _contentPool[index];
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
                        runState.MarkSuccess(Game.CurrentTime);
                        ResolveRewardOutcome(runState);
                        GrantProgressionForSuccessfulRun(runState);
                        Logger.Info($"Mythic Rift run {runState.Config.RunId} completed by defeating {evt.Defender.PrototypeName}.");
                    }

                    continue;
                }

                if (runState.BossUnlocked || ShouldCountKill(evt) == false)
                    continue;

                int previousKillCount = runState.CurrentKillCount;
                runState.AddKills(1);

                if (runState.BossUnlocked && previousKillCount < runState.Config.KillQuota)
                    Logger.Info($"Mythic Rift run {runState.Config.RunId} unlocked its boss after reaching {runState.CurrentKillCount}/{runState.Config.KillQuota} kills.");
            }
        }

        private static bool IsExpectedBossKill(MythicRiftRunState runState, in EntityDeadGameEvent evt)
        {
            if (runState == null || evt.Defender == null)
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
            if (recipientDbIds.Count == 0 && runState.RegionId != 0)
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
            if (evt.Killer == null || evt.Defender == null)
                return false;

            if (evt.Defender is Avatar)
                return false;

            if (evt.Defender is Agent == false)
                return false;

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
                RegionProtoRef = ResolvePrototype(definition.RegionPrototypeName),
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

        private sealed record MythicRiftContentDefinition(
            string Id,
            string DisplayName,
            string RegionPrototypeName,
            string MissionPrototypeName,
            string BossPrototypeName,
            string BossLootTablePrototypeName);
    }
}
