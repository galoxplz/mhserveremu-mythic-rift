using System.Linq;
using MHServerEmu.Games.MythicRifts;

namespace MHServerEmu.Games.Tests.MythicRifts
{
    public class MythicRiftLauncherSelectionTests
    {
        [Fact]
        public void ConsumableEntryPoints_UseIndependentStandardAndEndlessLaunchers()
        {
            MythicRiftEntryService entryService = new(null);

            MythicRiftEntryPointDefinition standardEntryPoint = entryService.GetEntryPoint(MythicRiftEntryService.ConsumablePortalEntryPointId);
            MythicRiftEntryPointDefinition endlessEntryPoint = entryService.GetEntryPoint(MythicRiftEntryService.EndlessConsumablePortalEntryPointId);

            Assert.NotNull(standardEntryPoint);
            Assert.Equal(MythicRiftMode.Standard, standardEntryPoint.Mode);
            Assert.Equal(MythicRiftLauncherService.CosmicRiftBeaconPrototypeName, standardEntryPoint.CandidateItemPrototypeName);
            Assert.Equal(MythicRiftLauncherService.PreferredCosmicRiftBeaconPrototypeName, standardEntryPoint.CandidateItemPrototypeName);
            Assert.True(standardEntryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.CosmicRiftBeaconPrototypeName));
            Assert.True(standardEntryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.PresentationCosmicRiftBeaconPrototypeName));
            Assert.False(standardEntryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.PresentationEndlessRiftBeaconPrototypeName));

            Assert.NotNull(endlessEntryPoint);
            Assert.Equal(MythicRiftMode.Endless, endlessEntryPoint.Mode);
            Assert.Equal(MythicRiftLauncherService.EndlessRiftBeaconPrototypeName, endlessEntryPoint.CandidateItemPrototypeName);
            Assert.True(endlessEntryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.EndlessRiftBeaconPrototypeName));
            Assert.True(endlessEntryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.EndlessRiftBeaconPrototypePath));
            Assert.True(endlessEntryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.PresentationEndlessRiftBeaconPrototypeName));
            Assert.True(endlessEntryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.PresentationEndlessRiftBeaconPrototypePath));
            Assert.False(endlessEntryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.PresentationCosmicRiftBeaconPrototypeName));
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

            MythicRiftLauncherItemCandidate endlessCandidate = entryService.LauncherItemCandidates.Single(candidate =>
                candidate.PrototypeName == MythicRiftLauncherService.EndlessRiftBeaconPrototypeName);
            MythicRiftLauncherItemCandidate endlessPresentationCandidate = entryService.LauncherItemCandidates.Single(candidate =>
                candidate.PrototypeName == MythicRiftLauncherService.PresentationEndlessRiftBeaconPrototypeName);

            Assert.Equal("chosen-endless", endlessCandidate.Recommendation);
            Assert.Equal("chosen-endless-presentation", endlessPresentationCandidate.Recommendation);
            Assert.Equal(MythicRiftItemPresentation.EndlessPresentationDisplayName, endlessPresentationCandidate.DisplayName);
            Assert.DoesNotContain(entryService.LauncherItemCandidates, candidate =>
                candidate.Recommendation == "compatibility");
        }
    }
}
