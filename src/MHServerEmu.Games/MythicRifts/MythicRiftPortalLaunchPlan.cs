namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftPortalLaunchPlan
    {
        public string EntryPointId { get; init; }
        public string LaunchModel { get; init; }
        public string LauncherItemPrototypeName { get; init; }
        public string TransitionPrototypeName { get; init; }
        public bool ConsumesLauncherItem { get; init; }
        public bool CreatesPrivatePortal { get; init; }
        public bool RandomContentOnly { get; init; }
        public bool IsPatcherFriendly { get; init; }
        public string Notes { get; init; }

        public bool IsValid => string.IsNullOrWhiteSpace(EntryPointId) == false;
    }
}
