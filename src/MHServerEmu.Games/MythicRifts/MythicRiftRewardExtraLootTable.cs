using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftRewardExtraLootTable
    {
        public string Id { get; init; }
        public PrototypeId LootTableProtoRef { get; init; }
        public int Rolls { get; init; }
        public float ChancePercent { get; init; }
        public string Delivery { get; init; } = "inventory";
    }
}
