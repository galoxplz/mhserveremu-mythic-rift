using System.Linq;
using MHServerEmu.Games.MythicRifts;

namespace MHServerEmu.Games.Tests.MythicRifts
{
    public class MythicRiftLauncherSelectionTests
    {
        [Fact]
        public void ConsumableEntryPoint_UsesCurrentChosenBeaconPrototype()
        {
            MythicRiftEntryService entryService = new(null);

            MythicRiftEntryPointDefinition entryPoint = entryService.GetEntryPoint(MythicRiftEntryService.ConsumablePortalEntryPointId);

            Assert.NotNull(entryPoint);
            Assert.Equal(MythicRiftLauncherService.CosmicRiftBeaconPrototypeName, entryPoint.CandidateItemPrototypeName);
            Assert.Equal(MythicRiftLauncherService.PreferredCosmicRiftBeaconPrototypeName, entryPoint.CandidateItemPrototypeName);
            Assert.True(entryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.CosmicRiftBeaconPrototypeName));
            Assert.True(entryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.PresentationCosmicRiftBeaconPrototypeName));
            Assert.True(entryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.PresentationCosmicRiftBeaconPrototypePath));
            Assert.Contains(MythicRiftLauncherService.PresentationCosmicRiftBeaconPrototypeName, entryPoint.AcceptedCandidateItemPrototypeNames);
            Assert.Contains(MythicRiftLauncherService.PresentationCosmicRiftBeaconPrototypePath, entryPoint.AcceptedCandidateItemPrototypeNames);
        }

        [Fact]
        public void LauncherCandidates_PreferMaxAffixWithoutLegacyDangerRoomCompatibility()
        {
            MythicRiftEntryService entryService = new(null);

            MythicRiftLauncherItemCandidate chosenCandidate = entryService.LauncherItemCandidates.Single(candidate =>
                candidate.PrototypeName == MythicRiftLauncherService.CosmicRiftBeaconPrototypeName);

            Assert.Equal("chosen", chosenCandidate.Recommendation);
            Assert.True(chosenCandidate.IsLikelyUnusedOrLowRisk);

            MythicRiftLauncherItemCandidate presentationCandidate = entryService.LauncherItemCandidates.Single(candidate =>
                candidate.PrototypeName == MythicRiftLauncherService.PresentationCosmicRiftBeaconPrototypeName);

            Assert.Equal("chosen-presentation", presentationCandidate.Recommendation);
            Assert.Equal(MythicRiftItemPresentation.PresentationDisplayName, presentationCandidate.DisplayName);
            Assert.DoesNotContain(entryService.LauncherItemCandidates, candidate =>
                candidate.Recommendation == "compatibility");
        }
    }
}
