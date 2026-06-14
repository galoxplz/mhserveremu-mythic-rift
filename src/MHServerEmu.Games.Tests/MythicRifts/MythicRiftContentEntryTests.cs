using MHServerEmu.Games.GameData;
using MHServerEmu.Games.MythicRifts;

namespace MHServerEmu.Games.Tests.MythicRifts
{
    public class MythicRiftContentEntryTests
    {
        [Fact]
        public void IsValid_AllowsBossOnlySource()
        {
            MythicRiftContentEntry content = new()
            {
                Id = "boss-only",
                DisplayName = "Boss Only",
                DefaultKillQuota = 1,
                RandomMapEligible = false,
                RandomBossEligible = true,
                BossProtoRef = (PrototypeId)100UL,
                BossLootTableProtoRef = (PrototypeId)200UL
            };

            Assert.True(content.IsValid);
            Assert.False(content.HasValidMap);
            Assert.True(content.HasValidBossSource);
        }

        [Fact]
        public void IsValid_RejectsBossOnlySourceAsRandomMap()
        {
            MythicRiftContentEntry content = new()
            {
                Id = "boss-only",
                DisplayName = "Boss Only",
                DefaultKillQuota = 1,
                RandomMapEligible = true,
                RandomBossEligible = true,
                BossProtoRef = (PrototypeId)100UL,
                BossLootTableProtoRef = (PrototypeId)200UL
            };

            Assert.False(content.IsValid);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(2, false)]
        public void SupportsPlayerCount_UsesContentLimit(int playerCount, bool expected)
        {
            MythicRiftContentEntry content = new()
            {
                MaxPlayerCount = 1
            };

            Assert.Equal(expected, content.SupportsPlayerCount(playerCount));
        }
    }
}
