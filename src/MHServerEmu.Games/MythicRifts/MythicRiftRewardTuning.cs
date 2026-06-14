using System.Text.Json;
using MHServerEmu.Core.Helpers;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftRewardTuning
    {
        public const string RelativeConfigPath = "Game/MythicRift/CosmicRiftRewards.json";

        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true
        };

        public string ProfileName { get; set; } = "default";
        public bool Enabled { get; set; } = true;
        public bool GrantBossLootOnSuccess { get; set; } = true;
        public bool GrantBossLootOnFailure { get; set; } = false;
        public bool SuppressNativeRiftBossLoot { get; set; } = true;
        public float TimedSuccessBonusRarityPct { get; set; } = 0.10f;
        public float TimedSuccessBonusSpecialPct { get; set; } = 0.15f;
        public float CheckpointSuccessBonusRarityPct { get; set; } = 0.05f;
        public float CheckpointSuccessBonusSpecialPct { get; set; } = 0.10f;
        public float FailureBonusRarityPct { get; set; } = 0f;
        public float FailureBonusSpecialPct { get; set; } = 0f;
        public string DefaultDelivery { get; set; } = "ground";
        public Dictionary<string, string> LootTableAliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<MythicRiftPrimaryLootTableTuning> PrimaryLootTableOverrides { get; set; } = new();
        public List<MythicRiftExtraLootTableTuning> ExtraLootTables { get; set; } = new();
        public List<MythicRiftRewardRecipeTuning> RewardRecipes { get; set; } = new();
        public List<MythicRiftRandomItemPoolTuning> RandomItemPools { get; set; } = new();
        public List<MythicRiftGuaranteedItemTuning> GuaranteedItems { get; set; } = new();

        public static string ConfigPath => Path.Combine(FileHelper.DataDirectory, RelativeConfigPath);

        public static MythicRiftRewardTuning CreateDefault()
        {
            MythicRiftRewardTuning tuning = new();
            tuning.Normalize();
            return tuning;
        }

        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(ProfileName))
                ProfileName = "default";

            TimedSuccessBonusRarityPct = Math.Max(TimedSuccessBonusRarityPct, 0f);
            TimedSuccessBonusSpecialPct = Math.Max(TimedSuccessBonusSpecialPct, 0f);
            CheckpointSuccessBonusRarityPct = Math.Max(CheckpointSuccessBonusRarityPct, 0f);
            CheckpointSuccessBonusSpecialPct = Math.Max(CheckpointSuccessBonusSpecialPct, 0f);
            FailureBonusRarityPct = Math.Max(FailureBonusRarityPct, 0f);
            FailureBonusSpecialPct = Math.Max(FailureBonusSpecialPct, 0f);
            DefaultDelivery = NormalizeDelivery(DefaultDelivery);
            LootTableAliases = LootTableAliases == null
                ? new(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(LootTableAliases, StringComparer.OrdinalIgnoreCase);
            PrimaryLootTableOverrides ??= new();
            ExtraLootTables ??= new();
            RewardRecipes ??= new();
            RandomItemPools ??= new();
            GuaranteedItems ??= new();

            foreach (MythicRiftPrimaryLootTableTuning entry in PrimaryLootTableOverrides)
                entry?.Normalize(DefaultDelivery);

            foreach (MythicRiftExtraLootTableTuning entry in ExtraLootTables)
                entry?.Normalize(DefaultDelivery);

            foreach (MythicRiftRewardRecipeTuning recipe in RewardRecipes)
                recipe?.Normalize(DefaultDelivery);

            foreach (MythicRiftRandomItemPoolTuning itemPool in RandomItemPools)
                itemPool?.Normalize(DefaultDelivery);

            foreach (MythicRiftGuaranteedItemTuning item in GuaranteedItems)
                item?.Normalize(DefaultDelivery);
        }

        public string ResolveLootTableReference(string lootTableReference)
        {
            if (string.IsNullOrWhiteSpace(lootTableReference))
                return string.Empty;

            string normalizedReference = lootTableReference.Trim();
            return LootTableAliases.TryGetValue(normalizedReference, out string prototypePath) &&
                   string.IsNullOrWhiteSpace(prototypePath) == false
                ? prototypePath.Trim()
                : normalizedReference;
        }

        public static string NormalizeDelivery(string delivery)
        {
            return string.Equals(delivery, "ground", StringComparison.OrdinalIgnoreCase)
                ? "ground"
                : "inventory";
        }

        public static bool IsGroundDelivery(string delivery)
        {
            return string.Equals(NormalizeDelivery(delivery), "ground", StringComparison.OrdinalIgnoreCase);
        }

        public static bool MatchesBossSourceIds(MythicRiftRunState runState, IReadOnlyCollection<string> bossSourceIds)
        {
            if (bossSourceIds == null || bossSourceIds.Count == 0)
                return true;

            if (runState?.Config == null)
                return false;

            IEnumerable<MythicRiftContentEntry> bossWaveContent = runState.Config.BossWaveContent.Count > 0
                ? runState.Config.BossWaveContent
                : new[] { runState.Config.BossContent };

            return bossWaveContent
                .Where(content => content != null)
                .Any(content => bossSourceIds.Any(id => string.Equals(id, content.Id, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public abstract class MythicRiftLootTableTuningBase
    {
        public string Id { get; set; }
        public bool Enabled { get; set; } = true;
        public string LootTablePrototype { get; set; }
        public int MinRiftLevel { get; set; } = 1;
        public int MaxRiftLevel { get; set; } = 0;
        public bool CheckpointOnly { get; set; } = false;
        public bool ClassicOnly { get; set; } = false;
        public List<string> ContentIds { get; set; } = new();
        public List<string> BossSourceIds { get; set; } = new();
        public string Delivery { get; set; }

        public virtual void Normalize(string defaultDelivery)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = string.IsNullOrWhiteSpace(LootTablePrototype) ? "unnamed-loot-table" : LootTablePrototype;

            MinRiftLevel = Math.Max(MinRiftLevel, 1);
            MaxRiftLevel = Math.Max(MaxRiftLevel, 0);
            ContentIds ??= new();
            BossSourceIds ??= new();
            Delivery = string.IsNullOrWhiteSpace(Delivery)
                ? MythicRiftRewardTuning.NormalizeDelivery(defaultDelivery)
                : MythicRiftRewardTuning.NormalizeDelivery(Delivery);
        }

        public bool AppliesTo(MythicRiftRunState runState, bool checkpointSuccess)
        {
            if (Enabled == false || runState?.Config == null)
                return false;

            if (CheckpointOnly && checkpointSuccess == false)
                return false;

            if (ClassicOnly && runState.Config.Content.BossOnlyCheckpointEligible)
                return false;

            int riftLevel = runState.Config.RiftLevel;
            if (riftLevel < MinRiftLevel)
                return false;

            if (MaxRiftLevel > 0 && riftLevel > MaxRiftLevel)
                return false;

            if (ContentIds.Count > 0 && ContentIds.Any(id => string.Equals(id, runState.Config.Content.Id, StringComparison.OrdinalIgnoreCase)) == false)
                return false;

            if (MythicRiftRewardTuning.MatchesBossSourceIds(runState, BossSourceIds) == false)
                return false;

            return true;
        }
    }

    public sealed class MythicRiftPrimaryLootTableTuning : MythicRiftLootTableTuningBase
    {
    }

    public sealed class MythicRiftExtraLootTableTuning : MythicRiftLootTableTuningBase
    {
        public float ChancePercent { get; set; } = 100f;
        public int Rolls { get; set; } = 1;
        public bool SuccessOnly { get; set; } = true;

        public override void Normalize(string defaultDelivery)
        {
            base.Normalize(defaultDelivery);
            ChancePercent = Math.Clamp(ChancePercent, 0f, 100f);
            Rolls = Math.Max(Rolls, 1);
        }

        public bool AppliesTo(MythicRiftRunState runState, bool timedSuccess, bool checkpointSuccess)
        {
            if (SuccessOnly && timedSuccess == false)
                return false;

            return base.AppliesTo(runState, checkpointSuccess);
        }
    }

    public sealed class MythicRiftRewardRecipeTuning
    {
        public string Id { get; set; }
        public bool Enabled { get; set; } = true;
        public int MinRiftLevel { get; set; } = 1;
        public int MaxRiftLevel { get; set; } = 0;
        public bool SuccessOnly { get; set; } = true;
        public bool CheckpointOnly { get; set; }
        public bool ClassicOnly { get; set; }
        public List<string> ContentIds { get; set; } = new();
        public List<string> BossSourceIds { get; set; } = new();
        public List<MythicRiftRewardRecipeTableTuning> Tables { get; set; } = new();

        public void Normalize(string defaultDelivery)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = "unnamed-reward-recipe";

            MinRiftLevel = Math.Max(MinRiftLevel, 1);
            MaxRiftLevel = Math.Max(MaxRiftLevel, 0);
            ContentIds ??= new();
            BossSourceIds ??= new();
            Tables ??= new();

            foreach (MythicRiftRewardRecipeTableTuning table in Tables)
                table?.Normalize(defaultDelivery);
        }

        public bool AppliesTo(MythicRiftRunState runState, bool timedSuccess, bool checkpointSuccess)
        {
            if (Enabled == false || runState?.Config == null)
                return false;

            if (SuccessOnly && timedSuccess == false)
                return false;

            if (CheckpointOnly && checkpointSuccess == false)
                return false;

            if (ClassicOnly && runState.Config.Content.BossOnlyCheckpointEligible)
                return false;

            int riftLevel = runState.Config.RiftLevel;
            if (riftLevel < MinRiftLevel || (MaxRiftLevel > 0 && riftLevel > MaxRiftLevel))
                return false;

            if (ContentIds.Count > 0 &&
                ContentIds.Any(id => string.Equals(id, runState.Config.Content.Id, StringComparison.OrdinalIgnoreCase)) == false)
            {
                return false;
            }

            if (MythicRiftRewardTuning.MatchesBossSourceIds(runState, BossSourceIds) == false)
            {
                return false;
            }

            return true;
        }
    }

    public sealed class MythicRiftRewardRecipeTableTuning
    {
        public string Id { get; set; }
        public string LootTable { get; set; }
        public float ChancePercent { get; set; } = 100f;
        public int Rolls { get; set; } = 1;
        public string Delivery { get; set; }

        public void Normalize(string defaultDelivery)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = string.IsNullOrWhiteSpace(LootTable) ? "unnamed-recipe-table" : LootTable;

            ChancePercent = Math.Clamp(ChancePercent, 0f, 100f);
            Rolls = Math.Max(Rolls, 1);
            Delivery = string.IsNullOrWhiteSpace(Delivery)
                ? MythicRiftRewardTuning.NormalizeDelivery(defaultDelivery)
                : MythicRiftRewardTuning.NormalizeDelivery(Delivery);
        }
    }

    public sealed class MythicRiftGuaranteedItemTuning
    {
        public string Id { get; set; }
        public bool Enabled { get; set; } = true;
        public ulong ItemPrototypeRuntimeId { get; set; }
        public int Quantity { get; set; } = 1;
        public int MinRiftLevel { get; set; } = 1;
        public int MaxRiftLevel { get; set; }
        public int MinWave { get; set; } = 1;
        public int MaxWave { get; set; }
        public bool SuccessOnly { get; set; } = true;
        public bool CheckpointOnly { get; set; }
        public bool ClassicOnly { get; set; }
        public List<string> ContentIds { get; set; } = new();
        public List<string> BossSourceIds { get; set; } = new();
        public string Delivery { get; set; }

        public void Normalize(string defaultDelivery)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = ItemPrototypeRuntimeId == 0 ? "unnamed-guaranteed-item" : ItemPrototypeRuntimeId.ToString();

            Quantity = Math.Max(Quantity, 1);
            MinRiftLevel = Math.Max(MinRiftLevel, 1);
            MaxRiftLevel = Math.Max(MaxRiftLevel, 0);
            MinWave = Math.Max(MinWave, 1);
            MaxWave = Math.Max(MaxWave, 0);
            ContentIds ??= new();
            BossSourceIds ??= new();
            Delivery = string.IsNullOrWhiteSpace(Delivery)
                ? MythicRiftRewardTuning.NormalizeDelivery(defaultDelivery)
                : MythicRiftRewardTuning.NormalizeDelivery(Delivery);
        }

        public bool AppliesTo(MythicRiftRunState runState, bool timedSuccess, bool checkpointSuccess)
        {
            if (Enabled == false || ItemPrototypeRuntimeId == 0 || runState?.Config == null)
                return false;

            if (SuccessOnly && timedSuccess == false)
                return false;

            if (CheckpointOnly && checkpointSuccess == false)
                return false;

            if (ClassicOnly && runState.Config.Content.BossOnlyCheckpointEligible)
                return false;

            int riftLevel = runState.Config.RiftLevel;
            if (riftLevel < MinRiftLevel || (MaxRiftLevel > 0 && riftLevel > MaxRiftLevel))
                return false;

            int wave = Math.Max(runState.Config.WaveNumber, 1);
            if (wave < MinWave || (MaxWave > 0 && wave > MaxWave))
                return false;

            if (ContentIds.Count > 0 &&
                ContentIds.Any(id => string.Equals(id, runState.Config.Content.Id, StringComparison.OrdinalIgnoreCase)) == false)
            {
                return false;
            }

            return MythicRiftRewardTuning.MatchesBossSourceIds(runState, BossSourceIds);
        }
    }

    public sealed class MythicRiftRandomItemPoolTuning
    {
        public string Id { get; set; }
        public bool Enabled { get; set; } = true;
        public string PrototypeDirectoryPrefix { get; set; }
        public float ChancePercent { get; set; } = 100f;
        public int Rolls { get; set; } = 1;
        public int ItemLevel { get; set; } = 1;
        public int MinRiftLevel { get; set; } = 1;
        public int MaxRiftLevel { get; set; }
        public bool SuccessOnly { get; set; } = true;
        public bool CheckpointOnly { get; set; }
        public bool ClassicOnly { get; set; }
        public List<string> ContentIds { get; set; } = new();
        public List<string> BossSourceIds { get; set; } = new();
        public string Delivery { get; set; }

        public void Normalize(string defaultDelivery)
        {
            PrototypeDirectoryPrefix = PrototypeDirectoryPrefix?.Trim().Replace('\\', '/') ?? string.Empty;
            if (PrototypeDirectoryPrefix.Length > 0 && PrototypeDirectoryPrefix.EndsWith('/') == false)
                PrototypeDirectoryPrefix += "/";

            if (string.IsNullOrWhiteSpace(Id))
                Id = string.IsNullOrWhiteSpace(PrototypeDirectoryPrefix) ? "unnamed-random-item-pool" : PrototypeDirectoryPrefix;

            ChancePercent = Math.Clamp(ChancePercent, 0f, 100f);
            Rolls = Math.Max(Rolls, 1);
            ItemLevel = Math.Max(ItemLevel, 1);
            MinRiftLevel = Math.Max(MinRiftLevel, 1);
            MaxRiftLevel = Math.Max(MaxRiftLevel, 0);
            ContentIds ??= new();
            BossSourceIds ??= new();
            Delivery = string.IsNullOrWhiteSpace(Delivery)
                ? MythicRiftRewardTuning.NormalizeDelivery(defaultDelivery)
                : MythicRiftRewardTuning.NormalizeDelivery(Delivery);
        }

        public bool AppliesTo(MythicRiftRunState runState, bool timedSuccess, bool checkpointSuccess)
        {
            if (Enabled == false || string.IsNullOrWhiteSpace(PrototypeDirectoryPrefix) || runState?.Config == null)
                return false;

            if (SuccessOnly && timedSuccess == false)
                return false;

            if (CheckpointOnly && checkpointSuccess == false)
                return false;

            if (ClassicOnly && runState.Config.Content.BossOnlyCheckpointEligible)
                return false;

            int riftLevel = runState.Config.RiftLevel;
            if (riftLevel < MinRiftLevel || (MaxRiftLevel > 0 && riftLevel > MaxRiftLevel))
                return false;

            if (ContentIds.Count > 0 &&
                ContentIds.Any(id => string.Equals(id, runState.Config.Content.Id, StringComparison.OrdinalIgnoreCase)) == false)
            {
                return false;
            }

            return MythicRiftRewardTuning.MatchesBossSourceIds(runState, BossSourceIds);
        }
    }
}
