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
            Assert.InRange(snapshot.EquivalentD3RiftLevel, 9.999f, 10.001f);
            Assert.InRange(snapshot.GroupHealthMultiplier, 0.999f, 1.001f);
            Assert.InRange(snapshot.HealthMultiplier, 4.107f, 4.109f);
            Assert.InRange(snapshot.DamageMultiplier, 3.003f, 3.005f);
        }

        [Fact]
        public void BuildSnapshot_LevelTwentyFiveFourPlayers_AppliesGroupHealthOnlyToHealth()
        {
            MythicRiftDifficultySnapshot soloSnapshot = MythicRiftScaling.BuildSnapshot(25, 1);
            MythicRiftDifficultySnapshot groupSnapshot = MythicRiftScaling.BuildSnapshot(25, 4);

            Assert.Equal(4, groupSnapshot.EffectivePlayerCount);
            Assert.InRange(groupSnapshot.EquivalentD3RiftLevel, 9.999f, 10.001f);
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

        [Theory]
        [InlineData(1, 1, 1.00f, 1.00f)]
        [InlineData(10, 2, 2.50f, 5.00f)]
        [InlineData(20, 3, 5.00f, 15.00f)]
        [InlineData(25, 4, 5.00f, 20.00f)]
        [InlineData(28, 5, 6.50f, 32.50f)]
        [InlineData(30, 6, 7.00f, 42.00f)]
        [InlineData(31, 1, 1.00f, 1.00f)]
        [InlineData(60, 6, 7.00f, 42.00f)]
        public void GetThirtyWaveProfile_MatchesThirtyWaveTable(
            int riftLevel,
            int expectedBossCount,
            float expectedPerBossMultiplier,
            float expectedTotalMultiplier)
        {
            MythicRiftWaveProfile profile = MythicRiftScaling.GetThirtyWaveProfile(riftLevel);

            Assert.Equal(((riftLevel - 1) % 30) + 1, profile.Wave);
            Assert.Equal(expectedBossCount, profile.BossCount);
            Assert.Equal(expectedPerBossMultiplier, profile.PerBossHealthMultiplier);
            Assert.Equal(expectedTotalMultiplier, profile.TotalBossHealthMultiplier);
        }

        [Fact]
        public void BuildSnapshot_ThirtyWaveModeUsesPerBossScalingWithoutPartyHealthStacking()
        {
            MythicRiftDifficultySnapshot solo = MythicRiftScaling.BuildSnapshot(20, 1, useThirtyWaveMode: true);
            MythicRiftDifficultySnapshot party = MythicRiftScaling.BuildSnapshot(20, 4, useThirtyWaveMode: true);

            Assert.Equal(20f, solo.EquivalentD3RiftLevel);
            Assert.Equal(5f, solo.HealthMultiplier);
            Assert.Equal(5f, party.HealthMultiplier);
            Assert.Equal(1f, party.GroupHealthMultiplier);
            Assert.Equal(4, party.EffectivePlayerCount);
        }
    }
}
