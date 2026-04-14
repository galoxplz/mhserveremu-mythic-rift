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

        public MythicRiftRunConfig Config { get; }
        public MythicRiftRunStatus Status { get; private set; } = MythicRiftRunStatus.Pending;
        public ulong RegionId { get; private set; }
        public ulong BossEntityId { get; private set; }
        public int CurrentKillCount { get; private set; }
        public bool BossUnlocked { get; private set; }
        public bool RewardsGranted { get; private set; }
        public MythicRiftRewardOutcome RewardOutcome { get; private set; }
        public TimeSpan StartedAt { get; private set; }
        public TimeSpan? CompletedAt { get; private set; }
        public TimeSpan? ExpiresAt { get; private set; }
        public IReadOnlyCollection<ulong> ParticipantPlayerDbIds => _participantPlayerDbIds;
        public IReadOnlyCollection<ulong> RewardedPlayerDbIds => _rewardedPlayerDbIds;
        public int ParticipantCount => _participantPlayerDbIds.Count;
        public int RewardedPlayerCount => _rewardedPlayerDbIds.Count;

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

        public void Start(TimeSpan startedAt)
        {
            if (Status != MythicRiftRunStatus.Pending)
                return;

            Status = MythicRiftRunStatus.Active;
            StartedAt = startedAt;
            ExpiresAt = startedAt + Config.TimeLimit;
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
