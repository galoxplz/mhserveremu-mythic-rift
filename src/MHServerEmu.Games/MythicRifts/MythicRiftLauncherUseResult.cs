using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftLauncherUseResult
    {
        public MythicRiftEntryResult EntryResult { get; set; }
        public MythicRiftLauncherItemCandidate Candidate { get; set; }
        public string ItemPrototypeName { get; set; }
        public PrototypeId PortalTargetRegionProtoRef { get; set; }
        public int ResolvedRiftLevel { get; set; }
        public TimeSpan ResolvedTimeLimit { get; set; }
        public string ErrorMessage { get; set; }

        public bool Success => EntryResult?.Success == true;
    }
}
