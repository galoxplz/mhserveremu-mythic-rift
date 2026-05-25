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
        public bool GrantBossLootOnFailure { get; set; } = true;
        public float TimedSuccessBonusRarityPct { get; set; } = 0.10f;
        public float TimedSuccessBonusSpecialPct { get; set; } = 0.15f;
        public float CheckpointSuccessBonusRarityPct { get; set; } = 0.05f;
        public float CheckpointSuccessBonusSpecialPct { get; set; } = 0.10f;
        public float FailureBonusRarityPct { get; set; } = 0f;
        public float FailureBonusSpecialPct { get; set; } = 0f;
        public List<MythicRiftExtraLootTableTuning> ExtraLootTables { get; set; } = new();

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
            ExtraLootTables ??= new();

            foreach (MythicRiftExtraLootTableTuning entry in ExtraLootTables)
                entry?.Normalize();
        }
    }

    public sealed class MythicRiftExtraLootTableTuning
    {
        public string Id { get; set; }
        public bool Enabled { get; set; } = true;
        public string LootTablePrototype { get; set; }
        public float ChancePercent { get; set; } = 100f;
        public int Rolls { get; set; } = 1;
        public int MinRiftLevel { get; set; } = 1;
        public int MaxRiftLevel { get; set; } = 0;
        public bool SuccessOnly { get; set; } = true;
        public bool CheckpointOnly { get; set; } = false;
        public bool ClassicOnly { get; set; } = false;
        public List<string> ContentIds { get; set; } = new();
        public List<string> BossSourceIds { get; set; } = new();

        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = string.IsNullOrWhiteSpace(LootTablePrototype) ? "unnamed-extra-loot" : LootTablePrototype;

            ChancePercent = Math.Clamp(ChancePercent, 0f, 100f);
            Rolls = Math.Max(Rolls, 1);
            MinRiftLevel = Math.Max(MinRiftLevel, 1);
            MaxRiftLevel = Math.Max(MaxRiftLevel, 0);
            ContentIds ??= new();
            BossSourceIds ??= new();
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
            if (riftLevel < MinRiftLevel)
                return false;

            if (MaxRiftLevel > 0 && riftLevel > MaxRiftLevel)
                return false;

            if (ContentIds.Count > 0 && ContentIds.Any(id => string.Equals(id, runState.Config.Content.Id, StringComparison.OrdinalIgnoreCase)) == false)
                return false;

            if (BossSourceIds.Count > 0 && BossSourceIds.Any(id => string.Equals(id, runState.Config.BossContent?.Id, StringComparison.OrdinalIgnoreCase)) == false)
                return false;

            return true;
        }
    }
}
