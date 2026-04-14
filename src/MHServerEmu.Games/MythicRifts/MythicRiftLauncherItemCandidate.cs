namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftLauncherItemCandidate
    {
        public string PrototypeName { get; init; }
        public string DisplayName { get; init; }
        public string SourceFamily { get; init; }
        public bool IsLikelyUnusedOrLowRisk { get; init; }
        public bool IsShopLinked { get; init; }
        public bool SupportsRandomThemeIdentity { get; init; }
        public bool PatcherFriendly { get; init; }
        public string Recommendation { get; init; }
        public string Notes { get; init; }

        public bool IsValid => string.IsNullOrWhiteSpace(PrototypeName) == false;
    }
}
