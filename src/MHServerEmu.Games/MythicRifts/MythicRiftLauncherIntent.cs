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

    public sealed class MythicRiftArmedLauncherState
    {
        public ulong PlayerDbId { get; init; }
        public TimeSpan ArmedAt { get; init; }
        public TimeSpan TimeLimit { get; init; }
        public int RequestedRiftLevel { get; init; }
        public string FixedContentId { get; init; }

        public bool IsValid => PlayerDbId != 0;
    }
}
