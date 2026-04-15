using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftLauncherUseResult
    {
        public MythicRiftEntryResult EntryResult { get; set; }
        public MythicRiftLauncherItemCandidate Candidate { get; set; }
        public string ItemPrototypeName { get; set; }
        public ulong ItemEntityId { get; set; }
        public PrototypeId PortalTargetRegionProtoRef { get; set; }
        public PrototypeId TeleportTargetProtoRef { get; set; }
        public int ResolvedRiftLevel { get; set; }
        public TimeSpan ResolvedTimeLimit { get; set; }
        public bool ConsumedArmedLaunchMode { get; set; }
        public bool InterceptedItemUse { get; set; }
        public bool UsedTrackedBeaconInstance { get; set; }
        public bool TeleportAttempted { get; set; }
        public bool TeleportSucceeded { get; set; }
        public string TeleportErrorMessage { get; set; }
        public string ErrorMessage { get; set; }

        public bool Success => EntryResult?.Success == true;
    }
}
