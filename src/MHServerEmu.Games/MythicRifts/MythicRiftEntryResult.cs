namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftEntryResult
    {
        public MythicRiftRunState RunState { get; init; }
        public MythicRiftPortalLaunchPlan LaunchPlan { get; init; }
        public string ErrorMessage { get; init; }

        public bool Success => RunState != null;
    }
}
