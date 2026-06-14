using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftRewardGuaranteedItem
    {
        public string Id { get; init; }
        public PrototypeId ItemProtoRef { get; init; }
        public int Quantity { get; init; }
        public int ItemLevel { get; init; } = 1;
        public string Delivery { get; init; } = "ground";
    }
}
