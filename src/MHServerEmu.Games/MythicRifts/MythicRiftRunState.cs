namespace MHServerEmu.Games.MythicRifts
{
    public enum MythicRiftRunStatus
    {
        Pending,
        Active,
        Success,
        Failed,
        Aborted
    }

    public sealed class MythicRiftRunState
    {
        private readonly HashSet<ulong> _participantPlayerDbIds = new();
        private readonly HashSet<ulong> _rewardedPlayerDbIds = new();
        private readonly HashSet<ulong> _bossUnlockEligiblePlayerDbIds = new();
        private readonly HashSet<ulong> _progressionEligiblePlayerDbIds = new();
        private readonly HashSet<ulong> _participantsSeenInRunRegion = new();
        private readonly HashSet<int> _sentTimeWarningThresholds = new();
        private readonly HashSet<int> _sentKillProgressMilestones = new();

        public MythicRiftRunConfig Config { get; }
        public MythicRiftRunStatus Status { get; private set; } = MythicRiftRunStatus.Pending;
        public TimeSpan RegisteredAt { get; private set; }
        public ulong RegionId { get; private set; }
        public ulong BossEntityId { get; private set; }
        public ulong ExitPortalEntityId { get; private set; }
        public int CurrentKillCount { get; private set; }
        public bool BossUnlocked { get; private set; }
        public bool RewardsGranted { get; private set; }
        public MythicRiftRewardOutcome RewardOutcome { get; private set; }
        public TimeSpan StartedAt { get; private set; }
        public TimeSpan? CompletedAt { get; private set; }
        public TimeSpan? ExpiresAt { get; private set; }
        public TimeSpan LastParticipantOnlineAt { get; private set; }
        public bool RegionDifficultyScalingApplied { get; private set; }
        public float RegionPlayerToMobDamageMultiplierBeforeScaling { get; private set; } = 1f;
        public float RegionMobToPlayerDamageMultiplierBeforeScaling { get; private set; } = 1f;
        public IReadOnlyCollection<ulong> ParticipantPlayerDbIds => _participantPlayerDbIds;
        public IReadOnlyCollection<ulong> RewardedPlayerDbIds => _rewardedPlayerDbIds;
        public IReadOnlyCollection<ulong> BossUnlockEligiblePlayerDbIds => _bossUnlockEligiblePlayerDbIds;
        public IReadOnlyCollection<ulong> ProgressionEligiblePlayerDbIds => _progressionEligiblePlayerDbIds;
        public IReadOnlyCollection<ulong> ParticipantsSeenInRunRegionPlayerDbIds => _participantsSeenInRunRegion;
        public int ParticipantCount => _participantPlayerDbIds.Count;
        public int RewardedPlayerCount => _rewardedPlayerDbIds.Count;
        public bool IsInProgress => Status == MythicRiftRunStatus.Pending || Status == MythicRiftRunStatus.Active;

        public MythicRiftRunState(MythicRiftRunConfig config)
        {
            Config = config;
        }

        public void AttachRegion(ulong regionId)
        {
            RegionId = regionId;
        }

        public void AttachBoss(ulong bossEntityId)
        {
            BossEntityId = bossEntityId;
        }

        public void AttachExitPortal(ulong exitPortalEntityId)
        {
            ExitPortalEntityId = exitPortalEntityId;
        }

        public void CaptureRegionDifficultyScaling(float playerToMobDamageMultiplier, float mobToPlayerDamageMultiplier)
        {
            RegionDifficultyScalingApplied = true;
            RegionPlayerToMobDamageMultiplierBeforeScaling = playerToMobDamageMultiplier;
            RegionMobToPlayerDamageMultiplierBeforeScaling = mobToPlayerDamageMultiplier;
        }

        public void ClearRegionDifficultyScaling()
        {
            RegionDifficultyScalingApplied = false;
            RegionPlayerToMobDamageMultiplierBeforeScaling = 1f;
            RegionMobToPlayerDamageMultiplierBeforeScaling = 1f;
        }

        public void SetRegisteredAt(TimeSpan registeredAt)
        {
            RegisteredAt = registeredAt;
            LastParticipantOnlineAt = registeredAt;
        }

        public void Start(TimeSpan startedAt)
        {
            if (Status != MythicRiftRunStatus.Pending)
                return;

            Status = MythicRiftRunStatus.Active;
            StartedAt = startedAt;
            ExpiresAt = startedAt + Config.TimeLimit;
        }

        public void TouchParticipantPresence(TimeSpan currentTime)
        {
            LastParticipantOnlineAt = currentTime;
        }

        public void AddKills(int count)
        {
            if (count <= 0 || Status != MythicRiftRunStatus.Active)
                return;

            CurrentKillCount += count;
            if (CurrentKillCount >= Config.KillQuota)
                BossUnlocked = true;
        }

        public void MarkSuccess(TimeSpan completedAt)
        {
            if (Status != MythicRiftRunStatus.Active)
                return;

            Status = MythicRiftRunStatus.Success;
            CompletedAt = completedAt;
        }

        public void MarkFailed(TimeSpan completedAt)
        {
            if (Status != MythicRiftRunStatus.Active)
                return;

            Status = MythicRiftRunStatus.Failed;
            CompletedAt = completedAt;
        }

        public void MarkAborted(TimeSpan completedAt)
        {
            if (Status == MythicRiftRunStatus.Success || Status == MythicRiftRunStatus.Failed || Status == MythicRiftRunStatus.Aborted)
                return;

            Status = MythicRiftRunStatus.Aborted;
            CompletedAt = completedAt;
        }

        public void MarkRewardsGranted()
        {
            RewardsGranted = true;
        }

        public bool RegisterParticipant(ulong playerDbId)
        {
            if (playerDbId == 0)
                return false;

            return _participantPlayerDbIds.Add(playerDbId);
        }

        public bool MarkParticipantSeenInRunRegion(ulong playerDbId)
        {
            if (playerDbId == 0)
                return false;

            return _participantsSeenInRunRegion.Add(playerDbId);
        }

        public bool HasParticipantBeenSeenInRunRegion(ulong playerDbId)
        {
            return playerDbId != 0 && _participantsSeenInRunRegion.Contains(playerDbId);
        }

        public bool HasRewardForPlayer(ulong playerDbId)
        {
            return playerDbId != 0 && _rewardedPlayerDbIds.Contains(playerDbId);
        }

        public bool MarkRewardGrantedToPlayer(ulong playerDbId)
        {
            if (playerDbId == 0)
                return false;

            bool added = _rewardedPlayerDbIds.Add(playerDbId);
            if (_rewardedPlayerDbIds.Count > 0)
                RewardsGranted = true;

            return added;
        }

        public void SetRewardOutcome(MythicRiftRewardOutcome rewardOutcome)
        {
            RewardOutcome = rewardOutcome;
        }

        public void SnapshotBossUnlockEligiblePlayers(IEnumerable<ulong> playerDbIds)
        {
            _bossUnlockEligiblePlayerDbIds.Clear();
            if (playerDbIds == null)
                return;

            foreach (ulong playerDbId in playerDbIds)
            {
                if (playerDbId != 0)
                    _bossUnlockEligiblePlayerDbIds.Add(playerDbId);
            }
        }

        public void SnapshotProgressionEligiblePlayers(IEnumerable<ulong> playerDbIds)
        {
            _progressionEligiblePlayerDbIds.Clear();
            if (playerDbIds == null)
                return;

            foreach (ulong playerDbId in playerDbIds)
            {
                if (playerDbId != 0 && _bossUnlockEligiblePlayerDbIds.Contains(playerDbId))
                    _progressionEligiblePlayerDbIds.Add(playerDbId);
            }
        }

        public bool MarkTimeWarningSent(int thresholdSeconds)
        {
            return thresholdSeconds > 0 && _sentTimeWarningThresholds.Add(thresholdSeconds);
        }

        public bool MarkKillProgressMilestoneSent(int milestonePercent)
        {
            return milestonePercent > 0 && _sentKillProgressMilestones.Add(milestonePercent);
        }

        public bool HasExpired(TimeSpan currentTime)
        {
            return Status == MythicRiftRunStatus.Active
                && ExpiresAt.HasValue
                && currentTime >= ExpiresAt.Value;
        }

        public TimeSpan GetTimeRemaining(TimeSpan currentTime)
        {
            if (ExpiresAt.HasValue == false)
                return Config.TimeLimit;

            TimeSpan remaining = ExpiresAt.Value - currentTime;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
}
