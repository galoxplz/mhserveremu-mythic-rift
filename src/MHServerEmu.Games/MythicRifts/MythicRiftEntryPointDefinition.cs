using System;
using System.Collections.Generic;

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
        public IReadOnlyList<string> AcceptedCandidateItemPrototypeNames { get; init; }
        public string CandidateTransitionPrototypeName { get; init; }
        public string Notes { get; init; }

        public bool IsValid => string.IsNullOrWhiteSpace(Id) == false;

        public bool AcceptsLauncherItemPrototypeName(string launcherItemPrototypeName)
        {
            if (string.IsNullOrWhiteSpace(launcherItemPrototypeName))
                return false;

            IReadOnlyList<string> acceptedCandidateItemPrototypeNames = AcceptedCandidateItemPrototypeNames;
            if (acceptedCandidateItemPrototypeNames != null)
            {
                foreach (string acceptedCandidateItemPrototypeName in acceptedCandidateItemPrototypeNames)
                {
                    if (string.Equals(acceptedCandidateItemPrototypeName, launcherItemPrototypeName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            if (string.IsNullOrWhiteSpace(CandidateItemPrototypeName))
                return true;

            return string.Equals(CandidateItemPrototypeName, launcherItemPrototypeName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
