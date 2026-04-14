using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftLauncherIntent
    {
        public ulong PlayerDbId { get; init; }
        public string ItemPrototypeName { get; init; }
        public string EntryPointId { get; init; }
        public PrototypeId PortalTargetRegionProtoRef { get; init; }
        public TimeSpan CreatedAt { get; init; }

        public bool IsValid => PlayerDbId != 0 && string.IsNullOrWhiteSpace(ItemPrototypeName) == false;
    }
}
