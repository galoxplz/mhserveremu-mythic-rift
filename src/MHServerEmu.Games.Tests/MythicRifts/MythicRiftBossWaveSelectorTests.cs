using MHServerEmu.Games.GameData;
using MHServerEmu.Games.MythicRifts;

namespace MHServerEmu.Games.Tests.MythicRifts
{
    public class MythicRiftBossWaveSelectorTests
    {
        [Fact]
        public void BuildDistinctRoster_SelectsDifferentBossesForMultiBossWave()
        {
            MythicRiftContentEntry primary = CreateBoss("primary", 10);
            MythicRiftContentEntry duplicateEntry = CreateBoss("primary-copy", 10);
            List<MythicRiftContentEntry> candidates = new()
            {
                primary,
                duplicateEntry,
                CreateBoss("boss-b", 20),
                CreateBoss("boss-c", 30),
                CreateBoss("boss-d", 40),
                CreateBoss("boss-e", 50),
                CreateBoss("boss-f", 60)
            };

            IReadOnlyList<MythicRiftContentEntry> roster = MythicRiftBossWaveSelector.BuildDistinctRoster(
                primary,
                candidates,
                requestedCount: 6,
                nextIndex: count => count - 1);

            Assert.Equal(6, roster.Count);
            Assert.Same(primary, roster[0]);
            Assert.Equal(roster.Count, roster.Select(entry => entry.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal(roster.Count, roster.Select(entry => entry.BossProtoRef).Distinct().Count());
        }

        [Fact]
        public void BuildDistinctRoster_ReturnsAvailableUniqueBossesWhenPoolIsSmall()
        {
            MythicRiftContentEntry primary = CreateBoss("primary", 10);

            IReadOnlyList<MythicRiftContentEntry> roster = MythicRiftBossWaveSelector.BuildDistinctRoster(
                primary,
                new[] { primary, CreateBoss("boss-b", 20) },
                requestedCount: 6,
                nextIndex: _ => 0);

            Assert.Equal(2, roster.Count);
        }

        private static MythicRiftContentEntry CreateBoss(string id, ulong bossPrototypeId)
        {
            return new MythicRiftContentEntry
            {
                Id = id,
                DisplayName = id,
                RegionProtoRef = (PrototypeId)1UL,
                StartTargetProtoRef = (PrototypeId)2UL,
                BossProtoRef = (PrototypeId)bossPrototypeId,
                BossLootTableProtoRef = (PrototypeId)(bossPrototypeId + 1)
            };
        }
    }
}
