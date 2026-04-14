using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftRunConfig
    {
        public ulong RunId { get; init; }
        public int RiftLevel { get; init; }
        public MythicRiftContentEntry Content { get; init; }
        public int RequestedPlayerCount { get; init; }
        public int EffectivePlayerCount { get; init; }
        public int KillQuota { get; init; }
        public TimeSpan TimeLimit { get; init; }
        public PrototypeId RegionProtoRef { get; init; }
        public PrototypeId MissionProtoRef { get; init; }
        public PrototypeId BossProtoRef { get; init; }
        public PrototypeId BossLootTableProtoRef { get; init; }
        public MythicRiftDifficultySnapshot Difficulty { get; init; }

        public bool IsValid =>
            RunId != 0 &&
            RiftLevel > 0 &&
            Content != null &&
            RegionProtoRef != PrototypeId.Invalid &&
            BossProtoRef != PrototypeId.Invalid &&
            TimeLimit > TimeSpan.Zero;
    }
}
