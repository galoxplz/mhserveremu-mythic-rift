using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftRewardOutcome
    {
        public PrototypeId BossLootTableProtoRef { get; init; }
        public string BossLootTableSourceId { get; init; }
        public string BossLootDelivery { get; init; } = "inventory";
        public string RewardProfileName { get; init; }
        public bool TimedSuccessBonusApplied { get; init; }
        public float BonusRarityPct { get; init; }
        public float BonusSpecialPct { get; init; }
        public IReadOnlyList<MythicRiftRewardExtraLootTable> ExtraLootTables { get; init; } = Array.Empty<MythicRiftRewardExtraLootTable>();

        public bool HasBossLootTable => BossLootTableProtoRef != PrototypeId.Invalid;
        public bool HasAnyLoot => HasBossLootTable || ExtraLootTables.Count > 0;
    }
}
