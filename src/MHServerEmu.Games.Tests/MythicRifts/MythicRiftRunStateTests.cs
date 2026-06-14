using MHServerEmu.Games.GameData;
using MHServerEmu.Games.MythicRifts;

namespace MHServerEmu.Games.Tests.MythicRifts
{
    public class MythicRiftRunStateTests
    {
        [Fact]
        public void MarkParticipantLeftEarly_RemovesParticipantAndPreventsReRegistration()
        {
            MythicRiftRunState runState = new(CreateConfig());

            Assert.True(runState.RegisterParticipant(100));
            Assert.True(runState.MarkParticipantSeenInRunRegion(100));

            Assert.True(runState.MarkParticipantLeftEarly(100));

            Assert.Empty(runState.ParticipantPlayerDbIds);
            Assert.Contains(100UL, runState.EarlyExitPlayerDbIds);
            Assert.False(runState.RegisterParticipant(100));
            Assert.False(runState.MarkParticipantSeenInRunRegion(100));
        }

        [Fact]
        public void MarkParticipantLeftEarly_DoesNotRemoveRemainingParticipants()
        {
            MythicRiftRunState runState = new(CreateConfig());

            runState.RegisterParticipant(100);
            runState.RegisterParticipant(200);

            Assert.True(runState.MarkParticipantLeftEarly(100));

            Assert.DoesNotContain(100UL, runState.ParticipantPlayerDbIds);
            Assert.Contains(200UL, runState.ParticipantPlayerDbIds);
            Assert.Single(runState.ParticipantPlayerDbIds);
        }

        [Fact]
        public void UnlockBoss_MarksQuotaCompleteForCheckpointRuns()
        {
            MythicRiftRunState runState = new(CreateConfig());

            runState.Start(TimeSpan.Zero);
            runState.UnlockBoss();

            Assert.True(runState.BossUnlocked);
            Assert.Equal(runState.Config.KillQuota, runState.CurrentKillCount);
        }

        [Fact]
        public void FinalizeAdmission_RebuildsScalingFromSuccessfullyAdmittedPlayers()
        {
            MythicRiftRunState runState = new(CreateConfig(requestedPlayerCount: 4));
            runState.RegisterParticipant(100);
            runState.RegisterParticipant(200);
            runState.RegisterParticipant(300);
            runState.RegisterParticipant(400);
            runState.EnableAdmissionTracking();

            Assert.True(runState.MarkParticipantAdmitted(100));
            Assert.True(runState.MarkParticipantAdmitted(200));
            Assert.True(runState.RemoveParticipantBeforeAdmission(300));
            Assert.True(runState.RemoveParticipantBeforeAdmission(400));

            runState.FinalizeAdmission();

            Assert.True(runState.AdmissionFinalized);
            Assert.Equal(2, runState.AdmittedPlayerCount);
            Assert.Equal(2, runState.EffectivePlayerCount);
            Assert.InRange(runState.Difficulty.GroupHealthMultiplier, 1.999f, 2.001f);
        }

        [Fact]
        public void SnapshotRewardEligiblePlayers_RequiresRegisteredNonExitedParticipants()
        {
            MythicRiftRunState runState = new(CreateConfig());
            runState.RegisterParticipant(100);
            runState.RegisterParticipant(200);
            runState.MarkParticipantSeenInRunRegion(100);
            runState.MarkParticipantSeenInRunRegion(200);
            runState.MarkParticipantLeftEarly(200);

            runState.SnapshotRewardEligiblePlayers(new[] { 100UL, 200UL, 300UL });

            Assert.True(runState.IsRewardEligible(100));
            Assert.False(runState.IsRewardEligible(200));
            Assert.False(runState.IsRewardEligible(300));
        }

        [Fact]
        public void BossWave_TracksEachSpawnAndRequiresEveryBossDefeat()
        {
            MythicRiftRunState runState = new(CreateConfig(requiredBossKillCount: 3));

            runState.AttachBoss(1000);
            runState.AttachBoss(2000);
            runState.AttachBoss(3000);

            Assert.Equal(3, runState.BossSpawnCount);
            Assert.Equal(3, runState.ActiveBossEntityIds.Count);
            Assert.True(runState.MarkBossDefeated(2000));
            Assert.Equal(1, runState.BossKillCount);
            Assert.Equal(2, runState.ActiveBossEntityIds.Count);
            Assert.False(runState.MarkBossDefeated(2000));

            Assert.True(runState.MarkBossDefeated(1000));
            Assert.True(runState.MarkBossDefeated(3000));
            Assert.Equal(runState.Config.RequiredBossKillCount, runState.BossKillCount);
            Assert.Empty(runState.ActiveBossEntityIds);
        }

        [Fact]
        public void MilestoneEncounter_CanOnlyBeMarkedOnce()
        {
            MythicRiftRunState runState = new(CreateConfig());

            Assert.False(runState.HasSpawnedMilestoneEncounter(50));
            Assert.True(runState.MarkMilestoneEncounterSpawned(50));
            Assert.True(runState.HasSpawnedMilestoneEncounter(50));
            Assert.False(runState.MarkMilestoneEncounterSpawned(50));
        }

        private static MythicRiftRunConfig CreateConfig(int requestedPlayerCount = 1, int requiredBossKillCount = 1)
        {
            MythicRiftContentEntry content = new()
            {
                Id = "test",
                DisplayName = "Test",
                DefaultKillQuota = 10,
                RegionProtoRef = (PrototypeId)1UL,
                StartTargetProtoRef = (PrototypeId)2UL,
                BossProtoRef = (PrototypeId)3UL,
                BossLootTableProtoRef = (PrototypeId)4UL
            };

            return new MythicRiftRunConfig
            {
                RunId = 1,
                RiftLevel = 1,
                Content = content,
                BossContent = content,
                BossWaveContent = new[] { content },
                RequestedPlayerCount = requestedPlayerCount,
                EffectivePlayerCount = MythicRiftScaling.GetEffectivePlayerCount(requestedPlayerCount),
                KillQuota = 10,
                TimeLimit = TimeSpan.FromMinutes(10),
                RegionProtoRef = content.RegionProtoRef,
                StartTargetProtoRef = content.StartTargetProtoRef,
                BossProtoRef = content.BossProtoRef,
                BossLootTableProtoRef = content.BossLootTableProtoRef,
                Difficulty = MythicRiftScaling.BuildSnapshot(1, requestedPlayerCount),
                RequiredBossKillCount = requiredBossKillCount
            };
        }
    }
}
