using MHServerEmu.Games.MythicRifts;

namespace MHServerEmu.Games.Tests.MythicRifts
{
    public class MythicRiftScalingTests
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        [InlineData(4, 4)]
        [InlineData(5, 4)]
        public void GetEffectivePlayerCount_UsesExpectedBuckets(int requestedPlayers, int expectedEffectivePlayers)
        {
            Assert.Equal(expectedEffectivePlayers, MythicRiftScaling.GetEffectivePlayerCount(requestedPlayers));
        }

        [Theory]
        [InlineData(1, 1.0f)]
        [InlineData(2, 2.0f)]
        [InlineData(3, 3.0f)]
        [InlineData(4, 4.0f)]
        [InlineData(5, 4.0f)]
        public void GetGroupHealthMultiplier_FollowsNormalizedD3GreaterRiftBuckets(int requestedPlayers, float expectedGroupHealthMultiplier)
        {
            float actualMultiplier = MythicRiftScaling.GetGroupHealthMultiplier(requestedPlayers);
            Assert.InRange(actualMultiplier, expectedGroupHealthMultiplier - 0.001f, expectedGroupHealthMultiplier + 0.001f);
        }

        [Fact]
        public void BuildSnapshot_LevelTwentyFiveSolo_UsesCompressedD3EquivalentScaling()
        {
            MythicRiftDifficultySnapshot snapshot = MythicRiftScaling.BuildSnapshot(25, 1);

            Assert.Equal(25, snapshot.RiftLevel);
            Assert.Equal(1, snapshot.EffectivePlayerCount);
            Assert.InRange(snapshot.EquivalentD3RiftLevel, 10.599f, 10.601f);
            Assert.InRange(snapshot.GroupHealthMultiplier, 0.999f, 1.001f);
            Assert.InRange(snapshot.HealthMultiplier, 4.513f, 4.515f);
            Assert.InRange(snapshot.DamageMultiplier, 3.232f, 3.234f);
        }

        [Fact]
        public void BuildSnapshot_LevelTwentyFiveFourPlayers_AppliesGroupHealthOnlyToHealth()
        {
            MythicRiftDifficultySnapshot soloSnapshot = MythicRiftScaling.BuildSnapshot(25, 1);
            MythicRiftDifficultySnapshot groupSnapshot = MythicRiftScaling.BuildSnapshot(25, 4);

            Assert.Equal(4, groupSnapshot.EffectivePlayerCount);
            Assert.InRange(groupSnapshot.EquivalentD3RiftLevel, 10.599f, 10.601f);
            Assert.InRange(groupSnapshot.GroupHealthMultiplier, 3.999f, 4.001f);
            Assert.InRange(groupSnapshot.HealthMultiplier, (soloSnapshot.HealthMultiplier * 4f) - 0.001f, (soloSnapshot.HealthMultiplier * 4f) + 0.001f);
            Assert.InRange(groupSnapshot.DamageMultiplier, soloSnapshot.DamageMultiplier - 0.001f, soloSnapshot.DamageMultiplier + 0.001f);
        }

        [Fact]
        public void BuildSnapshot_HigherRiftLevelsRemainMonotonic()
        {
            MythicRiftDifficultySnapshot levelTen = MythicRiftScaling.BuildSnapshot(10, 1);
            MythicRiftDifficultySnapshot levelTwentyFive = MythicRiftScaling.BuildSnapshot(25, 1);
            MythicRiftDifficultySnapshot levelFifty = MythicRiftScaling.BuildSnapshot(50, 1);

            Assert.True(levelTwentyFive.EquivalentD3RiftLevel > levelTen.EquivalentD3RiftLevel);
            Assert.True(levelFifty.EquivalentD3RiftLevel > levelTwentyFive.EquivalentD3RiftLevel);
            Assert.True(levelTwentyFive.HealthMultiplier > levelTen.HealthMultiplier);
            Assert.True(levelFifty.HealthMultiplier > levelTwentyFive.HealthMultiplier);
            Assert.True(levelTwentyFive.DamageMultiplier > levelTen.DamageMultiplier);
            Assert.True(levelFifty.DamageMultiplier > levelTwentyFive.DamageMultiplier);
        }
    }
}
