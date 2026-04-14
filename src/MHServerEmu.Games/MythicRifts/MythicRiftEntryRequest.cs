namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftEntryRequest
    {
        public string EntryPointId { get; init; }
        public string LauncherItemPrototypeName { get; init; }
        public int RiftLevel { get; init; }
        public string ContentId { get; init; }
        public int? KillQuotaOverride { get; init; }
        public TimeSpan TimeLimit { get; init; }

        public bool HasFixedContent => string.IsNullOrWhiteSpace(ContentId) == false;
        public bool HasKillQuotaOverride => KillQuotaOverride.HasValue && KillQuotaOverride.Value > 0;
        public bool HasLauncherItemPrototypeName => string.IsNullOrWhiteSpace(LauncherItemPrototypeName) == false;
    }
}
