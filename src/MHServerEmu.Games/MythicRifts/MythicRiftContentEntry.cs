using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftContentEntry
    {
        public string Id { get; init; }
        public string DisplayName { get; init; }
        public int DefaultKillQuota { get; init; }
        public bool RandomMapEligible { get; init; } = true;
        public bool RandomBossEligible { get; init; } = true;
        public bool IsSpecialRandomMap { get; init; }
        public bool UseOwnBossSourceWhenSelected { get; init; }
        public bool UseCustomPopulation { get; init; }
        public bool BossOnlyCheckpointEligible { get; init; }
        public int MinRandomRiftLevel { get; init; } = 1;
        public int MaxRandomRiftLevel { get; init; }
        public PrototypeId RegionProtoRef { get; init; }
        public PrototypeId StartTargetProtoRef { get; init; }
        public PrototypeId MissionProtoRef { get; init; }
        public PrototypeId BossProtoRef { get; init; }
        public PrototypeId BossLootTableProtoRef { get; init; }

        public bool HasValidMap =>
            string.IsNullOrWhiteSpace(Id) == false &&
            DefaultKillQuota > 0 &&
            RegionProtoRef != PrototypeId.Invalid &&
            StartTargetProtoRef != PrototypeId.Invalid;

        public bool HasValidBossSource =>
            BossProtoRef != PrototypeId.Invalid &&
            BossLootTableProtoRef != PrototypeId.Invalid;

        public bool IsValid =>
            HasValidMap &&
            (RandomBossEligible == false || HasValidBossSource);

        public bool CanAppearAtRandomRiftLevel(int riftLevel)
        {
            int normalizedLevel = Math.Max(riftLevel, 1);
            if (normalizedLevel < Math.Max(MinRandomRiftLevel, 1))
                return false;

            return MaxRandomRiftLevel <= 0 || normalizedLevel <= MaxRandomRiftLevel;
        }
    }
}
