using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftContentEntry
    {
        public string Id { get; init; }
        public string DisplayName { get; init; }
        public int DefaultKillQuota { get; init; }
        public PrototypeId RegionProtoRef { get; init; }
        public PrototypeId MissionProtoRef { get; init; }
        public PrototypeId BossProtoRef { get; init; }
        public PrototypeId BossLootTableProtoRef { get; init; }

        public bool IsValid =>
            string.IsNullOrWhiteSpace(Id) == false &&
            DefaultKillQuota > 0 &&
            RegionProtoRef != PrototypeId.Invalid &&
            BossProtoRef != PrototypeId.Invalid;
    }
}
