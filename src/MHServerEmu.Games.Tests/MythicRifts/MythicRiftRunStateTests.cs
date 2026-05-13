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

        private static MythicRiftRunConfig CreateConfig()
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
                RequestedPlayerCount = 1,
                EffectivePlayerCount = 1,
                KillQuota = 10,
                TimeLimit = TimeSpan.FromMinutes(10),
                RegionProtoRef = content.RegionProtoRef,
                StartTargetProtoRef = content.StartTargetProtoRef,
                BossProtoRef = content.BossProtoRef,
                BossLootTableProtoRef = content.BossLootTableProtoRef,
                Difficulty = MythicRiftScaling.BuildSnapshot(1, 1)
            };
        }
    }
}
