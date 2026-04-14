namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftEntryPointDefinition
    {
        public string Id { get; init; }
        public string DisplayName { get; init; }
        public bool AllowsRandomContent { get; init; }
        public bool AllowsFixedContentSelection { get; init; }
        public bool IsPatcherFriendly { get; init; }
        public string LaunchModel { get; init; }
        public string CandidateItemPrototypeName { get; init; }
        public string CandidateTransitionPrototypeName { get; init; }
        public string Notes { get; init; }

        public bool IsValid => string.IsNullOrWhiteSpace(Id) == false;
    }
}
