using MHServerEmu.Games.GameData;
using MHServerEmu.Games.MythicRifts;

namespace MHServerEmu.Games.Tests.MythicRifts
{
    public class MythicRiftRewardTuningTests
    {
        [Fact]
        public void CreateDefault_UsesDeliberateGroundRewardsOnlyOnSuccess()
        {
            MythicRiftRewardTuning tuning = MythicRiftRewardTuning.CreateDefault();

            Assert.Equal("ground", tuning.DefaultDelivery);
            Assert.True(tuning.GrantBossLootOnSuccess);
            Assert.False(tuning.GrantBossLootOnFailure);
            Assert.True(tuning.SuppressNativeRiftBossLoot);
        }

        [Fact]
        public void ResolveLootTableReference_UsesCaseInsensitiveAliases()
        {
            MythicRiftRewardTuning tuning = new()
            {
                LootTableAliases = new()
                {
                    ["Boss-Table"] = "Loot/Tables/Test/Boss.prototype"
                }
            };

            tuning.Normalize();

            Assert.Equal("Loot/Tables/Test/Boss.prototype", tuning.ResolveLootTableReference("boss-table"));
            Assert.Equal("Loot/Tables/Direct.prototype", tuning.ResolveLootTableReference("Loot/Tables/Direct.prototype"));
        }

        [Fact]
        public void RewardRecipe_AppliesToConfiguredBossAndLevel()
        {
            MythicRiftRewardRecipeTuning recipe = new()
            {
                MinRiftLevel = 20,
                MaxRiftLevel = 30,
                BossSourceIds = new() { "boss-a" },
                Tables = new()
                {
                    new() { LootTable = "boss-table" }
                }
            };
            recipe.Normalize("inventory");

            MythicRiftRunState matchingRun = new(CreateConfig("boss-a", 25));
            MythicRiftRunState wrongBossRun = new(CreateConfig("boss-b", 25));
            MythicRiftRunState wrongLevelRun = new(CreateConfig("boss-a", 31));

            Assert.True(recipe.AppliesTo(matchingRun, timedSuccess: true, checkpointSuccess: false));
            Assert.False(recipe.AppliesTo(wrongBossRun, timedSuccess: true, checkpointSuccess: false));
            Assert.False(recipe.AppliesTo(wrongLevelRun, timedSuccess: true, checkpointSuccess: false));
            Assert.False(recipe.AppliesTo(matchingRun, timedSuccess: false, checkpointSuccess: false));
        }

        [Fact]
        public void RewardRecipe_MatchesAnyBossInFinalWave()
        {
            MythicRiftRewardRecipeTuning recipe = new()
            {
                BossSourceIds = new() { "boss-b" },
                Tables = new() { new() { LootTable = "boss-table" } }
            };
            recipe.Normalize("ground");

            MythicRiftRunState run = new(CreateConfig("boss-a", 30, "boss-b"));

            Assert.True(recipe.AppliesTo(run, timedSuccess: true, checkpointSuccess: true));
        }

        [Fact]
        public void GuaranteedItem_AppliesByWaveAndCheckpoint()
        {
            MythicRiftGuaranteedItemTuning item = new()
            {
                ItemPrototypeRuntimeId = 123,
                MinWave = 20,
                MaxWave = 30,
                CheckpointOnly = true
            };
            item.Normalize("ground");

            MythicRiftRunState matchingRun = new(CreateConfig("boss-a", 25, waveNumber: 25));
            MythicRiftRunState earlyWaveRun = new(CreateConfig("boss-a", 19, waveNumber: 19));

            Assert.True(item.AppliesTo(matchingRun, timedSuccess: true, checkpointSuccess: true));
            Assert.False(item.AppliesTo(matchingRun, timedSuccess: true, checkpointSuccess: false));
            Assert.False(item.AppliesTo(earlyWaveRun, timedSuccess: true, checkpointSuccess: true));
            Assert.False(item.AppliesTo(matchingRun, timedSuccess: false, checkpointSuccess: true));
        }

        [Fact]
        public void RandomItemPool_NormalizesDirectoryAndAppliesByLevel()
        {
            MythicRiftRandomItemPoolTuning itemPool = new()
            {
                PrototypeDirectoryPrefix = @"Entity\Items\Artifacts\CosmicArtifacts",
                MinRiftLevel = 20,
                MaxRiftLevel = 30,
                ChancePercent = 150f,
                ItemLevel = 63
            };
            itemPool.Normalize("ground");

            Assert.Equal("Entity/Items/Artifacts/CosmicArtifacts/", itemPool.PrototypeDirectoryPrefix);
            Assert.Equal(100f, itemPool.ChancePercent);
            Assert.Equal(63, itemPool.ItemLevel);
            Assert.True(itemPool.AppliesTo(new(CreateConfig("boss-a", 25)), timedSuccess: true, checkpointSuccess: false));
            Assert.False(itemPool.AppliesTo(new(CreateConfig("boss-a", 31)), timedSuccess: true, checkpointSuccess: false));
            Assert.False(itemPool.AppliesTo(new(CreateConfig("boss-a", 25)), timedSuccess: false, checkpointSuccess: false));
        }

        private static MythicRiftRunConfig CreateConfig(
            string bossSourceId,
            int riftLevel,
            string additionalBossSourceId = null,
            int waveNumber = 1)
        {
            MythicRiftContentEntry mapContent = new()
            {
                Id = "map-a",
                DisplayName = "Map A",
                DefaultKillQuota = 10,
                RegionProtoRef = (PrototypeId)1UL,
                StartTargetProtoRef = (PrototypeId)2UL,
                BossProtoRef = (PrototypeId)3UL,
                BossLootTableProtoRef = (PrototypeId)4UL
            };
            MythicRiftContentEntry bossContent = new()
            {
                Id = bossSourceId,
                DisplayName = "Boss",
                DefaultKillQuota = 10,
                RegionProtoRef = (PrototypeId)1UL,
                StartTargetProtoRef = (PrototypeId)2UL,
                BossProtoRef = (PrototypeId)3UL,
                BossLootTableProtoRef = (PrototypeId)4UL
            };
            List<MythicRiftContentEntry> bossWaveContent = new() { bossContent };
            if (string.IsNullOrWhiteSpace(additionalBossSourceId) == false)
            {
                bossWaveContent.Add(new MythicRiftContentEntry
                {
                    Id = additionalBossSourceId,
                    DisplayName = "Additional Boss",
                    DefaultKillQuota = 10,
                    RegionProtoRef = (PrototypeId)1UL,
                    StartTargetProtoRef = (PrototypeId)2UL,
                    BossProtoRef = (PrototypeId)5UL,
                    BossLootTableProtoRef = (PrototypeId)6UL
                });
            }

            return new MythicRiftRunConfig
            {
                RunId = 1,
                RiftLevel = riftLevel,
                Content = mapContent,
                BossContent = bossContent,
                BossWaveContent = bossWaveContent,
                RequestedPlayerCount = 1,
                EffectivePlayerCount = 1,
                KillQuota = 10,
                TimeLimit = TimeSpan.FromMinutes(10),
                RegionProtoRef = mapContent.RegionProtoRef,
                StartTargetProtoRef = mapContent.StartTargetProtoRef,
                BossProtoRef = bossContent.BossProtoRef,
                BossLootTableProtoRef = bossContent.BossLootTableProtoRef,
                Difficulty = MythicRiftScaling.BuildSnapshot(riftLevel, 1),
                WaveNumber = waveNumber,
                RequiredBossKillCount = bossWaveContent.Count
            };
        }
    }
}
