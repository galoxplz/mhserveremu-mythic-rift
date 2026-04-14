using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftRewardOutcome
    {
        public PrototypeId BossLootTableProtoRef { get; init; }
        public bool TimedSuccessBonusApplied { get; init; }
        public float BonusRarityPct { get; init; }
        public float BonusSpecialPct { get; init; }

        public bool HasBossLootTable => BossLootTableProtoRef != PrototypeId.Invalid;
    }
}
